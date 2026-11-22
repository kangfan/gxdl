using System.Collections.Generic;
using Gxdl.Modules;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Gxdl.EditorTools
{
    /// <summary>
    /// 秘境详情 prefab 生成器。十层难度节点的坐标按旧版难度路径预先摆好，
    /// 可用 / 通关 / 选中状态与底部详情由 DungeonDetailView.Apply 按存档填入。
    /// </summary>
    [InitializeOnLoad]
    public static class DungeonDetailPrefabSource
    {
        public const string PrefabPath = "Assets/Resources/" + UiScreens.DungeonDetail + ".prefab";

        const float HeaderHeight = 180f;
        const float DetailHeight = 425f;
        const float ContentHeight = 808f;

        // 十层节点中心坐标，照旧版难度路径点位。
        static readonly Vector2[] Platforms =
        {
            new Vector2(364f, 694f), new Vector2(538f, 590f), new Vector2(202f, 510f),
            new Vector2(547f, 436f), new Vector2(205f, 372f), new Vector2(517f, 307f),
            new Vector2(244f, 250f), new Vector2(489f, 192f), new Vector2(281f, 141f),
            new Vector2(446f, 91f)
        };

        static readonly Color Ink = new Color(0.12f, 0.25f, 0.28f);

        static DungeonDetailPrefabSource()
        {
            ScreenPrefabKit.Register("DungeonDetail", Build);
        }

        [MenuItem("Gxdl/UI/生成/秘境详情", priority = 304)]
        public static void Generate()
        {
            var path = Build();
            if (path != null) EditorUtility.DisplayDialog("生成完成", "已生成\n" + path, "确定");
        }

        public static string Build()
        {
            using (var builder = new UiPrefabBuilder("DungeonDetailView",
                new Vector2(UiRootLayout.DesignWidth, UiRootLayout.DesignHeight)))
            {
                var root = builder.Root;
                AnchorUtility.Apply(root, AnchorPreset.Stretch, Vector2.zero, Vector2.zero);
                ScreenPrefabKit.AddRootComponents(root);

                var background = builder.AnchoredNode("", "Background", AnchorPreset.Stretch, Vector2.zero)
                    .gameObject.AddComponent<Image>();
                background.color = new Color(0.035f, 0.13f, 0.18f);
                background.raycastTarget = false;

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
            // 视口夹在头部 180 与底部详情 425 之间。
            var viewport = builder.AnchoredNode("", "Difficulty Path Viewport", AnchorPreset.Stretch,
                new Vector2(0f, -HeaderHeight - DetailHeight), new Vector2(0f, HeaderHeight));
            viewport.offsetMin = new Vector2(0f, DetailHeight);
            viewport.offsetMax = new Vector2(0f, -HeaderHeight);

            var hit = viewport.gameObject.AddComponent<Image>();
            hit.color = Color.clear;
            hit.raycastTarget = true;
            viewport.gameObject.AddComponent<RectMask2D>();

            var content = builder.Node("Difficulty Path Viewport", "Difficulty Path Content", 0f, 0f, 750f, ContentHeight);
            var scroll = viewport.gameObject.AddComponent<ScrollRect>();
            scroll.viewport = viewport;
            scroll.content = content;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 75f;
            scroll.decelerationRate = 0.06f;

            // 未开层遮罩：高度在运行时按最后一个可达层设置。
            var shade = builder.Node("Difficulty Path Viewport/Difficulty Path Content", "Locked Shade", 0f, 0f, 750f, ContentHeight);
            shade.gameObject.AddComponent<Image>().color = new Color(0.015f, 0.045f, 0.065f, 0.52f);
            shade.gameObject.SetActive(false);

            for (var i = 0; i < 10; i++) BuildNode(builder, i + 1);
        }

        static void BuildNode(UiPrefabBuilder builder, int tier)
        {
            var point = Platforms[tier - 1];
            var width = tier <= 2 ? 140f : tier <= 5 ? 120f : 104f;
            var height = width * 0.75f;
            // 节点底部对齐平台点下方一点，与旧版锚点公式一致。
            var left = point.x - width * 0.5f;
            var top = point.y - height * 0.875f;
            var parent = "Difficulty Path Viewport/Difficulty Path Content";
            var name = "Node " + tier;

            var button = builder.Node(parent, name, left, top, width, height + 10f);
            var buttonImage = button.gameObject.AddComponent<Image>();
            buttonImage.color = new Color(0f, 0f, 0f, 0.001f);
            buttonImage.raycastTarget = true;
            var selectable = button.gameObject.AddComponent<Button>();
            selectable.targetGraphic = buttonImage;
            selectable.transition = Selectable.Transition.None;

            var plate = builder.ImageNode(parent + "/" + name, "Plate", 0f, 0f, width, height,
                new Color(0.28f, 0.40f, 0.42f, 0.95f));
            plate.name = "Plate";

            var ring = builder.Node(parent + "/" + name, "Selection", -8f, -8f, width + 16f, height + 16f);
            ring.gameObject.AddComponent<Image>().color = new Color(1f, 0.78f, 0.31f, 0.55f);
            ring.gameObject.SetActive(false);

            builder.TextNode(parent + "/" + name, "Number", 12f, height * (tier <= 2 ? 0.15f : 0.10f),
                width - 24f, tier <= 2 ? 44f : 36f, tier.ToString(), tier <= 2 ? 32 : 27,
                ScreenPrefabKit.Cream, TextAnchor.MiddleCenter);

            builder.TextNode(parent + "/" + name, "State", width * 0.5f - 14f, height - (tier <= 2 ? 44f : 34f),
                28f, 30f, string.Empty, 18, ScreenPrefabKit.Cream, TextAnchor.MiddleCenter);
        }

        static void BuildHeader(UiPrefabBuilder builder)
        {
            var header = builder.Node("", "Header", 0f, 0f, 750f, HeaderHeight);
            header.gameObject.AddComponent<Image>().color = new Color(0.025f, 0.10f, 0.14f);

            var back = builder.Node("Header", "Back", 14f, 22f, 177f, 52f);
            var backImage = back.gameObject.AddComponent<Image>();
            backImage.color = new Color(0.15f, 0.38f, 0.35f, 0.94f);
            backImage.raycastTarget = true;
            back.gameObject.AddComponent<Button>().targetGraphic = backImage;
            builder.TextNode("Header/Back", "Label", 0f, 0f, 177f, 52f, "‹ 返回秘境", 24, ScreenPrefabKit.Cream, TextAnchor.MiddleCenter);

            builder.TextNode("Header", "Name", 193f, 19f, 400f, 63f, string.Empty, 43, ScreenPrefabKit.Gold);
            builder.ImageNode("Header", "Portal", 633f, 8f, 100f, 100f, new Color(0.10f, 0.22f, 0.28f, 0.95f));
            builder.TextNode("Header", "Attempts", 60f, 95f, 630f, 34f, string.Empty, 25, ScreenPrefabKit.Cream);
            builder.TextNode("Header", "Hint", 40f, 137f, 420f, 30f, "已通关难度可速通", 22, ScreenPrefabKit.Cream);

            var history = builder.Node("Header", "History", 495f, 127f, 225f, 44f);
            var historyImage = history.gameObject.AddComponent<Image>();
            historyImage.color = new Color(0.15f, 0.38f, 0.35f, 0.0f);
            historyImage.raycastTarget = true;
            history.gameObject.AddComponent<Button>().targetGraphic = historyImage;
            builder.TextNode("Header/History", "Label", 0f, 0f, 225f, 44f, "收益记录", 22, ScreenPrefabKit.Cream, TextAnchor.MiddleCenter);
        }

        static void BuildDetail(UiPrefabBuilder builder)
        {
            var detail = builder.AnchoredNode("", "Detail", AnchorPreset.StretchBottom,
                new Vector2(0f, DetailHeight), Vector2.zero);
            detail.sizeDelta = new Vector2(0f, DetailHeight);
            detail.gameObject.AddComponent<Image>().color = new Color(0.055f, 0.12f, 0.16f, 0.96f);

            builder.ImageNode("Detail", "Plate", 16f, 0f, 718f, 415f, new Color(0.06f, 0.14f, 0.17f, 0.98f));
            builder.TextNode("Detail", "Title", 180f, 14f, 390f, 46f, string.Empty, 28, ScreenPrefabKit.Cream, TextAnchor.MiddleCenter);
            builder.TextNode("Detail", "Reward State", 60f, 62f, 630f, 36f, string.Empty, 26, Ink);

            for (var i = 0; i < 4; i++)
            {
                var slot = builder.Node("Detail", "Reward " + i, 61f + i * 157f, 112f, 150f, 47f);
                builder.ImageNode("Detail/Reward " + i, "Icon", 0f, 0f, 47f, 47f, new Color(0.66f, 0.52f, 0.28f, 0.9f));
                builder.TextNode("Detail/Reward " + i, "Value", 50f, 0f, 100f, 47f, string.Empty, 23, Ink);
                slot.gameObject.SetActive(false);
            }

            builder.TextNode("Detail", "Condition", 55f, 208f, 640f, 52f, string.Empty, 23, Ink);

            var challenge = builder.Node("Detail", "Challenge", 68f, 292f, 345f, 80f);
            var challengeImage = challenge.gameObject.AddComponent<Image>();
            challengeImage.color = new Color(0.15f, 0.38f, 0.35f, 0.94f);
            challengeImage.raycastTarget = true;
            challenge.gameObject.AddComponent<Button>().targetGraphic = challengeImage;
            builder.TextNode("Detail/Challenge", "Label", 0f, 0f, 345f, 80f, "挑战", 32, ScreenPrefabKit.Cream, TextAnchor.MiddleCenter);

            var sweep = builder.Node("Detail", "Sweep", 443f, 292f, 235f, 80f);
            var sweepImage = sweep.gameObject.AddComponent<Image>();
            sweepImage.color = new Color(0.42f, 0.34f, 0.16f, 0.94f);
            sweepImage.raycastTarget = true;
            sweep.gameObject.AddComponent<Button>().targetGraphic = sweepImage;
            builder.TextNode("Detail/Sweep", "Label", 0f, 0f, 235f, 80f, "速通", 32, ScreenPrefabKit.Cream, TextAnchor.MiddleCenter);
        }

        static void Bind(string prefabPath)
        {
            var contents = PrefabUtility.LoadPrefabContents(prefabPath);
            if (contents == null) return;
            try
            {
                var view = contents.GetComponent<DungeonDetailView>();
                if (view == null) view = contents.AddComponent<DungeonDetailView>();

                var so = new SerializedObject(view);
                Set(so, "backButton", Find(contents, "Header/Back")?.GetComponent<Button>());
                Set(so, "nameText", Find(contents, "Header/Name")?.GetComponent<Text>());
                Set(so, "attemptsText", Find(contents, "Header/Attempts")?.GetComponent<Text>());
                Set(so, "historyButton", Find(contents, "Header/History")?.GetComponent<Button>());
                Set(so, "scroll", Find(contents, "Difficulty Path Viewport")?.GetComponent<ScrollRect>());
                Set(so, "content", Find(contents, "Difficulty Path Viewport/Difficulty Path Content"));
                Set(so, "lockedShade", Find(contents, "Difficulty Path Viewport/Difficulty Path Content/Locked Shade")?.GetComponent<Image>());
                Set(so, "detailTitleText", Find(contents, "Detail/Title")?.GetComponent<Text>());
                Set(so, "detailRewardStateText", Find(contents, "Detail/Reward State")?.GetComponent<Text>());
                Set(so, "conditionText", Find(contents, "Detail/Condition")?.GetComponent<Text>());
                Set(so, "challengeButton", Find(contents, "Detail/Challenge")?.GetComponent<Button>());
                Set(so, "challengeText", Find(contents, "Detail/Challenge/Label")?.GetComponent<Text>());
                Set(so, "sweepButton", Find(contents, "Detail/Sweep")?.GetComponent<Button>());

                var buttons = new List<Button>();
                var plates = new List<Image>();
                var selections = new List<GameObject>();
                var numbers = new List<Text>();
                var states = new List<Text>();
                for (var tier = 1; tier <= 10; tier++)
                {
                    var path = "Difficulty Path Viewport/Difficulty Path Content/Node " + tier;
                    buttons.Add(Find(contents, path)?.GetComponent<Button>());
                    plates.Add(Find(contents, path + "/Plate")?.GetComponent<Image>());
                    selections.Add(Find(contents, path + "/Selection")?.gameObject);
                    numbers.Add(Find(contents, path + "/Number")?.GetComponent<Text>());
                    states.Add(Find(contents, path + "/State")?.GetComponent<Text>());
                }

                SetArray(so, "tierButtons", buttons);
                SetArray(so, "tierPlates", plates);
                SetArray(so, "tierSelections", selections);
                SetArray(so, "tierNumbers", numbers);
                SetArray(so, "tierStates", states);

                var slots = new List<GameObject>();
                var slotLabels = new List<Text>();
                for (var i = 0; i < 4; i++)
                {
                    slots.Add(Find(contents, "Detail/Reward " + i)?.gameObject);
                    slotLabels.Add(Find(contents, "Detail/Reward " + i + "/Value")?.GetComponent<Text>());
                }

                SetArray(so, "detailSlots", slots);
                SetArray(so, "detailSlotLabels", slotLabels);
                so.ApplyModifiedPropertiesWithoutUndo();

                PrefabUtility.SaveAsPrefabAsset(contents, prefabPath);
                AssetDatabase.SaveAssets();
                Debug.Log("[DungeonDetail] 已生成并绑定 " + prefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contents);
            }
        }

        static Transform Find(GameObject root, string path)
        {
            return root.transform.Find(path);
        }

        static void Set(SerializedObject so, string field, Object value)
        {
            var property = so.FindProperty(field);
            if (property == null || value == null) return;
            property.objectReferenceValue = value;
        }

        static void SetArray<T>(SerializedObject so, string field, List<T> values) where T : Object
        {
            var property = so.FindProperty(field);
            if (property == null) return;
            property.arraySize = values.Count;
            for (var i = 0; i < values.Count; i++) property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
        }
    }
}
