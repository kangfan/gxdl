using System;
using Gxdl.Core;

namespace Gxdl.Systems
{
    public interface IEquipmentSystem
    {
        /// <summary>装备某个槽位当前穿戴的装备。</summary>
        Gear Equipped(int slot);

        /// <summary>当前待处理的掉落装备，没有则为 null。</summary>
        Gear Pending { get; }

        /// <summary>破封：消耗一个破封符产出待处理装备。</summary>
        bool Unseal(out string failureReason);

        /// <summary>结算待处理装备：true 替换，false 分解。</summary>
        bool Resolve(bool replace, out int coinReward, out int powerDelta);
    }

    /// <summary>
    /// 装备系统：破封、替换、分解。
    /// 原先这些逻辑散落在 GxdlGame.Chop / ResolveDrop 里，同时混了动画与提示，
    /// 现在把「状态变更」与「表现」彻底分开，表现改由界面订阅事件完成。
    /// </summary>
    public sealed class EquipmentSystem : IEquipmentSystem
    {
        readonly IEventBus events;
        readonly ISaveSystem saves;
        GameRules rules;

        public EquipmentSystem(IEventBus events, ISaveSystem saves)
        {
            this.events = events ?? throw new ArgumentNullException(nameof(events));
            this.saves = saves ?? throw new ArgumentNullException(nameof(saves));
        }

        public void Bind(GameRules instance)
        {
            rules = instance ?? throw new ArgumentNullException(nameof(instance));
        }

        public Gear Pending => rules != null ? rules.Save.pending : null;

        public Gear Equipped(int slot)
        {
            if (rules == null || slot < 0 || slot >= 12) return null;
            return rules.Save.gear[slot];
        }

        public bool Unseal(out string failureReason)
        {
            failureReason = null;
            if (rules == null) { failureReason = "尚未进入游戏"; return false; }
            if (rules.Save.pending != null) { failureReason = "请先装备或分解当前遗宝"; return false; }
            if (rules.Save.peaches <= 0) { failureReason = "破封符不足，完成冒险可获得破封符"; return false; }
            if (!rules.Chop()) { failureReason = "破封失败"; return false; }

            saves.Store(rules.Save);
            events.Publish(new GameEvents.EquipmentChanged(rules.Save.pending != null ? rules.Save.pending.slot : -1));
            return true;
        }

        public bool Resolve(bool replace, out int coinReward, out int powerDelta)
        {
            coinReward = 0;
            powerDelta = 0;
            if (rules == null || rules.Save.pending == null) return false;

            var coinsBefore = rules.Save.coins;
            var powerBefore = rules.Attributes.Power;
            var slot = rules.Save.pending.slot;

            if (!rules.Resolve(replace)) return false;

            coinReward = rules.Save.coins - coinsBefore;
            powerDelta = rules.Attributes.Power - powerBefore;

            saves.Store(rules.Save);
            events.Publish(new GameEvents.EquipmentChanged(slot));
            if (powerDelta != 0) events.Publish(new GameEvents.PlayerStatsChanged(rules.Attributes.Power));
            return true;
        }
    }
}
