using System.Collections.Generic;
using Gxdl.Modules;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Gxdl.EditorTools
{
    /// <summary>
    /// 锻造 prefab 生成器。
    /// 12 个槽位、3 项加成与 2 行消耗静态建出，
    /// 等级、材料与可用性由 ForgeView.Apply 在运行时填入。
    /// </summary>
    [InitializeOnLoad]
    public static class ForgePrefabSource
    {
        public const string PrefabPath = "Assets/Resources/" + UiScreens.Forge + ".prefab";

        static ForgePrefabSource()
        {
            ScreenPrefabKit.Register("Forge", Build);
        }

        [MenuItem("Gxdl/UI/生成/锻造", priority = 311)]
        public static void Generate()
        {
            var path = Build();
            if (path != null) EditorUtility.DisplayDialog("生成完成", "已生成\n" + path, "确定");
        }

        public static string Build()
        {
            using (var builder = new UiPrefabBuilder("ForgeView",
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
                BuildSlots(builder);
                BuildWorkbench(builder);
                BuildFooter(builder);

                var path = builder.Save(PrefabPath);
                AssetDatabase.SaveAssets();
                if (path != null) Bind(path);
                return path;
            }
        }

        static void BuildHeader(UiPrefabBuilder builder)
        {
            var backNode = builder.AnchoredNode("", "Header Back", AnchorPreset.TopLeft, new Vector2(174f, 52f), new Vector2(15f, 24f));
            var backImage = backNode.gameObject.AddComponent<Image>();
            backImage.color = new Color(0.15f, 0.38f, 0.35f, 0.94f);
            backImage.raycastTarget = true;
            backNode.gameObject.AddComponent<Button>().targetGraphic = backImage;
            builder.TextNode("Header Back", "Label", 0f, 0f, 174f, 52f, "‹ 返回修行", 24, ScreenPrefabKit.Cream, TextAnchor.MiddleCenter);

            builder.TextNode("", "Title", 205f, 14f, 340f, 65f, "槽位锻造", 42, ScreenPrefabKit.Gold);

            // 货币图标是静态配置，数量文本由控制器填。
            builder.ImageNode("", "Ore Icon", 207f, 83f, 32f, 32f, Color.white);
            builder.TextNode("", "Forge Ore Balance", 241f, 83f, 170f, 32f, string.Empty, 23, ScreenPrefabKit.Cream);
            builder.ImageNode("", "Dust Icon", 420f, 83f, 32f, 32f, Color.white);
            builder.TextNode("", "Forge Dust Balance", 457f, 83f, 230f, 32f, string.Empty, 23, ScreenPrefabKit.Cream);
            builder.TextNode("", "Forge Hint", 65f, 120f, 620f, 31f, "锻造绑定槽位 · 更换遗宝后继承", 23, ScreenPrefabKit.Cream);
        }

        static void BuildSlots(UiPrefabBuilder builder)
        {
            builder.Node("", "Forge Slot Root", 0f, 160f, UiRootLayout.DesignWidth, 214f);

            for (var i = 0; i < 12; i++)
            {
                var path = "Forge Slot Root/Slot " + i;
                var cell = builder.Node("Forge Slot Root", "Slot " + i, 18f + i % 6 * 122f, i / 6 * 110f, 108f, 104f);
                cell.gameObject.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.001f);

                builder.ImageNode(path, "Icon", 12f, 0f, 83f, 83f, Color.white);
                builder.ImageNode(path, "Frame", 6f, -3f, 95f, 95f, new Color(0.86f, 0.78f, 0.54f, 0.16f));
                builder.ImageNode(path, "Plate", 0f, 77f, 108f, 28f, new Color(0f, 0f, 0f, 0.35f));
                builder.TextNode(path, "Level", 0f, 76f, 108f, 30f, string.Empty, 21,
                    ScreenPrefabKit.Cream, TextAnchor.MiddleCenter);
                builder.TextNode(path, "Mark", 73f, 3f, 22f, 22f, string.Empty, 18,
                    ScreenPrefabKit.Gold, TextAnchor.MiddleCenter);

                var button = cell.gameObject.AddComponent<Button>();
                button.targetGraphic = cell.gameObject.GetComponent<Image>();
                button.transition = Selectable.Transition.None;
            }
        }

        static void BuildWorkbench(UiPrefabBuilder builder)
        {
            var stage = builder.Node("", "Forge Workbench", 0f, 385f, UiRootLayout.DesignWidth, 405f);

            builder.ImageNode("Forge Workbench", "Selected Equipment", 285f, 95f, 180f, 180f, Color.white);
            builder.ImageNode("Forge Workbench", "Name Plate", 35f, 77f, 192f, 58f, new Color(0f, 0f, 0f, 0.35f));
            builder.TextNode("Forge Workbench", "Selected Name", 45f, 83f, 172f, 46f, string.Empty, 32,
                ScreenPrefabKit.Cream, TextAnchor.MiddleCenter);
            builder.ImageNode("Forge Workbench", "Tier Plate", 35f, 145f, 192f, 42f, new Color(0f, 0f, 0f, 0.35f));
            builder.TextNode("Forge Workbench", "Selected Tier", 43f, 145f, 176f, 42f, string.Empty, 24,
                ScreenPrefabKit.Cream, TextAnchor.MiddleCenter);
            builder.ImageNode("Forge Workbench", "Progress Plate", 175f, 342f, 400f, 53f, new Color(0f, 0f, 0f, 0.35f));
            builder.TextNode("Forge Workbench", "Selected Progress", 188f, 344f, 374f, 49f, string.Empty, 28,
                ScreenPrefabKit.Cream, TextAnchor.MiddleCenter);
        }

        static void BuildFooter(UiPrefabBuilder builder)
        {
            var footer = builder.Node("", "Forge Footer", 0f, 814f, UiRootLayout.DesignWidth, 520f);
            var tray = builder.Node("Forge Footer", "Summary", 25f, 0f, 700f, 310f);
            tray.gameObject.AddComponent<Image>().color = ScreenPrefabKit.Panel;

            builder.TextNode("Forge Footer/Summary", "Bonus Title", 55f, 18f, 590f, 36f, string.Empty, 25,
                ScreenPrefabKit.Cream);

            for (var i = 0; i < 3; i++)
            {
                var path = "Forge Footer/Summary/Stat " + i;
                var x = 44f + i * 210f;
                builder.ImageNode("Forge Footer/Summary", "Stat " + i, x, 61f, 210f, 104f, new Color(0f, 0f, 0f, 0.001f));
                builder.ImageNode(path, "Icon", 55f, 61f, 46f, 46f, Color.white);
                builder.TextNode(path, "Name", 105f, 63f, 84f, 42f, string.Empty, 28, ScreenPrefabKit.Cream);
                builder.TextNode(path, "Current", 0f, 121f, 89f, 44f, string.Empty, 29,
                    ScreenPrefabKit.Cream, TextAnchor.MiddleCenter);
                builder.TextNode(path, "Arrow", 87f, 121f, 31f, 44f, "→", 27, ScreenPrefabKit.Cream, TextAnchor.MiddleCenter);
                builder.TextNode(path, "Next", 119f, 121f, 91f, 44f, string.Empty, 31,
                    new Color(0.86f, 0.62f, 0.3f), TextAnchor.MiddleCenter);
            }

            builder.ImageNode("Forge Footer/Summary", "Divider", 50f, 184f, 600f, 1f, new Color(0.4f, 0.3f, 0.12f, 0.25f));

            builder.ImageNode("Forge Footer/Summary", "Ore Icon", 48f, 205f, 35f, 35f, Color.white);
            builder.TextNode("Forge Footer/Summary", "Ore Cost", 98f, 203f, 552f, 38f, string.Empty, 24, ScreenPrefabKit.Cream);
            builder.ImageNode("Forge Footer/Summary", "Dust Icon", 48f, 249f, 35f, 35f, Color.white);
            builder.TextNode("Forge Footer/Summary", "Dust Cost", 98f, 247f, 552f, 38f, string.Empty, 24, ScreenPrefabKit.Cream);

            var upgrade = builder.Node("Forge Footer", "Upgrade", 65f, 344f, 402f, 88f);
            var upgradeImage = upgrade.gameObject.AddComponent<Image>();
            upgradeImage.color = new Color(0.15f, 0.38f, 0.35f, 0.94f);
            upgradeImage.raycastTarget = true;
            upgrade.gameObject.AddComponent<Button>().targetGraphic = upgradeImage;
            builder.TextNode("Forge Footer/Upgrade", "Label", 0f, 0f, 402f, 88f, string.Empty, 28,
                ScreenPrefabKit.Cream, TextAnchor.MiddleCenter);

            var getMore = builder.Node("Forge Footer", "Get More", 495f, 361f, 195f, 52f);
            var getMoreImage = getMore.gameObject.AddComponent<Image>();
            getMoreImage.color = new Color(0.15f, 0.38f, 0.35f, 0.94f);
            getMoreImage.raycastTarget = true;
            getMore.gameObject.AddComponent<Button>().targetGraphic = getMoreImage;
            builder.TextNode("Forge Footer/Get More", "Label", 0f, 0f, 195f, 52f, "获取玄铁", 24,
                ScreenPrefabKit.Cream, TextAnchor.MiddleCenter);

            builder.ImageNode("Forge Footer", "Summary Plate", 25f, 460f, 700f, 50f, new Color(0f, 0f, 0f, 0.35f));
            builder.TextNode("Forge Footer", "Summary Text", 40f, 465f, 670f, 40f, string.Empty, 23,
                ScreenPrefabKit.Gold, TextAnchor.MiddleCenter);
        }

        static void Bind(string prefabPath)
        {
            var contents = PrefabUtility.LoadPrefabContents(prefabPath);
            if (contents == null) return;
            try
            {
                var view = contents.GetComponent<ForgeView>();
                if (view == null) view = contents.AddComponent<ForgeView>();

                var so = new SerializedObject(view);
                Set(so, "backButton", ButtonAt(contents, "Header Back"));
                Set(so, "oreBalance", FindText(contents, "Forge Ore Balance"));
                Set(so, "dustBalance", FindText(contents, "Forge Dust Balance"));
                Set(so, "hintText", FindText(contents, "Forge Hint"));

                var icons = new List<Image>();
                var frames = new List<Image>();
                var levels = new List<Text>();
                var marks = new List<Text>();
                var buttons = new List<Button>();
                for (var i = 0; i < 12; i++)
                {
                    var path = "Forge Slot Root/Slot " + i;
                    icons.Add(FindImage(contents, path + "/Icon"));
                    frames.Add(FindImage(contents, path + "/Frame"));
                    levels.Add(FindText(contents, path + "/Level"));
                    marks.Add(FindText(contents, path + "/Mark"));
                    buttons.Add(ButtonAt(contents, path));
                }

                SetArray(so, "slotIcons", icons);
                SetArray(so, "slotFrames", frames);
                SetArray(so, "slotLevels", levels);
                SetArray(so, "slotMarks", marks);
                SetArray(so, "slotButtons", buttons);
                Set(so, "slotRoot", Find(contents, "Forge Slot Root"));

                Set(so, "selectedIcon", FindImage(contents, "Forge Workbench/Selected Equipment"));
                Set(so, "selectedName", FindText(contents, "Forge Workbench/Selected Name"));
                Set(so, "selectedTier", FindText(contents, "Forge Workbench/Selected Tier"));
                Set(so, "selectedProgress", FindText(contents, "Forge Workbench/Selected Progress"));

                Set(so, "bonusTitle", FindText(contents, "Forge Footer/Summary/Bonus Title"));
                var names = new List<Text>();
                var current = new List<Text>();
                var next = new List<Text>();
                for (var i = 0; i < 3; i++)
                {
                    var path = "Forge Footer/Summary/Stat " + i;
                    names.Add(FindText(contents, path + "/Name"));
                    current.Add(FindText(contents, path + "/Current"));
                    next.Add(FindText(contents, path + "/Next"));
                }

                SetArray(so, "bonusNames", names);
                SetArray(so, "bonusCurrent", current);
                SetArray(so, "bonusNext", next);

                Set(so, "oreCost", FindText(contents, "Forge Footer/Summary/Ore Cost"));
                Set(so, "dustCost", FindText(contents, "Forge Footer/Summary/Dust Cost"));
                Set(so, "upgradeButton", ButtonAt(contents, "Forge Footer/Upgrade"));
                Set(so, "upgradeLabel", FindText(contents, "Forge Footer/Upgrade/Label"));
                Set(so, "getMoreButton", ButtonAt(contents, "Forge Footer/Get More"));
                Set(so, "summaryText", FindText(contents, "Forge Footer/Summary Text"));
                so.ApplyModifiedPropertiesWithoutUndo();

                PrefabUtility.SaveAsPrefabAsset(contents, prefabPath);
                AssetDatabase.SaveAssets();
                Debug.Log("[Forge] 已生成并绑定 " + prefabPath);
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
