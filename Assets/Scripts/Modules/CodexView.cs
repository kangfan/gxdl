using UnityEngine;
using UnityEngine.UI;

namespace Gxdl.Modules
{
    /// <summary>
    /// 遗宝图鉴界面视图。12 个部位的图标与名称来自静态配置，
    /// 阶级数值文本由控制器按当前遗域等级计算后填入。
    /// </summary>
    public sealed class CodexView : UiModule
    {
        public override string ModuleId => "CodexView";

        [Header("顶栏")]
        [SerializeField] Button backButton;
        [SerializeField] Text hintText;

        [Header("部位格")]
        [SerializeField] Transform entryRoot;
        [SerializeField] Image[] slotIcons;
        [SerializeField] Text[] slotNames;
        [SerializeField] Image[] slotShadows;
        [SerializeField] Button[] slotButtons;

        [Header("阶级表")]
        [SerializeField] RectTransform tablePanel;
        [SerializeField] Text tableTitle;
        [SerializeField] Text tableNote;
        [SerializeField] Transform rowRoot;
        [SerializeField] Text[] rowNames;
        [SerializeField] Text[] rowAttack;
        [SerializeField] Text[] rowHp;
        [SerializeField] Text[] rowDefense;
        [SerializeField] Button[] rowButtons;
        [SerializeField] Button closeTableButton;

        public Button Back => backButton;
        public Button CloseTable => closeTableButton;

        public void ShowTable(bool visible)
        {
            if (tablePanel != null) tablePanel.gameObject.SetActive(visible);
            if (closeTableButton != null) closeTableButton.gameObject.SetActive(visible);
        }

        public void BindCallbacks(System.Action<int> onSlot, System.Action<int> onRow)
        {
            Wire(slotButtons, onSlot);
            Wire(rowButtons, onRow);
        }

        /// <summary>刷新 12 个部位的名称与可用状态。</summary>
        public void ApplyEntries(CodexSnapshot snapshot)
        {
            if (hintText != null) hintText.text = snapshot.Hint;
            var count = slotNames == null ? 0 : slotNames.Length;
            for (var i = 0; i < count && snapshot.Entries != null && i < snapshot.Entries.Length; i++)
            {
                var entry = snapshot.Entries[i];
                Set(slotNames, i, entry.Name);
                if (slotIcons != null && i < slotIcons.Length && slotIcons[i] != null)
                    slotIcons[i].sprite = GuxuUiArt.Item(i);
                if (slotButtons != null && i < slotButtons.Length && slotButtons[i] != null)
                    slotButtons[i].interactable = entry.Enabled;
            }
        }

        /// <summary>刷新某个部位的品质 × 阶级数值表；rows 最多 15 行。</summary>
        public void ApplyTable(string title, string note, TierRow[] rows)
        {
            if (tableTitle != null) tableTitle.text = title;
            if (tableNote != null) tableNote.text = note;
            if (rowButtons != null)
            {
                for (var i = 0; i < rowButtons.Length; i++)
                {
                    if (rowButtons[i] == null) continue;
                    rowButtons[i].gameObject.SetActive(rows != null && i < rows.Length);
                }
            }

            if (rows == null) return;
            for (var i = 0; i < rows.Length; i++)
            {
                Set(rowNames, i, rows[i].Label);
                Set(rowAttack, i, rows[i].Attack);
                Set(rowHp, i, rows[i].Hp);
                Set(rowDefense, i, rows[i].Defense);
            }
        }

        static void Wire(Button[] target, System.Action<int> handler)
        {
            if (target == null || handler == null) return;
            for (var i = 0; i < target.Length; i++)
            {
                var index = i;
                if (target[i] == null) continue;
                target[i].onClick.RemoveAllListeners();
                target[i].onClick.AddListener(() => handler(index));
            }
        }

        static void Set(Text[] target, int index, string value)
        {
            if (target == null || index < 0 || index >= target.Length || target[index] == null) return;
            target[index].text = value;
        }

        public struct EntryCell
        {
            public string Name;
            public bool Enabled;
        }

        public struct TierRow
        {
            public string Label;
            public string Attack;
            public string Hp;
            public string Defense;
        }

        public struct CodexSnapshot
        {
            public string Hint;
            public EntryCell[] Entries;
        }
    }
}
