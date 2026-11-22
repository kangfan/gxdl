using UnityEngine;
using UnityEngine.UI;

namespace Gxdl.Modules
{
    /// <summary>
    /// 世界地图（十域总览）界面视图。只持有节点引用，
    /// 章节解锁、进度与资源数值由控制器按真实存档填入。
    /// </summary>
    public sealed class WorldMapView : UiModule
    {
        public override string ModuleId => "WorldMapView";

        [Header("顶栏")]
        [SerializeField] Button backButton;
        [SerializeField] Text progressText;

        [Header("章节卡片")]
        [SerializeField] Transform chapterRoot;
        [SerializeField] Text[] chapterNames;
        [SerializeField] Text[] chapterRanges;
        [SerializeField] Text[] chapterProgress;
        [SerializeField] Text[] chapterEntries;
        [SerializeField] Button[] chapterButtons;
        [SerializeField] Image[] chapterLocks;
        [SerializeField] Text[] chapterIndex;

        [Header("底部入口")]
        [SerializeField] Button dungeonsButton;
        [SerializeField] Button towerButton;
        [SerializeField] Text tokensText;
        [SerializeField] Text dustText;
        [SerializeField] Text powerText;

        public Button Back => backButton;

        public Button Dungeons => dungeonsButton;

        public Button Tower => towerButton;

        /// <summary>给十张章节卡片挂统一回调，索引即章节序号。</summary>
        public void BindChapterCallbacks(System.Action<int> handler)
        {
            if (chapterButtons == null || handler == null) return;
            for (var i = 0; i < chapterButtons.Length; i++)
            {
                if (chapterButtons[i] == null) continue;
                var index = i;
                chapterButtons[i].onClick.RemoveAllListeners();
                chapterButtons[i].onClick.AddListener(() => handler(index));
            }
        }

        /// <summary>按真实配置与存档刷新整页。</summary>
        public void Apply(MapSnapshot snapshot)
        {
            if (progressText != null) progressText.text = snapshot.ProgressLabel;

            var count = Mathf.Min(snapshot.Chapters.Length, chapterButtons == null ? 0 : chapterButtons.Length);
            for (var i = 0; i < count; i++)
            {
                var chapter = snapshot.Chapters[i];
                Set(chapterNames, i, chapter.Name);
                Set(chapterRanges, i, chapter.Range);
                Set(chapterProgress, i, chapter.Progress);
                Set(chapterEntries, i, chapter.Entry);
                Set(chapterIndex, i, chapter.Index);

                if (chapterButtons[i] != null) chapterButtons[i].interactable = chapter.Unlocked;
                if (chapterLocks != null && i < chapterLocks.Length && chapterLocks[i] != null)
                {
                    chapterLocks[i].gameObject.SetActive(!chapter.Unlocked);
                }
            }

            if (tokensText != null) tokensText.text = snapshot.Tokens;
            if (dustText != null) dustText.text = snapshot.Dust;
            if (powerText != null) powerText.text = snapshot.Power;
        }

        public struct ChapterCell
        {
            public string Name;
            public string Range;
            public string Progress;
            public string Entry;
            public string Index;
            public bool Unlocked;
        }

        public struct MapSnapshot
        {
            public string ProgressLabel;
            public ChapterCell[] Chapters;
            public string Tokens;
            public string Dust;
            public string Power;
        }

        static void Set(Text[] target, int index, string value)
        {
            if (target == null || index < 0 || index >= target.Length) return;
            if (target[index] != null) target[index].text = value;
        }
    }
}
