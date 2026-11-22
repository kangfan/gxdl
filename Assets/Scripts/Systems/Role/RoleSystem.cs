using System;
using System.Collections.Generic;
using Gxdl.Core;

namespace Gxdl.Systems
{
    /// <summary>
    /// 角色定义。数值与表现分离：这里的字段只描述「是什么」，
    /// 立绘、动画、特效由视图层按 key 自行解析，不进入数据模型。
    /// </summary>
    [Serializable]
    public sealed class RoleDefinition
    {
        public string id;
        public string displayName;

        /// <summary>基础战斗属性。</summary>
        public Stats baseStats;

        /// <summary>技能 id 列表，顺序即技能栏顺序。</summary>
        public List<string> skillIds = new List<string>();

        /// <summary>视图层使用的资源 key，不参与任何数值计算。</summary>
        public string visualKey;
    }

    public interface IRoleSystem
    {
        /// <summary>已注册的全部角色定义。</summary>
        IReadOnlyList<RoleDefinition> Definitions { get; }

        /// <summary>当前出战角色 id。</summary>
        string ActiveRoleId { get; }

        void Load(IEnumerable<RoleDefinition> definitions);

        RoleDefinition Get(string roleId);

        /// <summary>切换出战角色。切换后战力相关界面应收到广播。</summary>
        bool SetActive(string roleId);

        /// <summary>取角色最终属性（含装备与成长加成）。</summary>
        Stats ResolveStats(string roleId);
    }

    /// <summary>
    /// 角色系统骨架：定义管理、出战切换与属性解析入口。
    /// 当前属性解析直接复用 GameRules，后续若引入多角色独立养成，
    /// 只需替换这一层的解析实现，界面与战斗不受影响。
    /// </summary>
    public sealed class RoleSystem : IRoleSystem
    {
        readonly IEventBus events;
        readonly List<RoleDefinition> definitions = new List<RoleDefinition>();
        GameRules rules;

        public RoleSystem(IEventBus events)
        {
            this.events = events ?? throw new ArgumentNullException(nameof(events));
        }

        public void Bind(GameRules instance)
        {
            rules = instance;
        }

        public IReadOnlyList<RoleDefinition> Definitions => definitions;

        public string ActiveRoleId { get; private set; } = string.Empty;

        public void Load(IEnumerable<RoleDefinition> source)
        {
            definitions.Clear();
            if (source == null) return;
            foreach (var definition in source)
            {
                if (definition == null || string.IsNullOrEmpty(definition.id)) continue;
                if (Get(definition.id) != null)
                {
                    UnityEngine.Debug.LogWarning("[Role] 角色 id 重复，已忽略：" + definition.id);
                    continue;
                }

                definitions.Add(definition);
            }
        }

        public RoleDefinition Get(string roleId)
        {
            if (string.IsNullOrEmpty(roleId)) return null;
            for (var i = 0; i < definitions.Count; i++)
            {
                if (definitions[i].id == roleId) return definitions[i];
            }

            return null;
        }

        public bool SetActive(string roleId)
        {
            if (Get(roleId) == null) return false;
            if (ActiveRoleId == roleId) return true;

            ActiveRoleId = roleId;
            events.Publish(new GameEvents.RoleChanged(roleId));
            return true;
        }

        public Stats ResolveStats(string roleId)
        {
            // 骨架阶段：所有角色共用玩家当前成长属性。
            // 接入独立养成后在此叠加角色自身的 baseStats 与天赋。
            if (rules != null) return rules.Attributes;

            var definition = Get(roleId);
            return definition != null ? definition.baseStats : default;
        }
    }
}
