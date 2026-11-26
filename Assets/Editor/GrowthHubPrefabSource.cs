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
        const string SkinPath = "Assets/Resources/Game/Guxu/Expansion/UI/";
        const float BoardWidth = 940f;
        const float BoardHeight = 1672f;
        const string Board = "Growth Hub";

        static readonly float[] RowY = { 380f, 631f, 882f, 1133f };
        static readonly string[] RowKeys = { "Forge", "Realm", "Talents", "Gems" };
        static readonly string[] RowTitles = { "槽位锻造", "境界修炼", "三系天赋", "灵玉镶嵌" };
        static readonly string[] RowCardKeys = { "growth-card-forge", "growth-card-realm", "growth-card-talents", "growth-card-gems" };

        // 效果图采样
        static readonly Color BackInk = FromHex(0xFDF4CD);
        static readonly Color SubtitleInk = FromHex(0x28323B);
        static readonly Color EntryTitleInk = FromHex(0x472004);
        static readonly Color EntryDetailInk = FromHex(0x4A403F);
        static readonly Color BtnLabelInk = FromHex(0xFDFDF6);
        static readonly Color DungeonLabelInk = FromHex(0x8A6A3C);
        static readonly Color FooterInk = FromHex(0xFEFEFD);

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

                var backing = builder.AnchoredNode("", "Backing", AnchorPreset.Stretch, Vector2.zero)
                    .gameObject.AddComponent<Image>();
                backing.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(SkinPath + "ui-bg-fullscreen.png");
                backing.raycastTarget = false;

                // 940 x 1672 的旧版底板按 750 宽等比缩放，避免在 prefab 根尺寸下溢出错位。
                var frame = builder.Node("", "Board Frame", 0f, 0f, BoardWidth, BoardHeight);
                frame.localScale = Vector3.one * (UiRootLayout.DesignWidth / BoardWidth);

                builder.Node("Board Frame", Board, 0f, 0f, BoardWidth, BoardHeight);

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
            // 返回钮：复用商城返回钮切片（同风格深蓝底）。
            var back = SkinImage(builder, p, "Back", 46f, 29f, 205f, 66f, "shop-back", true);
            back.gameObject.AddComponent<Button>().targetGraphic = back;
            builder.TextNode(p, "Home Title", 60f, 36f, 175f, 46f, "古墟", 30, BackInk, TextAnchor.MiddleCenter);
            builder.TextNode(p, "Subtitle", 284f, 95f, 420f, 34f,
                "装备、境界、天赋、灵玉共同决定战斗表现", 24, SubtitleInk);
        }

        static void BuildEntries(UiPrefabBuilder builder)
        {
            var p = "Board Frame/" + Board;
            for (var i = 0; i < RowY.Length; i++)
            {
                var y = RowY[i];
                var row = "Entry " + RowKeys[i];
                builder.Node(p, row, 50f, y, 777f, 205f);
                var rowPath = p + "/" + row;

                SkinImage(builder, rowPath, "Card", 0f, 0f, 777f, 205f, RowCardKeys[i]);

                builder.TextNode(rowPath, "Title", 290f, 42f, 240f, 50f, RowTitles[i], 34, EntryTitleInk);
                builder.TextNode(rowPath, "Detail", 290f, 100f, 340f, 38f, string.Empty, 24, EntryDetailInk);
                builder.TextNode(rowPath, "Balance", 290f, 142f, 340f, 40f, string.Empty, 26, EntryDetailInk);

                // 按钮底板先建，文字后建，避免“修习”被底板遮住。
                var hit = SkinImage(builder, rowPath, "Hit", 636f, 76f, 123f, 66f, "growth-btn-blue", true);
                hit.gameObject.AddComponent<Button>().targetGraphic = hit;
                builder.TextNode(rowPath, "Action", 636f, 86f, 123f, 48f, "修习", 26, BtnLabelInk,
                    TextAnchor.MiddleCenter);
            }
        }

        static void BuildBottom(UiPrefabBuilder builder)
        {
            var p = "Board Frame/" + Board;
            var bottom = p + "/Bottom";
            builder.Node(p, "Bottom", 0f, 0f, 0f, 0f);

            var dungeons = SkinImage(builder, bottom, "Dungeons", 96f, 1364f, 355f, 82f, "growth-btn-gold-off", true);
            dungeons.gameObject.AddComponent<Button>().targetGraphic = dungeons;
            builder.TextNode(bottom, "Dungeons Label", 96f, 1379f, 355f, 52f, "前往资源秘境", 28, DungeonLabelInk,
                TextAnchor.MiddleCenter);

            var loadout = SkinImage(builder, bottom, "Loadout", 501f, 1362f, 301f, 79f, "growth-btn-blue-wide", true);
            loadout.gameObject.AddComponent<Button>().targetGraphic = loadout;
            builder.TextNode(bottom, "Loadout Label", 501f, 1377f, 301f, 50f, "战前技能", 30, BtnLabelInk,
                TextAnchor.MiddleCenter);

            builder.TextNode(bottom, "Tokens", 84f, 1590f, 175f, 44f, string.Empty, 26, FooterInk);
            builder.TextNode(bottom, "Dust", 347f, 1590f, 121f, 44f, string.Empty, 26, FooterInk);
            builder.TextNode(bottom, "Power", 585f, 1590f, 184f, 44f, string.Empty, 26, FooterInk);
        }

        static readonly Color PaleGoldTint = new Color(1f, 0.89f, 0.62f);

        static Color PaleGold() => new Color(0.16f, 0.13f, 0.09f);

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
