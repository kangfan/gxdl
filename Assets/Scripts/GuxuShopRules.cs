using UnityEngine;

namespace Gxdl
{
    public partial class GameRules
    {
        // MVP 初版定价：统一用灵尘结算，数值待模拟与试玩后调整，界面不写死价格。
        public struct ShopGood
        {
            public string Name;
            public string Desc;
            public string Icon;
            public int Price;
            public GuxuReward Reward;
        }

        public static readonly ShopGood[] ShopGoods =
        {
            new ShopGood { Name = "破封符", Desc = "破封一次，获得一件遗宝", Icon = "resource-seal", Price = 60,
                Reward = new GuxuReward { seals = 1 } },
            new ShopGood { Name = "玄铁", Desc = "锻造装备所需", Icon = "resource-ore", Price = 40,
                Reward = new GuxuReward { ore = 2 } },
            new ShopGood { Name = "悟道点", Desc = "修习天赋所需", Icon = "resource-insight", Price = 30,
                Reward = new GuxuReward { insight = 2 } },
            new ShopGood { Name = "突破石", Desc = "境界突破所需", Icon = "resource-stone", Price = 150,
                Reward = new GuxuReward { stones = 1 } },
        };

        public bool CanBuyShopGood(int index) =>
            index >= 0 && index < ShopGoods.Length && Save.coins >= ShopGoods[index].Price;

        /// <summary>先校验再扣费再发放；任一步失败都不改余额。</summary>
        public bool BuyShopGood(int index)
        {
            if (!CanBuyShopGood(index)) return false;
            var good = ShopGoods[index];
            Save.coins -= good.Price;
            Grant(good.Reward);
            return true;
        }
    }
}
