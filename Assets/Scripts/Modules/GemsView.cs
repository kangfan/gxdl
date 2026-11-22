using UnityEngine;
using UnityEngine.UI;

namespace Gxdl.Modules
{
    /// <summary>
    /// 灵玉工坊界面视图。只持有节点与回调，槽位、库存、镶嵌状态由控制器按真实存档填入。
    /// </summary>
    public sealed class GemsView : UiModule
    {
        public override string ModuleId => "GemsView";

        [Header("顶栏")]
        [SerializeField] Button backButton;
        [SerializeField] Text totalText;
        [SerializeField] Text hintText;

        [Header("装备槽")]
        [SerializeField] Transform slotRoot;
        [SerializeField] Image[] slotIcons;
        [SerializeField] Image[] slotFrames;
        [SerializeField] Text[] slotNames;
        [SerializeField] Image[] slotHighlights;
        [SerializeField] Button[] slotButtons;

        [Header("工作台")]
        [SerializeField] Image selectedIcon;
        [SerializeField] Text selectedLabel;
        [SerializeField] Image[] socketIcons;
        [SerializeField] Text[] socketLabels;
        [SerializeField] Text[] socketHints;
        [SerializeField] Image[] socketHighlights;
        [SerializeField] Button[] socketButtons;
        [SerializeField] Text socketSummary;
        [SerializeField] Button removeButton;

        [Header("灵玉种类")]
        [SerializeField] Image[] typeIcons;
        [SerializeField] Text[] typeLabels;
        [SerializeField] Image[] typeFrames;
        [SerializeField] Button[] typeButtons;

        [Header("库存")]
        [SerializeField] Image[] stockIcons;
        [SerializeField] Image[] stockFrames;
        [SerializeField] Text[] stockCounts;
        [SerializeField] Text[] stockLevels;
        [SerializeField] Image[] stockHighlights;
        [SerializeField] Button[] stockButtons;

        [Header("详情")]
        [SerializeField] Image detailIcon;
        [SerializeField] Text detailName;
        [SerializeField] Text detailEffect;
        [SerializeField] Text detailOwned;
        [SerializeField] Text detailTarget;
        [SerializeField] Button equipButton;
        [SerializeField] Text equipLabel;
        [SerializeField] Button combineButton;
        [SerializeField] Text combineLabel;
        [SerializeField] Button getMoreButton;
        [SerializeField] Button returnButton;

        int shownColor;

        public Button Back => backButton;
        public Button GetMore => getMoreButton;
        public Button Return => returnButton;

        public void BindCallbacks(
            System.Action<int> onSlot,
            System.Action<int> onSocket,
            System.Action<int> onType,
            System.Action<int> onStock,
            System.Action onRemove,
            System.Action onEquip,
            System.Action onCombine)
        {
            Wire(slotButtons, onSlot);
            Wire(socketButtons, onSocket);
            Wire(typeButtons, onType);
            Wire(stockButtons, onStock);
            Wire(removeButton, onRemove);
            Wire(equipButton, onEquip);
            Wire(combineButton, onCombine);
        }

        /// <summary>按真实存档刷新整页；Equip 为 null 表示无可用镶嵌目标。</summary>
        public void Apply(GemSnapshot snapshot)
        {
            if (totalText != null) totalText.text = snapshot.TotalLabel;
            if (hintText != null) hintText.text = snapshot.Hint;

            // 槽位数量以 prefab 实际绑定为准，快照缺失时留空而不是越界。
            var slotCount = slotNames == null ? 0 : slotNames.Length;
            for (var i = 0; i < slotCount && snapshot.Equipment != null && i < snapshot.Equipment.Length; i++)
            {
                var cell = snapshot.Equipment[i];
                Set(slotNames, i, cell.Name);
                Select(slotHighlights, i, i == snapshot.SlotIndex);
            }

            var selected = snapshot.Equipment != null && snapshot.Equipment.Length > 0
                ? snapshot.Equipment[Mathf.Clamp(snapshot.SlotIndex, 0, snapshot.Equipment.Length - 1)]
                : default(EquipmentCell);
            if (selectedLabel != null) selectedLabel.text = selected.DetailLabel;

            var socketCount = socketLabels == null ? 0 : socketLabels.Length;
            for (var hole = 0; hole < socketCount && snapshot.Sockets != null && hole < snapshot.Sockets.Length; hole++)
            {
                var socket = snapshot.Sockets[hole];
                Select(socketHighlights, hole, hole == snapshot.SocketIndex);
                if (socket.Locked)
                {
                    Set(socketLabels, hole, string.Empty);
                    Set(socketHints, hole, socket.Hint);
                }
                else
                {
                    Set(socketLabels, hole, socket.GemLabel);
                    Set(socketHints, hole, socket.Hint);
                }
            }

            if (socketSummary != null) socketSummary.text = snapshot.SocketSummary;
            if (removeButton != null) removeButton.interactable = snapshot.CanRemove;

            shownColor = Mathf.Clamp(snapshot.ColorIndex, 0, 3);
            var typeCount = typeLabels == null ? 0 : typeLabels.Length;
            for (var type = 0; type < typeCount && snapshot.Types != null && type < snapshot.Types.Length; type++)
            {
                Set(typeLabels, type, snapshot.Types[type].Name);
                SetTitle(typeLabels, type, type == shownColor);
            }

            var stockCount = stockCounts == null ? 0 : stockCounts.Length;
            for (var level = 0; level < stockCount && snapshot.Stocks != null && level < snapshot.Stocks.Length; level++)
            {
                var stock = snapshot.Stocks[level];
                Set(stockCounts, level, stock.Count);
                Set(stockLevels, level, stock.LevelLabel);
                if (stockIcons != null && level < stockIcons.Length && stockIcons[level] != null)
                    stockIcons[level].color = stock.Greyed ? new Color(.48f, .57f, .59f) : Color.white;
                Select(stockHighlights, level, level == snapshot.StockIndex);
            }

            if (detailName != null) detailName.text = snapshot.DetailName;
            if (detailEffect != null) detailEffect.text = snapshot.DetailEffect;
            if (detailOwned != null) detailOwned.text = snapshot.DetailOwned;
            if (detailTarget != null) detailTarget.text = snapshot.DetailTarget;
            if (equipLabel != null) equipLabel.text = snapshot.EquipLabel;
            if (combineLabel != null) combineLabel.text = snapshot.CombineLabel;

            // 按钮可用性直接取存档计算出的结果，界面不自行推导。
            if (equipButton != null) equipButton.interactable = snapshot.CanEquip;
            if (combineButton != null) combineButton.interactable = snapshot.CanCombine;
        }

        void SetTitle(Text[] target, int index, bool selected)
        {
            if (target == null || index < 0 || index >= target.Length || target[index] == null) return;
            target[index].color = selected ? new Color(0.94f, 0.78f, 0.36f) : new Color(0.94f, 0.94f, 0.87f);
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

        static void Wire(Button target, System.Action handler)
        {
            if (target == null || handler == null) return;
            target.onClick.RemoveAllListeners();
            target.onClick.AddListener(() => handler());
        }

        static void Set(Text[] target, int index, string value)
        {
            if (target == null || index < 0 || index >= target.Length || target[index] == null) return;
            target[index].text = value;
        }

        static void Select(Image[] target, int index, bool active)
        {
            if (target == null || index < 0 || index >= target.Length || target[index] == null) return;
            target[index].gameObject.SetActive(active);
        }

        public struct EquipmentCell
        {
            public string Name;
            public string DetailLabel;
        }

        public struct SocketCell
        {
            public bool Locked;
            public string GemLabel;
            public string Hint;
        }

        public struct StockCell
        {
            public string Count;
            public string LevelLabel;
            public bool Greyed;
        }

        public struct TypeCell
        {
            public string Name;
        }

        public struct GemSnapshot
        {
            public string TotalLabel;
            public string Hint;
            public EquipmentCell[] Equipment;
            public SocketCell[] Sockets;
            public TypeCell[] Types;
            public StockCell[] Stocks;
            public int SlotIndex;
            public int SocketIndex;
            public int ColorIndex;
            public int StockIndex;
            public string SocketSummary;
            public bool CanRemove;
            public string DetailName;
            public string DetailEffect;
            public string DetailOwned;
            public string DetailTarget;
            public string EquipLabel;
            public bool CanEquip;
            public string CombineLabel;
            public bool CanCombine;
        }
    }
}
