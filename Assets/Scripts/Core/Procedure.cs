using System;
using System.Collections.Generic;

namespace Gxdl.Core
{
    /// <summary>
    /// 一段游戏流程（登录、Loading、大厅、副本、战斗等）。
    /// 生命周期由 <see cref="ProcedureManager"/> 统一驱动，
    /// 各流程不自行切换，切换请求交给管理器裁决。
    /// </summary>
    public interface IProcedure
    {
        /// <summary>流程内唯一的稳定标识，用于迁移与断言。</summary>
        string Id { get; }

        /// <summary>进入该流程的前置条件。不满足时管理器会拒绝切换。</summary>
        bool CanEnter();

        void OnEnter();

        void OnUpdate(float deltaTime);

        void OnLeave();
    }

    /// <summary>
    /// 流程基类：提供默认空实现，子类只需覆盖实际关心的阶段。
    /// </summary>
    public abstract class ProcedureBase : IProcedure
    {
        public abstract string Id { get; }

        public virtual bool CanEnter()
        {
            return true;
        }

        public virtual void OnEnter()
        {
        }

        public virtual void OnUpdate(float deltaTime)
        {
        }

        public virtual void OnLeave()
        {
        }

        public override string ToString()
        {
            return Id;
        }
    }

    /// <summary>
    /// 流程流转事件，供界面与调试工具观察，不参与流程控制。
    /// </summary>
    public readonly struct ProcedureChangedEvent
    {
        public readonly string From;
        public readonly string To;

        public ProcedureChangedEvent(string from, string to)
        {
            From = from;
            To = to;
        }
    }

    public sealed class ProcedureManager
    {
        readonly Dictionary<string, IProcedure> procedures = new Dictionary<string, IProcedure>();
        readonly IEventBus events;
        readonly List<string> history = new List<string>();
        bool switching;

        public ProcedureManager(IEventBus events)
        {
            this.events = events ?? throw new ArgumentNullException(nameof(events));
        }

        public IProcedure Current { get; private set; }

        public string CurrentId => Current != null ? Current.Id : string.Empty;

        public IReadOnlyList<string> History => history;

        /// <summary>已注册的全部流程标识，便于自测与调试。</summary>
        public IEnumerable<string> RegisteredIds => procedures.Keys;

        public void Register(IProcedure procedure)
        {
            if (procedure == null) throw new ArgumentNullException(nameof(procedure));
            if (string.IsNullOrEmpty(procedure.Id))
            {
                throw new ArgumentException("流程必须提供非空 Id", nameof(procedure));
            }

            if (procedures.ContainsKey(procedure.Id))
            {
                throw new InvalidOperationException($"流程 {procedure.Id} 已注册");
            }

            procedures.Add(procedure.Id, procedure);
        }

        public IProcedure Get(string id)
        {
            if (id == null || !procedures.TryGetValue(id, out var procedure))
            {
                throw new InvalidOperationException($"流程 {id} 未注册");
            }

            return procedure;
        }

        public bool TryGet(string id, out IProcedure procedure)
        {
            return procedures.TryGetValue(id, out procedure);
        }

        public bool Change(string id)
        {
            var next = Get(id);

            if (Current != null && ReferenceEquals(Current, next))
            {
                return true;
            }

            if (!next.CanEnter())
            {
                UnityEngine.Debug.LogWarning($"[Procedure] 拒绝进入 {id}：CanEnter 未通过");
                return false;
            }

            // 防止流程在 OnLeave/OnEnter 里再次触发切换导致递归。
            if (switching)
            {
                throw new InvalidOperationException(
                    $"检测到流程切换重入：正在从 {CurrentId} 切换到 {id}，流程内不得再次请求切换");
            }

            switching = true;
            var previous = Current;
            try
            {
                previous?.OnLeave();
                Current = next;
                history.Add(next.Id);
                events.Publish(new ProcedureChangedEvent(previous?.Id ?? string.Empty, next.Id));
                next.OnEnter();
            }
            finally
            {
                switching = false;
            }

            return true;
        }

        public void Update(float deltaTime)
        {
            Current?.OnUpdate(deltaTime);
        }

        public void Clear()
        {
            Current?.OnLeave();
            Current = null;
            procedures.Clear();
            history.Clear();
            switching = false;
        }
    }
}
