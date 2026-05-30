using System;
using System.Collections.Generic;
using BaronsBounties.Cargo;
using Newtonsoft.Json;
using ThunderRoad;
using ThunderRoad.DebugViz;
using UnityEngine;
using Random = System.Random;

namespace BaronsBounties.Bounties
{
    [Serializable]
    public class ResolvedDrop
    {
        public string referenceId;
        public CargoData.Drop.DropType dropType;
        public int quantity;
        public int amount;
        public int ownedCount;

        [NonSerialized]
        public List<Item> spawnedItems = new();

        [JsonIgnore]
        public int Remaining => quantity - ownedCount;

        [JsonIgnore]
        public List<Item> ClaimedRewards { get; private set; } = new();
        
        #if !SDK
        public void Spawn(CargoData cargoData, Vector3 position, Action<Item> onComplete)
        {
            CargoData.Drop drop = cargoData.GetDrop(referenceId);
            switch (dropType)
            {
                case CargoData.Drop.DropType.Item:
                    for (int i = 0; i < Remaining; i++) drop.itemData.SpawnAsync(onComplete, position, Quaternion.identity);
                    break;

                case CargoData.Drop.DropType.Currency:
                    drop.itemData.SpawnAsync(item =>
                    {
                        onComplete?.Invoke(item);
                        item.RunAfter(() =>
                        {
                            item.SetValue(amount);
                            if (item.TryGetComponentInChildren(out ShopPriceTag shopPriceTag)) shopPriceTag.Set(amount, 0);
                            item.OnDespawnEvent += OnDespawn;
                            item.OnItemStored += OnItemStored;

                            void OnDespawn(EventTime eventTime)
                            {
                                if (eventTime == EventTime.OnEnd) return;
                                if (item.IsHeld() && !ClaimedRewards.Contains(item))
                                {
                                    Player.characterData.inventory.AddCurrencyValue(item.data.valueType, amount);
                                    ClaimedRewards.Add(item);
                                }

                                item.OnDespawnEvent -= OnDespawn;
                                item.OnItemStored -= OnItemStored;
                            }
                        }, 1f);

                    }, position, Quaternion.identity);
                    break;

                case CargoData.Drop.DropType.Table:
                    foreach (ItemData itemData in drop.lootTable.Pick()) itemData.SpawnAsync(onComplete, position, Quaternion.identity);
                    break;
            }
        }

        private void OnItemStored(UIInventory inventory, ItemContent itemContent, Item item)
        {
            Player.characterData.inventory.AddCurrencyValue(item.data.valueType, amount);
            Debug.Log($"Added {amount} {item.data.valueType} to inventory");
            ClaimedRewards.Add(item);
        }
        #endif
    }
}