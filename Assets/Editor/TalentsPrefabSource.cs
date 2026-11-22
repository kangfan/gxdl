using System.Collections.Generic;
using Gxdl.Core;
using Gxdl.Modules;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Gxdl.EditorTools
{
    /// <summary>
    /// 天赋树 prefab 生成器。
    /// 三系各八节点，位置照 GuxuTalentsUI.DrawTalentTree 的坐标还原；
    /// 等级、前置锁定、选中与消耗全部由 TalentsView 在运行时填入。
    /// 节点扁平号 = 系别 * 8 + 系内序号，与存档 talents[24] 对齐。
    /// </summary>
    [InitializeOnLoad]
    public static class TalentsPrefabSource
    {
        public const string PrefabPath = "Assets/Resources/" + UiScreens.Talents + ".prefab";
        const string ArtFolder = "Game/Guxu/Expansion/UI/";

        /// <summary>旧实现的八个节点中心坐标（页内左上为原点）。</summary>
        static readonly Vector2[] NodePoints =
        {
            new Vector2(150f, 62f), new Vector2(470f, 107f), new Vector2(452f, 241f), new Vector2(150f, 272f),
            new Vector2(150f, 400f), new Vector2(465f, 426f), new Vector2(468f, 558f), new Vector2(150f, 582f)
        };

        static readonly string[] Branches = { "攻伐", "御守", "灵息" };
        static readonly string[] BranchSymbols = { "attack", "shield", "regenerate" };

        static TalentsPrefabSource()
        {
            ScreenPrefabKit.Register("Talents", Build);
        }

        [MenuItem("Gxdl/UI/生成/天赋树", priority = 304)]
        public static void Generate()
        {
            var path = Build();
            if (path != null) EditorUtility.DisplayDialog("生成完成", "已生成\n" + path, "确定");
        }

        public static string Build()
        {
            using (var builder = new UiPrefabBuilder("TalentsView",
                new Vector2(UiRootLayout.DesignWidth, UiRootLayout.DesignHeight)))
            {
                var root = builder.Root;
                AnchorUtility.Apply(root, AnchorPreset.Stretch, Vector2.zero, Vector2.zero);
                ScreenPrefabKit.AddRootComponents(root);

                var backdrop = builder.ImageNode("", "Backdrop", 0f, 0f,
                    UiRootLayout.DesignWidth, UiRootLayout.DesignHeight, new Color(0.02f, 0.06f, 0.08f));
                backdrop.raycastTarget = false;
                var environment = Load("talent-environment");
                if (environment != null)
                {
                    backdrop.sprite = environment;
                    backdrop.color = Color.white;
                }

                BuildHeader(builder);
                BuildTabs(builder);
                BuildBranches(builder);
                BuildFooter(builder);

                var path = builder.Save(PrefabPath);
                AssetDatabase.SaveAssets();
                if (path != null) Bind(path);
                return path;
            }
        }

        static void BuildHeader(UiPrefabBuilder builder)
        {
            var back = SealButton(builder, "", "Talent Back", 16f, 24f, 174f, 52f, "‹ 返回修行", 24);
            back.name = "Talent Back";

            var titleBacking = builder.ImageNode("", "Talent Title Backing", 232f, 16f, 486f, 68f,
                new Color(0.15f, 0.38f, 0.35f, 0.94f));
            titleBacking.name = "Talent Title Backing";
            builder.TextNode("Talent Title Backing", "Title", 3f, 2f, 480f, 64f, "悟道天赋", 44,
                ScreenPrefabKit.Gold, TextAnchor.MiddleCenter);

            var insightIcon = builder.ImageNode("", "Insight Icon", 237f, 91f, 48f, 48f, Color.white);
            insightIcon.sprite = Load("resource-insight");
            builder.TextNode("", "Talent Balance", 297f, 90f, 260f, 48f, string.Empty, 30, ScreenPrefabKit.Cream);
        }

        static void BuildTabs(UiPrefabBuilder builder)
        {
            var tabs = builder.Node("", "Talent Branches", 27f, 160f, 750f, 52f);
            tabs.name = "Talent Branches";

            for (var i = 0; i < Branches.Length; i++)
            {
                var tab = SealButton(builder, "Talent Branches", "Branch " + i, i * 240f, 0f, 216f, 52f,
                    string.Empty, 28);
                tab.name = "Talent Branch " + i;

                var symbol = builder.ImageNode("Talent Branches/Branch " + i, "Symbol", 12f, -6f, 64f, 64f, Color.white);
                symbol.sprite = Load("effect-" + BranchSymbols[i]);
                builder.TextNode("Talent Branches/Branch " + i, "Label", 78f, 1f, 126f, 50f, Branches[i], 28,
                    ScreenPrefabKit.Cream, TextAnchor.MiddleCenter);
            }
        }

        static void BuildBranches(UiPrefabBuilder builder)
        {
            // 天赋星座区固定高度；三系页并排，切换系别只做整体平移。
            var map = builder.AnchoredNode("", "Talent Constellation", AnchorPreset.StretchBottom,
                new Vector2(750f, 669f), Vector2.zero);
            map.pivot = new Vector2(0.5f, 0f);
            var mask = map.gameObject.AddComponent<Image>();
            mask.color = Color.clear;
            mask.raycastTarget = false;
            map.gameObject.AddComponent<RectMask2D>();

            var content = builder.Node("Talent Constellation", "Map Content", 0f, 0f, 750f, 669f);

            for (var branch = 0; branch < Branches.Length; branch++)
            {
                var page = builder.Node("Talent Constellation/Map Content", "Branch " + branch,
                    branch * 750f, 0f, 750f, 669f);
                page.gameObject.SetActive(branch == 0);

                for (var index = 0; index < NodePoints.Length; index++)
                {
                    var node = branch * NodesPerBranch + index;
                    BuildNode(builder, page.gameObject.name, branch, index, node);
                }
            }
        }

        static int NodesPerBranch => TalentsView.NodesPerBranch;

        static void BuildNode(UiPrefabBuilder builder, string pageName, int branch, int index, int node)
        {
            var parent = "Talent Constellation/Map Content/" + pageName;
            var point = NodePoints[index];
            // 旧实现以中心定位且给出 270 x 126 的点击区；节点间的空白也让给可点区域。
            var path = parent + "/Node " + index;
            var button = builder.Node(parent, "Node " + index, point.x - 135f, point.y - 63f, 270f, 126f);
            button.gameObject.name = "Talent " + branch + " " + index;

            var ring = builder.ImageNode(path, "Selected Ring", 120f, 48f, 142f, 142f, Color.white);
            ring.sprite = Load("state-selected");
            ring.gameObject.SetActive(false);

            var baseArt = builder.ImageNode(path, "Base", 135f, 63f, 112f, 112f, Color.white);
            baseArt.sprite = Load("talent-node-base");

            var symbol = builder.ImageNode(path, "Symbol", 156f, 79f, 70f, 70f, Color.white);
            var own = Resources.Load<Texture2D>(ArtFolder + "talent-" + node.ToString("00"));
            symbol.sprite = own != null ? Load(ArtFolder + "talent-" + node.ToString("00")) : Load("effect-" + BranchSymbols[branch]);

            var locked = builder.ImageNode(path, "Lock", 208f, 127f, 28f, 35f, Color.white);
            locked.sprite = Load("state-lock");
            locked.gameObject.SetActive(false);

            builder.TextNode(path, "Name", 209f, 80f, 168f, 38f, string.Empty, 29, ScreenPrefabKit.Cream);
            builder.TextNode(path, "Rank", 209f, 117f, 170f, 31f, string.Empty, 23, ScreenPrefabKit.Cream);

            for (var pip = 0; pip < 3; pip++)
            {
                var pipRect = builder.Node(path, "Rank Pip " + pip, 165f + pip * 20f, 162f, 12f, 12f);
                pipRect.gameObject.AddComponent<Image>().color = new Color(0.24f, 0.37f, 0.42f, 1f);
            }

            var hit = button.gameObject.AddComponent<Image>();
            hit.color = new Color(0f, 0f, 0f, 0.001f);
            hit.raycastTarget = true;
            var selectable = button.gameObject.AddComponent<Button>();
            selectable.targetGraphic = hit;
            selectable.transition = Selectable.Transition.None;
        }

        static void BuildFooter(UiPrefabBuilder builder)
        {
            var footer = builder.AnchoredNode("", "Talent Selected Detail", AnchorPreset.StretchBottom,
                new Vector2(750f, 435f), Vector2.zero);
            footer.pivot = new Vector2(0.5f, 0f);
            var paper = footer.gameObject.AddComponent<Image>();
            paper.color = new Color(0.94f, 0.92f, 0.85f, 0.98f);
            paper.raycastTarget = true;

            var path = "Talent Selected Detail";
            var symbol = builder.ImageNode(path, "Symbol", 28f, 19f, 82f, 82f, Color.white);
            symbol.sprite = Load("effect-attack");

            builder.TextNode(path, "Name", 127f, 19f, 310f, 50f, string.Empty, 36, UiFactory.Ink);
            builder.TextNode(path, "Rank", 478f, 24f, 185f, 42f, string.Empty, 28, UiFactory.Ink, TextAnchor.MiddleRight);
            builder.TextNode(path, "Effect", 127f, 75f, 535f, 43f, string.Empty, 29, UiFactory.Ink);
            builder.TextNode(path, "Requirement", 32f, 131f, 635f, 40f, string.Empty, 25, UiFactory.Ink);
            builder.TextNode(path, "Cost", 35f, 185f, 360f, 39f, string.Empty, 27, UiFactory.Ink);
            builder.TextNode(path, "Balance", 396f, 185f, 267f, 39f, string.Empty, 26, UiFactory.Ink, TextAnchor.MiddleRight);

            var train = builder.Node(path, "Train", 65f, 240f, 570f, 80f);
            var trainImage = train.gameObject.AddComponent<Image>();
            trainImage.color = new Color(0.86f, 0.78f, 0.54f, 0.94f);
            trainImage.raycastTarget = true;
            train.gameObject.AddComponent<Button>().targetGraphic = trainImage;
            builder.TextNode(path + "/Train", "Train Label", 0f, 0f, 570f, 80f, string.Empty, 30, UiFactory.Ink, TextAnchor.MiddleCenter);

            SealButton(builder, path, "Reset", 72f, 354f, 270f, 52f, "免费重置", 24);
            SealButton(builder, path, "Get Insight", 408f, 354f, 270f, 52f, "获取悟道点", 24);
        }

        static RectTransform SealButton(UiPrefabBuilder builder, string parent, string name,
            float x, float y, float width, float height, string label, int size)
        {
            var rect = builder.Node(parent, name, x, y, width, height);
            var image = rect.gameObject.AddComponent<Image>();
            image.color = new Color(0.15f, 0.38f, 0.35f, 0.94f);
            image.raycastTarget = true;
            rect.gameObject.AddComponent<Button>().targetGraphic = image;
            if (!string.IsNullOrEmpty(label))
            {
                // 父路径为空时不能再拼前导斜杠，否则节点层级多出一层空节点。
                var labelPath = string.IsNullOrEmpty(parent) ? name : parent + "/" + name;
                builder.TextNode(labelPath, "Label", 0f, 0f, width, height, label, size,
                    ScreenPrefabKit.Cream, TextAnchor.MiddleCenter);
            }

            return rect;
        }

        static Sprite Load(string key)
        {
            var path = key.StartsWith("Game/") ? key : ArtFolder + key;
            var texture = Resources.Load<Texture2D>(path);
            if (texture == null) return null;
            return Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100f);
        }

        static void Bind(string prefabPath)
        {
            var contents = PrefabUtility.LoadPrefabContents(prefabPath);
            if (contents == null) return;
            try
            {
                var view = contents.GetComponent<TalentsView>();
                if (view == null) view = contents.AddComponent<TalentsView>();

                var so = new SerializedObject(view);
                Set(so, "backButton", Component<Button>(contents, "Talent Back"));
                Set(so, "insightText", FindText(contents, "Talent Balance"));
                Set(so, "branchRoot", Find(contents, "Talent Branches"));
                Set(so, "mapContent", Find(contents, "Talent Constellation/Map Content"));
                Set(so, "detailSymbol", Component<Image>(contents, "Talent Selected Detail/Symbol"));
                Set(so, "detailName", FindText(contents, "Talent Selected Detail/Name"));
                Set(so, "detailRank", FindText(contents, "Talent Selected Detail/Rank"));
                Set(so, "detailEffect", FindText(contents, "Talent Selected Detail/Effect"));
                Set(so, "detailRequirement", FindText(contents, "Talent Selected Detail/Requirement"));
                Set(so, "detailCost", FindText(contents, "Talent Selected Detail/Cost"));
                Set(so, "detailBalance", FindText(contents, "Talent Selected Detail/Balance"));
                Set(so, "trainButton", Component<Button>(contents, "Talent Selected Detail/Train"));
                Set(so, "trainLabel", FindText(contents, "Talent Selected Detail/Train/Train Label"));
                Set(so, "resetButton", Component<Button>(contents, "Talent Selected Detail/Reset"));
                Set(so, "insightButton", Component<Button>(contents, "Talent Selected Detail/Get Insight"));

                var pages = new List<GameObject>();
                for (var branch = 0; branch < Branches.Length; branch++)
                {
                    var page = Find(contents, "Talent Constellation/Map Content/Branch " + branch);
                    pages.Add(page != null ? page.gameObject : null);
                }

                SetArray(so, "branchPages", pages);

                var branchButtons = new List<Button>();
                var branchLabels = new List<Text>();
                for (var b = 0; b < Branches.Length; b++)
                {
                    branchButtons.Add(Component<Button>(contents, "Talent Branches/Branch " + b));
                    branchLabels.Add(FindText(contents, "Talent Branches/Branch " + b + "/Label"));
                }

                SetArray(so, "branchButtons", branchButtons);
                SetArray(so, "branchLabels", branchLabels);

                var nodeButtons = new List<Button>();
                var nodeRings = new List<Image>();
                var nodeBases = new List<Image>();
                var nodeSymbols = new List<Image>();
                var nodeLocks = new List<Image>();
                var nodeNames = new List<Text>();
                var nodeRanks = new List<Text>();
                var nodePips = new List<Image>();
                for (var branch = 0; branch < Branches.Length; branch++)
                {
                    for (var index = 0; index < NodesPerBranch; index++)
                    {
                        var path = "Talent Constellation/Map Content/Branch " + branch + "/Node " + index;
                        nodeButtons.Add(Component<Button>(contents, path));
                        nodeRings.Add(Component<Image>(contents, path + "/Selected Ring"));
                        nodeBases.Add(Component<Image>(contents, path + "/Base"));
                        nodeSymbols.Add(Component<Image>(contents, path + "/Symbol"));
                        nodeLocks.Add(Component<Image>(contents, path + "/Lock"));
                        nodeNames.Add(FindText(contents, path + "/Name"));
                        nodeRanks.Add(FindText(contents, path + "/Rank"));
                        for (var pip = 0; pip < 3; pip++)
                        {
                            nodePips.Add(Component<Image>(contents, path + "/Rank Pip " + pip));
                        }
                    }
                }

                SetArray(so, "nodeButtons", nodeButtons);
                SetArray(so, "nodeRings", nodeRings);
                SetArray(so, "nodeBases", nodeBases);
                SetArray(so, "nodeSymbols", nodeSymbols);
                SetArray(so, "nodeLocks", nodeLocks);
                SetArray(so, "nodeNames", nodeNames);
                SetArray(so, "nodeRanks", nodeRanks);
                SetArray(so, "nodePips", nodePips);
                so.ApplyModifiedPropertiesWithoutUndo();

                PrefabUtility.SaveAsPrefabAsset(contents, prefabPath);
                AssetDatabase.SaveAssets();
                Debug.Log("[Talents] 已生成并绑定 " + prefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contents);
            }
        }

        static Transform Find(GameObject root, string path) => root.transform.Find(path);

        static T Component<T>(GameObject root, string path) where T : Component
        {
            var node = Find(root, path);
            return node != null ? node.GetComponent<T>() : null;
        }

        static Text FindText(GameObject root, string path) => Component<Text>(root, path);

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
