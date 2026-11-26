using System.Collections.Generic;
using Gxdl.Modules;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Gxdl.EditorTools
{
    /// <summary>
    /// 主界面 prefab 生成器。
    /// 使用 Art/主界面/主界面切图的独立素材；主界面文字和状态由 HomeView 刷新。
    /// </summary>
    [InitializeOnLoad]
    public static class HomePrefabSource
    {
        public const string PrefabPath = "Assets/Resources/" + UiScreens.Home + ".prefab";
        const string SkinPath = "Assets/Resources/Game/Guxu/Expansion/UI/HomeReskin/";

        static readonly Color Gold = ScreenPrefabKit.Gold;
        static readonly Color Cream = ScreenPrefabKit.Cream;
        static readonly Color Ink = new Color(0.16f, 0.13f, 0.09f);
        static readonly Color QuestJade = new Color(0.66f, 1f, 0.86f);

        static HomePrefabSource()
        {
            ScreenPrefabKit.Register("Home", Build);
        }

        [MenuItem("Gxdl/UI/生成/主界面", priority = 303)]
        public static void Generate()
        {
            var path = Build();
            if (path != null) EditorUtility.DisplayDialog("生成完成", "已生成\n" + path, "确定");
        }

        public static string Build()
        {
            using (var builder = new UiPrefabBuilder("HomeView",
                new Vector2(UiRootLayout.DesignWidth, UiRootLayout.DesignHeight)))
            {
                var root = builder.Root;
                // Home is a fixed art composition; FitPage is its only root layout owner.
                AnchorUtility.Apply(root, AnchorPreset.Center,
                    new Vector2(UiRootLayout.DesignWidth, UiRootLayout.DesignHeight), Vector2.zero);

                var background = builder.AnchoredNode("", "Background", AnchorPreset.Stretch, Vector2.zero)
                    .gameObject.AddComponent<Image>();
                background.color = Color.white;
                background.raycastTarget = false;

                BuildCharacterLayer(builder);
                BuildPlayerHud(builder);
                BuildSideEntries(builder);
                BuildSealGroup(builder);
                BuildQuest(builder);
                BuildRelicDecision(builder);
                BuildNavigation(builder);

                var path = builder.Save(PrefabPath);
                AssetDatabase.SaveAssets();
                if (path != null) Bind(path);
                return path;
            }
        }

        static void BuildCharacterLayer(UiPrefabBuilder builder)
        {
            // 坐标对照旧 Home()：角色与封石压在背景的岩台上。
            ChildImage(builder, "", "Seeker Ground Shadow", 112f, 955f, 166f, 36f,
                new Color(0.035f, 0.08f, 0.09f, 0.20f));

            var seeker = ChildImage(builder, "", "Seeker Character Layer", -25f, 440f, 400f, 527f, Color.white);
            seeker.preserveAspect = true;

            var stone = ChildImage(builder, "", "Sealed Stone Layer", 430f, 455f, 200f, 220f, Color.white);
            stone.preserveAspect = true;

            builder.Node("", "Companion A Zhuan", 515f, 300f, 145f, 125f);

            var relic = ChildImage(builder, "", "Pending Relic Display", 450f, 470f, 150f, 150f, Color.white);
            relic.preserveAspect = true;
            relic.gameObject.SetActive(false);
        }

        static void BuildPlayerHud(UiPrefabBuilder builder)
        {
            builder.Node("", "Player HUD", 24f, 12f, 702f, 108f);

            SkinImage(builder, "Player HUD", "Header Flourish", 78f, 52f, 606f, 23f, "home_img_line");
            var portrait = SkinImage(builder, "Player HUD", "Home Player Portrait", 0f, 0f, 96f, 102f,
                "img_toux");
            portrait.raycastTarget = true;
            portrait.gameObject.AddComponent<Button>().targetGraphic = portrait;

            var identity = builder.TextNode("Player HUD", "Player Identity", 101f, 0f, 286f, 32f, string.Empty, 27, Cream);
            identity.resizeTextForBestFit = true;
            identity.resizeTextMinSize = 16;
            identity.supportRichText = false;
            HeaderOutline(identity);
            HeaderOutline(builder.TextNode("Player HUD", "Player Power", 101f, 32f, 286f, 34f, string.Empty, 28, Gold));
            HeaderOutline(builder.TextNode("Player HUD", "Player Realm", 101f, 68f, 286f, 32f, string.Empty, 25, Cream));

            HitArea(builder, "Player HUD", "Cultivation Header", 97f, 0f, 290f, 104f);

            BuildBalance(builder, "Seal Token Plate", "Seal Token Icon", "Seal Token Balance", "home_btn_caiwu", 405f, 137f);
            BuildBalance(builder, "Spirit Dust Plate", "Spirit Dust Icon", "Spirit Stone Balance", "home_btn_jinbi", 572f, 129f);
        }

        static void BuildBalance(UiPrefabBuilder builder, string plateName, string iconName, string textName, string key, float x, float width)
        {
            var plate = SkinImage(builder, "Player HUD", plateName, x, 14f, width, 47f, key);
            // 图标已在底图中，旧图标节点仍提供精确的灵尘到账位置。
            var icon = ChildImage(builder, "Player HUD", iconName, x + 7f, 20f, 34f, 34f, Color.white);
            icon.enabled = plate.sprite == null;
            var amount = builder.TextNode("Player HUD", textName, x + 43f, 14f, width - 50f, 47f, string.Empty, 27, Cream);
            amount.resizeTextForBestFit = true;
            amount.resizeTextMinSize = 14;
            HeaderOutline(amount);
        }

        static void HeaderOutline(Text text)
        {
            var outline = text.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(0.06f, 0.13f, 0.19f, 0.85f);
            outline.effectDistance = new Vector2(1f, -1f);
        }

        static void BuildSideEntries(UiPrefabBuilder builder)
        {
            // 五入口等距排右；账号位在 accounts 不可用时整体隐藏。
            SideEntry(builder, "Entry 账号", "账号", 638f, 121f);
            SideEntry(builder, "Entry 商城", "商城", 638f, 230f);
            SideEntry(builder, "Entry 鉴宝台", "鉴宝台", 638f, 339f, "home_icon_xyy", 68f);
            SideEntry(builder, "Entry 资源秘境", "资源秘境", 638f, 448f, "home_icon_zymj", 72f);
            SideEntry(builder, "Entry 背包", "背包", 638f, 557f, "home_icon_bag", 74f);
        }

        static void SideEntry(UiPrefabBuilder builder, string nodeName, string label, float x, float y,
            string key = null, float labelTop = 72f)
        {
            HitArea(builder, "", nodeName, x, y, 104f, 106f);

            var art = SkinImage(builder, nodeName, "Art", 2f, 0f, 100f, 105f, key);
            var icon = ChildImage(builder, nodeName, "Icon", 18f, 6f, 68f, 68f, Color.white);
            icon.preserveAspect = true;
            icon.gameObject.SetActive(art.sprite == null);

            var text = builder.TextNode(nodeName, "Label", 0f, labelTop, 104f, 26f, label, 22,
                new Color32(45, 40, 31, 255), TextAnchor.MiddleCenter);
            text.fontStyle = FontStyle.Bold;
            if (art.sprite == null)
            {
                var outline = text.gameObject.AddComponent<Outline>();
                outline.effectColor = new Color32(255, 239, 206, 130);
                outline.effectDistance = new Vector2(.65f, -.65f);
            }
        }

        static void BuildSealGroup(UiPrefabBuilder builder)
        {
            var unseal = HitArea(builder, "", "Unseal", 375f, 803f, 345f, 111f);
            unseal.targetGraphic = SkinImage(builder, "Unseal", "Unseal Gold Art", 0f, 0f, 345f, 111f, "home_btn_pofen");
            unseal.transition = Selectable.Transition.ColorTint;
            builder.TextNode("Unseal", "Label", 44f, 8f, 257f, 94f, "破  封", 42,
                new Color(0.25f, 0.14f, 0.05f), TextAnchor.MiddleCenter);

            ChildImage(builder, "", "Seal Token Cost Icon", 492f, 921f, 30f, 30f, Color.white);
            builder.TextNode("", "Seal Token Cost", 530f, 915f, 175f, 38f, string.Empty, 24, Ink);

            SkinImage(builder, "", "Auto Options Plate", 423f, 960f, 280f, 47f, "home_bg_info_02");
            HitArea(builder, "", "Auto Unseal", 423f, 957f, 156f, 53f);
            ChildImage(builder, "Auto Unseal", "Toggle Track", 15f, 11f, 50f, 32f, Color.white);
            var thumb = builder.Node("Auto Unseal", "Toggle Thumb", 19f, 16f, 22f, 22f).gameObject.AddComponent<GuxuPortraitDisc>();
            thumb.color = new Color32(253, 232, 181, 255);
            thumb.raycastTarget = false;
            builder.TextNode("Auto Unseal", "Toggle Caption", 64f, 0f, 88f, 53f, "自动破封", 19, Ink, TextAnchor.MiddleCenter);

            HitArea(builder, "", "Retention", 579f, 957f, 124f, 53f);
            builder.TextNode("Retention", "Label", 4f, 0f, 100f, 53f, "保留规则", 21, Ink,
                TextAnchor.MiddleCenter);
        }

        static void BuildQuest(UiPrefabBuilder builder)
        {
            HitArea(builder, "", "Go Quest Target", 19f, 1048f, 458f, 141f);
            SkinImage(builder, "Go Quest Target", "Quest Plate", 0f, 0f, 458f, 141f, "home_bg_info_01");
            builder.TextNode("Go Quest Target", "Main Quest Title", 69f, 19f, 315f, 32f, string.Empty, 24, Gold);
            builder.TextNode("Go Quest Target", "Main Quest Target", 69f, 52f, 326f, 35f, string.Empty, 25, QuestJade);
            var progress = builder.TextNode("Go Quest Target", "Main Quest Progress", 69f, 88f, 340f, 42f, string.Empty, 20, Cream);
            progress.resizeTextForBestFit = true;
            progress.resizeTextMinSize = 17;
        }

        static void BuildRelicDecision(UiPrefabBuilder builder)
        {
            // 金纹横幅弹窗（官方图集 1000x496）：显示 700x346，分隔线烘焙，文字实时居中。
            var panel = SkinImage(builder, "", "Relic Decision", 25f, 876f, 700f, 346f, "relic-modal-panel");
            panel.raycastTarget = true;
            panel.gameObject.SetActive(false);

            ChildImage(builder, "Relic Decision", "Relic Icon", 60f, 62f, 140f, 138f, Color.white);
            builder.TextNode("Relic Decision", "Relic Name", 75f, 52f, 500f, 44f, string.Empty, 30,
                new Color(0.20f, 0.20f, 0.20f), TextAnchor.MiddleCenter);
            builder.TextNode("Relic Decision", "Relic Delta", 75f, 100f, 500f, 52f, string.Empty, 32,
                new Color(0.40f, 0.25f, 0.09f), TextAnchor.MiddleCenter);
            var affix = builder.TextNode("Relic Decision", "Relic Affix", 100f, 180f, 500f, 70f, string.Empty, 20,
                new Color(0.36f, 0.32f, 0.28f), TextAnchor.MiddleCenter);
            affix.lineSpacing = 1.1f;

            SkinButton(builder, "Relic Decision", "Resolve Equipment", 506f, 138f, 170f, 63f,
                "relic-btn-teal", "分解", new Color(0.98f, 0.96f, 0.89f));
            SkinButton(builder, "Relic Decision", "Equip Relic", 506f, 215f, 170f, 63f,
                "relic-btn-gold", "装备", new Color(0.26f, 0.17f, 0.06f));
        }

        static Button SkinButton(UiPrefabBuilder builder, string parent, string name, float x, float y,
            float width, float height, string key, string label, Color textColor)
        {
            var image = SkinImage(builder, parent, name, x, y, width, height, key);
            image.raycastTarget = true;
            var button = image.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            builder.TextNode(parent + "/" + name, "Label", 8f, 4f, width - 16f, height - 8f, label, 24,
                textColor, TextAnchor.MiddleCenter);
            return button;
        }

        static void BuildNavigation(UiPrefabBuilder builder)
        {
            // 底栏贴底，整条随屏幕高度移动；五格均分 150 宽。
            var bar = builder.AnchoredNode("", "Navigation Panel", AnchorPreset.StretchBottom, new Vector2(0f, 132f));
            var skin = bar.gameObject.AddComponent<Image>();
            skin.sprite = GetSkin("home_tab_btn_bg");
            skin.color = skin.sprite != null ? Color.white : new Color(0.055f, 0.15f, 0.2f, 0.95f);
            skin.raycastTarget = false;

            string[] names = { "洞府", "修行", "寻墟", "图鉴", "战备" };
            for (var i = 0; i < names.Length; i++)
            {
                HitArea(builder, "Navigation Panel", "Tab " + names[i], i * 150f, 0f, 150f, 132f);
                if (i == 2) ChildImage(builder, "Navigation Panel/Tab " + names[i], "Selected Tab Light",
                    15f, -4f, 120f, 100f, new Color(1f, 0.76f, 0.28f, 0.25f)).enabled = skin.sprite == null;
                ChildImage(builder, "Navigation Panel/Tab " + names[i], "Icon", 34f, 0f, 82f, 82f, Color.white)
                    .enabled = skin.sprite == null;
                builder.TextNode("Navigation Panel/Tab " + names[i], "Label", 5f, 89f, 140f, 34f, names[i], 26,
                    i == 2 ? new Color(1f, 0.91f, 0.68f) : Gold, TextAnchor.MiddleCenter);
            }
        }

        // —— 节点原语 ——

        static Image ChildImage(UiPrefabBuilder builder, string parent, string name, float x, float y,
            float width, float height, Color color, bool raycast = false)
        {
            return builder.ImageNode(parent, name, x, y, width, height, color, raycast);
        }

        // 洞府换皮切片在 Expansion/UI 根目录，旧主界面皮肤在 HomeReskin/ 子目录。
        static Sprite GetSkin(string key) => key == null ? null :
            AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Resources/Game/Guxu/Expansion/UI/" + key + ".png") ??
            AssetDatabase.LoadAssetAtPath<Sprite>(SkinPath + key + ".png");

        static Image SkinImage(UiPrefabBuilder builder, string parent, string name, float x, float y, float width, float height, string key)
        {
            var image = ChildImage(builder, parent, name, x, y, width, height, Color.white);
            image.sprite = GetSkin(key);
            image.enabled = image.sprite != null;
            return image;
        }

        static Button HitArea(UiPrefabBuilder builder, string parent, string name, float x, float y, float width, float height)
        {
            var node = builder.Node(parent, name, x, y, width, height);
            var hit = node.gameObject.AddComponent<UiRaycast>();
            hit.raycastTarget = true;
            var button = node.gameObject.AddComponent<Button>();
            button.targetGraphic = hit;
            button.transition = Selectable.Transition.None;
            return button;
        }

        static void PanelButton(UiPrefabBuilder builder, string parent, string name, float x, float y,
            float width, float height, string label)
        {
            var node = builder.Node(parent, name, x, y, width, height);
            var image = node.gameObject.AddComponent<Image>();
            image.color = label == "装备" ? new Color(0.8f, 0.66f, 0.36f) : new Color(0.15f, 0.38f, 0.35f);
            image.raycastTarget = true;
            node.gameObject.AddComponent<Button>().targetGraphic = image;
            builder.TextNode(parent + "/" + name, "Label", 0f, 0f, width, height, label, 24,
                label == "装备" ? new Color(0.25f, 0.14f, 0.05f) : Cream, TextAnchor.MiddleCenter);
        }

        static void Bind(string prefabPath)
        {
            var contents = PrefabUtility.LoadPrefabContents(prefabPath);
            if (contents == null) return;
            try
            {
                var view = contents.GetComponent<HomeView>();
                if (view == null) view = contents.AddComponent<HomeView>();

                var so = new SerializedObject(view);
                Set(so, "background", FindImage(contents, "Background"));
                Set(so, "seeker", FindImage(contents, "Seeker Character Layer"));
                Set(so, "stone", FindImage(contents, "Sealed Stone Layer"));
                Set(so, "pendingRelic", FindImage(contents, "Pending Relic Display"));
                Set(so, "companion", FindRect(contents, "Companion A Zhuan"));

                Set(so, "portraitButton", FindButton(contents, "Player HUD/Home Player Portrait"));
                Set(so, "identityText", FindText(contents, "Player HUD/Player Identity"));
                Set(so, "powerText", FindText(contents, "Player HUD/Player Power"));
                Set(so, "realmText", FindText(contents, "Player HUD/Player Realm"));
                Set(so, "sealTokenIcon", FindImage(contents, "Player HUD/Seal Token Icon"));
                Set(so, "sealTokenText", FindText(contents, "Player HUD/Seal Token Balance"));
                Set(so, "spiritStoneIcon", FindImage(contents, "Player HUD/Spirit Dust Icon"));
                Set(so, "spiritStoneText", FindText(contents, "Player HUD/Spirit Stone Balance"));
                Set(so, "cultivationButton", FindButton(contents, "Player HUD/Cultivation Header"));

                Set(so, "accountEntry", FindRect(contents, "Entry 账号"));
                Set(so, "accountButton", FindButton(contents, "Entry 账号"));
                Set(so, "accountIcon", FindImage(contents, "Entry 账号/Icon"));
                Set(so, "shopButton", FindButton(contents, "Entry 商城"));
                Set(so, "shopArt", FindImage(contents, "Entry 商城/Art"));
                Set(so, "appraisalButton", FindButton(contents, "Entry 鉴宝台"));
                Set(so, "appraisalArt", FindImage(contents, "Entry 鉴宝台/Art"));
                Set(so, "dungeonButton", FindButton(contents, "Entry 资源秘境"));
                Set(so, "dungeonArt", FindImage(contents, "Entry 资源秘境/Art"));
                Set(so, "bagButton", FindButton(contents, "Entry 背包"));
                Set(so, "bagArt", FindImage(contents, "Entry 背包/Art"));

                Set(so, "unsealButton", FindButton(contents, "Unseal"));
                Set(so, "unsealArt", FindImage(contents, "Unseal/Unseal Gold Art"));
                Set(so, "unsealLabel", FindText(contents, "Unseal/Label"));
                Set(so, "sealCostIcon", FindImage(contents, "Seal Token Cost Icon"));
                Set(so, "sealCostText", FindText(contents, "Seal Token Cost"));
                Set(so, "autoButton", FindButton(contents, "Auto Unseal"));
                Set(so, "autoTrack", FindImage(contents, "Auto Unseal/Toggle Track"));
                Set(so, "autoThumb", FindRect(contents, "Auto Unseal/Toggle Thumb"));
                Set(so, "autoLabel", FindText(contents, "Auto Unseal/Toggle Caption"));
                Set(so, "retentionButton", FindButton(contents, "Retention"));

                Set(so, "questButton", FindButton(contents, "Go Quest Target"));
                Set(so, "questTitle", FindText(contents, "Go Quest Target/Main Quest Title"));
                Set(so, "questTarget", FindText(contents, "Go Quest Target/Main Quest Target"));
                Set(so, "questUnderline", FindImage(contents, "Go Quest Target/Main Quest Underline"));
                Set(so, "questProgress", FindText(contents, "Go Quest Target/Main Quest Progress"));

                Set(so, "relicPanel", FindRect(contents, "Relic Decision"));
                Set(so, "relicIcon", FindImage(contents, "Relic Decision/Relic Icon"));
                Set(so, "relicName", FindText(contents, "Relic Decision/Relic Name"));
                Set(so, "relicDelta", FindText(contents, "Relic Decision/Relic Delta"));
                Set(so, "relicAffix", FindText(contents, "Relic Decision/Relic Affix"));
                Set(so, "relicDismantle", FindButton(contents, "Relic Decision/Resolve Equipment"));
                Set(so, "relicEquip", FindButton(contents, "Relic Decision/Equip Relic"));

                var tabs = new List<Button>();
                var icons = new List<Image>();
                string[] names = { "洞府", "修行", "寻墟", "图鉴", "战备" };
                for (var i = 0; i < names.Length; i++)
                {
                    tabs.Add(FindButton(contents, "Navigation Panel/Tab " + names[i]));
                    icons.Add(FindImage(contents, "Navigation Panel/Tab " + names[i] + "/Icon"));
                }
                SetArray(so, "navButtons", tabs);
                Set(so, "navPanel", FindImage(contents, "Navigation Panel"));
                SetArray(so, "navIcons", icons);

                so.ApplyModifiedPropertiesWithoutUndo();

                PrefabUtility.SaveAsPrefabAsset(contents, prefabPath);
                AssetDatabase.SaveAssets();
                Debug.Log("[Home] 已生成并绑定 " + prefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contents);
            }
        }

        static Image FindImage(GameObject root, string path)
        {
            var node = root.transform.Find(path);
            return node != null ? node.GetComponent<Image>() : null;
        }

        static RectTransform FindRect(GameObject root, string path)
        {
            return root.transform.Find(path) as RectTransform;
        }

        static Text FindText(GameObject root, string path)
        {
            var node = root.transform.Find(path);
            return node != null ? node.GetComponent<Text>() : null;
        }

        static Button FindButton(GameObject root, string path)
        {
            var node = root.transform.Find(path);
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
