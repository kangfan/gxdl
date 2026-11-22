using UnityEngine;
using UnityEngine.UI;

namespace Gxdl.Modules
{
    /// <summary>
    /// 资源秘境总览视图。四个入口的解锁、通关层数与今日剩余次数由控制器填入，
    /// prefab 只负责 2x2 宫格与底部两个入口的静态骨架。
    /// </summary>
    public sealed class DungeonsView : UiModule
    {
        public override string ModuleId => "DungeonsView";

        [Header("头部")]
        [SerializeField] Button backButton;

        [Header("四个秘境入口")]
        [SerializeField] Button[] gateButtons;
        [SerializeField] Image[] gatePortals;
        [SerializeField] Text[] gateNames;
        [SerializeField] GameObject[] gateLocks;
        [SerializeField] Text[] gateProgress;
        [SerializeField] Text[] gateAttempts;

        [Header("底部入口")]
        [SerializeField] Button towerButton;
        [SerializeField] Button growthButton;

        public Button Back => backButton;

        public Button Tower => towerButton;

        public Button Growth => growthButton;

        public void BindGateCallbacks(System.Action<int> handler)
        {
            if (gateButtons == null || handler == null) return;
            for (var i = 0; i < gateButtons.Length; i++)
            {
                if (gateButtons[i] == null) continue;
                var index = i;
                gateButtons[i].onClick.RemoveAllListeners();
                gateButtons[i].onClick.AddListener(() => handler(index));
            }
        }

        public void BindTower(System.Action handler)
        {
            if (towerButton == null || handler == null) return;
            towerButton.onClick.RemoveAllListeners();
            towerButton.onClick.AddListener(() => handler());
        }

        public void BindGrowth(System.Action handler)
        {
            if (growthButton == null || handler == null) return;
            growthButton.onClick.RemoveAllListeners();
            growthButton.onClick.AddListener(() => handler());
        }

        /// <summary>按 Rules.Save 刷新四个入口。</summary>
        public void Apply(GateState[] gates)
        {
            var count = gates == null ? 0 : Mathf.Min(gates.Length, gateButtons == null ? 0 : gateButtons.Length);
            for (var i = 0; i < count; i++)
            {
                var gate = gates[i];
                if (gateButtons[i] != null) gateButtons[i].interactable = gate.Unlocked;

                if (gatePortals != null && i < gatePortals.Length && gatePortals[i] != null)
                {
                    gatePortals[i].color = gate.Unlocked ? Color.white : new Color(0.61f, 0.67f, 0.68f);
                }

                if (gateNames != null && i < gateNames.Length && gateNames[i] != null) gateNames[i].text = gate.Name;
                if (gateLocks != null && i < gateLocks.Length && gateLocks[i] != null) gateLocks[i].SetActive(!gate.Unlocked);
                if (gateProgress != null && i < gateProgress.Length && gateProgress[i] != null) gateProgress[i].text = gate.Progress;
                if (gateAttempts != null && i < gateAttempts.Length && gateAttempts[i] != null)
                {
                    gateAttempts[i].text = gate.Attempts;
                    // 次数用尽时用暖色提醒，与其它文案区分。
                    gateAttempts[i].color = gate.AttemptsExhausted
                        ? new Color(1f, 0.65f, 0.42f)
                        : new Color(0.94f, 0.94f, 0.87f);
                }
            }
        }

        public struct GateState
        {
            public string Name;
            public string Progress;
            public string Attempts;
            public bool Unlocked;
            public bool AttemptsExhausted;
        }
    }
}
