using System.Linq;
using ThunderRoad;

#if !SDK
using System;
using ThunderRoad.DebugViz;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
#endif

namespace BaronsBounties.Bounties
{
    public class UIBountyInfo : ThunderBehaviour
    {
        #if !SDK
        public static GameObject bountyTabPrefab;

        public HorizontalLayoutGroup horizontalLayoutGroup;
        public UICustomisableButton acceptButton;
        public UICustomisableButton abandonButton;
        public TextMeshPro title;
        public TextMeshPro info;
        public TextMeshPro list;
        public TextMeshPro descriptor;
        public Transform separator;
        public Action<Tab> onTabClick;

        public void Init(LevelModuleBountyBoard levelModuleBountyBoard, BountyBoard bountyBoard)
        {
            horizontalLayoutGroup = bountyBoard.GetComponentInChildren<HorizontalLayoutGroup>();
            UICustomisableButton[] buttons = GetComponentsInChildren<UICustomisableButton>(true);
            abandonButton = buttons[0];
            acceptButton = buttons[1];
            TextMeshPro[] texts = GetComponentsInChildren<TextMeshPro>();
            title = texts[0];
            info = texts[1];
            list = texts[2];
            descriptor = texts[3];

            separator = acceptButton.transform.parent.GetChild(1).GetChild(0);
            bountyTabPrefab = horizontalLayoutGroup.transform.GetChild(0).gameObject;
            RefreshTabs();

            abandonButton.gameObject.SetActive(false);
            acceptButton.gameObject.SetActive(false);
            separator.gameObject.SetActive(false);
        }

        public void SetBounty(BountyContent bountyContent, bool isExisting = false)
        {
            title.text = bountyContent.BountyData.BountyDisplayName;
            info.text = bountyContent.BountyData.BountyDescription;
            list.text = "- " + string.Join("\n\n- ", bountyContent.BountyData.bountyItemIds.Select(b => bountyContent.CountsAsFound(b.itemId) ? $"<s>{b.bountyDetailTitle} - {b.bountyDetailDescription}</s>" : $"{b.bountyDetailTitle} - {b.bountyDetailDescription}"));
            descriptor.text = bountyContent.BountyData.Descriptor;
            if (isExisting)
            {
                abandonButton.gameObject.SetActive(false);
                abandonButton.gameObject.SetActive(true);
            }
            else
            {
                acceptButton.gameObject.SetActive(true);
                abandonButton.gameObject.SetActive(false);
            }
            separator.gameObject.SetActive(true);
        }

        public void ClearBounty()
        {
            title.text = "";
            info.text = "";
            list.text = "";
            descriptor.text = "";
            abandonButton.gameObject.SetActive(false);
            acceptButton.gameObject.SetActive(false);
            separator.gameObject.SetActive(false);
        }

        public void RefreshTabs()
        {
            foreach (Tab tab in horizontalLayoutGroup.GetComponentsInChildren<Tab>(true)) Destroy(tab.gameObject);
            foreach (BountyContent bountyContent in BountySaveData.instance.GetSave(Player.characterData.ID).activeBounties)
            {
                Tab tab = Instantiate(bountyTabPrefab, horizontalLayoutGroup.transform).AddComponent<Tab>();
                tab.gameObject.SetActive(true);
                tab.bountyContent = bountyContent;
                tab.GetComponentsInChildren<Image>(true)[3].sprite = bountyContent.BountyData.bountyTabIcon;
                tab.uiCustomisableButton.onPointerClick.AddListener(() =>
                {
                    onTabClick?.Invoke(tab);
                    SetBounty(tab.bountyContent, true);
                });
            }
        }

        public class Tab : ThunderBehaviour
        {
            public UICustomisableButton uiCustomisableButton;
            public BountyContent bountyContent;

            public void Awake() => uiCustomisableButton = GetComponentInChildren<UICustomisableButton>();
        }
        #endif
    }
}