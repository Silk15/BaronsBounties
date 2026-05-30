using System;
using Newtonsoft.Json;
using ThunderRoad;
using ThunderRoad.Skill.Spell;
using TriInspector;
using UnityEngine;

namespace BaronsBounties
{
    public class LiquidGravnadoPotion : LiquidData
    {
        [Header("Zap Radius")]
        public AnimationCurve zapRadiusByConsumptionAmount = AnimationCurve.Linear(40f, 2.5f, 100f, 5f);
        
        [Header("Zap Speed")]
        public AnimationCurve zapCooldownByConsumptionAmount = AnimationCurve.Linear(40f, 2.5f, 100f, 0.1f);

        [Header("Consumption")]
        public float consumptionDecayPerSecond = 10f;

        public float consumptionPerDrink = 20f;
        public float decayDelay = 1f;
        public float overloadThreshold = 100f;

        [Header("Status Effects")]
        public float statusDuration = 5f;

        [Header("Overload Blast")]
        public float zapExplosionForce = 130f;

        public float zapExplosionUpwardModifier = 0.5f;

        [Header("Visuals")]
        public Gradient previewBoltGradient;

        [Dropdown(nameof(GetAllEffectID))]
        public string pulseEffectId;
        
        [Dropdown(nameof(GetAllEffectID))]
        public string readyEffectId;

        [Header("Spell IDs")]
        [Dropdown(nameof(GetAllSpellID))]
        public string lightningSpellId;

        [Header("Status IDs")]
        [Dropdown(nameof(GetAllStatusEffectID))]
        public string electrocuteStatusId;

        [Dropdown(nameof(GetAllStatusEffectID))]
        public string floatingStatusId;

        public static TriDropdownList<string> GetAllStatusEffectID() => Catalog.GetDropdownAllID(Category.Status);
        
        public static TriDropdownList<string> GetAllEffectID() => Catalog.GetDropdownAllID(Category.Effect);
        
        public static TriDropdownList<string> GetAllSpellID() => Catalog.GetDropdownAllID<SpellData>();

        [JsonIgnore]
        public EffectData PulseEffectData { get; protected set; }
        
        [JsonIgnore]
        public EffectData ReadyEffectData { get; protected set; }

        [JsonIgnore]
        public SpellCastLightning LightningSpellData { get; protected set; }

        [JsonIgnore]
        public StatusData ElectrocuteStatusData { get; protected set; }

        [JsonIgnore]
        public StatusData FloatingStatusData { get; protected set; }

        #if !SDK
        public override void OnCatalogRefresh()
        {
            base.OnCatalogRefresh();
            ReadyEffectData = Catalog.GetData<EffectData>(readyEffectId);
            PulseEffectData = Catalog.GetData<EffectData>(pulseEffectId);
            LightningSpellData = Catalog.GetData<SpellCastLightning>(lightningSpellId);
            ElectrocuteStatusData = Catalog.GetData<StatusData>(electrocuteStatusId);
            FloatingStatusData = Catalog.GetData<StatusData>(floatingStatusId);
        }

        public override void OnLiquidReception(LiquidReceiver liquidReceiver, float dilution, LiquidContainer liquidContainer)
        {
            base.OnLiquidReception(liquidReceiver, dilution, liquidContainer);

            Creature creature = liquidReceiver.Relay.creature;
            if (!creature.TryGetVariable("GravnadoPulse", out GravnadoPulse pulse))
            {
                pulse = new GameObject("GravnadoPulse").AddComponent<GravnadoPulse>();
                pulse.transform.SetParent(creature.transform, worldPositionStays: false);
                pulse.Init(this, creature);
                creature.SetVariable("GravnadoPulse", pulse);
            }

            pulse.lastConsumptionTime = Time.time;
            pulse.ConsumptionAmount += consumptionPerDrink * dilution;
        }
        #endif

        [Serializable]
        public class GravnadoChargeIndicator : Indicator
        {
            public override string GetName() => "(Gravnado Potion) Gravnado Charge";

            #if !SDK
            public override float GetValue(LiquidContainer container)
            {
                if (Player.currentCreature == null) return 0f;
                return Player.currentCreature.TryGetVariable("GravnadoPulse", out GravnadoPulse pulse) ? pulse.ConsumptionAmount : 0f;
            }
            #endif
        }
    }
}