using System;
using System.IO;
using System.Linq;
using Gxdl.EditorTools;
using UnityEditor;
using UnityEngine;

/// <summary>文件触发的 prefab 重建：写入 Tools/qa/generate.trigger 即重新生成主界面相关 prefab。</summary>
[InitializeOnLoad]
internal static class GxdlPrefabRefresh
{
    static string Root => Directory.GetParent(Application.dataPath)?.FullName ?? ".";

    static GxdlPrefabRefresh()
    {
        EditorApplication.update += Tick;
        // refresh/资产变动必发 projectChanged：后台编辑器 update 被节流时靠它确定性消费。
        EditorApplication.projectChanged += Tick;
        // 后台编辑器的 update 循环可能被节流，域重载后立即消费一次触发器。
        // [InitializeOnLoad] 各生成器的注册顺序不保证，先强制跑它们的类构造函数。
        foreach (var type in new[] {
                     typeof(HomePrefabSource), typeof(ResidencePrefabSource), typeof(BagPrefabSource),
                     typeof(ShopPrefabSource), typeof(RankPrefabSource), typeof(GrowthHubPrefabSource),
                     typeof(CodexPrefabSource), typeof(TowerPrefabSource), typeof(WorldMapPrefabSource),
                     typeof(LoadoutPrefabSource), typeof(BattlePrepPrefabSource)})
            System.Runtime.CompilerServices.RuntimeHelpers.RunClassConstructor(type.TypeHandle);
        Tick();
    }

    static float nextAttempt;
    static bool delayQueued;

    static void Tick()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling) return;
        var trigger = Path.Combine(Root, "Tools/qa/generate.trigger");
        if (!File.Exists(trigger)) return;
        // 刷新/导入进行中立即构建会把脚本引用序列化丢掉——推迟到刷新完成后的下一帧再试。
        if (EditorApplication.isUpdating || Time.realtimeSinceStartup < nextAttempt) { QueueDelayed(); return; }
        nextAttempt = Time.realtimeSinceStartup + 3f;
        var report = new System.Text.StringBuilder();
        Build(report, "Home", HomePrefabSource.Build, "UI/HomeView", "HomeView");
        Build(report, "Residence", ResidencePrefabSource.Build, "UI/ResidenceView", "ResidenceView");
        Build(report, "Bag", BagPrefabSource.Build, "UI/BagView", "BagView");
        Build(report, "Shop", ShopPrefabSource.Build, "UI/ShopView", "ShopView");
        Build(report, "Rank", RankPrefabSource.Build, "UI/RankView", "RankView");
        Build(report, "GrowthHub", GrowthHubPrefabSource.Build, "UI/GrowthHubView", "GrowthHubView");
        Build(report, "Codex", CodexPrefabSource.Build, "UI/CodexView", "CodexView");
        Build(report, "Tower", TowerPrefabSource.Build, "UI/TowerView", "TowerView");
        Build(report, "WorldMap", WorldMapPrefabSource.Build, "UI/WorldMapView", "WorldMapView");
        Build(report, "Loadout", LoadoutPrefabSource.Build, "UI/LoadoutView", "LoadoutView");
        Build(report, "BattlePrep", BattlePrepPrefabSource.Build, "UI/BattlePrepView", "BattlePrepView");
        var text = report.ToString();
        // 刷新中构建可能返回 null 或把脚本引用序列化丢掉（写坏 prefab）。
        // 任一失败：保留触发器 3 秒后整轮重试，全部通过才消费并写报告。
        if (text.Contains("-> null") || text.Contains("异常"))
        {
            Debug.LogWarning("[GxdlPrefabRefresh] 重试保留，本轮结果：\n" + text);
            return;
        }
        try { File.Delete(trigger); } catch { }
        File.WriteAllText(Path.Combine(Root, "Logs/qa/generate.txt"), text);
        Debug.Log("[GxdlPrefabRefresh]\n" + text);
    }

    // MCP/人工直调入口：菜单执行时编辑器必然空闲，绕过 isUpdating/节流守卫立即同步重建。
    [MenuItem("Gxdl/QA/Rebuild Main Prefabs")]
    public static void ForceRebuild()
    {
        var trigger = Path.Combine(Root, "Tools/qa/generate.trigger");
        if (!File.Exists(trigger)) { Debug.Log("[GxdlPrefabRefresh] 无触发器，跳过"); return; }
        nextAttempt = 0;
        var report = new System.Text.StringBuilder();
        Build(report, "Home", HomePrefabSource.Build, "UI/HomeView", "HomeView");
        Build(report, "Residence", ResidencePrefabSource.Build, "UI/ResidenceView", "ResidenceView");
        Build(report, "Bag", BagPrefabSource.Build, "UI/BagView", "BagView");
        Build(report, "Shop", ShopPrefabSource.Build, "UI/ShopView", "ShopView");
        Build(report, "Rank", RankPrefabSource.Build, "UI/RankView", "RankView");
        Build(report, "GrowthHub", GrowthHubPrefabSource.Build, "UI/GrowthHubView", "GrowthHubView");
        Build(report, "Codex", CodexPrefabSource.Build, "UI/CodexView", "CodexView");
        Build(report, "Tower", TowerPrefabSource.Build, "UI/TowerView", "TowerView");
        Build(report, "WorldMap", WorldMapPrefabSource.Build, "UI/WorldMapView", "WorldMapView");
        Build(report, "Loadout", LoadoutPrefabSource.Build, "UI/LoadoutView", "LoadoutView");
        Build(report, "BattlePrep", BattlePrepPrefabSource.Build, "UI/BattlePrepView", "BattlePrepView");
        var text = report.ToString();
        if (text.Contains("-> null") || text.Contains("异常"))
        {
            Debug.LogWarning("[GxdlPrefabRefresh] 直调重建存在失败：\n" + text);
            return;
        }
        try { File.Delete(trigger); } catch { }
        File.WriteAllText(Path.Combine(Root, "Logs/qa/generate.txt"), text);
        Debug.Log("[GxdlPrefabRefresh] 直调重建完成\n" + text);
    }

    static void QueueDelayed()
    {
        if (delayQueued) return;
        delayQueued = true;
        EditorApplication.delayCall += () => { delayQueued = false; Tick(); };
    }

    static void Build(System.Text.StringBuilder report, string name, Func<string> build,
        string resourcesPath, string moduleName)
    {
        try
        {
            var path = build();
            var go = path != null ? AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/" + resourcesPath + ".prefab") : null;
            // GetComponent(string) 对带命名空间的类型解析不可靠，按简单名精确匹配。
            var ok = go != null && go.GetComponents<Component>().Any(c => c != null && c.GetType().Name == moduleName);
            report.AppendLine(name + " -> " + (ok ? path : "null"));
        }
        catch (Exception error) { report.AppendLine(name + " 异常：" + error.Message); }
    }

    static void Build(System.Text.StringBuilder report, string name, Func<string> build)
    {
        try { report.AppendLine(name + " -> " + (build() ?? "null")); }
        catch (Exception error) { report.AppendLine(name + " 异常：" + error.Message); }
    }
}
