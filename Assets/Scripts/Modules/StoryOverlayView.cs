using UnityEngine;
using UnityEngine.UI;

namespace Gxdl.Modules
{
    /// <summary>
    /// 开场动画界面视图。只持有控件引用，不参与流程控制。
    /// prefab 由 Gxdl/UI/生成/开场动画界面 生成，
    /// 7 个镜头共用同一套节点，图片内容由控制器逐张填入。
    /// </summary>
    public partial class StoryOverlayView : UiModule
    {
        public override string ModuleId => "StoryOverlayView";

        [Header("图片槽")]
        [SerializeField] RawImage outgoingArt;
        [SerializeField] RawImage incomingArt;

        [Header("特效")]
        [SerializeField] GuxuSpellLayer movingRunes;
        [SerializeField] GuxuSoftVfx focusGlow;
        [SerializeField] GuxuSoftVfx lightBloom;
        [SerializeField] GuxuSoftVfx[] foregroundMist;
        [SerializeField] GuxuSoftVfx[] lightDust;

        [Header("遮罩与文本")]
        [SerializeField] CanvasGroup overlayFade;
        [SerializeField] CanvasGroup captionGroup;
        [SerializeField] CanvasGroup titleGroup;
        [SerializeField] Text headerText;
        [SerializeField] Text chapterText;
        [SerializeField] Text speakerText;
        [SerializeField] Text subtitleText;
        [SerializeField] RectTransform captionRect;
        [SerializeField] RectTransform progressBar;
        [SerializeField] Button skipButton;
        [SerializeField] RectTransform titleRect;
        [SerializeField] RectTransform viewport;
        [SerializeField] CanvasGroup rootFade;

        public RawImage Outgoing => outgoingArt;

        public RawImage Incoming => incomingArt;

        public GuxuSpellLayer Runes => movingRunes;

        public GuxuSoftVfx Glow => focusGlow;

        public GuxuSoftVfx Bloom => lightBloom;

        public GuxuSoftVfx[] Mist => foregroundMist;

        public GuxuSoftVfx[] Dust => lightDust;

        public CanvasGroup Fade => overlayFade;

        public CanvasGroup Captions => captionGroup;

        public CanvasGroup Title => titleGroup;

        public RectTransform CaptionRect => captionRect;

        public RectTransform Progress => progressBar;

        public RectTransform TitleRect => titleRect;

        public RectTransform Viewport => viewport;

        public CanvasGroup RootFade => rootFade;

        public Button SkipButton => skipButton;

        /// <summary>按镜头更新文本。动画曲线由控制器负责。</summary>
        public void ApplyShotText(string chapter, string speaker, string subtitle)
        {
            if (chapterText != null) chapterText.text = chapter ?? string.Empty;
            if (speakerText != null) speakerText.text = speaker ?? string.Empty;
            if (subtitleText != null) subtitleText.text = subtitle ?? string.Empty;
        }

        public void SetHeader(string value)
        {
            if (headerText != null) headerText.text = value ?? string.Empty;
        }

        /// <summary>进度条宽度按 0..1 映射到 674 像素基准。</summary>
        public void SetProgress(float normalized)
        {
            if (progressBar == null) return;
            progressBar.sizeDelta = new Vector2(674f * Mathf.Clamp01(normalized), progressBar.sizeDelta.y);
        }

        public void SetTitleVisible(bool visible)
        {
            if (titleGroup != null) titleGroup.gameObject.SetActive(visible);
        }

        void OnValidate()
        {
            // 数组字段在 inspector 中容易漏填，这里提示但不强制报错。
            if (foregroundMist != null && foregroundMist.Length != 5)
            {
                Debug.LogWarning("[StoryOverlayView] 前景雾应为 5 层，当前 " + foregroundMist.Length + " 层");
            }

            if (lightDust != null && lightDust.Length != 22)
            {
                Debug.LogWarning("[StoryOverlayView] 光尘应为 22 粒，当前 " + lightDust.Length + " 粒");
            }
        }
    }
}
