using Gxdl.Modules;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Gxdl.EditorTools
{
    /// <summary>
    /// 难度选择 prefab 生成器。
    /// 骨架照 GuxuDifficultyUI.ShowDungeon 还原：可滚动路径 + 底部详情。
    /// 节点状态、奖励与条件由 DifficultyView 运行时填入；
    /// 原界面按屏幕高度重排，这里改为锚点 + 运行期按比例缩放内容高度。
    /// </summary>
    [InitializeOnLoad]
    public static class DifficultyPrefabSource
    {
        public const string PrefabPath = "Assets/Resources/" + UiScreens.Difficulty + ".prefab";

        const float PathHeight = 808f;
        const float DetailHeight = 425f;
        const float HeaderHeight = 180f;

        static readonly Color Ink = new Color(0.12f, 0.16f, 0.15f);
        static readonly Color Cream = new Color(0.94f, 0.94f, 0.87f);
        static readonly Color Gold = new Color(0.86f, 0.78f, 0.54f);

        static DifficultyPrefabSource()
        {
            ScreenPrefabKit.Register("Difficulty", Build);
        }

        [MenuItem("Gxdl/UI/生成/难度选择", priority = 305)]
        public static void Generate()
        {
            var path = Build();
            if (path != null) EditorUtility.DisplayDialog("生成完成", "已生成\n" + path, "确定");
        }

        public static string Build()
        {
            using (var builder = new UiPrefabBuilder("DifficultyView",
                new Vector2(UiRootLayout.DesignWidth, UiRootLayout.DesignHeight)))
            {
                var root = builder.Root;
                AnchorUtility.Apply(root, AnchorPreset.Stretch, Vector2.zero, Vector2.zero);
                ScreenPrefabKit.AddRootComponents(root);

                var background = builder.AnchoredNode("", "Background", AnchorPreset.Stretch, Vector2.zero);
                var backgroundImage = background.gameObject.AddComponent<Image>();
                backgroundImage.color = new Color(0.035f, 0.13f, 0.18f);
                backgroundImage.raycastTarget = false;

                BuildPath(builder);
                BuildHeader(builder);
                BuildDetail(builder);

                var path = builder.Save(PrefabPath);
                AssetDatabase.SaveAssets();
                if (path != null) Bind(path);
                return path;
            }
        }

        static void BuildPath(UiPrefabBuilder builder)
        {
            // 视口贴顶下移 180，底部留出详情高度；内容高度由 View 按屏幕比例伸缩。
            var viewport = builder.AnchoredNode("", "Difficulty Path Viewport", AnchorPreset.Stretch, Vector2.zero);
            viewport.offsetMin = new Vector2(0f, DetailHeight);
            viewport.offsetMax = new Vector2(0f, -HeaderHeight);
            var hit = viewport.gameObject.AddComponent<Image>();
            hit.color = Color.clear;
            hit.raycastTarget = true;
            viewport.gameObject.AddComponent<RectMask2D>();

            var content = builder.Node("Difficulty Path Viewport", "Difficulty Path Content", 0f, 0f,
                UiRootLayout.DesignWidth, PathHeight);
            var path = builder.ImageNode("Difficulty Path Viewport/Difficulty Path Content", "Path Art",
                0f, 0f, UiRootLayout.DesignWidth, PathHeight, new Color(0.05f, 0.15f, 0.19f, 0.92f));
            path.raycastTarget = false;

            // 未开放路段：底边压暗，高度每打开一档重算。
            var shade = builder.ImageNode("Difficulty Path Viewport/Difficulty Path Content", "Difficulty Locked Path",
                0f, 0f, UiRootLayout.DesignWidth, PathHeight, new Color(0.015f, 0.045f, 0.065f, 0.52f));
            shade.raycastTarget = false;

            for (var i = 0; i < 10; i++)
            {
                DrawNode(builder, i);
            }

            // 选中环独立于节点，由 SetSelected 定位，避免每个节点各存一张。
            var ring = builder.ImageNode("Difficulty Path Viewport/Difficulty Path Content", "Difficulty Selection",
                -8f, -8f, 156f, 165f, new Color(1f, 0.88f, 0.58f, 0.55f));
            ring.raycastTarget = false;
            ring.gameObject.SetActive(false);

            var scroll = viewport.gameObject.AddComponent<ScrollRect>();
            scroll.viewport = viewport;
            scroll.content = content;
            scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 75f;
            scroll.decelerationRate = 0.06f;
        }

        static void DrawNode(UiPrefabBuilder builder, int index)
        {
            var tier = index + 1;
            var point = DifficultyView.TierPoints[index];
            var width = TierWidth(tier);
            var height = width * 0.75f;
            var path = "Difficulty Path Viewport/Difficulty Path Content";
            var name = "Difficulty Node " + tier;

            var node = builder.Node(path, name, point.x - width * 0.5f, point.y - height * 0.5f, width, height + 10f);
            var image = node.gameObject.AddComponent<Image>();
            image.color = Color.clear;
            image.raycastTarget = true;
            var button = node.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.transition = Selectable.Transition.None;

            var plate = builder.ImageNode(path + "/" + name, "Plate", 0f, 5f, width, height,
                new Color(0.15f, 0.38f, 0.35f, 0.94f));
            plate.raycastTarget = false;

            builder.TextNode(path + "/" + name, "Tier", 12f, height * (tier <= 2 ? 0.15f : 0.10f) + 5f, width - 24f,
                tier <= 2 ? 44f : 36f, tier.ToString(), tier <= 2 ? 32 : 27, Cream, TextAnchor.MiddleCenter);

            var cleared = builder.ImageNode(path + "/" + name, "Cleared", width * 0.5f - 14f,
                height - (tier <= 2 ? 44f : 34f) + 5f, 28f, 30f, new Color(0.45f, 0.85f, 0.60f, 0.9f));
            cleared.raycastTarget = false;
            cleared.gameObject.SetActive(false);

            var locked = builder.ImageNode(path + "/" + name, "Locked", width * 0.5f - 14f,
                height - (tier <= 2 ? 44f : 34f) + 5f, 28f, 30f, new Color(0.55f, 0.60f, 0.62f, 0.9f));
            locked.raycastTarget = false;

            var available = builder.TextNode(path + "/" + name, "Available", 10f,
                height - (tier <= 2 ? 44f : 34f) + 5f, width - 20f, 29f, "可挑战", 18, Gold,
                TextAnchor.MiddleCenter);
            available.gameObject.SetActive(false);
        }

        static void BuildHeader(UiPrefabBuilder builder)
        {
            var backing = builder.AnchoredNode("", "Difficulty Header", AnchorPreset.StretchTop,
                new Vector2(0f, HeaderHeight));
            var backingImage = backing.gameObject.AddComponent<Image>();
            backingImage.color = new Color(0.025f, 0.10f, 0.14f);
            backingImage.raycastTarget = true;

            var back = builder.Node("Difficulty Header", "Difficulty Back", 14f, 22f, 177f, 52f);
            var backImage = back.gameObject.AddComponent<Image>();
            backImage.color = new Color(0.15f, 0.38f, 0.35f, 0.94f);
            backImage.raycastTarget = true;
            back.gameObject.AddComponent<Button>().targetGraphic = backImage;
            builder.TextNode("Difficulty Header/Difficulty Back", "Label", 0f, 0f, 177f, 52f, "‹ 返回秘境", 26, Cream,
                TextAnchor.MiddleCenter);

            var name = builder.TextNode("Difficulty Header", "Difficulty Name", 193f, 19f, 400f, 63f, string.Empty, 43,
                Gold, TextAnchor.MiddleLeft);
            Fit(name);

            builder.ImageNode("Difficulty Header", "Difficulty Portal", 633f, 8f, 100f, 100f,
                new Color(0.10f, 0.30f, 0.28f, 0.8f)).raycastTarget = false;

            var attempts = builder.TextNode("Difficulty Header", "Difficulty Attempts", 60f, 95f, 630f, 34f,
                string.Empty, 25, Cream, TextAnchor.MiddleLeft);
            Fit(attempts);
            builder.TextNode("Difficulty Header", "Difficulty Hint", 40f, 137f, 420f, 30f, "已通关难度可速通", 22, Cream);
        }

        static void BuildDetail(UiPrefabBuilder builder)
        {
            // 详情贴底，高度固定；奖励横排两行三列，条数由 ApplyRewards 决定显隐。
            var detail = builder.AnchoredNode("", "Difficulty Selection Detail", AnchorPreset.StretchBottom,
                new Vector2(0f, DetailHeight));
            var paper = builder.ImageNode("Difficulty Selection Detail", "Detail Paper", 16f, 0f, 718f, DetailHeight,
                new Color(0.055f, 0.12f, 0.16f, 0.97f));
            paper.raycastTarget = true;

            var strip = builder.ImageNode("Difficulty Selection Detail", "Title Strip", 170f, 0f, 410f, 58f, Cream);
            strip.raycastTarget = false;
            var title = builder.TextNode("Difficulty Selection Detail/Title Strip", "Title", 10f, -4f, 390f, 46f,
                string.Empty, 28, Cream, TextAnchor.MiddleCenter);
            Fit(title);

            var rewardState = builder.TextNode("Difficulty Selection Detail", "Reward State", 60f, 62f, 630f, 36f,
                string.Empty, 26, Ink, TextAnchor.MiddleLeft);
            Fit(rewardState);

            for (var i = 0; i < 6; i++)
            {
                var x = 61f + i % 3 * 628f / 3f;
                var y = 112f + i / 3 * 57f;
                var slot = builder.Node("Difficulty Selection Detail", "Reward Slot " + i, x, y, 47f, 47f);
                builder.ImageNode("Difficulty Selection Detail/Reward Slot " + i, "Icon", 0f, 0f, 47f, 47f, Color.white)
                    .raycastTarget = false;
                var label = builder.TextNode("Difficulty Selection Detail/Reward Slot " + i, "Label", 50f, 0f,
                    628f / 3f - 50f, 47f, string.Empty, 23, Ink, TextAnchor.MiddleLeft);
                Fit(label);
                slot.gameObject.SetActive(false);
            }

            var condition = builder.TextNode("Difficulty Selection Detail", "Condition", 55f, 208f, 640f, 52f,
                string.Empty, 23, Ink, TextAnchor.UpperLeft);
            Fit(condition);
            condition.verticalOverflow = VerticalWrapMode.Overflow;

            AddSeal(builder, "Difficulty Challenge", 68f, 292f, 345f, 80f);
            Fit(builder.TextNode("Difficulty Challenge", "Label", 0f, 0f, 345f, 80f, string.Empty, 32, Cream,
                TextAnchor.MiddleCenter));

            AddSeal(builder, "Difficulty Sweep", 443f, 292f, 235f, 80f);
            Fit(builder.TextNode("Difficulty Sweep", "Label", 0f, 0f, 235f, 80f, "速通", 32, Cream,
                TextAnchor.MiddleCenter));
        }

        static void AddSeal(UiPrefabBuilder builder, string name, float x, float y, float width, float height)
        {
            var node = builder.Node("", name, x, y, width, height);
            var image = node.gameObject.AddComponent<Image>();
            image.color = new Color(0.15f, 0.38f, 0.35f, 0.94f);
            image.raycastTarget = true;
            node.gameObject.AddComponent<Button>().targetGraphic = image;
        }

        static float TierWidth(int tier) => tier <= 2 ? 140f : tier <= 5 ? 120f : 104f;

        static void Fit(Text text)
        {
            if (text == null) return;
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = Mathf.Max(16, text.fontSize * 2 / 3);
            text.resizeTextMaxSize = text.fontSize;
        }

        static void Bind(string prefabPath)
        {
            var contents = PrefabUtility.LoadPrefabContents(prefabPath);
            if (contents == null) return;
            try
            {
                var view = contents.GetComponent<DifficultyView>();
                if (view == null) view = contents.AddComponent<DifficultyView>();

                var so = new SerializedObject(view);
                Set(so, "backButton", Get<Button>(contents, "Difficulty Header/Difficulty Back"));
                Set(so, "dungeonNameText", FindText(contents, "Difficulty Header/Difficulty Name"));
                Set(so, "attemptsText", FindText(contents, "Difficulty Header/Difficulty Attempts"));
                Set(so, "pathScroll", Get<ScrollRect>(contents, "Difficulty Path Viewport"));
                Set(so, "selectionRing", Find(contents,
                    "Difficulty Path Viewport/Difficulty Path Content/Difficulty Selection"));
                Set(so, "detailTitleText", FindText(contents, "Difficulty Selection Detail/Title Strip/Title"));
                Set(so, "rewardStateText", FindText(contents, "Difficulty Selection Detail/Reward State"));
                Set(so, "conditionText", FindText(contents, "Difficulty Selection Detail/Condition"));
                Set(so, "challengeButton", Get<Button>(contents, "Difficulty Challenge"));
                Set(so, "challengeLabel", FindText(contents, "Difficulty Challenge/Label"));
                Set(so, "sweepButton", Get<Button>(contents, "Difficulty Sweep"));

                var buttons = new System.Collections.Generic.List<Button>();
                var cleared = new System.Collections.Generic.List<GameObject>();
                var locked = new System.Collections.Generic.List<GameObject>();
                var available = new System.Collections.Generic.List<GameObject>();
                var plates = new System.Collections.Generic.List<Image>();
                for (var i = 1; i <= 10; i++)
                {
                    var path = "Difficulty Path Viewport/Difficulty Path Content/Difficulty Node " + i;
                    buttons.Add(Get<Button>(contents, path));
                    cleared.Add(Find(contents, path + "/Cleared"));
                    locked.Add(Find(contents, path + "/Locked"));
                    available.Add(Find(contents, path + "/Available"));
                    plates.Add(Get<Image>(contents, path + "/Plate"));
                }
                SetArray(so, "tierButtons", buttons);
                SetArray(so, "tierCleared", cleared);
                SetArray(so, "tierLocked", locked);
                SetArray(so, "tierAvailable", available);
                SetArray(so, "tierPlates", plates);

                var slots = new System.Collections.Generic.List<GameObject>();
                var icons = new System.Collections.Generic.List<Image>();
                var labels = new System.Collections.Generic.List<Text>();
                for (var i = 0; i < 6; i++)
                {
                    var path = "Difficulty Selection Detail/Reward Slot " + i;
                    slots.Add(Find(contents, path));
                    icons.Add(Get<Image>(contents, path + "/Icon"));
                    labels.Add(FindText(contents, path + "/Label"));
                }
                SetArray(so, "rewardSlots", slots);
                SetArray(so, "rewardIcons", icons);
                SetArray(so, "rewardLabels", labels);
                so.ApplyModifiedPropertiesWithoutUndo();

                PrefabUtility.SaveAsPrefabAsset(contents, prefabPath);
                AssetDatabase.SaveAssets();
                Debug.Log("[Difficulty] 已生成并绑定 " + prefabPath);
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
