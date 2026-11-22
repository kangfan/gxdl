using System.Collections.Generic;
using Gxdl.Modules;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Gxdl.EditorTools
{
    /// <summary>
    /// 章节地图 prefab 生成器。20 个关卡节点的坐标按 ChapterMapView.PointOf 预先摆好，
    /// 关卡文字、三种状态与底部详情由 ChapterMapView.Apply 在运行时按存档填入。
    /// </summary>
    [InitializeOnLoad]
    public static class ChapterMapPrefabSource
    {
        public const string PrefabPath = "Assets/Resources/" + UiScreens.ChapterMap + ".prefab";

        const float ContentHeight = 2800f;
        const float HeaderHeight = 230f;
        const float DetailHeight = 394f;

        static readonly Color Scenery = new Color(0.055f, 0.16f, 0.23f);
        static readonly Color Ink = new Color(0.12f, 0.25f, 0.28f);

        static ChapterMapPrefabSource()
        {
            ScreenPrefabKit.Register("ChapterMap", Build);
        }

        [MenuItem("Gxdl/UI/生成/章节地图", priority = 302)]
        public static void Generate()
        {
            var path = Build();
            if (path != null) EditorUtility.DisplayDialog("生成完成", "已生成\n" + path, "确定");
        }

        public static string Build()
        {
            using (var builder = new UiPrefabBuilder("ChapterMapView",
                new Vector2(UiRootLayout.DesignWidth, UiRootLayout.DesignHeight)))
            {
                var root = builder.Root;
                AnchorUtility.Apply(root, AnchorPreset.Stretch, Vector2.zero, Vector2.zero);
                ScreenPrefabKit.AddRootComponents(root);

                var background = builder.AnchoredNode("", "Background", AnchorPreset.Stretch, Vector2.zero)
                    .gameObject.AddComponent<Image>();
                background.color = new Color(0.015f, 0.045f, 0.065f);
                background.raycastTarget = false;

                BuildMap(builder);
                BuildHeader(builder);
                BuildDetail(builder);

                var path = builder.Save(PrefabPath);
                AssetDatabase.SaveAssets();
                if (path != null) Bind(path);
                return path;
            }
        }

        static void BuildMap(UiPrefabBuilder builder)
        {
            // 视口在头部与详情之间四向拉伸，高度随屏幕变化，内容高度固定为四段地形。
            var viewport = builder.AnchoredNode("", "Chapter Map Viewport", AnchorPreset.Stretch,
                new Vector2(0f, -HeaderHeight - DetailHeight), new Vector2(0f, HeaderHeight));
            viewport.offsetMin = new Vector2(0f, DetailHeight);
            viewport.offsetMax = new Vector2(0f, -HeaderHeight);

            var hit = viewport.gameObject.AddComponent<Image>();
            hit.color = Color.clear;
            hit.raycastTarget = true;
            viewport.gameObject.AddComponent<RectMask2D>();

            var content = builder.Node("Chapter Map Viewport", "Chapter Map Content", 0f, 0f, 750f, ContentHeight);
            var scroll = viewport.gameObject.AddComponent<ScrollRect>();
            scroll.viewport = viewport;
            scroll.content = content;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 90f;
            scroll.decelerationRate = 0.06f;

            for (var tile = 0; tile < 4; tile++)
            {
                var terrain = builder.ImageNode("Chapter Map Viewport/Chapter Map Content",
                    "Chapter Terrain " + tile, 0f, 45f + tile * 720f, 750f, 720f, Scenery);
                terrain.name = "Chapter Terrain " + tile;
            }

            for (var i = 0; i < 20; i++) BuildNode(builder, i);
        }

        static void BuildNode(UiPrefabBuilder builder, int index)
        {
            var point = ChapterMapView.PointOf(index);
            var boss = index % 20 == 19;
            var elite = !boss && (index + 1) % 5 == 0;
            var width = boss ? 210f : elite ? 146f : 120f;
            var height = boss ? 210f : elite ? 183f : 120f;
            var hitWidth = boss ? 240f : elite ? 224f : 158f;
            var parent = "Chapter Map Viewport/Chapter Map Content";
            var name = "Stage " + (index + 1);

            // 按钮中心对齐平台点，上部留 46px 给状态标记。
            var button = builder.Node(parent, name,
                point.x - hitWidth * 0.5f, point.y + 8f - height, hitWidth, height + 46f);
            var buttonImage = button.gameObject.AddComponent<Image>();
            buttonImage.color = new Color(0f, 0f, 0f, 0.001f);
            buttonImage.raycastTarget = true;
            var selectable = button.gameObject.AddComponent<Button>();
            selectable.targetGraphic = buttonImage;
            selectable.transition = Selectable.Transition.None;
            selectable.name = name;

            var glowWidth = elite || boss ? 270f : 180f;
            var glow = builder.ImageNode(parent + "/" + name, "Glow", (hitWidth - glowWidth) * 0.5f, height - 48f,
                glowWidth, 88f, new Color(1f, 0.74f, 0.2f, 0.85f));
            glow.name = "Glow";

            // 无美术资源时用色块占位，运行时只改颜色区分锁定 / 通关 / 开放。
            var artColor = boss ? new Color(0.45f, 0.16f, 0.14f, 0.95f)
                : elite ? new Color(0.28f, 0.30f, 0.20f, 0.95f)
                : new Color(0.32f, 0.40f, 0.42f, 0.95f);
            var art = builder.ImageNode(parent + "/" + name, "Art", (hitWidth - width) * 0.5f, 0f, width, height, artColor);
            art.name = "Art";
            art.gameObject.AddComponent<Outline>().enabled = false;

            if (!elite && !boss)
            {
                builder.TextNode(parent + "/" + name, "Index", 0f, 21f, hitWidth, 40f,
                    "第" + (index + 1) + "关", 30, ScreenPrefabKit.Cream, TextAnchor.MiddleCenter);
            }

            var labelTop = elite || boss ? height - 5f : height - 28f;
            var labelHeight = elite || boss ? 39f : 28f;
            var labelLeft = elite || boss ? 4f : 19f;
            var labelWidth = elite || boss ? hitWidth - 8f : 120f;
            if (elite || boss)
            {
                builder.ImageNode(parent + "/" + name, "Plate", 0f, height - 5f, hitWidth, 39f,
                    new Color(0.05f, 0.09f, 0.08f, 0.92f));
            }

            builder.TextNode(parent + "/" + name, "Label", labelLeft, labelTop, labelWidth, labelHeight,
                string.Empty, elite || boss ? 27 : 21, ScreenPrefabKit.Cream, TextAnchor.MiddleCenter);

            var markX = elite || boss ? hitWidth - 35f : hitWidth * 0.5f - 16f;
            var markY = elite || boss ? height - 43f : 57f;
            var mark = builder.TextNode(parent + "/" + name, "Mark", markX, markY, 32f, 35f,
                string.Empty, 22, ScreenPrefabKit.Cream, TextAnchor.MiddleCenter);
            mark.gameObject.SetActive(false);

            var current = builder.Node(parent + "/" + name, "Current", -32f, height - 97f, 103f, 47f);
            current.gameObject.AddComponent<Image>().color = new Color(1f, 0.87f, 0.57f, 0.95f);
            builder.TextNode(parent + "/" + name + "/Current", "Label", 0f, 0f, 103f, 47f, "当前", 26,
                new Color(0.30f, 0.15f, 0.025f), TextAnchor.MiddleCenter);
            current.gameObject.SetActive(false);
        }

        static void BuildHeader(UiPrefabBuilder builder)
        {
            var header = builder.Node("", "Header", 0f, 0f, 750f, HeaderHeight);
            header.gameObject.AddComponent<Image>().color = new Color(0.035f, 0.13f, 0.2f);

            var back = builder.Node("Header", "Back", 16f, 22f, 175f, 52f);
            var backImage = back.gameObject.AddComponent<Image>();
            backImage.color = new Color(0.15f, 0.38f, 0.35f, 0.94f);
            backImage.raycastTarget = true;
            back.gameObject.AddComponent<Button>().targetGraphic = backImage;
            builder.TextNode("Header/Back", "Label", 0f, 0f, 175f, 52f, "‹ 十域地图", 24, ScreenPrefabKit.Cream, TextAnchor.MiddleCenter);

            builder.TextNode("Header", "Name", 205f, 10f, 520f, 64f, string.Empty, 43, new Color(1f, 0.87f, 0.57f));
            builder.TextNode("Header", "Story", 100f, 76f, 600f, 38f, string.Empty, 26, ScreenPrefabKit.Cream, TextAnchor.MiddleCenter);
            builder.ImageNode("Header", "Progress Track", 70f, 194f, 537f, 3f, new Color(0.41f, 0.55f, 0.56f));

            var fill = builder.Node("Header", "Progress Fill", 70f, 194f, 537f, 3f);
            var fillImage = fill.gameObject.AddComponent<Image>();
            fillImage.color = new Color(1f, 0.82f, 0.45f);
            fillImage.raycastTarget = false;
            // 只改水平 anchorMax 会让宽度塌成 0，这里显式清掉。
            fill.anchorMax = new Vector2(0f, 1f);
            fill.sizeDelta = new Vector2(537f, 3f);

            builder.TextNode("Header", "Progress", 250f, 114f, 250f, 42f, string.Empty, 27, ScreenPrefabKit.Cream, TextAnchor.MiddleCenter);

            // 四个里程碑对应第 5 / 10 / 15 / 20 关。
            for (var i = 0; i < 4; i++)
            {
                var room = (i + 1) * 5;
                var x = 70f + 537f * room / 20f - 41f;
                builder.ImageNode("Header", "Milestone " + i, x + 14f, 150f, 54f, 54f,
                    new Color(0.52f, 0.67f, 0.69f, 0.9f));
                builder.TextNode("Header", "Milestone " + i + " Label", x - 5f, 199f, 92f, 30f,
                    room + "关", 23, ScreenPrefabKit.Cream, TextAnchor.MiddleCenter);
            }

            var chest = builder.Node("Header", "Chest", 653f, 145f, 82f, 79f);
            var chestImage = chest.gameObject.AddComponent<Image>();
            chestImage.color = new Color(0.05f, 0.09f, 0.08f, 0.0f);
            chestImage.raycastTarget = true;
            chest.gameObject.AddComponent<Button>().targetGraphic = chestImage;
            builder.ImageNode("Header/Chest", "Icon", 12f, 0f, 58f, 58f, new Color(0.66f, 0.52f, 0.28f));
            builder.TextNode("Header/Chest", "State", -13f, 56f, 108f, 26f, string.Empty, 21, ScreenPrefabKit.Cream, TextAnchor.MiddleCenter);
        }

        static void BuildDetail(UiPrefabBuilder builder)
        {
            // 详情面板贴底固定高度，地图视口的高度差额全部由它上方的地图吸收。
            var detail = builder.AnchoredNode("", "Detail", AnchorPreset.StretchBottom,
                new Vector2(0f, DetailHeight), Vector2.zero);
            detail.sizeDelta = new Vector2(0f, DetailHeight);
            detail.gameObject.AddComponent<Image>().color = new Color(0.055f, 0.12f, 0.16f, 0.96f);

            builder.ImageNode("Detail", "Plate", 16f, 0f, 718f, 384f, new Color(0.06f, 0.14f, 0.17f, 0.98f));

            builder.TextNode("Detail", "Title", 182f, 10f, 386f, 57f, string.Empty, 33, ScreenPrefabKit.Cream, TextAnchor.MiddleCenter);
            builder.TextNode("Detail", "Enemy", 45f, 59f, 660f, 39f, string.Empty, 27, Ink);
            builder.TextNode("Detail", "Reward State", 45f, 102f, 660f, 31f, string.Empty, 24, Ink);

            for (var i = 0; i < 5; i++)
            {
                var slot = builder.Node("Detail", "Reward " + i, 65f + i * 120f, 139f, 112f, 47f);
                builder.ImageNode("Detail/Reward " + i, "Icon", 0f, 0f, 47f, 47f, new Color(0.66f, 0.52f, 0.28f, 0.9f));
                builder.TextNode("Detail/Reward " + i, "Value", 50f, 0f, 62f, 47f, string.Empty, 24, Ink);
                slot.gameObject.SetActive(false);
            }

            var challenge = builder.Node("Detail", "Challenge", 238f, 282f, 444f, 80f);
            var challengeImage = challenge.gameObject.AddComponent<Image>();
            challengeImage.color = new Color(0.15f, 0.38f, 0.35f, 0.94f);
            challengeImage.raycastTarget = true;
            challenge.gameObject.AddComponent<Button>().targetGraphic = challengeImage;
            builder.TextNode("Detail/Challenge", "Label", 0f, 0f, 444f, 80f, "挑战", 32, ScreenPrefabKit.Cream, TextAnchor.MiddleCenter);

            var growth = builder.Node("Detail", "Growth", 42f, 290f, 174f, 64f);
            var growthImage = growth.gameObject.AddComponent<Image>();
            growthImage.color = new Color(0.06f, 0.14f, 0.17f, 0.0f);
            growthImage.raycastTarget = true;
            growth.gameObject.AddComponent<Button>().targetGraphic = growthImage;
            builder.TextNode("Detail/Growth", "Label", 0f, 0f, 174f, 64f, "修行养成 ›", 26, Ink, TextAnchor.MiddleCenter);

            // 提示浮在详情面板顶边上方，由 View 依据滚动位置显示 / 隐藏。
            var hint = builder.Node("", "Scroll Hint", 0f, 0f, 230f, 34f);
            builder.TextNode("Scroll Hint", "Label", 0f, 0f, 230f, 34f, "滑动探索  ▾", 24,
                new Color(0.12f, 0.25f, 0.28f), TextAnchor.MiddleCenter).gameObject
                .AddComponent<Shadow>().effectColor = Color.white;
            hint.anchorMin = new Vector2(0f, 0f);
            hint.anchorMax = new Vector2(0f, 0f);
            hint.pivot = new Vector2(0f, 0f);
            hint.anchoredPosition = new Vector2(260f, DetailHeight + 34f);
        }

        static void Bind(string prefabPath)
        {
            var contents = PrefabUtility.LoadPrefabContents(prefabPath);
            if (contents == null) return;
            try
            {
                var view = contents.GetComponent<ChapterMapView>();
                if (view == null) view = contents.AddComponent<ChapterMapView>();

                var so = new SerializedObject(view);
                Set(so, "backButton", Find(contents, "Header/Back")?.GetComponent<Button>());
                Set(so, "chapterNameText", FindText(contents, "Header/Name"));
                Set(so, "chapterStoryText", FindText(contents, "Header/Story"));
                Set(so, "chapterProgressText", FindText(contents, "Header/Progress"));
                Set(so, "progressFill", Find(contents, "Header/Progress Fill"));
                Set(so, "claimButton", Find(contents, "Header/Chest")?.GetComponent<Button>());
                Set(so, "chestStateText", FindText(contents, "Header/Chest/State"));
                Set(so, "scroll", Find(contents, "Chapter Map Viewport")?.GetComponent<ScrollRect>());
                Set(so, "content", Find(contents, "Chapter Map Viewport/Chapter Map Content"));
                Set(so, "detailRoot", Find(contents, "Detail"));
                Set(so, "detailTitleText", FindText(contents, "Detail/Title"));
                Set(so, "detailEnemyText", FindText(contents, "Detail/Enemy"));
                Set(so, "detailRewardStateText", FindText(contents, "Detail/Reward State"));
                Set(so, "challengeButton", Find(contents, "Detail/Challenge")?.GetComponent<Button>());
                Set(so, "challengeText", FindText(contents, "Detail/Challenge/Label"));
                Set(so, "growthButton", Find(contents, "Detail/Growth")?.GetComponent<Button>());
                Set(so, "hintRoot", Find(contents, "Scroll Hint")?.gameObject);

                var milestones = new List<Text>();
                for (var i = 0; i < 4; i++) milestones.Add(FindText(contents, "Header/Milestone " + i + " Label"));
                SetArray(so, "milestoneLabels", milestones);

                var terrain = new List<Image>();
                for (var i = 0; i < 4; i++) terrain.Add(ImageOf(contents, "Chapter Map Viewport/Chapter Map Content/Chapter Terrain " + i));
                SetArray(so, "terrainImages", terrain);

                var buttons = new List<Button>();
                var glows = new List<Image>();
                var outlines = new List<Outline>();
                var arts = new List<Image>();
                var labels = new List<Text>();
                var marks = new List<Text>();
                var currents = new List<GameObject>();
                for (var i = 1; i <= 20; i++)
                {
                    var path = "Chapter Map Viewport/Chapter Map Content/Stage " + i;
                    var node = Find(contents, path);
                    buttons.Add(node?.GetComponent<Button>());
                    glows.Add(ImageOf(contents, path + "/Glow"));
                    arts.Add(ImageOf(contents, path + "/Art"));
                    outlines.Add(Find(contents, path + "/Art")?.GetComponent<Outline>());
                    labels.Add(FindText(contents, path + "/Label"));
                    marks.Add(FindText(contents, path + "/Mark"));
                    currents.Add(Find(contents, path + "/Current")?.gameObject);
                }

                SetArray(so, "nodeButtons", buttons);
                SetArray(so, "nodeGlows", glows);
                SetArray(so, "nodeOutlines", outlines);
                SetArray(so, "nodeArts", arts);
                SetArray(so, "nodeLabels", labels);
                SetArray(so, "nodeMarks", marks);
                SetArray(so, "nodeCurrentMarks", currents);

                var slots = new List<GameObject>();
                var slotLabels = new List<Text>();
                for (var i = 0; i < 5; i++)
                {
                    slots.Add(Find(contents, "Detail/Reward " + i)?.gameObject);
                    slotLabels.Add(FindText(contents, "Detail/Reward " + i + "/Value"));
                }

                SetArray(so, "detailSlots", slots);
                SetArray(so, "detailSlotLabels", slotLabels);
                so.ApplyModifiedPropertiesWithoutUndo();

                PrefabUtility.SaveAsPrefabAsset(contents, prefabPath);
                AssetDatabase.SaveAssets();
                Debug.Log("[ChapterMap] 已生成并绑定 " + prefabPath);
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

        static Text FindText(GameObject root, string path) => Find(root, path)?.GetComponent<Text>();

        static Image ImageOf(GameObject root, string path) => Find(root, path)?.GetComponent<Image>();

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
