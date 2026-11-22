using System;
using Gxdl.Core;

namespace Gxdl.Systems
{
    public interface IGrowthSystem
    {
        /// <summary>当前玩家可用的成长规则实例，所有公式委托给它。</summary>
        GameRules Rules { get; }

        /// <summary>绑定到某个存档对应的规则实例。</summary>
        void Bind(GameRules rules);

        /// <summary>校验成长解锁条件，供界面决定入口是否可点。</summary>
        bool IsUnlocked(GrowthGroup group);

        /// <summary>
        /// 执行一次成长操作并广播结果，返回是否成功。
        /// 参数含义按操作而定：锻造/天赋取 <paramref name="first"/>（槽位或节点），
        /// 镶宝石需要槽位、孔位、背包索引三项。
        /// </summary>
        bool Execute(GrowthAction action, int first, int second = -1, int third = -1);
    }

    public enum GrowthGroup
    {
        Forge = 0,
        Talent = 1,
        Gem = 2
    }

    public enum GrowthAction
    {
        Forge,
        TrainTalent,
        ResetTalents,
        SocketGem,
        UnsocketGem,
        CombineGem,
        UpgradeTree,
        Breakthrough
    }

    /// <summary>
    /// 成长系统：锻造、天赋、宝石、渡劫等。
    /// 规则实现仍在 <see cref="GameRules"/> 的成长分部里，
    /// 这一层只做参数校验、状态广播与存档落盘，避免界面直接调规则并忘记存档。
    /// </summary>
    public sealed class GrowthSystem : IGrowthSystem
    {
        readonly IEventBus events;
        readonly ISaveSystem saves;
        GameRules rules;

        public GrowthSystem(IEventBus events, ISaveSystem saves)
        {
            this.events = events ?? throw new ArgumentNullException(nameof(events));
            this.saves = saves ?? throw new ArgumentNullException(nameof(saves));
        }

        public GameRules Rules =>
            rules ?? throw new InvalidOperationException("成长系统尚未绑定规则实例，请检查登录流程");

        public void Bind(GameRules instance)
        {
            rules = instance ?? throw new ArgumentNullException(nameof(instance));
        }

        public bool IsUnlocked(GrowthGroup group)
        {
            if (rules == null) return false;
            return rules.GrowthUnlocked((int)group);
        }

        public bool Execute(GrowthAction action, int first, int second = -1, int third = -1)
        {
            if (rules == null) return false;

            var changed = false;
            var powerBefore = rules.Attributes.Power;

            switch (action)
            {
                case GrowthAction.Forge:
                    changed = rules.Forge(first);
                    break;
                case GrowthAction.TrainTalent:
                    changed = rules.TrainTalent(first);
                    break;
                case GrowthAction.ResetTalents:
                    rules.ResetTalents();
                    changed = true;
                    break;
                case GrowthAction.SocketGem:
                    changed = rules.SocketGem(first, second, third);
                    break;
                case GrowthAction.UnsocketGem:
                    changed = rules.UnsocketGem(first, second);
                    break;
                case GrowthAction.CombineGem:
                    changed = rules.CombineGem(first);
                    break;
                case GrowthAction.UpgradeTree:
                    changed = rules.UpgradeTree();
                    break;
                case GrowthAction.Breakthrough:
                    changed = rules.Breakthrough();
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(action), action, "未处理的成长操作");
            }

            if (!changed) return false;

            saves.Store(rules.Save);
            var powerAfter = rules.Attributes.Power;
            if (powerAfter != powerBefore) events.Publish(new GameEvents.PlayerStatsChanged(powerAfter));
            return true;
        }
    }
}
