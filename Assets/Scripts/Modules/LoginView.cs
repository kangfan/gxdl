using UnityEngine;
using UnityEngine.UI;

namespace Gxdl.Modules
{
    /// <summary>
    /// 登录界面视图。只持有控件引用与展示方法，
    /// 登录/认证流程仍由 GuxuAccountSession 与宿主控制，View 不直接读存档或改状态。
    /// prefab 由 Gxdl/UI/生成/登录界面 生成。
    /// </summary>
    public sealed class LoginView : UiModule
    {
        public override string ModuleId => "LoginView";

        [Header("提示区")]
        [SerializeField] Button consentButton;
        [SerializeField] Text consentLabel;
        [SerializeField] Button termsButton;
        [SerializeField] Button privacyButton;
        [SerializeField] Text messageText;

        [Header("面板")]
        [SerializeField] RectTransform panel;

        [Header("TapTap 登录")]
        [SerializeField] Button tapLoginButton;
        [SerializeField] Text tapLoginLabel;

        [Header("手机验证码")]
        [SerializeField] RectTransform phoneGroup;
        [SerializeField] InputField phoneInput;
        [SerializeField] InputField codeInput;
        [SerializeField] Button sendCodeButton;
        [SerializeField] Text sendCodeLabel;
        [SerializeField] Button phoneLoginButton;
        [SerializeField] Text phoneHintText;

        [Header("体验账号")]
        [SerializeField] RectTransform experienceGroup;
        [SerializeField] InputField experienceUserInput;
        [SerializeField] InputField experiencePasswordInput;
        [SerializeField] Button experienceLoginButton;
        [SerializeField] Text experienceHintText;

        [Header("实名认证")]
        [SerializeField] RectTransform certifyGroup;
        [SerializeField] Text certifyHintText;
        [SerializeField] Button retryCertifyButton;
        [SerializeField] Button switchAccountButton;

        [Header("页脚")]
        [SerializeField] Text footerText;

        public Button Consent => consentButton;

        public Button Terms => termsButton;

        public Button Privacy => privacyButton;

        public Button TapLogin => tapLoginButton;

        public InputField Phone => phoneInput;

        public InputField Code => codeInput;

        public Button SendCode => sendCodeButton;

        public InputField ExperienceUser => experienceUserInput;

        public InputField ExperiencePassword => experiencePasswordInput;

        public Button ExperienceLogin => experienceLoginButton;

        public Button RetryCertify => retryCertifyButton;

        public Button SwitchAccount => switchAccountButton;

        /// <summary>按「是否实名中 + 当前登录方式」切换整块面板的布局与高度。</summary>
        public void ApplyMode(bool certify, bool experience)
        {
            if (phoneGroup != null) phoneGroup.gameObject.SetActive(!certify && !experience);
            if (experienceGroup != null) experienceGroup.gameObject.SetActive(!certify && experience);
            if (certifyGroup != null) certifyGroup.gameObject.SetActive(certify);
            if (tapLoginButton != null) tapLoginButton.gameObject.SetActive(!certify);

            // 实名模式内容更少，面板随之收短，避免下方留出空洞。
            if (panel != null)
            {
                var height = certify ? 560f : 690f;
                var size = panel.sizeDelta;
                panel.sizeDelta = new Vector2(size.x, height);
            }
        }

        /// <summary>用真实会话状态刷新整页：文案、勾选态、可用性与重发倒计时。</summary>
        public void Apply(LoginState state)
        {
            if (consentLabel != null) consentLabel.text = (state.Consented ? "☑" : "□") + " 我已阅读并同意";
            if (messageText != null) messageText.text = state.Message ?? string.Empty;
            if (footerText != null) footerText.text = state.Footer ?? string.Empty;

            var busy = state.Busy;
            if (consentButton != null) consentButton.interactable = !busy;
            // 协议/隐私链接只在配置了 https 地址时才可点，与旧界面一致。
            if (termsButton != null) termsButton.interactable = !busy && state.TermsLink;
            if (privacyButton != null) privacyButton.interactable = !busy && state.PrivacyLink;

            if (tapLoginButton != null) tapLoginButton.interactable = !busy && state.CanTap;
            if (phoneInput != null) phoneInput.interactable = !busy;
            if (codeInput != null) codeInput.interactable = !busy;
            if (phoneLoginButton != null) phoneLoginButton.interactable = !busy;
            if (experienceUserInput != null) experienceUserInput.interactable = !busy;
            if (experiencePasswordInput != null) experiencePasswordInput.interactable = !busy;
            if (experienceLoginButton != null) experienceLoginButton.interactable = !busy;
            if (retryCertifyButton != null) retryCertifyButton.interactable = !busy;
            if (switchAccountButton != null) switchAccountButton.interactable = !busy;

            ApplyResend(state.ResendSeconds, busy);
        }

        /// <summary>单独刷新重发按钮，倒计时轮询时不必整页重绘。</summary>
        public void ApplyResend(int seconds, bool busy)
        {
            if (sendCodeLabel != null) sendCodeLabel.text = seconds > 0 ? seconds + "秒后重发" : "获取验证码";
            if (sendCodeButton != null) sendCodeButton.interactable = !busy && seconds <= 0;
        }

        /// <summary>统一挂按下回调，控制器只需传入动作。</summary>
        public void BindActions(
            System.Action consent,
            System.Action terms,
            System.Action privacy,
            System.Action tapLogin,
            System.Action sendCode,
            System.Action phoneLogin,
            System.Action experienceLogin,
            System.Action retryCertify,
            System.Action switchAccount)
        {
            Bind(consentButton, consent);
            Bind(termsButton, terms);
            Bind(privacyButton, privacy);
            Bind(tapLoginButton, tapLogin);
            Bind(sendCodeButton, sendCode);
            Bind(phoneLoginButton, phoneLogin);
            Bind(experienceLoginButton, experienceLogin);
            Bind(retryCertifyButton, retryCertify);
            Bind(switchAccountButton, switchAccount);
        }

        static void Bind(Button button, System.Action handler)
        {
            if (button == null) return;
            button.onClick.RemoveAllListeners();
            if (handler != null) button.onClick.AddListener(() => handler());
        }

        /// <summary>登录页需要的会话快照，全部来自 GuxuAccountSession 与其配置。</summary>
        public struct LoginState
        {
            public bool Consented;
            public bool CanTap;
            public bool Busy;
            public string Message;
            public int ResendSeconds;
            public bool TermsLink;
            public bool PrivacyLink;
            public string Footer;

            public static LoginState Capture(GuxuAccountSession session)
            {
                var state = new LoginState();
                if (session == null) return state;

                state.Consented = session.Consented;
                state.Busy = session.IsBusy;
                // 旧界面把默认提示隐藏，这里保持一致，避免顶部出现无意义文案。
                state.Message = session.Message == "请选择登录方式" ? string.Empty : session.Message;
                state.ResendSeconds = session.ResendSeconds;
                var config = session.Config;
                if (config != null)
                {
                    state.TermsLink = GuxuAccountConfig.Https(config.termsUrl);
                    state.PrivacyLink = GuxuAccountConfig.Https(config.privacyUrl);
                }

                state.CanTap = !session.CanCertify;
                state.Footer = GuxuPhoneTestLogin.Enabled
                    ? "真机测试包 · 短信与实名验证已跳过"
                    : "实名信息由 TapTap 官方认证页面处理";
                return state;
            }
        }
    }
}
