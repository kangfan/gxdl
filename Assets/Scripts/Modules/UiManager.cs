using System;
using System.Collections.Generic;
using Gxdl.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Gxdl.Modules
{
    /// <summary>界面层级。数字越大越靠上，弹窗与提示永远压在普通界面之上。</summary>
    public enum UiLayer
    {
        Background = 0,
        Normal = 1,
        Modal = 2,
        Toast = 3
    }

    /// <summary>
    /// 界面管理器：负责 prefab 的实例化、缓存、分层、层级与显隐。
    ///
    /// 每个层是一个铺满的 RectTransform，层内靠 sibling 顺序排；
    /// 弹窗打开时，下层普通界面进入暂停（停动画与倒计时），关闭后恢复。
    /// </summary>
    public sealed class UiManager : MonoBehaviour
    {
        readonly Dictionary<string, UiModule> cacheById = new Dictionary<string, UiModule>();
        readonly Dictionary<string, string> idByPath = new Dictionary<string, string>();
        readonly Dictionary<string, UiLayer> layers = new Dictionary<string, UiLayer>();
        readonly List<string> order = new List<string>();
        readonly HashSet<string> paused = new HashSet<string>();

        IEventBus events;
        UiFactory ui;
        RectTransform moduleRoot;
        RectTransform[] layerRoots;

        /// <summary>创建界面管理器并建立分层根节点。</summary>
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
            manager.BuildLayers();
            return manager;
        }

        public string CurrentModuleId { get; private set; }

        void BuildLayers()
        {
            layerRoots = new RectTransform[4];
            for (var i = 0; i < layerRoots.Length; i++)
            {
                var go = new GameObject(((UiLayer)i) + " Layer", typeof(RectTransform));
                var rect = (RectTransform)go.transform;
                rect.SetParent(moduleRoot, false);
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;
                layerRoots[i] = rect;
            }
        }

        /// <summary>
        /// 打开界面。首次打开从 Resources 实例化并缓存，再次打开复用。
        /// 弹窗层打开时暂停其下界面，关闭后恢复。
        /// </summary>
        public UiModule Open(string prefabPath, UiLayer layer = UiLayer.Normal)
        {
            if (string.IsNullOrEmpty(prefabPath)) throw new ArgumentException("界面 prefab 路径不能为空", nameof(prefabPath));

            var module = Acquire(prefabPath, layer);
            if (module == null) return null;

            layers[module.ModuleId] = layer;
            var active = module.gameObject;
            active.transform.SetParent(layerRoots[(int)layer], false);
            active.SetActive(true);
            active.transform.SetAsLastSibling();

            order.Remove(module.ModuleId);
            order.Add(module.ModuleId);
            CurrentModuleId = module.ModuleId;

            module.OnShow();
            ApplyPauseState();
            return module;
        }

        /// <summary>关闭指定界面，保留实例以便下次复用。</summary>
        public void Close(string moduleId)
        {
            if (string.IsNullOrEmpty(moduleId)) return;
            if (!cacheById.TryGetValue(moduleId, out var module) || module == null) return;

            if (paused.Remove(moduleId)) module.OnResume();
            module.OnHide();
            module.gameObject.SetActive(false);
            order.Remove(moduleId);
            if (CurrentModuleId == moduleId) CurrentModuleId = order.Count > 0 ? order[order.Count - 1] : null;
            ApplyPauseState();
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

        /// <summary>是否已有指定层的界面在显示。</summary>
        public bool HasVisible(UiLayer layer)
        {
            for (var i = 0; i < order.Count; i++)
            {
                var id = order[i];
                if (!cacheById.TryGetValue(id, out var module) || module == null || !module.gameObject.activeSelf) continue;
                if (layers.TryGetValue(id, out var value) && value == layer) return true;
            }

            return false;
        }

        /// <summary>销毁缓存实例，退出游戏或切换账号时调用。</summary>
        public void Clear()
        {
            foreach (var pair in cacheById)
            {
                if (pair.Value != null) Destroy(pair.Value.gameObject);
            }

            cacheById.Clear();
            idByPath.Clear();
            layers.Clear();
            order.Clear();
            paused.Clear();
            CurrentModuleId = null;
        }

        /// <summary>弹窗压住时暂停下层界面，弹窗关闭后恢复；只处理被压住的普通层。</summary>
        void ApplyPauseState()
        {
            var modalVisible = HasVisible(UiLayer.Modal);

            for (var i = 0; i < order.Count; i++)
            {
                var id = order[i];
                if (!cacheById.TryGetValue(id, out var module) || module == null || !module.gameObject.activeSelf) continue;
                if (!layers.TryGetValue(id, out var layer)) continue;

                var covered = modalVisible && layer < UiLayer.Modal;
                if (covered)
                {
                    if (paused.Add(id)) module.OnPause();
                }
                else if (paused.Remove(id))
                {
                    module.OnResume();
                }
            }
        }

        UiModule Acquire(string prefabPath, UiLayer layer)
        {
            if (idByPath.TryGetValue(prefabPath, out var knownId) &&
                cacheById.TryGetValue(knownId, out var cached) && cached != null) return cached;

            var prefab = Resources.Load<GameObject>(prefabPath);
            if (prefab == null)
            {
                Debug.LogError("[UiManager] 找不到界面 prefab：" + prefabPath);
                return null;
            }

            var instance = Instantiate(prefab, layerRoots[(int)layer], false);
            var module = instance.GetComponent<UiModule>();
            if (module == null)
            {
                Debug.LogError("[UiManager] prefab 根节点缺少 UiModule 组件：" + prefabPath);
                Destroy(instance);
                return null;
            }

            module.Bind(events, ui);
            cacheById[module.ModuleId] = module;
            idByPath[prefabPath] = module.ModuleId;
            return module;
        }

        void OnDestroy()
        {
            Clear();
        }
    }
}
