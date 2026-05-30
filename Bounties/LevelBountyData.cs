using System;
using System.Collections.Generic;
using BaronsBounties.Bounties.CrystalHunt;
using BaronsBounties.Bounties.Sandbox;
using Newtonsoft.Json;
using ThunderRoad;
using ThunderRoad.Modules;
using TriInspector;
using UnityEngine;

namespace BaronsBounties.Bounties
{
    [DeclareBoxGroup("Addresses")]
    [DeclareHorizontalGroup("Horizontal1")]
    public class LevelBountyData : BountyData
    {
        [HideLabel]
        [Header("Icon Address")]
        [Group("Addresses")]
        public string mapLocationIconAddress = "Bas.Icon.Location.DungeonOutpost";
        
        [HideLabel]
        [Header("Hover Icon Address")]
        [Group("Addresses")]
        public string mapLocationIconHoverAddress = "Bas.Icon.Location.DungeonOutpost_Highlight";

        [HideLabel]
        [Header("Map Preview Address")]
        [Group("Addresses")]
        public string mapPreviewImageAddress = "Bas.Image.Preview.Outpost";

        [TextArea(5, 15)]
        [Group("Horizontal1")]
        public string levelDisplayName;
        
        [TextArea(5, 15)]
        [Group("Horizontal1")]
        public string levelDescription;
        
        [Title("IDs")]
        [HideLabel]
        [Dropdown(nameof(GetAllLevelID))]
        public string bountyLevelId;
        
        [HideLabel]
        public CrystalHuntLevelInstancesModule.LootType lootType;
        
        [Title("Positioning")]
        public int randomNearest = 0;
        public Vector2Int minMaxMapLocation;

        #if !SDK
        [NonSerialized]
        public LevelData bountyLevelData;

        public int MapLocation => UnityEngine.Random.Range(minMaxMapLocation.x, minMaxMapLocation.y + 1);

        [JsonIgnore]
        public override string Descriptor => "(This bounty's item(s) must be found in a specific outpost!)";
        
        [JsonIgnore]
        public override bool ShowNoteIcon => true;

        public override Type GetModuleType() => typeof(LevelModuleBountySpawner);

        public override void OnCatalogRefresh()
        {
            base.OnCatalogRefresh();
            bountyLevelData = Catalog.GetData<LevelData>(bountyLevelId);
        }
        
        public override Type GetBountyContentType()
        {
            switch (GameModeManager.instance.currentGameMode.id)
            {
                case "CrystalHunt":
                    return typeof(CrystalHuntLevelBountyContent);
                
                case "Sandbox":
                    return typeof(SandboxLevelBountyContent);
            }
            return typeof(BountyContent);
        }

        public string LevelDisplayName => LocalizationManager.Instance.TryGetLocalization("Bounties", levelDisplayName);

        public string LevelDescription => LocalizationManager.Instance.TryGetLocalization("Bounties", levelDescription);
        #endif

        public TriDropdownList<string> GetAllItemID() => Catalog.GetDropdownAllID(Category.Item);
        public TriDropdownList<string> GetAllLevelID() => Catalog.GetDropdownAllID(Category.Level);
    }
}