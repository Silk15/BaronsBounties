using System;
using System.Collections.Generic;
using BaronsBounties.Bounties;
using Newtonsoft.Json;
using ThunderRoad;
using TriInspector;
using UnityEngine;
using Random = System.Random;

namespace BaronsBounties.Cargo
{
    [Serializable]
    public class CargoCollectionData : CustomData
    {
        [TableList]
        public List<Cargo> cargo = new();

        #if !SDK
        public override void OnCatalogRefresh()
        {
            base.OnCatalogRefresh();

            for (int i = cargo.Count - 1; i >= 0; i--)
            {
                Cargo cargo = this.cargo[i];
                cargo.OnCatalogRefresh();
                if (!GameManager.CheckContentActive(cargo.sensitiveContent, cargo.sensitiveFilterBehaviour)) this.cargo.RemoveAt(i);
            }
        }
        
        public CargoData GetDrop()
        {
            float total = 0f;
            foreach (Cargo cargo in cargo) total += cargo.probabilityWeight;

            float roll = UnityEngine.Random.Range(0f, total);
            float cumulative = 0f;

            foreach (Cargo cargo in cargo)
            {
                cumulative += cargo.probabilityWeight;
                if (roll <= cumulative) return cargo.cargoData;
            }

            return null;
        }
        
        public List<CargoData> GetDrops()
        {
            List<CargoData> result = new();
            foreach (Cargo cargo in this.cargo)
            {
                if (UnityEngine.Random.value <= cargo.probabilityWeight)
                    result.Add(cargo.cargoData);
            }
    
            return result;
        }

        public List<Reward> GetReward()
        {
            List<Reward> rewards = new();
            foreach (CargoData cargoData in GetDrops())
            {
                Reward reward = new()
                {
                    cargoId = cargoData.id
                };
                reward.resolvedDrops.AddRange(cargoData.GetResolvedDrops());

                rewards.Add(reward);
            }
            return rewards;
        }
        
        #endif
        
        #if UNITY_EDITOR
        public override string GetCatalogPath()
        {
            string result = $"CargoCollections";
            if (!groupPath.IsNullOrEmptyOrWhitespace()) result += $"/{groupPath}";
            if (result[result.Length - 1] == '/') result = result.Substring(0, result.Length - 1);
            return result;
        }
        #endif

        [Serializable]
        public class Cargo
        {
            [JsonMergeKey]
            [Dropdown(nameof(GetAllCargoID))]
            public string referenceId;

            public float probabilityWeight;
            public BuildSettings.ContentFlag sensitiveContent;
            public BuildSettings.ContentFlagBehaviour sensitiveFilterBehaviour;
            
            [NonSerialized]
            public CargoData cargoData;

            public void OnCatalogRefresh() => cargoData = Catalog.GetData<CargoData>(referenceId);

            public TriDropdownList<string> GetAllCargoID() => Catalog.GetDropdownAllID<CargoData>();
        }
    }
}