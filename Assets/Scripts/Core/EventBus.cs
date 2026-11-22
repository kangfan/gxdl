using System;
using System.Collections.Generic;

namespace Gxdl.Core
{
    /// <summary>
    /// 类型化事件总线。系统之间通过事件解耦，不直接互相引用。
    /// 订阅返回 <see cref="IDisposable"/>，离开作用域即自动退订；
    /// 界面在销毁时必须退订，否则会被总线持有导致泄漏。
    /// </summary>
    public interface IEventBus
    {
        IDisposable Subscribe<TEvent>(Action<TEvent> handler) where TEvent : struct;

        void Unsubscribe<TEvent>(Action<TEvent> handler) where TEvent : struct;

        void Publish<TEvent>(TEvent payload) where TEvent : struct;

        void Clear();
    }

    public sealed class EventBus : IEventBus
    {
        readonly Dictionary<Type, Delegate> handlers = new Dictionary<Type, Delegate>();

        public IDisposable Subscribe<TEvent>(Action<TEvent> handler) where TEvent : struct
        {
            if (handler == null) throw new ArgumentNullException(nameof(handler));
            var key = typeof(TEvent);
            handlers[key] = handlers.TryGetValue(key, out var existing)
                ? Delegate.Combine(existing, handler)
                : handler;
            return new Subscription<TEvent>(this, handler);
        }

        public void Unsubscribe<TEvent>(Action<TEvent> handler) where TEvent : struct
        {
            if (handler == null) return;
            var key = typeof(TEvent);
            if (!handlers.TryGetValue(key, out var existing)) return;

            var remaining = Delegate.Remove(existing, handler);
            if (remaining == null) handlers.Remove(key);
            else handlers[key] = remaining;
        }

        public void Publish<TEvent>(TEvent payload) where TEvent : struct
        {
            if (!handlers.TryGetValue(typeof(TEvent), out var existing)) return;

            // 复制一份快照，允许处理器在回调里退订或新增订阅而不破坏遍历。
            var snapshot = existing.GetInvocationList();
            for (var i = 0; i < snapshot.Length; i++)
            {
                try
                {
                    ((Action<TEvent>)snapshot[i]).Invoke(payload);
                }
                catch (Exception error)
                {
                    UnityEngine.Debug.LogException(error);
                }
            }
        }

        public void Clear()
        {
            handlers.Clear();
        }

        sealed class Subscription<TEvent> : IDisposable where TEvent : struct
        {
            EventBus bus;
            Action<TEvent> handler;

            public Subscription(EventBus bus, Action<TEvent> handler)
            {
                this.bus = bus;
                this.handler = handler;
            }

            public void Dispose()
            {
                if (bus == null) return;
                bus.Unsubscribe(handler);
                bus = null;
                handler = null;
            }
        }
    }
}
