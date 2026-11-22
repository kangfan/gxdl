using System;
using Gxdl.Core;
using UnityEngine;

namespace Gxdl.Modules
{
    /// <summary>
    /// 界面模块基类。每个模块对应一张 prefab，
    /// 职责只有三件事：绑定事件、响应事件刷新视图、销毁时解绑。
    /// 模块不直接操作存档或其他系统，业务动作一律通过 Controller。
    /// </summary>
    public abstract class UiModule : MonoBehaviour
    {
        readonly System.Collections.Generic.List<IDisposable> subscriptions =
            new System.Collections.Generic.List<IDisposable>();

        /// <summary>模块唯一标识，用于流程与调试定位。</summary>
        public abstract string ModuleId { get; }

        protected IEventBus Events { get; private set; }

        protected UiFactory Ui { get; private set; }

        protected bool IsBound { get; private set; }

        /// <summary>由框架在实例化 prefab 后调用，完成依赖注入。</summary>
        public void Bind(IEventBus events, UiFactory ui)
        {
            if (IsBound) return;
            Events = events;
            Ui = ui;
            IsBound = true;
            OnBind();
        }

        /// <summary>订阅事件并返回自动释放句柄，模块销毁时统一释放。</summary>
        protected IDisposable Track<TEvent>(Action<TEvent> handler) where TEvent : struct
        {
            if (Events == null) throw new InvalidOperationException("事件总线尚未注入，请先调用 Bind");
            var subscription = Events.Subscribe(handler);
            subscriptions.Add(subscription);
            return subscription;
        }

        /// <summary>子类在此订阅事件、初始化控件状态。</summary>
        protected virtual void OnBind()
        {
        }

        /// <summary>子类在此释放事件以外的资源。</summary>
        protected virtual void OnUnbind()
        {
        }

        /// <summary>模块显示时调用，用于刷新数据。</summary>
        public virtual void OnShow()
        {
        }

        /// <summary>模块隐藏时调用，用于停止动画与计时。</summary>
        public virtual void OnHide()
        {
        }

        void OnDestroy()
        {
            OnUnbind();
            for (var i = 0; i < subscriptions.Count; i++)
            {
                try { subscriptions[i].Dispose(); }
                catch (Exception error) { Debug.LogException(error); }
            }

            subscriptions.Clear();
            IsBound = false;
        }
    }
}
