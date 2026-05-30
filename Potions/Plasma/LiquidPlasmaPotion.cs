using System;
using Newtonsoft.Json;
using ThunderRoad;
using ThunderRoad.Skill.Spell;
using TriInspector;
using UnityEngine;

namespace BaronsBounties
{
    public class LiquidPlasmaPotion : LiquidData
    {
        [Header("Consumption")]
        public float consumptionDecayPerSecond = 5f;

        public float consumptionPerDrink = 20f;
        public float decayDelay = 2f;

        [Header("Status Effects")]
        public float statusDuration = 5f;

        [Dropdown(nameof(GetAllEffectID))]
        public string handEffectId;
        
        [Dropdown(nameof(GetAllEffectID))]
        public string impactEffectId;

        [Header("Spell IDs")]
        [Dropdown(nameof(GetAllSpellID))]
        public string lightningSpellId;

        [Header("Status IDs")]
        [Dropdown(nameof(GetAllStatusEffectID))]
        public string electrocuteStatusId;

        [Dropdown(nameof(GetAllStatusEffectID))]
        public string burningStatusId;

        public static TriDropdownList<string> GetAllStatusEffectID() => Catalog.GetDropdownAllID(Category.Status);
        
        public static TriDropdownList<string> GetAllEffectID() => Catalog.GetDropdownAllID(Category.Effect);
        
        public static TriDropdownList<string> GetAllSpellID() => Catalog.GetDropdownAllID<SpellData>();

        [JsonIgnore]
        public EffectData HandEffectData { get; protected set; }
        
        [JsonIgnore]
        public EffectData ImpactEffectData { get; protected set; }

        [JsonIgnore]
        public SpellCastLightning LightningSpellData { get; protected set; }

        [JsonIgnore]
        public StatusData ElectrocuteStatusData { get; protected set; }

        [JsonIgnore]
        public StatusData BurningStatusData { get; protected set; }

        #if !SDK
        public override void OnCatalogRefresh()
        {
            base.OnCatalogRefresh();
            ImpactEffectData = Catalog.GetData<EffectData>(impactEffectId);
            HandEffectData = Catalog.GetData<EffectData>(handEffectId);
            LightningSpellData = Catalog.GetData<SpellCastLightning>(lightningSpellId);
            ElectrocuteStatusData = Catalog.GetData<StatusData>(electrocuteStatusId);
            BurningStatusData = Catalog.GetData<StatusData>(burningStatusId);
        }

        public override void OnLiquidReception(LiquidReceiver liquidReceiver, float dilution, LiquidContainer liquidContainer)
        {
            base.OnLiquidReception(liquidReceiver, dilution, liquidContainer);

            Creature creature = liquidReceiver.Relay.creature;
            if (!creature.TryGetVariable("PlasmaHands", out PlasmaHands pulse))
            {
                pulse = new GameObject("PlasmaHands").AddComponent<PlasmaHands>();
                pulse.transform.SetParent(creature.transform, worldPositionStays: false);
                creature.SetVariable("PlasmaHands", pulse);
            }

            pulse.Sip(this, creature);
            pulse.lastConsumptionTime = Time.time;
            pulse.ConsumptionAmount += consumptionPerDrink * dilution;
        }
        #endif

        [Serializable]
        public class PlasmaChargeIndicator : Indicator
        {
            public override string GetName() => "(Plasma Potion) Plasma Charge";

            #if !SDK
            public override float GetValue(LiquidContainer container)
            {
                if (Player.currentCreature == null) return 0f;
                return Player.currentCreature.TryGetVariable("PlasmaHands", out PlasmaHands pulse) ? pulse.ConsumptionAmount : 0f;
            }
            #endif
        }
    }
}