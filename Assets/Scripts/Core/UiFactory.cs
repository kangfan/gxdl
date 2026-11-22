using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Gxdl.Core
{
    /// <summary>
    /// UGUI 构建原语集合。承接原先散落在 GxdlGame 上的 Rect/Txt/Btn/Img/Art 等方法，
    /// 但持有自己的字体与贴图缓存，不再依赖任何界面实例状态。
    /// 设计基准分辨率固定为 750 x 1334，所有界面坐标沿用该参考系。
    /// </summary>
    public sealed class UiFactory
    {
        public const float DesignWidth = 750f;
        public const float DesignHeight = 1334f;

        public static readonly Color Cream = new Color(0.94f, 0.94f, 0.87f);
        public static readonly Color Ink = new Color(0.16f, 0.24f, 0.22f);
        public static readonly Color Gold = new Color(0.86f, 0.78f, 0.54f);

        readonly Dictionary<string, Sprite> sprites = new Dictionary<string, Sprite>();
        Font font;

        public UiFactory(RectTransform canvasRoot, Font uiFont)
        {
            CanvasRoot = canvasRoot != null
                ? canvasRoot
                : throw new ArgumentNullException(nameof(canvasRoot));
            font = uiFont;
        }

        /// <summary>界面内容的父节点，由 Canvas 提供。</summary>
        public RectTransform CanvasRoot { get; }

        public Font Font => font != null ? font : font = Resources.Load<Font>("Original/GameFont");

        /// <summary>
        /// 按设计像素坐标创建节点。原点在左上角，y 向下为正，
        /// 与项目现有界面坐标保持一致，迁移时可以直接复用原数值。
        /// </summary>
        public RectTransform Rect(string name, Transform parent, float x, float y, float w, float h)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rect = go.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(x, -y);
            rect.sizeDelta = new Vector2(w, h);
            return rect;
        }

        /// <summary>
        /// 按资源路径取贴图，带进程内缓存。路径不存在时只告警一次，
        /// 不允许抛异常打断界面构建。
        /// </summary>
        public Sprite Sprite(string path)
        {
            if (string.IsNullOrEmpty(path)) return null;
            if (sprites.TryGetValue(path, out var cached)) return cached;

            var texture = Resources.Load<Texture2D>(path);
            if (texture == null)
            {
                Debug.LogWarning($"[UiFactory] 缺少贴图：{path}");
                sprites[path] = null;
                return null;
            }

            var sprite = UnityEngine.Sprite.Create(
                texture,
                new Rect(0f, 0f, texture.width, texture.height),
                new Vector2(0.5f, 0.5f),
                100f);
            sprites[path] = sprite;
            return sprite;
        }

        /// <summary>创建一个不参与射线检测的图片节点。</summary>
        public Image Img(string path, Transform parent, float x, float y, float w, float h, Color? color = null)
        {
            var rect = Rect(path, parent, x, y, w, h);
            var image = rect.gameObject.AddComponent<Image>();
            image.color = color ?? Color.white;
            image.raycastTarget = false;
            if (!string.IsNullOrEmpty(path)) image.sprite = Sprite(path);
            return image;
        }

        /// <summary>创建图片节点并保留原始宽高比，九宫格图自动切到 Sliced 模式。</summary>
        public Image Art(Sprite sprite, Transform parent, float x, float y, float w, float h, bool preserveAspect = true)
        {
            var image = Img(string.Empty, parent, x, y, w, h);
            image.sprite = sprite;
            image.preserveAspect = preserveAspect;
            if (sprite != null && sprite.border.sqrMagnitude > 0f)
            {
                image.type = Image.Type.Sliced;
                image.preserveAspect = false;
            }

            return image;
        }

        /// <summary>创建文本节点。默认不接收射线，避免遮挡按钮点击。</summary>
        public Text Txt(
            string value,
            Transform parent,
            float x,
            float y,
            float w,
            float h,
            int size = 26,
            Color? color = null,
            TextAnchor align = TextAnchor.MiddleCenter)
        {
            var rect = Rect(string.IsNullOrEmpty(value) ? "Text" : value, parent, x, y, w, h);
            var text = rect.gameObject.AddComponent<Text>();
            text.font = Font;
            text.text = value;
            text.fontSize = size;
            text.color = color ?? Cream;
            text.alignment = align;
            text.raycastTarget = false;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            return text;
        }

        /// <summary>
        /// 创建按钮。传入空文本即为纯图标按钮。
        /// 返回的 Button 已挂好回调，调用方不必再处理 targetGraphic。
        /// </summary>
        public Button Btn(
            string value,
            Transform parent,
            float x,
            float y,
            float w,
            float h,
            Action action,
            Sprite sprite = null,
            Color? color = null,
            int size = 26)
        {
            var image = Img(string.Empty, parent, x, y, w, h, color ?? new Color(0.15f, 0.38f, 0.35f, 0.94f));
            image.name = string.IsNullOrEmpty(value) ? "Button" : value;
            image.raycastTarget = true;

            if (sprite != null)
            {
                image.sprite = sprite;
                if (sprite.border.sqrMagnitude > 0f) image.type = Image.Type.Sliced;
            }

            var button = image.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            if (action != null) button.onClick.AddListener(() => action());
            if (!string.IsNullOrEmpty(value))
            {
                Txt(value, image.transform, 0f, 0f, w, h, size);
            }

            return button;
        }

        /// <summary>
        /// 把 750 x 1334 的页面按画布实际尺寸等比缩放并居中，
        /// 替代原先 GxdlGame.FitPage 的逐帧重算逻辑。
        /// </summary>
        public void FitPage(RectTransform page)
        {
            if (page == null) return;

            var canvas = CanvasRoot;
            var scale = Mathf.Min(
                canvas.rect.width / DesignWidth,
                canvas.rect.height / DesignHeight);
            if (scale <= 0f) scale = 1f;

            page.anchorMin = page.anchorMax = new Vector2(0.5f, 0.5f);
            page.pivot = new Vector2(0f, 1f);
            page.anchoredPosition = new Vector2(-DesignWidth * 0.5f, DesignHeight * 0.5f) * scale;
            page.localScale = Vector3.one * scale;
        }

        /// <summary>创建一张新页面并完成适配，作为界面切换时的根节点。</summary>
        public RectTransform CreatePage(string name)
        {
            var page = Rect(name, CanvasRoot, 0f, 0f, DesignWidth, DesignHeight);
            FitPage(page);
            return page;
        }

        /// <summary>释放贴图缓存，退出播放模式时调用以避免残留引用。</summary>
        public void ClearCache()
        {
            sprites.Clear();
        }
    }
}
