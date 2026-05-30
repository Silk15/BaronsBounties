using ThunderRoad;
using UnityEngine;

namespace BaronsBounties
{
    public class NeutronVortex : ThunderBehaviour
    {
        public EffectInstance effect;
        public LiquidNeutronPotion potionData;
        public SphereCollider sphereCollider;
        public Creature source;
        public Zone zone;

        public float lastConsumptionTime;
        public float highestConsumption;

        private float consumptionAmount;

        public override ManagedLoops EnabledManagedLoops => ManagedLoops.Update;

        public float ConsumptionAmount
        {
            get => consumptionAmount;
            set
            {
                highestConsumption = !value.IsApproximately(0f) ? Mathf.Max(highestConsumption, value) : 0f;
                consumptionAmount = value;
            }
        }

        public void Init(LiquidNeutronPotion potionData, Creature source)
        {
            this.potionData = potionData;
            this.source = source;
        }

        protected override void ManagedUpdate()
        {
            base.ManagedUpdate();
            if (zone)
            {
                zone.forceIgnoredItemIds = new()
                {
                    Player.currentCreature.handLeft.grabbedHandle?.item?.data?.id ?? "Arrow",
                    Player.currentCreature.handRight.grabbedHandle?.item?.data?.id ?? "Arrow",
                };
            }

            if (ConsumptionAmount > 0f && Time.time > lastConsumptionTime + potionData.decayDelay)
                ConsumptionAmount -= potionData.consumptionDecayPerSecond * Time.deltaTime;

            if (ConsumptionAmount <= 0f && zone)
            {
                effect?.End();
                source.Remove(potionData.FloatingStatusData, this);

                ConsumptionAmount = 0f;
                Destroy(zone);
                zone = null;
            }
            else if (ConsumptionAmount > 0f && !zone)
            {
                potionData.VortexEffectData.Spawn(source.ragdoll.targetPart.transform).Play();

                sphereCollider = source.ragdoll.targetPart.GetOrAddComponent<SphereCollider>();
                sphereCollider.radius = potionData.radius;
                sphereCollider.isTrigger = true;

                zone = source.ragdoll.targetPart.GetOrAddComponent<Zone>();
                zone.ignorePlayerCreature = true;
                zone.creatureForceMode = Zone.CreatureForceMode.ForceParts;
                zone.ignoreNonRootParts = false;
                zone.forceNonCreatures = true;
                zone.forceTransform = source.ragdoll.targetPart.transform;

                zone.statuses.Add(potionData.FloatingStatusData, (0.1f, 0f));
                zone.playStatusEffects = true;
                zone.statusOnCreature = true;
                zone.statusOnItem = true;
                zone.statusOnPlayer = true;
                zone.disableArmorDetection = true;

                zone.radialForceActive = true;
                zone.radialForceMode = ForceMode.Force;
                zone.radialForce = potionData.radialForceCurve;
                zone.radialForceMultiplier = 20f;
                zone.radialForceProjectOnSwirlPlane = true;
                zone.noDownwardsForce = false;

                zone.swirlForceActive = true;
                zone.swirlForce = potionData.swirlForce;
                zone.swirlForceMultiplier = 100f;
                zone.swirlRandomDirection = false;
                zone.swirlForceDegrees = 70f;
                zone.swirlLocalAxis = Vector3.right;

                zone.resistiveForceActive = true;
                zone.resistiveForce = 125f;
                zone.resistiveForceMode = ForceMode.Acceleration;
                zone.minimumResistedVelocity = 10f;
                zone.resistInForceTransformForward = false;
                zone.resistInAllDirections = true;
                zone.resistiveForceDirection = Vector3.zero;

                zone.enabled = true;
                sphereCollider.enabled = true;
                source.Inflict(potionData.FloatingStatusData, this);
            }
        }
    }
}