using UnityEngine;
using UnityEngine.UI;

namespace Gxdl.Modules
{
    /// <summary>
    /// 战斗结算弹窗视图。胜败标题、奖励、建议与统计全部运行时填入，
    /// prefab 只提供固定的面板与槽位。
    /// </summary>
    public sealed class SettlementView : UiModule
    {
        public override string ModuleId => "SettlementView";

        [Header("标题")]
        [SerializeField] Text resultText;
        [SerializeField] Text stageText;
        [SerializeField] Button closeButton;
        [SerializeField] Button completeButton;

        [Header("内容")]
        [SerializeField] Text rewardHeadingText;
        [SerializeField] GameObject rewardBlock;
        [SerializeField] GameObject[] rewardSlots;
        [SerializeField] Image[] rewardIcons;
        [SerializeField] Text[] rewardNames;
        [SerializeField] Text[] rewardValues;
        [SerializeField] Text noRewardText;
        [SerializeField] GameObject defeatBlock;
        [SerializeField] Text defeatAdviceText;
        [SerializeField] Text unlockText;

        [Header("统计")]
        [SerializeField] Text[] statLabels;
        [SerializeField] Text[] statValues;

        [Header("底部动作")]
        [SerializeField] Button bonusButton;
        [SerializeField] Text bonusLabel;
        [SerializeField] Button challengeButton;
        [SerializeField] Text challengeLabel;
        [SerializeField] Button growthButton;
        [SerializeField] Button backButton;

        public Button Close => closeButton;

        public Button Challenge => challengeButton;

        public Button Growth => growthButton;

        public Button Back => backButton;

        public Button Bonus => bonusButton;

        /// <summary>按真实战斗与结算结果刷新整页。</summary>
        public void Apply(Model model)
        {
            var won = model.Won;
            if (resultText != null)
            {
                resultText.text = won ? "破 阵 凯 旋" : "重 整 行 装";
                resultText.color = won ? new Color(0.86f, 0.78f, 0.54f) : new Color(0.94f, 0.94f, 0.87f);
            }

            if (stageText != null) stageText.text = model.StageName ?? string.Empty;

            // 终局标记只在主线最后一关或古塔百层出现。
            if (completeButton != null)
            {
                completeButton.gameObject.SetActive(model.Finished);
                var label = completeButton.GetComponentInChildren<Text>();
                if (label != null) label.text = model.CompleteLabel ?? string.Empty;
            }

            if (rewardBlock != null) rewardBlock.SetActive(won);
            if (defeatBlock != null) defeatBlock.SetActive(!won);
            if (rewardHeadingText != null) rewardHeadingText.text = model.RewardHeading ?? string.Empty;
            ApplyRewards(model.Rewards);
            if (noRewardText != null) noRewardText.gameObject.SetActive(won && (model.Rewards == null || model.Rewards.Length == 0));
            if (defeatAdviceText != null) defeatAdviceText.text = model.DefeatAdvice ?? string.Empty;

            if (unlockText != null)
            {
                unlockText.text = model.Unlock ?? string.Empty;
                unlockText.gameObject.SetActive(!string.IsNullOrEmpty(model.Unlock));
            }

            SetStats(model.Round, model.DamageDone, model.DamageTaken, model.HealingDone);

            if (bonusButton != null)
            {
                var show = model.HasReceipt;
                bonusButton.gameObject.SetActive(show);
                if (show)
                {
                    bonusButton.interactable = !model.ReceiptDoubled;
                    if (bonusLabel != null)
                    {
                        bonusLabel.text = model.ReceiptDoubled ? "已领取双倍奖励" : "▷ 双倍奖励 · 点击即领";
                    }
                }
            }

            if (challengeButton != null)
            {
                challengeButton.gameObject.SetActive(!model.Finished);
                if (model.ChallengeLabel != null && challengeLabel != null) challengeLabel.text = model.ChallengeLabel;
            }

            if (backButton != null) backButton.gameObject.SetActive(model.Finished);
        }

        /// <summary>逐格填入奖励，列宽按结算真实条目数保持三列布局。</summary>
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

                Set(rewardNames, i, cell.Name);
                Set(rewardValues, i, cell.Value);
            }
        }

        public void SetStats(int round, int damageDone, int damageTaken, int healingDone)
        {
            Set(statValues, 0, round.ToString());
            Set(statValues, 1, damageDone.ToString());
            Set(statValues, 2, damageTaken.ToString());
            Set(statValues, 3, healingDone.ToString());
        }

        public void SetChallenge(bool interactable, string label)
        {
            if (challengeButton != null) challengeButton.interactable = interactable;
            if (challengeLabel != null && label != null) challengeLabel.text = label;
        }

        static void Set(Text[] target, int index, string value)
        {
            if (target == null || index < 0 || index >= target.Length) return;
            if (target[index] != null) target[index].text = value ?? string.Empty;
        }

        /// <summary>图标类型沿用 GuxuUiArt.BattlePrepReward 的编号，-1 表示灵玉。</summary>
        public struct RewardCell
        {
            public string Name;
            public string Value;
            public int IconKind;
            public int GemIndex;
        }

        public struct Model
        {
            public bool Won;
            public bool Finished;
            public string CompleteLabel;
            public string StageName;
            public string RewardHeading;
            public RewardCell[] Rewards;
            public string DefeatAdvice;
            public string Unlock;
            public int Round;
            public int DamageDone;
            public int DamageTaken;
            public int HealingDone;
            public bool HasReceipt;
            public bool ReceiptDoubled;
            public string ChallengeLabel;
        }
    }
}
