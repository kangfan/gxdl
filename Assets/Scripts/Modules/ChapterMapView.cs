using UnityEngine;
using UnityEngine.UI;

namespace Gxdl.Modules
{
    /// <summary>
    /// 章节地图界面视图。20 个关卡节点的位置在 prefab 中按 ChapterPoint 公式预先摆好，
    /// 解锁、通关、当前、选中状态与底部详情全部由控制器按真实存档在运行时填入。
    /// </summary>
    public sealed class ChapterMapView : UiModule
    {
        public override string ModuleId => "ChapterMapView";

        const int NodeCount = 20;
        const float ContentHeight = 2800f;

        [Header("地形")]
        [SerializeField] Image[] terrainImages;

        [Header("章节头部")]
        [SerializeField] Button backButton;
        [SerializeField] Text chapterNameText;
        [SerializeField] Text chapterStoryText;
        [SerializeField] Text chapterProgressText;
        [SerializeField] RectTransform progressFill;
        [SerializeField] Text[] milestoneLabels;
        [SerializeField] Button claimButton;
        [SerializeField] Text chestStateText;

        [Header("关卡节点")]
        [SerializeField] ScrollRect scroll;
        [SerializeField] RectTransform content;
        [SerializeField] Button[] nodeButtons;
        [SerializeField] Image[] nodeGlows;
        [SerializeField] Outline[] nodeOutlines;
        [SerializeField] Image[] nodeArts;
        [SerializeField] Text[] nodeLabels;
        [SerializeField] Text[] nodeMarks;
        [SerializeField] GameObject[] nodeCurrentMarks;

        [Header("底部详情")]
        [SerializeField] Transform detailRoot;
        [SerializeField] Text detailTitleText;
        [SerializeField] Text detailEnemyText;
        [SerializeField] Text detailRewardStateText;
        [SerializeField] GameObject[] detailSlots;
        [SerializeField] Text[] detailSlotLabels;
        [SerializeField] Button challengeButton;
        [SerializeField] Text challengeText;
        [SerializeField] Button growthButton;
        [SerializeField] GameObject hintRoot;

        readonly Vector2[] points = new Vector2[NodeCount];
        bool pointsReady;

        public Button Back => backButton;

        public Button Chest => claimButton;

        public Button Challenge => challengeButton;

        public Button Growth => growthButton;

        /// <summary>节点坐标只算一次，聚焦时直接查表。</summary>
        void EnsurePoints()
        {
            if (pointsReady) return;
            for (var i = 0; i < NodeCount; i++) points[i] = PointOf(i);
            pointsReady = true;
        }

        /// <summary>节点索引 → 坐标。与 prefab 生成器共用同一公式，保证聚焦一致。</summary>
        public static Vector2 PointOf(int index)
        {
            // 平台坐标按原版章节地图逐关规定；先 7 关一屏，之后每 5 关叠加一个地形段。
            var platforms = new[]
            {
                new Vector2(152f, 73f), new Vector2(375f, 121f), new Vector2(185f, 210f),
                new Vector2(542f, 273f), new Vector2(265f, 371f), new Vector2(541f, 449f),
                new Vector2(336f, 550f)
            };
            var later = new[] { 0, 1, 4, 5, 6 };
            var tile = index < 7 ? 0 : 1 + (index - 7) / 5;
            var slot = index < 7 ? index : later[(index - 7) % 5];
            var point = platforms[slot];
            point.y += 45f + tile * 720f;
            return point;
        }

        public void BindNodeCallbacks(System.Action<int> handler)
        {
            if (nodeButtons == null || handler == null) return;
            for (var i = 0; i < nodeButtons.Length; i++)
            {
                if (nodeButtons[i] == null) continue;
                var index = i;
                nodeButtons[i].onClick.RemoveAllListeners();
                nodeButtons[i].onClick.AddListener(() => handler(index));
            }
        }

        public void BindChallenge(System.Action handler)
        {
            if (challengeButton == null || handler == null) return;
            challengeButton.onClick.RemoveAllListeners();
            challengeButton.onClick.AddListener(() => handler());
        }

        public void BindGrowth(System.Action handler)
        {
            if (growthButton == null || handler == null) return;
            growthButton.onClick.RemoveAllListeners();
            growthButton.onClick.AddListener(() => handler());
        }

        /// <summary>按章节与真实存档刷新整页。</summary>
        public void Apply(int chapterIndex, ChapterHeader header, ChapterNodeState[] nodes)
        {
            if (chapterNameText != null) chapterNameText.text = header.Name;
            if (chapterStoryText != null) chapterStoryText.text = header.Story;
            if (chapterProgressText != null) chapterProgressText.text = header.Progress;

            // anchorMax 改了宽度会残留，sizeDelta 归零让宽度完全由锚点决定。
            var ratio = Mathf.Clamp01(header.ClearedCount / 20f);
            if (progressFill != null)
            {
                progressFill.anchorMax = new Vector2(ratio, 1f);
                progressFill.sizeDelta = Vector2.zero;
            }
            if (milestoneLabels != null)
            {
                // 四个里程碑分别对应第 5 / 10 / 15 / 20 关。
                for (var i = 0; i < milestoneLabels.Length; i++)
                {
                    if (milestoneLabels[i] != null)
                    {
                        milestoneLabels[i].color = header.ClearedCount >= (i + 1) * 5 ? new Color(0.86f, 0.78f, 0.54f) : new Color(0.94f, 0.94f, 0.87f);
                    }
                }
            }

            if (claimButton != null) claimButton.interactable = header.ChestReady;
            if (chestStateText != null) chestStateText.text = header.ChestState;

            var count = nodes == null ? 0 : Mathf.Min(nodes.Length, nodeButtons == null ? 0 : nodeButtons.Length);
            for (var i = 0; i < count; i++) ApplyNode(i, nodes[i]);

            // 选中态由 Select 独立控制，切章时先清空避免旧高亮残留。
            Select(-1);
        }

        void ApplyNode(int index, ChapterNodeState node)
        {
            // 三种状态共用同一结构：锁定压灰、通关染青、当前加金色光晕与标记。
            if (nodeArts != null && index < nodeArts.Length && nodeArts[index] != null)
            {
                nodeArts[index].color = !node.Unlocked
                    ? new Color(0.68f, 0.77f, 0.80f)
                    : node.Cleared ? new Color(0.75f, 1f, 0.93f) : Color.white;
            }

            if (nodeLabels != null && index < nodeLabels.Length && nodeLabels[index] != null)
            {
                nodeLabels[index].text = node.Label;
                nodeLabels[index].color = node.Unlocked ? new Color(0.94f, 0.94f, 0.87f) : new Color(0.85f, 0.9f, 0.91f);
            }

            if (nodeMarks != null && index < nodeMarks.Length && nodeMarks[index] != null)
            {
                var show = node.Cleared || !node.Unlocked;
                nodeMarks[index].gameObject.SetActive(show);
                nodeMarks[index].text = node.Cleared ? "✓" : "锁";
                nodeMarks[index].color = node.Cleared ? new Color(1f, 0.93f, 0.65f) : new Color(0.93f, 0.87f, 0.69f);
            }

            if (nodeCurrentMarks != null && index < nodeCurrentMarks.Length && nodeCurrentMarks[index] != null)
            {
                nodeCurrentMarks[index].SetActive(node.Current);
            }
        }

        /// <summary>切换选中高亮，不影响节点数据。</summary>
        public void Select(int index)
        {
            if (nodeGlows != null)
            {
                for (var i = 0; i < nodeGlows.Length; i++)
                {
                    if (nodeGlows[i] != null) nodeGlows[i].gameObject.SetActive(i == index);
                }
            }

            if (nodeOutlines != null)
            {
                for (var i = 0; i < nodeOutlines.Length; i++)
                {
                    if (nodeOutlines[i] != null) nodeOutlines[i].enabled = i == index;
                }
            }
        }

        /// <summary>把选中节点滚到视口偏上的位置，避免被底部详情面板挡住。</summary>
        public void Focus(int index)
        {
            if (scroll == null || content == null) return;
            if (index < 0 || index >= points.Length) return;
            EnsurePoints();

            var viewport = scroll.viewport != null ? scroll.viewport.rect.height : 0f;
            var limit = Mathf.Max(0f, content.rect.height - viewport);
            scroll.StopMovement();
            content.anchoredPosition = new Vector2(0f, Mathf.Clamp(points[index].y - viewport * 0.68f, 0f, limit));
        }

        /// <summary>非首章用地区色给地形叠一层色调。</summary>
        public void SetTerrainTint(Color tint)
        {
            if (terrainImages == null) return;
            var baseColor = Color.Lerp(Color.white, tint, 0.22f);
            for (var i = 0; i < terrainImages.Length; i++)
            {
                if (terrainImages[i] != null) terrainImages[i].color = baseColor;
            }
        }

        public void ApplyDetail(ChapterDetail detail)
        {
            if (detailTitleText != null) detailTitleText.text = detail.Title;
            if (detailEnemyText != null) detailEnemyText.text = detail.Enemy;
            if (detailRewardStateText != null) detailRewardStateText.text = detail.RewardState;

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
            if (challengeButton != null) challengeButton.interactable = detail.CanEnter;
        }

        public void SetHintVisible(bool visible)
        {
            if (hintRoot != null) hintRoot.SetActive(visible);
        }

        /// <summary>头部动态值：章节名、通关数、宝匣状态。</summary>
        public struct ChapterHeader
        {
            public string Name;
            public string Story;
            public string Progress;
            public int ClearedCount;
            public bool ChestReady;
            public string ChestState;
        }

        /// <summary>单关节点状态。Label 由控制器按章节规则生成，视图不拼业务文案。</summary>
        public struct ChapterNodeState
        {
            public string Label;
            public bool Unlocked;
            public bool Cleared;
            public bool Current;
        }

        /// <summary>底部选中详情。Rewards 已是「名称 + 数值」成品文本。</summary>
        public struct ChapterDetail
        {
            public string Title;
            public string Enemy;
            public string RewardState;
            public string[] Rewards;
            public string ChallengeLabel;
            public bool CanEnter;
        }
    }
}
