using System;
using System.Linq;
using ThunderRoad;
using UnityEngine.Serialization;

#if !SDK
using TMPro;
using UnityEngine;
using UnityEngine.UI;
#endif

namespace BaronsBounties.Bounties
{
    public class UIBounty : ThunderBehaviour
    {
        #if !SDK
        public UICustomisableButton infoButton;
        public Transform levelDisplay;
        public TextMeshPro description;
        public TextMeshPro title;

        [NonSerialized]
        public BountyData bountyData;
        
        public void Init(BountyData bountyData)
        {
            this.bountyData = bountyData;
            infoButton = GetComponentInChildren<UICustomisableButton>();
            TextMeshPro[] texts = GetComponentsInChildren<TextMeshPro>();

            levelDisplay = infoButton.transform.parent.GetChild(1);
            levelDisplay.gameObject.SetActive(bountyData.ShowNoteIcon);
            if (bountyData.ShowNoteIcon)
            {
                levelDisplay.GetComponent<Image>().color = bountyData.bountyNoteColor;
                levelDisplay.GetChild(0).GetComponent<Image>().sprite = bountyData.bountyNoteIcon;
            }

            description = texts[0];
            title = texts[1];
        }

        public void Refresh(BountyContent bountyContent)
        {
            description.text = "\nRequired Loot:\n- " + string.Join("\n- ", bountyData.bountyItemIds.Select(i => i.bountyDetailTitle));
            description.text += "\n\nRewards:\n- " + string.Join("\n- ", bountyContent?.rewards?.Where(r => r.resolvedDrops.Count > 0)?.Select(r => r?.CargoData?.displayName));
            title.text = bountyData.BountyDisplayName;
        }
        #endif
    }
}