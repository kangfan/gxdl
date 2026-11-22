using System;
using System.Collections.Generic;

namespace Gxdl.Core
{
    /// <summary>
    /// 接口契约到实例的映射。支持「以接口注册、以接口取用」，
    /// 也支持以具体类型注册后按接口解析（向上查找已注册类型的兼容关系）。
    /// </summary>
    internal sealed class ServiceRegistry
    {
        readonly Dictionary<Type, object> services = new Dictionary<Type, object>();

        internal void Register<T>(T service) where T : class
        {
            if (service == null) throw new ArgumentNullException(nameof(service));
            Register(typeof(T), service);
        }

        internal void Register(Type contract, object service)
        {
            if (contract == null) throw new ArgumentNullException(nameof(contract));
            if (service == null) throw new ArgumentNullException(nameof(service));
            if (!contract.IsInstanceOfType(service))
            {
                throw new ArgumentException(
                    $"服务实例 {service.GetType().FullName} 不实现契约 {contract.FullName}", nameof(service));
            }

            if (services.ContainsKey(contract))
            {
                throw new InvalidOperationException(
                    $"服务 {contract.FullName} 已注册，重复注册通常意味着初始化顺序有误");
            }

            services.Add(contract, service);
        }

        internal T Get<T>() where T : class
        {
            if (services.TryGetValue(typeof(T), out var known)) return (T)known;

            // 允许按接口取用：查找唯一一个实现了该契约的已注册实例。
            Type match = null;
            object instance = null;
            foreach (var pair in services)
            {
                if (!typeof(T).IsAssignableFrom(pair.Key)) continue;
                if (match != null)
                {
                    throw new InvalidOperationException(
                        $"服务 {typeof(T).FullName} 有多个候选实现（{match.FullName}、{pair.Key.FullName}），请按具体类型取用");
                }

                match = pair.Key;
                instance = pair.Value;
            }

            if (match == null)
            {
                throw new InvalidOperationException(
                    $"服务 {typeof(T).FullName} 未注册，请检查 GameBootstrap 的初始化顺序");
            }

            return (T)instance;
        }

        internal bool TryGet<T>(out T service) where T : class
        {
            try
            {
                service = Get<T>();
                return true;
            }
            catch (InvalidOperationException)
            {
                service = null;
                return false;
            }
        }

        internal bool Has<T>() where T : class
        {
            return TryGet<T>(out _);
        }

        internal void Unregister<T>() where T : class
        {
            services.Remove(typeof(T));
        }

        internal void Clear()
        {
            services.Clear();
        }
    }
}
