using System;
using System.Collections.Generic;
using Gxdl.Core;

namespace Gxdl.Systems
{
    public interface IQuestSystem
    {
        /// <summary>已加载的全部任务定义。</summary>
        IReadOnlyList<QuestDefinition> Definitions { get; }

        /// <summary>加载任务定义表，供配置系统或测试注入。</summary>
        void Load(IEnumerable<QuestDefinition> definitions);

        /// <summary>取任务当前进度状态。</summary>
        QuestState GetState(string questId);

        /// <summary>推进指定目标的进度，返回受影响并因此完成的任务。</summary>
        IReadOnlyList<QuestDefinition> ReportProgress(QuestObjective objective, int amount);

        /// <summary>领取已完成任务的奖励。</summary>
        bool Claim(string questId, out IReadOnlyList<QuestReward> rewards);

        /// <summary>任务是否因前置条件未满足而锁定。</summary>
        bool IsLocked(QuestDefinition definition);
    }

    /// <summary>
    /// 任务系统骨架。当前只实现进度累计、完成判定与领取状态机，
    /// 奖励发放通过事件交给道具系统，系统本身不认识具体道具。
    /// 正式任务链配置留待后续迭代。
    /// </summary>
    public sealed class QuestSystem : IQuestSystem
    {
        readonly IEventBus events;
        readonly List<QuestDefinition> definitions = new List<QuestDefinition>();
        readonly Dictionary<string, QuestState> states = new Dictionary<string, QuestState>();

        public QuestSystem(IEventBus events)
        {
            this.events = events ?? throw new ArgumentNullException(nameof(events));
        }

        public IReadOnlyList<QuestDefinition> Definitions => definitions;

        public void Load(IEnumerable<QuestDefinition> source)
        {
            definitions.Clear();
            states.Clear();
            if (source == null) return;

            foreach (var definition in source)
            {
                if (definition == null || string.IsNullOrEmpty(definition.id)) continue;
                if (Find(definition.id) != null)
                {
                    UnityEngine.Debug.LogWarning("[Quest] 任务 id 重复，已忽略：" + definition.id);
                    continue;
                }

                definitions.Add(definition);
                states[definition.id] = new QuestState(definition.id);
            }
        }

        public QuestState GetState(string questId)
        {
            if (string.IsNullOrEmpty(questId)) return null;
            return states.TryGetValue(questId, out var state) ? state : null;
        }

        public bool IsLocked(QuestDefinition definition)
        {
            if (definition == null || string.IsNullOrEmpty(definition.prerequisite)) return false;
            var prerequisite = GetState(definition.prerequisite);
            return prerequisite == null || !prerequisite.completed;
        }

        public IReadOnlyList<QuestDefinition> ReportProgress(QuestObjective objective, int amount)
        {
            var completed = new List<QuestDefinition>();
            if (amount <= 0) return completed;

            // 锁定判定按本轮开始前的完成快照，避免同一次推进里前置与后续级联完成。
            var completedBefore = new HashSet<string>();
            foreach (var pair in states)
            {
                if (pair.Value.completed) completedBefore.Add(pair.Key);
            }

            for (var i = 0; i < definitions.Count; i++)
            {
                var definition = definitions[i];
                if (definition.objective != objective) continue;
                if (!string.IsNullOrEmpty(definition.prerequisite) && !completedBefore.Contains(definition.prerequisite)) continue;

                var state = states[definition.id];
                if (state.completed) continue;

                state.progress = Math.Min(definition.targetAmount, state.progress + amount);
                events.Publish(new GameEvents.QuestProgressChanged(definition.id, state.progress, definition.targetAmount));

                if (state.progress < definition.targetAmount) continue;

                state.completed = true;
                completed.Add(definition);
                events.Publish(new GameEvents.QuestCompleted(definition.id));
            }

            return completed;
        }

        public bool Claim(string questId, out IReadOnlyList<QuestReward> rewards)
        {
            rewards = null;
            var state = GetState(questId);
            if (state == null || !state.completed || state.claimed) return false;

            var definition = Find(questId);
            if (definition == null) return false;

            state.claimed = true;
            rewards = definition.rewards;

            // 奖励发放本身交由订阅方处理，任务系统不直接操作背包。
            for (var i = 0; i < definition.rewards.Count; i++)
            {
                var reward = definition.rewards[i];
                if (reward == null || string.IsNullOrEmpty(reward.itemId)) continue;
                events.Publish(new GameEvents.QuestRewardGranted(questId, reward.itemId, reward.amount));
            }

            return true;
        }

        QuestDefinition Find(string questId)
        {
            for (var i = 0; i < definitions.Count; i++)
            {
                if (definitions[i].id == questId) return definitions[i];
            }

            return null;
        }
    }
}
