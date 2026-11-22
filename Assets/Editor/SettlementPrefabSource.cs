using Gxdl.Modules;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Gxdl.EditorTools
{
    /// <summary>
    /// 战斗结算 prefab 生成器。
    /// 骨架照 GuxuSettlementUI.ShowBattleResult 的弹窗构图还原；
    /// 原界面按内容动态算高度，prefab 改为固定槽位 + 分区显隐，
    /// 胜败、奖励条数、解锁文案与按钮可用性由 SettlementView.Apply 填入。
    /// </summary>
    [InitializeOnLoad]
    public static class SettlementPrefabSource
    {
        public const string PrefabPath = "Assets/Resources/" + UiScreens.Settlement + ".prefab";

        const float PanelWidth = 680f;
        const float PanelHeight = 940f;

        static readonly Color Ink = new Color(0.12f, 0.16f, 0.15f);
        static readonly Color Cream = new Color(0.94f, 0.94f, 0.87f);
        static readonly Color Gold = new Color(0.86f, 0.78f, 0.54f);

        /// <summary>统计行、奖励槽与底部动作的纵坐标，修改布局时只需改这里。</summary>
        const float UnlockY = 592f;
        const float StatsY = 648f;
        const float BonusY = 742f;
        const float ChallengeY = 866f;

        static SettlementPrefabSource()
        {
            ScreenPrefabKit.Register("Settlement", Build);
        }

        [MenuItem("Gxdl/UI/生成/战斗结算", priority = 304)]
        public static void Generate()
        {
            var path = Build();
            if (path != null) EditorUtility.DisplayDialog("生成完成", "已生成\n" + path, "确定");
        }

        public static string Build()
        {
            using (var builder = new UiPrefabBuilder("SettlementView",
                new Vector2(UiRootLayout.DesignWidth, UiRootLayout.DesignHeight)))
            {
                var root = builder.Root;
                AnchorUtility.Apply(root, AnchorPreset.Stretch, Vector2.zero, Vector2.zero);
                ScreenPrefabKit.AddRootComponents(root);

                var shade = builder.AnchoredNode("", "Full Canvas Modal Shade", AnchorPreset.Stretch, Vector2.zero);
                var shadeImage = shade.gameObject.AddComponent<Image>();
                shadeImage.color = new Color(0.01f, 0.05f, 0.07f, 0.68f);
                shadeImage.raycastTarget = true;

                var panel = builder.AnchoredNode("", "Settlement Panel", AnchorPreset.Center,
                    new Vector2(PanelWidth, PanelHeight));
                BuildPanel(builder, panel);
                BuildHeader(builder);
                BuildRewards(builder);
                BuildDefeat(builder);
                BuildFooter(builder);

                var path = builder.Save(PrefabPath);
                AssetDatabase.SaveAssets();
                if (path != null) Bind(path);
                return path;
            }
        }

        static void BuildPanel(UiPrefabBuilder builder, RectTransform panel)
        {
            var paper = builder.ImageNode("", "Modal Paper", 0f, 0f, PanelWidth, PanelHeight,
                new Color(0.055f, 0.12f, 0.16f, 0.98f));
            paper.raycastTarget = true;

            // 胜利徽记占位：真实立绘由美术资源提供，缺失时保持底色不空白。
            builder.ImageNode("", "Settlement Emblem", 50f, 0f, 580f, 220f, new Color(0.09f, 0.18f, 0.19f, 0.9f))
                .raycastTarget = false;

            var result = builder.TextNode("", "Settlement Result", 45f, 115f, 590f, 70f, string.Empty, 52, Gold,
                TextAnchor.MiddleCenter);
            AddOutline(result, new Color(0.09f, 0.13f, 0.12f, 0.95f), new Vector2(1f, -2f));

            var strip = builder.ImageNode("", "Settlement Stage Strip", 50f, 192f, 580f, 54f, Cream);
            strip.raycastTarget = false;
            builder.TextNode("Settlement Stage Strip", "Stage", 22f, 7f, 536f, 40f, string.Empty, 29, Ink,
                TextAnchor.MiddleCenter);

            // 终局徽记：仅主线最后一关或古塔百层显示。
            var complete = builder.Node("", "Settlement Complete", 165f, 251f, 350f, 46f);
            var completeImage = complete.gameObject.AddComponent<Image>();
            completeImage.color = new Color(0.15f, 0.38f, 0.35f, 0.94f);
            var completeButton = complete.gameObject.AddComponent<Button>();
            completeButton.targetGraphic = completeImage;
            completeButton.interactable = false;
            builder.TextNode("Settlement Complete", "Label", 0f, 0f, 350f, 46f, string.Empty, 25, Cream,
                TextAnchor.MiddleCenter);
            complete.gameObject.SetActive(false);

            var close = builder.Node("", "Settlement Close", PanelWidth - 70f, 16f, 54f, 54f);
            var closeImage = close.gameObject.AddComponent<Image>();
            closeImage.color = Color.clear;
            closeImage.raycastTarget = true;
            close.gameObject.AddComponent<Button>().targetGraphic = closeImage;
            builder.TextNode("Settlement Close", "Label", 0f, 0f, 54f, 54f, "×", 34, Cream, TextAnchor.MiddleCenter);
        }

        static void BuildHeader(UiPrefabBuilder builder)
        {
            var reward = builder.Node("", "Settlement Reward", 0f, 308f, PanelWidth, 300f);
            var heading = builder.TextNode("Settlement Reward", "Reward Heading", 55f, 0f, 570f, 39f, string.Empty, 30,
                Ink, TextAnchor.MiddleLeft);
            Fit(heading);
        }

        static void BuildRewards(UiPrefabBuilder builder)
        {
            // 三列三行，覆盖全部奖励种类；多出的槽位由 ApplyRewards 隐藏。
            for (var i = 0; i < 3; i++)
            {
                for (var j = 0; j < 3; j++)
                {
                    var index = i * 3 + j;
                    var x = 45f + j * 590f / 3f;
                    var y = 356f + i * 184f;
                    var slot = builder.Node("Settlement Reward", "Reward Slot " + index, x, y, 190f, 170f);

                    var cell = builder.ImageNode("Settlement Reward/Reward Slot " + index, "Icon", 39f, 0f, 112f, 112f,
                        Color.white);
                    cell.raycastTarget = false;

                    // 数量角标：深底浅字，与图标左上对齐。
                    builder.ImageNode("Settlement Reward/Reward Slot " + index, "Count Backing", 51f, 5f, 94f, 28f,
                        new Color(0.02f, 0.04f, 0.05f, 0.82f)).raycastTarget = false;
                    var value = builder.TextNode("Settlement Reward/Reward Slot " + index, "Value", 53f, 4f, 90f, 30f,
                        string.Empty, 23, Cream, TextAnchor.MiddleRight);
                    Fit(value);
                    var name = builder.TextNode("Settlement Reward/Reward Slot " + index, "Name", 3f, 113f, 184f, 29f,
                        string.Empty, 24, Ink, TextAnchor.MiddleCenter);
                    Fit(name);

                    slot.gameObject.SetActive(false);
                }
            }

            var none = builder.TextNode("Settlement Reward", "No Reward", 55f, 357f, 570f, 48f, "本次无可领取奖励", 26, Ink,
                TextAnchor.MiddleLeft);
            Fit(none);
        }

        static void BuildDefeat(UiPrefabBuilder builder)
        {
            var block = builder.Node("", "Settlement Defeat", 0f, 308f, PanelWidth, 260f);
            builder.ImageNode("Settlement Defeat", "Shield Icon", 65f, 30f, 85f, 85f,
                new Color(0.26f, 0.75f, 0.94f, 0.85f)).raycastTarget = false;
            var title = builder.TextNode("Settlement Defeat", "Title", 175f, 0f, 440f, 42f, "整备后再战", 31, Ink,
                TextAnchor.MiddleLeft);
            Fit(title);
            var advice = builder.TextNode("Settlement Defeat", "Advice", 175f, 52f, 440f, 111f, string.Empty, 27, Ink,
                TextAnchor.UpperLeft);
            Fit(advice);
            advice.verticalOverflow = VerticalWrapMode.Overflow;
            block.gameObject.SetActive(false);
        }

        static void BuildFooter(UiPrefabBuilder builder)
        {
            var unlock = builder.TextNode("", "Settlement Unlock", 60f, UnlockY, 560f, 54f, string.Empty, 23,
                new Color(0.15f, 0.48f, 0.35f));
            Fit(unlock);
            unlock.gameObject.SetActive(false);

            builder.ImageNode("", "Stats Rule", 50f, StatsY, 580f, 1f, new Color(0.44f, 0.39f, 0.25f, 0.35f))
                .raycastTarget = false;

            string[] labels = { "回合", "输出", "承伤", "治疗" };
            for (var i = 0; i < 4; i++)
            {
                var x = 45f + i * 147.5f;
                builder.TextNode("", "Stat " + i + " Label", x, StatsY + 9f, 147.5f, 29f, labels[i], 23, Ink,
                    TextAnchor.MiddleCenter);
                var value = builder.TextNode("", "Stat " + i + " Value", x, StatsY + 39f, 147.5f, 36f, string.Empty, 29,
                    Ink, TextAnchor.MiddleCenter);
                Fit(value);
                if (i > 0)
                {
                    builder.ImageNode("", "Stat Divider " + i, x, StatsY + 17f, 1f, 51f,
                        new Color(0.44f, 0.39f, 0.25f, 0.22f)).raycastTarget = false;
                }
            }

            AddSeal(builder, "Reward Double", 65f, BonusY, 550f, 68f);
            AddSealLabel(builder, "Reward Double", 550f, 68f, 28);
            SetActive(builder, "Reward Double", false);

            AddSeal(builder, "Settlement Challenge", 45f, ChallengeY, 285f, 68f);
            AddSealLabel(builder, "Settlement Challenge", 285f, 68f, 28);

            AddSeal(builder, "Settlement Growth", 350f, ChallengeY, 285f, 68f);
            var growth = builder.TextNode("Settlement Growth", "Label", 0f, 0f, 285f, 68f, "调整养成", 28, Cream,
                TextAnchor.MiddleCenter);
            Fit(growth);

            // 「返回」是纯文字按钮，只有终局状态显示。
            AddHit(builder, "Settlement Back", 200f, ChallengeY + 88f, 280f, 52f);
            var back = builder.TextNode("Settlement Back", "Label", 0f, 0f, 280f, 52f, "返回", 25, Ink,
                TextAnchor.MiddleCenter);
            Fit(back);
            SetActive(builder, "Settlement Back", false);
        }

        /// <summary>封蜡风格主按钮：底色 + 可点击区域 + 子文本。</summary>
        static void AddSeal(UiPrefabBuilder builder, string name, float x, float y, float width, float height)
        {
            var node = builder.Node("", name, x, y, width, height);
            var image = node.gameObject.AddComponent<Image>();
            image.color = new Color(0.15f, 0.38f, 0.35f, 0.94f);
            image.raycastTarget = true;
            node.gameObject.AddComponent<Button>().targetGraphic = image;
        }

        static void AddHit(UiPrefabBuilder builder, string name, float x, float y, float width, float height)
        {
            var node = builder.Node("", name, x, y, width, height);
            var image = node.gameObject.AddComponent<Image>();
            image.color = Color.clear;
            image.raycastTarget = true;
            node.gameObject.AddComponent<Button>().targetGraphic = image;
        }

        /// <summary>按钮文案单独建节点，保持与按钮同尺寸、居中。</summary>
        static void AddSealLabel(UiPrefabBuilder builder, string buttonName, float width, float height, int size)
        {
            var text = builder.TextNode(buttonName, "Label", 0f, 0f, width, height, string.Empty, size, Cream,
                TextAnchor.MiddleCenter);
            Fit(text);
        }

        static GameObject Find(UiPrefabBuilder builder, string path)
        {
            var node = builder.Root.Find(path);
            return node != null ? node.gameObject : null;
        }

        static void SetActive(UiPrefabBuilder builder, string path, bool active)
        {
            var node = Find(builder, path);
            if (node != null) node.SetActive(active);
        }

        static void Fit(Text text)
        {
            if (text == null) return;
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = Mathf.Max(16, text.fontSize * 2 / 3);
            text.resizeTextMaxSize = text.fontSize;
        }

        static void AddOutline(Text text, Color color, Vector2 distance)
        {
            if (text == null) return;
            var outline = text.gameObject.AddComponent<Outline>();
            outline.effectColor = color;
            outline.effectDistance = distance;
        }

        static void Bind(string prefabPath)
        {
            var contents = PrefabUtility.LoadPrefabContents(prefabPath);
            if (contents == null) return;
            try
            {
                var view = contents.GetComponent<SettlementView>();
                if (view == null) view = contents.AddComponent<SettlementView>();

                var so = new SerializedObject(view);
                Set(so, "resultText", FindText(contents, "Settlement Result"));
                Set(so, "stageText", FindText(contents, "Settlement Stage Strip/Stage"));
                Set(so, "closeButton", Get<Button>(contents, "Settlement Close"));
                Set(so, "completeButton", Get<Button>(contents, "Settlement Complete"));
                Set(so, "rewardHeadingText", FindText(contents, "Settlement Reward/Reward Heading"));
                Set(so, "rewardBlock", Find(contents, "Settlement Reward"));
                Set(so, "noRewardText", FindText(contents, "Settlement Reward/No Reward"));
                Set(so, "defeatBlock", Find(contents, "Settlement Defeat"));
                Set(so, "defeatAdviceText", FindText(contents, "Settlement Defeat/Advice"));
                Set(so, "unlockText", FindText(contents, "Settlement Unlock"));
                Set(so, "bonusButton", Get<Button>(contents, "Reward Double"));
                Set(so, "bonusLabel", FindText(contents, "Reward Double/Label"));
                Set(so, "challengeButton", Get<Button>(contents, "Settlement Challenge"));
                Set(so, "challengeLabel", FindText(contents, "Settlement Challenge/Label"));
                Set(so, "growthButton", Get<Button>(contents, "Settlement Growth"));
                Set(so, "backButton", Get<Button>(contents, "Settlement Back"));

                var slots = new System.Collections.Generic.List<GameObject>();
                var icons = new System.Collections.Generic.List<Image>();
                var names = new System.Collections.Generic.List<Text>();
                var values = new System.Collections.Generic.List<Text>();
                for (var i = 0; i < 9; i++)
                {
                    var path = "Settlement Reward/Reward Slot " + i;
                    slots.Add(Find(contents, path));
                    icons.Add(Get<Image>(contents, path + "/Icon"));
                    names.Add(FindText(contents, path + "/Name"));
                    values.Add(FindText(contents, path + "/Value"));
                }
                SetArray(so, "rewardSlots", slots);
                SetArray(so, "rewardIcons", icons);
                SetArray(so, "rewardNames", names);
                SetArray(so, "rewardValues", values);

                var labels = new System.Collections.Generic.List<Text>();
                var stats = new System.Collections.Generic.List<Text>();
                for (var i = 0; i < 4; i++)
                {
                    labels.Add(FindText(contents, "Stat " + i + " Label"));
                    stats.Add(FindText(contents, "Stat " + i + " Value"));
                }
                SetArray(so, "statLabels", labels);
                SetArray(so, "statValues", stats);
                so.ApplyModifiedPropertiesWithoutUndo();

                PrefabUtility.SaveAsPrefabAsset(contents, prefabPath);
                AssetDatabase.SaveAssets();
                Debug.Log("[Settlement] 已生成并绑定 " + prefabPath);
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
