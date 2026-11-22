using System;

namespace Gxdl.Systems
{
    public interface IConfigSystem
    {
        /// <summary>装备、境界、章节等基础配置。首次访问时加载并缓存。</summary>
        OriginalConfig Game { get; }

        /// <summary>关卡、区域、副本编队配置。首次访问时加载并缓存。</summary>
        GuxuStageConfig Stages { get; }

        /// <summary>强制重新加载，仅在编辑器调整配置后使用。</summary>
        void Reload();

        /// <summary>
        /// 配置完整性校验。启动时调用一次，配置缺字段应尽早失败，
        /// 而不是等到某个界面打开时才崩。
        /// </summary>
        bool Validate(out string error);
    }

    /// <summary>
    /// 配置系统。集中管理 Resources 里的 JSON 配置加载与缓存，
    /// 避免各处重复 Resources.Load 与反序列化。
    /// </summary>
    public sealed class ConfigSystem : IConfigSystem
    {
        OriginalConfig game;
        GuxuStageConfig stages;

        public OriginalConfig Game => game ?? (game = OriginalConfig.Load());

        public GuxuStageConfig Stages => stages ?? (stages = GuxuStageConfig.Load());

        public void Reload()
        {
            game = null;
            stages = null;
        }

        /// <summary>
        /// 配置完整性校验。启动时调用一次，配置缺字段应尽早失败，
        /// 而不是等到某个界面打开时才崩。
        /// </summary>
        public bool Validate(out string error)
        {
            try
            {
                var config = Game;
                if (config == null) { error = "原始配置为空"; return false; }
                if (config.realms == null || config.realms.Length == 0) { error = "境界配置缺失"; return false; }
                if (config.equipment == null || config.equipment.Length == 0) { error = "装备配置缺失"; return false; }
                if (config.stages == null || config.stages.Length == 0) { error = "关卡配置缺失"; return false; }

                var stageConfig = Stages;
                if (stageConfig == null) { error = "关卡表为空"; return false; }
                if (stageConfig.stages == null || stageConfig.stages.Length == 0) { error = "主线关卡缺失"; return false; }
                if (stageConfig.regions == null || stageConfig.regions.Length == 0) { error = "区域配置缺失"; return false; }

                error = null;
                return true;
            }
            catch (Exception exception)
            {
                error = exception.Message;
                return false;
            }
        }
    }
}
