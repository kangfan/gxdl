using System;
using UnityEngine;
using UnityEngine.UI;

namespace Gxdl.Modules
{
    /// <summary>
    /// 页签组。按钮数组 + 每页的选中指示对象，选中态只切换指示与可用性。
    /// 业务响应通过 Selected 事件交给控制器，页签本身不做页面切换。
    /// </summary>
    public sealed class UiTabGroup : MonoBehaviour
    {
        [SerializeField] Button[] tabs;
        [SerializeField] GameObject[] indicators;

        public int Current { get; private set; } = -1;

        /// <summary>用户点击页签时触发，参数为页签索引。</summary>
        public event Action<int> Selected;

        public void Bind()
        {
            if (tabs == null) return;
            for (var i = 0; i < tabs.Length; i++)
            {
                if (tabs[i] == null) continue;
                var index = i;
                tabs[i].onClick.RemoveAllListeners();
                tabs[i].onClick.AddListener(() => Select(index, true));
            }

            if (Current < 0 && tabs.Length > 0) Select(0, false);
        }

        /// <summary>设置选中页签并刷新指示；notify 为真时向外广播。</summary>
        public void Select(int index, bool notify = false)
        {
            if (tabs == null || index < 0 || index >= tabs.Length) return;
            Current = index;

            for (var i = 0; i < tabs.Length; i++)
            {
                if (tabs[i] == null) continue;
                tabs[i].interactable = i != index;
            }

            if (indicators == null) return;
            for (var i = 0; i < indicators.Length; i++)
            {
                if (indicators[i] != null) indicators[i].SetActive(i == index);
            }

            if (notify) Selected?.Invoke(index);
        }

        /// <summary>按条件禁用个别页签，例如未解锁的分支。</summary>
        public void SetInteractable(int index, bool value)
        {
            if (tabs == null || index < 0 || index >= tabs.Length || tabs[index] == null) return;
            if (index == Current) return;
            tabs[index].interactable = value;
        }
    }
}
