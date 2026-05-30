using System.Collections.Generic;
using ThunderRoad;

namespace BaronsBounties.Bounties.CrystalHunt
{
    #if !SDK
    public class CrystalHuntBountyInstanceData : CrystalHuntLevelInstanceData
    {
        public int seed;

        public CrystalHuntBountyInstanceData(
            int seed,
            int length,
            string lootConfigDataId,
            string enemyConfigDataId,
            List<(string address, UnityEngine.Color color)> rewardTierIcon,
            string difficultyIcon,
            UnityEngine.Color difficultyIconColor) : base(length, lootConfigDataId, enemyConfigDataId, rewardTierIcon, difficultyIcon, difficultyIconColor)
        {
            this.seed = seed;
        }
        
        public override Dictionary<string, string> BuildLevelOptions(Dictionary<string, string> baseOptions)
        {
            baseOptions.Add("Seed", seed.ToString());
            return base.BuildLevelOptions(baseOptions);
        }
    }
    #endif
}