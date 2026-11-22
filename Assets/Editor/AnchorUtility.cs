using UnityEngine;

namespace Gxdl.EditorTools
{
    /// <summary>
    /// 锚点预设。prefab 里的界面必须用锚点描述「相对屏幕的位置」，
    /// 而不是照抄某个分辨率下的绝对像素坐标，否则换机型就会错位。
    /// </summary>
    public enum AnchorPreset
    {
        /// <summary>左上角对齐，尺寸固定。适合顶部固定高度的标题栏。</summary>
        TopLeft,

        /// <summary>顶部居中，尺寸固定。适合顶部居中的标题与计数。</summary>
        TopCenter,

        /// <summary>右上角对齐，尺寸固定。适合设置、关闭等角落按钮。</summary>
        TopRight,

        /// <summary>左边居中，尺寸固定。适合侧边栏入口。</summary>
        MiddleLeft,

        /// <summary>屏幕正中，尺寸固定。适合弹窗、居中提示。</summary>
        Center,

        /// <summary>右边居中，尺寸固定。适合右侧竖排入口。</summary>
        MiddleRight,

        /// <summary>左下角对齐，尺寸固定。适合左下角的信息栏。</summary>
        BottomLeft,

        /// <summary>底部居中，尺寸固定。适合底部中央的主按钮。</summary>
        BottomCenter,

        /// <summary>右下角对齐，尺寸固定。适合右下角按钮。</summary>
        BottomRight,

        /// <summary>横向拉伸，高度固定，贴顶。适合占满宽度、高度固定的顶栏。</summary>
        StretchTop,

        /// <summary>横向拉伸，高度固定，贴底。适合占满宽度的底部导航栏。</summary>
        StretchBottom,

        /// <summary>纵向拉伸，宽度固定，贴左。适合左侧竖栏。</summary>
        StretchLeft,

        /// <summary>纵向拉伸，宽度固定，贴右。适合右侧竖栏。</summary>
        StretchRight,

        /// <summary>四向拉伸，铺满父节点。适合背景、全屏遮罩、内容区。</summary>
        Stretch
    }

    /// <summary>
    /// 锚点预设到 RectTransform 参数的换算。
    /// 保持与 UGUI 编辑器里 Anchor Preset 面板一致的行为。
    /// </summary>
    public static class AnchorUtility
    {
        public static void Apply(RectTransform rect, AnchorPreset preset, Vector2 size, Vector2 offset)
        {
            switch (preset)
            {
                case AnchorPreset.TopLeft:
                    Set(rect, Vector2.up, Vector2.up, new Vector2(0.5f, 1f));
                    rect.anchoredPosition = new Vector2(offset.x, -offset.y);
                    rect.sizeDelta = size;
                    break;

                case AnchorPreset.TopCenter:
                    Set(rect, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f));
                    rect.anchoredPosition = new Vector2(offset.x, -offset.y);
                    rect.sizeDelta = size;
                    break;

                case AnchorPreset.TopRight:
                    Set(rect, Vector2.one, Vector2.one, new Vector2(0.5f, 1f));
                    rect.anchoredPosition = new Vector2(-offset.x, -offset.y);
                    rect.sizeDelta = size;
                    break;

                case AnchorPreset.MiddleLeft:
                    Set(rect, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f));
                    rect.anchoredPosition = new Vector2(offset.x, -offset.y);
                    rect.sizeDelta = size;
                    break;

                case AnchorPreset.Center:
                    Set(rect, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
                    rect.anchoredPosition = new Vector2(offset.x, -offset.y);
                    rect.sizeDelta = size;
                    break;

                case AnchorPreset.MiddleRight:
                    Set(rect, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f));
                    rect.anchoredPosition = new Vector2(-offset.x, -offset.y);
                    rect.sizeDelta = size;
                    break;

                case AnchorPreset.BottomLeft:
                    Set(rect, Vector2.zero, Vector2.zero, Vector2.zero);
                    rect.anchoredPosition = new Vector2(offset.x, offset.y);
                    rect.sizeDelta = size;
                    break;

                case AnchorPreset.BottomCenter:
                    Set(rect, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f));
                    rect.anchoredPosition = new Vector2(offset.x, offset.y);
                    rect.sizeDelta = size;
                    break;

                case AnchorPreset.BottomRight:
                    Set(rect, Vector2.right, Vector2.right, new Vector2(1f, 0f));
                    rect.anchoredPosition = new Vector2(-offset.x, offset.y);
                    rect.sizeDelta = size;
                    break;

                case AnchorPreset.StretchTop:
                    Set(rect, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f));
                    rect.sizeDelta = new Vector2(size.x, size.y);
                    rect.anchoredPosition = new Vector2(0f, -offset.y);
                    break;

                case AnchorPreset.StretchBottom:
                    Set(rect, Vector2.zero, Vector2.right, new Vector2(0.5f, 0f));
                    rect.sizeDelta = new Vector2(size.x, size.y);
                    rect.anchoredPosition = new Vector2(0f, offset.y);
                    break;

                case AnchorPreset.StretchLeft:
                    Set(rect, Vector2.zero, Vector2.up, new Vector2(0f, 0.5f));
                    rect.sizeDelta = new Vector2(size.x, size.y);
                    rect.anchoredPosition = new Vector2(offset.x, 0f);
                    break;

                case AnchorPreset.StretchRight:
                    Set(rect, Vector2.right, Vector2.one, new Vector2(1f, 0.5f));
                    rect.sizeDelta = new Vector2(size.x, size.y);
                    rect.anchoredPosition = new Vector2(-offset.x, 0f);
                    break;

                case AnchorPreset.Stretch:
                    Set(rect, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f));
                    // 拉伸模式下 sizeDelta 表示相对父节点的内缩量，
                    // 用 offset 表达四边留白，全屏则传 zero。
                    rect.offsetMin = new Vector2(offset.x, offset.y);
                    rect.offsetMax = new Vector2(-offset.x, -offset.y);
                    break;

                default:
                    Set(rect, Vector2.up, Vector2.up, new Vector2(0.5f, 1f));
                    rect.anchoredPosition = new Vector2(offset.x, -offset.y);
                    rect.sizeDelta = size;
                    break;
            }
        }

        static void Set(RectTransform rect, Vector2 min, Vector2 max, Vector2 pivot)
        {
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.pivot = pivot;
        }
    }
}
