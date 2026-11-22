using System;
using System.IO;
using Newtonsoft.Json;
using UnityEngine;
using Gxdl.Core;

namespace Gxdl.Systems
{
    public interface ISaveSystem
    {
        /// <summary>当前存档作用域，空字符串代表本地单机档。</summary>
        string Scope { get; }

        /// <summary>当前存档路径，用于自测与备份。</summary>
        string Path { get; }

        /// <summary>读取存档；不存在或损坏时返回 null，由调用方决定新建。</summary>
        SaveData Load(string scope);

        /// <summary>写入存档，采用「临时文件 + 原子替换」避免写一半损坏。</summary>
        void Store(SaveData data);

        /// <summary>切换存档作用域（登录不同账号时调用）。</summary>
        void UseScope(string scope);

        /// <summary>导出存档副本，供自测夹具备份与恢复。</summary>
        void Backup(string destinationPath);

        /// <summary>从备份恢复，自测结束必须调用以还原玩家真实进度。</summary>
        bool Restore(string sourcePath);
    }

    /// <summary>
    /// 存档系统。负责路径隔离、原子写入与损坏兜底，
    /// 不承载任何业务规则——规则仍在 GameRules 里。
    /// </summary>
    public sealed class SaveSystem : ISaveSystem
    {
        readonly IEventBus events;
        string scope = string.Empty;

        public SaveSystem(IEventBus events)
        {
            this.events = events ?? throw new ArgumentNullException(nameof(events));
        }

        public string Scope => scope;

        public string Path => ResolvePath(scope);

        /// <summary>
        /// 账号作用域必须为空或 64 位小写十六进制，防止路径穿越。
        /// 这与原先 GameRules.SelectAccount 的约束保持一致。
        /// </summary>
        public static string ResolvePath(string accountScope)
        {
            var directory = Application.persistentDataPath;
            if (string.IsNullOrEmpty(accountScope)) return System.IO.Path.Combine(directory, "gxdl-local-v1.json");
            return System.IO.Path.Combine(directory, "gxdl-account-" + accountScope + ".json");
        }

        public void UseScope(string accountScope)
        {
            if (accountScope != null && accountScope.Length != 0 &&
                !System.Text.RegularExpressions.Regex.IsMatch(accountScope, @"\A[a-f0-9]{64}\z"))
            {
                throw new ArgumentException("非法账号存档作用域：" + accountScope, nameof(accountScope));
            }

            scope = accountScope ?? string.Empty;
        }

        public SaveData Load(string accountScope)
        {
            var path = ResolvePath(accountScope);
            if (!File.Exists(path)) return null;

            try
            {
                var data = JsonConvert.DeserializeObject<SaveData>(File.ReadAllText(path));
                GameRules.ValidateSave(data);
                return data;
            }
            catch (Exception error)
            {
                // 损坏的存档保留现场再返回 null，绝不静默覆盖玩家数据。
                Debug.LogWarning("[Save] 存档读取失败，保留原文件：" + error.Message);
                TryCopy(path, path + ".invalid");
                return null;
            }
        }

        public void Store(SaveData data)
        {
            if (data == null) return;

            var path = Path;
            var temp = path + ".tmp";
            try
            {
                var json = JsonConvert.SerializeObject(data);
                File.WriteAllText(temp, json);
                if (File.Exists(path)) File.Replace(temp, path, path + ".bak");
                else File.Move(temp, path);
                events.Publish(new GameEvents.SaveStored(path));
            }
            catch (Exception error)
            {
                Debug.LogError("[Save] 存档写入失败：" + error.Message);
                if (File.Exists(temp)) TryDelete(temp);
            }
        }

        public void Backup(string destinationPath)
        {
            if (string.IsNullOrEmpty(destinationPath)) return;
            var directory = System.IO.Path.GetDirectoryName(destinationPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory)) Directory.CreateDirectory(directory);
            TryCopy(Path, destinationPath);
        }

        public bool Restore(string sourcePath)
        {
            if (string.IsNullOrEmpty(sourcePath) || !File.Exists(sourcePath)) return false;
            TryCopy(sourcePath, Path);
            return File.Exists(Path);
        }

        static void TryCopy(string from, string to)
        {
            try
            {
                if (!File.Exists(from)) return;
                File.Copy(from, to, true);
            }
            catch (Exception error)
            {
                Debug.LogWarning("[Save] 复制存档失败 " + from + " -> " + to + "：" + error.Message);
            }
        }

        static void TryDelete(string path)
        {
            try { File.Delete(path); }
            catch { /* 清理失败不影响主流程 */ }
        }
    }
}
