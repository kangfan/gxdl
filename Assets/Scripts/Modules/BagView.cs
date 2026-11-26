using System;
using System.Collections;

using UnityEngine;
using UnityEngine.UI;

namespace Gxdl.Modules
{
    /// <summary>
    /// 背包视图。面板、12 个装备槽（图标与阶数角标动态）、装备详情子面板都在 prefab 内。
    /// 数值一律由控制器从 GameRules 组装后传入；点击槽位弹详情（带缩放动画），关闭回背包。
    /// </summary>
    public sealed class BagView : UiModule
    {
        public override string ModuleId => "BagView";

        [Header("背包面板")]
        [SerializeField] RectTransform bagPanel;
        [SerializeField] Button closeButton;

        [Header("装备槽")]
        [SerializeField] Button[] slotButtons;
        [SerializeField] Image[] slotIcons;
        [SerializeField] Text[] slotBadges;

        [Header("装备详情")]
        [SerializeField] RectTransform detailPanel;
        [SerializeField] Image detailIcon;
        [SerializeField] Text detailBadge;
        [SerializeField] Text detailName;
        [SerializeField] Text detailSubtitle;
        [SerializeField] Text[] statValues;
        [SerializeField] Text affixValue;
        [SerializeField] Text descLine;
        [SerializeField] Button forgeButton;
        [SerializeField] Button gemButton;
        [SerializeField] Button detailCloseButton;

        const float OpenSeconds = 0.16f;
        const float CloseSeconds = 0.1f;

        /// <summary>装备详情快照。全部真实数值由控制器组装。</summary>
        public struct GearDetail
        {
            public int Slot;
            public string IconKey;
            public string Badge;
            public string Name;
            public string Subtitle;
            public string[] Stats;
            public string Affix;
            public string Desc;
        }

        public bool IsDetailOpen => detailPanel != null && detailPanel.gameObject.activeSelf;

        /// <summary>旧版 prefab 缺字段时回退旧构建。</summary>
        public bool IsComplete =>
            bagPanel != null && closeButton != null &&
            slotButtons != null && slotButtons.Length == 12 &&
            slotIcons != null && slotIcons.Length == 12 && slotBadges != null && slotBadges.Length == 12 &&
            detailPanel != null && detailName != null && forgeButton != null && gemButton != null;

        /// <summary>填充 12 个槽位。图标按槽位取真实装备图，阶数角标文字来自真实存档。</summary>
        public void Apply(string[] badges)
        {
            if (badges == null) return;
            for (var i = 0; i < 12; i++)
            {
                if (slotIcons != null && i < slotIcons.Length && slotIcons[i] != null)
                {
                    slotIcons[i].sprite = GuxuUiArt.Item(i);
                    slotIcons[i].enabled = slotIcons[i].sprite != null;
                }
                if (slotBadges != null && i < slotBadges.Length && slotBadges[i] != null)
                {
                    slotBadges[i].text = i < badges.Length ? badges[i] ?? string.Empty : string.Empty;
                    slotBadges[i].transform.parent.gameObject.SetActive(!string.IsNullOrEmpty(slotBadges[i].text));
                }
            }
        }

        public void BindActions(Action close, Action<int> slotClicked, Action<int> forge, Action<int> gem, Action detailClose)
        {
            Bind(closeButton, close);
            Bind(forgeButton, forge);
            Bind(gemButton, gem);
            Bind(detailCloseButton, detailClose);
            if (slotButtons == null || slotClicked == null) return;
            for (var i = 0; i < slotButtons.Length; i++)
            {
                if (slotButtons[i] == null) continue;
                var index = i;
                slotButtons[i].onClick.RemoveAllListeners();
                slotButtons[i].onClick.AddListener(() => slotClicked(index));
            }
        }

        /// <summary>打开装备详情（缩放入场）。数据由控制器传入，View 不读存档。</summary>
        public void ShowDetail(GearDetail detail)
        {
            if (detailPanel == null) return;
            if (detailIcon != null)
            {
                detailIcon.sprite = GuxuUiArt.Item(detail.Slot);
                detailIcon.enabled = detailIcon.sprite != null;
            }
            SetText(detailBadge, detail.Badge);
            detailBadge.transform.parent.gameObject.SetActive(!string.IsNullOrEmpty(detail.Badge));
            SetText(detailName, detail.Name);
            SetText(detailSubtitle, detail.Subtitle);
            if (statValues != null)
            {
                for (var i = 0; i < statValues.Length; i++)
                    SetText(statValues[i], detail.Stats != null && i < detail.Stats.Length ? detail.Stats[i] : string.Empty);
            }
            SetText(affixValue, detail.Affix);
            SetText(descLine, detail.Desc);
            detailPanel.gameObject.SetActive(true);
            PlayScale(detailPanel, 0.75f, 1f, OpenSeconds);
        }

        public void HideDetail()
        {
            if (detailPanel == null || !detailPanel.gameObject.activeSelf) return;
            HideDetailAnimated();
        }

        public override void OnShow()
        {
            if (bagPanel != null) PlayScale(bagPanel, 0.8f, 1f, OpenSeconds);
            if (detailPanel != null) detailPanel.gameObject.SetActive(false);
        }

        /// <summary>关闭按钮先播缩放再回调真实关闭。回调里做关页。</summary>
        public void CloseAnimated(Action after)
        {
            StartClose(bagPanel, after);
        }

        void HideDetailAnimated()
        {
            StartClose(detailPanel, () =>
            {
                if (detailPanel != null) detailPanel.gameObject.SetActive(false);
                if (bagPanel != null) bagPanel.localScale = Vector3.one;
            });
        }

        void SetText(Text text, string value)
        {
            if (text != null) text.text = value ?? string.Empty;
        }

        static void Bind(Button button, Action handler)
        {
            if (button == null) return;
            button.onClick.RemoveAllListeners();
            if (handler != null) button.onClick.AddListener(() => handler());
        }

        static void Bind(Button button, Action<int> handler)
        {
            if (button == null || handler == null) return;
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => handler(0));
        }

        IEnumerator ScaleTo(RectTransform target, float from, float to, float seconds)
        {
            var elapsed = 0f;
            while (elapsed < seconds && target != null)
            {
                elapsed += Time.unscaledDeltaTime;
                var t = Mathf.Clamp01(elapsed / seconds);
                var eased = 1f - (1f - t) * (1f - t);
                target.localScale = Vector3.one * Mathf.Lerp(from, to, eased);
                yield return null;
            }
            if (target != null) target.localScale = Vector3.one * to;
        }

        void PlayScale(RectTransform target, float from, float to, float seconds)
        {
            if (target == null) return;
            StopAllCoroutinesFor(target);
            var routine = ScaleTo(target, from, to, seconds);
            var host = target.GetComponent<UiScaleHost>() ?? target.gameObject.AddComponent<UiScaleHost>();
            host.Play(routine);
        }

        void StartClose(RectTransform target, Action after)
        {
            if (target == null) { after?.Invoke(); return; }
            StopAllCoroutinesFor(target);
            var host = target.GetComponent<UiScaleHost>() ?? target.gameObject.AddComponent<UiScaleHost>();
            host.Play(CloseRoutine(target, after));
        }

        IEnumerator CloseRoutine(RectTransform target, Action after)
        {
            yield return ScaleTo(target, 1f, 0.88f, CloseSeconds);
            after?.Invoke();
        }

        void StopAllCoroutinesFor(RectTransform target)
        {
            if (target == null) return;
            var host = target.GetComponent<UiScaleHost>();
            if (host != null) host.Stop();
        }

        /// <summary>缩放动画宿主：动画协程挂在目标节点上，随节点销毁自动终止。</summary>
        sealed class UiScaleHost : MonoBehaviour
        {
            Coroutine running;

            public void Play(IEnumerator routine)
            {
                if (running != null) StopCoroutine(running);
                running = StartCoroutine(routine);
            }

            public void Stop()
            {
                if (running != null) StopCoroutine(running);
                running = null;
            }
        }
    }
}
