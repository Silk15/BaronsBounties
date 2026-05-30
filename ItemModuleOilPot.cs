using System;
using System.Collections.Generic;
using System.Linq;
using ThunderRoad;
using ThunderRoad.Skill.Spell;
using TriInspector;
using UnityEngine;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

namespace BaronsBounties
{
    public class ItemModuleOilPot : ItemModule
    {
        [Dropdown(nameof(GetAllEffectID))]
        public string oilSplashEffectId;

        [Dropdown(nameof(GetAllEffectID))]
        public string oilPuddleEffectId;

        [Dropdown(nameof(GetAllEffectID))]
        public string trailEffectId;
        
        [Dropdown(nameof(GetAllSpellID))]
        public string[] spellIdsToLookFor;

        public float splashRadius = 1.25f;
        public float statusHeatTransfer = 100f;

        [NonSerialized]
        public EffectData oilSplashEffectData;

        [NonSerialized]
        public EffectData oilPuddleEffectData;

        [NonSerialized]
        public EffectData trailEffectData;

        public TriDropdownList<string> GetAllEffectID() => Catalog.GetDropdownAllID(Category.Effect);

        public TriDropdownList<string> GetAllSpellID() => Catalog.GetDropdownAllID<SpellCastCharge>();


        #if !SDK
        public override void OnItemLoaded(Item item)
        {
            base.OnItemLoaded(item);
            oilSplashEffectData = Catalog.GetData<EffectData>(oilSplashEffectId);
            oilPuddleEffectData = Catalog.GetData<EffectData>(oilPuddleEffectId);
            trailEffectData = Catalog.GetData<EffectData>(trailEffectId);
            item.gameObject.GetOrAddComponent<OilPot>().Init(item, this);
        }

        public class OilPot : ThunderBehaviour
        {
            public List<ParticleCollisionEvent> collisionEvents = new();
            public ItemModuleOilPot itemModuleOilPot;
            public EffectInstance effectInstance;
            public Item item;

            public void Init(Item item, ItemModuleOilPot itemModuleOilPot)
            {
                this.itemModuleOilPot = itemModuleOilPot;
                this.item = item;
      
                item.OnBreakStart -= OnBreakStart;
                item.OnBreakStart += OnBreakStart;
            }

            private void OnBreakStart(Breakable breakable)
            {
                effectInstance = itemModuleOilPot.oilSplashEffectData.Spawn(breakable.transform.position, breakable.transform.rotation);
                effectInstance.Play();

                effectInstance.OnParticleCollisionEvent -= OnParticleCollisionEvent;
                effectInstance.OnParticleCollisionEvent += OnParticleCollisionEvent;
            }

            public void OnDestroy()
            {
                if (effectInstance != null) 
                    effectInstance.OnParticleCollisionEvent -= OnParticleCollisionEvent;
            }

            private void OnParticleCollisionEvent(GameObject other)
            {
                foreach (ParticleSystem particleSystem in effectInstance.GetParticleSystems())
                {
                    int count = particleSystem.GetCollisionEvents(other, collisionEvents);
                    for (int i = 0; i < count; i++)
                    {
                        if (OilPuddle.ActivePuddles.Count >= OilPuddle.MaxPuddles) return;
                        Vector3 hitPosition = collisionEvents[i].intersection;
                        Vector3 hitNormal = collisionEvents[i].normal;
                        Quaternion rotation = Quaternion.FromToRotation(Vector3.up, hitNormal) * Quaternion.Euler(90, Random.Range(0f, 180f), 0);

                        OilPuddle oilPuddle = new GameObject($"Oil Puddle {i}").AddComponent<OilPuddle>();
                        oilPuddle.Init(itemModuleOilPot, hitPosition, rotation, other.transform);
                        oilPuddle.oilPuddle = itemModuleOilPot.oilPuddleEffectData.Spawn(hitPosition, rotation);
                        oilPuddle.oilPuddle.Play();
                    }
                }
            }

            public class OilPuddle : ThunderBehaviour
            {
                public static readonly List<OilPuddle> ActivePuddles = new();
                public const int MaxPuddles = 128;

                public SphereCollider sphereCollider;
                public TriggerDetector triggerDetector;
                public ItemModuleOilPot itemModuleOilPot;
                public EffectInstance oilPuddle;
                public bool ignited;

                public void Init(ItemModuleOilPot itemModuleOilPot, Vector3 position, Quaternion rotation, Transform parent)
                {
                    this.itemModuleOilPot = itemModuleOilPot;
                    sphereCollider = gameObject.AddComponent<SphereCollider>();
                    sphereCollider.radius = itemModuleOilPot.splashRadius;
                    sphereCollider.isTrigger = true;
                    gameObject.layer = GameManager.GetLayer(LayerName.Avatar);

                    transform.SetPositionAndRotation(position, rotation);
                    transform.parent = parent;

                    triggerDetector = gameObject.AddComponent<TriggerDetector>();
                    triggerDetector.OnTriggerEnterEvent.AddListener(OnTriggerEnter);

                    ActivePuddles.Add(this);
                }

                private void OnDestroy()
                {
                    ActivePuddles.Remove(this);
                    triggerDetector?.OnTriggerEnterEvent.RemoveListener(OnTriggerEnter);
                }

                private void OnTriggerEnter(Collider other)
                {
                    if (ignited) return;
                    if (other.TryGetComponentInParent(out ColliderGroup colliderGroup) && colliderGroup.imbue?.spellCastBase != null && itemModuleOilPot.spellIdsToLookFor.Contains(colliderGroup.imbue.spellCastBase.id)) Ignite();
                    else if (other.TryGetComponentInParent(out Item item) && item.data != null && (item.data.modules.Any(m => m is ItemModuleMagicProjectile) || item.data.id.Contains("Torch"))) Ignite();
                }

                public void Ignite()
                {
                    if (ignited) return;
                    ignited = true;

                    this.RunAfter(() =>
                    {
                        oilPuddle.End();
                        FlameWall flameWall = FlameWall.Create(transform.position);
                        flameWall.Init(null, itemModuleOilPot.trailEffectData, sphereCollider.radius, sphereCollider.radius, sphereCollider.radius * 1.5f, 0f, Random.Range(15, 12), Catalog.GetData<StatusDataBurning>("Burning"), 5f, itemModuleOilPot.statusHeatTransfer, drop: false);
                        foreach (Collider collider in Physics.OverlapSphere(transform.position, sphereCollider.radius * 3f))
                            if (collider.GetComponent<OilPuddle>() is OilPuddle neighbour) neighbour.Ignite();

                        Destroy(gameObject);
                    }, Random.Range(0.3f, 0.85f));
                }
            }
        }
        #endif
    }
}