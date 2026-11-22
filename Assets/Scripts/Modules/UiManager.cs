using System;
using System.Collections.Generic;
using Gxdl.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Gxdl.Modules
{
    /// <summary>
    /// 界面管理器：负责 prefab 的实例化、缓存、层级与显隐。
    /// 所有界面都挂在一个固定的界面根节点下，
    /// 避免各自往 Canvas 里乱塞导致层级顺序失控。
    /// </summary>
    public sealed class UiManager : MonoBehaviour
    {
        readonly Dictionary<string, UiModule> cache = new Dictionary<string, UiModule>();
        readonly List<string> order = new List<string>();

        IEventBus events;
        UiFactory ui;
        RectTransform moduleRoot;

        /// <summary>创建界面管理器并建立界面根节点。</summary>
        public static UiManager Create(Transform parent, IEventBus eventBus, UiFactory factory)
        {
            var go = new GameObject("Ui Manager", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            var manager = go.AddComponent<UiManager>();
            manager.events = eventBus;
            manager.ui = factory;
            manager.moduleRoot = rect;
            return manager;
        }

        public string CurrentModuleId { get; private set; }

        /// <summary>
        /// 打开界面。首次打开时从 Resources 实例化并缓存，
        /// 再次打开复用实例，避免频繁创建销毁带来的开销与动画重置。
        /// </summary>
        public UiModule Open(string prefabPath)
        {
            if (string.IsNullOrEmpty(prefabPath)) throw new ArgumentException("界面 prefab 路径不能为空", nameof(prefabPath));

            var module = Acquire(prefabPath);
            if (module == null) return null;

            var active = module.gameObject;
            active.SetActive(true);
            active.transform.SetAsLastSibling();

            order.Remove(module.ModuleId);
            order.Add(module.ModuleId);
            CurrentModuleId = module.ModuleId;

            module.OnShow();
            return module;
        }

        /// <summary>关闭指定界面，保留实例以便下次复用。</summary>
        public void Close(string moduleId)
        {
            if (string.IsNullOrEmpty(moduleId)) return;
            if (!cache.TryGetValue(moduleId, out var module)) return;

            module.OnHide();
            module.gameObject.SetActive(false);
            order.Remove(moduleId);
            if (CurrentModuleId == moduleId) CurrentModuleId = order.Count > 0 ? order[order.Count - 1] : null;
        }

        /// <summary>关闭当前界面。</summary>
        public void CloseCurrent()
        {
            if (CurrentModuleId != null) Close(CurrentModuleId);
        }

        /// <summary>关闭除指定界面外的全部界面，用于场景级别的切换。</summary>
        public void CloseAllExcept(string keepModuleId)
        {
            for (var i = order.Count - 1; i >= 0; i--)
            {
                var id = order[i];
                if (id == keepModuleId) continue;
                Close(id);
            }
        }

        /// <summary>销毁缓存实例，退出游戏或切换账号时调用。</summary>
        public void Clear()
        {
            foreach (var pair in cache)
            {
                if (pair.Value != null) Destroy(pair.Value.gameObject);
            }

            cache.Clear();
            order.Clear();
            CurrentModuleId = null;
        }

        UiModule Acquire(string prefabPath)
        {
            if (cache.TryGetValue(prefabPath, out var cached) && cached != null) return cached;

            var prefab = Resources.Load<GameObject>(prefabPath);
            if (prefab == null)
            {
                Debug.LogError("[UiManager] 找不到界面 prefab：" + prefabPath);
                return null;
            }

            var instance = Instantiate(prefab, moduleRoot, false);
            var module = instance.GetComponent<UiModule>();
            if (module == null)
            {
                Debug.LogError("[UiManager] prefab 根节点缺少 UiModule 组件：" + prefabPath);
                Destroy(instance);
                return null;
            }

            module.Bind(events, ui);
            cache[prefabPath] = module;
            return module;
        }

        void OnDestroy()
        {
            Clear();
        }
    }
}
