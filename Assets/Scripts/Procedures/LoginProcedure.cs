using Gxdl.Core;
using UnityEngine;
using Gxdl.Systems;

namespace Gxdl.Procedures
{
    /// <summary>
    /// 流程标识常量。集中定义避免字符串拼写错误导致流程找不到。
    /// </summary>
    public static class ProcedureIds
    {
        public const string Login = "Login";
        public const string Loading = "Loading";
        public const string Lobby = "Lobby";
        public const string Dungeon = "Dungeon";
        public const string Battle = "Battle";
    }

    /// <summary>
    /// 登录流程：等待账号认证通过。
    /// 编辑器下的预览登录也走这里，只是认证实现不同，
    /// 流程本身不感知登录渠道。
    /// </summary>
    public sealed class LoginProcedure : ProcedureBase
    {
        readonly IAccountSystem accounts;
        readonly ProcedureManager procedures;

        public LoginProcedure(IAccountSystem accounts, ProcedureManager procedures)
        {
            this.accounts = accounts;
            this.procedures = procedures;
        }

        public override string Id => ProcedureIds.Login;

        public override void OnEnter()
        {
            Debug.Log("[Procedure] 进入登录流程");
            accounts.Changed += HandleAccountChanged;
            HandleAccountChanged();
        }

        public override void OnLeave()
        {
            accounts.Changed -= HandleAccountChanged;
        }

        public override void OnUpdate(float deltaTime)
        {
            // 账号认证是异步的，由事件驱动切换；这里只做兜底轮询，
            // 应对事件在流程切换瞬间丢失的边界情况。
            if (accounts.IsLoggedIn) HandleAccountChanged();
        }

        void HandleAccountChanged()
        {
            if (!accounts.IsLoggedIn) return;
            procedures.Change(ProcedureIds.Loading);
        }
    }
}
