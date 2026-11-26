using UnityEngine;
using UnityEngine.UI;

namespace Gxdl.Modules
{
    /// <summary>
    /// 不可见的点击区域。不生成网格，只接收射线。
    ///
    /// 替代「alpha=0.001 的全透明 Image」这种写法：
    /// 后者会参与合批与遮罩计算，层级复杂时容易漏拾取。
    /// </summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class UiRaycast : MaskableGraphic
    {
        protected override void Awake()
        {
            base.Awake();
            raycastTarget = true;
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
        }
    }
}
