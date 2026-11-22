using System.Collections.Generic;
using Gxdl.Core;
using Gxdl.Modules;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Gxdl.EditorTools
{
    /// <summary>
    /// 通天古塔 prefab 生成器。
    /// 骨架照 GuxuTowerUI.ShowTower 的坐标还原：左侧攀登列表、右侧分层索引、底部详情；
    /// 当前层号、解锁与首通状态全部由 TowerView 在运行时填入。
    /// </summary>
    [InitializeOnLoad]
    public static class TowerPrefabSource
    {
        public const string PrefabPath = "Assets/Resources/" + UiScreens.Tower + ".prefab";
        const string Root = "Tower Climb Content";
        const string ContentPath = "Tower Climb Viewport/" + Root;
        const string ArtFolder = "Game/Guxu/Expansion/UI/";
        const int FloorCount = 10;

        static TowerPrefabSource()
        {
            ScreenPrefabKit.Register("Tower", Build);
        }

        [MenuItem("Gxdl/UI/生成/通天古塔", priority = 302)]
        public static void Generate()
        {
            var path = Build();
            if (path != null) EditorUtility.DisplayDialog("生成完成", "已生成\n" + path, "确定");
        }

        public static string Build()
        {
            using (var builder = new UiPrefabBuilder("TowerView",
                new Vector2(UiRootLayout.DesignWidth, UiRootLayout.DesignHeight)))
            {
                var root = builder.Root;
                AnchorUtility.Apply(root, AnchorPreset.Stretch, Vector2.zero, Vector2.zero);
                ScreenPrefabKit.AddRootComponents(root);

                // 背景贴图缺失时保留纯色底，生成器不因美术资源未就绪而失败。
                var background = builder.ImageNode("", "Background", 0f, 0f,
                    UiRootLayout.DesignWidth, UiRootLayout.DesignHeight, new Color(0.025f, 0.10f, 0.14f));
                background.raycastTarget = false;
                var skin = Load("Game/Guxu/background");
                if (skin != null)
                {
                    background.sprite = skin;
                    background.color = Color.white;
                }

                BuildHeader(builder);
                BuildClimb(builder);
                BuildIndex(builder);
                BuildDetail(builder);

                var path = builder.Save(PrefabPath);
                AssetDatabase.SaveAssets();
                if (path != null) Bind(path);
                return path;
            }
        }

        static void BuildHeader(UiPrefabBuilder builder)
        {
            var bar = builder.ImageNode("", "Header", 0f, 0f, 750f, 180f, new Color(0.025f, 0.10f, 0.14f, 0.96f), true);
            bar.name = "Header";

            TextButton(builder, "Header", "Back", 14f, 24f, 175f, 52f, "‹ 返回寻墟", 24, 0.15f, 0.38f, 0.35f);

            builder.TextNode("Header", "Title", 200f, 12f, 490f, 63f, "通天古塔", 43, ScreenPrefabKit.Gold);
            builder.TextNode("Header", "Progress", 200f, 78f, 490f, 40f, string.Empty, 28, ScreenPrefabKit.Cream);
            builder.TextNode("Header", "Status", 50f, 130f, 345f, 35f, string.Empty, 24, ScreenPrefabKit.Gold);

            TextButton(builder, "Header", "Reward History", 480f, 122f, 245f, 40f, "奖励记录", 20, 0.15f, 0.38f, 0.35f);
        }

        static void BuildClimb(UiPrefabBuilder builder)
        {
            // 视口高度跟随屏幕，旧实现在 Fit 里按实际高度重算，这里交给锚点拉伸。
            var viewport = builder.AnchoredNode("", "Tower Climb Viewport", AnchorPreset.StretchLeft,
                new Vector2(575f, 220f), new Vector2(0f, 180f));
            viewport.pivot = new Vector2(0f, 1f);
            viewport.offsetMin = new Vector2(0f, 465f);
            var hit = viewport.gameObject.AddComponent<Image>();
            hit.color = Color.clear;
            hit.raycastTarget = true;
            viewport.gameObject.AddComponent<RectMask2D>();

            var content = builder.Node("Tower Climb Viewport", Root, 0f, 0f, 575f, 1950f);
            var scroll = viewport.gameObject.AddComponent<ScrollRect>();
            scroll.viewport = viewport;
            scroll.content = content;
            scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 90f;
            scroll.decelerationRate = 0.06f;

            BuildFloors(builder);
        }

        static void BuildFloors(UiPrefabBuilder builder)
        {
            for (var i = 0; i < FloorCount; i++)
            {
                // 旧代码 index 从下往上递增，列表自上而下是 10 层到 1 层。
                var y = 38f + (FloorCount - 1 - i) * 190f;
                var path = ContentPath + "/Floor " + (i + 1);
                var card = builder.Node(ContentPath, "Floor " + (i + 1), 18f, y, 540f, 190f);
                var hit = card.gameObject.AddComponent<Image>();
                hit.color = new Color(0f, 0f, 0f, 0.001f);
                hit.raycastTarget = true;
                var button = card.gameObject.AddComponent<Button>();
                button.targetGraphic = hit;
                button.transition = Selectable.Transition.None;

                var art = builder.ImageNode(path, "Art", 0f, 0f, 540f, 210.9f, Color.white);
                art.sprite = Load("tower-tier");
                art.preserveAspect = true;

                var glow = builder.ImageNode(path, "Selection Glow", 160f, 162f, 220f, 60f, Color.white);
                glow.sprite = Load("state-selected");
                glow.gameObject.SetActive(false);

                builder.TextNode(path, "Label", 190f, 7f, 160f, 34f, string.Empty, 24, ScreenPrefabKit.Cream);
                var boss = builder.ImageNode(path, "Boss", 226f, 62f, 88f, 93f, Color.white);
                boss.sprite = Load("chapter-node-boss");
                boss.gameObject.SetActive(false);

                var cleared = builder.ImageNode(path, "Cleared", 254f, 105f, 35f, 44f, Color.white);
                cleared.sprite = Load("state-cleared");
                cleared.gameObject.SetActive(false);

                var locked = builder.ImageNode(path, "Lock", 254f, 105f, 35f, 44f, Color.white);
                locked.sprite = Load("state-lock");
                locked.gameObject.SetActive(false);

                var target = builder.TextNode(path, "Target", 198f, 108f, 150f, 34f, "当前目标", 23, ScreenPrefabKit.Gold);
                target.gameObject.SetActive(false);
            }
        }

        static void BuildIndex(UiPrefabBuilder builder)
        {
            var panel = builder.AnchoredNode("", "Tower Segment Index", AnchorPreset.BottomRight,
                new Vector2(150f, 650f), new Vector2(16f, 34f));
            panel.gameObject.AddComponent<Image>().color = new Color(0.055f, 0.12f, 0.16f, 0.96f);
            var path = "Tower Segment Index";

            for (var i = 0; i < FloorCount; i++)
            {
                var marker = builder.Node(path, "Marker " + i, 10f, 16f + (9 - i) * 43f, 130f, 41f);
                var image = marker.gameObject.AddComponent<Image>();
                image.color = new Color(0f, 0f, 0f, 0.001f);
                image.raycastTarget = true;
                marker.gameObject.AddComponent<Button>().targetGraphic = image;
                builder.TextNode(path + "/Marker " + i, "Label", 0f, 0f, 130f, 41f, string.Empty, 22,
                    ScreenPrefabKit.Cream, TextAnchor.MiddleCenter);
            }

            builder.TextNode(path, "Hint", 10f, 455f, 130f, 33f, "滑动攀登", 22, ScreenPrefabKit.Cream, TextAnchor.MiddleCenter);
            builder.TextNode(path, "Segment", 10f, 499f, 130f, 33f, string.Empty, 24, ScreenPrefabKit.Gold, TextAnchor.MiddleCenter);
            SealButton(builder, path, "Previous", 8f, 545f, 134f, 42f, string.Empty);
            SealButton(builder, path, "Next", 8f, 594f, 134f, 42f, string.Empty);
        }

        static void BuildDetail(UiPrefabBuilder builder)
        {
            var panel = builder.AnchoredNode("", "Tower Selection Detail", AnchorPreset.StretchBottom,
                new Vector2(750f, 425f), Vector2.zero);
            panel.pivot = new Vector2(0.5f, 0f);
            var paper = panel.gameObject.AddComponent<Image>();
            paper.color = new Color(0.94f, 0.92f, 0.85f, 0.98f);
            paper.raycastTarget = true;
            var path = "Tower Selection Detail";

            builder.TextNode(path, "Title", 177f, 4f, 396f, 47f, string.Empty, 28, UiFactory.Ink, TextAnchor.MiddleCenter);

            var portrait = builder.ImageNode(path, "Enemy Portrait", 50f, 72f, 146f, 160f, Color.white);
            portrait.sprite = Load("Game/Guxu/Final/guardian");
            portrait.preserveAspect = true;

            builder.TextNode(path, "Enemy Name", 220f, 65f, 455f, 40f, string.Empty, 28, UiFactory.Ink);
            builder.TextNode(path, "Reward", 220f, 117f, 480f, 64f, string.Empty, 23, UiFactory.Ink);
            builder.TextNode(path, "Condition", 60f, 237f, 630f, 38f, string.Empty, 23, UiFactory.Ink, TextAnchor.MiddleCenter);

            TextButton(builder, path, "Growth", 55f, 304f, 240f, 74f, "调整养成", 26, 0.15f, 0.38f, 0.35f);
            // 挑战按钮文案由 View 按解锁状态填写，prefab 里留空避免误导。
            TextButton(builder, path, "Challenge", 327f, 304f, 368f, 74f, string.Empty, 30, 0.86f, 0.78f, 0.54f);
        }

        static RectTransform TextButton(UiPrefabBuilder builder, string parent, string name,
            float x, float y, float width, float height, string label, int size, float r, float g, float b)
        {
            var rect = builder.Node(parent, name, x, y, width, height);
            var image = rect.gameObject.AddComponent<Image>();
            image.color = new Color(r, g, b, 0.94f);
            image.raycastTarget = true;
            rect.gameObject.AddComponent<Button>().targetGraphic = image;
            builder.TextNode(parent + "/" + name, "Label", 0f, 0f, width, height, label, size,
                ScreenPrefabKit.Cream, TextAnchor.MiddleCenter);
            return rect;
        }

        /// <summary>索引页按钮沿用旧实现的金边印章样式。</summary>
        static void SealButton(UiPrefabBuilder builder, string parent, string name,
            float x, float y, float width, float height, string label)
        {
            TextButton(builder, parent, name, x, y, width, height, label, 21, 0.15f, 0.38f, 0.35f);
        }

        static Sprite Load(string key)
        {
            var path = key.StartsWith("Game/") ? key : ArtFolder + key;
            var texture = Resources.Load<Texture2D>(path);
            if (texture == null) return null;
            return Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100f);
        }

        static void Bind(string prefabPath)
        {
            var contents = PrefabUtility.LoadPrefabContents(prefabPath);
            if (contents == null) return;
            try
            {
                var view = contents.GetComponent<TowerView>();
                if (view == null) view = contents.AddComponent<TowerView>();

                var so = new SerializedObject(view);
                Set(so, "backButton", Component<Button>(contents, "Header/Back"));
                Set(so, "backLabel", FindText(contents, "Header/Back/Label"));
                Set(so, "titleText", FindText(contents, "Header/Title"));
                Set(so, "progressText", FindText(contents, "Header/Progress"));
                Set(so, "statusText", FindText(contents, "Header/Status"));
                Set(so, "rewardButton", Component<Button>(contents, "Header/Reward History"));
                Set(so, "segmentLabel", FindText(contents, "Tower Segment Index/Segment"));
                Set(so, "previousButton", Component<Button>(contents, "Tower Segment Index/Previous"));
                Set(so, "previousLabel", FindText(contents, "Tower Segment Index/Previous/Label"));
                Set(so, "nextButton", Component<Button>(contents, "Tower Segment Index/Next"));
                Set(so, "nextLabel", FindText(contents, "Tower Segment Index/Next/Label"));
                Set(so, "floorTitle", FindText(contents, "Tower Selection Detail/Title"));
                Set(so, "enemyPortrait", Component<Image>(contents, "Tower Selection Detail/Enemy Portrait"));
                Set(so, "enemyName", FindText(contents, "Tower Selection Detail/Enemy Name"));
                Set(so, "rewardText", FindText(contents, "Tower Selection Detail/Reward"));
                Set(so, "conditionText", FindText(contents, "Tower Selection Detail/Condition"));
                Set(so, "growthButton", Component<Button>(contents, "Tower Selection Detail/Growth"));
                Set(so, "challengeButton", Component<Button>(contents, "Tower Selection Detail/Challenge"));
                Set(so, "challengeLabel", FindText(contents, "Tower Selection Detail/Challenge/Label"));

                var viewport = Find(contents, "Tower Climb Viewport");
                if (viewport != null) Set(so, "scroll", viewport.GetComponent<ScrollRect>());

                var arts = new List<Image>();
                var glows = new List<Image>();
                var labels = new List<Text>();
                var bosses = new List<Image>();
                var locks = new List<Image>();
                var cleareds = new List<Image>();
                var targets = new List<Text>();
                var markers = new List<Button>();
                var markerLabels = new List<Text>();
                for (var i = 0; i < FloorCount; i++)
                {
                    var floor = ContentPath + "/Floor " + (i + 1);
                    arts.Add(Component<Image>(contents, floor + "/Art"));
                    glows.Add(Component<Image>(contents, floor + "/Selection Glow"));
                    labels.Add(Text(contents, floor + "/Label"));
                    bosses.Add(Component<Image>(contents, floor + "/Boss"));
                    locks.Add(Component<Image>(contents, floor + "/Lock"));
                    cleareds.Add(Component<Image>(contents, floor + "/Cleared"));
                    targets.Add(Text(contents, floor + "/Target"));

                    var marker = "Tower Segment Index/Marker " + i;
                    markers.Add(Component<Button>(contents, marker));
                    markerLabels.Add(Text(contents, marker + "/Label"));
                }

                SetArray(so, "floorArts", arts);
                SetArray(so, "floorGlows", glows);
                SetArray(so, "floorLabels", labels);
                SetArray(so, "floorBosses", bosses);
                SetArray(so, "floorLocks", locks);
                SetArray(so, "floorCleareds", cleareds);
                SetArray(so, "floorTargets", targets);
                SetArray(so, "markerButtons", markers);
                SetArray(so, "markerLabels", markerLabels);
                so.ApplyModifiedPropertiesWithoutUndo();

                PrefabUtility.SaveAsPrefabAsset(contents, prefabPath);
                AssetDatabase.SaveAssets();
                Debug.Log("[Tower] 已生成并绑定 " + prefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contents);
            }
        }

        static Transform Find(GameObject root, string path) => root.transform.Find(path);

        static T Component<T>(GameObject root, string path) where T : Component
        {
            var node = Find(root, path);
            return node != null ? node.GetComponent<T>() : null;
        }

        static Text Text(GameObject root, string path) => Component<Text>(root, path);

        static Text FindText(GameObject root, string path) => Text(root, path);

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
