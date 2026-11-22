using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Gxdl.EditorTools
{
    /// <summary>
    /// 用代码构建 UGUI prefab。相比运行时抓取，这里没有动画干扰：
    /// 生成的是静态骨架，动画所需的初始值全部显式给定，
    /// 不会把协程跑到一半的中间状态烘进资源。
    ///
    /// 设计基准：750 x 1334，原点左上，y 向下为正，
    /// 与原界面代码的坐标习惯一致，便于逐行对照迁移。
    /// </summary>
    public sealed class UiPrefabBuilder : IDisposable
    {
        const float DesignWidth = 750f;
        const float DesignHeight = 1334f;

        readonly Dictionary<string, GameObject> nodes = new Dictionary<string, GameObject>();
        readonly GameObject root;
        readonly bool ownsRoot;

        public UiPrefabBuilder(string name, Vector2 size)
        {
            root = new GameObject(name, typeof(RectTransform));
            var rect = (RectTransform)root.transform;
            rect.sizeDelta = size;
            nodes[""] = root;
            ownsRoot = true;
        }

        public RectTransform Root => (RectTransform)root.transform;

        /// <summary>
        /// 以左上角为原点创建子节点。parentPath 为空表示挂在根节点下。
        /// </summary>
        public RectTransform Node(
            string parentPath,
            string name,
            float x,
            float y,
            float width,
            float height,
            Action<RectTransform> configure = null)
        {
            var parent = RequireTransform(parentPath);
            var go = new GameObject(name, typeof(RectTransform));
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(x, -y);
            rect.sizeDelta = new Vector2(width, height);

            configure?.Invoke(rect);
            Register(parentPath, name, go);
            return rect;
        }

        /// <summary>以节点中心为锚点创建子节点，用于需要居中的图片与特效层。</summary>
        public RectTransform CenteredNode(
            string parentPath,
            string name,
            float centerX,
            float centerY,
            float width,
            float height,
            Action<RectTransform> configure = null)
        {
            var parent = RequireTransform(parentPath);
            var go = new GameObject(name, typeof(RectTransform));
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(centerX, -centerY);
            rect.sizeDelta = new Vector2(width, height);

            configure?.Invoke(rect);
            Register(parentPath, name, go);
            return rect;
        }

        /// <summary>取节点上的组件，不存在则添加。避免与 UnityEngine.Component 类型同名。</summary>
        /// <summary>
        /// 按锚点预设创建子节点。界面布局应优先用这个方法：
        /// 尺寸与偏移相对于锚点表达，换分辨率时位置语义不变。
        /// </summary>
        public RectTransform AnchoredNode(
            string parentPath,
            string name,
            AnchorPreset preset,
            Vector2 size,
            Vector2 offset = default,
            Action<RectTransform> configure = null)
        {
            var parent = RequireTransform(parentPath);
            var go = new GameObject(name, typeof(RectTransform));
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            AnchorUtility.Apply(rect, preset, size, offset);

            configure?.Invoke(rect);
            Register(parentPath, name, go);
            return rect;
        }

        public T GetOrAddComponent<T>(string path) where T : UnityEngine.Component
        {
            var go = RequireNode(path);
            return go.GetComponent<T>() ?? go.AddComponent<T>();
        }

        public Text TextNode(
            string parentPath,
            string name,
            float x,
            float y,
            float width,
            float height,
            string content,
            int fontSize,
            Color color,
            TextAnchor anchor = TextAnchor.MiddleLeft)
        {
            var rect = Node(parentPath, name, x, y, width, height);
            var text = rect.gameObject.AddComponent<Text>();
            text.font = LoadFont();
            text.text = content;
            text.fontSize = fontSize;
            text.color = color;
            text.alignment = anchor;
            text.raycastTarget = false;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            return text;
        }

        public Image ImageNode(
            string parentPath,
            string name,
            float x,
            float y,
            float width,
            float height,
            Color color,
            bool raycast = false)
        {
            var rect = Node(parentPath, name, x, y, width, height);
            var image = rect.gameObject.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = raycast;
            return image;
        }

        /// <summary>
        /// 保存为 prefab。已存在则覆盖，并保留 meta 中的 GUID，
        /// 避免场景或其他 prefab 对它的引用断开。
        /// </summary>
        public string Save(string prefabPath)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(prefabPath) ?? ".");

            GameObject saved;
            try
            {
                saved = PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            }
            catch (Exception error)
            {
                Debug.LogError("[UiPrefabBuilder] 保存 prefab 失败：" + error.Message);
                throw;
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[UiPrefabBuilder] 已生成 {prefabPath}，节点 {nodes.Count} 个");
            return saved != null ? prefabPath : null;
        }

        void Register(string parentPath, string name, GameObject go)        {
            var path = string.IsNullOrEmpty(parentPath) ? name : parentPath + "/" + name;
            if (nodes.ContainsKey(path))
            {
                throw new InvalidOperationException("节点路径重复：" + path);
            }

            nodes[path] = go;
        }

        GameObject RequireNode(string path)
        {
            if (path == null) path = string.Empty;
            if (nodes.TryGetValue(path, out var node)) return node;
            throw new InvalidOperationException("找不到父节点：" + (path.Length == 0 ? "<root>" : path));
        }

        /// <summary>取父节点的 Transform。SetParent 需要 Transform 而非 GameObject。</summary>
        Transform RequireTransform(string path)
        {
            return RequireNode(path).transform;
        }

        static Font cachedFont;
        static Font LoadFont()
        {
            if (cachedFont != null) return cachedFont;
            cachedFont = Resources.Load<Font>("Original/GameFont");
            if (cachedFont == null) Debug.LogWarning("[UiPrefabBuilder] 未找到 Original/GameFont，文本将使用默认字体");
            return cachedFont;
        }

        public void Dispose()
        {
            if (ownsRoot && root != null) UnityEngine.Object.DestroyImmediate(root);
        }
    }
}
