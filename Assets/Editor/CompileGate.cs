using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Gxdl.EditorTools
{
    /// <summary>
    /// 编译门禁：由外部进程写触发文件，编辑器在空闲后收集 Console 中的
    /// 编译错误与警告，连同编译/播放状态一起落盘。
    ///
    /// Unity 的 Editor.log 位于用户 AppData 下，外部工具未必能读取，
    /// 因此这里主动把结论写到项目内，形成可被外部校验的闭环。
    ///
    /// 触发：写入 Tools/qa/compile.request
    /// 产出：Logs/qa/compile-report.json
    /// </summary>
    [InitializeOnLoad]
    public static class CompileGate
    {
        const string RequestFile = "Tools/qa/compile.request";
        const string ReportFile = "Logs/qa/compile-report.json";

        static readonly List<string> errors = new List<string>();
        static readonly List<string> warnings = new List<string>();
        static double nextCheck;
        static bool collecting;

        static CompileGate()
        {
            Application.logMessageReceived -= HandleLog;
            Application.logMessageReceived += HandleLog;
            AssemblyReloadEvents.beforeAssemblyReload += HandleBeforeReload;
            AssemblyReloadEvents.afterAssemblyReload += HandleAfterReload;
            EditorApplication.update += Tick;
        }

        static void HandleLog(string condition, string stackTrace, LogType type)
        {
            // 编译期日志可能发生在收集开关打开之前，因此始终按类型留底，
            // 触发门禁时再整体写出，避免漏掉真正的编译错误。
            if (type == LogType.Exception)
            {
                if (errors.Count < 200) errors.Add(condition);
            }
            else if (type == LogType.Error)
            {
                if (errors.Count < 200) errors.Add(condition);
            }
            else if (type == LogType.Warning)
            {
                if (warnings.Count < 200) warnings.Add(condition);
            }
        }

        static void HandleBeforeReload()
        {
            collecting = true;
        }

        static void HandleAfterReload()
        {
            collecting = false;
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

        static void Run()
        {
            // 编译或资源导入期间不产出结论，避免写出「假的通过」。
            if (EditorApplication.isCompiling || EditorApplication.isUpdating || collecting)
            {
                WriteReport("pending", "编辑器仍在编译或导入资源，稍后重试");
                return;
            }

            var ok = errors.Count == 0;
            WriteReport(ok ? "ok" : "failed", ok ? "编译通过，未发现错误" : "发现编译错误");
        }

        static void WriteReport(string status, string message)
        {
            var path = Path.Combine(ProjectRoot, ReportFile);
            Directory.CreateDirectory(Path.GetDirectoryName(path));

            var builder = new StringBuilder();
            builder.AppendLine("{");
            builder.AppendLine($"  \"success\": {(status == "ok" ? "true" : "false")},");
            builder.AppendLine($"  \"status\": \"{status}\",");
            builder.AppendLine($"  \"message\": \"{Escape(message)}\",");
            builder.AppendLine($"  \"timestamp\": \"{DateTime.UtcNow:O}\",");
            builder.AppendLine($"  \"unityVersion\": \"{Application.unityVersion}\",");
            builder.AppendLine($"  \"isPlaying\": {(EditorApplication.isPlaying ? "true" : "false")},");
            builder.AppendLine($"  \"errorCount\": {errors.Count},");
            builder.AppendLine($"  \"warningCount\": {warnings.Count},");
            builder.AppendLine("  \"errors\": [");
            AppendArray(builder, errors);
            builder.AppendLine("  ],");
            builder.AppendLine("  \"warnings\": [");
            AppendArray(builder, warnings);
            builder.AppendLine("  ]");
            builder.AppendLine("}");

            File.WriteAllText(path, builder.ToString(), new UTF8Encoding(false));
            Debug.Log($"[CompileGate] 已写出编译报告（{status}，错误 {errors.Count}，警告 {warnings.Count}）");

            // 报告写出后清空缓冲，下一轮门禁只反映新产生的日志。
            errors.Clear();
            warnings.Clear();
        }

        static void AppendArray(StringBuilder builder, List<string> values)
        {
            for (var i = 0; i < values.Count; i++)
            {
                builder.Append("    \"").Append(Escape(values[i])).Append('"');
                builder.AppendLine(i + 1 < values.Count ? "," : string.Empty);
            }
        }

        static string Escape(string raw)
        {
            if (string.IsNullOrEmpty(raw)) return string.Empty;
            return raw.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\r", " ").Replace("\n", " ");
        }

        static string ProjectRoot => Directory.GetParent(Application.dataPath)?.FullName ?? ".";

        /// <summary>供菜单手动触发一次门禁，便于不依赖文件触发时使用。</summary>
        [MenuItem("Gxdl/QA/写出编译报告", priority = 200)]
        static void ManualReport()
        {
            Run();
        }
    }
}
