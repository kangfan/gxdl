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
                background.color = new Color(0.015f, 0.045f, 0.065f);
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
            var backNode = builder.AnchoredNode("", "Header Back", AnchorPreset.TopLeft, new Vector2(174f, 52f), new Vector2(16f, 24f));
            var backImage = backNode.gameObject.AddComponent<Image>();
            backImage.color = new Color(0.15f, 0.38f, 0.35f, 0.94f);
            backImage.raycastTarget = true;
            backNode.gameObject.AddComponent<Button>().targetGraphic = backImage;
            builder.TextNode("Header Back", "Label", 0f, 0f, 174f, 52f, "‹ 返回修行", 24, ScreenPrefabKit.Cream, TextAnchor.MiddleCenter);

            builder.ImageNode("", "Title Backing", 195f, 7f, 410f, 76f, new Color(0.015f, 0.06f, 0.08f, 0.8f));
            builder.TextNode("", "Title", 210f, 15f, 365f, 65f, "战前布置", 43, ScreenPrefabKit.Gold);
            builder.TextNode("", "Subtitle", 100f, 104f, 550f, 39f, "选择一个主动技能", 27, ScreenPrefabKit.Cream);
        }

        static void BuildSkills(UiPrefabBuilder builder)
        {
            builder.Node("", "Skill Root", 0f, 190f, UiRootLayout.DesignWidth, 194f);

            for (var i = 0; i < 4; i++)
            {
                var path = "Skill Root/Skill " + i;
                var cell = builder.Node("Skill Root", "Skill " + i, 15f + i * 184f, 0f, 168f, 194f);
                cell.gameObject.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.001f);

                builder.ImageNode(path, "Icon", 39f, 18f, 90f, 130f, Color.white);
                var ring = builder.ImageNode(path, "Ring", 8f, 6f, 152f, 140f, new Color(0.86f, 0.78f, 0.54f, 0.3f));
                ring.gameObject.SetActive(false);
                builder.ImageNode(path, "Name Plate", 0f, 147f, 168f, 47f, new Color(0f, 0f, 0f, 0.35f));
                builder.TextNode(path, "Name", 13f, 150f, 142f, 40f, string.Empty, 27,
                    ScreenPrefabKit.Cream, TextAnchor.MiddleCenter);

                // 已装备角标初始隐藏，由存档决定显示。
                var flag = builder.ImageNode(path, "Equipped Flag", 20f, 0f, 128f, 39f,
                    new Color(0.86f, 0.78f, 0.54f, 0.92f));
                flag.gameObject.SetActive(false);
                builder.TextNode(path + "/Equipped Flag", "Label", 0f, 0f, 128f, 39f, "已装备", 22,
                    new Color(0.06f, 0.1f, 0.12f), TextAnchor.MiddleCenter);

                var button = cell.gameObject.AddComponent<Button>();
                button.targetGraphic = cell.gameObject.GetComponent<Image>();
                button.transition = Selectable.Transition.None;
            }
        }

        static void BuildDetail(UiPrefabBuilder builder)
        {
            var body = builder.Node("", "Loadout Detail", 25f, 400f, 700f, 420f);
            body.gameObject.AddComponent<Image>().color = new Color(0.055f, 0.12f, 0.16f, 0.96f);

            builder.ImageNode("Loadout Detail", "Altar Disc", 250f, 15f, 200f, 200f, new Color(0.06f, 0.20f, 0.21f));
            builder.ImageNode("Loadout Detail", "Selected Icon", 285f, 55f, 130f, 130f, Color.white);
            builder.ImageNode("Loadout Detail", "Name Plate", 125f, 200f, 450f, 53f, new Color(0f, 0f, 0f, 0.35f));
            builder.TextNode("Loadout Detail", "Selected Name", 145f, 201f, 410f, 51f, string.Empty, 36,
                ScreenPrefabKit.Cream, TextAnchor.MiddleCenter);
            builder.TextNode("Loadout Detail", "Selected Detail", 50f, 258f, 600f, 95f, string.Empty, 27,
                ScreenPrefabKit.Cream);

            var equip = builder.Node("Loadout Detail", "Equip", 170f, 350f, 360f, 60f);
            var equipImage = equip.gameObject.AddComponent<Image>();
            equipImage.color = new Color(0.15f, 0.38f, 0.35f, 0.94f);
            equipImage.raycastTarget = true;
            equip.gameObject.AddComponent<Button>().targetGraphic = equipImage;
            builder.TextNode("Loadout Detail/Equip", "Label", 0f, 0f, 360f, 60f, string.Empty, 28,
                ScreenPrefabKit.Cream, TextAnchor.MiddleCenter);

            var auto = builder.Node("Loadout Detail", "Auto", 37f, 362f, 278f, 50f);
            var autoImage = auto.gameObject.AddComponent<Image>();
            autoImage.color = new Color(0.15f, 0.38f, 0.35f, 0.94f);
            autoImage.raycastTarget = true;
            auto.gameObject.AddComponent<Button>().targetGraphic = autoImage;
            builder.TextNode("Loadout Detail/Auto", "Label", 0f, 0f, 278f, 50f, string.Empty, 24,
                ScreenPrefabKit.Cream, TextAnchor.MiddleCenter);
            builder.TextNode("Loadout Detail", "Auto Detail", 337f, 356f, 320f, 62f, string.Empty, 22,
                ScreenPrefabKit.Cream);
            builder.TextNode("Loadout Detail", "Passive Note", 45f, 385f, 610f, 32f, "被动效果来自装备、天赋与灵玉。", 24,
                ScreenPrefabKit.Cream);
        }

        static void BuildActions(UiPrefabBuilder builder)
        {
            var actions = builder.AnchoredNode("", "Loadout Actions", AnchorPreset.BottomLeft,
                new Vector2(700f, 78f), new Vector2(25f, 22f));

            var back = builder.AnchoredNode("Loadout Actions", "Return", AnchorPreset.TopLeft,
                new Vector2(240f, 78f), new Vector2(35f, 0f));
            var backImage = back.gameObject.AddComponent<Image>();
            backImage.color = new Color(0.15f, 0.38f, 0.35f, 0.94f);
            backImage.raycastTarget = true;
            back.gameObject.AddComponent<Button>().targetGraphic = backImage;
            builder.TextNode("Loadout Actions/Return", "Label", 0f, 0f, 240f, 78f, "返回修行", 28,
                ScreenPrefabKit.Cream, TextAnchor.MiddleCenter);

            var challenge = builder.AnchoredNode("Loadout Actions", "Challenge", AnchorPreset.TopLeft,
                new Vector2(365f, 78f), new Vector2(301f, 0f));
            var challengeImage = challenge.gameObject.AddComponent<Image>();
            challengeImage.color = new Color(0.86f, 0.78f, 0.54f, 0.92f);
            challengeImage.raycastTarget = true;
            challenge.gameObject.AddComponent<Button>().targetGraphic = challengeImage;
            builder.TextNode("Loadout Actions/Challenge", "Label", 0f, 0f, 365f, 78f, "前往挑战", 28,
                new Color(0.06f, 0.1f, 0.12f), TextAnchor.MiddleCenter);
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
