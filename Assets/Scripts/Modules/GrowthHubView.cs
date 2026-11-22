using UnityEngine;
using UnityEngine.UI;

namespace Gxdl.Modules
{
    /// <summary>
    /// 成长总览界面视图。四行入口的说明、余额与全局资源栏都由控制器填入，
    /// View 不计算任何数值，也不决定解锁。
    /// </summary>
    public sealed class GrowthHubView : UiModule
    {
        public override string ModuleId => "GrowthHubView";

        [Header("顶栏")]
        [SerializeField] Button backButton;

        /// <summary>四项成长入口，顺序为锻造、境界、天赋、灵玉。</summary>
        [Header("成长入口")]
        [SerializeField] Button[] entryButtons;
        [SerializeField] Text[] entryTitles;
        [SerializeField] Text[] entryDetails;
        [SerializeField] Text[] entryBalances;

        [Header("底部入口")]
        [SerializeField] Button dungeonsButton;
        [SerializeField] Button loadoutButton;
        [SerializeField] Text tokensText;
        [SerializeField] Text dustText;
        [SerializeField] Text powerText;

        public Button Back => backButton;

        public Button Dungeons => dungeonsButton;

        public Button Loadout => loadoutButton;

        /// <summary>四项入口统一回调，索引对应锻造、境界、天赋、灵玉的固定顺序。</summary>
        public void BindEntryCallbacks(System.Action<int> handler)
        {
            if (entryButtons == null || handler == null) return;
            for (var i = 0; i < entryButtons.Length; i++)
            {
                if (entryButtons[i] == null) continue;
                var index = i;
                entryButtons[i].onClick.RemoveAllListeners();
                entryButtons[i].onClick.AddListener(() => handler(index));
            }
        }

        /// <summary>按真实规则与存档刷新整页。</summary>
        public void Apply(Snapshot snapshot)
        {
            var count = Mathf.Min(snapshot.Entries.Length, entryButtons == null ? 0 : entryButtons.Length);
            for (var i = 0; i < count; i++)
            {
                var entry = snapshot.Entries[i];
                Set(entryTitles, i, entry.Title);
                Set(entryDetails, i, entry.Detail);
                Set(entryBalances, i, entry.Balance);
                if (entryButtons[i] != null) entryButtons[i].interactable = entry.Enabled;
            }

            Set(tokensText, snapshot.Tokens);
            Set(dustText, snapshot.Dust);
            Set(powerText, snapshot.Power);
        }

        public struct EntryCell
        {
            public string Title;
            public string Detail;
            public string Balance;
            public bool Enabled;
        }

        public struct Snapshot
        {
            public EntryCell[] Entries;
            public string Tokens;
            public string Dust;
            public string Power;
        }

        static void Set(Text target, string value)
        {
            if (target != null) target.text = value;
        }

        static void Set(Text[] target, int index, string value)
        {
            if (target == null || index < 0 || index >= target.Length) return;
            if (target[index] != null) target[index].text = value;
        }
    }
}
