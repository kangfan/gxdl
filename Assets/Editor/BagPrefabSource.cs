using System.Collections.Generic;
using Gxdl.Modules;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Gxdl.EditorTools
{
    /// <summary>
    /// 背包 prefab 生成器。按 Art/背包 效果图搭骨架：居中大面板 + 12 装备槽 +
    /// 装备详情子面板（缩放动画由 BagView 驱动）。切片来源见 ResourceCache/bag-20261125。
    /// </summary>
    [InitializeOnLoad]
    public static class BagPrefabSource
    {
        public const string PrefabPath = "Assets/Resources/" + UiScreens.Bag + ".prefab";
        const string SkinPath = "Assets/Resources/Game/Guxu/Expansion/UI/";

        static readonly Color TitleGold = FromHex(0x9B6728);
        static readonly Color HeaderInk = FromHex(0x363D47);
        static readonly Color SlotLabelInk = FromHex(0x3A414A);
        static readonly Color FooterInk = FromHex(0x3F4750);
        static readonly Color BadgeText = FromHex(0xFFFFFE);
        static readonly Color NameInk = FromHex(0x3D4248);
        static readonly Color SubtitleInk = FromHex(0x6A5944);
        static readonly Color SectionInk = FromHex(0x5C4B36);
        static readonly Color StatInk = FromHex(0x4C4C4C);
        static readonly Color DescInk = FromHex(0x4B4C4D);
        static readonly Color ForgeText = FromHex(0x5C4318);
        static readonly Color GemText = FromHex(0x394A53);

        static Color FromHex(long value) => new Color(
            ((value >> 16) & 0xFF) / 255f,
            ((value >> 8) & 0xFF) / 255f,
            (value & 0xFF) / 255f);

        static BagPrefabSource()
        {
            ScreenPrefabKit.Register("Bag", Build);
        }

        [MenuItem("Gxdl/UI/生成/背包", priority = 305)]
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

        static void Text(UiPrefabBuilder builder, string parent, string name,
            float x, float y, float w, float h, string value, int size, Color color, TextAnchor anchor)
        {
            builder.TextNode(parent, name, x, y, w, h, value, size, color, anchor);
        }

        public static string Build()
        {
            using (var builder = new UiPrefabBuilder("BagView",
                new Vector2(UiRootLayout.DesignWidth, UiRootLayout.DesignHeight)))
            {
                var root = builder.Root;
                AnchorUtility.Apply(root, AnchorPreset.Stretch, Vector2.zero, Vector2.zero);
                ScreenPrefabKit.AddRootComponents(root);

                var blocker = builder.AnchoredNode("", "Blocker", AnchorPreset.Stretch, Vector2.zero)
                    .gameObject.AddComponent<UiRaycast>();
                blocker.raycastTarget = true;

                BuildBagPanel(builder);
                BuildDetailPanel(builder);

                var path = builder.Save(PrefabPath);
                AssetDatabase.SaveAssets();
                if (path != null) Bind(path);
                return path;
            }
        }

        static void BuildBagPanel(UiPrefabBuilder builder)
        {
            // 图集面板切片 710x587，显示 682x563；分隔线/标题饰带已烘焙，文字实时。
            var panel = SkinImage(builder, "", "BagPanel", 34f, 368f, 682f, 563f, "bag-panel", true);

            Text(builder, "BagPanel", "Title", 96f, 7f, 91f, 53f, "背包", 34, TitleGold, TextAnchor.MiddleCenter);
            var close = SkinImage(builder, "BagPanel", "BtnClose", 598f, 10f, 71f, 72f, "bag-close", true);
            close.gameObject.AddComponent<Button>().targetGraphic = close;

            Text(builder, "BagPanel", "Section Label", 298f, 84f, 100f, 24f, "当前装备", 17, HeaderInk,
                TextAnchor.MiddleCenter);

            string[] slots = { "兵器", "头冠", "羽翼", "靴子", "腰带", "护腕", "葫芦", "项链", "玉佩", "幡旗", "宝珠", "道袍" };
            float[] xs = { 67f, 217f, 367f, 517f };
            float[] ys = { 123f, 240f, 357f };
            for (var i = 0; i < 12; i++)
            {
                var name = "BagPanel/Slot " + i;
                var x = xs[i % 4];
                var y = ys[i / 4];
                var cell = SkinImage(builder, "BagPanel", "Slot " + i, x, y, 97f, 85f, "bag-slot", true);
                cell.gameObject.AddComponent<Button>().targetGraphic = cell;
                var icon = builder.Node(name, "Icon", 8f, 8f, 81f, 69f).gameObject.AddComponent<Image>();
                icon.raycastTarget = false;
                SkinImage(builder, name, "Badge", 50f, 2f, 52f, 26f, "bag-badge");
                Text(builder, name + "/Badge", "Value", 10f, 2f, 32f, 22f, string.Empty, 15, BadgeText,
                    TextAnchor.MiddleCenter);
                Text(builder, name, "Label", 0f, 89f, 97f, 22f, slots[i], 19, SlotLabelInk, TextAnchor.MiddleCenter);
            }

            var cloud = SkinImage(builder, "BagPanel", "Cloud", -15f, 430f, 221f, 106f, "bag-cloud");
            cloud.enabled = cloud.sprite != null;
            Text(builder, "BagPanel", "Footer Note", 139f, 518f, 384f, 26f, "点击装备查看属性 · 锻造等级随槽位保留", 16,
                FooterInk, TextAnchor.MiddleCenter);
        }

        static void BuildDetailPanel(UiPrefabBuilder builder)
        {
            // 图集面板切片 655x907，显示 373x516；分段饰线左右对切，中间实时文字。
            var panel = SkinImage(builder, "", "DetailPanel", 193f, 420f, 373f, 516f, "gear-detail-panel", true);
            panel.gameObject.SetActive(false);

            // 关闭按钮用新获得装备图集中的深色圆形 X。
            var close = SkinImage(builder, "DetailPanel", "BtnClose2", 302f, -4f, 64f, 70f, "relic-close", true);
            close.gameObject.AddComponent<Button>().targetGraphic = close;

            SkinImage(builder, "DetailPanel", "Icon Box", 17f, 48f, 142f, 127f, "gear-detail-iconbox");
            var icon = builder.Node("DetailPanel/Icon Box", "Icon", 12f, 11f, 118f, 105f).gameObject.AddComponent<Image>();
            icon.raycastTarget = false;
            SkinImage(builder, "DetailPanel/Icon Box", "Badge", 85f, 9f, 52f, 26f, "bag-badge");
            Text(builder, "DetailPanel/Icon Box/Badge", "Value", 10f, 2f, 32f, 22f, string.Empty, 16, BadgeText,
                TextAnchor.MiddleCenter);

            Text(builder, "DetailPanel", "Gear Name", 173f, 86f, 160f, 34f, string.Empty, 26, NameInk,
                TextAnchor.MiddleLeft);
            Text(builder, "DetailPanel", "Gear Subtitle", 173f, 126f, 185f, 26f, string.Empty, 16, SubtitleInk,
                TextAnchor.MiddleLeft);

            BuildSection(builder, "Header 1", 171f, "基础属性");
            BuildSection(builder, "Header 2", 276f, "附加属性");
            BuildSection(builder, "Header 3", 371f, "装备描述");

            string[] statNames = { "攻击力", "生命值", "防御力", "敏捷度" };
            string[] statIcons = { "effect-attack", "effect-health", "effect-defense", "effect-speed" };
            float[] statXs = { 57f, 190f };
            float[] statYs = { 214f, 244f };
            for (var i = 0; i < 4; i++)
            {
                var name = "Stat " + i;
                var x = statXs[i % 2];
                var y = statYs[i / 2];
                var statIcon = SkinImage(builder, "DetailPanel", name, x, y, 22f, 22f, statIcons[i]);
                statIcon.preserveAspect = true;
                Text(builder, "DetailPanel", name + " Label", x + 25f, y, 55f, 24f, statNames[i], 17, StatInk,
                    TextAnchor.MiddleLeft);
                Text(builder, "DetailPanel", name + " Value", x + 82f, y, 60f, 24f, string.Empty, 17, StatInk,
                    TextAnchor.MiddleLeft);
            }

            Text(builder, "DetailPanel", "Affix Value", 57f, 308f, 250f, 46f, string.Empty, 17, StatInk,
                TextAnchor.MiddleLeft);
            var desc = builder.TextNode("DetailPanel", "Desc Line", 40f, 406f, 285f, 52f, string.Empty, 17, DescInk,
                TextAnchor.UpperLeft);
            desc.lineSpacing = 1.15f;

            BuildActionButton(builder, "BtnForge", 45f, 452f, 154f, 59f, "gear-btn-gold", "槽位锻造", ForgeText);
            BuildActionButton(builder, "BtnGem", 215f, 455f, 154f, 50f, "gear-btn-teal", "灵玉镶嵌", GemText);
        }

        /// <summary>分段饰线：图集对切左右两片，中间实时文字；右片水平翻转对称。</summary>
        static void BuildSection(UiPrefabBuilder builder, string name, float y, string label)
        {
            var left = SkinImage(builder, "DetailPanel", name + " L", 32f, y, 92f, 28f, "gear-detail-header-l");
            left.raycastTarget = false;
            var right = SkinImage(builder, "DetailPanel", name + " R", 254f, y, 87f, 28f, "gear-detail-header-r");
            right.raycastTarget = false;
            var rect = (RectTransform)right.transform;
            rect.localScale = new Vector3(-1f, 1f, 1f);
            Text(builder, "DetailPanel", name + " Label", 124f, y + 1f, 130f, 26f, label, 15, SectionInk,
                TextAnchor.MiddleCenter);
        }

        static void BuildActionButton(UiPrefabBuilder builder, string name, float x, float y,
            float width, float height, string key, string label, Color textColor)
        {
            var image = SkinImage(builder, "DetailPanel", name, x, y, width, height, key, true);
            image.gameObject.AddComponent<Button>().targetGraphic = image;
            builder.TextNode("DetailPanel/" + name, "Label", 8f, 4f, width - 16f, height - 8f, label, 18, textColor,
                TextAnchor.MiddleCenter);
        }

        static void Bind(string prefabPath)
        {
            var contents = PrefabUtility.LoadPrefabContents(prefabPath);
            if (contents == null) return;
            try
            {
                var view = contents.GetComponent<BagView>();
                if (view == null) view = contents.AddComponent<BagView>();

                var so = new SerializedObject(view);
                Set(so, "bagPanel", Find(contents, "BagPanel")?.GetComponent<RectTransform>());
                Set(so, "closeButton", Find(contents, "BagPanel/BtnClose")?.GetComponent<Button>());
                Set(so, "detailPanel", Find(contents, "DetailPanel")?.GetComponent<RectTransform>());
                Set(so, "detailIcon", Find(contents, "DetailPanel/Icon Box/Icon")?.GetComponent<Image>());
                Set(so, "detailBadge", Find(contents, "DetailPanel/Icon Box/Badge/Value")?.GetComponent<Text>());
                Set(so, "detailName", Find(contents, "DetailPanel/Gear Name")?.GetComponent<Text>());
                Set(so, "detailSubtitle", Find(contents, "DetailPanel/Gear Subtitle")?.GetComponent<Text>());
                Set(so, "affixValue", Find(contents, "DetailPanel/Affix Value")?.GetComponent<Text>());
                Set(so, "descLine", Find(contents, "DetailPanel/Desc Line")?.GetComponent<Text>());
                Set(so, "forgeButton", Find(contents, "DetailPanel/BtnForge")?.GetComponent<Button>());
                Set(so, "gemButton", Find(contents, "DetailPanel/BtnGem")?.GetComponent<Button>());
                Set(so, "detailCloseButton", Find(contents, "DetailPanel/BtnClose2")?.GetComponent<Button>());

                var slotButtons = new List<Button>();
                var slotIcons = new List<Image>();
                var slotBadges = new List<Text>();
                var statValues = new List<Text>();
                for (var i = 0; i < 12; i++)
                {
                    slotButtons.Add(Find(contents, "BagPanel/Slot " + i)?.GetComponent<Button>());
                    slotIcons.Add(Find(contents, "BagPanel/Slot " + i + "/Icon")?.GetComponent<Image>());
                    slotBadges.Add(Find(contents, "BagPanel/Slot " + i + "/Badge/Value")?.GetComponent<Text>());
                }
                for (var i = 0; i < 4; i++)
                    statValues.Add(Find(contents, "DetailPanel/Stat " + i + " Value")?.GetComponent<Text>());
                SetArray(so, "slotButtons", slotButtons);
                SetArray(so, "slotIcons", slotIcons);
                SetArray(so, "slotBadges", slotBadges);
                SetArray(so, "statValues", statValues);
                so.ApplyModifiedPropertiesWithoutUndo();

                PrefabUtility.SaveAsPrefabAsset(contents, prefabPath);
                AssetDatabase.SaveAssets();
                Debug.Log("[Bag] 已生成并绑定 " + prefabPath);
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
