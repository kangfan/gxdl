using System.Collections.Generic;
using Gxdl.Modules;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Gxdl.EditorTools
{
    /// <summary>
    /// 遗宝图鉴 prefab 生成器。
    /// 12 个部位格与 5 品质 × 3 阶级的数值表全部静态建出，
    /// 表内数值由 CodexView.ApplyTable 在运行时按当前遗域计算后填入。
    /// </summary>
    [InitializeOnLoad]
    public static class CodexPrefabSource
    {
        public const string PrefabPath = "Assets/Resources/" + UiScreens.Codex + ".prefab";
        const string SkinPath = "Assets/Resources/Game/Guxu/Expansion/UI/";

        // 15 行 = 5 品质 × 3 阶级，每 3 行留一道分组间隔。
        const int RowCount = 15;
        const float RowStart = 164f;
        const float RowHeight = 34f;

        // 效果图采样
        static readonly Color TitleInk = FromHex(0x7A3F02);
        static readonly Color HintInk = FromHex(0x453822);
        static readonly Color CellNameInk = FromHex(0x3B1903);

        static Color FromHex(long value) => new Color(
            ((value >> 16) & 0xFF) / 255f, ((value >> 8) & 0xFF) / 255f, (value & 0xFF) / 255f);

        static readonly Color CloseInk = FromHex(0x6E4208);

        static Sprite Skin(string key) => AssetDatabase.LoadAssetAtPath<Sprite>(SkinPath + key + ".png");

        static Sprite BgSprite() =>
            AssetDatabase.LoadAssetAtPath<Sprite>(SkinPath + "ui-bg-fullscreen.png");

        static Image SkinImage(UiPrefabBuilder builder, string parent, string name,
            float x, float y, float width, float height, string key, bool raycast = false)
        {
            var image = builder.Node(parent, name, x, y, width, height).gameObject.AddComponent<Image>();
            image.sprite = Skin(key);
            image.enabled = image.sprite != null;
            image.raycastTarget = raycast;
            return image;
        }

        static CodexPrefabSource()
        {
            ScreenPrefabKit.Register("Codex", Build);
        }

        [MenuItem("Gxdl/UI/生成/图鉴", priority = 313)]
        public static void Generate()
        {
            var path = Build();
            if (path != null) EditorUtility.DisplayDialog("生成完成", "已生成\n" + path, "确定");
        }

        public static string Build()
        {
            using (var builder = new UiPrefabBuilder("CodexView",
                new Vector2(UiRootLayout.DesignWidth, UiRootLayout.DesignHeight)))
            {
                var root = builder.Root;
                AnchorUtility.Apply(root, AnchorPreset.Stretch, Vector2.zero, Vector2.zero);
                ScreenPrefabKit.AddRootComponents(root);

                var background = builder.AnchoredNode("", "Background", AnchorPreset.Stretch, Vector2.zero)
                    .gameObject.AddComponent<Image>();
                background.sprite = BgSprite();
                background.raycastTarget = false;

                BuildHeader(builder);
                BuildEntries(builder);
                BuildTable(builder);

                var path = builder.Save(PrefabPath);
                AssetDatabase.SaveAssets();
                if (path != null) Bind(path);
                return path;
            }
        }

        static void BuildHeader(UiPrefabBuilder builder)
        {
            // 面板大框（图集比例 0.71，显示 620x780 轻微拉伸）。
            SkinImage(builder, "", "Codex Panel", 65f, 300f, 620f, 780f, "codex-panel");
            builder.TextNode("Codex Panel", "Title", 152f, 30f, 316f, 44f, "道宝图鉴 · 五品质三阶", 26, TitleInk,
                TextAnchor.MiddleCenter);

            // 关闭钮：图集金色圆形 X，复用 backButton 绑定。
            var backNode = builder.AnchoredNode("", "Header Back", AnchorPreset.TopLeft, new Vector2(42f, 46f), new Vector2(650f, 320f));
            var backImage = backNode.gameObject.AddComponent<Image>();
            backImage.sprite = Skin("codex-close");
            backImage.raycastTarget = true;
            backNode.gameObject.AddComponent<Button>().targetGraphic = backImage;
            builder.TextNode("Header Back", "Label", 0f, 0f, 42f, 46f, string.Empty, 24, ScreenPrefabKit.Cream, TextAnchor.MiddleCenter);

            builder.TextNode("", "Codex Hint", 210f, 402f, 340f, 30f, string.Empty, 18, HintInk);
        }

        static void BuildEntries(UiPrefabBuilder builder)
        {
            builder.Node("", "Codex Entry Root", 0f, 436f, UiRootLayout.DesignWidth, 350f);

            for (var i = 0; i < 12; i++)
            {
                var path = "Codex Entry Root/Entry " + i;
                var x = 100f + i % 4 * 143f;
                var y = i / 4 * 118f;
                var cell = builder.Node("Codex Entry Root", "Entry " + i, x, y, 110f, 112f);
                cell.gameObject.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.001f);

                SkinImage(builder, path, "Cell", 0f, 0f, 110f, 82f, "codex-cell");
                builder.ImageNode(path, "Shadow", 19f, 74f, 72f, 8f, new Color(0.31f, 0.28f, 0.16f, 0.25f));
                builder.ImageNode(path, "Icon", 12f, 4f, 86f, 74f, Color.white).raycastTarget = false;
                builder.TextNode(path, "Name", 0f, 86f, 110f, 26f, string.Empty, 19,
                    CellNameInk, TextAnchor.MiddleCenter);

                var button = cell.gameObject.AddComponent<Button>();
                button.targetGraphic = cell.gameObject.GetComponent<Image>();
                button.transition = Selectable.Transition.None;
            }
        }

        static void BuildTable(UiPrefabBuilder builder)
        {
            var panel = builder.AnchoredNode("", "Tier Table", AnchorPreset.Center, new Vector2(700f, 900f));
            panel.gameObject.AddComponent<Image>().color = new Color(0.96f, 0.94f, 0.88f, 0.98f);
            // 品质表默认收起，由 CodexView.ShowTable 按点击展开。
            panel.gameObject.SetActive(false);

            builder.TextNode("Tier Table", "Title", 35f, 20f, 630f, 48f, string.Empty, 30, TitleInk);
            builder.TextNode("Tier Table", "Note", 45f, 88f, 590f, 24f, string.Empty, 18, HintInk);

            var headers = new[] { "品质 / 阶级", "攻击力", "生命值", "防御力" };
            var xs = new[] { 60f, 250f, 390f, 530f };
            var widths = new[] { 175f, 125f, 125f, 105f };
            builder.ImageNode("Tier Table", "Header Backing", 45f, 116f, 590f, 36f, new Color(0.13f, 0.29f, 0.27f, 0.13f));
            for (var c = 0; c < 4; c++)
            {
                builder.TextNode("Tier Table", "Header " + c, xs[c], 116f, widths[c], 36f, headers[c], 23,
                    HintInk, c == 0 ? TextAnchor.MiddleLeft : TextAnchor.MiddleRight);
            }

            builder.Node("Tier Table", "Row Root", 45f, 0f, 610f, 700f);

            for (var i = 0; i < RowCount; i++)
            {
                var path = "Tier Table/Row Root/Row " + i;
                var y = RowStart + i * RowHeight + i / 3 * 12f;
                var row = builder.Node("Tier Table/Row Root", "Row " + i, 0f, y, 610f, RowHeight);
                row.gameObject.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.001f);

                builder.ImageNode(path, "Quality Mark", 1f, 0f, 5f, 34f, Color.white);
                builder.TextNode(path, "Label", 15f, 0f, 175f, RowHeight, string.Empty, 23, CellNameInk);
                builder.TextNode(path, "Attack", 205f, 0f, 125f, RowHeight, string.Empty, 23,
                    CellNameInk, TextAnchor.MiddleRight);
                builder.TextNode(path, "Hp", 345f, 0f, 125f, RowHeight, string.Empty, 23,
                    CellNameInk, TextAnchor.MiddleRight);
                builder.TextNode(path, "Defense", 485f, 0f, 105f, RowHeight, string.Empty, 23,
                    CellNameInk, TextAnchor.MiddleRight);

                var button = row.gameObject.AddComponent<Button>();
                button.targetGraphic = row.gameObject.GetComponent<Image>();
                button.transition = Selectable.Transition.None;
            }

            var close = builder.Node("Tier Table", "Close", 180f, 757f, 320f, 60f);
            var closeImage = close.gameObject.AddComponent<Image>();
            closeImage.color = new Color(0.15f, 0.38f, 0.35f, 0.94f);
            closeImage.raycastTarget = true;
            close.gameObject.AddComponent<Button>().targetGraphic = closeImage;
            builder.TextNode("Tier Table/Close", "Label", 0f, 0f, 320f, 60f, "返回图鉴", 26,
                ScreenPrefabKit.Cream, TextAnchor.MiddleCenter);
        }

        static void Bind(string prefabPath)
        {
            var contents = PrefabUtility.LoadPrefabContents(prefabPath);
            if (contents == null) return;
            try
            {
                var view = contents.GetComponent<CodexView>();
                if (view == null) view = contents.AddComponent<CodexView>();

                var so = new SerializedObject(view);
                Set(so, "backButton", ButtonAt(contents, "Header Back"));
                Set(so, "hintText", FindText(contents, "Codex Hint"));

                var icons = new List<Image>();
                var names = new List<Text>();
                var shadows = new List<Image>();
                var buttons = new List<Button>();
                for (var i = 0; i < 12; i++)
                {
                    var path = "Codex Entry Root/Entry " + i;
                    icons.Add(FindImage(contents, path + "/Icon"));
                    names.Add(FindText(contents, path + "/Name"));
                    shadows.Add(FindImage(contents, path + "/Shadow"));
                    buttons.Add(ButtonAt(contents, path));
                }

                SetArray(so, "slotIcons", icons);
                SetArray(so, "slotNames", names);
                SetArray(so, "slotShadows", shadows);
                SetArray(so, "slotButtons", buttons);
                Set(so, "entryRoot", Find(contents, "Codex Entry Root"));

                Set(so, "tableTitle", FindText(contents, "Tier Table/Title"));
                Set(so, "tableNote", FindText(contents, "Tier Table/Note"));
                Set(so, "tablePanel", Find(contents, "Tier Table"));
                Set(so, "rowRoot", Find(contents, "Tier Table/Row Root"));

                var rowNames = new List<Text>();
                var rowAttack = new List<Text>();
                var rowHp = new List<Text>();
                var rowDefense = new List<Text>();
                var rowButtons = new List<Button>();
                for (var i = 0; i < RowCount; i++)
                {
                    var path = "Tier Table/Row Root/Row " + i;
                    rowNames.Add(FindText(contents, path + "/Label"));
                    rowAttack.Add(FindText(contents, path + "/Attack"));
                    rowHp.Add(FindText(contents, path + "/Hp"));
                    rowDefense.Add(FindText(contents, path + "/Defense"));
                    rowButtons.Add(ButtonAt(contents, path));
                }

                SetArray(so, "rowNames", rowNames);
                SetArray(so, "rowAttack", rowAttack);
                SetArray(so, "rowHp", rowHp);
                SetArray(so, "rowDefense", rowDefense);
                SetArray(so, "rowButtons", rowButtons);
                Set(so, "closeTableButton", ButtonAt(contents, "Tier Table/Close"));
                so.ApplyModifiedPropertiesWithoutUndo();

                PrefabUtility.SaveAsPrefabAsset(contents, prefabPath);
                AssetDatabase.SaveAssets();
                Debug.Log("[Codex] 已生成并绑定 " + prefabPath);
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
