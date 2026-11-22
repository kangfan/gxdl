using UnityEngine;

namespace Gxdl.Modules
{
    /// <summary>
    /// 安全区适配。把界面根节点收缩到屏幕安全区内，
    /// 避开刘海、挖孔与手势条，防止关键内容被遮挡。
    ///
    /// 挂在界面根节点上，运行时会覆盖锚点为「四向拉伸 + Screen.safeArea 内缩」。
    /// 不支持安全区查询的平台会退化为全屏，不影响桌面与编辑器预览。
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public sealed class SafeAreaFitter : MonoBehaviour
    {
        RectTransform rect;
        Rect lastSafeArea;
        Vector2Int lastScreenSize;

        void Awake()
        {
            rect = (RectTransform)transform;
            Apply();
        }

        void Update()
        {
            // 旋转屏幕或切换分屏会改变安全区，需要跟随。
            if (Screen.safeArea != lastSafeArea ||
                Screen.width != lastScreenSize.x ||
                Screen.height != lastScreenSize.y)
            {
                Apply();
            }
        }

        void Apply()
        {
            if (rect == null) return;

            lastSafeArea = Screen.safeArea;
            lastScreenSize = new Vector2Int(Screen.width, Screen.height);

            if (Screen.width <= 0 || Screen.height <= 0) return;

            var min = lastSafeArea.position;
            var max = lastSafeArea.position + lastSafeArea.size;

            min.x /= Screen.width;
            min.y /= Screen.height;
            max.x /= Screen.width;
            max.y /= Screen.height;

            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }
}
