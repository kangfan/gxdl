using System;
using Gxdl.Core;
using Gxdl.Systems;
using UnityEngine;

namespace Gxdl.Procedures
{
    /// <summary>
    /// 加载流程：读取配置与存档，构建规则实例，准备大厅。
    /// 进度以 0..1 单调递增上报，界面只负责显示，不得自行推进进度。
    /// </summary>
    public sealed class LoadingProcedure : ProcedureBase
    {
        readonly IAccountSystem accounts;
        readonly IConfigSystem configs;
        readonly ISaveSystem saves;
        readonly IEventBus events;
        readonly ProcedureManager procedures;
        readonly Action<GameRules> onRulesReady;

        public LoadingProcedure(
            IAccountSystem accounts,
            IConfigSystem configs,
            ISaveSystem saves,
            IEventBus events,
            ProcedureManager procedures,
            Action<GameRules> onRulesReady)
        {
            this.accounts = accounts;
            this.configs = configs;
            this.saves = saves;
            this.events = events;
            this.procedures = procedures;
            this.onRulesReady = onRulesReady;
        }

        public override string Id => ProcedureIds.Loading;

        public bool HasFailed { get; private set; }

        public string FailureMessage { get; private set; }

        public float Progress { get; private set; }

        public override bool CanEnter()
        {
            return accounts.IsLoggedIn;
        }

        public override void OnEnter()
        {
            HasFailed = false;
            FailureMessage = null;
            Progress = 0f;
            Report(0f, "正在读取游戏配置……");

            try
            {
                if (!configs.Validate(out var configError))
                {
                    Fail("配置校验失败：" + configError);
                    return;
                }

                Report(0.3f, "正在校验关卡数据……");

                saves.UseScope(accounts.SaveScope);
                var save = saves.Load(accounts.SaveScope);
                var rules = new GameRules(configs.Game, save);
                onRulesReady?.Invoke(rules);

                Report(0.75f, "正在准备冒险……");
                Report(1f, "准备完毕");
            }
            catch (Exception error)
            {
                Fail(error.Message);
            }
        }

        public override void OnUpdate(float deltaTime)
        {
            if (HasFailed) return;
            if (Progress < 1f) return;
            procedures.Change(ProcedureIds.Lobby);
        }

        void Report(float progress, string message)
        {
            Progress = Mathf.Clamp01(progress);
            events.Publish(new GameEvents.LoadingProgressChanged(Progress, message));
        }

        void Fail(string reason)
        {
            HasFailed = true;
            FailureMessage = reason;
            Debug.LogError("[Procedure] 加载失败：" + reason);
            events.Publish(new GameEvents.LoadingFailed(reason));
        }
    }
}
