using System;
using Gxdl.Core;
using Gxdl.Systems;
using UnityEngine;

namespace Gxdl.Procedures
{
    /// <summary>
    /// 副本流程：关卡选择与出战准备。
    /// 选中关卡后进入战斗，退出则回到大厅。
    /// </summary>
    public sealed class DungeonProcedure : ProcedureBase
    {
        readonly IBattleSystem battles;
        readonly IConfigSystem configs;
        readonly IEventBus events;
        readonly ProcedureManager procedures;

        public DungeonProcedure(
            IBattleSystem battles,
            IConfigSystem configs,
            IEventBus events,
            ProcedureManager procedures)
        {
            this.battles = battles;
            this.configs = configs;
            this.events = events;
            this.procedures = procedures;
        }

        public override string Id => ProcedureIds.Dungeon;

        /// <summary>当前选中的关卡 id，0 表示尚未选择。</summary>
        public int SelectedStageId { get; private set; }

        public override void OnEnter()
        {
            Debug.Log("[Procedure] 进入副本");
            SelectedStageId = 0;
            events.Publish(new GameEvents.DungeonEntered());
        }

        public override void OnLeave()
        {
            SelectedStageId = 0;
            events.Publish(new GameEvents.DungeonLeft());
        }

        /// <summary>选中一个关卡。返回是否可进入，并可取到拒绝原因。</summary>
        public bool Select(GuxuStage stage, out string reason)
        {
            if (!battles.CanEnter(stage, out reason)) return false;
            SelectedStageId = stage.id;
            events.Publish(new GameEvents.StageSelected(stage.id));
            return true;
        }

        /// <summary>以当前选中关卡开战。失败原因写入 <see cref="LastReason"/>。</summary>
        public bool StartBattle(int seed, out string reason)
        {
            reason = null;
            GuxuStage stage = null;
            if (SelectedStageId > 0)
            {
                var all = configs.Stages;
                stage = FindStage(all, SelectedStageId);
            }

            if (stage == null)
            {
                reason = "未选择关卡";
                return false;
            }

            if (!battles.CanEnter(stage, out reason)) return false;

            try
            {
                battles.Start(stage, seed);
            }
            catch (InvalidOperationException error)
            {
                reason = error.Message;
                return false;
            }

            reason = null;
            return procedures.Change(ProcedureIds.Battle);
        }

        /// <summary>最近一次的失败原因，界面用于提示。</summary>
        public string LastReason { get; private set; }

        public bool ReturnToLobby()
        {
            return procedures.Change(ProcedureIds.Lobby);
        }

        static GuxuStage FindStage(GuxuStageConfig config, int stageId)
        {
            var groups = new[] { config.stages, config.dungeons, config.tower };
            for (var g = 0; g < groups.Length; g++)
            {
                var list = groups[g];
                if (list == null) continue;
                for (var i = 0; i < list.Length; i++)
                {
                    if (list[i] != null && list[i].id == stageId) return list[i];
                }
            }

            return null;
        }
    }
}
