using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Gxdl.EditorTools
{
    /// <summary>
    /// 开场动画界面的 prefab 生成器。
    ///
    /// 节点结构照 GuxuStoryPresentation.PlayStory 还原，但布局方式改为锚点：
    /// 全屏元素用 Stretch，贴边元素用 Top/Bottom 锚点，固定尺寸元素保持原尺寸。
    /// 这样在长屏、宽屏与平板机型上，标题贴顶、字幕贴底、图片居中铺满，
    /// 不会像固定 750x1334 那样出现上下黑边或内容被裁。
    /// </summary>
    public static class StoryOverlayPrefabSource
    {
        public const string PrefabPath = "Assets/Resources/UI/StoryOverlayView.prefab";

        static readonly Color Gold = new Color(0.86f, 0.78f, 0.54f);
        static readonly Color Cream = new Color(0.94f, 0.94f, 0.87f);
        static readonly Color Backing = new Color(0.025f, 0.055f, 0.075f, 1f);

        [MenuItem("Gxdl/UI/生成/开场动画界面", priority = 300)]
        public static void Generate()
        {
            var path = Build();
            if (path != null)
            {
                EditorUtility.DisplayDialog("生成完成", "已生成\n" + path, "确定");
            }
        }

        public static string Build()
        {
            using (var builder = new UiPrefabBuilder("StoryOverlayView", new Vector2(750f, 1334f)))
            {
                // 根节点：宽度锁设计值，高度随屏幕，配合 SafeAreaFitter 避开刘海。
                var root = builder.Root;
                AnchorUtility.Apply(root, AnchorPreset.Stretch, Vector2.zero, Vector2.zero);
                builder.GetOrAddComponent<Gxdl.Modules.UiRootLayout>("");
                builder.GetOrAddComponent<Gxdl.Modules.SafeAreaFitter>("");
                var fadeRoot = root.GetComponent<CanvasGroup>();
                if (fadeRoot == null) fadeRoot = root.gameObject.AddComponent<CanvasGroup>();
                fadeRoot.alpha = 0f;
                if (root.GetComponent<Gxdl.Modules.StoryOverlayController>() == null)
                {
                    root.gameObject.AddComponent<Gxdl.Modules.StoryOverlayController>();
                }

                // 全屏不透明遮罩，阻断下层界面点击，任意分辨率铺满。
                var backing = builder.AnchoredNode("", "Backing", AnchorPreset.Stretch, Vector2.zero);
                var backingImage = backing.gameObject.AddComponent<Image>();
                backingImage.color = Backing;
                backingImage.raycastTarget = true;

                // 图片视口全屏铺满，用 RectMask2D 裁掉越界部分。
                var viewport = builder.AnchoredNode("", "Story Viewport", AnchorPreset.Stretch, Vector2.zero);
                viewport.gameObject.AddComponent<RectMask2D>();

                // 两个图片槽尺寸均为 900x1350，锚定屏幕中心，
                // 由控制器按镜头做推拉，锚点保证中心不随分辨率漂移。
                var outgoing = builder.AnchoredNode("Story Viewport", "Outgoing Story Art",
                    AnchorPreset.Center, new Vector2(900f, 1350f));
                var outgoingImage = outgoing.gameObject.AddComponent<RawImage>();
                outgoingImage.raycastTarget = false;
                outgoingImage.color = Color.clear;

                var incoming = builder.AnchoredNode("Story Viewport", "Incoming Story Art",
                    AnchorPreset.Center, new Vector2(900f, 1350f));
                var incomingImage = incoming.gameObject.AddComponent<RawImage>();
                incomingImage.raycastTarget = false;
                incomingImage.color = Color.clear;

                // 符文与光晕锚在图片槽中心，跟随图片推拉。
                var spell = builder.AnchoredNode("Story Viewport/Incoming Story Art", "Story Moving Runes",
                    AnchorPreset.Center, new Vector2(440f, 440f));
                spell.gameObject.AddComponent<GuxuSpellLayer>().raycastTarget = false;

                var glow = builder.AnchoredNode("Story Viewport/Incoming Story Art", "Story Focus Glow",
                    AnchorPreset.Center, new Vector2(400f, 400f));
                glow.gameObject.AddComponent<GuxuSoftVfx>().raycastTarget = false;

                // 氛围层铺满视口，雾与光尘的绝对位置由动画每帧重算。
                builder.AnchoredNode("Story Viewport", "Story Atmosphere", AnchorPreset.Stretch, Vector2.zero);

                for (var i = 0; i < 5; i++)
                {
                    var wisp = builder.AnchoredNode("Story Viewport/Story Atmosphere", "Foreground Mist " + i,
                        AnchorPreset.BottomCenter, new Vector2(650f, 290f),
                        new Vector2(-320f + i * 170f, 420f + i % 2 * 130f));
                    var vfx = wisp.gameObject.AddComponent<GuxuSoftVfx>();
                    vfx.Smoke = true;
                    vfx.Seed = i * 0.63f;
                    vfx.raycastTarget = false;
                }

                for (var i = 0; i < 22; i++)
                {
                    var size = 7f + i % 3 * 3f;
                    var mote = builder.AnchoredNode("Story Viewport/Story Atmosphere", "Story Light Dust " + i,
                        AnchorPreset.Center, new Vector2(size, size));
                    mote.gameObject.AddComponent<GuxuSoftVfx>().raycastTarget = false;
                }

                var flash = builder.AnchoredNode("Story Viewport/Story Atmosphere", "Story Light Bloom",
                    AnchorPreset.Center, new Vector2(570f, 670f));
                flash.gameObject.AddComponent<GuxuSoftVfx>().raycastTarget = false;

                // 上下渐变贴合屏幕边缘，宽度铺满、高度固定。
                var topShade = builder.AnchoredNode("", "Story Top Shade", AnchorPreset.StretchTop, new Vector2(0f, 260f));
                topShade.gameObject.AddComponent<GuxuStoryShade>().Top = true;

                builder.AnchoredNode("", "Story Subtitle Shade", AnchorPreset.StretchBottom, new Vector2(0f, 414f))
                    .gameObject.AddComponent<GuxuStoryShade>();

                builder.AnchoredNode("", "Story Header", AnchorPreset.TopLeft,
                    new Vector2(500f, 36f), new Vector2(38f, 38f), rect => rect.pivot = Vector2.up);
                builder.TextNode("Story Header", "Label", 0f, 0f, 500f, 36f,
                    "序 章  /  遗 石 初 鸣", 21, Gold);

                // 跳过按钮贴右上角，保证任何分辨率下都在可点击范围内。
                var skip = builder.AnchoredNode("", "Skip Story", AnchorPreset.TopRight,
                    new Vector2(162f, 52f), new Vector2(38f, 28f), rect => rect.pivot = Vector2.one);
                var skipImage = skip.gameObject.AddComponent<RawImage>();
                skipImage.texture = Resources.Load<Texture2D>("Game/Guxu/Expansion/UI/button-story-skip");
                skipImage.color = Color.white;
                skipImage.raycastTarget = true;
                var skipButton = skip.gameObject.AddComponent<Button>();
                skipButton.targetGraphic = skipImage;
                builder.TextNode("Skip Story", "Label", 0f, 0f, 162f, 52f, "跳过动画", 26, Gold, TextAnchor.MiddleCenter);

                builder.AnchoredNode("", "Story Chapter", AnchorPreset.TopLeft,
                    new Vector2(570f, 33f), new Vector2(38f, 80f), rect => rect.pivot = Vector2.up);
                builder.TextNode("Story Chapter", "Label", 0f, 0f, 570f, 33f, string.Empty, 22, Cream);

                // 字幕组贴底，高度固定；屏幕越高，字幕仍紧贴底部。
                var captions = builder.AnchoredNode("", "Story Captions", AnchorPreset.StretchBottom,
                    new Vector2(0f, 240f));
                captions.gameObject.AddComponent<CanvasGroup>();
                builder.AnchoredNode("Story Captions", "Caption Rule", AnchorPreset.TopLeft,
                    new Vector2(38f, 2f), new Vector2(38f, 0f), rect => rect.pivot = Vector2.up).gameObject.AddComponent<Image>().color = Gold;

                var speakerRect = builder.AnchoredNode("Story Captions", "Story Speaker", AnchorPreset.StretchTop,
                    new Vector2(-76f, 32f), new Vector2(0f, 20f));
                var speaker = speakerRect.gameObject.AddComponent<Text>();
                ConfigureText(speaker, string.Empty, 23, Gold, TextAnchor.MiddleLeft);

                var subtitleRect = builder.AnchoredNode("Story Captions", "Story Subtitle", AnchorPreset.StretchTop,
                    new Vector2(-76f, 161f), new Vector2(0f, 67f));
                var subtitle = subtitleRect.gameObject.AddComponent<Text>();
                ConfigureText(subtitle, string.Empty, 29, Cream, TextAnchor.UpperLeft);
                subtitle.lineSpacing = 1.22f;

                // 进度条贴底边，宽度由控制器按播放进度调整。
                builder.AnchoredNode("", "Story Progress", AnchorPreset.BottomLeft,
                    new Vector2(0f, 2f), new Vector2(38f, 15f));

                // 章节标题组锚定屏幕中心偏上，不随高度变化而压到字幕。
                var titleGroup = builder.AnchoredNode("", "Story Title", AnchorPreset.Center,
                    new Vector2(750f, 200f), new Vector2(0f, 282f));
                titleGroup.gameObject.AddComponent<CanvasGroup>();

                var titleShade = builder.AnchoredNode("Story Title", "Title Soft Shade", AnchorPreset.Center,
                    new Vector2(830f, 260f));
                var shadeVfx = titleShade.gameObject.AddComponent<GuxuSoftVfx>();
                shadeVfx.raycastTarget = false;
                shadeVfx.color = new Color(0.015f, 0.04f, 0.065f, 0.7f);

                var titleRect = builder.AnchoredNode("Story Title", "Game Title", AnchorPreset.TopCenter,
                    new Vector2(700f, 110f));
                var title = titleRect.gameObject.AddComponent<Text>();
                ConfigureText(title, "古 墟 大 陆", 76, new Color(1f, 0.91f, 0.67f), TextAnchor.MiddleCenter);
                var titleShadow = title.gameObject.AddComponent<Shadow>();
                titleShadow.effectColor = new Color(0.03f, 0.1f, 0.13f, 0.9f);
                titleShadow.effectDistance = new Vector2(3f, -3f);

                var subtitleLine = builder.AnchoredNode("Story Title", "Subtitle Line", AnchorPreset.TopCenter,
                    new Vector2(670f, 50f), new Vector2(0f, 119f));
                var line = subtitleLine.gameObject.AddComponent<Text>();
                ConfigureText(line, "一 符 破 古 封  ·  一 剑 问 前 尘", 23, Cream, TextAnchor.MiddleCenter);

                var path = builder.Save(PrefabPath);
                AssetDatabase.SaveAssets();

                if (path != null) BindExplicit(path);

                return path;
            }
        }

        static void BindExplicit(string prefabPath)
        {
            var contents = PrefabUtility.LoadPrefabContents(prefabPath);
            if (contents == null)
            {
                Debug.LogError("[StoryOverlay] 无法打开 prefab：" + prefabPath);
                return;
            }

            try
            {
                var view = contents.GetComponent<Gxdl.Modules.StoryOverlayView>();
                if (view == null) view = contents.AddComponent<Gxdl.Modules.StoryOverlayView>();

                var so = new SerializedObject(view);
                Set(so, "outgoingArt", Find(contents, "Story Viewport/Outgoing Story Art"));
                Set(so, "incomingArt", Find(contents, "Story Viewport/Incoming Story Art"));
                Set(so, "movingRunes", Find(contents, "Story Viewport/Incoming Story Art/Story Moving Runes"));
                Set(so, "focusGlow", Find(contents, "Story Viewport/Incoming Story Art/Story Focus Glow"));
                Set(so, "lightBloom", Find(contents, "Story Viewport/Story Atmosphere/Story Light Bloom"));
                SetArray(so, "foregroundMist", contents, "Foreground Mist ");
                SetArray(so, "lightDust", contents, "Story Light Dust ");
                Set(so, "overlayFade", contents.GetComponent<CanvasGroup>());
                Set(so, "captionGroup", Find(contents, "Story Captions"));
                Set(so, "titleGroup", Find(contents, "Story Title"));
                Set(so, "headerText", Find(contents, "Story Header/Label"));
                Set(so, "chapterText", Find(contents, "Story Chapter/Label"));
                Set(so, "speakerText", Find(contents, "Story Captions/Story Speaker"));
                Set(so, "subtitleText", Find(contents, "Story Captions/Story Subtitle"));
                Set(so, "captionRect", FindRect(contents, "Story Captions"));
                Set(so, "progressBar", FindRect(contents, "Story Progress"));
                Set(so, "skipButton", contents.transform.Find("Skip Story").GetComponent<Button>());
                Set(so, "titleRect", FindRect(contents, "Story Title"));
                Set(so, "viewport", FindRect(contents, "Story Viewport"));
                Set(so, "rootFade", contents.GetComponent<CanvasGroup>());
                so.ApplyModifiedPropertiesWithoutUndo();

                PrefabUtility.SaveAsPrefabAsset(contents, prefabPath);
                AssetDatabase.SaveAssets();
                Debug.Log("[StoryOverlay] 已显式绑定 " + prefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contents);
            }
        }

        static Component FindRect(GameObject root, string path)
        {
            var node = root.transform.Find(path);
            return node;
        }

        static Component Find(GameObject root, string path)
        {
            var node = root.transform.Find(path);
            if (node == null) return null;
            foreach (var component in node.GetComponents<Component>())
            {
                if (component is Transform || component is CanvasRenderer) continue;
                return component;
            }

            return null;
        }

        static void Set(SerializedObject so, string field, Component value)
        {
            var property = so.FindProperty(field);
            if (property == null)
            {
                Debug.LogWarning("[StoryOverlay] View 缺少字段：" + field);
                return;
            }

            if (value == null)
            {
                Debug.LogWarning("[StoryOverlay] 节点未找到：" + field);
                return;
            }

            property.objectReferenceValue = value;
        }

        static void SetArray(SerializedObject so, string field, GameObject root, string prefix)
        {
            var property = so.FindProperty(field);
            if (property == null)
            {
                Debug.LogWarning("[StoryOverlay] View 缺少数组字段：" + field);
                return;
            }

            var elementType = field == "lightDust" ? typeof(GuxuSoftVfx) : typeof(GuxuSoftVfx);
            var matches = new System.Collections.Generic.List<Component>();
            foreach (var component in root.GetComponentsInChildren(elementType, true))
            {
                if (component == null || !component.name.StartsWith(prefix, System.StringComparison.Ordinal)) continue;
                matches.Add(component);
            }

            matches.Sort((a, b) => a.transform.GetSiblingIndex().CompareTo(b.transform.GetSiblingIndex()));
            property.arraySize = matches.Count;
            for (var i = 0; i < matches.Count; i++) property.GetArrayElementAtIndex(i).objectReferenceValue = matches[i];
        }

        static void ConfigureText(Text text, string content, int size, Color color, TextAnchor anchor)
        {
            text.font = Resources.Load<Font>("Original/GameFont");
            text.text = content;
            text.fontSize = size;
            text.color = color;
            text.alignment = anchor;
            text.raycastTarget = false;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
        }
    }
}
