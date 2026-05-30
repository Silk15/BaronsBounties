using ThunderRoad;
using ThunderRoad.Pools;
using ThunderRoad.Skill.Spell;
using UnityEngine;

namespace BaronsBounties
{
    public class PlasmaHands : ThunderBehaviour
    {
        public float boltRadius = 3f;
        public float damageShareRatio = 0.5f;

        public LiquidPlasmaPotion potionData;
        public Creature source;

        public float lastConsumptionTime;
        public float highestConsumption;

        private EffectInstance[] hands = new EffectInstance[2];
        private float consumptionAmount;
        private float lastHitTime;
        private float hitCooldown = 0.1f;
        
        public float ConsumptionAmount
        {
            get => consumptionAmount;
            set
            {
                highestConsumption = !value.IsApproximately(0f) ? Mathf.Max(highestConsumption, value) : 0f;
                consumptionAmount = value;
            }
        }

        public override ManagedLoops EnabledManagedLoops => ManagedLoops.Update;

        protected override void ManagedUpdate()
        {
            base.ManagedUpdate();
            if (Time.time > lastConsumptionTime + potionData.decayDelay)
            {
                ConsumptionAmount -= potionData.consumptionDecayPerSecond * Time.deltaTime;
                if (consumptionAmount < 0f)
                {
                    Clear();
                    ConsumptionAmount = 0f;
                }
            }
        }

        public void Sip(LiquidPlasmaPotion potionData, Creature source)
        {
            this.potionData = potionData;
            this.source = source;

            if (hands[0] == null)
            {
                EffectInstance left = potionData.HandEffectData.Spawn(source.handLeft.grip);
                hands[0] = left;
                left?.Play();
            }

            if (hands[1] == null)
            {
                EffectInstance right = potionData.HandEffectData.Spawn(source.handRight.grip);
                hands[1] = right;
                right?.Play();
            }

            source.handLeft.OnPunchHitEvent -= OnPunchHitEvent;
            source.handLeft.OnPunchHitEvent += OnPunchHitEvent;

            source.handRight.OnPunchHitEvent -= OnPunchHitEvent;
            source.handRight.OnPunchHitEvent += OnPunchHitEvent;
        }

        public void Clear()
        {
            for (int i = 0; i < hands.Length; i++)
                if (hands[i] != null)
                {
                    hands[i].End();
                    hands[i] = null;
                }
            
            source.handLeft.OnPunchHitEvent -= OnPunchHitEvent;
            source.handRight.OnPunchHitEvent -= OnPunchHitEvent;
        }

        private void OnPunchHitEvent(RagdollHand hand, CollisionInstance hit, bool fist)
        {
            if (Time.time - lastHitTime < hitCooldown) return;
            lastHitTime = Time.time;
            potionData.ImpactEffectData.Spawn(hit.contactPoint, Quaternion.identity, hit.targetCollider.transform).Play();
            if (hit.targetColliderGroup?.collisionHandler?.Entity is not Creature targetCreature || targetCreature.isPlayer) return;
            
            SpellCastLightning lightning = potionData.LightningSpellData;
            if (lightning != null)
            {
                lightning.ResetBoltColor();
                lightning.ForceChargeSappingLoop();
                lightning.Hit(targetCreature.ragdoll.targetPart.colliderGroup, targetCreature.ragdoll.targetPart.transform.position, Vector3.zero, Vector3.zero, 1f, false);
                targetCreature.Inflict(potionData.BurningStatusData, this, parameter: 33f);
                
                targetCreature.TryPush(Creature.PushType.Magic, targetCreature.ragdoll.targetPart.transform.position - hit.contactPoint, 1, RagdollPart.Type.Torso);
                targetCreature.AddExplosionForce(40f, hit.contactPoint, 2f, 0.5f, ForceMode.Impulse);
            }

            foreach (Creature nearbyCreature in Creature.InRadius(hit.contactPoint, boltRadius, Filter.LiveNPCs))
            {
                if (nearbyCreature.isPlayer || nearbyCreature == targetCreature) continue;

                lightning?.Hit(nearbyCreature.ragdoll.targetPart.colliderGroup, nearbyCreature.ragdoll.targetPart.transform.position, Vector3.zero, Vector3.zero, 1f, false);
                nearbyCreature.Damage(hit.damageStruct.damage * damageShareRatio);
                
                EffectInstance bolt = potionData.LightningSpellData.boltEffectData.Spawn(hit.contactPoint, Quaternion.identity);
                bolt?.SetMainGradient(new Gradient()
                {
                    colorKeys = new GradientColorKey[2]
                    {
                        new(Color.white, 0f),
                        new(Color.white, 1f)
                    },
                    alphaKeys = new GradientAlphaKey[2]
                    {
                        new(1f, 0f),
                        new(1f, 1f)
                    }
                });
                potionData.LightningSpellData.boltHitEffectData.Spawn(hit.contactPoint, Quaternion.identity).Play();
                if (bolt != null)
                {
                    Transform boltSource = PoolUtils.GetTransformPoolManager().Get();
                    Transform boltTarget = PoolUtils.GetTransformPoolManager().Get();

                    boltSource.SetParent(hit.targetCollider.transform);
                    boltSource.position = hit.contactPoint;

                    boltTarget.SetParent(nearbyCreature.ragdoll.targetPart.transform);
                    boltTarget.localPosition = Vector3.zero;

                    nearbyCreature.TryPush(Creature.PushType.Magic, boltTarget.transform.position - hit.contactPoint, 1, RagdollPart.Type.Torso);
                    nearbyCreature.Inflict(potionData.ElectrocuteStatusData, this, potionData.statusDuration / 2f);
                    nearbyCreature.Inflict(potionData.BurningStatusData, this, parameter: 33f);

                    bolt.onEffectFinished += OnFinish;
                    bolt.SetSource(boltSource);
                    bolt.SetTarget(boltTarget);
                    bolt.Play();

                    void OnFinish(EffectInstance effect)
                    {
                        bolt.onEffectFinished -= OnFinish;
                        PoolUtils.GetTransformPoolManager().Release(boltSource);
                        PoolUtils.GetTransformPoolManager().Release(boltTarget);
                        effect.Despawn();
                    }
                }
            }
        }
    }
}