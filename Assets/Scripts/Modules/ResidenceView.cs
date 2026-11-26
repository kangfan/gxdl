using System;

using UnityEngine;
using UnityEngine.UI;

namespace Gxdl.Modules
{
    /// <summary>
    /// 洞府 · 修行手记视图。静态结构（背景、面板框、窗景、故事文案、铭牌框、按钮皮）
    /// 全部在 ResidenceView.prefab 内；境界与战力来自真实存档，由控制器组装快照填入。
    /// </summary>
    public sealed class ResidenceView : UiModule
    {
        public override string ModuleId => "ResidenceView";

        [Header("铭牌")]
        [SerializeField] Text realmValue;
        [SerializeField] Text powerValue;

        [Header("入口按钮")]
        [SerializeField] Button replayButton;
        [SerializeField] Button bagButton;
        [SerializeField] Button companionButton;
        [SerializeField] Button socialButton;
        [SerializeField] Button rankButton;
        [SerializeField] Button shopButton;
        [SerializeField] Button closeButton;

        public Text RealmValue => realmValue;
        public Text PowerValue => powerValue;
        public Button Replay => replayButton;
        public Button Bag => bagButton;
        public Button Companion => companionButton;
        public Button Social => socialButton;
        public Button Rank => rankButton;
        public Button Shop => shopButton;
        public Button Close => closeButton;

        /// <summary>旧版 prefab 缺字段时回退旧构建。</summary>
        public bool IsComplete =>
            realmValue != null && powerValue != null &&
            replayButton != null && bagButton != null && companionButton != null &&
            socialButton != null && rankButton != null && shopButton != null && closeButton != null;

        public void BindActions(Action replay, Action bag, Action companion, Action social,
            Action rank, Action shop, Action close)
        {
            Bind(replayButton, replay);
            Bind(bagButton, bag);
            Bind(companionButton, companion);
            Bind(socialButton, social);
            Bind(rankButton, rank);
            Bind(shopButton, shop);
            Bind(closeButton, close);
        }

        /// <summary>按真实存档刷新数值。其余文本为静态文案，已在 prefab 中。</summary>
        public void Apply(ResidenceSnapshot snapshot)
        {
            if (realmValue != null) realmValue.text = snapshot.Realm ?? string.Empty;
            if (powerValue != null) powerValue.text = snapshot.Power ?? string.Empty;
        }

        static void Bind(Button button, Action handler)
        {
            if (button == null) return;
            button.onClick.RemoveAllListeners();
            if (handler != null) button.onClick.AddListener(() => handler());
        }

        /// <summary>洞府快照。数值由控制器从 GameRules 组装，View 不读存档。</summary>
        public struct ResidenceSnapshot
        {
            public string Realm;
            public string Power;

            public static ResidenceSnapshot Capture(GameRules rules)
            {
                if (rules == null) return default;
                return new ResidenceSnapshot
                {
                    Realm = rules.Realm.displayName,
                    Power = rules.Attributes.Power.ToString(),
                };
            }
        }
    }
}
