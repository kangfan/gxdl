using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Gxdl.Modules
{
    /// <summary>
    /// 垂直滚动列表。行由模板克隆，超出当前数量的行回收复用而不是销毁。
    ///
    /// 项目里的列表最多二十来行（章节 10、关卡 20、天赋 24），
    /// 因此不做虚拟化，只做池化：行数变化时增删激活状态，滚动交给 ScrollRect。
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public sealed class UiList : MonoBehaviour
    {
        [SerializeField] ScrollRect scroll;
        [SerializeField] RectTransform content;
        [SerializeField] RectTransform rowTemplate;

        [Header("排布")]
        [SerializeField] float rowHeight = 96f;
        [SerializeField] float spacing = 12f;
        [SerializeField] float paddingTop = 8f;
        [SerializeField] float paddingBottom = 8f;

        readonly List<RectTransform> rows = new List<RectTransform>();

        public int Count { get; private set; }

        public ScrollRect Scroll => scroll;

        public RectTransform Content => content;

        /// <summary>供生成器在 prefab 里注入节点，运行时不调用。</summary>
        public void Configure(ScrollRect scrollRect, RectTransform contentRect, RectTransform template,
            float height, float gap = 12f)
        {
            scroll = scrollRect;
            content = contentRect;
            rowTemplate = template;
            rowHeight = height;
            spacing = gap;
        }

        /// <summary>设置行数并逐行回调填充。多出的行隐藏，不足的行从模板克隆。</summary>
        public void SetCount(int count, Action<RectTransform, int> configure = null)
        {
            if (content == null) return;
            if (count < 0) count = 0;

            for (var i = 0; i < count; i++)
            {
                var row = Acquire(i);
                if (row == null) break;
                row.gameObject.SetActive(true);
                row.anchoredPosition = new Vector2(0f, -(paddingTop + i * (rowHeight + spacing)));
                row.sizeDelta = new Vector2(row.sizeDelta.x, rowHeight);
                configure?.Invoke(row, i);
            }

            for (var i = count; i < rows.Count; i++)
            {
                if (rows[i] != null) rows[i].gameObject.SetActive(false);
            }

            Count = count;
            RefreshContentHeight();
        }

        /// <summary>取第 index 行，越界返回 null。</summary>
        public RectTransform Get(int index)
        {
            if (index < 0 || index >= Count || index >= rows.Count) return null;
            return rows[index];
        }

        /// <summary>把指定行滚动到视口内。视口尺寸不可用时静默忽略。</summary>
        public void ScrollTo(int index)
        {
            if (scroll == null || content == null || index < 0 || index >= Count) return;
            var viewport = scroll.viewport != null ? scroll.viewport.rect.height : 0f;
            if (viewport <= 0f) return;

            var rowTop = paddingTop + index * (rowHeight + spacing);
            var limit = Mathf.Max(0f, content.rect.height - viewport);
            var target = Mathf.Clamp(rowTop - viewport * 0.5f + rowHeight * 0.5f, 0f, limit);
            content.anchoredPosition = new Vector2(content.anchoredPosition.x, target);
        }

        public void Clear()
        {
            SetCount(0);
        }

        /// <summary>行内容总高度，用于 content 尺寸与滚动边界。</summary>
        public float ContentHeight(int count)
        {
            if (count <= 0) return paddingTop + paddingBottom;
            return paddingTop + paddingBottom + count * rowHeight + (count - 1) * spacing;
        }

        RectTransform Acquire(int index)
        {
            if (index < rows.Count && rows[index] != null) return rows[index];
            if (index < rows.Count && rows[index] == null) rows.RemoveAt(index);

            if (rowTemplate == null)
            {
                Debug.LogError("[UiList] 缺少行模板，无法创建列表行");
                return null;
            }

            var row = Instantiate(rowTemplate, content, false);
            row.name = rowTemplate.name.Replace(" Template", string.Empty) + " " + index;
            row.gameObject.SetActive(true);

            while (rows.Count <= index) rows.Add(null);
            rows[index] = row;
            return row;
        }

        void RefreshContentHeight()
        {
            if (content == null) return;
            content.sizeDelta = new Vector2(content.sizeDelta.x, ContentHeight(Count));
        }
    }
}
