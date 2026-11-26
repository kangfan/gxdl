using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>文件触发的贴图重导入：写入 Tools/qa/reimport-textures.trigger 后对 Resources 下全部贴图重跑导入器。</summary>
[InitializeOnLoad]
internal static class GxdlTextureReimport
{
    static string Root => Directory.GetParent(Application.dataPath)?.FullName ?? ".";

    static GxdlTextureReimport()
    {
        EditorApplication.update += Tick;
    }

    [MenuItem("GXDL/QA/Reimport Resources Textures")]
    static void Run()
    {
        var guids = AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets/Resources" });
        var report = new System.Text.StringBuilder();
        int overridden = 0;
        try
        {
            AssetDatabase.StartAssetEditing();
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            }
        }
        finally
        {
            AssetDatabase.StopAssetEditing();
        }
        AssetDatabase.Refresh();
        foreach (var guid in guids)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) continue;
            var android = importer.GetPlatformTextureSettings("Android");
            if (android.overridden) overridden++;
            else report.AppendLine("no-android-override " + path);
        }
        report.Insert(0, "textures " + guids.Length + " androidOverridden " + overridden + "\n");
        Directory.CreateDirectory(Path.Combine(Root, "Logs/qa"));
        File.WriteAllText(Path.Combine(Root, "Logs/qa/reimport-textures.txt"), report.ToString());
        Debug.Log("[GxdlTextureReimport] " + report.ToString().Split('\n')[0]);
    }

    static void Tick()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating) return;
        var trigger = Path.Combine(Root, "Tools/qa/reimport-textures.trigger");
        if (!File.Exists(trigger)) return;
        try { File.Delete(trigger); } catch { }
        Run();
    }
}
