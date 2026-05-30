using System;
using System.Collections;
using System.Linq;
using BaronsBounties.Bounties.CrystalHunt;
using BaronsBounties.Bounties.Sandbox;
using BaronsBounties.Cargo;
using Newtonsoft.Json;
using ThunderRoad;
using TriInspector;
using UnityEngine;

namespace BaronsBounties.Bounties
{
    [Serializable, DeclareHorizontalGroup("Horizontal"), DeclareHorizontalGroup("HorizontalToggles")]
    public class BountyData : CustomData
    {
        [Group("Horizontal"), TextArea(5, 15)]
        public string bountyDisplayName;
        
        [Group("Horizontal"), TextArea(5, 15)]
        public string bountyDescription;
        
        [Group("HorizontalToggles")]
        public bool isBountyUnique;
        
        [Group("HorizontalToggles")]
        public bool showOnBoard = true;

        public int minLevel = 1;
        public float probability = 1.0f;

        [Header("Bounty Icons")]
        public string bountyTabIconAddress = "Bas.Ui.SkillTree.Icons[Fireball]";
        public string bountyNoteIconAddress = "Bas.Ui.SkillTree.Icons[Fireball]";
        public Color bountyNoteColor;
        
        
        [Dropdown(nameof(GetAllCargoCollectionID))]
        public string cargoCollectionId;
        
        public BountyItem[] bountyItemIds;

        [NonSerialized]
        public CargoCollectionData cargoCollectionData;

        [NonSerialized]
        public ItemData[] bountyItemData;

        [NonSerialized]
        public Sprite bountyTabIcon;
        
        [NonSerialized]
        public Sprite bountyNoteIcon;
        
        #if !SDK
        [JsonIgnore]
        public string BountyDisplayName => LocalizationManager.Instance.TryGetLocalization("Bounties", bountyDisplayName);

        [JsonIgnore]
        public string BountyDescription => LocalizationManager.Instance.TryGetLocalization("Bounties", bountyDescription);

        [JsonIgnore]
        public virtual string Descriptor => "(This bounty's item(s) can be found anywhere!)";
        
        [JsonIgnore]
        public virtual bool ShowNoteIcon => false;

        public virtual Type GetModuleType() => null;

        public virtual Type GetBountyContentType()
        {
            switch (GameModeManager.instance.currentGameMode.id)
            {
                case "CrystalHunt":
                    return typeof(CrystalHuntBountyContent);
                
                case "Sandbox":
                    return typeof(SandboxBountyContent);
            }
            return typeof(BountyContent);
        }
        
        public override void OnCatalogRefresh()
        {
            base.OnCatalogRefresh();
            bountyItemData = bountyItemIds.Select(b => b.itemId).AsDataArray<ItemData>();
            cargoCollectionData = Catalog.GetData<CargoCollectionData>(cargoCollectionId);
        }

        public override IEnumerator LoadAddressableAssetsCoroutine()
        {
            yield return Catalog.LoadAssetCoroutine<Sprite>(bountyTabIconAddress, sprite => bountyTabIcon = sprite, $"Bounty Tab Sprite {id}");
            yield return Catalog.LoadAssetCoroutine<Sprite>(bountyNoteIconAddress, sprite => bountyNoteIcon = sprite, $"Bounty Note Sprite {id}");
        }
        #endif
        
        #if UNITY_EDITOR
        public override string GetCatalogPath()
        {
            string result = $"Bounties";
            if (!groupPath.IsNullOrEmptyOrWhitespace()) result += $"/{groupPath}";
            if (result[result.Length - 1] == '/') result = result.Substring(0, result.Length - 1);
            return result;
        }
        #endif
        
        public TriDropdownList<string> GetAllCargoCollectionID() => Catalog.GetDropdownAllID<CargoCollectionData>();

        [Serializable, DeclareHorizontalGroup("Horizontal")]
        public class BountyItem
        {
            [Dropdown(nameof(GetAllItemID)), HideLabel]
            public string itemId;
            
            [Group("Horizontal"), TextArea(2, 5)]
            public string bountyDetailDescription;
            
            [Group("Horizontal"), TextArea(2, 5)]
            public string bountyDetailTitle;
            
            public TriDropdownList<string> GetAllItemID() => Catalog.GetDropdownAllID(Category.Item);
            
            public static implicit operator string (BountyItem bountyItem) => bountyItem.itemId;
        }
    }
}