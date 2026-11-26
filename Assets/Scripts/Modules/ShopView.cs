using Gxdl;

using UnityEngine;
using UnityEngine.UI;

namespace Gxdl.Modules
{
    /// <summary>
    /// 商城视图。商品行固定在 prefab 里，名称、价格、图标与可买状态由 Apply 填入。
    /// </summary>
    public sealed class ShopView : UiModule
    {
        public override string ModuleId => "ShopView";

        [SerializeField] Button backButton;
        [SerializeField] Text coinText;
        [SerializeField] Image[] goodIcons;
        [SerializeField] Text[] goodNames;
        [SerializeField] Text[] goodDescs;
        [SerializeField] Text[] goodPrices;
        [SerializeField] Button[] buyButtons;

        public Button Back => backButton;

        public void BindBuy(System.Action<int> handler)
        {
            if (buyButtons == null || handler == null) return;
            for (var i = 0; i < buyButtons.Length; i++)
            {
                if (buyButtons[i] == null) continue;
                var index = i;
                buyButtons[i].onClick.RemoveAllListeners();
                buyButtons[i].onClick.AddListener(() => handler(index));
            }
        }

        public struct GoodCell
        {
            public string Name;
            public string Desc;
            public string Icon;
            public int Price;
            public bool Affordable;
        }

        public void Apply(int coins, GoodCell[] goods)
        {
            if (coinText != null) coinText.text = coins.ToString();
            if (goods == null) return;
            for (var i = 0; i < goods.Length; i++)
            {
                var cell = goods[i];
                if (goodIcons != null && i < goodIcons.Length && goodIcons[i] != null)
                {
                    goodIcons[i].sprite = GuxuItemIcons.Get(cell.Icon);
                }
                if (goodNames != null && i < goodNames.Length && goodNames[i] != null) goodNames[i].text = cell.Name;
                if (goodDescs != null && i < goodDescs.Length && goodDescs[i] != null) goodDescs[i].text = cell.Desc;
                if (goodPrices != null && i < goodPrices.Length && goodPrices[i] != null) goodPrices[i].text = cell.Price + " 灵尘";
                if (buyButtons != null && i < buyButtons.Length && buyButtons[i] != null)
                {
                    buyButtons[i].interactable = cell.Affordable;
                }
            }
        }
    }
}
