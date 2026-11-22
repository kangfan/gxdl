using System;
using System.Collections.Generic;
using Gxdl.Core;

namespace Gxdl.Systems
{
    public enum SkillEffect
    {
        /// <summary>按攻击力百分比造成伤害。</summary>
        Damage = 0,

        /// <summary>按最大生命值百分比获得护盾。</summary>
        Shield = 1,

        /// <summary>按最大生命值百分比治疗。</summary>
        Heal = 2,

        /// <summary>伤害并附加破甲。</summary>
        ArmorBreak = 3
    }

    /// <summary>
    /// 技能定义。与现有 GameRules.SkillNames 一一对应，
    /// 但把硬编码常量变成可配置数据，后续新增技能不需要改战斗代码。
    /// </summary>
    [Serializable]
    public sealed class SkillDefinition
    {
        public string id;
        public string displayName;

        /// <summary>技能效果文案，界面直接显示，不在此处拼接数值。</summary>
        public string description;

        public SkillEffect effect;

        /// <summary>效果强度，按 <see cref="effect"/> 解释为攻击力或生命值百分比。</summary>
        public int magnitudePercent;

        public int cooldownTurns = 3;

        /// <summary>附加效果的持续回合数，0 表示无持续效果。</summary>
        public int durationTurns;
    }

    public interface ISkillSystem
    {
        IReadOnlyList<SkillDefinition> Definitions { get; }

        void Load(IEnumerable<SkillDefinition> definitions);

        SkillDefinition Get(string skillId);

        /// <summary>从现有 GameRules 常量生成默认技能表，保证与既有战斗一致。</summary>
        void LoadDefaults();

        /// <summary>取角色可用技能列表。</summary>
        IReadOnlyList<SkillDefinition> ForRole(string roleId);
    }

    /// <summary>
    /// 技能系统骨架：技能定义表与查询。
    /// 战斗结算目前仍由 GuxuBattle 按常量执行，
    /// 后续把常量替换为这里的定义即可完成数据驱动，不影响调用方。
    /// </summary>
    public sealed class SkillSystem : ISkillSystem
    {
        readonly IEventBus events;
        readonly List<SkillDefinition> definitions = new List<SkillDefinition>();

        public SkillSystem(IEventBus events)
        {
            this.events = events ?? throw new ArgumentNullException(nameof(events));
        }

        public IReadOnlyList<SkillDefinition> Definitions => definitions;

        public void Load(IEnumerable<SkillDefinition> source)
        {
            definitions.Clear();
            if (source == null) return;

            foreach (var definition in source)
            {
                if (definition == null || string.IsNullOrEmpty(definition.id)) continue;
                if (Get(definition.id) != null)
                {
                    UnityEngine.Debug.LogWarning("[Skill] 技能 id 重复，已忽略：" + definition.id);
                    continue;
                }

                definitions.Add(definition);
            }

            events.Publish(new GameEvents.SkillsLoaded(definitions.Count));
        }

        public SkillDefinition Get(string skillId)
        {
            if (string.IsNullOrEmpty(skillId)) return null;
            for (var i = 0; i < definitions.Count; i++)
            {
                if (definitions[i].id == skillId) return definitions[i];
            }

            return null;
        }

        /// <summary>
        /// 用现有规则常量生成默认表。
        /// 数值来源是 GameRules 上的常量，确保骨架阶段与线上行为完全一致。
        /// </summary>
        public void LoadDefaults()
        {
            var defaults = new List<SkillDefinition>
            {
                new SkillDefinition
                {
                    id = "sword",
                    displayName = GameRules.SkillNames.Length > 0 ? GameRules.SkillNames[0] : "御剑诀",
                    effect = SkillEffect.Damage,
                    magnitudePercent = GameRules.SwordDamagePercent,
                    cooldownTurns = GameRules.SkillCooldownTurns
                },
                new SkillDefinition
                {
                    id = "shield",
                    displayName = GameRules.SkillNames.Length > 1 ? GameRules.SkillNames[1] : "玄甲盾",
                    effect = SkillEffect.Shield,
                    magnitudePercent = GameRules.ShieldHpPercent,
                    cooldownTurns = GameRules.SkillCooldownTurns
                },
                new SkillDefinition
                {
                    id = "heal",
                    displayName = GameRules.SkillNames.Length > 2 ? GameRules.SkillNames[2] : "回春术",
                    effect = SkillEffect.Heal,
                    magnitudePercent = GameRules.HealHpPercent,
                    cooldownTurns = GameRules.SkillCooldownTurns
                },
                new SkillDefinition
                {
                    id = "break",
                    displayName = GameRules.SkillNames.Length > 3 ? GameRules.SkillNames[3] : "碎甲剑",
                    effect = SkillEffect.ArmorBreak,
                    magnitudePercent = GameRules.BreakSwordDamagePercent,
                    cooldownTurns = GameRules.SkillCooldownTurns,
                    durationTurns = GameRules.ArmorBreakTurns
                }
            };

            for (var i = 0; i < defaults.Count; i++)
            {
                if (GameRules.SkillDetails.Length > i) defaults[i].description = GameRules.SkillDetails[i];
            }

            Load(defaults);
        }

        public IReadOnlyList<SkillDefinition> ForRole(string roleId)
        {
            // 骨架阶段：所有角色共用全部技能，后续按角色筛选。
            return definitions;
        }
    }
}
