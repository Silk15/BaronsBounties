#if !SDK
using System.Collections.Generic;
using ThunderRoad;

namespace BaronsBounties.Bounties.Sandbox
{
    public class SandboxBountyInstanceData : LevelInstanceData
    {
        public string enemyConfig;
        public string lootConfig;
        public int length;
        public int seed;
        
        public override Dictionary<string, string> BuildLevelOptions(Dictionary<string, string> baseOptions)
        {
            baseOptions.Add("Seed", seed.ToString());
            baseOptions.Add("DungeonLength", length.ToString());
            baseOptions.Add("EnemyConfig", enemyConfig);
            baseOptions.Add("LootConfig", lootConfig);
            return base.BuildLevelOptions(baseOptions);
        }
    }
}
#endif