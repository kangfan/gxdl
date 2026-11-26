using System.Collections.Generic;
using Gxdl.Modules;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Gxdl.EditorTools
{
    /// <summary>
    /// 战备装备 prefab 生成器。
    /// 4 张技能卡结构相同，选中环与"已装备"标记由 LoadoutView.Apply 控制显隐。
    /// </summary>
    [InitializeOnLoad]
    public static class LoadoutPrefabSource
    {
        public const string PrefabPath = "Assets/Resources/" + UiScreens.Loadout + ".prefab";
        const string SkinPath = "Assets/Resources/Game/Guxu/Expansion/UI/";

        // 效果图采样
        static readonly Color TitleInk = FromHex(0x1E3A50);
        static readonly Color SubtitleInk = FromHex(0x2F4A64);
        static readonly Color SkillNameInk = FromHex(0xFEFDF6);
        static readonly Color DetailNameInk = FromHex(0x653308);
        static readonly Color DescInk = FromHex(0x181C24);
        static readonly Color BtnBackInk = FromHex(0xEAEDF9);
        static readonly Color BtnGoInk = FromHex(0x5A3A10);
        static readonly Color PlateInk = new Color(0.10f, 0.16f, 0.22f);

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

        static LoadoutPrefabSource()
        {
            ScreenPrefabKit.Register("Loadout", Build);
        }

        [MenuItem("Gxdl/UI/生成/战备装备", priority = 312)]
        public static void Generate()
        {
            var path = Build();
            if (path != null) EditorUtility.DisplayDialog("生成完成", "已生成\n" + path, "确定");
        }

        public static string Build()
        {
            using (var builder = new UiPrefabBuilder("LoadoutView",
                new Vector2(UiRootLayout.DesignWidth, UiRootLayout.DesignHeight)))
            {
                var root = builder.Root;
                AnchorUtility.Apply(root, AnchorPreset.Stretch, Vector2.zero, Vector2.zero);
                ScreenPrefabKit.AddRootComponents(root);

                var background = builder.AnchoredNode("", "Background", AnchorPreset.Stretch, Vector2.zero)
                    .gameObject.AddComponent<Image>();
                background.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(SkinPath + "ui-bg-fullscreen.png");
                background.raycastTarget = false;

                BuildHeader(builder);
                BuildSkills(builder);
                BuildDetail(builder);
                BuildActions(builder);

                var path = builder.Save(PrefabPath);
                AssetDatabase.SaveAssets();
                if (path != null) Bind(path);
                return path;
            }
        }

        static void BuildHeader(UiPrefabBuilder builder)
        {
            // 回修行返回钮（图集深蓝底）。
            var backNode = builder.AnchoredNode("", "Header Back", AnchorPreset.TopLeft, new Vector2(150f, 58f), new Vector2(95f, 74f));
            var backImage = backNode.gameObject.AddComponent<Image>();
            backImage.sprite = Skin("shop-back");
            backImage.raycastTarget = true;
            backNode.gameObject.AddComponent<Button>().targetGraphic = backImage;
            builder.TextNode("Header Back", "Label", 20f, 12f, 110f, 34f, "回修行", 22, BtnBackInk, TextAnchor.MiddleCenter);

            // 标题横幅（图集空蓝金横幅）+ 实时标题。
            SkinImage(builder, "", "Title Banner", 215f, 30f, 320f, 66f, "prep-title");
            builder.TextNode("Title Banner", "Title", 60f, 12f, 200f, 44f, "战前布置", 32, TitleInk,
                TextAnchor.MiddleCenter);

            SkinImage(builder, "", "Prep Divider", 215f, 122f, 320f, 36f, "prep-divider");
            builder.TextNode("", "Subtitle", 215f, 130f, 320f, 26f, "选择一个主动技能", 20, SubtitleInk,
                TextAnchor.MiddleCenter);
        }

        static void BuildSkills(UiPrefabBuilder builder)
        {
            builder.Node("", "Skill Root", 0f, 180f, UiRootLayout.DesignWidth, 210f);

            string[] circleKeys = { "prep-skill-1", "prep-skill-2", "prep-skill-3", "prep-skill-4" };
            for (var i = 0; i < 4; i++)
            {
                var path = "Skill Root/Skill " + i;
                var cell = builder.Node("Skill Root", "Skill " + i, 25f + i * 178f, 0f, 172f, 205f);
                cell.gameObject.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.001f);

                // 图集技能圆盘（含左侧选中金盘变体），动态技能图标叠在盘心。
                SkinImage(builder, path, "Circle", 6f, 0f, 160f, 172f, circleKeys[i]);
                var icon = builder.Node(path, "Icon", 52f, 42f, 70f, 74f).gameObject.AddComponent<Image>();
                icon.raycastTarget = false;
                var ring = builder.ImageNode(path, "Ring", 6f, 0f, 160f, 172f, new Color(1f, 0.85f, 0.4f, 0.30f));
                ring.gameObject.SetActive(false);

                SkinImage(builder, path, "Name Plate", 22f, 170f, 128f, 32f, "prep-nameplate");
                builder.TextNode(path, "Name", 22f, 150f, 128f, 28f, string.Empty, 20,
                    SkillNameInk, TextAnchor.MiddleCenter);

                // 已装备角标：圆盘上方的小铭牌，由存档决定显示。
                var flag = builder.ImageNode(path, "Equipped Flag", 12f, -28f, 120f, 30f,
                    new Color(0.98f, 0.91f, 0.62f, 0.95f));
                flag.gameObject.SetActive(false);
                builder.TextNode(path + "/Equipped Flag", "Label", 0f, 0f, 120f, 30f, "已装备", 20,
                    PlateInk, TextAnchor.MiddleCenter);

                var button = cell.gameObject.AddComponent<Button>();
                button.targetGraphic = cell.gameObject.GetComponent<Image>();
                button.transition = Selectable.Transition.None;
            }
        }

        static void BuildDetail(UiPrefabBuilder builder)
        {
            var body = SkinImage(builder, "", "Loadout Detail", 50f, 400f, 600f, 566f, "prep-card");

            // 卡面烘焙圆盘中心叠动态技能图标。
            var disc = builder.Node("Loadout Detail", "Altar Disc", 232f, 12f, 178f, 168f).gameObject.AddComponent<Image>();
            disc.color = new Color(0.10f, 0.20f, 0.30f, 0.9f);
            disc.raycastTarget = false;
            var selectedIcon = builder.Node("Loadout Detail", "Selected Icon", 258f, 30f, 126f, 134f)
                .gameObject.AddComponent<Image>();
            selectedIcon.raycastTarget = false;

            // 卡面烘焙铭牌上放实时技能名。
            builder.TextNode("Loadout Detail", "Selected Name", 185f, 176f, 212f, 40f, string.Empty, 28,
                DetailNameInk, TextAnchor.MiddleCenter);
            var detail = builder.TextNode("Loadout Detail", "Selected Detail", 90f, 282f, 420f, 76f, string.Empty, 20,
                DescInk);
            detail.alignment = TextAnchor.MiddleCenter;

            SkinImage(builder, "Loadout Detail", "Split Divider", 180f, 366f, 240f, 26f, "prep-divider");

            // 装备与自动释放为真实玩法按钮，效果图未绘制，保留功能。
            var equip = SkinImage(builder, "Loadout Detail", "Equip", 175f, 402f, 250f, 52f, "shop-buy-gold", true);
            equip.gameObject.AddComponent<Button>().targetGraphic = equip;
            builder.TextNode("Loadout Detail/Equip", "Label", 0f, 0f, 250f, 52f, string.Empty, 24,
                BtnGoInk, TextAnchor.MiddleCenter);

            var auto = SkinImage(builder, "Loadout Detail", "Auto", 130f, 460f, 170f, 40f, "prep-nameplate", true);
            auto.gameObject.AddComponent<Button>().targetGraphic = auto;
            builder.TextNode("Loadout Detail/Auto", "Label", 0f, 0f, 170f, 40f, string.Empty, 18,
                SkillNameInk, TextAnchor.MiddleCenter);
            builder.TextNode("Loadout Detail", "Auto Detail", 310f, 460f, 280f, 40f, string.Empty, 16,
                DescInk, TextAnchor.MiddleLeft);
            builder.TextNode("Loadout Detail", "Passive Note", 90f, 508f, 420f, 28f, "被动效果来自装备、天赋与灵玉。", 16,
                DescInk, TextAnchor.MiddleCenter);
        }

        static void BuildActions(UiPrefabBuilder builder)
        {
            var actions = builder.AnchoredNode("", "Loadout Actions", AnchorPreset.BottomLeft,
                new Vector2(700f, 78f), new Vector2(25f, 22f));

            var back = SkinImage(builder, "Loadout Actions", "Return", 10f, 6f, 280f, 62f, "prep-btn-teal", true);
            back.gameObject.AddComponent<Button>().targetGraphic = back;
            builder.TextNode("Loadout Actions/Return", "Label", 40f, 8f, 200f, 46f, "返回修行", 24,
                BtnBackInk, TextAnchor.MiddleCenter);

            var challenge = SkinImage(builder, "Loadout Actions", "Challenge", 340f, 2f, 330f, 66f, "prep-btn-gold", true);
            challenge.gameObject.AddComponent<Button>().targetGraphic = challenge;
            builder.TextNode("Loadout Actions/Challenge", "Label", 60f, 10f, 240f, 46f, "前往挑战", 26,
                BtnGoInk, TextAnchor.MiddleCenter);
        }

        static void Bind(string prefabPath)
        {
            var contents = PrefabUtility.LoadPrefabContents(prefabPath);
            if (contents == null) return;
            try
            {
                var view = contents.GetComponent<LoadoutView>();
                if (view == null) view = contents.AddComponent<LoadoutView>();

                var so = new SerializedObject(view);
                Set(so, "backButton", ButtonAt(contents, "Header Back"));
                Set(so, "returnButton", ButtonAt(contents, "Loadout Actions/Return"));
                Set(so, "challengeButton", ButtonAt(contents, "Loadout Actions/Challenge"));

                var icons = new List<Image>();
                var rings = new List<Image>();
                var names = new List<Text>();
                var flags = new List<Image>();
                var buttons = new List<Button>();
                for (var i = 0; i < 4; i++)
                {
                    var path = "Skill Root/Skill " + i;
                    icons.Add(FindImage(contents, path + "/Icon"));
                    rings.Add(FindImage(contents, path + "/Ring"));
                    names.Add(FindText(contents, path + "/Name"));
                    flags.Add(FindImage(contents, path + "/Equipped Flag"));
                    buttons.Add(ButtonAt(contents, path));
                }

                SetArray(so, "skillIcons", icons);
                SetArray(so, "skillRings", rings);
                SetArray(so, "skillNames", names);
                SetArray(so, "skillEquippedFlags", flags);
                SetArray(so, "skillButtons", buttons);
                Set(so, "skillRoot", Find(contents, "Skill Root"));

                Set(so, "selectedIcon", FindImage(contents, "Loadout Detail/Selected Icon"));
                Set(so, "selectedName", FindText(contents, "Loadout Detail/Selected Name"));
                Set(so, "selectedDetail", FindText(contents, "Loadout Detail/Selected Detail"));
                Set(so, "equipButton", ButtonAt(contents, "Loadout Detail/Equip"));
                Set(so, "equipLabel", FindText(contents, "Loadout Detail/Equip/Label"));
                Set(so, "autoButton", ButtonAt(contents, "Loadout Detail/Auto"));
                Set(so, "autoLabel", FindText(contents, "Loadout Detail/Auto/Label"));
                Set(so, "autoDetail", FindText(contents, "Loadout Detail/Auto Detail"));
                so.ApplyModifiedPropertiesWithoutUndo();

                PrefabUtility.SaveAsPrefabAsset(contents, prefabPath);
                AssetDatabase.SaveAssets();
                Debug.Log("[Loadout] 已生成并绑定 " + prefabPath);
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

        static Image FindImage(GameObject root, string path)
        {
            var node = Find(root, path);
            return node != null ? node.GetComponent<Image>() : null;
        }

        static Button ButtonAt(GameObject root, string path)
        {
            var node = Find(root, path);
            return node != null ? node.GetComponent<Button>() : null;
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
