using System;
using Gxdl.Procedures;
using Gxdl.Systems;
using UnityEngine;
using UnityEngine.UI;

namespace Gxdl.Core
{
    /// <summary>
    /// 组合根：整个游戏唯一负责「谁创建谁、谁注册谁」的地方。
    /// 所有系统在此一次性注册进 <see cref="GameContext"/>，
    /// 其余代码只按接口取用，不 new 系统、不访问 static Instance。
    /// </summary>
    public sealed class GameBootstrap : MonoBehaviour
    {
        /// <summary>全局唯一的引导实例。</summary>
        public static GameBootstrap Instance { get; private set; }

        public IEventBus Events { get; private set; }

        public UiFactory Ui { get; private set; }

        public ProcedureManager Procedures { get; private set; }

        public ISaveSystem Saves { get; private set; }

        public IConfigSystem Configs { get; private set; }

        public IAccountSystem Accounts { get; private set; }

        public IGrowthSystem Growth { get; private set; }

        public IEquipmentSystem Equipment { get; private set; }

        public IBattleSystem Battles { get; private set; }

        public IQuestSystem Quests { get; private set; }

        public IRoleSystem Roles { get; private set; }

        public ISkillSystem Skills { get; private set; }

        /// <summary>当前玩家规则实例，由加载流程注入。</summary>
        public GameRules Rules { get; private set; }

        public bool IsInitialized { get; private set; }

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
            Initialize();
        }

        void OnDestroy()
        {
            if (Instance != this) return;
            Shutdown();
            Instance = null;
        }

        /// <summary>
        /// 建立服务容器。顺序即依赖顺序：
        /// 事件与服务注册必须早于任何系统使用。
        /// </summary>
        public void Initialize()
        {
            if (IsInitialized) return;

            GameContext.Reset();

            Events = new EventBus();
            GameContext.Register<IEventBus>(Events);

            Saves = new SaveSystem(Events);
            Configs = new ConfigSystem();

            Growth = new GrowthSystem(Events, Saves);
            Equipment = new EquipmentSystem(Events, Saves);
            Battles = new BattleSystem(Events, Saves, Configs);
            Quests = new QuestSystem(Events);
            Roles = new RoleSystem(Events);
            Skills = new SkillSystem(Events);

            GameContext.Register<ISaveSystem>(Saves);
            GameContext.Register<IConfigSystem>(Configs);
            GameContext.Register<IGrowthSystem>(Growth);
            GameContext.Register<IEquipmentSystem>(Equipment);
            GameContext.Register<IBattleSystem>(Battles);
            GameContext.Register<IQuestSystem>(Quests);
            GameContext.Register<IRoleSystem>(Roles);
            GameContext.Register<ISkillSystem>(Skills);

            Skills.LoadDefaults();

            Procedures = new ProcedureManager(Events);
            GameContext.Register<ProcedureManager>(Procedures);

            IsInitialized = true;
        }

        /// <summary>
        /// 构建 UI 工厂并注册。需要 Canvas 根节点，由调用方在场景就绪后提供。
        /// </summary>
        public void InitializeUi(RectTransform canvasRoot, Font uiFont)
        {
            Ui = new UiFactory(canvasRoot, uiFont);
            GameContext.Register<UiFactory>(Ui);

            UiModules = Modules.UiManager.Create(canvasRoot, Events, Ui);
            GameContext.Register<Modules.UiManager>(UiModules);
        }

        /// <summary>prefab 界面管理器。宿主未初始化时为 null。</summary>
        public Modules.UiManager UiModules { get; private set; }
        /// <summary>
        /// 绑定账号系统并注册全部流程。
        /// 账号会话是场景里的组件，必须等它存在后才能绑定。
        /// </summary>
        public void BindAccount(GuxuAccountSession session)
        {
            var accountSystem = new AccountSystem(Events);
            accountSystem.Bind(session);
            Accounts = accountSystem;
            GameContext.Register<IAccountSystem>(Accounts);

            RegisterProcedures();
        }

        void RegisterProcedures()
        {
            if (Procedures == null) throw new InvalidOperationException("流程管理器尚未创建");

            Procedures.Register(new LoginProcedure(Accounts, Procedures));
            Procedures.Register(new LoadingProcedure(Accounts, Configs, Saves, Events, Procedures, ApplyRules));
            Procedures.Register(new LobbyProcedure(Events, Procedures, Accounts));
            Procedures.Register(new DungeonProcedure(Battles, Configs, Events, Procedures));
            Procedures.Register(new BattleProcedure(Battles, Events, Procedures));
        }

        /// <summary>
        /// 加载流程读到存档后回调这里，把规则实例分发给需要的系统。
        /// 集中在一处绑定，避免各系统重复持有或漏绑。
        /// </summary>
        void ApplyRules(GameRules rules)
        {
            Rules = rules;
            if (rules == null) return;

            if (Growth is GrowthSystem growth) growth.Bind(rules);
            if (Equipment is EquipmentSystem equipment) equipment.Bind(rules);
            if (Battles is BattleSystem battles) battles.Bind(rules);
            if (Roles is RoleSystem roles) roles.Bind(rules);

            Events.Publish(new GameEvents.PlayerStatsChanged(rules.Attributes.Power));
        }

        /// <summary>
        /// 启动流程。登录完成的判定交给流程自身，这里只负责进入初始状态。
        /// </summary>
        public bool StartFlow()
        {
            if (Procedures == null) return false;
            return Procedures.Change(ProcedureIds.Login);
        }

        void Update()
        {
            Procedures?.Update(Time.deltaTime);
        }

        void Shutdown()
        {
            Procedures?.Clear();
            Events?.Clear();
            Ui?.ClearCache();
            GameContext.Reset();
            IsInitialized = false;
        }

        /// <summary>
        /// 退出播放模式时清理静态状态，防止编辑器下连续播放残留。
        /// </summary>
#if UNITY_EDITOR
        [UnityEditor.InitializeOnLoadMethod]
        static void ResetOnPlayModeExit()
        {
            UnityEditor.EditorApplication.playModeStateChanged += state =>
            {
                if (state != UnityEditor.PlayModeStateChange.ExitingPlayMode) return;
                GameContext.Reset();
            };
        }
#endif
    }
}
