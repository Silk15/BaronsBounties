using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using BaronsBounties.Bounties.Modules;
using ThunderRoad;
using ThunderRoad.Modules;
using UnityEngine;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

namespace BaronsBounties.Bounties.CrystalHunt.Modules
{
    public class CrystalHuntBountyModule : BountyModule
    {
        #if !SDK
        [NonSerialized]
        public CrystalHuntSaveData saveData;

        [NonSerialized]
        public CrystalHuntLevelInstancesModule crystalHuntLevelInstancesModule;

        [NonSerialized]
        public CrystalHuntDungeonLengthBalance dungeonLengthBalance;

        [NonSerialized]
        private CrystalHuntDungeonEndRewardBalance endRewardLootMultipliers;

        [NonSerialized]
        private CrystalHuntFactionTierBalance outpostFactionTierBalance;
        
        public override IEnumerator OnLoadCoroutine()
        {
            yield return base.OnLoadCoroutine();
            GameModeManager.Instance.currentGameMode.TryGetModule(out crystalHuntLevelInstancesModule);
            saveData = GameModeManager.Instance.currentGameMode.GetGameModeSaveData() as CrystalHuntSaveData;

            if (!string.IsNullOrEmpty(crystalHuntLevelInstancesModule.dungeonLengthBalanceAddress))
                yield return Catalog.LoadAssetCoroutine<CrystalHuntDungeonLengthBalance>(crystalHuntLevelInstancesModule.dungeonLengthBalanceAddress, balance => dungeonLengthBalance = balance, "Crystal Hunt Bounty Module");

            if (!string.IsNullOrEmpty(crystalHuntLevelInstancesModule.endRewardBalanceAddress))
                yield return Catalog.LoadAssetCoroutine<CrystalHuntDungeonEndRewardBalance>(crystalHuntLevelInstancesModule.endRewardBalanceAddress, balance => endRewardLootMultipliers = balance, "Crystal Hunt Bounty Module");

            if (!string.IsNullOrEmpty(crystalHuntLevelInstancesModule.outpostFactionTierBalanceAddress))
                yield return Catalog.LoadAssetCoroutine<CrystalHuntFactionTierBalance>(crystalHuntLevelInstancesModule.outpostFactionTierBalanceAddress, balance => outpostFactionTierBalance = balance, "Crystal Hunt Bounty Module");
        }

        protected override void LoadBounties()
        {
            base.LoadBounties();
            BountySaveData.LoadAsync(() =>
            {
                BountySaveData.BountySave bountySave = BountySaveData.instance.GetSave(Player.characterData.ID);
                List<CrystalHuntLevelBountyContent> crystalHuntBounties = bountySave.activeBounties.OfType<CrystalHuntLevelBountyContent>().ToList();

                foreach (BountyContent bountyContent in bountySave.activeBounties) this.bountyContent.Add(bountyContent);
                int remaining = crystalHuntBounties.Count;
                if (remaining == 0) return;

                foreach (CrystalHuntLevelBountyContent crystalHuntLevelBountyContent in crystalHuntBounties.Where(b => b.showOnMap))
                    CreateLevelInstance(crystalHuntLevelBountyContent, false, () => { });
            });
        }

        protected override void OnBountyAccepted(BountyContent bountyContent)
        {
            base.OnBountyAccepted(bountyContent);
            switch (bountyContent)
            {
                case CrystalHuntLevelBountyContent crystalHuntLevelBountyContent when bountyContent.BountyData is LevelBountyData levelBountyData:
                    int factionTier = Mathf.Clamp(saveData.levelProgression + Random.Range(1, 3), 0, 4);
                    int dungeonLength = dungeonLengthBalance.GetDungeonLength(saveData.levelProgression);

                    CrystalHuntDungeonLootMultiplierBalance tierLengthLootMultipliers = crystalHuntLevelInstancesModule.GetField("tierLengthLootMultipliers") as CrystalHuntDungeonLootMultiplierBalance;
                    CrystalHuntDungeonEndRewardBalance.EndRewardLootMultiplier endRewardLootMultiplier = endRewardLootMultipliers.GetEndRewardLootMultiplier(levelBountyData.lootType);
                    float multiplierChance = tierLengthLootMultipliers.GetLootMultiplierChance(factionTier, dungeonLength);
                    List<(string, Color)> rewardTierIcons = new()
                    {
                        (bountyContent.BountyData.bountyTabIconAddress, new Color(0.215686277f, 0.1764706f, 0.1764706f, 1.0f))
                    };
                    crystalHuntLevelBountyContent.rewardTierIcons = rewardTierIcons;
                    crystalHuntLevelBountyContent.mapLocationIndex = levelBountyData.MapLocation;
                    crystalHuntLevelBountyContent.randomNearest = levelBountyData.randomNearest;
                    crystalHuntLevelBountyContent.tierLengthLootMultiplier = multiplierChance;
                    crystalHuntLevelBountyContent.endRewardLootMultiplier = endRewardLootMultiplier;
                    crystalHuntLevelBountyContent.levelType = CrystalHuntLevelInstanceData.CrystalHuntLevelType.Dungeon;
                    crystalHuntLevelBountyContent.randomNearest = levelBountyData.randomNearest;
                    crystalHuntLevelBountyContent.levelId = levelBountyData.bountyLevelId;
                    crystalHuntLevelBountyContent.mapLocationIndex = levelBountyData.MapLocation;
                    crystalHuntLevelBountyContent.seed = Random.Range(int.MinValue, int.MaxValue);
                    crystalHuntLevelBountyContent.dungeonLength = dungeonLength;
                    crystalHuntLevelBountyContent.factionTier = factionTier;

                    crystalHuntLevelBountyContent.lootConfigDataId = GetLootConfigId(crystalHuntLevelInstancesModule.outpostLootConfigs, factionTier);
                    crystalHuntLevelBountyContent.enemyConfigDataId = GetEnemyConfigId(crystalHuntLevelInstancesModule.outpostEnemyConfigs, factionTier);
                    
                    this.bountyContent.Add(bountyContent);
                    CreateLevelInstance(crystalHuntLevelBountyContent, true);
                    break;
            }
            
            BountySaveData.BountySave save = BountySaveData.instance.GetSave(Player.characterData.ID);
            save.activeBounties.Add(bountyContent);
            save.startedBounty = true;
            BountySaveData.SaveAsync();
        }

        protected override void OnBountyAbandoned(BountyContent bountyContent)
        {
            base.OnBountyAbandoned(bountyContent);
            if (bountyContent.BountyData is LevelBountyData) Build();
            BountySaveData.SaveAsync();
        }

        protected override void OnUpdateMap(EventTime eventTime)
        {
            base.OnUpdateMap(eventTime);
            if (eventTime == EventTime.OnStart)
            {
                BountySaveData.BountySave bountySave = BountySaveData.instance.GetSave(Player.characterData.ID);
                for (int i = bountyContent.Count - 1; i >= 0; i--)
                {
                    if (!bountySave.activeBounties.Contains(bountyContent[i]) || (bountyContent[i] is LevelBountyContent levelBountyContent && !levelBountyContent.showOnMap))
                        bountyContent.RemoveAt(i);
                }
                Debug.Log(string.Join(", ", crystalHuntLevelInstancesModule.LevelInstances.Select(l => l.levelDataId + $" {l.instanceData.mapLocationIndex}")));
            }
            else
            {
                foreach (CrystalHuntLevelBountyContent sandboxLevelBountyContent in bountyContent.OfType<CrystalHuntLevelBountyContent>()) 
                    crystalHuntLevelInstancesModule.LevelInstances.Add(sandboxLevelBountyContent.LevelInstance);
                Debug.Log(string.Join(", ", crystalHuntLevelInstancesModule.LevelInstances.Select(l => l.levelDataId + $" {l.instanceData.mapLocationIndex}")));
            }
        }

        protected virtual void CreateLevelInstance(CrystalHuntLevelBountyContent crystalHuntLevelBountyContent, bool build, Action onComplete = null)
        {
            CrystalHuntBountyInstanceData levelInstanceData = new(crystalHuntLevelBountyContent.seed, crystalHuntLevelBountyContent.dungeonLength, crystalHuntLevelBountyContent.lootConfigDataId, crystalHuntLevelBountyContent.enemyConfigDataId, crystalHuntLevelBountyContent.rewardTierIcons, crystalHuntLevelInstancesModule.difficultyIcon, crystalHuntLevelInstancesModule.difficultyIconColor);
            levelInstanceData.mapLocationIndex = crystalHuntLevelBountyContent.mapLocationIndex;
            levelInstanceData.randomNearest = crystalHuntLevelBountyContent.randomNearest;
            levelInstanceData.tierLengthLootMultiplier = crystalHuntLevelBountyContent.tierLengthLootMultiplier;
            levelInstanceData.endRewardLootMultiplier = crystalHuntLevelBountyContent.endRewardLootMultiplier;
            levelInstanceData.lootType = crystalHuntLevelBountyContent.LevelBountyData.lootType;
            levelInstanceData.levelType = crystalHuntLevelBountyContent.levelType;
            
            GameManager.local.StartCoroutine(CloneLevelData(crystalHuntLevelBountyContent.LevelBountyData, levelData =>
            {
                LevelInstance levelInstance = new(levelData, levelData.GetMode(), levelInstanceData);
 
                crystalHuntLevelBountyContent.LevelData = levelData;
                crystalHuntLevelBountyContent.LevelInstance = levelInstance;
                crystalHuntLevelInstancesModule.LevelInstances.Add(levelInstance);
                onComplete?.Invoke();
                if (build) Build();
            }));
        }

        protected void Build() => crystalHuntLevelInstancesModule.BuildLevelInstances();
        
        private string GetEnemyConfigId(CrystalHuntLevelInstancesModule.EnemyConfigTiers configTiers, int factionTier)
        {
            List<string> tier = configTiers.GetTier(factionTier);
            if (!tier.IsNullOrEmpty()) return tier[UnityEngine.Random.Range(0, tier.Count)];
            return null;
        }

        private string GetLootConfigId(CrystalHuntLevelInstancesModule.LootConfigTiers configTiers, int factionTier)
        {
            List<string> tier = configTiers.GetTier(factionTier);
            if (!tier.IsNullOrEmpty()) return tier[UnityEngine.Random.Range(0, tier.Count)];
            return null;
        }
        
        public virtual int GetDungeonLength(LevelData levelData)
        {
            foreach (OptionBase option in levelData.GetMode("CrystalHunt").availableOptions)
                if (option is Option lengthOption && lengthOption.name == "DungeonLength") return UnityEngine.Random.Range(lengthOption.minValue, lengthOption.maxValue);
            return 1;
        }
        #endif
    }
}