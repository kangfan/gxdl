using Gxdl.Modules;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Gxdl.EditorTools
{
    /// <summary>
    /// 战前准备 prefab 生成器。
    /// 内容照 GxdlGame.DrawBattlePreparation 的信息层级还原；
    /// 原界面是 940x1672 的缩放构图，这里按 750x1334 基准重排，
    /// 舞台名、敌人属性、奖励与技能由 BattlePrepView.Apply 运行时填入。
    /// </summary>
    [InitializeOnLoad]
    public static class BattlePrepPrefabSource
    {
        public const string PrefabPath = "Assets/Resources/" + UiScreens.BattlePrep + ".prefab";

        static readonly Color Ink = new Color(0.08f, 0.12f, 0.12f);
        static readonly Color PaleGold = new Color(1f, 0.88f, 0.58f);
        static readonly Color Cream = ScreenPrefabKit.Cream;

        static BattlePrepPrefabSource()
        {
            ScreenPrefabKit.Register("BattlePrep", Build);
        }

        [MenuItem("Gxdl/UI/生成/战前准备", priority = 302)]
        public static void Generate()
        {
            var path = Build();
            if (path != null) EditorUtility.DisplayDialog("生成完成", "已生成\n" + path, "确定");
        }

        public static string Build()
        {
            using (var builder = new UiPrefabBuilder("BattlePrepView",
                new Vector2(UiRootLayout.DesignWidth, UiRootLayout.DesignHeight)))
            {
                var root = builder.Root;
                AnchorUtility.Apply(root, AnchorPreset.Stretch, Vector2.zero, Vector2.zero);
                ScreenPrefabKit.AddRootComponents(root);

                // 整屏遮罩：阻断背后的章节界面点击，换分辨率时铺满。
                var shade = builder.AnchoredNode("", "Prep Shade", AnchorPreset.Stretch, Vector2.zero);
                var shadeImage = shade.gameObject.AddComponent<Image>();
                shadeImage.color = new Color(0.025f, 0.075f, 0.10f, 0.90f);
                shadeImage.raycastTarget = true;

                BuildHeader(builder);
                BuildEnemy(builder);
                BuildRewards(builder);
                BuildSkills(builder);
                BuildAssessment(builder);
                BuildFooter(builder);

                var path = builder.Save(PrefabPath);
                AssetDatabase.SaveAssets();
                if (path != null) Bind(path);
                return path;
            }
        }

        static void BuildHeader(UiPrefabBuilder builder)
        {
            var title = builder.TextNode("", "Prep Stage", 40f, 34f, 670f, 64f, string.Empty, 40, PaleGold,
                TextAnchor.MiddleCenter);
            Fit(title);

            var close = builder.Node("", "Prep Close", 640f, 30f, 96f, 64f);
            var closeImage = close.gameObject.AddComponent<Image>();
            closeImage.color = new Color(0.15f, 0.38f, 0.35f, 0.94f);
            closeImage.raycastTarget = true;
            close.gameObject.AddComponent<Button>().targetGraphic = closeImage;
            builder.TextNode("Prep Close", "Label", 0f, 0f, 96f, 64f, "×", 34, Cream, TextAnchor.MiddleCenter);
        }

        static void BuildEnemy(UiPrefabBuilder builder)
        {
            var guardian = builder.TextNode("", "Prep Guardian", 40f, 108f, 670f, 56f, string.Empty, 36, Ink,
                TextAnchor.MiddleCenter);
            Fit(guardian);

            builder.ImageNode("", "Prep Trait Icon", 168f, 180f, 40f, 40f, new Color(0.08f, 0.24f, 0.23f))
                .raycastTarget = false;
            builder.TextNode("", "Prep Trait Label", 214f, 182f, 160f, 36f, "敌方特性", 24, Cream);

            var mechanic = builder.TextNode("", "Prep Mechanic", 214f, 222f, 380f, 30f, string.Empty, 24, Ink,
                TextAnchor.MiddleLeft);
            Fit(mechanic);

            var wave = builder.TextNode("", "Prep Wave", 214f, 252f, 380f, 28f, string.Empty, 22, Ink,
                TextAnchor.MiddleLeft);
            Fit(wave);

            // 三项敌方属性等宽三栏，数值长度不定，用最佳适配避免溢出。
            Fit(builder.TextNode("", "Prep Enemy Attack", 60f, 300f, 200f, 36f, string.Empty, 26, Ink));
            Fit(builder.TextNode("", "Prep Enemy HP", 275f, 300f, 200f, 36f, string.Empty, 26, Ink));
            Fit(builder.TextNode("", "Prep Enemy Defense", 490f, 300f, 200f, 36f, string.Empty, 26, Ink));
        }

        static void BuildRewards(UiPrefabBuilder builder)
        {
            Fit(builder.TextNode("", "Prep Reward Title", 40f, 356f, 164f, 34f, string.Empty, 24, Cream,
                TextAnchor.MiddleLeft));
            Fit(builder.TextNode("", "Prep Reward Limit", 450f, 356f, 260f, 34f, string.Empty, 23, Ink,
                TextAnchor.MiddleRight));

            // 三列两行：最多七种产出，位置固定，避免运行时按数量重排。
            for (var i = 0; i < 6; i++)
            {
                var x = 60f + i % 3 * 228f;
                var y = 398f + i / 3 * 46f;
                var slot = builder.Node("", "Prep Reward Slot " + i, x, y, 220f, 42f);
                builder.ImageNode("Prep Reward Slot " + i, "Icon", 0f, 1f, 38f, 38f, Color.white).raycastTarget = false;
                var label = builder.TextNode("Prep Reward Slot " + i, "Label", 44f, 0f, 176f, 42f, string.Empty, 24,
                    Ink, TextAnchor.MiddleLeft);
                label.verticalOverflow = VerticalWrapMode.Overflow;
                label.horizontalOverflow = HorizontalWrapMode.Wrap;
                slot.gameObject.SetActive(false);
            }

            var none = builder.TextNode("", "Prep No Reward", 60f, 400f, 630f, 42f, "本层首通奖励已领取", 25, Ink,
                TextAnchor.MiddleLeft);
            Fit(none);
        }

        static void BuildSkills(UiPrefabBuilder builder)
        {
            builder.TextNode("", "Prep Skill Heading", 60f, 500f, 115f, 36f, "出战技能", 26, Cream);
            var hint = builder.TextNode("", "Prep Skill Hint", 190f, 500f, 500f, 36f, "天赋、装备词条与灵玉自动生效。", 22,
                Ink, TextAnchor.MiddleLeft);
            Fit(hint);

            for (var i = 0; i < 4; i++)
            {
                var x = 40f + i % 2 * 345f;
                var y = 548f + i / 2 * 94f;
                var card = builder.Node("", "Skill Choice " + i, x, y, 330f, 86f);
                var cardImage = card.gameObject.AddComponent<Image>();
                cardImage.color = new Color(0.12f, 0.22f, 0.22f, 0.94f);
                cardImage.raycastTarget = true;
                var button = card.gameObject.AddComponent<Button>();
                button.targetGraphic = cardImage;

                var icon = builder.ImageNode("Skill Choice " + i, "Icon", 14f, -2f, 86f, 90f,
                    new Color(0.67f, 0.96f, 1f));
                icon.raycastTarget = false;

                // 选中标记：金色圆环 + 深金内盘 + 对勾。
                var check = builder.Node("Skill Choice " + i, "Selected", 278f, 1f, 40f, 40f);
                var rim = check.gameObject.AddComponent<GuxuPortraitDisc>();
                rim.color = new Color(1f, 0.88f, 0.52f);
                rim.raycastTarget = false;
                var center = builder.Node("Skill Choice " + i + "/Selected", "Gold", 3f, 3f, 34f, 34f);
                var disc = center.gameObject.AddComponent<GuxuPortraitDisc>();
                disc.color = new Color(0.72f, 0.42f, 0.06f);
                disc.raycastTarget = false;
                builder.TextNode("Skill Choice " + i + "/Selected", "Tick", 4f, 0f, 32f, 37f, "✓", 30, Color.white,
                    TextAnchor.MiddleCenter);
                check.gameObject.SetActive(i == 0);

                var name = builder.TextNode("Skill Choice " + i, "Name", 112f, 8f, 160f, 70f, string.Empty, 32,
                    Cream, TextAnchor.MiddleLeft);
                Fit(name);
            }

            var detail = builder.TextNode("", "Prep Skill Detail", 60f, 740f, 630f, 56f, string.Empty, 22, Ink,
                TextAnchor.MiddleCenter);
            Fit(detail);

            // 整条区域可点，文案由控制器回填，因此不挂子文本按钮。
            var mode = builder.Node("", "Prep Auto Skill", 60f, 800f, 630f, 50f);
            var modeImage = mode.gameObject.AddComponent<Image>();
            modeImage.color = Color.clear;
            modeImage.raycastTarget = true;
            mode.gameObject.AddComponent<Button>().targetGraphic = modeImage;
            var modeText = builder.TextNode("Prep Auto Skill", "Label", 0f, 0f, 630f, 50f, string.Empty, 25, Cream,
                TextAnchor.MiddleCenter);
            Fit(modeText);
        }

        static void BuildAssessment(UiPrefabBuilder builder)
        {
            builder.TextNode("", "Prep Power Label", 60f, 866f, 200f, 34f, "基础战力", 26, Ink, TextAnchor.MiddleLeft);
            Fit(builder.TextNode("", "Prep Power", 265f, 862f, 200f, 44f, string.Empty, 36,
                new Color(0.12f, 0.32f, 0.30f), TextAnchor.MiddleLeft));

            // 三条战斗属性等宽，与规则里的会心 / 连击 / 反击对应。
            Fit(builder.TextNode("", "Prep Critical", 60f, 920f, 200f, 36f, string.Empty, 26, Ink));
            Fit(builder.TextNode("", "Prep Combo", 275f, 920f, 200f, 36f, string.Empty, 26, Ink));
            Fit(builder.TextNode("", "Prep Counter", 490f, 920f, 200f, 36f, string.Empty, 26, Ink));

            Fit(builder.TextNode("", "Prep Assessment", 40f, 972f, 670f, 40f, string.Empty, 26, Ink));
            Fit(builder.TextNode("", "Prep Assessment Conditions", 40f, 1014f, 670f, 34f,
                "当前配装 / 自动释放抽样估计，手动时机影响结果", 22, Ink));
        }

        static void BuildFooter(UiPrefabBuilder builder)
        {
            AddHit(builder, "Prep Growth", 40f, 1110f, 315f, 80f);
            builder.TextNode("Prep Growth", "Label", 0f, 0f, 315f, 80f, "调整养成", 30, Cream, TextAnchor.MiddleCenter);

            AddHit(builder, "Ruins Challenge", 380f, 1110f, 315f, 80f);
            var challenge = builder.TextNode("Ruins Challenge", "Label", 0f, 0f, 315f, 80f, "开始挑战", 30,
                new Color(0.18f, 0.09f, 0.02f), TextAnchor.MiddleCenter);
            challenge.name = "Challenge Caption";
            Fit(challenge);

            // 不可挑战时的压暗层，默认隐藏，由 SetChallenge 控制。
            var block = builder.ImageNode("", "Prep Challenge Block", 388f, 1117f, 299f, 66f,
                new Color(0.03f, 0.06f, 0.08f, 0.35f));
            block.raycastTarget = false;
            block.rectTransform.SetAsLastSibling();
            block.gameObject.SetActive(false);
        }

        static Button AddHit(UiPrefabBuilder builder, string name, float x, float y, float width, float height)
        {
            var node = builder.Node("", name, x, y, width, height);
            var image = node.gameObject.AddComponent<Image>();
            image.color = new Color(0.15f, 0.38f, 0.35f, 0.94f);
            image.raycastTarget = true;
            var button = node.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            return button;
        }

        /// <summary>原界面统一用 resizeTextForBestFit，prefab 同步保留，长文案不撑破版面。</summary>
        static void Fit(Text text)
        {
            if (text == null) return;
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = Mathf.Max(18, text.fontSize * 2 / 3);
            text.resizeTextMaxSize = text.fontSize;
        }

        static void Bind(string prefabPath)
        {
            var contents = PrefabUtility.LoadPrefabContents(prefabPath);
            if (contents == null) return;
            try
            {
                var view = contents.GetComponent<BattlePrepView>();
                if (view == null) view = contents.AddComponent<BattlePrepView>();

                var so = new SerializedObject(view);
                Set(so, "stageNameText", FindText(contents, "Prep Stage"));
                Set(so, "closeButton", Get<Button>(contents, "Prep Close"));
                Set(so, "guardianText", FindText(contents, "Prep Guardian"));
                Set(so, "mechanicText", FindText(contents, "Prep Mechanic"));
                Set(so, "waveText", FindText(contents, "Prep Wave"));
                Set(so, "attackText", FindText(contents, "Prep Enemy Attack"));
                Set(so, "hpText", FindText(contents, "Prep Enemy HP"));
                Set(so, "defenseText", FindText(contents, "Prep Enemy Defense"));
                Set(so, "rewardTitleText", FindText(contents, "Prep Reward Title"));
                Set(so, "rewardLimitText", FindText(contents, "Prep Reward Limit"));
                Set(so, "noRewardText", FindText(contents, "Prep No Reward"));
                Set(so, "skillDetailText", FindText(contents, "Prep Skill Detail"));
                Set(so, "autoSkillButton", Get<Button>(contents, "Prep Auto Skill"));
                Set(so, "skillModeText", FindText(contents, "Prep Auto Skill/Label"));
                Set(so, "assessmentText", FindText(contents, "Prep Assessment"));
                Set(so, "powerText", FindText(contents, "Prep Power"));
                Set(so, "criticalText", FindText(contents, "Prep Critical"));
                Set(so, "comboText", FindText(contents, "Prep Combo"));
                Set(so, "counterText", FindText(contents, "Prep Counter"));
                Set(so, "growthButton", Get<Button>(contents, "Prep Growth"));
                Set(so, "challengeButton", Get<Button>(contents, "Ruins Challenge"));
                Set(so, "challengeLabel", FindText(contents, "Ruins Challenge/Challenge Caption"));
                Set(so, "challengeBlock", Find(contents, "Prep Challenge Block"));

                var slots = new System.Collections.Generic.List<GameObject>();
                var icons = new System.Collections.Generic.List<Image>();
                var labels = new System.Collections.Generic.List<Text>();
                var values = new System.Collections.Generic.List<Text>();
                for (var i = 0; i < 6; i++)
                {
                    var path = "Prep Reward Slot " + i;
                    slots.Add(Find(contents, path));
                    icons.Add(Get<Image>(contents, path + "/Icon"));
                    labels.Add(FindText(contents, path + "/Label"));
                    values.Add(FindText(contents, path + "/Label"));
                }
                SetArray(so, "rewardSlots", slots);
                SetArray(so, "rewardIcons", icons);
                SetArray(so, "rewardLabels", labels);
                SetArray(so, "rewardValues", values);

                var buttons = new System.Collections.Generic.List<Button>();
                var skillIcons = new System.Collections.Generic.List<Image>();
                var checks = new System.Collections.Generic.List<GameObject>();
                for (var i = 0; i < 4; i++)
                {
                    var path = "Skill Choice " + i;
                    buttons.Add(Get<Button>(contents, path));
                    skillIcons.Add(Get<Image>(contents, path + "/Icon"));
                    var check = Find(contents, path + "/Selected");
                    checks.Add(check != null ? check.gameObject : null);
                }
                SetArray(so, "skillButtons", buttons);
                SetArray(so, "skillIcons", skillIcons);
                SetArray(so, "skillChecks", checks);
                so.ApplyModifiedPropertiesWithoutUndo();

                PrefabUtility.SaveAsPrefabAsset(contents, prefabPath);
                AssetDatabase.SaveAssets();
                Debug.Log("[BattlePrep] 已生成并绑定 " + prefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contents);
            }
        }

        static GameObject Find(GameObject root, string path)
        {
            var node = root.transform.Find(path);
            return node != null ? node.gameObject : null;
        }


        static Text FindText(GameObject root, string path)
        {
            var node = Find(root, path);
            return node != null ? node.GetComponent<Text>() : null;
        }

        /// <summary>按路径取组件；节点或组件缺失时返回 null，绑定阶段只跳过不中断。</summary>
        static T Get<T>(GameObject root, string path) where T : Component
        {
            var node = Find(root, path);
            return node != null ? node.GetComponent<T>() : null;
        }

        static void Set(SerializedObject so, string field, Object value)
        {
            var property = so.FindProperty(field);
            if (property == null || value == null) return;
            property.objectReferenceValue = value;
        }

        static void SetArray<T>(SerializedObject so, string field, System.Collections.Generic.List<T> values) where T : Object
        {
            var property = so.FindProperty(field);
            if (property == null) return;
            property.arraySize = values.Count;
            for (var i = 0; i < values.Count; i++) property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
        }
    }
}
