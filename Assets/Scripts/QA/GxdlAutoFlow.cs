#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Gxdl;
using Gxdl.Core;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.UI;

namespace Gxdl.QA
{
    /// <summary>
    /// 仅在编辑器 Play 中使用：项目根存在 Tools/qa/flowcheck.run 时自动执行界面流程检查，
    /// 写入 Logs/qa/flow-check.txt 与 Logs/qa/shots/，跑完删除触发文件。
    /// </summary>
    public static class GxdlAutoFlow
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (!File.Exists(Path.Combine(ProjectRoot, "Tools/qa/flowcheck.run"))) return;
            var go = new GameObject("QA Flow Runner");
            UnityEngine.Object.DontDestroyOnLoad(go);
            go.AddComponent<FlowRunner>();
        }

        internal static string ProjectRoot =>
            Directory.GetParent(Application.dataPath)?.FullName ?? ".";
    }

    public sealed class FlowRunner : MonoBehaviour
    {
        const string ReportPath = "Logs/qa/flow-check.txt";
        const string ShotDir = "Logs/qa/shots";
        static readonly List<string> lines = new List<string>();

        IEnumerator Start()
        {
            Directory.CreateDirectory(Path.Combine(GxdlAutoFlow.ProjectRoot, ShotDir));
            Write("== 自动验证启动 ==");
            ScreenCapture.CaptureScreenshot(Path.Combine(ShotDir, "00-boot.png"));
            var deadline = Time.realtimeSinceStartup + 120f;
            bool loginTried = false;
            float openingSince = -1f;
            while (GxdlGame.Instance == null || GxdlGame.Instance.Rules == null ||
                   !GxdlGame.Instance.CanPlay ||
                   GameBootstrap.Instance == null || GameBootstrap.Instance.UiModules == null)
            {
                if (Time.realtimeSinceStartup > deadline) { Write("FAIL 120 秒内未初始化"); Stop(); yield break; }
                var stage = GxdlGame.Instance != null ? GxdlGame.Instance.EntryStage.ToString() : "no-instance";
                if (stage == "Login" && !loginTried)
                {
                    var session = UnityEngine.Object.FindObjectOfType<GuxuAccountSession>();
                    if (session != null) { session.LoginEditorPreview(); loginTried = true; Write("执行编辑器测试登录"); }
                }
                if (stage == "Opening")
                {
                    if (openingSince < 0f) openingSince = Time.realtimeSinceStartup;
                    else if (Time.realtimeSinceStartup - openingSince > 8f) SkipOpening();
                }
                Write("初始化等待 stage=" + stage +
                      " rules=" + (GxdlGame.Instance != null && GxdlGame.Instance.Rules != null) +
                      " bootstrap=" + (GameBootstrap.Instance != null));
                yield return new WaitForSeconds(1f);
            }
            Write("== 游戏已初始化 ==");
            var game = GxdlGame.Instance;
            var backup = JsonConvert.SerializeObject(game.Rules.Save);
            var failures = new List<string>();
            var inner = Steps(game, failures, backup);
            while (true)
            {
                object current = null; bool more;
                try { more = inner.MoveNext(); current = more ? inner.Current : null; }
                catch (Exception error) { Write("EXCEPTION " + error); Write("RESULT FAIL exception"); break; }
                if (!more) break;
                yield return current;
            }
            game.Rules.Save = JsonConvert.DeserializeObject<SaveData>(backup);
            game.Rules.Store();
            yield return null;
            File.Delete(Path.Combine(GxdlAutoFlow.ProjectRoot, "Tools/qa/flowcheck.run"));
            Write("== 结束，可停止 Play ==");
        }

        IEnumerator Steps(GxdlGame game, List<string> failures, string backup)
        {
            int shot = 0;
            yield return Step("Home", () => game.Home(), "HomeView", shot++, failures);
            DiagnoseHome();
            yield return Step("Residence", game.ShowFinalResidence, "ResidenceView", shot++, failures);
            yield return Click("BtnClose", shot++, failures);
            Check("关闭洞府回到主页", ActiveId() == "HomeView", failures);
            yield return Step("Bag", game.ShowBag, "BagView", shot++, failures);
            yield return Click("Slot 1", shot++, failures);
            Check("背包槽位打开装备详情", Find("BtnClose2") != null, failures);
            yield return Click("BtnClose2", shot++, failures);
            // 缩放动画各 0.1 秒，等它结束再断言。
            yield return new WaitForSeconds(0.3f);
            yield return Click("BtnClose", shot++, failures);
            yield return new WaitForSeconds(0.3f);
            Check("关闭背包回到主页", ActiveId() == "HomeView", failures);
            game.Rules.Save.peaches = Mathf.Max(game.Rules.Save.peaches, 1);
            game.Rules.Chop();
            yield return Step("RelicDrop", () => game.Home(), "HomeView", shot++, failures);
            yield return Click("Resolve Equipment", shot++, failures);
            Check("遗宝分解后弹窗关闭", game.Rules.Save.pending == null, failures);
            // 分解演出期间 busy=true，等它结束再继续导航。
            yield return new WaitForSeconds(3f);
            yield return Step("WorldMap", game.ShowWorldMap, "WorldMapView", shot++, failures);
            yield return Click("Header Back", shot++, failures);

            yield return Step("Chapter-1", () => game.ShowChapter(0), "ChapterMapView", shot++, failures);
            yield return Step("BattlePrep", () => game.ShowBattlePrep(Stage(1)), "BattlePrepView", shot++, failures);
            yield return Click("Prep Close", shot++, failures);
            Check("关闭战前准备回到章节", ActiveId() == "ChapterMapView", failures);

            yield return Step("GrowthHub", game.ShowGrowthHub, "GrowthHubView", shot++, failures);
            yield return Click("Dungeons", shot++, failures);
            Check("成长总览底部按钮可用", ActiveId() == "DungeonsView", failures);
            yield return Click("Back", shot++, failures);

            yield return Step("Shop", game.ShowShop, "ShopView", shot++, failures);
            int coins = game.Rules.Save.coins;
            var buy = Find("Buy");
            if (buy != null) { buy.onClick.Invoke(); yield return null; }
            Check("商城购买扣灵尘", game.Rules.Save.coins < coins, failures);
            yield return Click("Back", shot++, failures);

            yield return Step("Rank", game.ShowRank, "RankView", shot++, failures);
            yield return Click("Back", shot++, failures);

            yield return Step("Codex", game.ShowCodex, "CodexView", shot++, failures);
            yield return Click("Header Back", shot++, failures);

            yield return Step("Loadout", game.ShowLoadout, "LoadoutView", shot++, failures);
            yield return Click("Return", shot++, failures);

            yield return Step("Dungeon-0", () => game.ShowDungeon(0), "DungeonDetailView", shot++, failures);
            yield return Click("Back", shot++, failures);

            yield return Step("Tower", game.ShowTower, "TowerView", shot++, failures);
            yield return Click("Tower Next Segment", shot++, failures);
            Check("古塔翻段后仍在古塔", ActiveId() == "TowerView", failures);

            yield return Step("Battle", () => { game.Home(); game.ShowBattlePrep(Stage(1)); }, "BattlePrepView", shot++, failures);
            yield return Click("Ruins Challenge", shot++, failures);
            yield return new WaitForSeconds(2f);
            Check("开战后战前页已关闭", ActiveId() == null, failures);
            yield return Click("退出挑战", shot++, failures);
            Check("退出战斗回到章节", ActiveId() == "ChapterMapView", failures);

            Write(failures.Count == 0 ? "RESULT PASS" : "RESULT FAIL " + failures.Count);
            foreach (var failure in failures) Write("  - " + failure);
        }

        static GuxuStage Stage(int id) => GuxuStageConfig.Load().ForProgress(id);

        static void DiagnoseHome()
        {
            var view = UnityEngine.Object.FindObjectOfType<Gxdl.Modules.HomeView>(true);
            if (view == null) { Write("诊断 HomeView 不在场景中"); return; }
            var field = typeof(Gxdl.Modules.HomeView).GetField("background",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var background = field != null ? field.GetValue(view) as Image : null;
            var navField = typeof(Gxdl.Modules.HomeView).GetField("navButtons",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var nav = navField != null ? navField.GetValue(view) as Button[] : null;
            var navPanel = view.transform.Find("Navigation Panel");
            Write("诊断 background=" + (background == null ? "null" :
                    (background.sprite == null ? "no-sprite" : background.sprite.name)) +
                  " enabled=" + (background != null && background.enabled) +
                  " nav=" + (nav == null ? "null" : nav.Length.ToString()) +
                  " navPanel=" + (navPanel == null ? "missing" : navPanel.gameObject.activeInHierarchy.ToString()) +
                  " size=" + view.GetComponent<RectTransform>().rect.size);
        }

        static string ActiveId()
        {
            // 以“仍在激活的模块”为准；CloseScreen 后 UiManager 的 CurrentModuleId 可能仍是旧值。
            var module = UnityEngine.Object.FindObjectsOfType<Gxdl.Modules.UiModule>(true)
                .FirstOrDefault(m => m != null && m.gameObject.activeInHierarchy);
            return module != null ? module.ModuleId : null;
        }

        static IEnumerator Step(string name, Action action, string expected, int shot, List<string> failures)
        {
            action();
            yield return null;
            yield return null;
            Check(name + " 打开 " + expected, ActiveId() == expected, failures);
            yield return Shot(shot, name);
        }

        static IEnumerator Click(string name, int shot, List<string> failures)
        {
            var button = Find(name);
            if (button == null) { failures.Add("找不到按钮 " + name); Write("FAIL 找不到按钮 " + name); yield break; }
            button.onClick.Invoke();
            yield return null;
            yield return null;
            yield return Shot(shot, "after-" + name);
        }

        static IEnumerator Shot(int index, string name)
        {
            Directory.CreateDirectory(Path.Combine(GxdlAutoFlow.ProjectRoot, ShotDir));
            ScreenCapture.CaptureScreenshot(Path.Combine(ShotDir, index.ToString("00") + "-" + name.Replace(' ', '_') + ".png"));
            yield return null;
        }

        static Button Find(string name) =>
            UnityEngine.Object.FindObjectsOfType<Button>(true)
                .FirstOrDefault(b => b.name == name && b.gameObject.activeInHierarchy);

        static void SkipOpening()
        {
            foreach (var button in UnityEngine.Object.FindObjectsOfType<Button>(true))
            {
                if (!button.gameObject.activeInHierarchy) continue;
                var label = button.GetComponentInChildren<Text>();
                if (label != null && label.text != null && label.text.Contains("跳过"))
                {
                    button.onClick.Invoke();
                    return;
                }
            }
        }

        static void Check(string what, bool ok, List<string> failures)
        {
            Write((ok ? "PASS " : "FAIL ") + what);
            if (!ok) failures.Add(what);
        }

        static void Write(string line)
        {
            lines.Add(line);
            try
            {
                var root = GxdlAutoFlow.ProjectRoot;
                Directory.CreateDirectory(Path.Combine(root, "Logs"));
                File.AppendAllText(Path.Combine(root, ReportPath), line + Environment.NewLine);
            }
            catch { }
        }

        void Stop()
        {
            Write("== 结束，可停止 Play ==");
        }
    }
}
#endif
