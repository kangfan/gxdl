using System.Collections.Generic;
using Gxdl.Modules;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Gxdl.EditorTools
{
    /// <summary>
    /// 主界面 prefab 生成器。
    /// 只搭结构稳定的部分：顶栏玩家信息、右侧三个入口、破封按钮组、底部五格导航；
    /// 角色立绘、遗宝决策面板、主线任务条由 HomeView.Apply 在运行时按存档创建，
    /// 因此这里给动态层留一个空容器即可。
    /// </summary>
    [InitializeOnLoad]
    public static class HomePrefabSource
    {
        public const string PrefabPath = "Assets/Resources/" + UiScreens.Home + ".prefab";

        static readonly Color Backing = new Color(0.015f, 0.045f, 0.065f);
        static readonly Color Bar = new Color(0.055f, 0.15f, 0.2f, 0.95f);
        static readonly Color Gold = ScreenPrefabKit.Gold;
        static readonly Color Cream = ScreenPrefabKit.Cream;
        static readonly Color Primary = new Color(0.8f, 0.66f, 0.36f);
        static readonly Color Secondary = new Color(0.15f, 0.38f, 0.35f);

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
                AnchorUtility.Apply(root, AnchorPreset.Stretch, Vector2.zero, Vector2.zero);
                ScreenPrefabKit.AddRootComponents(root);

                // 场景背景图由控制器按资源情况替换，这里先给纯色底。
                var background = builder.AnchoredNode("", "Background", AnchorPreset.Stretch, Vector2.zero)
                    .gameObject.AddComponent<Image>();
                background.color = Backing;
                background.raycastTarget = false;

                BuildPlayerHud(builder);
                BuildSideEntries(builder);
                BuildSealGroup(builder);
                BuildNavigation(builder);

                // 动态层：角色、遗宝面板、任务条都挂在它下面，层级在静态元素之上。
                var dynamicLayer = builder.AnchoredNode("", "Dynamic Layer", AnchorPreset.Stretch, Vector2.zero);
                dynamicLayer.gameObject.AddComponent<CanvasGroup>().blocksRaycasts = false;

                var path = builder.Save(PrefabPath);
                AssetDatabase.SaveAssets();
                if (path != null) Bind(path);
                return path;
            }
        }

        static void BuildPlayerHud(UiPrefabBuilder builder)
        {
            // 顶栏沿用旧 Header() 的 24,12 / 702x108 尺寸。
            var hud = builder.Node("", "Player HUD", 24f, 12f, 702f, 108f);
            hud.gameObject.AddComponent<Image>().color = Bar;

            // 头像热区：图由控制器填，点击进入改名。
            var portrait = builder.Node("Player HUD", "Home Player Portrait", 0f, 3f, 84f, 84f);
            var portraitImage = portrait.gameObject.AddComponent<Image>();
            portraitImage.color = new Color(0.2f, 0.3f, 0.33f, 1f);
            portraitImage.raycastTarget = true;
            portrait.gameObject.AddComponent<Button>().targetGraphic = portraitImage;

            var identity = builder.TextNode("Player HUD", "Player Identity", 98f, 0f, 255f, 32f, string.Empty, 26, Cream);
            identity.resizeTextForBestFit = true;
            identity.resizeTextMinSize = 16;
            identity.supportRichText = false;
            identity.resizeTextForBestFit = true;
            identity.resizeTextMinSize = 16;
            identity.supportRichText = false;

            builder.TextNode("Player HUD", "Player Power", 98f, 32f, 255f, 34f, string.Empty, 27, Gold);
            builder.TextNode("Player HUD", "Player Realm", 98f, 68f, 255f, 32f, string.Empty, 24, Cream);

            // 修行入口覆盖身份区，点击打开境界页。
            var cultivation = builder.Node("Player HUD", "Cultivation Header", 94f, 0f, 260f, 104f);
            var cultivationImage = cultivation.gameObject.AddComponent<Image>();
            cultivationImage.color = new Color(0f, 0f, 0f, 0.001f);
            cultivationImage.raycastTarget = true;
            cultivation.gameObject.AddComponent<Button>().targetGraphic = cultivationImage;

            // 两种货币的数值文本给足宽度并自动缩字，避免十位数溢出。
            builder.TextNode("Player HUD", "Seal Token Balance", 405f, 14f, 120f, 45f, string.Empty, 27, Cream)
                .resizeTextForBestFit = true;
            builder.TextNode("Player HUD", "Spirit Stone Balance", 574f, 14f, 128f, 45f, string.Empty, 27, Cream)
                .resizeTextForBestFit = true;
        }

        static void BuildSideEntries(UiPrefabBuilder builder)
        {
            // 三个入口贴右，纵向 140 间距，与旧 FinalSideEntry 的 630/210 起点一致。
            SideEntry(builder, "Entry 鉴宝台", "鉴宝台", 630f, 210f);
            SideEntry(builder, "Entry 资源秘境", "资源秘境", 630f, 350f);
            SideEntry(builder, "Entry 背包", "背包", 630f, 490f);
        }

        static void SideEntry(UiPrefabBuilder builder, string nodeName, string label, float x, float y)
        {
            var node = builder.Node("", nodeName, x, y, 108f, 130f);
            var image = node.gameObject.AddComponent<Image>();
            image.color = Secondary;
            image.raycastTarget = true;
            node.gameObject.AddComponent<Button>().targetGraphic = image;
            builder.TextNode(nodeName, "Label", 2f, 99f, 104f, 30f, label, 22, Cream, TextAnchor.MiddleCenter);
        }

        static void BuildSealGroup(UiPrefabBuilder builder)
        {
            // 破封主按钮：300x88，文字颜色随破封符数量由 View 改写。
            builder.Node("", "Unseal", 320f, 751f, 300f, 88f);
            var unseal = builder.GetOrAddComponent<Image>("Unseal");
            unseal.color = Primary;
            unseal.raycastTarget = true;
            builder.GetOrAddComponent<Button>("Unseal").targetGraphic = unseal;
            builder.TextNode("Unseal", "Label", 25f, 4f, 250f, 80f, "破  封", 36, Gold, TextAnchor.MiddleCenter);

            builder.TextNode("", "Seal Token Cost", 449f, 837f, 168f, 38f, string.Empty, 24, Cream);

            builder.Node("", "Auto Unseal", 315f, 875f, 188f, 88f);
            var auto = builder.GetOrAddComponent<Image>("Auto Unseal");
            auto.color = Secondary;
            auto.raycastTarget = true;
            builder.GetOrAddComponent<Button>("Auto Unseal").targetGraphic = auto;
            builder.TextNode("Auto Unseal", "Label", 0f, 0f, 188f, 88f, "自动破封", 24, Cream, TextAnchor.MiddleCenter);

            builder.Node("", "Retention", 510f, 875f, 132f, 88f);
            var retention = builder.GetOrAddComponent<Image>("Retention");
            retention.color = Secondary;
            retention.raycastTarget = true;
            builder.GetOrAddComponent<Button>("Retention").targetGraphic = retention;
            builder.TextNode("Retention", "Label", 0f, 0f, 132f, 88f, "保留规则 ›", 24, Cream, TextAnchor.MiddleCenter);
        }

        static void BuildNavigation(UiPrefabBuilder builder)
        {
            // 底栏整条贴底，五格均分 150 宽；文字进 prefab，图标由控制器按资源补。
            var bar = builder.AnchoredNode("", "Navigation", AnchorPreset.StretchBottom, new Vector2(0f, 109f));
            bar.gameObject.AddComponent<Image>().color = Bar;

            string[] names = { "洞府", "修行", "寻墟", "图鉴", "战备" };
            for (var i = 0; i < names.Length; i++)
            {
                var tab = builder.Node("Navigation", "Tab " + names[i], i * 150f, 0f, 150f, 109f);
                var image = tab.gameObject.AddComponent<Image>();
                image.color = new Color(0f, 0f, 0f, 0.001f);
                image.raycastTarget = true;
                tab.gameObject.AddComponent<Button>().targetGraphic = image;
                builder.TextNode("Navigation/Tab " + names[i], "Label", 5f, 65f, 140f, 36f, names[i], 25, Gold,
                    TextAnchor.MiddleCenter);
            }
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
                Set(so, "portraitButton", Find(contents, "Player HUD/Home Player Portrait"));
                Set(so, "identityText", FindText(contents, "Player HUD/Player Identity"));
                Set(so, "powerText", FindText(contents, "Player HUD/Player Power"));
                Set(so, "realmText", FindText(contents, "Player HUD/Player Realm"));
                Set(so, "sealTokenText", FindText(contents, "Player HUD/Seal Token Balance"));
                Set(so, "spiritStoneText", FindText(contents, "Player HUD/Spirit Stone Balance"));
                Set(so, "cultivationButton", Find(contents, "Player HUD/Cultivation Header"));

                Set(so, "appraisalButton", Find(contents, "Entry 鉴宝台"));
                Set(so, "dungeonButton", Find(contents, "Entry 资源秘境"));
                Set(so, "bagButton", Find(contents, "Entry 背包"));

                Set(so, "unsealButton", Find(contents, "Unseal"));
                Set(so, "unsealLabel", FindText(contents, "Unseal/Label"));
                Set(so, "sealCostText", FindText(contents, "Seal Token Cost"));
                Set(so, "autoButton", Find(contents, "Auto Unseal"));
                Set(so, "autoLabel", FindText(contents, "Auto Unseal/Label"));
                Set(so, "retentionButton", Find(contents, "Retention"));

                var tabs = new List<Button>();
                string[] names = { "洞府", "修行", "寻墟", "图鉴", "战备" };
                for (var i = 0; i < names.Length; i++) tabs.Add(FindButton(contents, "Navigation/Tab " + names[i]));
                SetArray(so, "navButtons", tabs);

                Set(so, "dynamicLayer", Find(contents, "Dynamic Layer"));
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

        static Component Find(GameObject root, string path)
        {
            var node = root.transform.Find(path);
            return node;
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
