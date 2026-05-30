using System;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;
using ThunderRoad;
using ThunderRoad.Pools;
using UnityEngine;
using UnityEngine.Events;
using Random = UnityEngine.Random;

namespace BaronsBounties.Bounties
{
    public class LevelModuleBountyTutorial : LevelModule
    {
        public List<TutorialSection> sections = new();

        [NonSerialized]
        public bool runningTutorial = false;

        public override IEnumerator OnPlayerSpawnCoroutine()
        {
            Shop.local.onPlayerChangeZone += OnPlayerChangeZone;
            return base.OnPlayerSpawnCoroutine();
        }

        private void OnPlayerChangeZone(Shop.ShopZone zone, bool entered)
        {
            if (!runningTutorial) 
                GameManager.local.StartCoroutine(WaitStart());
        }

        public IEnumerator WaitStart()
        {
            yield return Yielders.ForSeconds(Random.Range(3, 5f));
            yield return new WaitUntil(() => !Shop.local.moduleShopkeeper.IsSpeaking() && !Shop.local.tutorialRunning);

            runningTutorial = true;
            BountySaveData.BountySave bountySave = BountySaveData.instance.GetSave(Player.characterData.ID);
            if (bountySave.tutorialState != TutorialState.Done) ShowSection(bountySave.tutorialState);
            else runningTutorial = false;
        }

        private void ShowSection(TutorialState state)
        {
            TutorialSection section = sections.Find(s => s.tutorialState == state);
            if (section == null)
            {
                runningTutorial = false;
                return;
            }

            DisplayMessage.instance.ShowMessage(section.GetMessageData(OnSectionComplete));
            DisplayMessage.instance.WarnPlayer(true);
        }

        private void OnSectionComplete(TutorialState nextState)
        {
            if (nextState == TutorialState.Done)
            {
                runningTutorial = false;
                return;
            }

            ShowSection(nextState);
        }

        [Serializable]
        public class TutorialSection
        {
            public TutorialState tutorialState;
            public TutorialState nextState;
            public string description;
            public Vector3 position;

            [NonSerialized]
            private UnityEvent<DisplayMessage.MessageData> onSkip;

            public DisplayMessage.MessageData GetMessageData(Action<TutorialState> onComplete)
            {
                Transform anchor = PoolUtils.GetTransformPoolManager().Get();
                anchor.position = position;

                onSkip = new UnityEvent<DisplayMessage.MessageData>();
                onSkip.AddListener(data => OnSkip(data, onComplete));

                return new DisplayMessage.MessageData(
                    Description,
                    1,
                    1f,
                    anchorType: MessageAnchorType.Transform,
                    anchorTargetTransform: anchor,
                    onMessageSkip: onSkip,
                    dismissTime: 99999,
                    dismissAutomatically: false
                );
            }

            private void OnSkip(DisplayMessage.MessageData data, Action<TutorialState> onComplete)
            {
                if (data.text != Description) return;

                onSkip.RemoveAllListeners();
                BountySaveData.BountySave bountySave = BountySaveData.instance.GetSave(Player.characterData.ID);
                bountySave.tutorialState = nextState;
                BountySaveData.SaveAsync();
                onComplete?.Invoke(nextState);
            }

            [JsonIgnore]
            public string Description => LocalizationManager.Instance.TryGetLocalization("Tutorials", description);
        }

        public enum TutorialState
        {
            Introduction,
            Selecting,
            ViewActive,
            Explanation,
            Done
        }
    }
}