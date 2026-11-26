using System.Collections.Generic;
using Gxdl.Modules;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Gxdl.EditorTools
{
    /// <summary>
    /// 洞府 · 修行手记 prefab 生成器。按 Art/洞府 效果图搭骨架：
    /// 全屏蓝白背景 + 居中装饰面板，境界/战力铭牌与六个入口按钮留空由 Apply 填。
    /// 切片来源与坐标记录见 ResourceCache/residence-20261125。
    /// </summary>
    [InitializeOnLoad]
    public static class ResidencePrefabSource
    {
        public const string PrefabPath = "Assets/Resources/" + UiScreens.Residence + ".prefab";
        const string SkinPath = "Assets/Resources/Game/Guxu/Expansion/UI/";

        static readonly Color TitleInk = FromHex(0x472205);
        static readonly Color StoryTitleInk = FromHex(0x2C2C27);
        static readonly Color StoryBodyInk = FromHex(0x5D6270);
        static readonly Color PlaqueLabelInk = FromHex(0x494641);
        static readonly Color PlaqueValueInk = FromHex(0x302D25);
        static readonly Color TealText = FromHex(0xFDFCF7);
        static readonly Color GoldText = FromHex(0x432B0F);

        static Color FromHex(long value) => new Color(
            ((value >> 16) & 0xFF) / 255f,
            ((value >> 8) & 0xFF) / 255f,
            (value & 0xFF) / 255f);

        static ResidencePrefabSource()
        {
            ScreenPrefabKit.Register("Residence", Build);
        }

        [MenuItem("Gxdl/UI/生成/洞府", priority = 304)]
        public static void Generate()
        {
            var path = Build();
            if (path != null) EditorUtility.DisplayDialog("生成完成", "已生成\n" + path, "确定");
        }

        static Sprite Skin(string key) =>
            AssetDatabase.LoadAssetAtPath<Sprite>(SkinPath + key + ".png");

        static Image SkinImage(UiPrefabBuilder builder, string parent, string name,
            float x, float y, float width, float height, string key, bool raycast = false)
        {
            var image = builder.Node(parent, name, x, y, width, height).gameObject.AddComponent<Image>();
            image.sprite = Skin(key);
            image.enabled = image.sprite != null;
            image.raycastTarget = raycast;
            return image;
        }

        public static string Build()
        {
            using (var builder = new UiPrefabBuilder("ResidenceView",
                new Vector2(UiRootLayout.DesignWidth, UiRootLayout.DesignHeight)))
            {
                var root = builder.Root;
                AnchorUtility.Apply(root, AnchorPreset.Stretch, Vector2.zero, Vector2.zero);
                ScreenPrefabKit.AddRootComponents(root);

                var background = builder.AnchoredNode("", "Background", AnchorPreset.Stretch, Vector2.zero)
                    .gameObject.AddComponent<Image>();
                background.sprite = Skin("residence-bg");
                background.enabled = background.sprite != null;
                background.raycastTarget = false;

                // 全屏背景之下不再透出主界面，遮罩节点吞掉残余点击。
                var blocker = builder.AnchoredNode("", "Blocker", AnchorPreset.Stretch, Vector2.zero)
                    .gameObject.AddComponent<UiRaycast>();
                blocker.raycastTarget = true;

                // 图集对象切片 578x480，按效果图比例显示 700x640（纵向 +10% 轻微拉伸），居中。
                var panel = SkinImage(builder, "", "Panel", 25f, 347f, 700f, 640f, "residence-panel", true);

                // 标题铭牌（擦字实时渲染），盖面板左上，文字居铭牌米白带。
                SkinImage(builder, "Panel", "Title Plaque", 78f, 6f, 360f, 66f, "residence-plaque-title");
                builder.TextNode("Panel", "Title", 158f, 14f, 250f, 48f, "洞府 · 修行手记", 27, TitleInk,
                    TextAnchor.MiddleCenter);

                SkinImage(builder, "Panel", "Window Art", 58f, 84f, 252f, 162f, "residence-window");

                builder.TextNode("Panel", "Story Title", 354f, 118f, 300f, 44f, "古墟大陆 · 遗石初鸣", 30,
                    StoryTitleInk, TextAnchor.MiddleCenter);
                SkinImage(builder, "Panel", "Divider", 394f, 170f, 220f, 48f, "residence-divider");
                builder.TextNode("Panel", "Story Body", 349f, 224f, 310f, 80f, "一符破古封，\n一剑问前尘。", 22,
                    StoryBodyInk, TextAnchor.MiddleCenter);

                BuildPlaque(builder, "Plaque Realm", 60f, 300f, "当前境界", "RealmValue");
                BuildPlaque(builder, "Plaque Power", 400f, 300f, "基础战力", "PowerValue");

                // 按钮按效果图统一放大 228x76，两列居中对齐。
                BuildButton(builder, "BtnReplay", 84f, 382f, 224f, 76f, "residence-btn-teal-1", "重温开场", TealText);
                BuildButton(builder, "BtnBag", 386f, 382f, 224f, 76f, "residence-btn-gold-1", "整理行囊", GoldText);
                BuildButton(builder, "BtnCompanion", 84f, 462f, 224f, 76f, "residence-btn-teal-2", "同行档案", TealText);
                BuildButton(builder, "BtnSocial", 386f, 462f, 224f, 76f, "residence-btn-teal-3", "好友与聊天", TealText);
                BuildButton(builder, "BtnRank", 84f, 542f, 224f, 76f, "residence-btn-teal-4", "排行榜", TealText);
                BuildButton(builder, "BtnShop", 386f, 542f, 224f, 76f, "residence-btn-gold-2", "商城", GoldText);

                // 关闭钮复用新获得装备图集圆形 X（切片共用）。
                var close = SkinImage(builder, "Panel", "BtnClose", 642f, 12f, 56f, 60f, "residence-close", true);
                close.gameObject.AddComponent<Button>().targetGraphic = close;

                var path = builder.Save(PrefabPath);
                AssetDatabase.SaveAssets();
                if (path != null) Bind(path);
                return path;
            }
        }

        static void BuildPlaque(UiPrefabBuilder builder, string name, float x, float y,
            string label, string valueName)
        {
            var path = "Panel/" + name;
            SkinImage(builder, "Panel", name, x, y, 240f, 90f, "residence-plaque");
            builder.TextNode(path, "Label", 15f, 8f, 210f, 34f, label, 20, PlaqueLabelInk, TextAnchor.MiddleCenter);
            builder.TextNode(path, valueName, 15f, 44f, 210f, 40f, string.Empty, 32, PlaqueValueInk,
                TextAnchor.MiddleCenter);
        }

        static void BuildButton(UiPrefabBuilder builder, string name, float x, float y,
            float width, float height, string key, string label, Color textColor)
        {
            var image = SkinImage(builder, "Panel", name, x, y, width, height, key, true);
            image.gameObject.AddComponent<Button>().targetGraphic = image;
            // 图标留在切片左侧，文字节点整体右移避开图标区。
            builder.TextNode("Panel/" + name, "Label", 52f, 2f, width - 64f, height - 4f, label, 25, textColor,
                TextAnchor.MiddleCenter);
        }

        static void Bind(string prefabPath)
        {
            var contents = PrefabUtility.LoadPrefabContents(prefabPath);
            if (contents == null) return;
            try
            {
                var view = contents.GetComponent<ResidenceView>();
                if (view == null) view = contents.AddComponent<ResidenceView>();

                var so = new SerializedObject(view);
                Set(so, "realmValue", Find(contents, "Panel/Plaque Realm/RealmValue")?.GetComponent<Text>());
                Set(so, "powerValue", Find(contents, "Panel/Plaque Power/PowerValue")?.GetComponent<Text>());
                Set(so, "replayButton", Find(contents, "Panel/BtnReplay")?.GetComponent<Button>());
                Set(so, "bagButton", Find(contents, "Panel/BtnBag")?.GetComponent<Button>());
                Set(so, "companionButton", Find(contents, "Panel/BtnCompanion")?.GetComponent<Button>());
                Set(so, "socialButton", Find(contents, "Panel/BtnSocial")?.GetComponent<Button>());
                Set(so, "rankButton", Find(contents, "Panel/BtnRank")?.GetComponent<Button>());
                Set(so, "shopButton", Find(contents, "Panel/BtnShop")?.GetComponent<Button>());
                Set(so, "closeButton", Find(contents, "Panel/BtnClose")?.GetComponent<Button>());
                so.ApplyModifiedPropertiesWithoutUndo();

                PrefabUtility.SaveAsPrefabAsset(contents, prefabPath);
                AssetDatabase.SaveAssets();
                Debug.Log("[Residence] 已生成并绑定 " + prefabPath);
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
    }
}
