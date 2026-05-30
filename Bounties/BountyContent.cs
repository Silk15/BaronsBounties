using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using ThunderRoad;
using ThunderRoad.Modules;
using TriInspector;

namespace BaronsBounties.Bounties
{
    public class BountyContent : ContainerContent
    {
        public List<string> soldItems = new();
        public List<Reward> rewards = new();
        public int boardIndex = 0;
        public int crateIndex = -1;
        
        private BountyData bountyData;

        [JsonIgnore]
        public BountyData BountyData
        {
            get => bountyData ??= Catalog.GetData<BountyData>(referenceID);
            set
            {
                bountyData = null;
                referenceID = value.id;
            }
        }

        [JsonIgnore]
        public bool IsComplete
        {
            get => soldItems.Count == BountyData.bountyItemIds.Length;
        }

        [JsonIgnore]
        public bool HasAllRewards
        {
            get
            {
                foreach (Reward reward in rewards)
                    foreach (ResolvedDrop resolvedDrop in reward.resolvedDrops)
                        if (resolvedDrop.ownedCount < resolvedDrop.quantity)
                            return false;
                return true;
            }
        }
        
        [JsonIgnore]
        public bool IsSpawned { get; set; }

        [JsonIgnore]
        public List<Reward> RemainingRewards
        {
            get
            {
                List<Reward> remainingRewards = new();
                foreach (Reward reward in rewards)
                    foreach (ResolvedDrop resolvedDrop in reward.resolvedDrops)
                        if (resolvedDrop.Remaining > 0)
                        {
                            remainingRewards.Add(reward);
                            break;
                        }

                return remainingRewards;
            }
        }

        public BountyContent() { }

        public BountyContent(string id) => referenceID = id;

        public virtual bool CountsAsFound(string itemId) => soldItems.Contains(itemId);

        public override bool OnCatalogRefresh() => true;

        public override CatalogData catalogData => bountyData;

        public override ContainerContent Clone() => new BountyContent(referenceID);

        public override TriDropdownList<string> DropdownOptions() => new();

        public override string GetTypeString() => GetType().Name;
    }
}