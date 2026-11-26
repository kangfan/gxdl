using System;
using UnityEngine;
using UnityEngine.UI;

namespace Gxdl
{
    public partial class GxdlGame
    {
        // —— 商城 ——

        bool ShowShopPrefab()
        {
            if (Gxdl.Core.GameBootstrap.Instance == null || Gxdl.Core.GameBootstrap.Instance.UiModules == null) return false;
            CloseScreen();
            var page = OpenScreen<Gxdl.Modules.ShopView>(Gxdl.Modules.UiScreens.Shop);
            if (page == null) return false;
            if (page.Back != null) page.Back.onClick.AddListener(Home);
            page.BindBuy(BuyShopItem);
            RefreshShopPage(page);
            return true;
        }

        void RefreshShopPage(Gxdl.Modules.ShopView page)
        {
            if (page == null) return;
            var cells = new Gxdl.Modules.ShopView.GoodCell[GameRules.ShopGoods.Length];
            for (var i = 0; i < cells.Length; i++)
            {
                var good = GameRules.ShopGoods[i];
                cells[i] = new Gxdl.Modules.ShopView.GoodCell
                {
                    Name = good.Name,
                    Desc = good.Desc,
                    Icon = good.Icon,
                    Price = good.Price,
                    Affordable = Rules.CanBuyShopGood(i)
                };
            }
            page.Apply(Rules.Save.coins, cells);
        }

        void BuyShopItem(int index)
        {
            if (busy || !CanPlay) return;
            if (!Rules.CanBuyShopGood(index))
            {
                Notify("灵尘不足");
                RefreshShopPage(activeModule as Gxdl.Modules.ShopView);
                return;
            }
            if (!Rules.BuyShopGood(index)) return;
            Rules.Store();
            RefreshHomeBalances();
            RefreshShopPage(activeModule as Gxdl.Modules.ShopView);
            Notify("已购买 " + GameRules.ShopGoods[index].Name);
        }

        public void ShowShop()
        {
            if (busy || !CanPlay) return;
            auto = false; battle = false; Clear(); ExpansionScreen = "shop"; Rules.EnsureExpansion();
            if (ShowShopPrefab()) return;
            // 旧构建回退：prefab 缺失时用简单弹窗保证功能可验证。
            var p = OpenModal("商城", 640);
            Txt("灵尘 " + Rules.Save.coins, p, 45, 100, 590, 40, 26, ink);
            for (var i = 0; i < GameRules.ShopGoods.Length; i++)
            {
                var index = i;
                var good = GameRules.ShopGoods[index];
                float y = 165 + index * 110;
                Txt(good.Name + " · " + good.Price + " 灵尘", p, 45, y, 380, 90, 27, ink);
                var buy = SealButton(good.Price + " 灵尘", p, 440, y + 10, 200, 64, () => { BuyShopItem(index); CloseModal(); });
                if (buy) buy.interactable = Rules.CanBuyShopGood(index);
            }
        }

        // —— 排行榜 ——

        bool ShowRankPrefab(out Gxdl.Modules.RankView page)
        {
            page = null;
            if (Gxdl.Core.GameBootstrap.Instance == null || Gxdl.Core.GameBootstrap.Instance.UiModules == null) return false;
            CloseScreen();
            page = OpenScreen<Gxdl.Modules.RankView>(Gxdl.Modules.UiScreens.Rank);
            if (page == null) return false;
            if (page.Back != null) page.Back.onClick.AddListener(Home);
            page.Apply(RankRows(), "本机记录 · 联网排行榜待服务器接入");
            return true;
        }

        Gxdl.Modules.RankView.RankRow[] RankRows()
        {
            var save = Rules.Save;
            return new[]
            {
                new Gxdl.Modules.RankView.RankRow
                {
                    Label = "基础战力", Value = Rules.Attributes.Power.ToString(),
                    Detail = "境界 " + Rules.Realm.displayName + " · 等级 " + save.level
                },
                new Gxdl.Modules.RankView.RankRow
                {
                    Label = "主线进度", Value = "第 " + Rules.HighestClear + " / 200 关",
                    Detail = save.stage > 200 ? "主线已通关" : "当前挑战第 " + save.stage + " 关"
                },
                new Gxdl.Modules.RankView.RankRow
                {
                    Label = "通天古塔", Value = "第 " + Mathf.Max(0, save.towerFloor - 1) + " / 100 层",
                    Detail = "下一层第 " + Mathf.Min(100, save.towerFloor) + " 层"
                },
            };
        }

        public void ShowRank()
        {
            if (busy || !CanPlay) return;
            auto = false; battle = false; Clear(); ExpansionScreen = "rank"; Rules.EnsureExpansion();
            if (ShowRankPrefab(out _)) return;
            var p = OpenModal("排行榜", 640);
            var rows = RankRows();
            for (var i = 0; i < rows.Length; i++)
            {
                float y = 105 + i * 130;
                Txt(rows[i].Label, p, 45, y, 200, 100, 28, ink);
                Txt(rows[i].Value, p, 260, y, 380, 50, 30, ink);
                Txt(rows[i].Detail, p, 260, y + 50, 380, 40, 22, ink);
            }
            Txt("本机记录 · 联网排行榜待服务器接入", p, 45, 520, 590, 40, 22, ink);
        }

        // —— 聊天：复用现有好友与私聊界面，只做入口与不可用状态提示 ——

        public void ShowChat()
        {
            ShowSocial();
        }
    }
}
