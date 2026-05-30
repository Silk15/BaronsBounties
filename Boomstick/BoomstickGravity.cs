using ThunderRoad;
using UnityEngine;

namespace BaronsBounties
{
    public class BoomstickGravity : BoomstickSpell
    {
        public float radius = 6;
        public float force = 160f;
        public float upwardsModifier = 0.5f;
        public ForceMode forceMode = ForceMode.Impulse;
        public string statusId;
        public string impactEffectId;
        public StatusData statusData;
        public EffectData impactEffectData;

        public override void Load()
        {
            base.Load();
            statusData = Catalog.GetData<StatusData>(statusId);
            impactEffectData = Catalog.GetData<EffectData>(impactEffectId);
        }

        public override void Detonate(ItemModuleBoomstick.Boomstick boomstick)
        {
            base.Detonate(boomstick);
            Vector3 position = boomstick.pierce.transform.position;
            impactEffectData.Spawn(boomstick.pierce.transform.position, boomstick.pierce.transform.rotation).Play();
            foreach (ThunderEntity thunderEntity in ThunderEntity.InRadius(boomstick.pierce.transform.position, radius))
            {
                switch (thunderEntity)
                {
                    case Item item when item != boomstick.item && !item.IsHeld():
                        item.AddExplosionForce(force / 2, position, radius, upwardsModifier, forceMode);
                        item.Inflict(statusData, this, 5f);
                        break;
                    
                    case Creature creature:
                        if (!creature.isPlayer)
                        {
                            if (Vector3.Distance(creature.ragdoll.targetPart.transform.position, position) < radius / 2) creature.ragdoll.SetState(Ragdoll.State.Destabilized);
                            else creature.TryPush(Creature.PushType.Magic, (creature.ragdoll.targetPart.transform.position - position).normalized, 1);
                        }
                        
                        creature.AddExplosionForce(force, position, radius, upwardsModifier, forceMode);
                        creature.Inflict(statusData, this, 5f);
                        break;
                    
                    case GolemController golemController:
                        golemController.StaggerImpact(position);
                        break;
                }
            }
        }
    }
}