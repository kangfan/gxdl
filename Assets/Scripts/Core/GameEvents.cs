namespace Gxdl.Core
{
    /// <summary>
    /// 全局事件定义集中在此，避免各系统各自定义匿名事件导致命名冲突。
    /// 全部使用 struct，不产生堆分配。
    /// </summary>
    public static class GameEvents
    {
        /// <summary>账号状态变化（登录成功、注销、名称变更）。</summary>
        public readonly struct AccountChanged
        {
            public readonly string SaveScope;
            public readonly bool LoggedIn;

            public AccountChanged(string saveScope, bool loggedIn)
            {
                SaveScope = saveScope;
                LoggedIn = loggedIn;
            }
        }

        /// <summary>存档已写入磁盘。</summary>
        public readonly struct SaveStored
        {
            public readonly string Path;

            public SaveStored(string path)
            {
                Path = path;
            }
        }

        /// <summary>玩家数值变化（金币、破封符、战力等），界面据此刷新。</summary>
        public readonly struct PlayerStatsChanged
        {
            public readonly int Power;

            public PlayerStatsChanged(int power)
            {
                Power = power;
            }
        }

        /// <summary>装备状态变化（获取、替换、分解）。</summary>
        public readonly struct EquipmentChanged
        {
            public readonly int Slot;

            public EquipmentChanged(int slot)
            {
                Slot = slot;
            }
        }

        /// <summary>战斗开始，携带参战关卡标识。</summary>
        public readonly struct BattleStarted
        {
            public readonly int StageId;

            public BattleStarted(int stageId)
            {
                StageId = stageId;
            }
        }

        /// <summary>战斗结束，携带胜负与结算是否已应用。</summary>
        public readonly struct BattleEnded
        {
            public readonly int StageId;
            public readonly bool Won;

            public BattleEnded(int stageId, bool won)
            {
                StageId = stageId;
                Won = won;
            }
        }

        /// <summary>任务进度变化。</summary>
        public readonly struct QuestProgressChanged
        {
            public readonly string QuestId;
            public readonly int Current;
            public readonly int Target;

            public QuestProgressChanged(string questId, int current, int target)
            {
                QuestId = questId;
                Current = current;
                Target = target;
            }
        }

        /// <summary>任务完成待领取。</summary>
        public readonly struct QuestCompleted
        {
            public readonly string QuestId;

            public QuestCompleted(string questId)
            {
                QuestId = questId;
            }
        }

        /// <summary>任务奖励发放。道具系统订阅后实际入包。</summary>
        public readonly struct QuestRewardGranted
        {
            public readonly string QuestId;
            public readonly string ItemId;
            public readonly int Amount;

            public QuestRewardGranted(string questId, string itemId, int amount)
            {
                QuestId = questId;
                ItemId = itemId;
                Amount = amount;
            }
        }

        /// <summary>出战角色切换。</summary>
        public readonly struct RoleChanged
        {
            public readonly string RoleId;

            public RoleChanged(string roleId)
            {
                RoleId = roleId;
            }
        }

        /// <summary>技能表加载完成。</summary>
        public readonly struct SkillsLoaded
        {
            public readonly int Count;

            public SkillsLoaded(int count)
            {
                Count = count;
            }
        }

        /// <summary>加载进度变化，0..1 单调递增。</summary>
        public readonly struct LoadingProgressChanged
        {
            public readonly float Progress;
            public readonly string Message;

            public LoadingProgressChanged(float progress, string message)
            {
                Progress = progress;
                Message = message;
            }
        }

        /// <summary>加载失败，界面应显示重试或返回登录。</summary>
        public readonly struct LoadingFailed
        {
            public readonly string Reason;

            public LoadingFailed(string reason)
            {
                Reason = reason;
            }
        }

        /// <summary>进入大厅。</summary>
        public readonly struct LobbyEntered
        {
        }

        /// <summary>离开大厅，进入副本或战斗。</summary>
        public readonly struct LobbyLeft
        {
        }

        /// <summary>进入副本界面。</summary>
        public readonly struct DungeonEntered
        {
        }

        /// <summary>离开副本界面。</summary>
        public readonly struct DungeonLeft
        {
        }

        /// <summary>选中关卡。</summary>
        public readonly struct StageSelected
        {
            public readonly int StageId;

            public StageSelected(int stageId)
            {
                StageId = stageId;
            }
        }

        /// <summary>一个回合的战斗事件产生完毕，界面据此播放表现。</summary>
        public readonly struct BattleTurnReady
        {
            public readonly System.Collections.Generic.IReadOnlyList<GuxuBattleEvent> Events;

            public BattleTurnReady(System.Collections.Generic.IReadOnlyList<GuxuBattleEvent> events)
            {
                Events = events;
            }
        }

        /// <summary>战斗结算完成。</summary>
        public readonly struct BattleSettled
        {
            public readonly Gxdl.Systems.BattleResult Result;

            public BattleSettled(Gxdl.Systems.BattleResult result)
            {
                Result = result;
            }
        }
    }
}
