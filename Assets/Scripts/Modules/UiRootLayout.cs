using UnityEngine;

namespace Gxdl.Modules
{
    /// <summary>
    /// 界面根布局。设计宽度固定为 750，高度跟随画布实际高度，
    /// 避免固定 1334 高度在长屏机型上产生上下黑边。
    ///
    /// 页面内容据此用锚点贴边：顶栏贴顶、底栏贴底、内容区四向拉伸，
    /// 中间空隙由内容区吸收，而不是靠整体缩放留白。
    ///
    /// 高度超出设计值时，超出的部分只影响拉伸型节点，
    /// 固定尺寸节点（按钮、图标、文本）保持原始大小与可读性。
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    [DefaultExecutionOrder(-100)]
    public sealed class UiRootLayout : MonoBehaviour
    {
        /// <summary>设计基准宽度。所有界面坐标以该宽度为参照。</summary>
        public const float DesignWidth = 750f;

        /// <summary>设计基准高度。仅作为排布参考，实际高度由屏幕决定。</summary>
        public const float DesignHeight = 1334f;

        /// <summary>是否把宽度也交给父级拉伸。全屏界面通常保持 false。</summary>
        [SerializeField] bool stretchWidth;

        RectTransform rect;
        bool applying;
        Vector2 appliedSize = new Vector2(-1f, -1f);

        void Awake()
        {
            rect = (RectTransform)transform;
            Apply();
        }

        void OnRectTransformDimensionsChange()
        {
            // 父级尺寸变化时重新计算，例如分屏或分辨率切换。
            if (rect != null && isActiveAndEnabled) Apply();
        }

        void Apply()
        {
            // Apply 自身会修改锚点与尺寸，从而再次触发尺寸变化回调；
            // 重入保护和尺寸缓存共同打断这条递归链。
            if (applying) return;

            var parent = rect.parent as RectTransform;
            if (parent == null) return;

            var parentWidth = parent.rect.width;
            var parentHeight = parent.rect.height;
            if (parentWidth <= 0f || parentHeight <= 0f) return;

            var target = new Vector2(stretchWidth ? parentWidth : DesignWidth, parentHeight);
            if ((appliedSize - target).sqrMagnitude < 0.01f) return;

            applying = true;
            try
            {
                // 宽度锁定设计值并水平居中；高度填满父级。
                rect.anchorMin = new Vector2(0.5f, 0f);
                rect.anchorMax = new Vector2(0.5f, 1f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;

                rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, target.x);
                rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, target.y);
                rect.anchoredPosition = Vector2.zero;
                appliedSize = target;
            }
            finally
            {
                applying = false;
            }
        }
    }
}
