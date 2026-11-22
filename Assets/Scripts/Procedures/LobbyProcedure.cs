using Gxdl.Core;
using Gxdl.Systems;
using UnityEngine;

namespace Gxdl.Procedures
{
    /// <summary>
    /// 大厅流程：玩家日常活动的主界面。
    /// 进入这里意味着已完成登录与加载，可以打开各功能界面。
    /// </summary>
    public sealed class LobbyProcedure : ProcedureBase
    {
        readonly IEventBus events;
        readonly ProcedureManager procedures;
        readonly IAccountSystem accounts;

        public LobbyProcedure(IEventBus events, ProcedureManager procedures, IAccountSystem accounts)
        {
            this.events = events;
            this.procedures = procedures;
            this.accounts = accounts;
        }

        public override string Id => ProcedureIds.Lobby;

        public override bool CanEnter()
        {
            return accounts.IsLoggedIn;
        }

        public override void OnEnter()
        {
            Debug.Log("[Procedure] 进入大厅");
            events.Publish(new GameEvents.LobbyEntered());
        }

        public override void OnLeave()
        {
            events.Publish(new GameEvents.LobbyLeft());
        }

        /// <summary>请求进入副本选择。</summary>
        public bool RequestDungeon()
        {
            return procedures.Change(ProcedureIds.Dungeon);
        }
    }
}
