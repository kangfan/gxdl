using Gxdl;

using UnityEngine;
using UnityEngine.UI;

namespace Gxdl.Modules
{
    /// <summary>
    /// 主界面视图。全部结构（背景、角色层、顶栏、侧边入口、破封组、任务条、遗宝决策、底部导航）
    /// 都在 HomeView.prefab 内；这里只负责把控制器算好的快照填进节点，并把点击转给控制器。
    /// 数值一律由控制器从 GameRules 取好再传进来，View 不做业务判断。
    /// </summary>
    public sealed class HomeView : UiModule
    {
        public override string ModuleId => "HomeView";

        [Header("背景与角色层")]
        [SerializeField] Image background;
        [SerializeField] Image seeker;
        [SerializeField] Image stone;
        [SerializeField] Image pendingRelic;
        [SerializeField] RectTransform companion;

        [Header("顶栏")]
        [SerializeField] Button portraitButton;
        [SerializeField] Text identityText;
        [SerializeField] Text powerText;
        [SerializeField] Text realmText;
        [SerializeField] Image sealTokenIcon;
        [SerializeField] Text sealTokenText;
        [SerializeField] Image spiritStoneIcon;
        [SerializeField] Text spiritStoneText;
        [SerializeField] Button cultivationButton;

        [Header("侧边入口")]
        [SerializeField] RectTransform accountEntry;
        [SerializeField] Button accountButton;
        [SerializeField] Button shopButton;
        [SerializeField] Button appraisalButton;
        [SerializeField] Button dungeonButton;
        [SerializeField] Button bagButton;
        [SerializeField] Image shopArt;
        [SerializeField] Image appraisalArt;
        [SerializeField] Image dungeonArt;
        [SerializeField] Image bagArt;
        [SerializeField] Image accountIcon;

        [Header("破封区")]
        [SerializeField] Button unsealButton;
        [SerializeField] Image unsealArt;
        [SerializeField] Text unsealLabel;
        [SerializeField] Image sealCostIcon;
        [SerializeField] Text sealCostText;
        [SerializeField] Button autoButton;
        [SerializeField] Image autoTrack;
        [SerializeField] RectTransform autoThumb;
        [SerializeField] Text autoLabel;
        [SerializeField] Button retentionButton;

        [Header("主线任务")]
        [SerializeField] Button questButton;
        [SerializeField] Text questTitle;
        [SerializeField] Text questTarget;
        [SerializeField] Image questUnderline;
        [SerializeField] Text questProgress;

        [Header("遗宝决策")]
        [SerializeField] RectTransform relicPanel;
        [SerializeField] Image relicIcon;
        [SerializeField] Text relicName;
        [SerializeField] Text relicDelta;
        [SerializeField] Text relicAffix;
        [SerializeField] Button relicDismantle;
        [SerializeField] Button relicEquip;

        [Header("底部导航")]
        [SerializeField] Button[] navButtons;
        [SerializeField] Image navPanel;
        [SerializeField] Image[] navIcons;

        public Button Portrait => portraitButton;
        public Button Cultivation => cultivationButton;
        public Button Account => accountButton;
        public Button Shop => shopButton;
        public Button Appraisal => appraisalButton;
        public Button Dungeon => dungeonButton;
        public Button Bag => bagButton;
        public Button Unseal => unsealButton;
        public Button Auto => autoButton;
        public Button Retention => retentionButton;
        public Button Quest => questButton;
        public Button RelicEquip => relicEquip;
        public Button RelicDismantle => relicDismantle;
        public Image Seeker => seeker;
        public Image Stone => stone;
        public RectTransform Companion => companion;
        public Text IdentityText => identityText;
        public Text PowerText => powerText;
        public Text SealTokenText => sealTokenText;
        public Image SpiritStoneIcon => spiritStoneIcon;
        public Text SpiritStoneText => spiritStoneText;
        public Text SealCostText => sealCostText;
        public Text AutoLabel => autoLabel;
        public Image AutoTrack => autoTrack;
        public RectTransform AutoThumb => autoThumb;
        public Text QuestTitle => questTitle;
        public Text QuestTarget => questTarget;
        public Text QuestProgress => questProgress;
        public Image QuestUnderline => questUnderline;

        /// <summary>旧版 prefab 里的字段可能缺失，控制器用它判断能否走 prefab 路径。</summary>
        public bool IsComplete =>
            background != null && seeker != null && stone != null &&
            unsealButton != null && autoButton != null && questButton != null &&
            navButtons != null && navButtons.Length == 5;

        /// <summary>底部五个导航格统一回调，索引即页签序号。</summary>
        public void BindNavigation(System.Action<int> handler)
        {
            if (navButtons == null || handler == null) return;
            for (var i = 0; i < navButtons.Length; i++)
            {
                if (navButtons[i] == null) continue;
                var index = i;
                navButtons[i].onClick.RemoveAllListeners();
                navButtons[i].onClick.AddListener(() => handler(index));
            }
        }

        /// <summary>统一挂侧边入口、破封区、任务条与决策面板的回调。</summary>
        public void BindActions(
            System.Action portrait,
            System.Action cultivation,
            System.Action account,
            System.Action shop,
            System.Action appraisal,
            System.Action dungeon,
            System.Action bag,
            System.Action unseal,
            System.Action auto,
            System.Action retention,
            System.Action quest,
            System.Action dismantle,
            System.Action equip)
        {
            Bind(portraitButton, portrait);
            Bind(cultivationButton, cultivation);
            Bind(accountButton, account);
            Bind(shopButton, shop);
            Bind(appraisalButton, appraisal);
            Bind(dungeonButton, dungeon);
            Bind(bagButton, bag);
            Bind(unsealButton, unseal);
            Bind(autoButton, auto);
            Bind(retentionButton, retention);
            Bind(questButton, quest);
            Bind(relicDismantle, dismantle);
            Bind(relicEquip, equip);
        }

        /// <summary>按真实存档刷新整页。</summary>
        public void Apply(HomeSnapshot snapshot)
        {
            ApplyArt(snapshot);

            if (identityText != null) identityText.text = snapshot.Identity;
            if (powerText != null) powerText.text = snapshot.Power;
            if (realmText != null) realmText.text = snapshot.Realm;
            if (sealTokenText != null) sealTokenText.text = snapshot.SealTokens;
            if (spiritStoneText != null) spiritStoneText.text = snapshot.SpiritStones;

            ApplySeal(snapshot);
            ApplyCharacter(snapshot);
            ApplyQuest(snapshot);
            ApplyRelic(snapshot);
        }

        /// <summary>静态图的资源绑定：换美术只需替换同路径贴图，不改结构。</summary>
        void ApplyArt(HomeSnapshot snapshot)
        {
            if (background != null)
            {
                background.sprite = GuxuUiArt.Optional("home-bg") ?? GuxuUiArt.Optional("screen-home");
                background.color = Color.white;
                GuxuParticleFx.Sort(background.transform, -40);
            }

            if (sealTokenIcon != null) sealTokenIcon.sprite = GuxuUiArt.Navigation(GuxuUiArt.Icon.SealToken);
            if (spiritStoneIcon != null) spiritStoneIcon.sprite = GuxuUiArt.Navigation(GuxuUiArt.Icon.SpiritDust);
            if (sealCostIcon != null) sealCostIcon.sprite = GuxuUiArt.Navigation(GuxuUiArt.Icon.SealToken);

            SetEntryArt(appraisalArt, "home-entry-appraisal", GuxuUiArt.Icon.Codex);
            SetEntryArt(shopArt, "home-entry-shop", GuxuUiArt.Icon.Sect);
            SetEntryArt(dungeonArt, "home-entry-dungeons", GuxuUiArt.Icon.Explore);
            SetEntryArt(bagArt, "home-entry-bag", GuxuUiArt.Icon.Inventory);
            if (accountIcon != null)
            {
                accountIcon.sprite = GuxuUiArt.Navigation(GuxuUiArt.Icon.Codex);
                accountIcon.gameObject.SetActive(true);
            }
            if (accountEntry != null)
            {
                accountEntry.gameObject.SetActive(snapshot.ShowAccount);
                var accountArt = accountEntry.Find("Art");
                if (accountArt != null) accountArt.gameObject.SetActive(false);
            }

            if (unsealArt != null)
            {
                var sprite = unsealArt.sprite != null ? unsealArt.sprite : GuxuUiArt.Optional("hud-unseal-button", true);
                unsealArt.sprite = sprite;
                unsealArt.type = sprite != null && sprite.border.sqrMagnitude > 0 ? Image.Type.Sliced : Image.Type.Simple;
                unsealArt.pixelsPerUnitMultiplier = 0.8f;
                unsealArt.enabled = sprite != null;
            }

            if (navPanel != null && navPanel.sprite == null)
            {
                var sprite = GuxuUiArt.Optional("ui23-panel-dark", true);
                navPanel.sprite = sprite;
                navPanel.type = Image.Type.Sliced;
                navPanel.pixelsPerUnitMultiplier = 1.4f;
                navPanel.color = sprite != null ? Color.white : new Color(0.055f, 0.15f, 0.2f, 0.95f);
            }

            var icons = new[]
            {
                GuxuUiArt.Icon.Sect, GuxuUiArt.Icon.Cultivation, GuxuUiArt.Icon.Ruins,
                GuxuUiArt.Icon.Codex, GuxuUiArt.Icon.Companion
            };
            if (navIcons != null)
            {
                for (var i = 0; i < navIcons.Length && i < icons.Length; i++)
                {
                    if (navIcons[i] != null) navIcons[i].sprite = GuxuUiArt.Navigation(icons[i]);
                }
            }

            var portrait = portraitButton != null ? portraitButton.targetGraphic as Image : null;
            if (portrait != null && portrait.sprite == null) portrait.sprite = GuxuUiArt.Portrait;
        }

        static void SetEntryArt(Image art, string key, GuxuUiArt.Icon fallback)
        {
            if (art == null) return;
            var iconNode = art.transform.parent != null ? art.transform.parent.Find("Icon") : null;
            var icon = iconNode != null ? iconNode.GetComponent<Image>() : null;
            var sprite = art.sprite != null ? art.sprite : GuxuUiArt.Optional(key);
            if (sprite != null)
            {
                art.sprite = sprite;
                art.preserveAspect = true;
                art.enabled = true;
                if (icon != null) icon.gameObject.SetActive(false);
                return;
            }

            // 没有专用圆环图时退回到图集图标，避免留下空白的白色方块。
            art.enabled = false;
            if (icon != null)
            {
                icon.sprite = GuxuUiArt.Navigation(fallback);
                icon.enabled = true;
                icon.gameObject.SetActive(true);
            }
        }

        internal void ApplySeal(HomeSnapshot snapshot)
        {
            if (unsealButton != null) unsealButton.interactable = snapshot.CanUnseal;
            if (unsealLabel != null)
            {
                unsealLabel.text = snapshot.UnsealLabel ?? string.Empty;
                unsealLabel.color = snapshot.HasTokens ? PaperInk : WarnOrange;
            }

            if (sealCostText != null)
            {
                sealCostText.text = snapshot.SealCostLabel ?? string.Empty;
                sealCostText.color = snapshot.HasTokens ? Ink : WarnOrange;
            }

            // 自动破封在开关开启时可点，否则要求满足发起条件。
            if (autoButton != null) autoButton.interactable = snapshot.Auto || snapshot.CanUnseal;
            if (autoLabel != null) autoLabel.text = snapshot.Auto ? "停止自动" : "自动破封";
            if (autoTrack != null)
            {
                autoTrack.sprite = GuxuUiArt.Optional(snapshot.Auto ? "hud-toggle-on" : "hud-toggle-off");
                autoTrack.color = Color.white;
            }
            if (autoThumb != null && autoTrack != null)
            {
                var track = autoTrack.rectTransform;
                var travel = Mathf.Max(0f, track.rect.width - autoThumb.rect.width);
                var inset = Mathf.Min(track.rect.height / 8f, travel / 2f);
                var x = snapshot.Auto ? travel - inset : inset;
                var y = (autoThumb.rect.height - track.rect.height) * .5f;
                autoThumb.anchoredPosition = track.anchoredPosition + new Vector2(x, y);
            }
            if (retentionButton != null) retentionButton.interactable = snapshot.Interactable;
        }

        /// <summary>
        /// 角色区：只切换封石、待处理遗宝的显隐与占位伙伴。
        /// 立绘、浮云、粒子等动态表现由控制器挂在同一批节点上。
        /// </summary>
        void ApplyCharacter(HomeSnapshot snapshot)
        {
            if (stone != null) stone.enabled = !snapshot.HasPendingRelic && snapshot.Interactable && stone.sprite != null;
            if (pendingRelic != null)
            {
                pendingRelic.sprite = snapshot.HasPendingRelic ? GuxuUiArt.Item(snapshot.Relic.Slot) : null;
                pendingRelic.gameObject.SetActive(snapshot.HasPendingRelic);
            }
        }

        void ApplyQuest(HomeSnapshot snapshot)
        {
            var visible = !snapshot.HasPendingRelic && snapshot.ShowQuest;
            if (questButton != null) questButton.gameObject.SetActive(visible);
            if (!visible) return;

            if (questTitle != null) questTitle.text = snapshot.QuestTitle ?? string.Empty;
            if (questTarget != null) questTarget.text = snapshot.QuestTarget ?? string.Empty;
            if (questProgress != null) questProgress.text = snapshot.QuestProgress ?? string.Empty;
            if (questUnderline != null)
            {
                // 下划线跟随目标文字宽度。
                var width = questTarget != null ? Mathf.Clamp(questTarget.preferredWidth, 0f, 620f) : 620f;
                questUnderline.rectTransform.sizeDelta = new Vector2(width, 2f);
            }
        }

        /// <summary>
        /// 遗宝决策面板：存在待处理遗宝时显示，
        /// 名字、战力增减与词条对比均由控制器从 GameRules 计算后填入。
        /// </summary>
        void ApplyRelic(HomeSnapshot snapshot)
        {
            if (relicPanel == null) return;
            var visible = snapshot.HasPendingRelic;
            relicPanel.gameObject.SetActive(visible);
            if (!visible) return;

            var relic = snapshot.Relic;
            if (relicIcon != null) relicIcon.sprite = GuxuUiArt.Item(relic.Slot);
            if (relicName != null) relicName.text = relic.Name ?? string.Empty;
            if (relicDelta != null)
            {
                relicDelta.text = relic.DeltaLabel ?? string.Empty;
                relicDelta.color = relic.Delta < 0 ? WarnOrange : RelicBrown;
            }

            if (relicAffix != null) relicAffix.text = relic.AffixComparison ?? string.Empty;
            if (relicDismantle != null) relicDismantle.interactable = snapshot.Interactable;
            if (relicEquip != null) relicEquip.interactable = snapshot.Interactable;
        }

        static void Bind(Button button, System.Action handler)
        {
            if (button == null) return;
            button.onClick.RemoveAllListeners();
            if (handler != null) button.onClick.AddListener(() => handler());
        }

        // 与 prefab 生成器同一套配色。
        static readonly Color Ink = new Color(0.16f, 0.13f, 0.09f);
        static readonly Color PaperInk = new Color(0.25f, 0.14f, 0.05f);
        static readonly Color WarnOrange = new Color(1f, 0.63f, 0.4f);
        // 遗宝战力正向增量的暖棕，与弹窗效果图一致。
        static readonly Color RelicBrown = new Color(0.40f, 0.25f, 0.09f);

        /// <summary>遗宝面板条目。禁止在这里写演示数值。</summary>
        public struct RelicCell
        {
            public int Slot;
            public string Name;
            public int Delta;
            public string DeltaLabel;
            public string AffixComparison;

            /// <summary>从真实存档组装，空档位视为无遗宝。</summary>
            public static RelicCell From(GameRules rules)
            {
                var cell = new RelicCell { Slot = -1 };
                var pending = rules != null ? rules.Save.pending : null;
                if (pending == null) return cell;

                cell.Slot = pending.slot;
                cell.Name = rules.GearName(pending) + " · " + GuxuUiArt.TierLabel(pending.quality, pending.level);
                cell.Delta = rules.AttributesWithGear(pending).Power - rules.Attributes.Power;
                cell.DeltaLabel = cell.Delta == 0
                    ? "基础战力不变"
                    : "基础战力 " + (cell.Delta > 0 ? "↑ +" : "↓ ") + cell.Delta;
                cell.AffixComparison = "攻击力 +" + pending.attack + "   生命值 +" + pending.hp + "\n" +
                    GameRules.GearAffix(rules.Save.gear[pending.slot]) + " → " + GameRules.GearAffix(pending);
                return cell;
            }
        }

        /// <summary>主界面快照。文本已由 Capture 按 Rules 组装，View 只负责显示。</summary>
        public struct HomeSnapshot
        {
            public string Identity;
            public string Power;
            public string Realm;
            public string SealTokens;
            public string SpiritStones;
            public string UnsealLabel;
            public string SealCostLabel;
            public bool HasTokens;
            public bool CanUnseal;
            public bool Auto;
            public bool Interactable;
            public bool ShowAccount;
            public bool HasPendingRelic;
            public bool ShowQuest;
            public string QuestTitle;
            public string QuestTarget;
            public string QuestProgress;
            public RelicCell Relic;

            /// <summary>从真实存档组装一份完整快照。Busy 由控制器传入，View 不感知流程状态。</summary>
            public static HomeSnapshot Capture(GameRules rules, string nickname, bool auto, bool busy, bool showAccount)
            {
                var snapshot = CaptureSeal(rules, auto, busy);
                if (rules == null) return snapshot;

                var save = rules.Save;
                snapshot.Identity = "等级" + save.level + "  " + (string.IsNullOrWhiteSpace(nickname) ? "寻墟者" : nickname);
                snapshot.Power = "基础战力  " + rules.Attributes.Power;
                snapshot.Realm = rules.Realm.displayName;
                snapshot.SealTokens = save.peaches.ToString();
                snapshot.SpiritStones = save.coins.ToString();

                snapshot.ShowAccount = showAccount;

                // 主线任务条只在无遗宝时出现；关卡数据来自真实 Stage 配置。
                snapshot.ShowQuest = !snapshot.HasPendingRelic;
                if (snapshot.ShowQuest)
                {
                    var config = GuxuStageConfig.Load();
                    var complete = save.stage > config.stages.Length;
                    var stage = config.ForProgress(save.stage);
                    snapshot.QuestTitle = complete ? "主线 · 已通关" : "主线 · " + stage.regionName;
                    snapshot.QuestTarget = complete ? "重访遗域 ›" : "前往 " + stage.regionName + " ›";
                    snapshot.QuestProgress = complete
                        ? "主线已通关 · 可继续挑战古塔与秘境"
                        : "目标：" + stage.guardianName + " · 第 " + stage.id + " / " + config.stages.Length + " 关";
                }

                snapshot.Relic = RelicCell.From(rules);
                return snapshot;
            }

            internal static HomeSnapshot CaptureSeal(GameRules rules, bool auto, bool busy)
            {
                if (rules == null) return default;
                var hasTokens = rules.Save.peaches > 0;
                var hasPendingRelic = rules.Save.pending != null;
                return new HomeSnapshot
                {
                    HasTokens = hasTokens,
                    HasPendingRelic = hasPendingRelic,
                    CanUnseal = hasTokens && !hasPendingRelic && !busy,
                    UnsealLabel = hasTokens ? "破  封" : "破封符不足",
                    SealCostLabel = hasTokens ? "消耗 ×1" : "消耗 ×1 · 不足",
                    Auto = auto,
                    Interactable = !busy,
                };
            }
        }
    }
}
