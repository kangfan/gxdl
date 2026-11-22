using System.Collections.Generic;
using Gxdl.Modules;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Gxdl.EditorTools
{
    /// <summary>
    /// 世界地图 prefab 生成器。
    /// 骨架照 GuxuExpansionUI.ShowWorldMap 的布局还原，
    /// 十张章节卡片的文字与解锁状态由 WorldMapView.Apply 在运行时填入。
    /// </summary>
    [InitializeOnLoad]
    public static class WorldMapPrefabSource
    {
        public const string PrefabPath = "Assets/Resources/" + UiScreens.WorldMap + ".prefab";

        static readonly float[] RowY = { 257f, 420f, 582f, 742f, 901f };

        static WorldMapPrefabSource()
        {
            ScreenPrefabKit.Register("WorldMap", Build);
        }

        [MenuItem("Gxdl/UI/生成/世界地图", priority = 301)]
        public static void Generate()
        {
            var path = Build();
            if (path != null) EditorUtility.DisplayDialog("生成完成", "已生成\n" + path, "确定");
        }

        public static string Build()
        {
            using (var builder = new UiPrefabBuilder("WorldMapView",
                new Vector2(UiRootLayout.DesignWidth, UiRootLayout.DesignHeight)))
            {
                var root = builder.Root;
                AnchorUtility.Apply(root, AnchorPreset.Stretch, Vector2.zero, Vector2.zero);
                ScreenPrefabKit.AddRootComponents(root);

                var background = builder.AnchoredNode("", "Background", AnchorPreset.Stretch, Vector2.zero)
                    .gameObject.AddComponent<Image>();
                background.color = new Color(0.015f, 0.045f, 0.065f);
                background.raycastTarget = false;

                BuildHeader(builder);
                BuildChapters(builder);
                BuildBottom(builder);

                var path = builder.Save(PrefabPath);
                AssetDatabase.SaveAssets();
                if (path != null) Bind(path);
                return path;
            }
        }

        static void BuildHeader(UiPrefabBuilder builder)
        {
            var backNode = builder.AnchoredNode("", "Header Back", AnchorPreset.TopLeft, new Vector2(136f, 46f), new Vector2(18f, 21f));
            var backImage = backNode.gameObject.AddComponent<Image>();
            backImage.color = new Color(0.15f, 0.38f, 0.35f, 0.94f);
            backImage.raycastTarget = true;
            backNode.gameObject.AddComponent<Button>().targetGraphic = backImage;
            builder.TextNode("Header Back", "Label", 4f, 0f, 132f, 46f, "‹ 古墟", 22, ScreenPrefabKit.Cream, TextAnchor.MiddleCenter);

            builder.TextNode("", "World Title", 65f, 22f, 76f, 43f, "古墟", 27, ScreenPrefabKit.Cream);
            builder.TextNode("", "World Progress", 115f, 149f, 194f, 36f, string.Empty, 20, ScreenPrefabKit.Cream);
            builder.TextNode("", "Chapter Hint", 350f, 149f, 125f, 36f, "每章20关", 20, ScreenPrefabKit.Cream);
            builder.TextNode("", "Elite Hint", 530f, 149f, 166f, 36f, "精英与首领交替", 18, ScreenPrefabKit.Cream);
        }

        static void BuildChapters(UiPrefabBuilder builder)
        {
            var cardRoot = builder.Node("", "Chapter Cards", 0f, 200f, UiRootLayout.DesignWidth, 960f);
            cardRoot.name = "Chapter Cards";

            for (var i = 0; i < 10; i++)
            {
                var x = i % 2 == 0 ? 27f : 385f;
                var y = RowY[i / 2] - 200f;
                var card = builder.Node("Chapter Cards", "Chapter " + (i + 1), x, y, 340f, 148f);
                card.gameObject.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.001f);

                var lockImage = builder.ImageNode("Chapter Cards/Chapter " + (i + 1), "Lock", 3f, 3f, 334f, 140f,
                    new Color(0.015f, 0.03f, 0.045f, 0.65f));
                lockImage.name = "Lock";

                builder.TextNode("Chapter Cards/Chapter " + (i + 1), "Index", 12f, 14f, 32f, 80f, string.Empty, 18,
                    ScreenPrefabKit.Cream, TextAnchor.UpperCenter);
                builder.ImageNode("Chapter Cards/Chapter " + (i + 1), "Divider", 52f, 22f, 1f, 67f,
                    new Color(1f, 0.87f, 0.56f, 0.55f));
                builder.TextNode("Chapter Cards/Chapter " + (i + 1), "Name", 72f, 11f, 258f, 39f, string.Empty, 26,
                    ScreenPrefabKit.Cream);
                builder.TextNode("Chapter Cards/Chapter " + (i + 1), "Range", 74f, 48f, 222f, 25f, string.Empty, 19,
                    ScreenPrefabKit.Cream);
                builder.TextNode("Chapter Cards/Chapter " + (i + 1), "Progress", 74f, 73f, 230f, 24f, string.Empty, 17,
                    ScreenPrefabKit.Cream);
                builder.TextNode("Chapter Cards/Chapter " + (i + 1), "Entry", 59f, 101f, 162f, 35f, string.Empty, 20,
                    ScreenPrefabKit.Cream, TextAnchor.MiddleCenter);

                var button = card.gameObject.AddComponent<Button>();
                button.targetGraphic = card.gameObject.GetComponent<Image>();
                button.transition = Selectable.Transition.None;
                button.name = "Chapter " + (i + 1);
            }
        }

        static void BuildBottom(UiPrefabBuilder builder)
        {
            var dungeons = builder.AnchoredNode("", "Dungeons", AnchorPreset.BottomLeft,
                new Vector2(289f, 69f), new Vector2(71f, 176f));
            var dungeonImage = dungeons.gameObject.AddComponent<Image>();
            dungeonImage.color = new Color(0.15f, 0.38f, 0.35f, 0.94f);
            dungeonImage.raycastTarget = true;
            dungeons.gameObject.AddComponent<Button>().targetGraphic = dungeonImage;
            builder.TextNode("Dungeons", "Label", 0f, 0f, 289f, 69f, "资源秘境", 29, ScreenPrefabKit.Cream, TextAnchor.MiddleCenter);

            var tower = builder.AnchoredNode("", "Tower", AnchorPreset.BottomLeft,
                new Vector2(288f, 69f), new Vector2(391f, 176f));
            var towerImage = tower.gameObject.AddComponent<Image>();
            towerImage.color = new Color(0.15f, 0.38f, 0.35f, 0.94f);
            towerImage.raycastTarget = true;
            tower.gameObject.AddComponent<Button>().targetGraphic = towerImage;
            builder.TextNode("Tower", "Label", 0f, 0f, 288f, 69f, "通天古塔", 29, ScreenPrefabKit.Cream, TextAnchor.MiddleCenter);

            var resources = builder.AnchoredNode("", "Resources", AnchorPreset.BottomLeft,
                new Vector2(700f, 34f), new Vector2(25f, 64f));
            builder.TextNode("Resources", "Tokens", 0f, 0f, 190f, 34f, string.Empty, 17, ScreenPrefabKit.Cream);
            builder.TextNode("Resources", "Dust", 200f, 0f, 190f, 34f, string.Empty, 17, ScreenPrefabKit.Cream);
            builder.TextNode("Resources", "Power", 400f, 0f, 190f, 34f, string.Empty, 17, ScreenPrefabKit.Cream);
        }

        static void Bind(string prefabPath)
        {
            var contents = PrefabUtility.LoadPrefabContents(prefabPath);
            if (contents == null) return;
            try
            {
                var view = contents.GetComponent<WorldMapView>();
                if (view == null) view = contents.AddComponent<WorldMapView>();

                var so = new SerializedObject(view);
                var back = Find(contents, "Header Back");
                Set(so, "backButton", back != null ? back.GetComponent<Button>() : null);
                Set(so, "progressText", FindText(contents, "World Progress"));
                Set(so, "dungeonsButton", Find(contents, "Dungeons").GetComponent<Button>());
                Set(so, "towerButton", Find(contents, "Tower").GetComponent<Button>());
                Set(so, "chapterRoot", Find(contents, "Chapter Cards"));
                Set(so, "dustText", FindText(contents, "Resources/Dust"));
                Set(so, "powerText", FindText(contents, "Resources/Power"));

                var names = new List<Text>();
                var ranges = new List<Text>();
                var progress = new List<Text>();
                var entries = new List<Text>();
                var inputs = new List<Button>();
                var locks = new List<Image>();
                var indexes = new List<Text>();
                for (var i = 1; i <= 10; i++)
                {
                    var card = "Chapter Cards/Chapter " + i;
                    indexes.Add(FindText(contents, card + "/Index"));
                    names.Add(FindText(contents, card + "/Name"));
                    ranges.Add(FindText(contents, card + "/Range"));
                    progress.Add(FindText(contents, card + "/Progress"));
                    entries.Add(FindText(contents, card + "/Entry"));
                    inputs.Add(Find(contents, card).GetComponent<Button>());
                    locks.Add(Find(contents, card + "/Lock") != null ? Find(contents, card + "/Lock").GetComponent<Image>() : null);
                }

                SetArray(so, "chapterNames", names);
                SetArray(so, "chapterRanges", ranges);
                SetArray(so, "chapterProgress", progress);
                SetArray(so, "chapterEntries", entries);
                SetArray(so, "chapterButtons", inputs);
                SetArray(so, "chapterLocks", locks);
                SetArray(so, "chapterIndex", indexes);
                so.ApplyModifiedPropertiesWithoutUndo();

                PrefabUtility.SaveAsPrefabAsset(contents, prefabPath);
                AssetDatabase.SaveAssets();
                Debug.Log("[WorldMap] 已生成并绑定 " + prefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contents);
            }
        }

        static Transform Find(GameObject root, string path) => root.transform.Find(path);

        static Text FindText(GameObject root, string path)
        {
            var node = Find(root, path);
            return node != null ? node.GetComponent<Text>() : null;
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
