using System.Collections.Generic;
using ThunderRoad;
using UnityEngine;

namespace BaronsBounties
{
    public class ItemModuleShardBrooch : ItemModule
    {
        public EffectData impactEffectData;
        public EffectData hardEnoughEffectData;

        public string impactEffectId;
        public string hardEnoughEffectId;

        public float minVelocity;
        public float triggerVelocity;

        public override void OnItemLoaded(Item item)
        {
            base.OnItemLoaded(item);
            impactEffectData = Catalog.GetData<EffectData>(impactEffectId);
            hardEnoughEffectData = Catalog.GetData<EffectData>(hardEnoughEffectId);

            item.gameObject.GetOrAddComponent<ShardBrooch>().Init(item, this);
        }
        
        public class ShardBrooch : ThunderBehaviour
        {
            public ItemModuleShardBrooch itemModule;
            public Item item;

            private float lastTime;
            
            public void Init(Item item, ItemModuleShardBrooch itemModule)
            {
                this.item = item;
                this.itemModule = itemModule;

                foreach (ColliderGroup colliderGroup in item.colliderGroups)
                {
                    if (colliderGroup.name.Contains("Brooch"))
                    {
                        colliderGroup.collisionHandler.OnCollisionStartEvent -= OnCollisionStartEvent;
                        colliderGroup.collisionHandler.OnCollisionStartEvent += OnCollisionStartEvent;
                    }
                }
            }

            private void OnCollisionStartEvent(CollisionInstance collisionInstance)
            {
                if (Time.time - lastTime <= 0.5f) return;
                lastTime = Time.time;
                
                float velocity = collisionInstance.impactVelocity.sqrMagnitude;
                if (velocity >= itemModule.minVelocity * itemModule.minVelocity)
                {
                    if (velocity >= itemModule.triggerVelocity * itemModule.triggerVelocity)
                    {
                        itemModule.hardEnoughEffectData.Spawn(collisionInstance.contactPoint, Quaternion.LookRotation(collisionInstance.contactNormal, collisionInstance.sourceCollider.transform.up), collisionInstance.sourceCollider.transform).Play();
                        List<ThunderEntity> entities = ThunderEntity.InRadius(collisionInstance.contactPoint, 1f);
                        if (entities.Count > 0)
                            foreach (ThunderEntity thunderEntity in entities)
                            {
                                if (!thunderEntity) continue;
                                thunderEntity.Inflict("Floating", this, 5f);
                                thunderEntity.AddExplosionForce(20f, collisionInstance.contactPoint, 1f, 1f, ForceMode.Impulse);
                            }
                    }
                    else
                    {
                        itemModule.impactEffectData.Spawn(collisionInstance.contactPoint, Quaternion.LookRotation(collisionInstance.contactNormal, collisionInstance.sourceCollider.transform.up), collisionInstance.sourceCollider.transform).Play();
                        item.Inflict("Floating", this, 5f);
                    }
                }
            }
        }
    }
}