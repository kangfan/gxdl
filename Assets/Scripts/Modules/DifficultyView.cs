using UnityEngine;
using UnityEngine.UI;

namespace Gxdl.Modules
{
    /// <summary>
    /// 难度选择界面视图。路径节点的解锁/通关/选中状态、
    /// 右侧详情里的奖励与条件全部由控制器按真实存档填入。
    /// </summary>
    public sealed class DifficultyView : UiModule
    {
        public override string ModuleId => "DifficultyView";

        /// <summary>节点中心坐标，原点左上，与 prefab 生成器共用同一份数据。</summary>
        public static readonly Vector2[] TierPoints =
        {
            new Vector2(364f, 640f), new Vector2(538f, 744f), new Vector2(202f, 824f),
            new Vector2(547f, 898f), new Vector2(205f, 962f), new Vector2(517f, 1027f),
            new Vector2(244f, 1084f), new Vector2(489f, 1142f), new Vector2(281f, 1193f),
            new Vector2(446f, 1243f)
        };

        static readonly float[] TierWidths = { 140f, 140f, 120f, 120f, 120f, 120f, 104f, 104f, 104f, 104f };

        [Header("顶部")]
        [SerializeField] Button backButton;
        [SerializeField] Text dungeonNameText;
        [SerializeField] Text attemptsText;

        [Header("路径")]
        [SerializeField] ScrollRect pathScroll;
        [SerializeField] Button[] tierButtons;
        [SerializeField] GameObject[] tierCleared;
        [SerializeField] GameObject[] tierLocked;
        [SerializeField] GameObject[] tierAvailable;
        [SerializeField] Image[] tierPlates;
        [SerializeField] RectTransform selectionRing;

        [Header("详情")]
        [SerializeField] Text detailTitleText;
        [SerializeField] Text rewardStateText;
        [SerializeField] GameObject[] rewardSlots;
        [SerializeField] Image[] rewardIcons;
        [SerializeField] Text[] rewardLabels;
        [SerializeField] Text conditionText;
        [SerializeField] Button challengeButton;
        [SerializeField] Text challengeLabel;
        [SerializeField] Button sweepButton;

        public Button Back => backButton;

        public Button Challenge => challengeButton;

        public Button Sweep => sweepButton;

        /// <summary>十个难度节点共用一套回调，索引即难度档位（1 起）。</summary>
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

        /// <summary>头部：秘境名、今日剩余次数；随后按当前视口高度重算路径。</summary>
        public void ApplyHeader(string dungeonName, string attempts)
        {
            if (dungeonNameText != null) dungeonNameText.text = dungeonName ?? string.Empty;
            if (attemptsText != null) attemptsText.text = attempts ?? string.Empty;
            FitPath();
            ScrollToTop();
        }

        /// <summary>路径节点状态。锁定层高度按最后一个可进入难度决定。</summary>
        public void SetPath(TierState[] tiers, int selectedTier)
        {
            if (tiers == null) return;
            var count = Mathf.Min(tiers.Length, tierButtons == null ? 0 : tierButtons.Length);
            for (var i = 0; i < count; i++)
            {
                var state = tiers[i];
                if (tierButtons[i] != null) tierButtons[i].interactable = state.Open;
                Toggle(tierCleared, i, state.Cleared);
                Toggle(tierLocked, i, !state.Cleared && !state.Open);
                Toggle(tierAvailable, i, state.Open && !state.Cleared);
                if (tierPlates != null && i < tierPlates.Length && tierPlates[i] != null)
                {
                    tierPlates[i].color = state.Open ? Color.white : new Color(0.63f, 0.71f, 0.72f);
                }
            }

            SetSelected(selectedTier);
        }

        /// <summary>选中环移到对应节点中心，不重建路径。</summary>
        public void SetSelected(int tier)
        {
            if (selectionRing == null || tier < 1 || tier > TierPoints.Length) return;
            selectionRing.gameObject.SetActive(true);
            var point = TierPoints[tier - 1];
            var size = selectionRing.sizeDelta;
            selectionRing.anchoredPosition = new Vector2(
                point.x - TierWidths[tier - 1] * 0.5f - size.x * 0.5f - 8f,
                -(point.y + TierWidths[tier - 1] * 0.375f + size.y * 0.5f + 8f));
        }

        /// <summary>详情面板：文案、奖励、条件与两个动作按钮。</summary>
        public void ApplyDetail(Detail detail)
        {
            if (detailTitleText != null) detailTitleText.text = detail.Title ?? string.Empty;
            if (rewardStateText != null) rewardStateText.text = detail.RewardState ?? string.Empty;

            ApplyRewards(detail.Rewards);
            if (conditionText != null) conditionText.text = detail.Condition ?? string.Empty;

            if (challengeButton != null) challengeButton.interactable = detail.CanChallenge;
            if (challengeLabel != null) challengeLabel.text = detail.ChallengeLabel ?? string.Empty;
            if (sweepButton != null) sweepButton.interactable = detail.CanSweep;
        }

        public void ApplyRewards(RewardCell[] rewards)
        {
            var count = rewards == null ? 0 : rewards.Length;
            var slots = rewardSlots == null ? 0 : rewardSlots.Length;
            for (var i = 0; i < slots; i++)
            {
                var show = i < count;
                if (rewardSlots[i] != null) rewardSlots[i].SetActive(show);
                if (!show) continue;

                var cell = rewards[i];
                if (rewardIcons != null && i < rewardIcons.Length && rewardIcons[i] != null)
                {
                    rewardIcons[i].sprite = cell.IconKind < 0
                        ? GuxuItemIcons.Gem(cell.GemIndex + 1)
                        : GuxuUiArt.BattlePrepReward(cell.IconKind);
                    rewardIcons[i].enabled = rewardIcons[i].sprite != null;
                }

                if (rewardLabels != null && i < rewardLabels.Length && rewardLabels[i] != null)
                {
                    rewardLabels[i].text = cell.Label ?? string.Empty;
                }
            }
        }

        /// <summary>切换难度时回到路径顶部，避免停留在旧位置。</summary>
        public void ScrollToTop()
        {
            if (pathScroll == null) return;
            pathScroll.StopMovement();
            pathScroll.content.anchoredPosition = Vector2.zero;
        }

        /// <summary>
        /// 按视口高度伸缩路径：750x808 是设计比例，长屏多出的高度平分给路径，
        /// 节点按同一比例放大，排版关系不变；节点点击区也随之变大。
        /// </summary>
        public void FitPath()
        {
            if (pathScroll == null) return;
            var viewport = pathScroll.viewport != null ? pathScroll.viewport.rect.height : 0f;
            if (viewport <= 0f) return;

            var factor = Mathf.Max(1f, viewport / 808f);
            var content = pathScroll.content;
            content.sizeDelta = new Vector2(UiRootLayout.DesignWidth, 808f * factor);
            for (var i = 0; i < content.childCount; i++)
            {
                var child = content.GetChild(i);
                if (child.name == "Difficulty Selection") continue;
                child.localScale = Vector3.one * factor;
            }

            // 选中环与节点同一比例，位置公式无需重算。
            var ring = content.Find("Difficulty Selection");
            if (ring != null) ring.localScale = Vector3.one * factor;
        }

        /// <summary>把选中的档位滚到视口内，长屏与短屏共用同一归一化坐标。</summary>
        public void FocusTier(int tier)
        {
            if (pathScroll == null || tier < 1 || tier > TierPoints.Length) return;
            var content = pathScroll.content;
            var viewport = pathScroll.viewport != null ? pathScroll.viewport.rect.height : 0f;
            if (content == null || viewport <= 0f) return;
            var position = TierPoints[tier - 1].y / 808f * content.sizeDelta.y;
            pathScroll.StopMovement();
            content.anchoredPosition = new Vector2(0f,
                Mathf.Clamp(position - viewport * 0.62f, 0f, Mathf.Max(0f, content.sizeDelta.y - viewport)));
        }

        static void Toggle(GameObject[] target, int index, bool show)
        {
            if (target == null || index < 0 || index >= target.Length) return;
            if (target[index] != null) target[index].SetActive(show);
        }

        public struct TierState
        {
            public bool Open;
            public bool Cleared;
        }

        public struct RewardCell
        {
            public string Label;
            public int IconKind;
            public int GemIndex;
        }

        public struct Detail
        {
            public string Title;
            public string RewardState;
            public RewardCell[] Rewards;
            public string Condition;
            public bool CanChallenge;
            public string ChallengeLabel;
            public bool CanSweep;
        }
    }
}
