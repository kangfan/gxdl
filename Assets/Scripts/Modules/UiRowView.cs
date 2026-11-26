using UnityEngine;
using UnityEngine.UI;

namespace Gxdl.Modules
{
    /// <summary>
    /// 标准列表行：图标、标题、副标题、数值、箭头，外加锁定与选中标记。
    /// 列表用 UiList 池化这些行，行本身只负责显示，不持有业务逻辑。
    /// </summary>
    public sealed class UiRowView : MonoBehaviour
    {
        [SerializeField] Image icon;
        [SerializeField] Text indexLabel;
        [SerializeField] Text titleText;
        [SerializeField] Text subtitleText;
        [SerializeField] Text valueText;
        [SerializeField] Text chevronText;
        [SerializeField] GameObject lockedMark;
        [SerializeField] GameObject selectedMark;
        [SerializeField] Button button;

        public Button Button => button;

        /// <summary>按真实数据刷新一行。空字符串表示隐藏对应文本。</summary>
        public void Apply(string title, string subtitle = null, string value = null,
            Sprite iconSprite = null, bool locked = false, bool selected = false, string index = null)
        {
            SetText(titleText, title);
            SetText(subtitleText, subtitle);
            SetText(valueText, value);
            SetText(indexLabel, index);

            if (icon != null)
            {
                icon.sprite = iconSprite;
                icon.enabled = iconSprite != null;
            }

            if (lockedMark != null) lockedMark.SetActive(locked);
            if (selectedMark != null) selectedMark.SetActive(selected);
            if (button != null) button.interactable = !locked;
            if (chevronText != null) chevronText.gameObject.SetActive(!locked);
        }

        /// <summary>挂点击回调。每次重绑前清空，避免池化复用时叠加监听。</summary>
        public void BindClick(System.Action handler)
        {
            if (button == null) return;
            button.onClick.RemoveAllListeners();
            if (handler != null) button.onClick.AddListener(() => handler());
        }

        static void SetText(Text target, string value)
        {
            if (target == null) return;
            target.text = value ?? string.Empty;
            target.gameObject.SetActive(!string.IsNullOrEmpty(value));
        }
    }
}
