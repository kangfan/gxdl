using UnityEngine;
using UnityEngine.UI;

namespace Gxdl.Modules
{
    /// <summary>
    /// 战前准备界面视图。只持有控件引用，
    /// 关卡、敌人、奖励、技能与评估数值一律由控制器按真实规则填入。
    /// </summary>
    public sealed class BattlePrepView : UiModule
    {
        public override string ModuleId => "BattlePrepView";

        [Header("标题与关闭")]
        [SerializeField] Text stageNameText;
        [SerializeField] Button closeButton;

        [Header("敌人信息")]
        [SerializeField] Text guardianText;
        [SerializeField] Text mechanicText;
        [SerializeField] Text waveText;
        [SerializeField] Text attackText;
        [SerializeField] Text hpText;
        [SerializeField] Text defenseText;

        [Header("奖励")]
        [SerializeField] Text rewardTitleText;
        [SerializeField] Text rewardLimitText;
        [SerializeField] GameObject[] rewardSlots;
        [SerializeField] Image[] rewardIcons;
        [SerializeField] Text[] rewardLabels;
        [SerializeField] Text[] rewardValues;
        [SerializeField] Text noRewardText;

        [Header("出战技能")]
        [SerializeField] Button[] skillButtons;
        [SerializeField] Image[] skillIcons;
        [SerializeField] GameObject[] skillChecks;
        [SerializeField] Text skillDetailText;
        [SerializeField] Button autoSkillButton;
        [SerializeField] Text skillModeText;

        [Header("评估")]
        [SerializeField] Text assessmentText;
        [SerializeField] Text powerText;
        [SerializeField] Text criticalText;
        [SerializeField] Text comboText;
        [SerializeField] Text counterText;

        [Header("底部动作")]
        [SerializeField] Button growthButton;
        [SerializeField] Button challengeButton;
        [SerializeField] Text challengeLabel;
        [SerializeField] GameObject challengeBlock;

        public Button Close => closeButton;

        public Button Growth => growthButton;

        public Button Challenge => challengeButton;

        public Button AutoSkill => autoSkillButton;

        /// <summary>四个技能位共用一套回调，索引即技能序号。</summary>
        public void BindSkillCallbacks(System.Action<int> handler)
        {
            if (skillButtons == null || handler == null) return;
            for (var i = 0; i < skillButtons.Length; i++)
            {
                if (skillButtons[i] == null) continue;
                var index = i;
                skillButtons[i].onClick.RemoveAllListeners();
                skillButtons[i].onClick.AddListener(() => handler(index));
            }
        }

        /// <summary>按真实关卡、配装与存档刷新整页静态信息。</summary>
        public void Apply(Model model)
        {
            if (stageNameText != null) stageNameText.text = model.StageName ?? string.Empty;
            if (guardianText != null) guardianText.text = model.Guardian ?? string.Empty;
            if (mechanicText != null) mechanicText.text = model.Mechanic ?? string.Empty;
            if (waveText != null) waveText.text = model.WaveLabel ?? string.Empty;
            if (attackText != null) attackText.text = model.EnemyAttack ?? string.Empty;
            if (hpText != null) hpText.text = model.EnemyHp ?? string.Empty;
            if (defenseText != null) defenseText.text = model.EnemyDefense ?? string.Empty;

            if (rewardTitleText != null) rewardTitleText.text = model.RewardTitle ?? string.Empty;
            if (rewardLimitText != null)
            {
                rewardLimitText.text = model.RewardLimit ?? string.Empty;
                rewardLimitText.gameObject.SetActive(!string.IsNullOrEmpty(model.RewardLimit));
            }

            ApplyRewards(model.Rewards);
            if (noRewardText != null) noRewardText.gameObject.SetActive(model.Rewards == null || model.Rewards.Length == 0);

            SetSkill(model.SelectedSkill);
            if (skillDetailText != null) skillDetailText.text = model.SkillDetail ?? string.Empty;
            SetSkillMode(model.AutoSkill);
            if (assessmentText != null) assessmentText.text = model.Assessment ?? string.Empty;

            if (powerText != null) powerText.text = model.Power ?? string.Empty;
            if (criticalText != null) criticalText.text = model.Critical ?? string.Empty;
            if (comboText != null) comboText.text = model.Combo ?? string.Empty;
            if (counterText != null) counterText.text = model.Counter ?? string.Empty;

            SetChallenge(model.CanChallenge, model.ChallengeLabel);
        }

        /// <summary>切换选中技能的高亮，不重建布局。</summary>
        public void SetSkill(int selected)
        {
            if (skillChecks == null) return;
            for (var i = 0; i < skillChecks.Length; i++)
            {
                if (skillChecks[i] != null) skillChecks[i].SetActive(i == selected);
            }

            if (skillIcons == null) return;
            for (var i = 0; i < skillIcons.Length; i++)
            {
                if (skillIcons[i] != null)
                {
                    skillIcons[i].color = i == selected
                        ? new Color(1f, 0.88f, 0.58f)
                        : new Color(0.67f, 0.96f, 1f);
                }
            }
        }

        /// <summary>自动/手动技能状态由控制器在切换后回填。</summary>
        public void SetSkillMode(bool auto)
        {
            if (skillModeText != null) skillModeText.text = auto ? "技能：自动释放（点击切换）" : "技能：手动释放（点击切换）";
        }

        /// <summary>挑战次数、解锁与按钮可用性由真实状态决定。</summary>
        public void SetChallenge(bool available, string label)
        {
            if (challengeButton != null) challengeButton.interactable = available;
            if (challengeLabel != null) challengeLabel.text = label ?? string.Empty;
            if (challengeBlock != null) challengeBlock.SetActive(!available);
        }

        /// <summary>奖励槽按实际产出逐个填入，多出的槽位隐藏。</summary>
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
                    rewardIcons[i].sprite = IconFor(cell.IconKind, cell.GemIndex);
                    rewardIcons[i].enabled = rewardIcons[i].sprite != null;
                }

                Set(rewardLabels, i, Combine(cell.Label, cell.Value));
            }
        }

        static Sprite IconFor(int kind, int gemIndex)
        {
            return kind < 0 ? GuxuItemIcons.Gem(gemIndex + 1) : GuxuUiArt.BattlePrepReward(kind);
        }

        /// <summary>战前奖励槽是单行文本：「破封符 +5」由名称与数量拼成。</summary>
        static string Combine(string label, string value)
        {
            if (string.IsNullOrEmpty(value)) return label;
            return label + " " + value;
        }

        static void Set(Text[] target, int index, string value)
        {
            if (target == null || index < 0 || index >= target.Length) return;
            if (target[index] != null) target[index].text = value ?? string.Empty;
        }

        /// <summary>一条奖励的展示内容；图标类型与结算页保持同一套编号。</summary>
        public struct RewardCell
        {
            public string Label;
            public string Value;
            public int IconKind;
            public int GemIndex;
        }

        public struct Model
        {
            public string StageName;
            public string Guardian;
            public string Mechanic;
            public string WaveLabel;
            public string EnemyAttack;
            public string EnemyHp;
            public string EnemyDefense;
            public string RewardTitle;
            public string RewardLimit;
            public RewardCell[] Rewards;
            public int SelectedSkill;
            public string SkillDetail;
            public bool AutoSkill;
            public string Assessment;
            public string Power;
            public string Critical;
            public string Combo;
            public string Counter;
            public bool CanChallenge;
            public string ChallengeLabel;
        }
    }
}
