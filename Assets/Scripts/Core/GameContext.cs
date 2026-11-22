using System;

namespace Gxdl.Core
{
    /// <summary>
    /// 服务定位器。所有系统在 <see cref="GameBootstrap"/> 里注册一次，
    /// 全局通过接口取用，避免 static Instance 造成的隐式耦合。
    /// 未注册的服务在取用时立即失败，而不是返回 null 静默降级。
    /// </summary>
    public static class GameContext
    {
        static readonly ServiceRegistry Services = new ServiceRegistry();

        public static void Register<T>(T service) where T : class
        {
            Services.Register(service);
        }

        public static void Register(Type contract, object service)
        {
            Services.Register(contract, service);
        }

        public static T Get<T>() where T : class
        {
            return Services.Get<T>();
        }

        /// <summary>
        /// 尝试取用服务。只吞掉「未注册」，其余配置错误（例如多实现歧义）
        /// 仍然向上抛出，避免把初始化错误伪装成可选服务缺失。
        /// </summary>
        public static bool TryGet<T>(out T service) where T : class
        {
            if (!Services.Has<T>())
            {
                service = null;
                return false;
            }

            service = Services.Get<T>();
            return true;
        }

        public static bool Has<T>() where T : class
        {
            return Services.Has<T>();
        }

        public static void Unregister<T>() where T : class
        {
            Services.Unregister<T>();
        }

        /// <summary>
        /// 清空全部注册。退出播放模式或切换测试夹具时调用，防止跨会话残留。
        /// </summary>
        public static void Reset()
        {
            Services.Clear();
        }
    }
}
