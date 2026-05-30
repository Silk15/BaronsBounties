using System;
using Newtonsoft.Json;
using ThunderRoad;
using ThunderRoad.Skill.Spell;
using TriInspector;
using UnityEngine;

namespace BaronsBounties
{
    public class LiquidNeutronPotion : LiquidData
    {
        [Dropdown(nameof(GetAllEffectID))]
        public string vortexEffectId;

        [Dropdown(nameof(GetAllStatusEffectID))]
        public string floatingStatusId;

        public float consumptionDecayPerSecond = 10f;
        public float consumptionPerDrink = 20f;
        public float decayDelay = 1f;
        public float radius = 3f;

        public AnimationCurve radialForceCurve = new(new Keyframe(0.0f, 1.0f, 0.001761158f, 0.001761158f, 0.0f, 0.1492926f)
        {
            weightedMode = WeightedMode.None
        }, new Keyframe(2.746386f, 0.001640698f, 0.00007675931f, 0.00007675931f, 0.6345344f, 0.7618981f)
        {
            weightedMode = WeightedMode.Both
        }, new Keyframe(25.0f, -1.0f, -0.04501025f, -0.04501025f, 0.3333333f, 0.0f)
        {
            weightedMode = WeightedMode.None
        })
        {
            preWrapMode = WrapMode.PingPong,
            postWrapMode = WrapMode.PingPong
        };

        public AnimationCurve swirlForce = new(new Keyframe(0f, 1f, -0.0007830127f, -0.0007830127f, 0f, 0.08838244f)
        {
            weightedMode = WeightedMode.None
        }, new Keyframe(100f, 0f, -0.02849142f, -0.02849142f, 0.0158889f, 0f)
        {
            weightedMode = WeightedMode.None
        })
        {
            preWrapMode = WrapMode.PingPong,
            postWrapMode = WrapMode.PingPong
        };

        public static TriDropdownList<string> GetAllStatusEffectID() => Catalog.GetDropdownAllID(Category.Status);

        public static TriDropdownList<string> GetAllEffectID() => Catalog.GetDropdownAllID(Category.Effect);

        [JsonIgnore]
        public EffectData VortexEffectData { get; protected set; }

        [JsonIgnore]
        public StatusData FloatingStatusData { get; protected set; }

        #if !SDK
        public override void OnCatalogRefresh()
        {
            base.OnCatalogRefresh();
            VortexEffectData = Catalog.GetData<EffectData>(vortexEffectId);
            FloatingStatusData = Catalog.GetData<StatusData>(floatingStatusId);
        }

        public override void OnLiquidReception(LiquidReceiver liquidReceiver, float dilution, LiquidContainer liquidContainer)
        {
            base.OnLiquidReception(liquidReceiver, dilution, liquidContainer);

            Creature creature = liquidReceiver.Relay.creature;
            if (!creature.TryGetVariable("NeutronVortex", out NeutronVortex vortex))
            {
                vortex = new GameObject("NeutronVortex").AddComponent<NeutronVortex>();
                vortex.transform.SetParent(creature.transform, worldPositionStays: false);
                creature.SetVariable("NeutronVortex", vortex);
            }
            
            vortex.Init(this, creature);
            vortex.lastConsumptionTime = Time.time;
            vortex.ConsumptionAmount += consumptionPerDrink * dilution;
        }
        #endif

        [Serializable]
        public class NeutronChargeIndicator : Indicator
        {
            public override string GetName() => "(Neutron Potion) Neutron Charge";

            #if !SDK
            public override float GetValue(LiquidContainer container)
            {
                if (Player.currentCreature == null) return 0f;
                return Player.currentCreature.TryGetVariable("NeutronVortex", out NeutronVortex neutron) ? neutron.ConsumptionAmount : 0f;
            }
            #endif
        }
    }
}