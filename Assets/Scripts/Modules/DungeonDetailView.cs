using UnityEngine;
using UnityEngine.UI;

namespace Gxdl.Modules
{
    /// <summary>
    /// 秘境详情（十层难度路径）视图。十层节点的可用、通关、选中状态与底部详情
    /// 都由控制器按 Rules.Save.dungeonClears / dungeonClaims 填入。
    /// </summary>
    public sealed class DungeonDetailView : UiModule
    {
        public override string ModuleId => "DungeonDetailView";

        [Header("头部")]
        [SerializeField] Button backButton;
        [SerializeField] Text nameText;
        [SerializeField] Text attemptsText;
        [SerializeField] Button historyButton;

        [Header("十层路径")]
        [SerializeField] ScrollRect scroll;
        [SerializeField] RectTransform content;
        [SerializeField] Image lockedShade;
        [SerializeField] Button[] tierButtons;
        [SerializeField] Image[] tierPlates;
        [SerializeField] GameObject[] tierSelections;
        [SerializeField] Text[] tierNumbers;
        [SerializeField] Text[] tierStates;

        [Header("底部详情")]
        [SerializeField] Text detailTitleText;
        [SerializeField] Text detailRewardStateText;
        [SerializeField] GameObject[] detailSlots;
        [SerializeField] Text[] detailSlotLabels;
        [SerializeField] Text conditionText;
        [SerializeField] Button challengeButton;
        [SerializeField] Text challengeText;
        [SerializeField] Button sweepButton;

        public Button Back => backButton;

        public Button History => historyButton;

        public Button Challenge => challengeButton;

        public Button Sweep => sweepButton;

        public void BindTierCallbacks(System.Action<int> handler)
        {
            if (tierButtons == null || handler == null) return;
            for (var i = 0; i < tierButtons.Length; i++)
            {
                if (tierButtons[i] == null) continue;
                var tier = i + 1;
                tierButtons[i].onClick.RemoveAllListeners();
                tierButtons[i].onClick.AddListener(() => handler(tier));
            }
        }

        public void BindChallenge(System.Action handler)
        {
            if (challengeButton == null || handler == null) return;
            challengeButton.onClick.RemoveAllListeners();
            challengeButton.onClick.AddListener(() => handler());
        }

        public void BindSweep(System.Action handler)
        {
            if (sweepButton == null || handler == null) return;
            sweepButton.onClick.RemoveAllListeners();
            sweepButton.onClick.AddListener(() => handler());
        }

        /// <summary>按秘境与存档刷新头部与十层路径。</summary>
        public void Apply(DetailHeader header, TierNodeState[] tiers)
        {
            if (nameText != null) nameText.text = header.Name;
            if (attemptsText != null) attemptsText.text = header.Attempts;
            if (historyButton != null) historyButton.gameObject.SetActive(header.ShowHistory);

            // 未开层的路径整体压暗，提示玩家当前可达上限。
            if (lockedShade != null)
            {
                lockedShade.gameObject.SetActive(header.LockBottom > 0f);
                if (header.LockBottom > 0f)
                {
                    var rect = lockedShade.rectTransform;
                    rect.anchorMin = Vector2.zero;
                    rect.anchorMax = new Vector2(1f, 0f);
                    rect.sizeDelta = new Vector2(0f, header.LockBottom);
                    rect.anchoredPosition = Vector2.zero;
                }
            }

            var count = tiers == null ? 0 : Mathf.Min(tiers.Length, tierButtons == null ? 0 : tierButtons.Length);
            for (var i = 0; i < count; i++) ApplyTier(i, tiers[i]);
        }

        void ApplyTier(int index, TierNodeState tier)
        {
            if (tierButtons != null && index < tierButtons.Length && tierButtons[index] != null) tierButtons[index].interactable = tier.Unlocked;

            if (tierPlates != null && index < tierPlates.Length && tierPlates[index] != null)
            {
                tierPlates[index].color = tier.Unlocked ? Color.white : new Color(0.63f, 0.71f, 0.72f);
            }

            if (tierSelections != null && index < tierSelections.Length && tierSelections[index] != null) tierSelections[index].SetActive(tier.Selected);
            if (tierNumbers != null && index < tierNumbers.Length && tierNumbers[index] != null) tierNumbers[index].text = (index + 1).ToString();

            if (tierStates != null && index < tierStates.Length && tierStates[index] != null)
            {
                var state = tierStates[index];
                if (tier.Cleared)
                {
                    state.gameObject.SetActive(true);
                    state.text = "✓";
                    state.color = new Color(1f, 0.93f, 0.65f);
                }
                else if (!tier.Unlocked)
                {
                    state.gameObject.SetActive(true);
                    state.text = "锁";
                    state.color = new Color(0.93f, 0.87f, 0.69f);
                }
                else
                {
                    state.gameObject.SetActive(true);
                    state.text = "可挑战";
                    state.color = new Color(0.86f, 0.78f, 0.54f);
                }
            }
        }

        /// <summary>只切换选中环，供点选时局部刷新。</summary>
        public void Select(int tier)
        {
            if (tierSelections == null) return;
            for (var i = 0; i < tierSelections.Length; i++)
            {
                if (tierSelections[i] != null) tierSelections[i].SetActive(i == tier - 1);
            }
        }

        public void ApplyDetail(DetailContent detail)
        {
            if (detailTitleText != null) detailTitleText.text = detail.Title;
            if (detailRewardStateText != null) detailRewardStateText.text = detail.RewardState;
            if (conditionText != null) conditionText.text = detail.Condition;

            var rewards = detail.Rewards ?? new string[0];
            if (detailSlots != null)
            {
                for (var i = 0; i < detailSlots.Length; i++)
                {
                    if (detailSlots[i] != null) detailSlots[i].SetActive(i < rewards.Length);
                }
            }

            if (detailSlotLabels != null)
            {
                for (var i = 0; i < detailSlotLabels.Length; i++)
                {
                    if (detailSlotLabels[i] != null) detailSlotLabels[i].text = i < rewards.Length ? rewards[i] : string.Empty;
                }
            }

            if (challengeText != null) challengeText.text = detail.ChallengeLabel;
            if (challengeButton != null) challengeButton.interactable = detail.CanChallenge;
            if (sweepButton != null) sweepButton.interactable = detail.CanSweep;
        }

        public struct DetailHeader
        {
            public string Name;
            public string Attempts;
            public bool ShowHistory;
            public float LockBottom;
        }

        public struct TierNodeState
        {
            public bool Unlocked;
            public bool Cleared;
            public bool Selected;
        }

        public struct DetailContent
        {
            public string Title;
            public string RewardState;
            public string[] Rewards;
            public string Condition;
            public string ChallengeLabel;
            public bool CanChallenge;
            public bool CanSweep;
        }
    }
}
