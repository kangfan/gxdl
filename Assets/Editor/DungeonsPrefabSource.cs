using System.Collections.Generic;
using Gxdl.Modules;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Gxdl.EditorTools
{
    /// <summary>
    /// 资源秘境总览 prefab 生成器。2x2 宫格四个入口与底部两个链接在此搭骨架，
    /// 名称（取自 GuxuStageRules.DungeonNames）、解锁、层数、剩余次数由 DungeonsView.Apply 填入。
    /// </summary>
    [InitializeOnLoad]
    public static class DungeonsPrefabSource
    {
        public const string PrefabPath = "Assets/Resources/" + UiScreens.Dungeons + ".prefab";

        const float LinkHeight = 244f;

        static readonly Color PanelDark = new Color(0.055f, 0.12f, 0.16f, 0.86f);

        static DungeonsPrefabSource()
        {
            ScreenPrefabKit.Register("Dungeons", Build);
        }

        [MenuItem("Gxdl/UI/生成/资源秘境", priority = 303)]
        public static void Generate()
        {
            var path = Build();
            if (path != null) EditorUtility.DisplayDialog("生成完成", "已生成\n" + path, "确定");
        }

        public static string Build()
        {
            using (var builder = new UiPrefabBuilder("DungeonsView",
                new Vector2(UiRootLayout.DesignWidth, UiRootLayout.DesignHeight)))
            {
                var root = builder.Root;
                AnchorUtility.Apply(root, AnchorPreset.Stretch, Vector2.zero, Vector2.zero);
                ScreenPrefabKit.AddRootComponents(root);

                var background = builder.AnchoredNode("", "Background", AnchorPreset.Stretch, Vector2.zero)
                    .gameObject.AddComponent<Image>();
                background.color = new Color(0.015f, 0.045f, 0.065f);
                background.raycastTarget = false;

                BuildHeading(builder);
                BuildGates(builder);
                BuildLinks(builder);

                var path = builder.Save(PrefabPath);
                AssetDatabase.SaveAssets();
                if (path != null) Bind(path);
                return path;
            }
        }

        static void BuildHeading(UiPrefabBuilder builder)
        {
            var backing = builder.Node("", "Heading", 100f, 0f, 550f, 185f);
            backing.gameObject.AddComponent<Image>().color = new Color(0.01f, 0.05f, 0.07f, 0.96f);

            var back = builder.Node("", "Back", 18f, 25f, 124f, 52f);
            var backImage = back.gameObject.AddComponent<Image>();
            backImage.color = new Color(0.15f, 0.38f, 0.35f, 0.94f);
            backImage.raycastTarget = true;
            back.gameObject.AddComponent<Button>().targetGraphic = backImage;
            builder.TextNode("Back", "Label", 0f, 0f, 124f, 52f, "‹ 古墟", 24, ScreenPrefabKit.Cream, TextAnchor.MiddleCenter);

            builder.TextNode("", "Title", 180f, 16f, 390f, 65f, "资源秘境", 44, ScreenPrefabKit.Gold);
            builder.TextNode("", "Subtitle", 80f, 88f, 590f, 37f, "每类每日3次收益 · 首通后可速通", 26, ScreenPrefabKit.Cream);
            builder.TextNode("", "Refresh", 200f, 132f, 350f, 30f, "北京时间零点刷新", 21, ScreenPrefabKit.Cream, TextAnchor.MiddleCenter);
        }

        static void BuildGates(UiPrefabBuilder builder)
        {
            for (var i = 0; i < 4; i++)
            {
                var name = "Gate " + i;
                // 2x2 宫格：列距 370，行距 420，与旧版一致。
                var gate = builder.Node("", name, 25f + i % 2 * 370f, 180f + i / 2 * 420f, 330f, 410f);
                gate.gameObject.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.001f);

                builder.ImageNode(name, "Portal", 0f, 0f, 330f, 330f, new Color(0.10f, 0.22f, 0.28f, 0.95f));
                builder.ImageNode(name, "Name Plate", -4f, 18f, 338f, 51f, PanelDark);
                builder.TextNode(name, "Name", 10f, 23f, 310f, 43f, GuxuStageRules.DungeonNames[i], 30, ScreenPrefabKit.Gold, TextAnchor.MiddleCenter);

                builder.ImageNode(name, "Reward Plate", 38f, 78f, 254f, 39f, new Color(0.04f, 0.10f, 0.13f, 0.7f));
                if (i == 3)
                {
                    // 聚灵洞天同时产出修为与突破石，并排两个图标。
                    builder.ImageNode(name, "Reward Icon A", 47f, 80f, 33f, 33f, new Color(0.42f, 0.68f, 0.46f));
                    builder.ImageNode(name, "Reward Icon B", 78f, 80f, 33f, 33f, new Color(0.62f, 0.58f, 0.72f));
                    builder.TextNode(name, "Reward A", 111f, 79f, 170f, 36f, "修为 / 突破石", 22, ScreenPrefabKit.Cream);
                }
                else
                {
                    builder.ImageNode(name, "Reward Icon A", 58f, 78f, 39f, 39f, new Color(0.55f, 0.6f, 0.62f));
                    var label = i == 0 ? "玄铁" : i == 1 ? "灵玉" : "悟道点";
                    builder.TextNode(name, "Reward A", 104f, 79f, 174f, 36f, label, 25, ScreenPrefabKit.Cream);
                }

                builder.TextNode(name, "Lock", 142f, 166f, 48f, 60f, "锁", 34, new Color(0.93f, 0.87f, 0.69f), TextAnchor.MiddleCenter);
                builder.ImageNode(name, "State Panel", 32f, 266f, 266f, 80f, PanelDark);
                builder.TextNode(name, "Progress", 41f, 271f, 248f, 33f, string.Empty, 23, ScreenPrefabKit.Cream);
                builder.TextNode(name, "Attempts", 41f, 307f, 248f, 32f, string.Empty, 23, ScreenPrefabKit.Cream);

                var enter = builder.Node(name, "Enter", 60f, 359f, 210f, 56f);
                var enterImage = enter.gameObject.AddComponent<Image>();
                enterImage.color = new Color(0.15f, 0.38f, 0.35f, 0.94f);
                enterImage.raycastTarget = true;
                var enterButton = enter.gameObject.AddComponent<Button>();
                enterButton.targetGraphic = enterImage;
                enter.gameObject.SetActive(true);
                builder.TextNode(name + "/Enter", "Label", 0f, 0f, 210f, 56f, "进入 ›", 26, ScreenPrefabKit.Cream, TextAnchor.MiddleCenter);

                // 整张卡片也是点击区，与底部按钮共用回调语义。
                gate.gameObject.AddComponent<Button>().targetGraphic = gate.gameObject.GetComponent<Image>();
            }
        }

        static void BuildLinks(UiPrefabBuilder builder)
        {
            var links = builder.AnchoredNode("", "Links", AnchorPreset.StretchBottom, new Vector2(0f, LinkHeight), new Vector2(0f, 0f));

            var tower = builder.Node("Links", "Tower", 65f, 0f, 620f, 76f);
            var towerImage = tower.gameObject.AddComponent<Image>();
            towerImage.color = new Color(0.15f, 0.38f, 0.35f, 0.94f);
            towerImage.raycastTarget = true;
            tower.gameObject.AddComponent<Button>().targetGraphic = towerImage;
            builder.TextNode("Links/Tower", "Label", 0f, 0f, 620f, 76f, "通天古塔 · 百层试炼", 28, ScreenPrefabKit.Cream, TextAnchor.MiddleCenter);

            var growth = builder.Node("Links", "Growth", 220f, 119f, 310f, 58f);
            var growthImage = growth.gameObject.AddComponent<Image>();
            growthImage.color = new Color(0.10f, 0.26f, 0.28f, 0.94f);
            growthImage.raycastTarget = true;
            growth.gameObject.AddComponent<Button>().targetGraphic = growthImage;
            builder.TextNode("Links/Growth", "Label", 0f, 0f, 310f, 58f, "修行与养成", 26, ScreenPrefabKit.Cream, TextAnchor.MiddleCenter);
        }

        static void Bind(string prefabPath)
        {
            var contents = PrefabUtility.LoadPrefabContents(prefabPath);
            if (contents == null) return;
            try
            {
                var view = contents.GetComponent<DungeonsView>();
                if (view == null) view = contents.AddComponent<DungeonsView>();

                var so = new SerializedObject(view);
                Set(so, "backButton", Find(contents, "Back")?.GetComponent<Button>());
                Set(so, "towerButton", Find(contents, "Links/Tower")?.GetComponent<Button>());
                Set(so, "growthButton", Find(contents, "Links/Growth")?.GetComponent<Button>());

                var buttons = new List<Button>();
                var portals = new List<Image>();
                var names = new List<Text>();
                var locks = new List<GameObject>();
                var progress = new List<Text>();
                var attempts = new List<Text>();
                for (var i = 0; i < 4; i++)
                {
                    var path = "Gate " + i;
                    buttons.Add(Find(contents, path)?.GetComponent<Button>());
                    portals.Add(Find(contents, path + "/Portal")?.GetComponent<Image>());
                    names.Add(Find(contents, path + "/Name")?.GetComponent<Text>());
                    locks.Add(Find(contents, path + "/Lock")?.gameObject);
                    progress.Add(Find(contents, path + "/Progress")?.GetComponent<Text>());
                    attempts.Add(Find(contents, path + "/Attempts")?.GetComponent<Text>());
                }

                SetArray(so, "gateButtons", buttons);
                SetArray(so, "gatePortals", portals);
                SetArray(so, "gateNames", names);
                SetArray(so, "gateLocks", locks);
                SetArray(so, "gateProgress", progress);
                SetArray(so, "gateAttempts", attempts);
                so.ApplyModifiedPropertiesWithoutUndo();

                PrefabUtility.SaveAsPrefabAsset(contents, prefabPath);
                AssetDatabase.SaveAssets();
                Debug.Log("[Dungeons] 已生成并绑定 " + prefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contents);
            }
        }

        static Transform Find(GameObject root, string path)
        {
            return root.transform.Find(path);
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
