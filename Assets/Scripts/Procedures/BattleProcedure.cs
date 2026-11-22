using System.Collections.Generic;
using Gxdl.Core;
using Gxdl.Systems;
using UnityEngine;

namespace Gxdl.Procedures
{
    /// <summary>
    /// 战斗流程：驱动回合推进并在结束时结算。
    /// 流程只管节奏与状态，伤害计算在 GuxuBattle，表现由界面订阅事件。
    /// </summary>
    public sealed class BattleProcedure : ProcedureBase
    {
        readonly IBattleSystem battles;
        readonly IEventBus events;
        readonly ProcedureManager procedures;

        /// <summary>每回合推进间隔，按战斗倍速缩放。</summary>
        public float TurnInterval = 0.75f;

        float elapsed;

        public BattleProcedure(IBattleSystem battles, IEventBus events, ProcedureManager procedures)
        {
            this.battles = battles;
            this.events = events;
            this.procedures = procedures;
        }

        public override string Id => ProcedureIds.Battle;

        /// <summary>战斗倍速，1 至 3，由界面切换。</summary>
        public int Speed { get; private set; } = 1;

        /// <summary>是否在战斗结束后自动返回副本界面。</summary>
        public bool AutoReturn { get; set; } = true;

        /// <summary>暂停回合推进，用于展示结算动画或界面遮挡。</summary>
        public bool Paused { get; set; }

        public override bool CanEnter()
        {
            return battles.Active != null;
        }

        public override void OnEnter()
        {
            Debug.Log("[Procedure] 进入战斗");
            elapsed = 0f;
            events.Publish(new GameEvents.BattleTurnReady(null));
        }

        public override void OnLeave()
        {
            elapsed = 0f;
        }

        public override void OnUpdate(float deltaTime)
        {
            var active = battles.Active;
            if (active == null)
            {
                // 战斗实例被外部放弃，流程需要同步退回。
                procedures.Change(ProcedureIds.Dungeon);
                return;
            }

            if (Paused) return;

            if (active.Completed)
            {
                Finish();
                return;
            }

            elapsed += deltaTime * Mathf.Max(1, Speed);
            var interval = Mathf.Max(0.05f, TurnInterval);
            if (elapsed < interval) return;

            elapsed = 0f;
            var turnEvents = battles.Advance();
            events.Publish(new GameEvents.BattleTurnReady(turnEvents));
        }

        public void SetSpeed(int speed)
        {
            Speed = Mathf.Clamp(speed, 1, 3);
        }

        /// <summary>主动结算并返回副本。重复调用安全。</summary>
        public void Finish()
        {
            if (battles.Active == null) return;

            if (!battles.Settle(out var result))
            {
                battles.Abandon();
                procedures.Change(ProcedureIds.Dungeon);
                return;
            }

            events.Publish(new GameEvents.BattleSettled(result));

            // 战斗结束后回到副本界面，由玩家决定继续挑战还是回大厅。
            if (AutoReturn) procedures.Change(ProcedureIds.Dungeon);
        }
    }
}
