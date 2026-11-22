using UnityEngine;
using UnityEngine.UI;

namespace Gxdl.Modules
{
    /// <summary>
    /// 战备装备界面视图。技能选择、装备状态与自动模式由控制器按真实存档填入。
    /// </summary>
    public sealed class LoadoutView : UiModule
    {
        public override string ModuleId => "LoadoutView";

        [Header("顶栏")]
        [SerializeField] Button backButton;

        [Header("技能卡")]
        [SerializeField] Transform skillRoot;
        [SerializeField] Image[] skillIcons;
        [SerializeField] Image[] skillRings;
        [SerializeField] Text[] skillNames;
        [SerializeField] Image[] skillEquippedFlags;
        [SerializeField] Button[] skillButtons;

        [Header("选中详情")]
        [SerializeField] Image selectedIcon;
        [SerializeField] Text selectedName;
        [SerializeField] Text selectedDetail;
        [SerializeField] Button equipButton;
        [SerializeField] Text equipLabel;

        [Header("自动模式")]
        [SerializeField] Button autoButton;
        [SerializeField] Text autoLabel;
        [SerializeField] Text autoDetail;

        [Header("底部")]
        [SerializeField] Button returnButton;
        [SerializeField] Button challengeButton;

        public Button Back => backButton;
        public Button Return => returnButton;
        public Button Challenge => challengeButton;

        public void BindCallbacks(
            System.Action<int> onSelect,
            System.Action onEquip,
            System.Action onAuto,
            System.Action onChallenge)
        {
            Wire(skillButtons, onSelect);
            Wire(equipButton, onEquip);
            Wire(autoButton, onAuto);
            Wire(challengeButton, onChallenge);
        }

        public void Apply(LoadoutSnapshot snapshot)
        {
            var selected = Mathf.Clamp(snapshot.SelectedIndex, 0, 3);
            var count = skillNames == null ? 0 : skillNames.Length;
            for (var i = 0; i < count && snapshot.Skills != null && i < snapshot.Skills.Length; i++)
            {
                var card = snapshot.Skills[i];
                Set(skillNames, i, card.Name);
                Select(skillRings, i, i == selected);
                Select(skillEquippedFlags, i, card.Equipped);
            }

            var shown = snapshot.Skills != null && snapshot.Skills.Length > selected
                ? snapshot.Skills[selected]
                : default(SkillCell);
            if (selectedName != null) selectedName.text = shown.Name;
            if (selectedDetail != null) selectedDetail.text = shown.Detail;
            if (equipLabel != null) equipLabel.text = snapshot.EquipLabel;
            if (equipButton != null) equipButton.interactable = snapshot.CanEquip;

            if (autoLabel != null) autoLabel.text = snapshot.AutoLabel;
            if (autoDetail != null) autoDetail.text = snapshot.AutoDetail;
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

        public struct SkillCell
        {
            public string Name;
            public string Detail;
            public bool Equipped;
        }

        public struct LoadoutSnapshot
        {
            public SkillCell[] Skills;
            public int SelectedIndex;
            public string EquipLabel;
            public bool CanEquip;
            public string AutoLabel;
            public string AutoDetail;
        }
    }
}
