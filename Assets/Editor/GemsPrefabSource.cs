using System.Collections.Generic;
using Gxdl.Modules;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Gxdl.EditorTools
{
    /// <summary>
    /// 灵玉工坊 prefab 生成器。
    /// 12 个装备槽、2 个镶孔、4 种灵玉、6 级库存全部静态建出，
    /// 选中、库存数量与按钮可用性由 GemsView.Apply 在运行时填入。
    /// </summary>
    [InitializeOnLoad]
    public static class GemsPrefabSource
    {
        public const string PrefabPath = "Assets/Resources/" + UiScreens.Gems + ".prefab";

        static GemsPrefabSource()
        {
            ScreenPrefabKit.Register("Gems", Build);
        }

        [MenuItem("Gxdl/UI/生成/灵玉工坊", priority = 310)]
        public static void Generate()
        {
            var path = Build();
            if (path != null) EditorUtility.DisplayDialog("生成完成", "已生成\n" + path, "确定");
        }

        public static string Build()
        {
            using (var builder = new UiPrefabBuilder("GemsView",
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
                BuildTypes(builder);
                BuildInventory(builder);
                BuildFooter(builder);

                var path = builder.Save(PrefabPath);
                AssetDatabase.SaveAssets();
                if (path != null) Bind(path);
                return path;
            }
        }

        static void BuildHeader(UiPrefabBuilder builder)
        {
            var backNode = builder.AnchoredNode("", "Header Back", AnchorPreset.TopLeft, new Vector2(173f, 52f), new Vector2(14f, 24f));
            var backImage = backNode.gameObject.AddComponent<Image>();
            backImage.color = new Color(0.15f, 0.38f, 0.35f, 0.94f);
            backImage.raycastTarget = true;
            backNode.gameObject.AddComponent<Button>().targetGraphic = backImage;
            builder.TextNode("Header Back", "Label", 0f, 0f, 173f, 52f, "‹ 返回修行", 24, ScreenPrefabKit.Cream, TextAnchor.MiddleCenter);

            builder.TextNode("", "Title", 203f, 17f, 340f, 63f, "灵玉镶嵌", 42, ScreenPrefabKit.Gold);
            builder.TextNode("", "Gem Total Inventory", 540f, 29f, 196f, 44f, string.Empty, 24, ScreenPrefabKit.Cream);
            builder.TextNode("", "Gem Hint", 140f, 84f, 490f, 32f, "20 / 40关开启镶孔 · 免费卸下", 23, ScreenPrefabKit.Cream);
        }

        static void BuildSlots(UiPrefabBuilder builder)
        {
            var slotRoot = builder.Node("", "Gem Slot Root", 0f, 124f, UiRootLayout.DesignWidth, 160f);

            for (var i = 0; i < 12; i++)
            {
                var x = 18f + i % 6 * 122f;
                var y = i / 6 * 81f;
                var cell = builder.Node("Gem Slot Root", "Slot " + i, x, y, 108f, 79f);
                cell.gameObject.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.001f);

                var highlight = builder.ImageNode("Gem Slot Root/Slot " + i, "Highlight", 0f, 0f, 108f, 79f,
                    new Color(0.86f, 0.78f, 0.54f, 0.22f));
                highlight.gameObject.SetActive(false);
                builder.ImageNode("Gem Slot Root/Slot " + i, "Icon", 25f, 0f, 58f, 56f, Color.white);
                builder.ImageNode("Gem Slot Root/Slot " + i, "Plate", 0f, 53f, 108f, 27f, new Color(0f, 0f, 0f, 0.35f));
                builder.TextNode("Gem Slot Root/Slot " + i, "Name", 0f, 52f, 108f, 28f, string.Empty, 21,
                    ScreenPrefabKit.Cream, TextAnchor.MiddleCenter);

                var button = cell.gameObject.AddComponent<Button>();
                button.targetGraphic = cell.gameObject.GetComponent<Image>();
                button.transition = Selectable.Transition.None;
            }
        }

        static void BuildWorkbench(UiPrefabBuilder builder)
        {
            var stage = builder.Node("", "Gem Workbench", 0f, 290f, UiRootLayout.DesignWidth, 340f);

            builder.ImageNode("Gem Workbench", "Selected Equipment", 285f, 75f, 180f, 180f, Color.white);
            builder.ImageNode("Gem Workbench", "Selection Plate", 275f, 270f, 200f, 40f, new Color(0f, 0f, 0f, 0.35f));
            builder.TextNode("Gem Workbench", "Selected Label", 275f, 270f, 200f, 40f, string.Empty, 25,
                ScreenPrefabKit.Cream, TextAnchor.MiddleCenter);

            for (var hole = 0; hole < 2; hole++)
            {
                var path = "Gem Workbench/Socket " + hole;
                var x = hole == 0 ? 105f : 475f;
                var cell = builder.Node("Gem Workbench", "Socket " + hole, x, 90f, 170f, 170f);
                cell.gameObject.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.001f);

                var highlight = builder.ImageNode(path, "Highlight", 0f, 0f, 170f, 170f,
                    new Color(0.86f, 0.78f, 0.54f, 0.3f));
                highlight.gameObject.SetActive(false);
                builder.ImageNode(path, "Icon", 41f, 41f, 88f, 88f, Color.white);
                builder.TextNode(path, "Label", -19f, 100f, 208f, 32f, string.Empty, 23,
                    ScreenPrefabKit.Cream, TextAnchor.MiddleCenter);
                builder.TextNode(path, "Hint", -5f, 146f, 180f, 24f, string.Empty, 18,
                    ScreenPrefabKit.Cream, TextAnchor.MiddleCenter);
                builder.TextNode(path, "Caption", 11f, 58f, 148f, 33f, "镶孔 " + (hole + 1), 24,
                    ScreenPrefabKit.Cream, TextAnchor.MiddleCenter);

                var button = cell.gameObject.AddComponent<Button>();
                button.targetGraphic = cell.gameObject.GetComponent<Image>();
                button.transition = Selectable.Transition.None;
            }

            builder.ImageNode("Gem Workbench", "Socket Effect Backing", 199f, 302f, 362f, 51f,
                new Color(0.015f, 0.08f, 0.10f, 0.96f));
            builder.TextNode("Gem Workbench", "Socket Effect", 215f, 312f, 330f, 30f, string.Empty, 23,
                ScreenPrefabKit.Cream, TextAnchor.MiddleCenter);

            var remove = builder.AnchoredNode("Gem Workbench", "Remove", AnchorPreset.TopLeft, new Vector2(121f, 42f), new Vector2(585f, 300f));
            var removeImage = remove.gameObject.AddComponent<Image>();
            removeImage.color = new Color(0.15f, 0.38f, 0.35f, 0.94f);
            removeImage.raycastTarget = true;
            remove.gameObject.AddComponent<Button>().targetGraphic = removeImage;
            builder.TextNode("Gem Workbench/Remove", "Label", 0f, 0f, 121f, 42f, "卸下", 24,
                ScreenPrefabKit.Cream, TextAnchor.MiddleCenter);
        }

        static void BuildTypes(UiPrefabBuilder builder)
        {
            var types = builder.Node("", "Gem Types", 0f, 640f, UiRootLayout.DesignWidth, 65f);

            for (var type = 0; type < 4; type++)
            {
                var path = "Gem Types/Type " + type;
                var cell = builder.Node("Gem Types", "Type " + type, 15f + type * 184f, 0f, 174f, 59f);
                cell.gameObject.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.001f);

                builder.ImageNode(path, "Frame", 0f, 0f, 174f, 59f, new Color(0.15f, 0.38f, 0.35f, 0.6f));
                builder.ImageNode(path, "Icon", 14f, 13f, 32f, 32f, Color.white);
                builder.TextNode(path, "Name", 47f, 3f, 109f, 53f, string.Empty, 19, ScreenPrefabKit.Cream);

                var button = cell.gameObject.AddComponent<Button>();
                button.targetGraphic = cell.gameObject.GetComponent<Image>();
                button.transition = Selectable.Transition.None;
            }
        }

        static void BuildInventory(UiPrefabBuilder builder)
        {
            var inventory = builder.Node("", "Gem Inventory", 25f, 720f, 700f, 200f);
            builder.ImageNode("Gem Inventory", "Panel", 0f, 0f, 700f, 200f, new Color(0.055f, 0.12f, 0.16f, 0.96f));

            for (var level = 0; level < 6; level++)
            {
                var path = "Gem Inventory/Stock " + level;
                var cell = builder.Node("Gem Inventory", "Stock " + level, 16f + level * 112f, 18f, 108f, 164f);
                cell.gameObject.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.001f);

                var highlight = builder.ImageNode(path, "Highlight", 0f, 0f, 108f, 164f,
                    new Color(0.86f, 0.78f, 0.54f, 0.18f));
                highlight.gameObject.SetActive(false);
                builder.ImageNode(path, "Icon", 3f, 0f, 102f, 102f, Color.white);
                builder.ImageNode(path, "Stock Backing", 34f, 4f, 66f, 27f, new Color(0.02f, 0.04f, 0.05f, 0.82f));
                builder.TextNode(path, "Count", 35f, 3f, 64f, 29f, string.Empty, 21,
                    ScreenPrefabKit.Cream, TextAnchor.MiddleRight);
                builder.TextNode(path, "Level", 0f, 105f, 108f, 29f, string.Empty, 23,
                    ScreenPrefabKit.Cream, TextAnchor.MiddleCenter);
                builder.TextNode(path, "Cap", 0f, 134f, 108f, 24f, string.Empty, 18,
                    ScreenPrefabKit.Cream, TextAnchor.MiddleCenter);

                var button = cell.gameObject.AddComponent<Button>();
                button.targetGraphic = cell.gameObject.GetComponent<Image>();
                button.transition = Selectable.Transition.None;
            }
        }

        static void BuildFooter(UiPrefabBuilder builder)
        {
            var footer = builder.Node("", "Gem Detail", 0f, 935f, UiRootLayout.DesignWidth, 399f);

            var tray = builder.Node("Gem Detail", "Paper", 25f, 0f, 700f, 310f);
            tray.gameObject.AddComponent<Image>().color = ScreenPrefabKit.Panel;

            builder.ImageNode("Gem Detail/Paper", "Icon", 30f, 22f, 95f, 95f, Color.white);
            builder.TextNode("Gem Detail/Paper", "Name", 143f, 18f, 330f, 40f, string.Empty, 30, ScreenPrefabKit.Cream);
            builder.TextNode("Gem Detail/Paper", "Effect", 143f, 62f, 330f, 37f, string.Empty, 28, ScreenPrefabKit.Cream);
            builder.TextNode("Gem Detail/Paper", "Owned", 143f, 102f, 330f, 31f, string.Empty, 24, ScreenPrefabKit.Cream);
            builder.TextNode("Gem Detail/Paper", "Target", 472f, 25f, 203f, 86f, string.Empty, 26, ScreenPrefabKit.Cream);

            var equip = builder.Node("Gem Detail/Paper", "Equip", 105f, 143f, 490f, 80f);
            var equipImage = equip.gameObject.AddComponent<Image>();
            equipImage.color = new Color(0.15f, 0.38f, 0.35f, 0.94f);
            equipImage.raycastTarget = true;
            equip.gameObject.AddComponent<Button>().targetGraphic = equipImage;
            builder.TextNode("Gem Detail/Paper/Equip", "Label", 0f, 0f, 490f, 80f, string.Empty, 28,
                ScreenPrefabKit.Cream, TextAnchor.MiddleCenter);

            var combine = builder.Node("Gem Detail/Paper", "Combine", 40f, 242f, 170f, 49f);
            var combineImage = combine.gameObject.AddComponent<Image>();
            combineImage.color = new Color(0.15f, 0.38f, 0.35f, 0.94f);
            combineImage.raycastTarget = true;
            combine.gameObject.AddComponent<Button>().targetGraphic = combineImage;
            builder.TextNode("Gem Detail/Paper/Combine", "Label", 0f, 0f, 170f, 49f, string.Empty, 24,
                ScreenPrefabKit.Cream, TextAnchor.MiddleCenter);

            builder.TextNode("Gem Detail", "Note", 245f, 246f, 400f, 40f, string.Empty, 25, ScreenPrefabKit.Cream);
            builder.TextNode("Gem Detail", "Get More", 75f, 334f, 270f, 52f, "获取灵玉", 24,
                ScreenPrefabKit.Cream, TextAnchor.MiddleCenter);
            builder.TextNode("Gem Detail", "Return", 405f, 334f, 270f, 52f, "返回修行", 24,
                ScreenPrefabKit.Cream, TextAnchor.MiddleCenter);
        }

        static void Bind(string prefabPath)
        {
            var contents = PrefabUtility.LoadPrefabContents(prefabPath);
            if (contents == null) return;
            try
            {
                var view = contents.GetComponent<GemsView>();
                if (view == null) view = contents.AddComponent<GemsView>();

                var so = new SerializedObject(view);
                Set(so, "backButton", ButtonAt(contents, "Header Back"));
                Set(so, "totalText", FindText(contents, "Gem Total Inventory"));
                Set(so, "hintText", FindText(contents, "Gem Hint"));

                var icons = new List<Image>();
                var frames = new List<Image>();
                var names = new List<Text>();
                var highlights = new List<Image>();
                var buttons = new List<Button>();
                for (var i = 0; i < 12; i++)
                {
                    var path = "Gem Slot Root/Slot " + i;
                    icons.Add(FindImage(contents, path + "/Icon"));
                    frames.Add(FindImage(contents, path + "/Plate"));
                    names.Add(FindText(contents, path + "/Name"));
                    highlights.Add(FindImage(contents, path + "/Highlight"));
                    buttons.Add(ButtonAt(contents, path));
                }

                SetArray(so, "slotIcons", icons);
                SetArray(so, "slotFrames", frames);
                SetArray(so, "slotNames", names);
                SetArray(so, "slotHighlights", highlights);
                SetArray(so, "slotButtons", buttons);
                Set(so, "slotRoot", Find(contents, "Gem Slot Root"));

                Set(so, "selectedIcon", FindImage(contents, "Gem Workbench/Selected Equipment"));
                Set(so, "selectedLabel", FindText(contents, "Gem Workbench/Selected Label"));
                Set(so, "socketSummary", FindText(contents, "Gem Workbench/Socket Effect"));
                Set(so, "removeButton", ButtonAt(contents, "Gem Workbench/Remove"));

                var socketIcons = new List<Image>();
                var socketLabels = new List<Text>();
                var socketHints = new List<Text>();
                var socketHighlights = new List<Image>();
                var socketButtons = new List<Button>();
                for (var hole = 0; hole < 2; hole++)
                {
                    var path = "Gem Workbench/Socket " + hole;
                    socketIcons.Add(FindImage(contents, path + "/Icon"));
                    socketLabels.Add(FindText(contents, path + "/Label"));
                    socketHints.Add(FindText(contents, path + "/Hint"));
                    socketHighlights.Add(FindImage(contents, path + "/Highlight"));
                    socketButtons.Add(ButtonAt(contents, path));
                }

                SetArray(so, "socketIcons", socketIcons);
                SetArray(so, "socketLabels", socketLabels);
                SetArray(so, "socketHints", socketHints);
                SetArray(so, "socketHighlights", socketHighlights);
                SetArray(so, "socketButtons", socketButtons);

                var typeIcons = new List<Image>();
                var typeLabels = new List<Text>();
                var typeFrames = new List<Image>();
                var typeButtons = new List<Button>();
                for (var type = 0; type < 4; type++)
                {
                    var path = "Gem Types/Type " + type;
                    typeIcons.Add(FindImage(contents, path + "/Icon"));
                    typeLabels.Add(FindText(contents, path + "/Name"));
                    typeFrames.Add(FindImage(contents, path + "/Frame"));
                    typeButtons.Add(ButtonAt(contents, path));
                }

                SetArray(so, "typeIcons", typeIcons);
                SetArray(so, "typeLabels", typeLabels);
                SetArray(so, "typeFrames", typeFrames);
                SetArray(so, "typeButtons", typeButtons);

                var stockIcons = new List<Image>();
                var stockFrames = new List<Image>();
                var stockCounts = new List<Text>();
                var stockLevels = new List<Text>();
                var stockHighlights = new List<Image>();
                var stockButtons = new List<Button>();
                for (var level = 0; level < 6; level++)
                {
                    var path = "Gem Inventory/Stock " + level;
                    stockIcons.Add(FindImage(contents, path + "/Icon"));
                    stockFrames.Add(FindImage(contents, path + "/Stock Backing"));
                    stockCounts.Add(FindText(contents, path + "/Count"));
                    stockLevels.Add(FindText(contents, path + "/Level"));
                    stockHighlights.Add(FindImage(contents, path + "/Highlight"));
                    stockButtons.Add(ButtonAt(contents, path));
                }

                SetArray(so, "stockIcons", stockIcons);
                SetArray(so, "stockFrames", stockFrames);
                SetArray(so, "stockCounts", stockCounts);
                SetArray(so, "stockLevels", stockLevels);
                SetArray(so, "stockHighlights", stockHighlights);
                SetArray(so, "stockButtons", stockButtons);

                Set(so, "detailIcon", FindImage(contents, "Gem Detail/Paper/Icon"));
                Set(so, "detailName", FindText(contents, "Gem Detail/Paper/Name"));
                Set(so, "detailEffect", FindText(contents, "Gem Detail/Paper/Effect"));
                Set(so, "detailOwned", FindText(contents, "Gem Detail/Paper/Owned"));
                Set(so, "detailTarget", FindText(contents, "Gem Detail/Paper/Target"));
                Set(so, "equipButton", ButtonAt(contents, "Gem Detail/Paper/Equip"));
                Set(so, "equipLabel", FindText(contents, "Gem Detail/Paper/Equip/Label"));
                Set(so, "combineButton", ButtonAt(contents, "Gem Detail/Paper/Combine"));
                Set(so, "combineLabel", FindText(contents, "Gem Detail/Paper/Combine/Label"));
                Set(so, "getMoreButton", ButtonAt(contents, "Gem Detail/Get More"));
                Set(so, "returnButton", ButtonAt(contents, "Gem Detail/Return"));
                so.ApplyModifiedPropertiesWithoutUndo();

                PrefabUtility.SaveAsPrefabAsset(contents, prefabPath);
                AssetDatabase.SaveAssets();
                Debug.Log("[Gems] 已生成并绑定 " + prefabPath);
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
