using System.Collections.Generic;
using Gxdl.Modules;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Gxdl.EditorTools
{
    /// <summary>
    /// 成长总览 prefab 生成器。
    /// 骨架照 GuxuGrowthHub.DrawGrowthHub 的 940 x 1672 版面还原，
    /// 四行入口的说明、余额与底部资源栏由 GrowthHubView.Apply 在运行时填入。
    /// </summary>
    [InitializeOnLoad]
    public static class GrowthHubPrefabSource
    {
        public const string PrefabPath = "Assets/Resources/" + UiScreens.GrowthHub + ".prefab";
        const string ArtFolder = "Game/Guxu/Expansion/UI/";
        const float BoardWidth = 940f;
        const float BoardHeight = 1672f;
        const string Board = "Growth Hub";

        static readonly float[] RowY = { 323f, 565f, 804f, 1048f };
        static readonly string[] RowKeys = { "Forge", "Realm", "Talents", "Gems" };
        static readonly string[] RowTitles = { "槽位锻造", "境界修炼", "三系天赋", "灵玉镶嵌" };

        static GrowthHubPrefabSource()
        {
            ScreenPrefabKit.Register("GrowthHub", Build);
        }

        [MenuItem("Gxdl/UI/生成/成长总览", priority = 303)]
        public static void Generate()
        {
            var path = Build();
            if (path != null) EditorUtility.DisplayDialog("生成完成", "已生成\n" + path, "确定");
        }

        public static string Build()
        {
            using (var builder = new UiPrefabBuilder("GrowthHubView",
                new Vector2(UiRootLayout.DesignWidth, UiRootLayout.DesignHeight)))
            {
                var root = builder.Root;
                AnchorUtility.Apply(root, AnchorPreset.Stretch, Vector2.zero, Vector2.zero);
                ScreenPrefabKit.AddRootComponents(root);

                var backing = builder.ImageNode("", "Backing", 0f, 0f,
                    UiRootLayout.DesignWidth, UiRootLayout.DesignHeight, new Color(0.025f, 0.075f, 0.12f));
                backing.rectTransform.anchorMin = Vector2.zero;
                backing.rectTransform.anchorMax = Vector2.one;
                backing.rectTransform.offsetMin = Vector2.zero;
                backing.rectTransform.offsetMax = Vector2.zero;
                backing.raycastTarget = false;

                // 旧实现按 940 x 1672 整体缩放居中，这里用比例适配替代逐帧重算。
                var frame = builder.AnchoredNode("", "Board Frame", AnchorPreset.Center,
                    new Vector2(BoardWidth, BoardHeight), Vector2.zero);
                var fitter = frame.gameObject.AddComponent<AspectRatioFitter>();
                fitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
                fitter.aspectRatio = BoardWidth / BoardHeight;

                var board = builder.Node("Board Frame", Board, 0f, 0f, BoardWidth, BoardHeight);
                var skin = builder.ImageNode("Board Frame/" + Board, "Skin", 0f, 0f, BoardWidth, BoardHeight, Color.white);
                var sprite = Load("growth-layout");
                if (sprite != null) skin.sprite = sprite;
                else skin.color = new Color(0.03f, 0.09f, 0.13f, 0f);

                BuildHeader(builder);
                BuildEntries(builder);
                BuildBottom(builder);

                var path = builder.Save(PrefabPath);
                AssetDatabase.SaveAssets();
                if (path != null) Bind(path);
                return path;
            }
        }

        static void BuildHeader(UiPrefabBuilder builder)
        {
            var p = "Board Frame/" + Board;
            builder.TextNode(p, "Home Title", 104f, 54f, 81f, 46f, "古墟", 35, ScreenPrefabKit.Cream);
            TextButton(builder, p, "Back", 32f, 49f, 178f, 56f, string.Empty, 0.15f, 0.38f, 0.35f);
            builder.TextNode(p, "Subtitle", 240f, 179f, 463f, 38f,
                "装备、境界、天赋、灵玉共同决定战斗表现", 28, ScreenPrefabKit.Cream);
        }

        static void BuildEntries(UiPrefabBuilder builder)
        {
            var p = "Board Frame/" + Board;
            for (var i = 0; i < RowY.Length; i++)
            {
                var y = RowY[i];
                var row = "Entry " + RowKeys[i];
                builder.Node(p, row, 0f, y, BoardWidth, 200f);
                var rowPath = p + "/" + row;

                builder.TextNode(rowPath, "Title", 153f, 24f, 235f, 65f, RowTitles[i], 46, PaleGold());
                builder.TextNode(rowPath, "Detail", 94f, 105f, 377f, 38f, string.Empty, 28, ScreenPrefabKit.Cream);
                builder.TextNode(rowPath, "Balance", 144f, 145f, 319f, 43f, string.Empty, 30, ScreenPrefabKit.Cream);
                builder.TextNode(rowPath, "Action", 748f, 98f, 88f, 60f, "修习", 40, ScreenPrefabKit.Cream);

                TextButton(builder, rowPath, "Hit", 707f, 96f, 192f, 62f, string.Empty, 0.15f, 0.38f, 0.35f);
            }
        }

        static void BuildBottom(UiPrefabBuilder builder)
        {
            var p = "Board Frame/" + Board;
            var bottom = p + "/Bottom";

            builder.TextNode(p, "Dungeons Label", 175f, 1367f, 210f, 62f, "前往资源秘境", 36, new Color(0.16f, 0.09f, 0.025f));
            TextButton(builder, p, "Dungeons", 51f, 1355f, 392f, 87f, string.Empty, 0.86f, 0.78f, 0.54f);

            builder.TextNode(p, "Loadout Label", 641f, 1367f, 167f, 62f, "战前技能", 42, ScreenPrefabKit.Cream);
            TextButton(builder, p, "Loadout", 508f, 1356f, 371f, 86f, string.Empty, 0.15f, 0.38f, 0.35f);

            builder.TextNode(p, "Tokens", 173f, 1561f, 151f, 43f, string.Empty, 29, PaleGold());
            builder.TextNode(p, "Dust", 416f, 1561f, 167f, 43f, string.Empty, 29, PaleGold());
            builder.TextNode(p, "Power", 681f, 1561f, 184f, 43f, string.Empty, 29, PaleGold());
        }

        static readonly Color PaleGoldTint = new Color(1f, 0.89f, 0.62f);

        static Color PaleGold() => PaleGoldTint;

        static RectTransform TextButton(UiPrefabBuilder builder, string parent, string name,
            float x, float y, float width, float height, string label, float r, float g, float b)
        {
            var rect = builder.Node(parent, name, x, y, width, height);
            var image = rect.gameObject.AddComponent<Image>();
            image.color = new Color(r, g, b, 0.94f);
            image.raycastTarget = true;
            rect.gameObject.AddComponent<Button>().targetGraphic = image;
            if (!string.IsNullOrEmpty(label))
            {
                builder.TextNode(parent + "/" + name, "Label", 0f, 0f, width, height, label, 26,
                    ScreenPrefabKit.Cream, TextAnchor.MiddleCenter);
            }

            return rect;
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
                var view = contents.GetComponent<GrowthHubView>();
                if (view == null) view = contents.AddComponent<GrowthHubView>();

                var so = new SerializedObject(view);
                var board = "Board Frame/" + Board;
                Set(so, "backButton", Component<Button>(contents, board + "/Back"));
                Set(so, "dungeonsButton", Component<Button>(contents, board + "/Bottom/Dungeons"));
                Set(so, "loadoutButton", Component<Button>(contents, board + "/Bottom/Loadout"));
                Set(so, "tokensText", Text(contents, board + "/Bottom/Tokens"));
                Set(so, "dustText", Text(contents, board + "/Bottom/Dust"));
                Set(so, "powerText", Text(contents, board + "/Bottom/Power"));

                var buttons = new List<Button>();
                var titles = new List<Text>();
                var details = new List<Text>();
                var balances = new List<Text>();
                for (var i = 0; i < RowKeys.Length; i++)
                {
                    var row = board + "/Entry " + RowKeys[i];
                    buttons.Add(Component<Button>(contents, row + "/Hit"));
                    titles.Add(Text(contents, row + "/Title"));
                    details.Add(Text(contents, row + "/Detail"));
                    balances.Add(Text(contents, row + "/Balance"));
                }

                SetArray(so, "entryButtons", buttons);
                SetArray(so, "entryTitles", titles);
                SetArray(so, "entryDetails", details);
                SetArray(so, "entryBalances", balances);
                so.ApplyModifiedPropertiesWithoutUndo();

                PrefabUtility.SaveAsPrefabAsset(contents, prefabPath);
                AssetDatabase.SaveAssets();
                Debug.Log("[GrowthHub] 已生成并绑定 " + prefabPath);
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
