using ThunderRoad;
using ThunderRoad.Skill.SpellMerge;
using UnityEngine;

namespace BaronsBounties
{
    public class GravnadoPulse : ThunderBehaviour
    {
        #if !SDK
        public LiquidGravnadoPotion potionData;
        public Creature source;

        public float lastConsumptionTime;
        public float highestConsumption;
        public float lastZapTime;
        public bool zapped = false;

        private float consumptionAmount;

        public float ConsumptionAmount
        {
            get => consumptionAmount;
            set
            {
                highestConsumption = !value.IsApproximately(0f) ? Mathf.Max(highestConsumption, value) : 0f;
                consumptionAmount = value;
            }
        }

        public float ZapRadius => potionData?.zapRadiusByConsumptionAmount != null ? EvaluateClamped(potionData.zapRadiusByConsumptionAmount, consumptionAmount) : 0f;

        public float ZapCooldown => potionData?.zapCooldownByConsumptionAmount != null ? EvaluateClamped(potionData.zapCooldownByConsumptionAmount, consumptionAmount) : 0f;

        public override ManagedLoops EnabledManagedLoops => ManagedLoops.Update;
        
        private static float EvaluateClamped(AnimationCurve curve, float time)
        {
            if (time <= curve.GetFirstTime()) return curve.GetFirstValue();
            if (time >= curve.GetLastTime()) return curve.GetLastValue();
            return curve.Evaluate(time);
        }

        public void Init(LiquidGravnadoPotion potionData, Creature source)
        {
            this.potionData = potionData;
            this.source = source;
        }

        protected override void ManagedUpdate()
        {
            base.ManagedUpdate();
            if (Time.time > lastConsumptionTime + potionData.decayDelay)
            {
                ConsumptionAmount -= potionData.consumptionDecayPerSecond * Time.deltaTime;
                if (consumptionAmount < 0f) ConsumptionAmount = 0f;
            }

            if (consumptionAmount <= 0f || Time.time - lastZapTime < ZapCooldown) return;
            
            lastZapTime = Time.time;
            if (consumptionAmount >= potionData.overloadThreshold)
            {
                potionData.ReadyEffectData.Spawn(source.ragdoll.targetPart.transform.position, Quaternion.identity).Play();
                this.RunAfter(() =>
                {
                    ZapInRadius();
                    consumptionAmount = 0f;
                }, Random.Range(0.7f, 1.75f));
            }

            ThunderEntity target = ThunderEntity.InRadius(source.ragdoll.targetPart.transform.position, ZapRadius, Filter.AllBut(source))?.RandomChoice();
            if (target == null)
            {
                Vector2 randomCircle = Random.insideUnitCircle.normalized;
                Vector3 rayOrigin = source.ragdoll.targetPart.transform.position;

                if (Physics.Raycast(rayOrigin, new Vector3(randomCircle.x, -0.5f, randomCircle.y).normalized, out RaycastHit hit, ZapRadius)) Zap(rayOrigin, hit.point);
                return;
            }
            ZapEntity(target, source.ragdoll.targetPart.transform);
        }
        
        private void ZapEntity(ThunderEntity entity, Transform origin)
        {
            potionData.LightningSpellData.boltHitEffectData.Spawn(entity.Center, Quaternion.identity).Play();
            potionData.LightningSpellData.PlayBolt(origin, null, null, entity.Center, potionData.previewBoltGradient);
            
            entity.Inflict(potionData.ElectrocuteStatusData, this, potionData.statusDuration);
            entity.Inflict(potionData.FloatingStatusData, this, potionData.statusDuration);
        }

        private void Zap(Vector3 source, Vector3 target)
        {
            potionData.LightningSpellData.boltHitEffectData.Spawn(target, Quaternion.identity).Play();
            potionData.LightningSpellData.PlayBolt(null, null, source, target, potionData.previewBoltGradient);
        }

        public void ZapInRadius()
        {
            if (zapped) return;
            zapped = true;
            Vector3 origin = source.ragdoll.targetPart.transform.position;
            float radius = ZapRadius;
            
            potionData.PulseEffectData.Spawn(source.ragdoll.targetPart.transform.position, Quaternion.identity).Play();
            foreach (ThunderEntity entity in ThunderEntity.InRadius(origin, radius, Filter.AllBut(source)))
            {
                ZapEntity(entity, source.ragdoll.targetPart.transform);

                switch (entity)
                {
                    case Creature creature when creature != source:
                        creature.ragdoll.SetState(Ragdoll.State.Destabilized);
                        creature.AddExplosionForce(potionData.zapExplosionForce, origin, radius, potionData.zapExplosionUpwardModifier, ForceMode.Impulse);
                        break;
                    
                    case Item item when !item.IsHeld():
                        item.AddExplosionForce(potionData.zapExplosionForce, origin, radius, potionData.zapExplosionUpwardModifier, ForceMode.Impulse);
                        break;
                    
                    case GolemController golemController:
                        golemController.StaggerImpact(origin);
                        break;
                }
            }

            this.RunAfter(() => zapped = false, 5f);
        }
        #endif
    }
}