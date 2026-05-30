using System.Collections;
using ThunderRoad;
using UnityEngine;

#if !SDK
using UnityEngine.ResourceManagement.ResourceLocations;
using IngameDebugConsole;
#endif

namespace BaronsBounties.Bounties
{
    public class LevelModuleBountyBoard : LevelModule
    {
        public string bountyBoardPrefabAddress;
        public Vector3 position;
        public Vector3 rotation;
        public Vector2 minMaxBounties;
        public int refreshDayInterval;
        public int maxBountiesPerRefresh = 4;
        
        #if !SDK
        public override IEnumerator OnPlayerSpawnCoroutine()
        {
            yield return Catalog.InstantiateCoroutine<GameObject>(bountyBoardPrefabAddress, bountyBoard =>
            {
                bountyBoard.transform.SetPositionAndRotation(position, Quaternion.Euler(rotation));
                BountyBoard board = bountyBoard.AddComponent<BountyBoard>();
                board.Init(this);
            }, "Bounty Board Prefab");
        }
        #endif
    }
}