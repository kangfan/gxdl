using System.Collections.Generic;
using Gxdl.Modules;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Gxdl.EditorTools
{
    /// <summary>
    /// 排行榜 prefab 生成器。MVP 展示本机真实记录的三行指标，
    /// 联网排行接入后只换数据源与说明文字，不动结构。
    /// </summary>
    [InitializeOnLoad]
    public static class RankPrefabSource
    {
        public const string PrefabPath = "Assets/Resources/" + UiScreens.Rank + ".prefab";

        static readonly Color Panel = new Color(0.06f, 0.12f, 0.15f, 0.94f);

        static RankPrefabSource()
        {
            ScreenPrefabKit.Register("Rank", Build);
        }

        [MenuItem("Gxdl/UI/生成/排行榜", priority = 303)]
        public static void Generate()
        {
            var path = Build();
            if (path != null) EditorUtility.DisplayDialog("生成完成", "已生成\n" + path, "确定");
        }

        public static string Build()
        {
            using (var builder = new UiPrefabBuilder("RankView",
                new Vector2(UiRootLayout.DesignWidth, UiRootLayout.DesignHeight)))
            {
                var root = builder.Root;
                AnchorUtility.Apply(root, AnchorPreset.Stretch, Vector2.zero, Vector2.zero);
                ScreenPrefabKit.AddRootComponents(root);

                var background = builder.AnchoredNode("", "Background", AnchorPreset.Stretch, Vector2.zero)
                    .gameObject.AddComponent<Image>();
                background.color = new Color(0.015f, 0.045f, 0.065f);
                background.raycastTarget = false;

                var back = builder.Node("", "Back", 18f, 25f, 124f, 52f);
                var backImage = back.gameObject.AddComponent<Image>();
                backImage.color = new Color(0.15f, 0.38f, 0.35f, 0.94f);
                backImage.raycastTarget = true;
                back.gameObject.AddComponent<Button>().targetGraphic = backImage;
                builder.TextNode("Back", "Label", 0f, 0f, 124f, 52f, "‹ 古墟", 24, ScreenPrefabKit.Cream,
                    TextAnchor.MiddleCenter);

                builder.TextNode("", "Title", 180f, 16f, 390f, 65f, "排行榜", 44, ScreenPrefabKit.Gold);
                builder.TextNode("", "Subtitle", 80f, 88f, 590f, 37f, "本机记录", 26, ScreenPrefabKit.Cream);

                for (var i = 0; i < 3; i++) BuildRow(builder, i);
                builder.TextNode("", "Note", 30f, 1200f, 690f, 60f, string.Empty, 22,
                    ScreenPrefabKit.Cream, TextAnchor.MiddleCenter);

                var path = builder.Save(PrefabPath);
                AssetDatabase.SaveAssets();
                if (path != null) Bind(path);
                return path;
            }
        }

        static void BuildRow(UiPrefabBuilder builder, int index)
        {
            var name = "Rank " + index;
            var row = builder.Node("", name, 30f, 200f + index * 230f, 690f, 190f);
            row.gameObject.AddComponent<Image>().color = Panel;

            builder.TextNode(name, "Label", 36f, 24f, 300f, 44f, string.Empty, 30, ScreenPrefabKit.Gold);
            builder.TextNode(name, "Value", 36f, 76f, 600f, 54f, string.Empty, 38, ScreenPrefabKit.Cream);
            builder.TextNode(name, "Detail", 36f, 136f, 620f, 38f, string.Empty, 22, ScreenPrefabKit.Cream);
        }

        static void Bind(string prefabPath)
        {
            var contents = PrefabUtility.LoadPrefabContents(prefabPath);
            if (contents == null) return;
            try
            {
                var view = contents.GetComponent<RankView>();
                if (view == null) view = contents.AddComponent<RankView>();

                var so = new SerializedObject(view);
                Set(so, "backButton", Find(contents, "Back")?.GetComponent<Button>());
                Set(so, "noteText", Find(contents, "Note")?.GetComponent<Text>());

                var labels = new List<Text>();
                var values = new List<Text>();
                var details = new List<Text>();
                for (var i = 0; i < 3; i++)
                {
                    var path = "Rank " + i;
                    labels.Add(Find(contents, path + "/Label")?.GetComponent<Text>());
                    values.Add(Find(contents, path + "/Value")?.GetComponent<Text>());
                    details.Add(Find(contents, path + "/Detail")?.GetComponent<Text>());
                }
                SetArray(so, "rowLabels", labels);
                SetArray(so, "rowValues", values);
                SetArray(so, "rowDetails", details);
                so.ApplyModifiedPropertiesWithoutUndo();

                PrefabUtility.SaveAsPrefabAsset(contents, prefabPath);
                AssetDatabase.SaveAssets();
                Debug.Log("[Rank] 已生成并绑定 " + prefabPath);
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
