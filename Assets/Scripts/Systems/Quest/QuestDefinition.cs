using System;
using System.Collections.Generic;

namespace Gxdl.Systems
{
    /// <summary>任务目标类型。新增类型时同步更新 QuestSystem 的进度统计。</summary>
    public enum QuestObjective
    {
        /// <summary>通关指定主线关卡。</summary>
        ClearStage = 0,

        /// <summary>累计破封次数。</summary>
        UnsealCount = 1,

        /// <summary>达到指定境界。</summary>
        ReachLevel = 2,

        /// <summary>累计击败指定副本的首领。</summary>
        DefeatDungeon = 3
    }

    /// <summary>
    /// 任务定义。纯数据，不含逻辑，可由 ScriptableObject 或 JSON 提供。
    /// 后续接入正式任务时只需要新增资源，不需要改动任务系统代码。
    /// </summary>
    [Serializable]
    public sealed class QuestDefinition
    {
        public string id;
        public string title;
        public string description;
        public QuestObjective objective;
        public int targetAmount = 1;

        /// <summary>前置任务 id，为空表示无前置。</summary>
        public string prerequisite;

        /// <summary>奖励配置：道具 id 到数量的映射。</summary>
        public List<QuestReward> rewards = new List<QuestReward>();
    }

    [Serializable]
    public sealed class QuestReward
    {
        public string itemId;
        public int amount;
    }

    /// <summary>单个任务的运行时状态。</summary>
    [Serializable]
    public sealed class QuestState
    {
        public string questId;
        public int progress;
        public bool completed;
        public bool claimed;

        public QuestState()
        {
        }

        public QuestState(string id)
        {
            questId = id;
        }
    }
}
