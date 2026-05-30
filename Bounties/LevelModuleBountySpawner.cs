using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using BaronsBounties.Bounties.Modules;
using ThunderRoad;
using UnityEngine;

#if !SDK
using ThunderRoad.DebugViz;
using UnityEngine.Events;
#endif

namespace BaronsBounties.Bounties
{
    public class LevelModuleBountySpawner : LevelModule, IBountyInteractor
    {
        #if !SDK
        public string levelBountyId;
        
        [NonSerialized]
        public int lastSpawnIndex = 0;

        [NonSerialized]
        public ItemData[] validItemData;
        
        [NonSerialized]
        public HashSet<string> visitedAreas = new();
        
        [NonSerialized]
        public HashSet<string> spawnRoomIds = new();

        [NonSerialized]
        public LevelBountyData levelBountyData;
        
        [NonSerialized]
        public LevelBountyContent levelBountyContent;

        public UnityEvent<DisplayMessage.MessageData> onSkip = new();

        public bool IsTreasure(ItemSpawner itemSpawner)
        {
            if (itemSpawner.priority == ItemSpawner.Priority.IgnoreOnAndroid) return false;
            return itemSpawner.spawnerType != ItemSpawner.SpawnerType.UseReferenceId && itemSpawner.spawnerType != ItemSpawner.SpawnerType.EnemyDrop;
        }

        public void OnLoad(LevelBountyData levelBountyData)
        {
            this.levelBountyData = levelBountyData;
            levelBountyId = levelBountyData.id;
        }

        public override IEnumerator OnLoadCoroutine()
        {
            if (AreaManager.Instance != null)
            {
                if (levelBountyData == null) levelBountyData = Catalog.GetData<LevelBountyData>(levelBountyId);
                if (levelBountyData == null)
                {
                    Debug.LogError("[Baron's Bounties] Given ID was null or empty, this module is useless :(");
                    return base.OnLoadCoroutine();
                }

                AreaManager.Instance.OnPlayerChangeAreaEvent -= OnAreaChangeEvent;
                AreaManager.Instance.OnPlayerChangeAreaEvent += OnAreaChangeEvent;
                EventManager.OnDungeonSuccessEvent -= OnDungeonSuccessEvent;
                EventManager.OnDungeonSuccessEvent += OnDungeonSuccessEvent;
                
                BountyContent bountyContent = BountySaveData.instance.GetSave(Player.characterData.ID).GetActiveBounty(levelBountyId);
                if (bountyContent is LevelBountyContent levelBountyContent)
                {
                    this.levelBountyContent = levelBountyContent;
                    validItemData = levelBountyData?.bountyItemData?.Where(b => !levelBountyContent.foundItems.Contains(b?.id))?.ToArray();
                    validItemData.Shuffle();
                }
                
                int itemCount = validItemData.Length;

                List<string> validRoomIds = AreaManager.Instance.CurrentTree
                    .Where(node =>
                    {
                        if (node.SpawnedArea == null) return false;
                        return node.SpawnedArea.GetComponentsInChildren<ItemSpawner>(true).Any(s => IsTreasure(s));
                    }).Select(node => node.AreaDataId).ToList().Shuffle();

                spawnRoomIds = validRoomIds.Take(itemCount).ToHashSet();
                Debug.Log($"[Baron's Bounties] Relic bounty loaded! Items will spawn in the following rooms: {string.Join(", ", spawnRoomIds)}");
            }
            else Debug.LogError("[Baron's Bounties] Level is not an area!");
            return base.OnLoadCoroutine();
        }

        public override IEnumerator OnPlayerSpawnCoroutine()
        {
            BountySaveData.BountySave bountySave = BountySaveData.instance.GetSave(Player.characterData.ID);
            if (bountySave.isFirstLevelBounty)
            {
                onSkip.AddListener(Skip);
                DisplayMessage.instance.ShowMessage(new DisplayMessage.MessageData("You have entered a Relic Outpost! \n\nA relic is hidden somewhere in these old ruins but it may not be where you'd expect. Make sure you check thoroughly before moving on and good luck!", 1, 2f, null, null, false, true, warnPlayer: true, anchorType: MessageAnchorType.PlayerForwardWorldPosition, dismissAutomatically: false, onMessageSkip: onSkip));
                void Skip(DisplayMessage.MessageData data) => bountySave.isFirstLevelBounty = false;
            }
            return base.OnPlayerSpawnCoroutine();
        }

        public override void OnUnload()
        {
            base.OnUnload();
            AreaManager.Instance.OnPlayerChangeAreaEvent -= OnAreaChangeEvent;
            EventManager.OnDungeonSuccessEvent -= OnDungeonSuccessEvent;
            spawnRoomIds.Clear();
            visitedAreas.Clear();
            BountySaveData.SaveAsync();
        }
        
        private void OnAreaChangeEvent(SpawnableArea newArea, SpawnableArea previousArea)
        {
            if (!visitedAreas.Contains(newArea.AreaDataId))
            {
                visitedAreas.Add(newArea.AreaDataId);
                if (spawnRoomIds.Contains(newArea.AreaDataId) && lastSpawnIndex <= validItemData.Length - 1)
                {
                    List<ItemSpawner> validSpawners = new();
                    foreach (ItemSpawner itemSpawner in newArea.SpawnedArea.GetComponentsInChildren<ItemSpawner>(true))
                        if (IsTreasure(itemSpawner)) validSpawners.Add(itemSpawner);

                    ItemSpawner spawner = validSpawners.Shuffle().RandomChoice();
                    validItemData[lastSpawnIndex].SpawnAsync(item =>
                    {
                        lastSpawnIndex++;
                        item.DisallowDespawn = true;
                        item.OnGrabEvent += OnGrab;

                        if (!levelBountyContent.previousPositions.ContainsKey(item.data.id))
                        {
                            spawner.AlignAndPlaceItem(item);
                            levelBountyContent.previousPositions[item.data.id] = item.transform.position;
                        }
                        else item.transform.position = levelBountyContent.previousPositions[item.data.id];
                        
                        Debug.Log($"[Baron's Bounties] Spawned {item.data.id} at ({item.transform.position}/{item.transform.rotation}) in ({newArea.AreaDataId}/{spawner.name}). Checking item light level..");
                        float lightLevel = LightManager.GetObjectBrightness(newArea.SpawnedArea, item.transform.position, item.lightVolumeReceiver, out float[] samples);
                        bool valid = lightLevel > 0.05f;
                        
                        Debug.Log($"[Baron's Bounties] Light level {lightLevel} is {(valid ? "valid!" : "invalid, despawning and waiting for the next area.")}");
                        if (!valid)
                        {
                            levelBountyContent.previousPositions.Remove(item.data.id);
                            lastSpawnIndex--;

                            item.DisallowDespawn = false;
                            item.OnGrabEvent -= OnGrab;
                            item.Despawn(1f);
                        }
                      
                    }, spawner.transform.position, spawner.transform.rotation);
                }
            }
        }

        private void OnGrab(Handle handle, RagdollHand ragdollHand)
        {
            if (!ragdollHand.creature.isPlayer) return;
            handle.item.SetOwner(Item.Owner.Player); 
            
            levelBountyContent.foundItems.Add(handle.item.data.id);
            handle.item.OnGrabEvent -= OnGrab;
            BountySaveData.SaveAsync();
        }

        private void OnDungeonSuccessEvent(LevelInstance levelInstance)
        {
            if (levelBountyContent.foundItems.Count == levelBountyData.bountyItemData.Length) levelBountyContent.showOnMap = false;
            BountySaveData.SaveAsync();
        }
        #endif
    }
}