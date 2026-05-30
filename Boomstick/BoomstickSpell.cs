using System;
using Newtonsoft.Json;
using ThunderRoad;

namespace BaronsBounties
{
    [Serializable]
    public class BoomstickSpell
    {
        public string spellId;
        public string primedEffectId;

        [NonSerialized]
        public SpellCastCharge spellCastCharge;
        
        [NonSerialized]
        public EffectData primedEffectData;
        
        public virtual void Load()
        {
            spellCastCharge = Catalog.GetData<SpellCastCharge>(spellId);
            primedEffectData = Catalog.GetData<EffectData>(primedEffectId);
        }

        public virtual void Prime(ItemModuleBoomstick.Boomstick boomstick)
        {
            
        }
        
        public virtual void Detonate(ItemModuleBoomstick.Boomstick boomstick)
        {
            
        }
    }
}