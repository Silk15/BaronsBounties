using System;
using System.Collections;
using BaronsBounties.Bounties.CrystalHunt;
using BaronsBounties.Bounties.Sandbox;
using Newtonsoft.Json;
using ThunderRoad;

namespace BaronsBounties.Bounties
{
    public class EnemyBountyData : LevelBountyData
    {
        public string creatureTableId;
        public int factionTier;
        
        [NonSerialized]
        public CreatureTable creatureTable;
        
        [JsonIgnore]
        public override string Descriptor => "(This wanted criminal can be found in an old outpost!)";

        [JsonIgnore]
        public override bool ShowNoteIcon => true;

        public override Type GetModuleType() => typeof(LevelModuleEnemySpawner);
        
        public override Type GetBountyContentType()
        {
            switch (GameModeManager.instance.currentGameMode.id)
            {
                case "CrystalHunt":
                    return typeof(CrystalHuntEnemyBountyContent);
                
                case "Sandbox":
                    return typeof(SandboxEnemyBountyContent);
            }
            return typeof(BountyContent);
        }

        public override void OnCatalogRefresh()
        {
            base.OnCatalogRefresh();
            creatureTable = Catalog.GetData<CreatureTable>(creatureTableId);
        }
    }
}