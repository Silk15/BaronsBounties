using System;
using System.Collections.Generic;
using System.Linq;
using ThunderRoad;
using UnityEngine;

namespace BaronsBounties
{
    public class ItemModuleBoomstick : ItemModule
    {
        public List<BoomstickSpell> spells = new();
        public BoomstickSpell defaultSpell = new();
        public float energyDrainOnDetonate = 150f;
        
        public override void OnItemLoaded(Item item)
        {
            base.OnItemLoaded(item);
            foreach (BoomstickSpell spell in spells) spell.Load();
            item.gameObject.GetOrAddComponent<Boomstick>().Init(item, this);
        }

        public class Boomstick : ThunderBehaviour
        {
            public ItemModuleBoomstick itemModuleBoomstick;
            public CollisionInstance lastCollision;
            public ColliderGroup crystal;
            public Item item;
            
            public EffectInstance[] chargeEffects;
            public List<Transform> chargeTargets;
            public int chargeStages;
            public int imbueStage;
            
            public EffectInstance primedEffect;
            public EffectData primeEffectData;
            public SpellCastCharge lastSpell;
            public Damager pierce;
            public bool isPrimed;

            public bool ReadyToPrime => imbueStage == chargeStages;

            public override ManagedLoops EnabledManagedLoops => ManagedLoops.Update;

            public void Init(Item item, ItemModuleBoomstick module)
            {
                this.item = item;
                this.itemModuleBoomstick = module;
                
                primeEffectData = Catalog.GetData<EffectData>("MusketDryFire");
                pierce = item.GetCustomReference<Damager>("Pierce");
                
                Transform customReference = this.item.GetCustomReference("CrystalTargets");
                crystal = item.GetCustomReference<ColliderGroup>("Crystal");
                chargeTargets = customReference != null ? customReference.Cast<Transform>().ToList() : null;

                if (chargeTargets == null)
                {
                    Debug.LogWarning("[Boomstick] Could not find CrystalTargets custom reference.");
                }
                else
                {
                    chargeEffects = new EffectInstance[chargeTargets.Count];
                    chargeStages = chargeTargets.Count;
                }
                
                this.item.OnDespawnEvent += OnDespawn;
            }

            // I would rather die than drink prime
            public void TryPrime()
            {
                if (lastSpell == null) return;
                
                item.Haptic(1f);
                isPrimed = true;
                primeEffectData.Spawn(crystal.transform).Play();
                
                BoomstickSpell spellData = itemModuleBoomstick.spells.FirstOrDefault(s => s.spellId == lastSpell.id) ?? itemModuleBoomstick.defaultSpell;
                try
                {
                    spellData.Prime(this);
                    isPrimed = true;
                    
                    if (spellData.primedEffectData != null)
                    {
                        primedEffect = spellData.primedEffectData.Spawn(crystal.transform);
                        primedEffect.Play();
                    }

                    pierce.OnPenetrateEvent -= OnPenetrateEvent;
                    pierce.OnPenetrateEvent += OnPenetrateEvent;
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
            }

            private void OnPenetrateEvent(Damager damager, CollisionInstance collision, EventTime time)
            {
                if (time == EventTime.OnEnd && collision.impactVelocity.sqrMagnitude > 0.7f * 0.7f)
                {
                    lastCollision = collision;
                    TryDetonate();
                }
            }

            public void TryDetonate()
            {
                if (lastSpell == null) return;
                if (!Imbue.infiniteImbue)
                {
                    primedEffect?.End();
                    isPrimed = false;
                    crystal.imbue.ConsumeInstant(itemModuleBoomstick.energyDrainOnDetonate);
                    pierce.OnPenetrateEvent -= OnPenetrateEvent;
                }
                
                BoomstickSpell spellData = itemModuleBoomstick.spells.FirstOrDefault(s => s.spellId == lastSpell.id) ?? itemModuleBoomstick.defaultSpell;
                try
                {
                    spellData.Detonate(this);
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
            }

            protected override void ManagedUpdate()
            {
                base.ManagedUpdate();
                if (crystal?.imbue == null) return;
                if (crystal.imbue.spellCastBase != lastSpell)
                {
                    ClearStageEffects();
                    lastSpell = crystal.imbue.spellCastBase;
                }

                if (lastSpell == null)
                {
                    imbueStage = 0;
                    return;
                }

                imbueStage = Mathf.FloorToInt(crystal.imbue.EnergyRatio.RemapClamp(0.0f, 0.9f, 0.0f, chargeStages));
                for (int i = 0; i < chargeStages; i++)
                {
                    if (i < imbueStage && chargeEffects[i] == null)
                    {
                        chargeEffects[i] = lastSpell.musketChargeEffectData.Spawn(chargeTargets[i].transform);
                        chargeEffects[i].Play();

                        item.Haptic(0.33f * imbueStage);

                        if (imbueStage == chargeStages && ReadyToPrime && !isPrimed)
                            TryPrime();
                    }
                    
                    else if (i >= imbueStage && chargeEffects[i] != null)
                    {
                        chargeEffects[i].End();
                        chargeEffects[i] = null;
                    }
                }
            }

            public void ClearStageEffects()
            {
                if (chargeEffects == null) return;
                primedEffect?.End();
                for (int i = 0; i < chargeEffects.Length; i++)
                {
                    chargeEffects[i]?.End();
                    chargeEffects[i] = null;
                }
            }

            private void OnDespawn(EventTime eventTime)
            {
                if (eventTime == EventTime.OnStart)
                {
                    ClearStageEffects();
                    item.OnDespawnEvent -= OnDespawn;
                }
            }
        }
    }
}