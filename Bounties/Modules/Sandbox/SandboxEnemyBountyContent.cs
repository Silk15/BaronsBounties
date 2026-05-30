using System;
using System.Collections.Generic;
using System.Reflection;
using BaronsBounties.Bounties.CrystalHunt;
using BaronsBounties.Bounties.Modules;
using ThunderRoad;
using UnityEngine;

namespace BaronsBounties.Bounties.Sandbox
{
    public class SandboxEnemyBountyContent : SandboxLevelBountyContent
    {
        public Dictionary<Creature.ColorModifier, Color> persistentColorModifiers;
        public List<ContainerContent> containerContent;
        
        public string ethnicGroupId;
        public string creatureId;
        public string brainId;
        public float height;

        [NonSerialized]
        public State state;

        [NonSerialized]
        public Creature spawnedCreature;
        
        public SandboxEnemyBountyContent() { }

        public SandboxEnemyBountyContent(Creature creature)
        {
            if (creature == null) return;
            spawnedCreature = creature;
            Item[] held = new[]
            {
                creature?.handLeft?.grabbedHandle?.item,
                creature?.handRight?.grabbedHandle?.item
            };
         
            if (creature.currentEthnicGroup != null)
                ethnicGroupId = creature.currentEthnicGroup.id;

            creatureId = creature.creatureId;
            brainId = creature.brain.instance.id;
            height = creature.GetHeight();
            persistentColorModifiers = SavePersistentColorModifiers(creature);

            if (creature.container != null)
                containerContent = creature.container.CloneContents();

            foreach (Holder holder in creature.holders)
                foreach (Item item in holder.items)
                {
                    if (creature.equipment == null || item.holder == null) continue;
                    var drawSlot = creature.equipment.GetHolder(item.holder.drawSlot);
                    if (drawSlot == null) continue;
                    var newContent = new ItemContent(item.data, new ContentStateHolder(drawSlot.drawSlot.ToString()), item.contentCustomData);
                    containerContent.Add(newContent);
                }

            foreach (Item item in held)
            {
                if (item != null)
                {
                    var newSlot = creature.equipment.GetFreeDrawHolder(item.mainHandler.side, true);
                    if (newSlot == null) continue;
                    var newContent = new ItemContent(item.data, new ContentStateHolder(newSlot.drawSlot.ToString()), item.contentCustomData);
                    containerContent.Add(newContent);
                }
            }

            if (!containerContent.IsNullOrEmpty())
                foreach (ContainerContent content in containerContent)
                    if (content is ItemContent itemContent && itemContent.data.type == ItemData.Type.Wardrobe)
                        itemContent.state = new ContentStateWorn();
        }

        public static Dictionary<Creature.ColorModifier, Color> SavePersistentColorModifiers(Creature creature)
        {
            Dictionary<Creature.ColorModifier, Color> persistentColorModifiers = new();
            foreach (Creature.ColorModifier colorModifier in Enum.GetValues(typeof(Creature.ColorModifier))) persistentColorModifiers.Add(colorModifier, creature.GetColor(colorModifier));
            return persistentColorModifiers;
        }

        public static void SetPersistentColorModifiers(Creature creature, Dictionary<Creature.ColorModifier, Color> colorModifiers)
        {
            foreach (KeyValuePair<Creature.ColorModifier, Color> keyValuePair in colorModifiers)
                creature.SetColor(keyValuePair.Value, keyValuePair.Key);
        }
        
        public void Clear()
        {
            spawnedCreature.container.OnContentAddEvent -= OnContentAddEvent;
            spawnedCreature.container.OnContentRemoveEvent -= OnContentRemoveEvent;
            spawnedCreature = null;
        }

        private bool IsValid() => !string.IsNullOrEmpty(creatureId) && !string.IsNullOrEmpty(brainId);

        public void Spawn(CreatureData creatureData, Vector3 position, Quaternion rotation, Action completeCallback = null)
        {
            creatureData.containerID = null;
            if (!IsValid() || state == State.Spawned || state == State.Spawning) return;
            state = State.Spawning;
            creatureData.SpawnAsync(position, rotation.eulerAngles.y, callback: FinishSpawn);
      
            void FinishSpawn(Creature creature)
            {
                spawnedCreature = creature;
                state = State.Spawned;
                if (!string.IsNullOrEmpty(brainId))
                {
                    creature.SetFaction(2);
                    creature.brain.Load(brainId == "HumanDummy" ? "HumanMedium" : brainId);
                }
                
                if (!string.IsNullOrEmpty(ethnicGroupId))
                    creature.SetEthnicGroup(creature.GetEthnicGroupFromId(ethnicGroupId));

                if (!float.IsNaN(height))
                    creature.SetHeight(height);

                if (!containerContent.IsNullOrEmpty())
                {
                    creature.container.Load(containerContent);
                    creature.container.OnContentAddEvent += OnContentAddEvent;
                    creature.container.OnContentRemoveEvent += OnContentRemoveEvent;
                    var methodInfo = creature.container.GetType().GetMethod("LoadContents", BindingFlags.Instance | BindingFlags.NonPublic);
                    methodInfo?.Invoke(creature.container, null);
                    creature.equipment?.EquipAllWardrobes(false, false);
                }

                var brainModuleDetection = creature.brain.instance.GetModule<BrainModuleDetection>();
                brainModuleDetection.canSeeLights = false;
                brainModuleDetection.canHear = false;

                SetPersistentColorModifiers(creature, persistentColorModifiers);
                completeCallback?.Invoke();
            }
        }

        private void OnContentAddEvent(ContainerContent content, EventTime eventTime)
        {
            if (eventTime == EventTime.OnStart) return;
            if (content is ItemContent itemContent && itemContent.data.type == ItemData.Type.Wardrobe) containerContent.Add(new ItemContent(itemContent.data, new ContentStateWorn()));
        }

        private void OnContentRemoveEvent(ContainerContent content, EventTime eventTime)
        {
            if (eventTime == EventTime.OnStart) return;
            if (containerContent.Contains(content)) containerContent.Remove(content);
        }
    }
}