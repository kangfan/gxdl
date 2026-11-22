using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Gxdl.Core;
using Gxdl.Procedures;
using Gxdl.Systems;
using UnityEditor;
using UnityEngine;

namespace Gxdl.EditorTools
{
    /// <summary>
    /// 自测运行器。用最少的断言覆盖重构后必须成立的契约，
    /// 结论写入 Logs/qa/selftest-report.json，供外部逐条核对。
    ///
    /// 触发：写入 Tools/qa/selftest.request，或使用菜单 Gxdl/QA/运行自检。
    /// 不依赖 NUnit，因此可以直接引用主程序集。
    /// </summary>
    [InitializeOnLoad]
    public static class QaSelfTest
    {
        const string RequestFile = "Tools/qa/selftest.request";
        const string ReportFile = "Logs/qa/selftest-report.json";

        static readonly List<string> passed = new List<string>();
        static readonly List<string> failed = new List<string>();
        static double nextCheck;

        static QaSelfTest()
        {
            EditorApplication.update += Tick;
        }

        static void Tick()
        {
            if (EditorApplication.timeSinceStartup < nextCheck) return;
            nextCheck = EditorApplication.timeSinceStartup + 1.0;

            var request = Path.Combine(ProjectRoot, RequestFile);
            if (!File.Exists(request)) return;

            try { File.Delete(request); }
            catch { return; }

            EditorApplication.delayCall += Run;
        }

        [MenuItem("Gxdl/QA/运行自检", priority = 201)]
        static void Run()
        {
            passed.Clear();
            failed.Clear();

            RunSafely("服务容器未注册时报错清楚", ServiceRegistry_Missing_Service_Throws);
            RunSafely("事件订阅与退订生效", EventBus_Subscribe_And_Dispose);
            RunSafely("事件处理器异常不影响其它订阅者", EventBus_Handler_Isolation);
            RunSafely("流程切换先 Leave 后 Enter", Procedure_Leave_Before_Enter);
            RunSafely("流程 CanEnter 为假时拒绝切换", Procedure_Rejects_Blocked);
            RunSafely("重复注册流程被拒绝", Procedure_Duplicate_Throws);
            RunSafely("存档路径按作用域隔离", Save_Path_Isolated);
            RunSafely("非法存档作用域被拒绝", Save_Rejects_Invalid_Scope);
            RunSafely("技能默认表与现有规则一致", Skills_Default_Table_Matches_Rules);
            RunSafely("任务进度累计与完成判定", Quest_Progress_And_Complete);
            RunSafely("任务前置未完成时锁定", Quest_Prerequisite_Locks);
            RunSafely("角色切换广播事件", Role_Change_Broadcasts);

            WriteReport();
        }

        static void RunSafely(string name, Action test)
        {
            try
            {
                test();
                passed.Add(name);
            }
            catch (Exception error)
            {
                failed.Add(name + " :: " + error.GetType().Name + " - " + error.Message);
            }
        }

        static void Expect(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }

        static void ServiceRegistry_Missing_Service_Throws()
        {
            GameContext.Reset();
            var threw = false;
            try { GameContext.Get<ISaveSystem>(); }
            catch (InvalidOperationException) { threw = true; }
            Expect(threw, "未注册服务应抛出 InvalidOperationException");
        }

        static void EventBus_Subscribe_And_Dispose()
        {
            var bus = new EventBus();
            var count = 0;
            var token = bus.Subscribe<GameEvents.PlayerStatsChanged>(_ => count++);
            bus.Publish(new GameEvents.PlayerStatsChanged(1));
            Expect(count == 1, "订阅后应收到一次事件");
            token.Dispose();
            bus.Publish(new GameEvents.PlayerStatsChanged(2));
            Expect(count == 1, "退订后不应再收到事件");
        }

        static void EventBus_Handler_Isolation()
        {
            var bus = new EventBus();
            var second = false;
            bus.Subscribe<GameEvents.LobbyEntered>(_ => throw new InvalidOperationException("故意失败"));
            bus.Subscribe<GameEvents.LobbyEntered>(_ => second = true);
            bus.Publish(new GameEvents.LobbyEntered());
            Expect(second, "第一个处理器异常不应阻断后续处理器");
        }

        static void Procedure_Leave_Before_Enter()
        {
            var log = new List<string>();
            var manager = new ProcedureManager(new EventBus());
            manager.Register(new Recorder("A", log));
            manager.Register(new Recorder("B", log));
            manager.Change("A");
            manager.Change("B");
            Expect(string.Join(",", log.ToArray()) == "enter:A,leave:A,enter:B", "切换顺序应为 leave 后 enter");
        }

        static void Procedure_Rejects_Blocked()
        {
            var manager = new ProcedureManager(new EventBus());
            manager.Register(new Blocked());
            Expect(!manager.Change("Blocked"), "CanEnter 为假时应返回 false");
            Expect(manager.Current == null, "拒绝后当前流程应保持为空");
        }

        static void Procedure_Duplicate_Throws()
        {
            var manager = new ProcedureManager(new EventBus());
            manager.Register(new Recorder("A", new List<string>()));
            var threw = false;
            try { manager.Register(new Recorder("A", new List<string>())); }
            catch (InvalidOperationException) { threw = true; }
            Expect(threw, "重复注册应抛出异常");
        }

        static void Save_Path_Isolated()
        {
            var local = SaveSystem.ResolvePath("");
            var account = SaveSystem.ResolvePath(new string('a', 64));
            Expect(local != account, "不同作用域必须写到不同文件");
            Expect(account.Contains("gxdl-account-"), "账号存档文件名应带 gxdl-account- 前缀");
        }

        static void Save_Rejects_Invalid_Scope()
        {
            var system = new SaveSystem(new EventBus());
            var threw = false;
            try { system.UseScope("../../etc/passwd"); }
            catch (ArgumentException) { threw = true; }
            Expect(threw, "非法作用域必须被拒绝，防止路径穿越");
        }

        static void Skills_Default_Table_Matches_Rules()
        {
            var skills = new SkillSystem(new EventBus());
            skills.LoadDefaults();
            Expect(skills.Definitions.Count == 4, "默认技能表应有 4 个技能");
            var sword = skills.Get("sword");
            Expect(sword != null, "御剑诀应存在");
            Expect(sword.magnitudePercent == GameRules.SwordDamagePercent, "御剑诀数值应与规则常量一致");
        }

        static void Quest_Progress_And_Complete()
        {
            var bus = new EventBus();
            var system = new QuestSystem(bus);
            system.Load(new[]
            {
                new Gxdl.Systems.QuestDefinition
                {
                    id = "q1",
                    objective = Gxdl.Systems.QuestObjective.ClearStage,
                    targetAmount = 2
                }
            });

            var completed = system.ReportProgress(Gxdl.Systems.QuestObjective.ClearStage, 1);
            Expect(completed.Count == 0, "未达目标不应完成");
            completed = system.ReportProgress(Gxdl.Systems.QuestObjective.ClearStage, 1);
            Expect(completed.Count == 1, "达到目标应完成一个任务");
            Expect(system.GetState("q1").completed, "状态应标记为完成");
        }

        static void Quest_Prerequisite_Locks()
        {
            var system = new QuestSystem(new EventBus());
            system.Load(new[]
            {
                new Gxdl.Systems.QuestDefinition { id = "a", objective = Gxdl.Systems.QuestObjective.ClearStage, targetAmount = 1 },
                new Gxdl.Systems.QuestDefinition { id = "b", objective = Gxdl.Systems.QuestObjective.ClearStage, targetAmount = 1, prerequisite = "a" }
            });

            Expect(system.IsLocked(system.Definitions[1]), "后续任务在前置完成前应保持锁定");

            var completed = system.ReportProgress(Gxdl.Systems.QuestObjective.ClearStage, 1);
            Expect(completed.Count == 1 && completed[0].id == "a", "只有前置任务应完成");

            completed = system.ReportProgress(Gxdl.Systems.QuestObjective.ClearStage, 1);
            Expect(completed.Count == 1 && completed[0].id == "b", "前置完成后后续任务应解锁并可完成");
        }

        static void Role_Change_Broadcasts()
        {
            var bus = new EventBus();
            var system = new RoleSystem(bus);
            system.Load(new[]
            {
                new Gxdl.Systems.RoleDefinition { id = "hero", displayName = "寻墟者" }
            });

            string received = null;
            bus.Subscribe<GameEvents.RoleChanged>(e => received = e.RoleId);
            Expect(system.SetActive("hero"), "有效角色应切换成功");
            Expect(received == "hero", "切换角色应广播 RoleChanged");
            Expect(!system.SetActive("missing"), "不存在的角色应切换失败");
        }

        static void WriteReport()
        {
            var path = Path.Combine(ProjectRoot, ReportFile);
            Directory.CreateDirectory(Path.GetDirectoryName(path));

            var builder = new StringBuilder();
            builder.AppendLine("{");
            builder.AppendLine($"  \"timestamp\": \"{DateTime.UtcNow:O}\",");
            builder.AppendLine($"  \"passed\": {passed.Count},");
            builder.AppendLine($"  \"failed\": {failed.Count},");
            builder.AppendLine($"  \"success\": {(failed.Count == 0 ? "true" : "false")},");
            builder.AppendLine("  \"passedCases\": [");
            AppendArray(builder, passed);
            builder.AppendLine("  ],");
            builder.AppendLine("  \"failedCases\": [");
            AppendArray(builder, failed);
            builder.AppendLine("  ]");
            builder.AppendLine("}");

            File.WriteAllText(path, builder.ToString(), new UTF8Encoding(false));
            Debug.Log($"[QaSelfTest] 自检完成：通过 {passed.Count}，失败 {failed.Count}，报告 {path}");
        }

        static void AppendArray(StringBuilder builder, List<string> values)
        {
            for (var i = 0; i < values.Count; i++)
            {
                var escaped = values[i].Replace("\\", "\\\\").Replace("\"", "\\\"");
                builder.Append("    \"").Append(escaped).Append('"');
                builder.AppendLine(i + 1 < values.Count ? "," : string.Empty);
            }
        }

        static string ProjectRoot => Directory.GetParent(Application.dataPath)?.FullName ?? ".";

        sealed class Recorder : ProcedureBase
        {
            readonly List<string> log;

            public Recorder(string id, List<string> log)
            {
                Id = id;
                this.log = log;
            }

            public override string Id { get; }

            public override void OnEnter() => log.Add("enter:" + Id);

            public override void OnLeave() => log.Add("leave:" + Id);
        }

        sealed class Blocked : ProcedureBase
        {
            public override string Id => "Blocked";

            public override bool CanEnter() => false;
        }
    }
}
