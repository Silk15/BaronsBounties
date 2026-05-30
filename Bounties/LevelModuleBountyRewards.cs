using System.Collections;
using System.Collections.Generic;
using System.Linq;
using ThunderRoad;
using TMPro;
using TriInspector;
using UnityEngine;

namespace BaronsBounties.Bounties
{
    [DeclareHorizontalGroup("Horizontal")]
    public class LevelModuleBountyRewards : LevelModule
    {
        [Group("Horizontal")]
        public string bountyRewardPrefabAddress;
        
        [Dropdown(nameof(GetAllItemID))]
        [Group("Horizontal")]
        public string crateItemId;
        
        [Title("Positioning")]
        public Vector3 position;
        public Vector3 rotation;

        [Title("Bounty Voicelines")]
        public string bountyBoardDialogId;
        public string levelExplanationDialogId;
        public string bountyRewardDialogId;
        
        [Title("Reward Voicelines")]
        public string crateRewardDialogId;

        private Transform[] cratePoints;
        private ItemData crateItemData;
        private ItemData notepadItemData;
        
        #if !SDK
        
        public override IEnumerator OnPlayerSpawnCoroutine()
        {
            Shop.local.onTransactionStart -= ShopOnOnTransactionStart;
            Shop.local.onTransactionStart += ShopOnOnTransactionStart;
            Shop.local.onTransactionCompleted -= OnTransactionCompleted;
            Shop.local.onTransactionCompleted += OnTransactionCompleted;
            Shop.local.onPlayerChangeZone -= OnPlayerChangeZone;
            Shop.local.onPlayerChangeZone += OnPlayerChangeZone;

            notepadItemData = Catalog.GetData<ItemData>("Notepad");
            crateItemData = Catalog.GetData<ItemData>(crateItemId);
            
            yield return Catalog.InstantiateCoroutine<GameObject>(bountyRewardPrefabAddress, bountyBoard =>
            {
                bountyBoard.transform.SetPositionAndRotation(position, Quaternion.Euler(rotation));
                
                Transform parent = bountyBoard.transform.GetChild(0);
                cratePoints = new Transform[parent.childCount];
                for (int i = 0; i < parent.childCount; i++) cratePoints[i] = parent.GetChild(i);
                SpawnCrates();
        
            }, "Bounty Reward Prefab");
        }

        private void OnPlayerChangeZone(Shop.ShopZone zone, bool entered)
        {
            BountySaveData.BountySave bountySave = BountySaveData.instance.GetSave(Player.characterData.ID);
            if (bountySave.heardIntro || Shop.local.tutorialRunning || Shop.local.moduleShopkeeper.IsSpeaking()) return;
            BrainModuleSpeak brainModuleSpeak = Shop.local.shopkeeper.brain.instance.GetModule<BrainModuleSpeak>(false);
            BrainModuleShopkeeper brainModuleShopkeeper = Shop.local.moduleShopkeeper;

            int currentIndex = 0;
            string[] dialogs = new[]
            {
                bountyBoardDialogId,
                levelExplanationDialogId,
                bountyRewardDialogId
            };

            bountySave.heardIntro = true;
            BountySaveData.SaveAsync();
            
            brainModuleSpeak.OnSpeakEnd += PlayNextDialog;
            brainModuleSpeak.moveMouth = true;
            if (!brainModuleSpeak.isSpeaking) PlayNextDialog(false);

            void PlayNextDialog(bool end)
            {
                if (currentIndex >= dialogs.Length)
                {
                    brainModuleSpeak.OnSpeakEnd -= PlayNextDialog;
                    return;
                }

                brainModuleShopkeeper.DelayedSpeak(() =>
                {
                    brainModuleShopkeeper.SayDialog(dialogs[currentIndex]);
                    currentIndex++;
                }, 0.5f, 2f);
            }
        }

        public override void OnUnload()
        {
            base.OnUnload();
            Shop.local.onTransactionStart -= ShopOnOnTransactionStart;
            Shop.local.onTransactionCompleted -= OnTransactionCompleted;
            
            foreach (BountyContent bountyContent in BountySaveData.instance.GetSave(Player.characterData.ID).completedBounties)
                bountyContent.IsSpawned = false;
        }

        private void ShopOnOnTransactionStart(Shop.Transaction transaction)
        {
            if (transaction != Shop.Transaction.Sell) return;

            BountySaveData.BountySave bountySave = BountySaveData.instance.GetSave(Player.characterData.ID);
            Dictionary<string, int> itemCounts = new();
            foreach (Item item in Shop.local.sellingItems)
            {
                if (!itemCounts.ContainsKey(item.data.id)) itemCounts[item.data.id] = 0;
                itemCounts[item.data.id]++;
            }

            foreach (BountyContent bountyContent in bountySave.activeBounties)
            {
                foreach (BountyData.BountyItem bountyItem in bountyContent.BountyData.bountyItemIds)
                    if (itemCounts.TryGetValue(bountyItem, out int count) && count > 0)
                    {
                        bountyContent.soldItems.Add(bountyItem);
                        itemCounts[bountyItem]--;
                    }
            }
        }

        private void OnTransactionCompleted(Shop.Transaction transaction, bool successful)
        {
            BountySaveData.BountySave bountySave = BountySaveData.instance.GetSave(Player.characterData.ID);
            
            foreach (BountyContent completed in bountySave.completedBounties)
                if (completed.HasAllRewards) completed.crateIndex = -1;

            bool completedBounty = false;
            for (int i = bountySave.activeBounties.Count - 1; i >= 0; i--)
            {
                BountyContent bountyContent = bountySave.activeBounties[i];
                if (bountyContent.IsComplete)
                {
                    bountySave.completedBounties.Add(bountyContent);
                    bountySave.activeBounties.RemoveAt(i);
                    
                    UIBountyBoard.instance.bountyInfo.ClearBounty();
                    UIBountyBoard.instance.bountyInfo.RefreshTabs();
                    
                    bountyContent.crateIndex = GetEmptyCrateIndex(); 
                    completedBounty = true;
                    BountySaveData.SaveAsync();
                }
            }
            
            if (!completedBounty) return;
            SpawnCrates();
            BrainModuleSpeak brainModuleSpeak = Shop.local.shopkeeper.brain.instance.GetModule<BrainModuleSpeak>(false);
            BrainModuleShopkeeper brainModuleShopkeeper = Shop.local.moduleShopkeeper;
         
            brainModuleSpeak.OnSpeakEnd += PlayDialog;
            brainModuleSpeak.moveMouth = true;
            if (!brainModuleSpeak.isSpeaking) PlayDialog(false);
            
            void PlayDialog(bool end)
            {
                brainModuleSpeak.OnSpeakEnd -= PlayDialog;
                brainModuleShopkeeper.DelayedSpeak(() =>
                {
                    brainModuleShopkeeper.SayDialog(crateRewardDialogId);
                }, 0.5f, 2f);
            }
        }

        public void SpawnCrates()
        {
            BountySaveData.BountySave bountySave = BountySaveData.instance.GetSave(Player.characterData.ID);
            if (bountySave.completedBounties.Count == 0) return;

            for (int i = bountySave.completedBounties.Count - 1; i >= 0; i--)
            {
                BountyContent bountyContent = bountySave.completedBounties[i];
                if (bountyContent.HasAllRewards) continue;
                SpawnRewardCrate(bountyContent);
            }
        }

        public void SpawnRewardCrate(BountyContent bountyContent)
        {
            if (bountyContent.crateIndex == -1)
                bountyContent.crateIndex = GetEmptyCrateIndex();
            
            if (bountyContent.crateIndex == -1 || bountyContent.IsSpawned)
                return;
            
            bountyContent.IsSpawned = true;
            Transform point = cratePoints[bountyContent.crateIndex];
            crateItemData.SpawnAsync(crate =>
            {
                crate.transform.localScale = new Vector3(crate.transform.localScale.x * 1.25f, crate.transform.localScale.y, crate.transform.localScale.z * 1.25f);
                foreach (Reward reward in bountyContent.RemainingRewards)
                {
                    Reward thisReward = reward;
                    thisReward.Spawn(crate.Center, 0.25f, item =>
                    {
                        item.OnGrabEvent -= OnGrabEvent;
                        item.OnGrabEvent += OnGrabEvent;

                        void OnGrabEvent(Handle handle, RagdollHand ragdollHand)
                        {
                            thisReward.MarkAsOwned(handle.item);
                            handle.item.SetOwner(Item.Owner.Player);
                            handle.item.OnGrabEvent -= OnGrabEvent;
                            BountySaveData.SaveAsync();
                        }
                    });
                }
            }, point.position, point.rotation);
            SpawnManifest(bountyContent, point.position);
        }

        public void SpawnManifest(BountyContent bountyContent, Vector3 basePosition)
        {
            basePosition.z += 0.6f;
            basePosition.x -= 0.2f;
            notepadItemData.SpawnAsync(item =>
            {
                TextMeshPro textMeshPro = item.GetComponentInChildren<TextMeshPro>();
                Object.Destroy(textMeshPro.GetComponent<UIText>());
                
                textMeshPro.text = $"{bountyContent.BountyData.BountyDisplayName}\n\nRewards:\n- " + string.Join("\n- ", bountyContent?.RemainingRewards?.Select(r => r?.CargoData?.displayName));
            }, basePosition, Quaternion.Euler(0, -18.642f, 0));
        }

        public int GetEmptyCrateIndex()
        {
            BountySaveData.BountySave bountySave = BountySaveData.instance.GetSave(Player.characterData.ID);
            HashSet<int> usedIndices = new(bountySave.completedBounties.Select(b => b.crateIndex));
            for (int i = 0; i < cratePoints.Length; i++) if (!usedIndices.Contains(i)) return i;
            return -1;
        }
        
        #endif
        
        public TriDropdownList<string> GetAllItemID() => Catalog.GetDropdownAllID(Category.Item);
    }
}