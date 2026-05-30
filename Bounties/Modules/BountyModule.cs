using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using ThunderRoad;
using ThunderRoad.Modules;
using TriInspector;
using UnityEngine;
using Object = UnityEngine.Object;

#if !SDK
using IngameDebugConsole;
#endif

namespace BaronsBounties.Bounties.Modules
{
    public class BountyModule : GameModeModule
    {
        #if !SDK
        public List<BountyContent> bountyContent = new();
        
        public override IEnumerator OnLoadCoroutine()
        {
            UIBountyBoard.onInstanceCreated += OnInstanceCreated;
            EventManager.onUnpossess += OnUnPossess;
            
            EventManager.OnUpdateMap -= OnUpdateMap;
            EventManager.OnUpdateMap += OnUpdateMap;
            yield return new WaitUntil(() => Player.characterData != null);
            LoadBounties();
        }

        public override void OnUnload()
        {
            base.OnUnload();
            UIBountyBoard.onInstanceCreated -= OnInstanceCreated;
            
            EventManager.onUnpossess -= OnUnPossess;
            EventManager.OnUpdateMap -= OnUpdateMap;
        }
        
        private void OnInstanceCreated(UIBountyBoard uiBountyBoard)
        {
            UIBountyBoard.instance.onBountyAccepted -= OnBountyAccepted;
            UIBountyBoard.instance.onBountyAccepted += OnBountyAccepted;
            
            UIBountyBoard.instance.onBountyAbandoned -= OnBountyAbandoned;
            UIBountyBoard.instance.onBountyAbandoned += OnBountyAbandoned;
        }

        private void OnUnPossess(Creature creature, EventTime eventTime)
        {
            if (eventTime == EventTime.OnStart && UIBountyBoard.instance != null) 
                UIBountyBoard.instance.onBountyAccepted -= OnBountyAccepted;
        }
        
        protected virtual void OnUpdateMap(EventTime eventTime) { }

        protected virtual void LoadBounties() { }

        protected virtual void OnBountyAccepted(BountyContent bountyContent) { }
        
        protected virtual void OnBountyAbandoned(BountyContent bountyContent) { }
        
        public IEnumerator CloneLevelData(LevelBountyData levelBountyData, Action<LevelData> onComplete = null)
        {
            LevelData levelData = levelBountyData.bountyLevelData.CloneJson();
            levelData.id += levelBountyData.id;
            levelData.name = $"{levelBountyData.LevelDisplayName} ({levelBountyData.BountyDisplayName})";
            levelData.description = levelBountyData.LevelDescription;

            levelData.mapLocationIconAddress = levelBountyData.mapLocationIconAddress;
            levelData.mapLocationIconHoverAddress = levelBountyData.mapLocationIconHoverAddress;
            levelData.mapPreviewImageAddress = levelBountyData.mapPreviewImageAddress;
            levelData.worldMapTravelAudioContainerAddress = "Silk.AudioGroup.UI.BountyLevelStart";

            LevelModule instance = Activator.CreateInstance(levelBountyData.GetModuleType()) as LevelModule;
            if (instance is IBountyInteractor bountyInteractor) bountyInteractor.OnLoad(levelBountyData);
            levelData.GetMode().modules.Add(instance);

            Catalog.TryGetCategoryData(Category.Level, out CatalogCategory catalogCategory);
            catalogCategory.AddCatalogData(levelData);
            
            yield return levelData.OnCatalogRefreshCoroutine();
            onComplete?.Invoke(levelData);
        }
        #endif
    }
}