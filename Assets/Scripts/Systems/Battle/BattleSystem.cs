using System;
using Gxdl.Core;

namespace Gxdl.Systems
{
    public interface IBattleSystem
    {
        /// <summary>当前进行中的战斗，未开战时为 null。</summary>
        GuxuBattle Active { get; }

        /// <summary>校验关卡是否可进入，并给出拒绝原因。</summary>
        bool CanEnter(GuxuStage stage, out string reason);

        /// <summary>开启一场战斗。失败时抛出说明性异常，不启动半成品状态。</summary>
        GuxuBattle Start(GuxuStage stage, int seed);

        /// <summary>推进一个回合，返回本回合产生的事件供表现层消费。</summary>
        System.Collections.Generic.List<GuxuBattleEvent> Advance();

        /// <summary>结算当前战斗。返回是否成功写入奖励。</summary>
        bool Settle(out BattleResult result);

        /// <summary>放弃当前战斗，不结算。</summary>
        void Abandon();
    }

    public readonly struct BattleResult
    {
        public readonly bool Won;
        public readonly int StageId;
        public readonly int SealReward;
        public readonly int DustReward;

        public BattleResult(bool won, int stageId, int sealReward, int dustReward)
        {
            Won = won;
            StageId = stageId;
            SealReward = sealReward;
            DustReward = dustReward;
        }
    }

    /// <summary>
    /// 战斗系统：把关卡进入校验、战斗实例生命周期、胜利结算收口到一处。
    /// 战斗推演本身由 <see cref="GuxuBattle"/> 完成，这里不重复实现任何伤害公式。
    /// 表现层只订阅结果，不再直接改存档。
    /// </summary>
    public sealed class BattleSystem : IBattleSystem
    {
        readonly IEventBus events;
        readonly ISaveSystem saves;
        readonly IConfigSystem configs;
        GameRules rules;

        public BattleSystem(IEventBus events, ISaveSystem saves, IConfigSystem configs)
        {
            this.events = events ?? throw new ArgumentNullException(nameof(events));
            this.saves = saves ?? throw new ArgumentNullException(nameof(saves));
            this.configs = configs ?? throw new ArgumentNullException(nameof(configs));
        }

        public void Bind(GameRules instance)
        {
            rules = instance ?? throw new ArgumentNullException(nameof(instance));
        }

        public GuxuBattle Active { get; private set; }

        public bool CanEnter(GuxuStage stage, out string reason)
        {
            reason = null;
            if (rules == null) { reason = "尚未进入游戏"; return false; }
            if (stage == null) { reason = "关卡不存在"; return false; }

            if (!GuxuStageRules.CanEnter(stage, rules.Save))
            {
                reason = "该挑战尚未解锁";
                return false;
            }

            // 秘境有每日收益次数上限，与主线共用进入校验会漏掉这条业务规则。
            if (stage.mode == 1 && stage.contentId >= 0 && stage.contentId < rules.Save.dungeonClaims.Length &&
                rules.Save.dungeonClaims[stage.contentId] >= 3)
            {
                reason = "今日收益次数已用尽，明日再来";
                return false;
            }

            return true;
        }

        public GuxuBattle Start(GuxuStage stage, int seed)
        {
            if (!CanEnter(stage, out var reason)) throw new InvalidOperationException(reason);
            if (Active != null) throw new InvalidOperationException("已有战斗在进行中，请先结算或放弃");

            rules.RefreshDaily(DateTime.UtcNow);
            Active = GuxuStageRules.Begin(rules, stage, seed);
            events.Publish(new GameEvents.BattleStarted(stage.id));
            return Active;
        }

        public System.Collections.Generic.List<GuxuBattleEvent> Advance()
        {
            if (Active == null) return null;
            return Active.NextTurn();
        }

        public bool Settle(out BattleResult result)
        {
            if (Active == null)
            {
                result = default;
                return false;
            }

            var battle = Active;
            var won = battle.Won;
            var sealReward = 0;
            var dustReward = 0;

            if (won && !battle.Settled)
            {
                var stageCount = configs.Stages.stages.Length;
                if (GuxuStageRules.TryApplyVictory(rules.Save, battle, stageCount))
                {
                    sealReward = battle.Stage.sealReward;
                    dustReward = battle.Stage.dustReward;
                }
            }

            saves.Store(rules.Save);
            result = new BattleResult(won, battle.Stage.id, sealReward, dustReward);
            events.Publish(new GameEvents.BattleEnded(battle.Stage.id, won));
            Active = null;
            return true;
        }

        public void Abandon()
        {
            if (Active == null) return;
            var stageId = Active.Stage.id;
            Active = null;
            events.Publish(new GameEvents.BattleEnded(stageId, false));
        }
    }
}
