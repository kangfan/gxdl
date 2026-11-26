using System.Collections.Generic;
using Gxdl.Modules;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Gxdl.EditorTools
{
    /// <summary>
    /// 商城 prefab 生成器。四行商品固定在骨架里，名称/价格/图标/可买状态由 ShopView.Apply 填入。
    /// </summary>
    [InitializeOnLoad]
    public static class ShopPrefabSource
    {
        public const string PrefabPath = "Assets/Resources/" + UiScreens.Shop + ".prefab";
        const string SkinPath = "Assets/Resources/Game/Guxu/Expansion/UI/";

        // 效果图采样：卡名/描述/价格/购买字/提示文字/返回字
        static readonly Color NameInk = FromHex(0x69410C);
        static readonly Color DescInk = FromHex(0x5A5650);
        static readonly Color PriceInk = FromHex(0x7A4A15);
        static readonly Color BuyInk = FromHex(0x6E4208);
        static readonly Color NoteInk = FromHex(0xFBFBF9);
        static readonly Color BackInk = FromHex(0xFCFBEE);
        static readonly Color SubtitleInk = FromHex(0x27313F);

        static Color FromHex(long value) => new Color(
            ((value >> 16) & 0xFF) / 255f, ((value >> 8) & 0xFF) / 255f, (value & 0xFF) / 255f);

        static Sprite Skin(string key) => AssetDatabase.LoadAssetAtPath<Sprite>(SkinPath + key + ".png");

        static Sprite BgSprite() =>
            AssetDatabase.LoadAssetAtPath<Sprite>(SkinPath + "ui-bg-fullscreen.png");

        static Image SkinImage(UiPrefabBuilder builder, string parent, string name,
            float x, float y, float width, float height, string key, bool raycast = false)
        {
            var image = builder.Node(parent, name, x, y, width, height).gameObject.AddComponent<Image>();
            image.sprite = Skin(key);
            image.enabled = image.sprite != null;
            image.raycastTarget = raycast;
            return image;
        }

        static ShopPrefabSource()
        {
            ScreenPrefabKit.Register("Shop", Build);
        }

        [MenuItem("Gxdl/UI/生成/商城", priority = 303)]
        public static void Generate()
        {
            var path = Build();
            if (path != null) EditorUtility.DisplayDialog("生成完成", "已生成\n" + path, "确定");
        }

        public static string Build()
        {
            using (var builder = new UiPrefabBuilder("ShopView",
                new Vector2(UiRootLayout.DesignWidth, UiRootLayout.DesignHeight)))
            {
                var root = builder.Root;
                AnchorUtility.Apply(root, AnchorPreset.Stretch, Vector2.zero, Vector2.zero);
                ScreenPrefabKit.AddRootComponents(root);

                var background = builder.AnchoredNode("", "Background", AnchorPreset.Stretch, Vector2.zero)
                    .gameObject.AddComponent<Image>();
                background.sprite = BgSprite();
                background.raycastTarget = false;

                BuildHeader(builder);
                for (var i = 0; i < 4; i++) BuildRow(builder, i);
                BuildNote(builder);

                var path = builder.Save(PrefabPath);
                AssetDatabase.SaveAssets();
                if (path != null) Bind(path);
                return path;
            }
        }

        static void BuildHeader(UiPrefabBuilder builder)
        {
            // 返回钮：图集切片，古墟文字实时。
            var back = SkinImage(builder, "", "Back", 30f, 28f, 150f, 58f, "shop-back", true);
            back.gameObject.AddComponent<Button>().targetGraphic = back;
            builder.TextNode("Back", "Label", 20f, 12f, 110f, 34f, "古墟", 24, BackInk, TextAnchor.MiddleCenter);

            // 书法标题美术字保留烘焙。
            SkinImage(builder, "", "Title Art", 210f, 36f, 160f, 64f, "shop-title-art");

            // 副题条：文字实时。
            SkinImage(builder, "", "Subtitle", 200f, 122f, 216f, 36f, "shop-subtitle-bar");
            builder.TextNode("Subtitle", "Label", 0f, 2f, 216f, 30f, "以灵尘兑换养成资源", 17, SubtitleInk,
                TextAnchor.MiddleCenter);

            // 货币栏：灵尘图标与加号已烘焙，数量实时。
            SkinImage(builder, "", "Currency", 596f, 18f, 132f, 44f, "shop-currency");
            builder.TextNode("Currency", "Coin Balance", 40f, 6f, 82f, 32f, string.Empty, 24,
                new Color(0.95f, 0.97f, 1f), TextAnchor.MiddleRight);
        }

        static void BuildRow(UiPrefabBuilder builder, int index)
        {
            var name = "Good " + index;
            var row = builder.Node("", name, 32f, 247f + index * 236f, 687f, 194f);
            SkinImage(builder, name, "Card", 0f, 0f, 687f, 194f, "shop-card");

            var icon = builder.Node(name, "Icon", 38f, 46f, 96f, 102f).gameObject.AddComponent<Image>();
            icon.raycastTarget = false;

            builder.TextNode(name, "Name", 168f, 34f, 210f, 42f, string.Empty, 29, NameInk);
            builder.TextNode(name, "Desc", 168f, 80f, 330f, 30f, string.Empty, 18, DescInk);
            builder.TextNode(name, "Price", 168f, 114f, 240f, 32f, string.Empty, 20, PriceInk);

            var buy = SkinImage(builder, name, "Buy", 534f, 46f, 112f, 96f, "shop-buy-gold", true);
            buy.gameObject.AddComponent<Button>().targetGraphic = buy;
            builder.TextNode(name + "/Buy", "Label", 4f, 8f, 104f, 76f, "购买", 26, BuyInk,
                TextAnchor.MiddleCenter);
        }

        static void BuildNote(UiPrefabBuilder builder)
        {
            SkinImage(builder, "", "Note Bar", 220f, 1246f, 310f, 42f, "shop-footer-bar");
            builder.TextNode("Note Bar", "Note", 0f, 4f, 310f, 34f, "首版验证价格，平衡确认后调整", 16, NoteInk,
                TextAnchor.MiddleCenter);
        }

        static void Bind(string prefabPath)
        {
            var contents = PrefabUtility.LoadPrefabContents(prefabPath);
            if (contents == null) return;
            try
            {
                var view = contents.GetComponent<ShopView>();
                if (view == null) view = contents.AddComponent<ShopView>();

                var so = new SerializedObject(view);
                Set(so, "backButton", Find(contents, "Back")?.GetComponent<Button>());
                Set(so, "coinText", Find(contents, "Currency/Coin Balance")?.GetComponent<Text>());

                var icons = new List<Image>();
                var names = new List<Text>();
                var descs = new List<Text>();
                var prices = new List<Text>();
                var buttons = new List<Button>();
                for (var i = 0; i < 4; i++)
                {
                    var path = "Good " + i;
                    icons.Add(Find(contents, path + "/Icon")?.GetComponent<Image>());
                    names.Add(Find(contents, path + "/Name")?.GetComponent<Text>());
                    descs.Add(Find(contents, path + "/Desc")?.GetComponent<Text>());
                    prices.Add(Find(contents, path + "/Price")?.GetComponent<Text>());
                    buttons.Add(Find(contents, path + "/Buy")?.GetComponent<Button>());
                }
                SetArray(so, "goodIcons", icons);
                SetArray(so, "goodNames", names);
                SetArray(so, "goodDescs", descs);
                SetArray(so, "goodPrices", prices);
                SetArray(so, "buyButtons", buttons);
                so.ApplyModifiedPropertiesWithoutUndo();

                PrefabUtility.SaveAsPrefabAsset(contents, prefabPath);
                AssetDatabase.SaveAssets();
                Debug.Log("[Shop] 已生成并绑定 " + prefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contents);
            }
        }

        static Transform Find(GameObject root, string path) => root.transform.Find(path);

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
