using Gxdl.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Gxdl.Modules
{
    /// <summary>
    /// 天赋树界面视图。三系各八节点全部在 prefab 里，结构相同、数据不同；
    /// 等级、前置锁定、选中与按钮可用性由控制器按真实存档逐节点填入。
    /// 索引约定：扁平节点号 = 系别 * 8 + 系内序号，与存档 talents[24] 一致。
    /// </summary>
    public sealed class TalentsView : UiModule
    {
        public override string ModuleId => "TalentsView";

        public const int BranchCount = 3;
        public const int NodesPerBranch = 8;

        [Header("顶栏")]
        [SerializeField] Button backButton;
        [SerializeField] Text insightText;

        [Header("系别页签")]
        [SerializeField] Transform branchRoot;
        [SerializeField] Button[] branchButtons;
        [SerializeField] Text[] branchLabels;

        /// <summary>系别页容器：三系各一页，切页时直接平移。</summary>
        [Header("系别页")]
        [SerializeField] RectTransform mapContent;
        [SerializeField] GameObject[] branchPages;

        [Header("天赋节点（3 x 8，按系别顺序排布）")]
        [SerializeField] Button[] nodeButtons;
        [SerializeField] Image[] nodeRings;
        [SerializeField] Image[] nodeBases;
        [SerializeField] Image[] nodeSymbols;
        [SerializeField] Image[] nodeLocks;
        [SerializeField] Text[] nodeNames;
        [SerializeField] Text[] nodeRanks;
        [SerializeField] Image[] nodePips;

        [Header("底部详情")]
        [SerializeField] Image detailSymbol;
        [SerializeField] Text detailName;
        [SerializeField] Text detailRank;
        [SerializeField] Text detailEffect;
        [SerializeField] Text detailRequirement;
        [SerializeField] Text detailCost;
        [SerializeField] Text detailBalance;
        [SerializeField] Button trainButton;
        [SerializeField] Text trainLabel;
        [SerializeField] Button resetButton;
        [SerializeField] Button insightButton;

        public Button Back => backButton;

        public Button Reset => resetButton;

        public Button GetInsight => insightButton;

        public Button Train => trainButton;

        /// <summary>三系页签回调，参数为系别（0 攻伐 / 1 御守 / 2 灵息）。</summary>
        public void BindBranchCallbacks(System.Action<int> handler)
        {
            if (branchButtons == null || handler == null) return;
            for (var i = 0; i < branchButtons.Length; i++)
            {
                if (branchButtons[i] == null) continue;
                var index = i;
                branchButtons[i].onClick.RemoveAllListeners();
                branchButtons[i].onClick.AddListener(() => handler(index));
            }
        }

        /// <summary>节点回调，参数为扁平节点号（0—23）。</summary>
        public void BindNodeCallbacks(System.Action<int> handler)
        {
            if (nodeButtons == null || handler == null) return;
            for (var i = 0; i < nodeButtons.Length; i++)
            {
                if (nodeButtons[i] == null) continue;
                var index = i;
                nodeButtons[i].onClick.RemoveAllListeners();
                nodeButtons[i].onClick.AddListener(() => handler(index));
            }
        }

        /// <summary>
        /// 只切换当前系别：页签外观、显示哪一页、选中环，
        /// 节点与页签全部常驻，避免切页时重建布局。
        /// </summary>
        public void SetBranch(int branch, BranchCell[] branches)
        {
            if (branch < 0 || branch >= BranchCount) return;
            if (branches != null)
            {
                var count = Mathf.Min(branches.Length, branchButtons == null ? 0 : branchButtons.Length);
                for (var i = 0; i < count; i++)
                {
                    Set(branchLabels, i, branches[i].Label);
                    if (branchButtons[i] == null) continue;
                    branchButtons[i].targetGraphic.color = branches[i].Active
                        ? new Color(0.86f, 0.78f, 0.54f, 0.94f)
                        : new Color(0.15f, 0.38f, 0.35f, 0.94f);
                    if (branchLabels != null && i < branchLabels.Length && branchLabels[i] != null)
                    {
                        branchLabels[i].color = branches[i].Active ? UiFactory.Ink : UiFactory.Cream;
                    }
                }
            }

            if (mapContent != null) mapContent.anchoredPosition = new Vector2(branch * -UiRootLayout.DesignWidth, 0f);
            if (branchPages == null) return;
            for (var i = 0; i < branchPages.Length; i++)
            {
                if (branchPages[i] != null) branchPages[i].SetActive(i == branch);
            }
        }

        /// <summary>逐节点填充等级、锁定、选中与等级点。</summary>
        public void SetNodes(NodeCell[] nodes)
        {
            if (nodes == null) return;
            var count = Mathf.Min(nodes.Length, nodeButtons == null ? 0 : nodeButtons.Length);
            for (var i = 0; i < count; i++)
            {
                var node = nodes[i];
                var index = node.Index;
                Set(nodeNames, i, node.Name);
                Set(nodeRanks, i, node.RankLabel);
                if (At(nodeNames, i) != null) nodeNames[i].color = node.Selected ? UiFactory.Gold : UiFactory.Cream;
                if (At(nodeRanks, i) != null) nodeRanks[i].color = node.Maxed ? UiFactory.Gold : UiFactory.Cream;

                Toggle(At(nodeRings, i), node.Selected);
                Toggle(At(nodeLocks, i), node.Locked);

                if (At(nodeBases, i) != null)
                {
                    nodeBases[i].color = node.Locked
                        ? new Color(0.66f, 0.76f, 0.80f, 1f)
                        : node.Rank > 0 ? new Color(0.78f, 1f, 0.92f, 1f) : Color.white;
                }

                if (At(nodeSymbols, i) != null)
                {
                    nodeSymbols[i].color = node.Locked ? new Color(0.66f, 0.76f, 0.80f, 1f) : Color.white;
                }

                // 等级点按「扁平节点号 x 3」平铺：每个节点固定三个点，与 nodePips 数组顺序一致。
                var pipBase = index * 3;
                for (var pip = 0; pip < 3; pip++)
                {
                    var filled = pip < node.Rank;
                    if (At(nodePips, pipBase + pip) != null)
                    {
                        nodePips[pipBase + pip].color = filled
                            ? new Color(1f, 0.85f, 0.45f, 1f)
                            : new Color(0.24f, 0.37f, 0.42f, 1f);
                    }
                }

                if (nodeButtons[i] != null) nodeButtons[i].name = "Talent " + index;
            }
        }

        /// <summary>底部所选节点详情与修习可用性。</summary>
        public void SetDetail(DetailCell detail)
        {
            Set(detailName, detail.Name);
            Set(detailRank, detail.RankLabel);
            Set(detailEffect, detail.Effect);
            Set(detailRequirement, detail.Requirement);
            Set(detailCost, detail.Cost);
            Set(detailBalance, detail.Balance);
            Set(trainLabel, detail.TrainLabel);
            if (detailSymbol != null && detail.Symbol != null) detailSymbol.sprite = detail.Symbol;
            if (trainButton != null) trainButton.interactable = detail.TrainEnabled;
        }

        public void SetInsight(string value)
        {
            Set(insightText, value);
        }

        public struct BranchCell
        {
            public string Label;
            public bool Active;
        }

        public struct NodeCell
        {
            public int Index;
            public string Name;
            public int Rank;
            public string RankLabel;
            public bool Locked;
            public bool Selected;
            public bool Maxed;
        }

        public struct DetailCell
        {
            public string Name;
            public string RankLabel;
            public string Effect;
            public string Requirement;
            public string Cost;
            public string Balance;
            public string TrainLabel;
            public Sprite Symbol;
            public bool TrainEnabled;
        }

        static Image At(Image[] source, int index) =>
            source != null && index >= 0 && index < source.Length ? source[index] : null;

        static Text At(Text[] source, int index) =>
            source != null && index >= 0 && index < source.Length ? source[index] : null;

        static void Set(Text target, string value)
        {
            if (target != null) target.text = value;
        }

        static void Set(Text[] target, int index, string value)
        {
            var node = At(target, index);
            if (node != null) node.text = value;
        }

        static void Set(Button button, string name)
        {
            if (button != null) button.name = name;
        }

        static void Toggle(Component target, bool visible)
        {
            if (target != null) target.gameObject.SetActive(visible);
        }
    }
}