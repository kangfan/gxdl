using UnityEngine;
using UnityEngine.UI;

namespace Gxdl.Modules
{
    /// <summary>
    /// 锻造界面视图。材料、等级、加成与按钮状态全部由控制器按真实存档填入。
    /// </summary>
    public sealed class ForgeView : UiModule
    {
        public override string ModuleId => "ForgeView";

        [Header("顶栏")]
        [SerializeField] Button backButton;
        [SerializeField] Text oreBalance;
        [SerializeField] Text dustBalance;
        [SerializeField] Text hintText;

        [Header("装备槽")]
        [SerializeField] Transform slotRoot;
        [SerializeField] Image[] slotIcons;
        [SerializeField] Image[] slotFrames;
        [SerializeField] Text[] slotLevels;
        [SerializeField] Text[] slotMarks;
        [SerializeField] Button[] slotButtons;

        [Header("工作台")]
        [SerializeField] Image selectedIcon;
        [SerializeField] Text selectedName;
        [SerializeField] Text selectedTier;
        [SerializeField] Text selectedProgress;

        [Header("加成与消耗")]
        [SerializeField] Text bonusTitle;
        [SerializeField] Text[] bonusNames;
        [SerializeField] Text[] bonusCurrent;
        [SerializeField] Text[] bonusNext;
        [SerializeField] Text oreCost;
        [SerializeField] Text dustCost;
        [SerializeField] Button upgradeButton;
        [SerializeField] Text upgradeLabel;
        [SerializeField] Button getMoreButton;
        [SerializeField] Text summaryText;

        public Button Back => backButton;
        public Button GetMore => getMoreButton;

        public void BindCallbacks(System.Action<int> onSlot, System.Action onUpgrade)
        {
            if (slotButtons != null && onSlot != null)
            {
                for (var i = 0; i < slotButtons.Length; i++)
                {
                    var index = i;
                    if (slotButtons[i] == null) continue;
                    slotButtons[i].onClick.RemoveAllListeners();
                    slotButtons[i].onClick.AddListener(() => onSlot(index));
                }
            }

            if (upgradeButton != null && onUpgrade != null)
            {
                upgradeButton.onClick.RemoveAllListeners();
                upgradeButton.onClick.AddListener(() => onUpgrade());
            }
        }

        public void Apply(ForgeSnapshot snapshot)
        {
            if (oreBalance != null) oreBalance.text = snapshot.OreBalance;
            if (dustBalance != null) dustBalance.text = snapshot.DustBalance;
            if (hintText != null) hintText.text = snapshot.Hint;

            var count = slotLevels == null ? 0 : slotLevels.Length;
            for (var i = 0; i < count && snapshot.Slots != null && i < snapshot.Slots.Length; i++)
            {
                var slot = snapshot.Slots[i];
                Set(slotLevels, i, slot.Label);
                if (slotMarks != null && i < slotMarks.Length && slotMarks[i] != null)
                {
                    slotMarks[i].text = slot.Selected ? "◆" : string.Empty;
                }
                SetLevelColor(slotLevels, i, slot.Selected);
            }

            if (selectedName != null) selectedName.text = snapshot.SelectedName;
            if (selectedTier != null) selectedTier.text = snapshot.SelectedTier;
            if (selectedProgress != null) selectedProgress.text = snapshot.SelectedProgress;

            if (bonusTitle != null) bonusTitle.text = snapshot.BonusTitle;
            var statCount = bonusNames == null ? 0 : bonusNames.Length;
            for (var i = 0; i < statCount && snapshot.Bonus != null && i < snapshot.Bonus.Length; i++)
            {
                Set(bonusNames, i, snapshot.Bonus[i].Name);
                Set(bonusCurrent, i, snapshot.Bonus[i].Current);
                Set(bonusNext, i, snapshot.Bonus[i].Next);
            }

            if (oreCost != null) oreCost.text = snapshot.OreCost;
            if (dustCost != null) dustCost.text = snapshot.DustCost;
            if (upgradeLabel != null) upgradeLabel.text = snapshot.UpgradeLabel;
            if (upgradeButton != null) upgradeButton.interactable = snapshot.CanUpgrade;
            if (summaryText != null) summaryText.text = snapshot.Summary;
        }

        static void SetLevelColor(Text[] target, int index, bool selected)
        {
            if (target == null || index < 0 || index >= target.Length || target[index] == null) return;
            target[index].color = selected ? new Color(0.94f, 0.78f, 0.36f) : new Color(0.94f, 0.94f, 0.87f);
        }

        static void Set(Text[] target, int index, string value)
        {
            if (target == null || index < 0 || index >= target.Length || target[index] == null) return;
            target[index].text = value;
        }

        public struct SlotCell
        {
            public string Label;
            public bool Selected;
        }

        public struct BonusCell
        {
            public string Name;
            public string Current;
            public string Next;
        }

        public struct ForgeSnapshot
        {
            public string OreBalance;
            public string DustBalance;
            public string Hint;
            public SlotCell[] Slots;
            public string SelectedName;
            public string SelectedTier;
            public string SelectedProgress;
            public string BonusTitle;
            public BonusCell[] Bonus;
            public string OreCost;
            public string DustCost;
            public string UpgradeLabel;
            public bool CanUpgrade;
            public string Summary;
        }
    }
}
