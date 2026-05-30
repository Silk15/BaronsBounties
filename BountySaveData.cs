using System;
using System.Collections;
using System.Collections.Generic;
using BaronsBounties.Bounties;
using Newtonsoft.Json;
using ThunderRoad;
using UnityEngine;

using Random = UnityEngine.Random;

namespace BaronsBounties
{
    [Serializable]
    public class BountySaveData
    {
        #if !SDK
        public static BountySaveData instance;
        public List<BountySave> bountySaves = new();

        public static void LoadAsync(Action onComplete = null) => GameManager.local.StartCoroutine(LoadCoroutine(onComplete));

        public static void SaveAsync(Action onComplete = null)
        {
            Debug.Log("Bounty Save - Saving...");
            GameManager.local.StartCoroutine(SaveCoroutine(onComplete));
        }

        public static void DeleteAsync(Action onComplete = null) => GameManager.local.StartCoroutine(DeleteCoroutine(onComplete));

        public static IEnumerator SaveCoroutine(Action onComplete)
        {
            yield return GameManager.platform.WriteSaveCoroutine(new PlatformBase.Save("Bounties", "sav", JsonConvert.SerializeObject(instance, Catalog.GetJsonNetSerializerSettings())));
            onComplete?.Invoke();
        }

        public static IEnumerator DeleteCoroutine(Action onComplete)
        {
            yield return GameManager.platform.DeleteSaveCoroutine(new PlatformBase.Save("Bounties", "sav"));
            onComplete?.Invoke();
        }

        public static IEnumerator LoadCoroutine(Action onComplete)
        {
            PlatformBase.Save save = null;
            yield return GameManager.platform.ReadSaveCoroutine("Bounties", "sav", value => save = value);

            bool createdNew = false;
            if (save != null && !string.IsNullOrEmpty(save.data))
            {
                try
                {
                    instance = JsonConvert.DeserializeObject<BountySaveData>(save.data, Catalog.GetJsonNetSerializerSettings());
                    if (instance == null)
                    {
                        Debug.LogWarning("[Baron's Bounties] Deserialized save data was null, creating new one.");
                        instance = new BountySaveData();
                        createdNew = true;
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogWarning("[Baron's Bounties] Failed to deserialize save data: " + ex.Message);
                    instance = new BountySaveData();
                    createdNew = true;
                }
            }
            else
            {
                Debug.Log("[Baron's Bounties] No save data file found, creating new!");
                instance = new BountySaveData();
                createdNew = true;
            }

            if (createdNew) yield return SaveCoroutine(onComplete);
            else onComplete?.Invoke();
        }

        public BountySave GetSave(string id)
        {
            for (int i = 0; i < bountySaves.Count; i++)
            {
                BountySave bountySave = bountySaves[i];
                if (bountySave.id == id) return bountySave;
            }

            return AddSave(id);
        }

        public bool TryGetSave(string id, out BountySave bountySave)
        {
            bountySave = GetSave(id);
            if (bountySave == null)
                return false;
            return true;
        }

        public BountySave AddSave(string id)
        {
            BountySave bountySave = new BountySave();
            bountySave.id = id;
            bountySaves.Add(bountySave);
            SaveAsync();
            return bountySave;
        }

        [Serializable]
        public class BountySave
        {
            public string id;
            public List<BountyContent> availableBounties = new();
            public List<BountyContent> activeBounties = new();
            public List<BountyContent> completedBounties = new();
            
            public Dictionary<string, int> randomisedLevelBountyProgressionLevels = new();
            public LevelModuleBountyTutorial.TutorialState tutorialState = LevelModuleBountyTutorial.TutorialState.Introduction;
            public bool isFirstLevelBounty = true;
            public bool startedBounty;
            public bool randomised;
            public bool heardIntro;
         
            public int lastRefreshDay = 0;

            public void TryRandomiseLevelBounties()
            {
                if (!randomised)
                {
                    List<LevelBountyData> allLevelBounties = Catalog.GetDataList<LevelBountyData>();
                    List<int> pool = new();

                    foreach (LevelBountyData levelBountyData in allLevelBounties)
                    {
                        if (pool.Count == 0)
                        {
                            pool = new List<int> { 1, 2, 3, 4 };

                            int count = pool.Count;
                            while (count > 1)
                            {
                                count--;
                                int random = Random.Range(0, count + 1);
                                (pool[random], pool[count]) = (pool[count], pool[random]);
                            }
                        }

                        int chosenLevel = pool[pool.Count - 1];
                        pool.RemoveAt(pool.Count - 1);
                        randomisedLevelBountyProgressionLevels[levelBountyData.id] = chosenLevel;
                    }

                    randomised = true;
                }
            }

            public BountyContent GetAvailableBounty(string id)
            {
                for (int i = 0; i < availableBounties.Count; i++)
                    if (availableBounties[i].referenceID == id) return availableBounties[i];
                return null;
            }
            
            public BountyContent GetActiveBounty(string id)
            {
                for (int i = 0; i < activeBounties.Count; i++)
                    if (activeBounties[i].referenceID == id) return activeBounties[i];
                return null;
            }
            
            public BountyContent GetCompleteBounty(string id)
            {
                for (int i = 0; i < completedBounties.Count; i++)
                    if (completedBounties[i].referenceID == id) return completedBounties[i];
                return null;
            }
        }
        #endif
    }
}