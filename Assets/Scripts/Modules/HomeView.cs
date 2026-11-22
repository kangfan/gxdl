using Gxdl;

using UnityEngine;
using UnityEngine.UI;

namespace Gxdl.Modules
{
    /// <summary>
    /// 主界面视图。静态骨架（顶栏、侧边入口、破封按钮组、底部导航）来自 prefab；
    /// 角色立绘、遗宝决策面板、主线任务条由 Apply 在运行时创建，
    /// 因为它们的显隐与子节点完全取决于当前存档。
    /// 数值一律由控制器从 GameRules 取好再传进来，View 不做业务判断。
    /// </summary>
    public sealed class HomeView : UiModule
    {
        public override string ModuleId => "HomeView";

        [Header("顶栏")]
        [SerializeField] Button portraitButton;
        [SerializeField] Text identityText;
        [SerializeField] Text powerText;
        [SerializeField] Text realmText;
        [SerializeField] Text sealTokenText;
        [SerializeField] Text spiritStoneText;
        [SerializeField] Button cultivationButton;

        [Header("侧边入口")]
        [SerializeField] Button appraisalButton;
        [SerializeField] Button dungeonButton;
        [SerializeField] Button bagButton;

        [Header("破封区")]
        [SerializeField] Button unsealButton;
        [SerializeField] Text unsealLabel;
        [SerializeField] Text sealCostText;
        [SerializeField] Button autoButton;
        [SerializeField] Text autoLabel;
        [SerializeField] Button retentionButton;

        [Header("底部导航")]
        [SerializeField] Button[] navButtons;

        [Header("动态层")]
        [SerializeField] RectTransform dynamicLayer;

        // 动态创建的子节点缓存在这里，避免每次 Apply 重复建。
        Image seekerImage;
        Image stoneImage;
        RectTransform companionRoot;
        RectTransform questStrip;
        Text questTitle;
        Text questTarget;
        Text questProgress;
        Image questUnderline;
        Button questButton;
        RectTransform relicPanel;
        Image relicIcon;
        Text relicName;
        Text relicDelta;
        Text relicAffix;
        Button relicDismantle;
        Button relicEquip;

        public Button Portrait => portraitButton;

        public Button Cultivation => cultivationButton;

        public Button Appraisal => appraisalButton;

        public Button Dungeon => dungeonButton;

        public Button Bag => bagButton;

        public Button Unseal => unsealButton;

        public Button Auto => autoButton;

        public Button Retention => retentionButton;

        public Button Quest => questButton;

        public Button RelicEquip => relicEquip;

        public Button RelicDismantle => relicDismantle;

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

        /// <summary>按真实存档刷新整页。</summary>
        public void Apply(HomeSnapshot snapshot)
        {
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

        void ApplySeal(HomeSnapshot snapshot)
        {
            if (unsealButton != null) unsealButton.interactable = snapshot.CanUnseal;
            if (unsealLabel != null)
            {
                unsealLabel.text = snapshot.UnsealLabel ?? string.Empty;
                unsealLabel.color = snapshot.HasTokens ? PaperInk : Cream;
            }

            if (sealCostText != null)
            {
                sealCostText.text = snapshot.SealCostLabel ?? string.Empty;
                sealCostText.color = snapshot.HasTokens ? Cream : WarnOrange;
            }

            // 自动破封在开关开启时可点，否则要求满足发起条件。
            if (autoButton != null) autoButton.interactable = snapshot.Auto || snapshot.CanUnseal;
            if (autoLabel != null) autoLabel.text = snapshot.Auto ? "停止自动" : "自动破封";
            if (retentionButton != null) retentionButton.interactable = snapshot.Interactable;
        }

        /// <summary>
        /// 角色区：首次调用按旧 Home() 的坐标创建立绘、封石与占位伙伴节点，
        /// 之后只切换封石的显隐（有遗宝时不悬空显示）。
        /// </summary>
        void ApplyCharacter(HomeSnapshot snapshot)
        {
            if (dynamicLayer == null) return;
            if (seekerImage == null)
            {
                seekerImage = SpriteChild("Seeker Character Layer", -25f, 513f, 400f, 527f, "Game/Guxu/Final/Hero/hero");
                stoneImage = SpriteChild("Sealed Stone Layer", 425f, 505f, 190f, 210f, "Game/Guxu/Final/stone");

                companionRoot = Node("Companion A Zhuan", 515f, 390f, 145f, 125f);
                if (companionRoot.GetComponent<GuxuCompanionMotion>() == null)
                {
                    companionRoot.gameObject.AddComponent<GuxuCompanionMotion>();
                }

                if (dynamicLayer.GetComponent<GuxuFinalAtmosphere>() == null)
                {
                    dynamicLayer.gameObject.AddComponent<GuxuFinalAtmosphere>();
                }
            }

            if (stoneImage != null) stoneImage.enabled = snapshot.HasTokens && !snapshot.HasPendingRelic;
        }

        /// <summary>
        /// 主线任务条：无待处理遗宝时显示。旧实现在 Home() 里按关卡生成三类文本，
        /// 这里改为一次建节点、后续只换文案，避免每次进主页重建。
        /// </summary>
        void ApplyQuest(HomeSnapshot snapshot)
        {
            if (dynamicLayer == null) return;
            if (questStrip == null)
            {
                questStrip = Node("Go Quest Target", 44f, 900f, 662f, 120f);
                var surface = questStrip.gameObject.AddComponent<Image>();
                surface.color = new Color(0.06f, 0.14f, 0.18f, 0.85f);
                surface.raycastTarget = true;
                questButton = questStrip.gameObject.AddComponent<Button>();
                questButton.targetGraphic = surface;

                questTitle = Label(questStrip, "Main Quest Title", 0f, 4f, 620f, 40f, 28, Gold);
                questTarget = Label(questStrip, "Main Quest Target", 0f, 40f, 620f, 40f, 26, Jade);
                questUnderline = Line(questStrip, "Main Quest Underline", 0f, 82f, 620f, 2f, Jade);
                questProgress = Label(questStrip, "Main Quest Progress", 0f, 78f, 640f, 36f, 22, Cream);
            }

            var visible = !snapshot.HasPendingRelic && snapshot.ShowQuest;
            questStrip.gameObject.SetActive(visible);
            if (!visible) return;

            if (questTitle != null) questTitle.text = snapshot.QuestTitle ?? string.Empty;
            if (questTarget != null) questTarget.text = snapshot.QuestTarget ?? string.Empty;
            if (questProgress != null) questProgress.text = snapshot.QuestProgress ?? string.Empty;
            if (questUnderline != null)
            {
                // 下划线跟随目标文字宽度，与旧实现一致。
                var width = questTarget != null ? Mathf.Clamp(questTarget.preferredWidth, 0f, 620f) : 620f;
                questUnderline.rectTransform.sizeDelta = new Vector2(width, 2f);
            }
        }

        /// <summary>
        /// 遗宝决策面板：存在待处理遗宝时创建并显示，
        /// 名字、战力增减与词条对比均由控制器从 GameRules 计算后填入。
        /// </summary>
        void ApplyRelic(HomeSnapshot snapshot)
        {
            if (dynamicLayer == null) return;
            if (!snapshot.HasPendingRelic)
            {
                if (relicPanel != null) relicPanel.gameObject.SetActive(false);
                return;
            }

            if (relicPanel == null)
            {
                relicPanel = Node("Relic Decision", 15f, 983f, 720f, 224f);
                var surface = relicPanel.gameObject.AddComponent<Image>();
                surface.color = new Color(0.13f, 0.12f, 0.1f, 0.97f);
                surface.raycastTarget = true;

                relicIcon = SpriteChildOf(relicPanel, "Relic Icon", 20f, 36f, 118f, 143f);
                relicName = Label(relicPanel, "Relic Name", 158f, 25f, 350f, 38f, 26, PaperInk);
                relicDelta = Label(relicPanel, "Relic Delta", 158f, 71f, 350f, 42f, 29, PaperInk);
                relicAffix = Label(relicPanel, "Relic Affix", 158f, 126f, 350f, 68f, 22, PaperInk);
                relicAffix.lineSpacing = 1.1f;

                relicDismantle = TextButton(relicPanel, "Resolve Equipment", 535f, 35f, 160f, 65f, "分解", false);
                relicEquip = TextButton(relicPanel, "Equip Relic", 535f, 125f, 160f, 65f, "装备", true);
                if (relicDismantle != null) relicDismantle.name = "Resolve Equipment";
                if (relicEquip != null) relicEquip.name = "Equip Relic";
            }

            // 遗宝未处理时封石先让位，避免与决策面板叠在一起。
            if (stoneImage != null) stoneImage.enabled = false;
            relicPanel.gameObject.SetActive(true);

            var relic = snapshot.Relic;
            if (relicIcon != null) relicIcon.sprite = GuxuUiArt.Item(relic.Slot);
            if (relicName != null) relicName.text = relic.Name ?? string.Empty;
            if (relicDelta != null)
            {
                relicDelta.text = relic.DeltaLabel ?? string.Empty;
                relicDelta.color = relic.Delta < 0 ? WarnOrange : PaperInk;
            }

            if (relicAffix != null) relicAffix.text = relic.AffixComparison ?? string.Empty;
            if (relicDismantle != null) relicDismantle.interactable = snapshot.Interactable;
            if (relicEquip != null) relicEquip.interactable = snapshot.Interactable;
        }

        /// <summary>统一挂侧边入口、破封区与决策面板的回调；未绑定的按钮保持无响应。</summary>
        public void BindActions(
            System.Action portrait,
            System.Action cultivation,
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
            Bind(appraisalButton, appraisal);
            Bind(dungeonButton, dungeon);
            Bind(bagButton, bag);
            Bind(unsealButton, unseal);
            Bind(autoButton, auto);
            Bind(retentionButton, retention);
            // 任务条和决策面板的按钮是运行后才创建的，统一在这里改挂监听。
            BindRuntime(questButton, quest);
            BindRuntime(relicDismantle, dismantle);
            BindRuntime(relicEquip, equip);
        }

        // —— 动态节点构建，坐标沿用旧 Home() 的设计值 ——

        RectTransform Node(string childName, float x, float y, float width, float height)
        {
            return NodeUnder(dynamicLayer, childName, x, y, width, height);
        }

        static RectTransform NodeUnder(RectTransform parent, string childName, float x, float y, float width, float height)
        {
            var go = new GameObject(childName, typeof(RectTransform));
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(x, -y);
            rect.sizeDelta = new Vector2(width, height);
            return rect;
        }

        Image SpriteChild(string childName, float x, float y, float width, float height, string resourcePath)
        {
            var image = SpriteChildOf(dynamicLayer, childName, x, y, width, height);
            if (image != null)
            {
                image.sprite = LoadSprite(resourcePath);
                image.preserveAspect = image.sprite != null;
            }

            return image;
        }

        static Image SpriteChildOf(RectTransform parent, string childName, float x, float y, float width, float height)
        {
            var rect = NodeUnder(parent, childName, x, y, width, height);
            var image = rect.gameObject.AddComponent<Image>();
            image.raycastTarget = false;
            return image;
        }

        static Text Label(RectTransform parent, string childName, float x, float y, float width, float height, int size, Color color)
        {
            var rect = NodeUnder(parent, childName, x, y, width, height);
            var text = rect.gameObject.AddComponent<Text>();
            text.font = Resources.Load<Font>("Original/GameFont");
            text.text = string.Empty;
            text.fontSize = size;
            text.color = color;
            text.alignment = TextAnchor.MiddleLeft;
            text.raycastTarget = false;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            return text;
        }

        static Image Line(RectTransform parent, string childName, float x, float y, float width, float height, Color color)
        {
            var rect = NodeUnder(parent, childName, x, y, width, height);
            var image = rect.gameObject.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        static Button TextButton(RectTransform parent, string childName, float x, float y, float width, float height, string label, bool primary)
        {
            var rect = NodeUnder(parent, childName, x, y, width, height);
            var image = rect.gameObject.AddComponent<Image>();
            image.color = primary ? new Color(0.8f, 0.66f, 0.36f) : new Color(0.15f, 0.38f, 0.35f);
            image.raycastTarget = true;
            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;

            var text = Label(rect, "Label", 0f, 0f, width, height, 24, primary ? PaperInk : Cream);
            text.alignment = TextAnchor.MiddleCenter;
            text.text = label;
            return button;
        }

        static Sprite LoadSprite(string resourcePath)
        {
            var texture = Resources.Load<Texture2D>(resourcePath);
            if (texture == null) return null;
            return Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100f);
        }

        static void Bind(Button button, System.Action handler)
        {
            if (button == null) return;
            button.onClick.RemoveAllListeners();
            if (handler != null) button.onClick.AddListener(() => handler());
        }

        static void BindRuntime(Button button, System.Action handler)
        {
            Bind(button, handler);
        }

        // 动态节点使用本地配色；与 ScreenPrefabKit 保持同一套色值，避免运行时代码反向依赖编辑器程序集。
        static readonly Color Gold = new Color(0.86f, 0.78f, 0.54f);
        static readonly Color Cream = new Color(0.94f, 0.94f, 0.87f);
        static readonly Color Jade = new Color(0.48f, 1f, 0.85f);
        static readonly Color PaperInk = new Color(0.25f, 0.14f, 0.05f);
        static readonly Color WarnOrange = new Color(1f, 0.63f, 0.4f);

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
            public bool HasPendingRelic;
            public bool ShowQuest;
            public string QuestTitle;
            public string QuestTarget;
            public string QuestProgress;
            public RelicCell Relic;

            /// <summary>从真实存档组装一份完整快照。Busy 由控制器传入，View 不感知流程状态。</summary>
            public static HomeSnapshot Capture(GameRules rules, string nickname, bool auto, bool busy)
            {
                var snapshot = new HomeSnapshot();
                if (rules == null) return snapshot;

                var save = rules.Save;
                snapshot.Identity = "等级" + save.level + "  " + (string.IsNullOrWhiteSpace(nickname) ? "寻墟者" : nickname);
                snapshot.Power = "基础战力  " + rules.Attributes.Power;
                snapshot.Realm = rules.Realm.displayName;
                snapshot.SealTokens = save.peaches.ToString();
                snapshot.SpiritStones = save.coins.ToString();

                // 发起破封需要：无待处理遗宝、有破封符、未处于其他进行中的流程。
                snapshot.HasTokens = save.peaches > 0;
                snapshot.HasPendingRelic = save.pending != null;
                snapshot.CanUnseal = snapshot.HasTokens && !snapshot.HasPendingRelic && !busy;
                snapshot.UnsealLabel = snapshot.HasTokens ? "破  封" : "破封符不足";
                snapshot.SealCostLabel = snapshot.HasTokens ? "消耗 ×1" : "消耗 ×1 · 不足";
                snapshot.Auto = auto;
                snapshot.Interactable = !busy;

                // 按钮文案随破封符与待处理遗宝的真实状态变化。
                snapshot.UnsealLabel = snapshot.HasTokens ? "破  封" : "破封符不足";
                snapshot.SealCostLabel = snapshot.HasTokens ? "消耗 ×1" : "消耗 ×1 · 不足";

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
        }
    }
}
