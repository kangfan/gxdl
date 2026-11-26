using Gxdl;

using UnityEngine;
using UnityEngine.UI;

namespace Gxdl.Modules
{
    /// <summary>
    /// 排行榜视图。MVP 阶段展示本机真实记录；联网排行接入后由控制器替换数据源，
    /// 结构与行节点保持不变。
    /// </summary>
    public sealed class RankView : UiModule
    {
        public override string ModuleId => "RankView";

        [SerializeField] Button backButton;
        [SerializeField] Text[] rowLabels;
        [SerializeField] Text[] rowValues;
        [SerializeField] Text[] rowDetails;
        [SerializeField] Text noteText;

        public Button Back => backButton;

        public struct RankRow
        {
            public string Label;
            public string Value;
            public string Detail;
        }

        public void Apply(RankRow[] rows, string note)
        {
            if (rows != null)
            {
                for (var i = 0; i < rows.Length; i++)
                {
                    if (rowLabels != null && i < rowLabels.Length && rowLabels[i] != null) rowLabels[i].text = rows[i].Label;
                    if (rowValues != null && i < rowValues.Length && rowValues[i] != null) rowValues[i].text = rows[i].Value;
                    if (rowDetails != null && i < rowDetails.Length && rowDetails[i] != null) rowDetails[i].text = rows[i].Detail;
                }
            }
            if (noteText != null) noteText.text = note ?? string.Empty;
        }
    }
}
