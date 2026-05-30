using ThunderRoad;
using ThunderRoad.Pools;
using ThunderRoad.Skill.Spell;
using UnityEngine;

namespace BaronsBounties
{
    public class BoomstickLightning : BoomstickSpell
    {
        public SpellCastLightning spellCastLightning;
        public SkillThunderbolt skillThunderbolt;
        public float radius = 6f;

        public override void Load()
        {
            base.Load();
            spellCastLightning = spellCastCharge as SpellCastLightning;
            skillThunderbolt = Catalog.GetData<SkillThunderbolt>("Thunderbolt");
        }

        public override void Detonate(ItemModuleBoomstick.Boomstick boomstick)
        {
            base.Detonate(boomstick);
            Transform transform = PoolUtils.GetTransformPoolManager().Get();
            transform.position = boomstick.pierce.transform.position + Vector3.up * Random.Range(4, 2f);
            skillThunderbolt.FireBoltAt(transform, boomstick.pierce.transform.position);

            foreach (ThunderEntity thunderEntity in ThunderEntity.InRadius(boomstick.pierce.transform.position, radius, Filter.AllButPlayer))
            {
                spellCastLightning.PlayBolt(boomstick.pierce.transform, thunderEntity.ClosestPoint(boomstick.pierce.transform.position));
                switch (thunderEntity)
                {
                    case Item item when item.colliderGroups[0].modifier.imbueType != ColliderGroupData.ImbueType.None && item != boomstick.item:
                        item.colliderGroups[0].imbue.Transfer(spellCastLightning, Random.Range(4f, 24f));
                        item.AddExplosionForce(70f, item.transform.position, radius, 0.5f, ForceMode.Impulse);
                        break;
                    
                    case Creature creature:
                        int random = Random.Range(0, 2);
                        for (int i = 0; i < random; i++)
                        {
                            creature.AddExplosionForce(100f, boomstick.transform.position, radius, 0.1f, ForceMode.Impulse);
                            creature.TryPush(Creature.PushType.Magic, (creature.ragdoll.targetPart.transform.position - boomstick.pierce.transform.position).normalized, 1);
                            creature.Inflict(spellCastLightning.electrocuteStatusData, this, Random.Range(0f, 5));
                        }

                        break;
                }
            }
        }
    }
}