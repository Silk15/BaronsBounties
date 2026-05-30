using System;
using System.Collections.Generic;
using BaronsBounties.Bounties.Modules;
using System.Linq;
using ThunderRoad;
using ThunderRoad.Modules;
using UnityEngine;

namespace BaronsBounties.Bounties.Sandbox.Modules
{
    public class SandboxBountyModule : BountyModule
    {
        #if !SDK
        protected override void LoadBounties()
        {
            base.LoadBounties();
            BountySaveData.LoadAsync(() =>
            {
                BountySaveData.BountySave bountySave = BountySaveData.instance.GetSave(Player.characterData.ID);
                List<SandboxLevelBountyContent> sandboxBounties = bountySave.activeBounties.OfType<SandboxLevelBountyContent>().ToList();

                foreach (BountyContent bountyContent in bountySave.activeBounties) this.bountyContent.Add(bountyContent);
                int remaining = sandboxBounties.Count;
                if (remaining == 0) return;

                foreach (SandboxLevelBountyContent sandboxLevelBountyContent in sandboxBounties.Where(b => b.showOnMap))
                    CreateLevelInstance(sandboxLevelBountyContent, false, () => { });
            });
        }

        protected override void OnBountyAccepted(BountyContent bountyContent)
        {
            base.OnBountyAccepted(bountyContent);
            switch (bountyContent)
            {
                case SandboxLevelBountyContent sandboxLevelBountyContent when bountyContent.BountyData is LevelBountyData levelBountyData:
                    sandboxLevelBountyContent.randomNearest = levelBountyData.randomNearest;
                    sandboxLevelBountyContent.levelId = levelBountyData.bountyLevelId;
                    sandboxLevelBountyContent.mapLocationIndex = levelBountyData.MapLocation;
                    sandboxLevelBountyContent.enemyConfigDataId = GetEnemyConfigId(levelBountyData.bountyLevelData);
                    sandboxLevelBountyContent.lootConfigDataId = GetLootConfigId();
                    sandboxLevelBountyContent.dungeonLength = GetDungeonLength(levelBountyData.bountyLevelData);
                    sandboxLevelBountyContent.seed = UnityEngine.Random.Range(int.MinValue, int.MaxValue);
                    this.bountyContent.Add(bountyContent);
                    CreateLevelInstance(sandboxLevelBountyContent, true);
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
            try
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

                    if (GameModeManager.instance.currentGameMode.TryGetModule(out SandboxLevelInstancesModule sandboxLevelInstancesModule))
                    {
                        foreach (SandboxLevelBountyContent sandboxLevelBountyContent in bountyContent.OfType<SandboxLevelBountyContent>())
                            sandboxLevelInstancesModule.LevelInstances.Add(sandboxLevelBountyContent.LevelInstance);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.Log("caught exception refreshing bounties and levels!");
                Debug.LogException(ex);
            }
        }

        protected virtual void CreateLevelInstance(SandboxLevelBountyContent sandboxLevelBountyContent, bool build, Action onComplete = null)
        {
            SandboxBountyInstanceData sandboxBountyInstanceData = new()
            {
                guid = Guid.NewGuid(),
                isMapPositionLocked = false,
                mapLocationIndex = sandboxLevelBountyContent.mapLocationIndex,
                randomNearest = sandboxLevelBountyContent.randomNearest,
                seed = sandboxLevelBountyContent.seed,
                enemyConfig = sandboxLevelBountyContent.enemyConfigDataId,
                lootConfig = sandboxLevelBountyContent.lootConfigDataId,
                length = sandboxLevelBountyContent.dungeonLength
            };
            
            GameManager.local.StartCoroutine(CloneLevelData(sandboxLevelBountyContent.LevelBountyData, levelData =>
            {
                LevelInstance levelInstance = new(levelData, levelData.GetMode(), sandboxBountyInstanceData);
                sandboxLevelBountyContent.LevelData = levelData;
                sandboxLevelBountyContent.LevelInstance = levelInstance;
                
                if (GameModeManager.instance.currentGameMode.TryGetModule(out SandboxLevelInstancesModule sandboxLevelInstancesModule))
                    sandboxLevelInstancesModule.LevelInstances.Add(levelInstance);
                
                onComplete?.Invoke();
                if (build) Build();
            }));
        }

        protected void Build()
        {
            if (GameModeManager.instance.currentGameMode.TryGetModule(out SandboxLevelInstancesModule sandboxLevelInstancesModule))
                sandboxLevelInstancesModule.BuildLevelInstances();
        }

        public virtual string GetEnemyConfigId(LevelData levelData)
        {
            foreach (OptionBase option in levelData.GetMode("Sandbox").availableOptions)
                if (option is EnemyConfigOption enemyConfigOption) return enemyConfigOption.valueList.RandomChoice();
            return null;
        }
        
        public virtual string GetLootConfigId() => Catalog.GetDataList(Category.LootConfig).RandomChoice().id;
        
        public virtual int GetDungeonLength(LevelData levelData)
        {
            foreach (OptionBase option in levelData.GetMode("Sandbox").availableOptions)
                if (option is Option lengthOption && lengthOption.name == "DungeonLength") return UnityEngine.Random.Range(lengthOption.minValue, lengthOption.maxValue);
            return 1;
        }
        #endif
    }
}