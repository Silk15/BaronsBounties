using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using BaronsBounties.Cargo;
using Newtonsoft.Json;
using ThunderRoad;
using ThunderRoad.DebugViz;
using UnityEngine;

namespace BaronsBounties.Bounties
{
    [Serializable]
    public class Reward
    {
        public List<ResolvedDrop> resolvedDrops = new();
        public string cargoId;

        private CargoData cargoData;

        [JsonIgnore]
        public CargoData CargoData
        {
            get => cargoData ??= Catalog.GetData<CargoData>(cargoId);
            set
            {
                cargoData = null;
                cargoId = value.id;
            }
        }

        public void MarkAsOwned(Item item)
        {
            foreach (ResolvedDrop resolvedDrop in resolvedDrops)
                if (resolvedDrop.spawnedItems.Contains(item))
                {
                    resolvedDrop.ownedCount++;
                    Debug.Log($"Item {item.data.id} has been marked owned. {resolvedDrop.ownedCount}/{resolvedDrop.quantity}");
                }
        }

        public ResolvedDrop GetDrop(Item item)
        {
            foreach (ResolvedDrop resolvedDrop in resolvedDrops)
                if (resolvedDrop.spawnedItems.Contains(item))
                    return resolvedDrop;
            return null;
        }

        #if !SDK
        public void Spawn(Vector3 center, float radius, Action<Item> onSpawned)
        {
            center.y += 0.2f;
            float spreadRadius = radius / 2f;

            for (int i = 0; i < resolvedDrops.Count; i++)
            {
                ResolvedDrop resolvedDrop = resolvedDrops[i];
                resolvedDrop.Spawn(CargoData, center, item =>
                {
                    item.SetOwner(Item.Owner.Shopkeeper);
                    onSpawned?.Invoke(item);
                    resolvedDrop.spawnedItems.Add(item);

                    Item.HolderPoint holderPoint = item.GetHolderPoint("RewardCrateAnchor");
                    Transform anchor = holderPoint != null ? holderPoint.anchor : item.spawnPoint;
                    Vector3 randomOffset = new(UnityEngine.Random.Range(-spreadRadius, spreadRadius), UnityEngine.Random.Range(0f, 0.6f), UnityEngine.Random.Range(-spreadRadius, spreadRadius));

                    Vector3 finalSpawnPoint = center + randomOffset;
                    item.transform.MoveAlign(anchor, finalSpawnPoint, Quaternion.identity);
                    
                    item.RunAfter(() =>
                    {
                        item.SetColliders(true);
                        item.physicBody.isKinematic = false;
                        item.physicBody.WakeUp();
                    }, 0.1f);
                });
            }
        }
        #endif
    }
}