using System.Collections.Generic;
using ThunderRoad;
using ThunderRoad.Skill.Spell;
using UnityEngine;

namespace BaronsBounties
{
    public class BoomstickFire : BoomstickSpell
    {
        public EffectData impactEffectData;
        public SpellCastProjectile spellCastProjectile;
        public float maxSpreadAngle = 10f;
        public string impactEffectId;
        
        public override void Load()
        {
            base.Load();
            spellCastProjectile = spellCastCharge as SpellCastProjectile;
            impactEffectData = Catalog.GetData<EffectData>(impactEffectId);
        }

        public override void Detonate(ItemModuleBoomstick.Boomstick boomstick)
        {
            base.Detonate(boomstick);
            impactEffectData.Spawn(boomstick.lastCollision.contactPoint, Quaternion.LookRotation(boomstick.lastCollision.contactNormal, boomstick.lastCollision.sourceCollider.transform.up), boomstick.lastCollision.targetCollider.transform).Play();
            int random = Random.Range(7, 10);
            for (int i = 0; i < random; i++)
            {
                Vector3 basePosition = boomstick.pierce.transform.position + boomstick.pierce.transform.forward * -0.5f;
                Vector3 offset = new(Random.Range(0.075f, -0.075f), Random.Range(0.075f, 0.075f), 0);
                Vector3 direction = Vector3.RotateTowards(current: -boomstick.pierce.transform.forward, target: Random.insideUnitSphere, maxRadiansDelta: maxSpreadAngle * Mathf.Deg2Rad, maxMagnitudeDelta: 0.0f);
                FireProjectile(basePosition + offset, direction * 15, boomstick.item);
            }
        }

        public void FireProjectile(Vector3 position, Vector3 direction, Item ignoredItem = null)
        {
            spellCastProjectile.ShootFireSpark(spellCastProjectile.imbueHitProjectileEffectData, position, direction, true, onSpawnEvent: projectile =>
            {
                projectile.guidance = GuidanceMode.NonGuided;
                projectile.guidanceFunc = null;
                projectile.homing = true;

                if (ignoredItem != null)
                    projectile.item.IgnoreItemCollision(ignoredItem);
                projectile.item.SetColliders(false);
                projectile.RunAfter(() =>
                {
                    projectile.item.SetColliders(true);
                }, 0.3f);
                
                projectile.OnProjectileCollisionEvent += OnProjectileCollisionEvent;
            });
        }

        private void OnProjectileCollisionEvent(ItemMagicProjectile projectile, CollisionInstance collisionInstance)
        {
            projectile.OnProjectileCollisionEvent -= OnProjectileCollisionEvent;
            if (collisionInstance.targetColliderGroup?.collisionHandler?.Entity is ThunderEntity thunderEntity)
                thunderEntity.Inflict("Burning", this, parameter: 45f);
        }
    }
}