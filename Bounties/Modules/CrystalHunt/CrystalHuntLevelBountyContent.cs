using System;
using System.Collections.Generic;
using BaronsBounties.Bounties.Modules;
using ThunderRoad;
using ThunderRoad.Modules;
using UnityEngine;

namespace BaronsBounties.Bounties.CrystalHunt
{
    public class CrystalHuntLevelBountyContent : LevelBountyContent
    {
        #if !SDK
        public CrystalHuntDungeonEndRewardBalance.EndRewardLootMultiplier endRewardLootMultiplier;
        public CrystalHuntLevelInstanceData.CrystalHuntLevelType levelType;
        public float tierLengthLootMultiplier;
        public int dungeonLength;
        public int factionTier;

        public List<(string, Color)> rewardTierIcons = new();

        public CrystalHuntLevelBountyContent() { }
        #endif
    }
}