using UnityEngine;

namespace Gxdl.Modules
{
    /// <summary>
    /// 界面 prefab 路径与标识的集中登记。
    /// 生成器、控制器与宿主共用同一份常量，避免路径字符串散落各处。
    /// </summary>
    public static class UiScreens
    {
        public const string Root = "UI/";

        public const string Login = Root + "LoginView";
        public const string Home = Root + "HomeView";
        public const string WorldMap = Root + "WorldMapView";
        public const string ChapterMap = Root + "ChapterMapView";
        public const string Dungeons = Root + "DungeonsView";
        public const string DungeonDetail = Root + "DungeonDetailView";
        public const string Tower = Root + "TowerView";
        public const string GrowthHub = Root + "GrowthHubView";
        public const string Talents = Root + "TalentsView";
        public const string Gems = Root + "GemsView";
        public const string Forge = Root + "ForgeView";
        public const string Loadout = Root + "LoadoutView";
        public const string Codex = Root + "CodexView";
        public const string BattlePrep = Root + "BattlePrepView";
        public const string BattleHud = Root + "BattleHudView";
        public const string Settlement = Root + "SettlementView";
        public const string Difficulty = Root + "DifficultyView";
        public const string Social = Root + "SocialView";
        public const string Residence = Root + "ResidenceView";
        public const string Companion = Root + "CompanionView";
        public const string Loading = Root + "LoadingView";
        public const string Shop = Root + "ShopView";
        public const string Rank = Root + "RankView";
        public const string Bag = Root + "BagView";

        /// <summary>全部界面路径，供生成器批量生成与自检遍历。</summary>
        public static readonly string[] All =
        {
            Login, Home, WorldMap, ChapterMap, Dungeons, DungeonDetail, Tower,
            GrowthHub, Talents, Gems, Forge, Loadout, Codex, BattlePrep,
            BattleHud, Settlement, Difficulty, Social, Residence, Companion, Loading,
            Shop, Rank
        };

        /// <summary>从 Resources 路径还原界面标识，用于日志与埋点。</summary>
        public static string IdOf(string prefabPath)
        {
            if (string.IsNullOrEmpty(prefabPath)) return string.Empty;
            var index = prefabPath.LastIndexOf('/');
            return index < 0 ? prefabPath : prefabPath.Substring(index + 1);
        }
    }
}
