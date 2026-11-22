using System;
using Gxdl.Core;

namespace Gxdl.Systems
{
    public interface IAccountSystem
    {
        /// <summary>当前账号存档作用域，空字符串代表编辑器本地预览。</summary>
        string SaveScope { get; }

        /// <summary>展示用昵称。</summary>
        string DisplayName { get; }

        /// <summary>是否已通过登录校验，可以进入游戏流程。</summary>
        bool IsLoggedIn { get; }

        /// <summary>是否处于忙碌状态（SDK 初始化、认证中）。</summary>
        bool IsBusy { get; }

        /// <summary>账号状态变化通知。</summary>
        event Action Changed;

        /// <summary>注销并清理当前会话。</summary>
        void Logout();
    }

    /// <summary>
    /// 账号系统。真正的 TapTap SDK 交互仍留在 <see cref="GuxuAccountSession"/>，
    /// 这里是面向游戏逻辑的稳定接口，隔离第三方 SDK 与运行时框架。
    /// 这样后续替换登录渠道不会影响流程层与界面层。
    /// </summary>
    public sealed class AccountSystem : IAccountSystem
    {
        readonly IEventBus events;
        GuxuAccountSession session;

        public AccountSystem(IEventBus events)
        {
            this.events = events ?? throw new ArgumentNullException(nameof(events));
        }

        /// <summary>绑定场景里的账号会话组件，由组合根在启动时调用。</summary>
        public void Bind(GuxuAccountSession accountSession)
        {
            if (session != null) session.Changed -= HandleChanged;
            session = accountSession;
            if (session != null) session.Changed += HandleChanged;
            HandleChanged();
        }

        public string SaveScope => session != null ? session.SaveScope : string.Empty;

        public string DisplayName => session != null ? session.DisplayName : string.Empty;

        public bool IsLoggedIn => session != null && session.CanPlay;

        public bool IsBusy => session != null && session.IsBusy;

        public event Action Changed;

        public void Logout()
        {
            if (session == null) return;
            session.Logout();
            HandleChanged();
        }

        void HandleChanged()
        {
            events.Publish(new GameEvents.AccountChanged(SaveScope, IsLoggedIn));
            Changed?.Invoke();
        }
    }
}
