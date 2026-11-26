using Gxdl.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Gxdl.Modules
{
    /// <summary>
    /// 通天古塔界面视图。只持有节点引用；
    /// 层数、解锁、首通状态与按钮可用性由控制器按真实存档填入。
    /// </summary>
    public sealed class TowerView : UiModule
    {
        public override string ModuleId => "TowerView";

        [Header("顶栏")]
        [SerializeField] Button backButton;
        [SerializeField] Text backLabel;
        [SerializeField] Text titleText;
        [SerializeField] Text progressText;
        [SerializeField] Text statusText;
        [SerializeField] Text hintText;
        [SerializeField] Button rewardButton;

        [Header("攀登列表")]
        [SerializeField] ScrollRect scroll;
        [SerializeField] Image[] floorArts;
        [SerializeField] Image[] floorGlows;
        [SerializeField] Text[] floorLabels;
        [SerializeField] Image[] floorBosses;
        [SerializeField] Image[] floorLocks;
        [SerializeField] Image[] floorCleareds;
        [SerializeField] Text[] floorTargets;

        [Header("右侧分层索引")]
        [SerializeField] Button[] markerButtons;
        [SerializeField] Text[] markerLabels;
        [SerializeField] Text segmentLabel;
        [SerializeField] Button previousButton;
        [SerializeField] Text previousLabel;
        [SerializeField] Button nextButton;
        [SerializeField] Text nextLabel;

        [Header("底部详情")]
        [SerializeField] Text floorTitle;
        [SerializeField] Image enemyPortrait;
        [SerializeField] Text enemyName;
        [SerializeField] Text rewardText;
        [SerializeField] Text conditionText;
        [SerializeField] Button growthButton;
        [SerializeField] Button challengeButton;
        [SerializeField] Text challengeLabel;

        public Button Back => backButton;

        public Button RewardHistory => rewardButton;

        public Button Growth => growthButton;

        public Button Challenge => challengeButton;

        /// <summary>给十个楼层条目挂统一回调，参数为真实层号。</summary>
        public void BindFloorCallbacks(System.Action<int> handler)
        {
            if (floorLabels == null || handler == null) return;
            for (var i = 0; i < floorLabels.Length; i++) Bind(floorLabels[i], i, handler);
        }

        /// <summary>右侧索引按层号跳转，同样走楼层选择回调。</summary>
        public void BindMarkerCallbacks(System.Action<int> handler)
        {
            if (markerButtons == null || handler == null) return;
            for (var i = 0; i < markerButtons.Length; i++) Bind(markerButtons[i].GetComponentInChildren<Text>(), i, handler);
        }

        /// <summary>上一段 / 下一段翻页。</summary>
        public void BindSegment(System.Action previous, System.Action next)
        {
            if (previousButton != null && previous != null)
            {
                previousButton.onClick.RemoveAllListeners();
                previousButton.onClick.AddListener(() => previous());
            }
            if (nextButton != null && next != null)
            {
                nextButton.onClick.RemoveAllListeners();
                nextButton.onClick.AddListener(() => next());
            }
        }

        void Bind(Text anchor, int index, System.Action<int> handler)
        {
            var button = anchor != null ? anchor.GetComponentInParent<Button>() : null;
            if (button == null) return;
            var captured = index;
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => handler(captured));
        }

        /// <summary>顶栏文案：来源返回、页标题、进度与当前目标。</summary>
        public void SetHeader(string back, string title, string progress, string status, string hint)
        {
            Set(backLabel, back);
            Set(titleText, title);
            Set(progressText, progress);
            Set(statusText, status);
            Set(hintText, hint);
        }

        /// <summary>十层阶梯：解锁/已通关决定压暗与状态图标，选中决定高亮。</summary>
        public void SetFloors(FloorCell[] floors)
        {
            if (floors == null) return;
            var count = Mathf.Min(floors.Length, floorLabels == null ? 0 : floorLabels.Length);
            for (var i = 0; i < count; i++)
            {
                var floor = floors[i];
                Set(floorLabels, i, floor.Label);
                if (At(floorArts, i) != null)
                {
                    floorArts[i].color = floor.Unlocked ? Color.white : new Color(0.59f, 0.68f, 0.70f, 1f);
                }

                Toggle(At(floorGlows, i), floor.Selected);
                Toggle(At(floorBosses, i), floor.Boss);
                Toggle(At(floorLocks, i), !floor.Unlocked);
                Toggle(At(floorCleareds, i), floor.Cleared && floor.Unlocked);
                Toggle(At(floorTargets, i), floor.Target);
            }
        }

        /// <summary>右侧索引：文字含 Boss 标记，当前目标层标金。</summary>
        public void SetMarkers(MarkerCell[] markers)
        {
            if (markers == null) return;
            var count = Mathf.Min(markers.Length, markerLabels == null ? 0 : markerLabels.Length);
            for (var i = 0; i < count; i++)
            {
                if (At(markerLabels, i) == null) continue;
                markerLabels[i].text = markers[i].Label;
                markerLabels[i].color = markers[i].Current ? UiFactory.Gold : UiFactory.Cream;
            }
        }

        /// <summary>分层翻页：页码两端禁用，文本由控制器给出。</summary>
        public void SetSegment(string label, string previous, bool previousEnabled, string next, bool nextEnabled)
        {
            Set(segmentLabel, label);
            Set(previousLabel, previous);
            Set(nextLabel, next);
            if (previousButton != null)
            {
                previousButton.interactable = previousEnabled;
                previousButton.name = "Tower Previous Segment";
            }

            if (nextButton != null)
            {
                nextButton.interactable = nextEnabled;
                nextButton.name = "Tower Next Segment";
            }
        }

        /// <summary>底部所选层详情：解锁状态、首通奖励是否已领与挑战可用性。</summary>
        public void SetDetail(DetailCell detail)
        {
            Set(floorTitle, detail.Title);
            Set(enemyName, detail.EnemyName);
            Set(rewardText, detail.Reward);
            Set(conditionText, detail.Condition);
            Set(challengeLabel, detail.ChallengeLabel);
            if (enemyPortrait != null && detail.Portrait != null) enemyPortrait.sprite = detail.Portrait;
            if (challengeButton != null) challengeButton.interactable = detail.ChallengeEnabled;
        }

        /// <summary>把选中层滚到视口内，对应旧实现的 FocusTowerFloor。</summary>
        public void ScrollToFloor(int index)
        {
            if (scroll == null || scroll.content == null) return;
            const float step = 190f;
            const float top = 38f;
            var viewportHeight = ((RectTransform)scroll.viewport).rect.height;
            var contentHeight = scroll.content.rect.height;
            var range = Mathf.Max(0f, contentHeight - viewportHeight);
            if (range <= 0f) return;
            var offset = Mathf.Clamp(top + index * step + 100f - viewportHeight * 0.72f, 0f, range);
            scroll.StopMovement();
            scroll.verticalNormalizedPosition = 1f - offset / range;
        }

        /// <summary>给十个楼层条目套真实阶梯美术，缺失时保留白色占位。</summary>
        public void ApplyFloorSkin(Sprite art)
        {
            if (art == null || floorArts == null) return;
            for (var i = 0; i < floorArts.Length; i++)
            {
                if (floorArts[i] != null) floorArts[i].sprite = art;
            }
        }

        public struct FloorCell
        {
            public string Label;
            public bool Unlocked;
            public bool Cleared;
            public bool Selected;
            public bool Boss;
            public bool Target;
        }

        public struct MarkerCell
        {
            public string Label;
            public bool Current;
        }

        public struct DetailCell
        {
            public string Title;
            public string EnemyName;
            public string Reward;
            public string Condition;
            public string ChallengeLabel;
            public Sprite Portrait;
            public bool ChallengeEnabled;
        }

        static Image At(Image[] source, int index) =>
            source != null && index >= 0 && index < source.Length ? source[index] : null;

        static Text At(Text[] source, int index) =>
            source != null && index >= 0 && index < source.Length ? source[index] : null;

        static void Set(Text target, string value)
        {
            if (target != null) target.text = value;
        }

        static void Set(Text[] target, int index, string value)
        {
            var node = At(target, index);
            if (node != null) node.text = value;
        }

        static void Toggle(Component target, bool visible)
        {
            if (target != null) target.gameObject.SetActive(visible);
        }
    }
}
