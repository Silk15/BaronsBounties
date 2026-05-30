using System;
using System.Collections.Generic;
using BaronsBounties.Bounties;
using Newtonsoft.Json;
using ThunderRoad;
using ThunderRoad.Modules;
using TriInspector;
using UnityEngine;
using UnityEngine.Serialization;
using Random = System.Random;

namespace BaronsBounties.Cargo
{
    [Serializable]
    public class CargoData : CustomData
    {
        public string displayName = "Cargo";
        
        [LabelText("Drops")]
        [HideReferencePicker]
        public List<Drop> drops = new();

        public List<ResolvedDrop> GetResolvedDrops()
        {
            List<ResolvedDrop> result = new();
            foreach (Drop drop in drops)
            {
                if (UnityEngine.Random.Range(0f, 1f) <= drop.weight)
                    result.Add(new ResolvedDrop
                    {
                        referenceId = drop.referenceId,
                        dropType = drop.dropType,
                        quantity = drop.SpawnCount,
                        amount = drop.Amount
                    });
            }
            
            return result;
        }

        public Drop GetDrop(string referenceId)
        {
            for (int i = 0; i < drops.Count; i++)
                if (drops[i].referenceId == referenceId) return drops[i];
            return null;
        }

        #if !SDK
        public override void OnCatalogRefresh()
        {
            base.OnCatalogRefresh();

            for (int i = drops.Count - 1; i >= 0; i--)
            {
                Drop drop = drops[i];
                drop.OnCatalogRefresh();
                if (!GameManager.CheckContentActive(drop.sensitiveContent, drop.sensitiveFilterBehaviour)) drops.RemoveAt(i);
            }
        }
        #endif
        
        #if UNITY_EDITOR
        public override string GetCatalogPath()
        {
            string result = $"Cargo";
            if (!groupPath.IsNullOrEmptyOrWhitespace()) result += $"/{groupPath}";
            if (result[result.Length - 1] == '/') result = result.Substring(0, result.Length - 1);
            return result;
        }
        #endif

        [Serializable, DeclareHorizontalGroup("Horizontal")]
        public class Drop
        {
            public float weight = 1.0f;
            
            [PropertyOrder(0), EnumToggleButtons]
            public DropType dropType;
            
            [PropertyOrder(1), JsonMergeKey, Dropdown(nameof(GetCorrectID))]
            public string referenceId;

            [Header("Loot Table"), EnableIf("dropType", DropType.Table)]
            public string tableDisplayName;
            
            [Header("Currency"), EnableIf("dropType", DropType.Currency)]
            public Vector2 amount;

            [Group("Horizontal"), EnableIf("dropType", DropType.Currency)]
            public bool multiplyByLevel = false;

            [Group("Horizontal"), EnableIf("dropType", DropType.Currency), TableList]
            public List<CurrencyLevelMultiplier> levelMultipliers = new();
            
            [Header("Spawning"), DisableIf("dropType", DropType.Currency)]
            public bool useRandomSpawnCount = true;

            [DisableIf("dropType", DropType.Currency)]
            public Vector2Int spawnCount;

            [HideLabel, Header("Content Filtering")]
            public BuildSettings.ContentFlag sensitiveContent;
            
            [HideLabel]
            public BuildSettings.ContentFlagBehaviour sensitiveFilterBehaviour;

            [NonSerialized]
            public ItemData itemData;

            [NonSerialized]
            public LootTable lootTable;

            [NonSerialized]
            public Item spawnedItem;

            [JsonIgnore]
            public int SpawnCount => useRandomSpawnCount ? UnityEngine.Random.Range(spawnCount.x, spawnCount.y) : 1;

            #if !SDK
            [JsonIgnore]
            public int Amount
            {
                get
                {
                    int amount = UnityEngine.Random.Range((int)this.amount.x, (int)this.amount.y);
                    if (multiplyByLevel && GameModeManager.instance.currentGameMode.TryGetModule(out CrystalHuntProgressionModule crystalHuntProgressionModule))
                    {
                        foreach (CurrencyLevelMultiplier levelMultiplier in levelMultipliers)
                            if (levelMultiplier.level == crystalHuntProgressionModule.progressionLevel)
                            { 
                                amount = Mathf.CeilToInt(amount * levelMultiplier.multiplier);
                            }
                    }
                    return amount;
                }
            }
            
            public void OnCatalogRefresh()
            {
                switch (dropType)
                {
                    case DropType.Currency:
                    case DropType.Item:
                        itemData = Catalog.GetData<ItemData>(referenceId);
                        break;
                    case DropType.Table:
                        lootTable = Catalog.GetData<LootTable>(referenceId);
                        break;
                }
            }
            #endif

            public TriDropdownList<string> GetCorrectID()
            {
                switch (dropType)
                {
                    case DropType.Currency:
                    case DropType.Item:
                        return Catalog.GetDropdownAllID<ItemData>();
                    case DropType.Table:
                        return Catalog.GetDropdownAllID<LootTable>();
                }
                return null;
            }
            
            public enum DropType
            {
                Item,
                Table,
                Currency
            }
        }
    }
    
    [Serializable]
    public class CurrencyLevelMultiplier
    {
        public int level;
        public float multiplier;
    }
}