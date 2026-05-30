using System;
using System.Collections.Generic;
using BaronsBounties.Bounties.Sandbox;
using Newtonsoft.Json;
using ThunderRoad;
using UnityEngine;

namespace BaronsBounties.Bounties.Modules
{
    public class LevelBountyContent : BountyContent
    {
        #if !SDK
        public bool showOnMap = true;
        public int randomNearest;
        public int mapLocationIndex;
        public string levelId;
        public string enemyConfigDataId;
        public string lootConfigDataId;
        public int dungeonLength;
        public int seed;
        
        public Dictionary<string, Vector3> previousPositions = new();
        public List<string> foundItems = new();
        
        private LevelBountyData levelBountyData;
        private LevelInstance levelInstance;
        private LevelData levelData;

        [JsonIgnore]
        public LevelBountyData LevelBountyData
        {
            get => levelBountyData ??= Catalog.GetData<LevelBountyData>(referenceID);
            set
            {
                levelBountyData = null;
                referenceID = value.id;
            }
        }

        [JsonIgnore]
        public LevelInstance LevelInstance
        {
            get => levelInstance;
            set => levelInstance = value;
        }

        [JsonIgnore]
        public LevelData LevelData
        {
            get => levelData ??= Catalog.GetData<LevelData>(levelId);
            set
            {
                levelData = null;
                levelId = value.id;
            }
        }
        
        public LevelBountyContent() { }
        

        public override bool CountsAsFound(string itemId) => foundItems.Contains(itemId);
        #endif
    }
}