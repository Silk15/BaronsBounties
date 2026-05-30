using ThunderRoad;
using UnityEngine.UI;

namespace BaronsBounties.Bounties
{
    public class BountyBoard : ThunderBehaviour
    {
        #if !SDK
        public UIBountyBoard bountyBoard;
        public UIBountyInfo bountyInfo;

        public void Init(LevelModuleBountyBoard levelModuleBountyBoard)
        {
            GraphicRaycaster[] graphicRaycasters = gameObject.GetComponentsInChildren<GraphicRaycaster>();
            
            bountyInfo = graphicRaycasters[1].gameObject.AddComponent<UIBountyInfo>();
            bountyInfo.Init(levelModuleBountyBoard, this);
            
            bountyBoard = graphicRaycasters[0].gameObject.AddComponent<UIBountyBoard>();
            bountyBoard.Init(levelModuleBountyBoard, this);
        }
        #endif
    }
}