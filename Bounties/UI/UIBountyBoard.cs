using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using BaronsBounties.Bounties.Modules;
using IngameDebugConsole;
using ThunderRoad;
using ThunderRoad.Modules;
using UnityEngine;
using Random = UnityEngine.Random;

namespace BaronsBounties.Bounties
{
    public class UIBountyBoard : ThunderBehaviour
    {
        public const string UIBountyPrefabAddress = "Silk.UI.Bounty";
        public const string BountyEffectId = "BountyStart";

        public static UIBountyBoard instance;

        #if !SDK
        public static GameObject uiBountyPrefab;
        public LevelModuleBountyBoard levelModuleBountyBoard;
        public BountyBoard bountyBoard;

        public UICustomisableButton refreshButton;
        public EffectData bountyStartEffectData;
        public List<UIBounty> bounties = new();
        public List<BountyData> bountyData = new();
        public BountyContent selectedBounty;
        public UIBountyInfo bountyInfo;

        public Transform[] bountyPoints;

        public static event Action<UIBountyBoard> onInstanceCreated;
        public event Action<BountyContent> onBountyAccepted;
        public event Action<BountyContent> onBountyAbandoned;

        public void Init(LevelModuleBountyBoard levelModuleBountyBoard, BountyBoard bountyBoard)
        {
            IngameDebugConsole.DebugLogConsole.AddCommand("refreshbounties", "Refreshes all bounties", () => Refresh());
            DebugLogConsole.AddCommand("buildlevelinstances", "Builds level instances", () => GameModeManager.instance.currentGameMode.GetModule<LevelInstancesModule>().BuildLevelInstances());

            this.levelModuleBountyBoard = levelModuleBountyBoard;
            this.bountyBoard = bountyBoard;
            bountyInfo = bountyBoard.bountyInfo;

            Transform parent = transform.GetChild(0);
            bountyPoints = new Transform[parent.childCount];
            for (int i = 0; i < parent.childCount; i++) bountyPoints[i] = parent.GetChild(i);

            bountyStartEffectData = Catalog.GetData<EffectData>(BountyEffectId);
            Catalog.LoadAssetAsync<GameObject>(UIBountyPrefabAddress, prefab =>
            {
                refreshButton = GetComponentInChildren<UICustomisableButton>(true);
                refreshButton.onPointerClick.AddListener(Refresh);
                
                uiBountyPrefab = prefab;
                BountySaveData.BountySave bountySave = BountySaveData.instance.GetSave(Player.characterData.ID);
                GetBountyData(bountySave);
                bountyData = bountyData.Where(b => b.showOnBoard && (!bountySave.completedBounties.Select(b => b.referenceID).Contains(b.id) || !b.isBountyUnique) && !bountySave.activeBounties.Select(a => a.referenceID).Contains(b.id)).ToList();
                if (ShouldRefresh()) Refresh();
                else Load();

                bountyInfo.acceptButton.onPointerClick.AddListener(() => AcceptBounty(selectedBounty));
                bountyInfo.abandonButton.onPointerClick.AddListener(() => AbandonBounty(selectedBounty));
                bountyInfo.onTabClick -= OnTabClick;
                bountyInfo.onTabClick += OnTabClick;

                instance = this;
                onInstanceCreated?.Invoke(this);
            }, "UI Bounty Prefab");
        }

        public void Update() => refreshButton?.gameObject?.SetActive(GameManager.DevMode);

        private void OnTabClick(UIBountyInfo.Tab tab) => selectedBounty = tab.bountyContent;

        public void GetBountyData(BountySaveData.BountySave bountySave)
        {
            bountySave.TryRandomiseLevelBounties();
            List<BountyData> allBounties = Catalog.GetDataList<BountyData>();
            bountyData.Clear();

            if (GameModeManager.instance.currentGameMode.TryGetModule(out CrystalHuntProgressionModule module))
            {
                bountyData = allBounties.Where(b =>
                {
                    int effectiveMinLevel = b.minLevel;
                    if (b is LevelBountyData levelBounty && bountySave.randomisedLevelBountyProgressionLevels.TryGetValue(levelBounty.id, out int randomLevel))
                        effectiveMinLevel = randomLevel;

                    return effectiveMinLevel <= module.progressionLevel && !bountySave.activeBounties.Select(b => b.referenceID).Contains(b.id);
                }).GroupBy(b => b.id).Select(g => g.First()).ToList();
            }
            else bountyData = allBounties.GroupBy(b => b.id).Select(g => g.First()).ToList();
        }

        public void Load()
        {
            BountySaveData.BountySave bountySave = BountySaveData.instance.GetSave(Player.characterData.ID);
            if (bountySave.availableBounties.Count == 0)
            {
                if (!GameModeManager.instance.currentGameMode.TryGetGameModeSaveData(out CrystalHuntSaveData _))
                {
                    Refresh();
                    BountySaveData.SaveAsync();
                }

                return;
            }

            foreach (BountyContent bountyContent in bountySave.availableBounties) AddBounty(bountyContent);
        }

        private bool ShouldRefresh()
        {
            BountySaveData.BountySave bountySave = BountySaveData.instance.GetSave(Player.characterData.ID);
            if (GameModeManager.instance.currentGameMode.TryGetGameModeSaveData(out CrystalHuntSaveData crystalHuntSaveData))
            {
                if (bountySave.lastRefreshDay == 0 || crystalHuntSaveData.days - bountySave.lastRefreshDay >= levelModuleBountyBoard.refreshDayInterval)
                {
                    bountySave.lastRefreshDay = crystalHuntSaveData.days;
                    BountySaveData.SaveAsync();
                    return true;
                }

                return false;
            }

            Debug.LogError("Cannot load CrystalHuntSaveData");
            return bountySave.availableBounties.Count == 0;
        }

        public void Refresh()
        {
            BountySaveData.BountySave bountySave = BountySaveData.instance.GetSave(Player.characterData.ID);
            bountySave.availableBounties.Clear();
            bounties.Clear();
            foreach (UIBounty bounty in GetComponentsInChildren<UIBounty>())
            {
                bountyInfo.ClearBounty();
                Destroy(bounty.gameObject);
            }

            if (!bountySave.startedBounty)
            {
                AddBounty(Catalog.GetData<BountyData>("TribalBounty"), 2);
                return;
            }

            int count = levelModuleBountyBoard.minMaxBounties.RandomRangeInt();
            bountyPoints.Shuffle();
            bountyData.Shuffle();

            int limit = Mathf.Min(count, bountyPoints.Length, Mathf.Clamp(bountyData.Count, 0, levelModuleBountyBoard.maxBountiesPerRefresh));
            for (int i = 0; i < limit; i++)
            {
                if (Random.Range(0f, 1f) <= bountyData[i].probability)
                    AddBounty(bountyData[i], i);
            }
        }

        public void AddBounty(BountyData bountyData, int index, List<Reward> rewards = null)
        {
            Transform bountyPoint = bountyPoints[index];

            UIBounty uiBounty = Instantiate(uiBountyPrefab, bountyPoint).AddComponent<UIBounty>();
            uiBounty.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
            uiBounty.Init(bountyData);
            bounties.Add(uiBounty);

            BountySaveData.BountySave bountySave = BountySaveData.instance.GetSave(Player.characterData.ID);
            BountyContent content = bountySave.availableBounties.FirstOrDefault(b => b.referenceID == bountyData.id);
            if (content != null) uiBounty.Refresh(content);
            else
            {
                content = Activator.CreateInstance(bountyData.GetBountyContentType()) as BountyContent;
                if (content != null)
                {
                    if (bountyData.cargoCollectionData == null)
                    {
                        Debug.LogError($"[Baron's Bounties] Bounty {bountyData.id} has an invalid or empty cargo data ID! Rewards will not be generated and this bounty will be ignored.");
                        return;
                    }

                    content.rewards = rewards ?? bountyData.cargoCollectionData.GetReward();

                    int attempts = 0;
                    while (content.rewards.Any(r => r.resolvedDrops.IsNullOrEmpty()) && attempts++ < 10) content.rewards = bountyData.cargoCollectionData.GetReward();

                    if (content.rewards.Count == 0)
                    {
                        Debug.LogError($"[Baron's Bounties] Failed to generate rewards for {bountyData.id} :(");
                        return;
                    }

                    content.referenceID = bountyData.id;
                    content.boardIndex = index;
                    bountySave.availableBounties.Add(content);
                    uiBounty.Refresh(content);
                    BountySaveData.SaveAsync();
                }
                else Debug.LogError($"[Baron's Bounties] {bountyData.GetBountyContentType()} on bounty {bountyData.id} is not a valid type for BountyContent!");
            }

            uiBounty.infoButton.onPointerClick.AddListener(() =>
            {
                selectedBounty = content;
                bountyInfo.SetBounty(content);
            });
        }

        public void AddBounty(BountyContent bountyContent) => AddBounty(bountyContent.BountyData, bountyContent.boardIndex, bountyContent.rewards);

        public void AcceptBounty(BountyContent bountyContent)
        {
            BountySaveData.BountySave bountySave = BountySaveData.instance.GetSave(Player.characterData.ID);
            int removed = bountySave.availableBounties.RemoveAll(b => b.referenceID == bountyContent.referenceID);
            bountyStartEffectData.Spawn(Player.currentCreature.ragdoll.targetPart.transform).Play();
            if (removed > 0)
            {
                onBountyAccepted?.Invoke(bountyContent);
                foreach (UIBounty uiBounty in bounties.ToList())
                {
                    if (uiBounty.bountyData.id == bountyContent.referenceID)
                    {
                        Destroy(uiBounty.gameObject);
                        bounties.Remove(uiBounty); 
                    }
                }
        
                bountyInfo.ClearBounty();
                bountyInfo.RefreshTabs();
            }
            else Debug.LogError($"[Baron's Bounties] Could not find {bountyContent.referenceID} in available bounties to accept!");
        }

        public void AbandonBounty(BountyContent bountyContent)
        {
            BountySaveData.BountySave bountySave = BountySaveData.instance.GetSave(Player.characterData.ID);
            if (bountySave.activeBounties.Any(b => b.referenceID == bountyContent.referenceID))
            {
                bountySave.activeBounties.Remove(bountyContent);
                bountyInfo.ClearBounty();
                bountyInfo.RefreshTabs();
                onBountyAbandoned?.Invoke(bountyContent);
            }
        }
        #endif
    }
}