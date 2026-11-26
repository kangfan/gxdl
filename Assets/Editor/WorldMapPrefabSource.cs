using System.Collections.Generic;
using Gxdl.Modules;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Gxdl.EditorTools
{
    /// <summary>
    /// 世界地图 prefab 生成器。
    ///
    /// 底图直接用美术的 world-layout.png：背景、标题、十张卡片框、
    /// 底部两个按钮和资源栏都已在图里画好，这里只叠实时文字与透明热区。
    /// 坐标照 GuxuExpansionUI.ShowWorldMap 对齐，那套数值就是为这张图调过的。
    /// </summary>
    [InitializeOnLoad]
    public static class WorldMapPrefabSource
    {
        public const string PrefabPath = "Assets/Resources/" + UiScreens.WorldMap + ".prefab";

        const string SkinPath = "Assets/Resources/Game/Guxu/Expansion/UI/";
        const float W = 750f, H = 1334f;

        static readonly float[] RowY = { 300f, 480f, 660f, 840f, 1020f };

        // 效果图采样
        static readonly Color TitleInk = FromHex(0x7C440B);
        static readonly Color SubtitleInk = FromHex(0x737681);
        static readonly Color CardNameInk = FromHex(0x3D291F);
        static readonly Color CardRangeInk = FromHex(0x34241F);
        static readonly Color CardUnlockInk = FromHex(0x263E37);
        static readonly Color CardStatusInk = FromHex(0x2E6E5C);
        static readonly Color TabActiveInk = FromHex(0x8A6A2C);
        static readonly Color TabIdleInk = FromHex(0x6A7684);
        static readonly Color FooterInk = FromHex(0xE8EEF4);

        static Color FromHex(long value) => new Color(
            ((value >> 16) & 0xFF) / 255f, ((value >> 8) & 0xFF) / 255f, (value & 0xFF) / 255f);

        static Sprite Skin(string key) => AssetDatabase.LoadAssetAtPath<Sprite>(SkinPath + key + ".png");

        static Image SkinImage(UiPrefabBuilder builder, string parent, string name,
            float x, float y, float width, float height, string key, bool raycast = false)
        {
            var image = builder.Node(parent, name, x, y, width, height).gameObject.AddComponent<Image>();
            image.sprite = Skin(key);
            image.enabled = image.sprite != null;
            image.raycastTarget = raycast;
            return image;
        }

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
            using (var builder = new UiPrefabBuilder("WorldMapView", new Vector2(W, H)))
            {
                // 不挂 UiRootLayout：整页按 750x1334 画，缩放交给 FitPage。
                var root = builder.Root;
                AnchorUtility.Apply(root, AnchorPreset.Stretch, Vector2.zero, Vector2.zero);

                var background = builder.ImageNode("", "Background", 0f, 0f, W, H, Color.white);
                background.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(SkinPath + "ui-bg-fullscreen.png");
                background.preserveAspect = false;
                background.raycastTarget = false;
                background.name = "Background";

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
            // 返回热区（左上角，覆盖返回钮位置）。
            var back = builder.Node("", "Header Back", 18f, 21f, 136f, 46f);
            var backHit = back.gameObject.AddComponent<Gxdl.Modules.UiRaycast>();
            back.gameObject.AddComponent<Button>().targetGraphic = backHit;
            builder.TextNode("Header Back", "Label", 0f, 0f, 136f, 46f, string.Empty, 20,
                ScreenPrefabKit.Cream, TextAnchor.MiddleCenter);

            // 古墟标题铭牌 + 副题（真实主线进度由 Apply 填）。
            SkinImage(builder, "", "Title Plaque", 58f, 192f, 200f, 62f, "ruins-title");
            builder.TextNode("Title Plaque", "World Title", 0f, 6f, 200f, 46f, "古墟", 30, TitleInk,
                TextAnchor.MiddleCenter);
            builder.TextNode("", "World Progress", 62f, 262f, 240f, 28f, string.Empty, 17,
                SubtitleInk, TextAnchor.MiddleLeft);
        }

        static void BuildChapters(UiPrefabBuilder builder)
        {
            for (var i = 0; i < 10; i++)
            {
                var x = i % 2 == 0 ? 37f : 387f;
                var y = RowY[i / 2];
                var card = "Chapter " + (i + 1);
                builder.Node("", card, x, y, 297f, 137f);

                // 未解锁时压一层暗色；卡片浮岛切片在下面。
                SkinImage(builder, card, "Card", 0f, 0f, 297f, 137f, "ruins-ch-" + (i + 1));
                // 效果图锁定章不压暗罩，仅文字提示；保留节点供 Apply 控制显隐。
                var lockImage = builder.ImageNode(card, "Lock", 2f, 2f, 293f, 133f,
                    new Color(0.015f, 0.03f, 0.045f, 0f));
                lockImage.name = "Lock";
                lockImage.raycastTarget = false;

                builder.TextNode(card, "Index", 12f, 16f, 30f, 100f, string.Empty, 17,
                    new Color(0.93f, 0.95f, 0.98f), TextAnchor.UpperCenter);
                builder.TextNode(card, "Name", 52f, 36f, 160f, 34f, string.Empty, 23,
                    CardNameInk, TextAnchor.MiddleLeft);
                builder.TextNode(card, "Range", 52f, 72f, 140f, 24f, string.Empty, 16,
                    CardRangeInk, TextAnchor.MiddleLeft);
                builder.TextNode(card, "Progress", 52f, 96f, 170f, 22f, string.Empty, 14,
                    CardUnlockInk, TextAnchor.MiddleLeft);

                // 解锁后显示「进入遗域」。
                builder.TextNode(card, "Entry", 92f, 116f, 150f, 20f, string.Empty, 15,
                    CardStatusInk, TextAnchor.MiddleCenter);

                // 点击热区覆盖整张卡，不拦截显示。
                var hit = builder.Node(card, "Hit", 0f, 0f, 297f, 137f);
                var hitArea = hit.gameObject.AddComponent<Gxdl.Modules.UiRaycast>();
                hit.gameObject.AddComponent<Button>().targetGraphic = hitArea;
            }
        }

        static void BuildBottom(UiPrefabBuilder builder)
        {
            // 底部两个页签：热区 + 实时文字。
            var dungeons = builder.Node("", "Dungeons", 100f, 1190f, 250f, 44f);
            var dungeonHit = dungeons.gameObject.AddComponent<Gxdl.Modules.UiRaycast>();
            dungeons.gameObject.AddComponent<Button>().targetGraphic = dungeonHit;
            builder.TextNode("Dungeons", "Label", 0f, 0f, 250f, 44f, "资源秘境", 26,
                TabActiveInk, TextAnchor.MiddleCenter);

            var tower = builder.Node("", "Tower", 400f, 1190f, 250f, 44f);
            var towerHit = tower.gameObject.AddComponent<Gxdl.Modules.UiRaycast>();
            tower.gameObject.AddComponent<Button>().targetGraphic = towerHit;
            builder.TextNode("Tower", "Label", 0f, 0f, 250f, 44f, "通天古墟", 26,
                TabIdleInk, TextAnchor.MiddleCenter);

            builder.TextNode("", "Tokens", 100f, 1288f, 160f, 32f, string.Empty, 18,
                FooterInk, TextAnchor.MiddleLeft);
            builder.TextNode("", "Dust", 300f, 1288f, 160f, 32f, string.Empty, 18,
                FooterInk, TextAnchor.MiddleLeft);
            builder.TextNode("", "Power", 490f, 1288f, 200f, 32f, string.Empty, 18,
                FooterInk, TextAnchor.MiddleLeft);
        }

        static Sprite LoadSprite(string resourcesPath)
        {
            var texture = Resources.Load<Texture2D>(resourcesPath);
            if (texture == null)
            {
                Debug.LogWarning("[WorldMap] 缺少底图：" + resourcesPath);
                return null;
            }

            return Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height),
                new Vector2(0.5f, 0.5f), 100f);
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
                Set(so, "backButton", Find(contents, "Header Back") != null ? Find(contents, "Header Back").GetComponent<Button>() : null);
                Set(so, "progressText", FindText(contents, "World Progress"));
                Set(so, "dungeonsButton", Find(contents, "Dungeons") != null ? Find(contents, "Dungeons").GetComponent<Button>() : null);
                Set(so, "towerButton", Find(contents, "Tower") != null ? Find(contents, "Tower").GetComponent<Button>() : null);
                Set(so, "tokensText", FindText(contents, "Tokens"));
                Set(so, "dustText", FindText(contents, "Dust"));
                Set(so, "powerText", FindText(contents, "Power"));

                var names = new List<Text>();
                var ranges = new List<Text>();
                var progress = new List<Text>();
                var entries = new List<Text>();
                var inputs = new List<Button>();
                var locks = new List<Image>();
                var indexes = new List<Text>();
                for (var i = 1; i <= 10; i++)
                {
                    var card = "Chapter " + i;
                    indexes.Add(FindText(contents, card + "/Index"));
                    names.Add(FindText(contents, card + "/Name"));
                    ranges.Add(FindText(contents, card + "/Range"));
                    progress.Add(FindText(contents, card + "/Progress"));
                    entries.Add(FindText(contents, card + "/Entry"));
                    var hit = Find(contents, card + "/Hit");
                    inputs.Add(hit != null ? hit.GetComponent<Button>() : null);
                    var lockNode = Find(contents, card + "/Lock");
                    locks.Add(lockNode != null ? lockNode.GetComponent<Image>() : null);
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
