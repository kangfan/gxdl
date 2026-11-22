using UnityEngine;
using UnityEngine.UI;

namespace Gxdl.Modules
{
    /// <summary>
    /// 战斗 HUD 视图。prefab 只提供静态布局，
    /// 血条、回合、速度、技能冷却等逐帧状态由战斗系统调用下方
    /// Set* 方法写入，View 不参与战斗推进。
    /// </summary>
    public sealed class BattleHudView : UiModule
    {
        public override string ModuleId => "BattleHudView";

        /// <summary>与原界面一致的满宽，换分辨率时血条按该基准换算比例。</summary>
        public const float PlayerHealthWidth = 270f;
        public const float EnemyHealthWidth = 550f;

        [Header("顶部导航")]
        [SerializeField] Text regionText;
        [SerializeField] Button exitButton;
        [SerializeField] Button speedButton;
        [SerializeField] Text speedText;

        [Header("敌方")]
        [SerializeField] Image enemyPortrait;
        [SerializeField] Text enemyNameText;
        [SerializeField] Text enemyHealthText;
        [SerializeField] Image enemyHealthBar;
        [SerializeField] Image enemyShieldBar;
        [SerializeField] Text enemyStatusText;

        [Header("回合与事件")]
        [SerializeField] Text roundText;
        [SerializeField] Text eventText;
        [SerializeField] Text warningText;

        [Header("我方")]
        [SerializeField] Image playerHealthBar;
        [SerializeField] Image playerShieldBar;
        [SerializeField] Text playerHealthText;
        [SerializeField] Text playerStatusText;

        [Header("技能")]
        [SerializeField] Button skillButton;
        [SerializeField] Image skillIcon;
        [SerializeField] Image cooldownRing;
        [SerializeField] Text skillNameText;
        [SerializeField] Text skillStateText;
        [SerializeField] Button autoSkillButton;
        [SerializeField] Image autoToggleTrack;
        [SerializeField] RectTransform autoToggleThumb;
        [SerializeField] Text autoToggleCaption;

        [Header("统计")]
        [SerializeField] Text statisticsText;

        public Button Exit => exitButton;

        public Button Speed => speedButton;

        public Button Skill => skillButton;

        public Button AutoSkill => autoSkillButton;

        // ---- 逐帧刷新接口：由战斗系统在每次事件后调用 ----

        /// <summary>我方血量文本与血条比例，maxHp 取 GuxuBattle.Player.hp。</summary>
        public void SetHealth(int current, int max)
        {
            if (playerHealthText != null) playerHealthText.text = current + " / " + max;
            SetFill(playerHealthBar, current, max);
        }

        /// <summary>敌方血量文本与血条比例。</summary>
        public void SetEnemyHealth(int current, int max)
        {
            if (enemyHealthText != null) enemyHealthText.text = current + " / " + max;
            SetFill(enemyHealthBar, current, max);
        }

        /// <summary>护盾按最大生命的比例绘制，与血条同一基准。</summary>
        public void SetShield(int shield, int maxHp)
        {
            SetWidth(playerShieldBar, shield, maxHp);
        }

        public void SetEnemyShield(int shield, int maxHp)
        {
            SetWidth(enemyShieldBar, shield, maxHp);
        }

        /// <summary>回合与波次，文案格式由战斗规则给出。</summary>
        public void SetRound(int round, int maxRounds, int wave, int waves)
        {
            if (roundText != null) roundText.text = "第 " + round + " / " + maxRounds + " 回合 · 第 " + wave + " / " + waves + " 波";
        }

        /// <summary>战斗速度按钮，文案直接反映当前倍率。</summary>
        public void SetSpeed(int multiplier)
        {
            if (speedText != null) speedText.text = "×" + multiplier + " 加速";
        }

        public void SetStatus(string player, string enemy)
        {
            if (playerStatusText != null) playerStatusText.text = player ?? string.Empty;
            if (enemyStatusText != null) enemyStatusText.text = enemy ?? string.Empty;
        }

        public void SetEvent(string value)
        {
            if (eventText != null) eventText.text = value ?? string.Empty;
        }

        /// <summary>Boss 预警独立一行，空串即隐藏语义。</summary>
        public void SetWarning(string value)
        {
            if (warningText != null) warningText.text = value ?? string.Empty;
        }

        /// <summary>顶部区域标题，例如「第3章 · 精英试炼」。</summary>
        public void SetRegion(string value)
        {
            if (regionText != null) regionText.text = value ?? string.Empty;
        }

        public void SetStatistics(int damageDone, int damageTaken, int healingDone)
        {
            if (statisticsText != null)
            {
                statisticsText.text = "输出 " + damageDone + "    承伤 " + damageTaken + "    治疗 " + healingDone;
            }
        }

        /// <summary>敌方立绘与名称由当前守护者决定。</summary>
        public void SetEnemyAppearance(Sprite portrait, Color tint, string name)
        {
            if (enemyPortrait != null)
            {
                enemyPortrait.sprite = portrait;
                enemyPortrait.color = portrait != null ? tint : new Color(0.08f, 0.24f, 0.23f);
            }

            if (enemyNameText != null) enemyNameText.text = name ?? string.Empty;
        }

        /// <summary>技能图标与名称，切换技能或进入战斗时调用。</summary>
        public void SetSkillAppearance(Sprite icon, string name)
        {
            if (skillIcon != null && icon != null) skillIcon.sprite = icon;
            if (skillNameText != null) skillNameText.text = name ?? string.Empty;
        }

        /// <summary>技能状态文案与可点击性：冷却中禁用，自动释放时也禁用。</summary>
        public void SetSkillState(string label, bool interactable, int cooldown, int cooldownTurns)
        {
            if (skillStateText != null) skillStateText.text = label ?? string.Empty;
            if (skillButton != null) skillButton.interactable = interactable;

            var cooling = cooldown > 0;
            if (cooldownRing != null)
            {
                cooldownRing.enabled = cooling && cooldownRing.sprite != null;
                cooldownRing.fillAmount = cooldown / (float)Mathf.Max(1, cooldownTurns - 1);
            }

            if (skillIcon != null) skillIcon.color = cooling ? new Color(0.58f, 0.66f, 0.67f) : Color.white;
        }

        /// <summary>自动技能开关的外观，轨道贴图缺失时仅移动滑块。</summary>
        public void SetAutoSkill(bool on, Sprite trackOn, Sprite trackOff)
        {
            if (autoToggleTrack != null) autoToggleTrack.sprite = on ? trackOn : trackOff;
            if (autoToggleThumb != null)
            {
                autoToggleThumb.anchoredPosition = new Vector2(on ? 47f : 7f, autoToggleThumb.anchoredPosition.y);
            }

            if (autoToggleCaption != null) autoToggleCaption.text = "自动技能";
        }

        /// <summary>血条用 Filled 模式，只改填充量，不改布局。</summary>
        static void SetFill(Image bar, int value, int max)
        {
            if (bar == null) return;
            bar.fillAmount = max <= 0 ? 0f : Mathf.Clamp01(value / (float)max);
        }

        /// <summary>护盾条按宽度伸缩，满宽基准存在高度字段里（生成器约定）。</summary>
        static void SetWidth(Image bar, int value, int max)
        {
            if (bar == null) return;
            var full = bar.rectTransform.sizeDelta.y;
            var ratio = max <= 0 ? 0f : Mathf.Clamp01(value / (float)max);
            bar.rectTransform.sizeDelta = new Vector2(full * ratio, full);
        }
    }
}
