using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Opens the main scene and enters play mode.
///
/// Two ways in:
///   1. Menu:  Gxdl > 运行游戏
///   2. Drop a file named run-game.trigger into Tools/ — the trigger is
///      consumed (deleted) once play mode starts, so it fires only once.
///
/// The trigger file exists so an external process that can only write files
/// (rather than call Unity APIs) can still start a play session.
/// </summary>
[InitializeOnLoad]
internal static class GxdlRunGame
{
    private const string MainScene = "Assets/Scenes/GxdlMain.unity";

    private static string ProjectRoot =>
        Directory.GetParent(Application.dataPath)?.FullName ?? ".";

    private static string TriggerFile =>
        Path.Combine(ProjectRoot, "Tools", "run-game.trigger");

    private static string ReportFile =>
        Path.Combine(ProjectRoot, "Logs", "run-game.txt");

    static GxdlRunGame()
    {
        EditorApplication.delayCall += CheckTrigger;
        EditorApplication.update += CheckTrigger;
    }

    [MenuItem("Gxdl/运行游戏 %#g", priority = 1)]
    private static void RunFromMenu()
    {
        StartPlay();
    }

    [MenuItem("Gxdl/运行游戏（快速重载当前场景）", priority = 2)]
    private static void RunCurrentScene()
    {
        StartPlay(saveCurrent: true);
    }

    private static void CheckTrigger()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        if (EditorApplication.isCompiling || EditorApplication.isUpdating) return;

        string trigger;
        try
        {
            trigger = TriggerFile;
            if (!File.Exists(trigger)) return;
        }
        catch
        {
            return;
        }

        // Consume the trigger before doing anything else so it cannot re-fire.
        try { File.Delete(trigger); } catch { }

        Log("trigger file detected -> entering play mode");
        StartPlay();
    }

    private static void StartPlay(bool saveCurrent = false)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Log("already in play mode, ignoring");
            return;
        }

        Log("run-game " + DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss.fffzzz"));
        Log("unity pid " + System.Diagnostics.Process.GetCurrentProcess().Id);

        try
        {
            var active = EditorSceneManager.GetActiveScene();
            bool needOpen = !string.Equals(active.path, MainScene, StringComparison.OrdinalIgnoreCase);

            if (saveCurrent && !string.IsNullOrEmpty(active.path))
            {
                EditorSceneManager.SaveScene(active);
                Log("saved scene " + active.path);
            }

            if (needOpen)
            {
                if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                {
                    var opened = EditorSceneManager.OpenScene(MainScene, OpenSceneMode.Single);
                    Log("opened scene " + MainScene + " (" + opened.name + ")");
                }
                else
                {
                    Log("ABORT: user cancelled saving modified scenes");
                    return;
                }
            }
            else
            {
                Log("already on " + MainScene);
            }

            EditorApplication.isPlaying = true;
            Log("EditorApplication.isPlaying = true");
            Log("VERDICT: play mode requested");
        }
        catch (Exception e)
        {
            Log("EXCEPTION " + e.GetType().FullName + ": " + e.Message);
            Log(e.StackTrace ?? "");
            Log("VERDICT: FAIL - could not enter play mode");
        }
    }

    private static void Log(string line)
    {
        Debug.Log("[Gxdl Run] " + line);
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(ReportFile));
            File.AppendAllText(ReportFile, line + Environment.NewLine);
        }
        catch { }
    }
}
