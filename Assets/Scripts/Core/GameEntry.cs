using UnityEngine;

namespace Gxdl.Core
{
    /// <summary>
    /// 运行时入口。场景启动后创建组合根，再由组合根串联系统与流程。
    /// 与旧入口共存：旧入口负责界面渲染，这里负责服务容器与流程状态，
    /// 待界面全部迁移到 prefab 模块后，旧入口即可移除。
    /// </summary>
    public static class GameEntry
    {
        /// <summary>
        /// 是否启用新架构。关闭时游戏完全走旧路径，用于出问题时快速回退。
        /// 迁移期间保持为 true，并依靠自测夹具验证两种路径行为一致。
        /// </summary>
        public static bool UseNewArchitecture = true;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Install()
        {
            if (!UseNewArchitecture) return;
            Application.targetFrameRate = 60;
            Application.runInBackground = true;
            Screen.orientation = ScreenOrientation.Portrait;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void CreateBootstrap()
        {
            if (!UseNewArchitecture) return;
            if (GameBootstrap.Instance != null) return;

            var host = new GameObject("Game Bootstrap");
            host.AddComponent<GameBootstrap>();
        }
    }
}
