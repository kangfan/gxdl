using Gxdl.Modules;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Gxdl.EditorTools
{
    /// <summary>
    /// 登录界面 prefab 生成器。
    /// 骨架照 GuxuAccountUI.RenderAccountState 的布局还原：
    /// 标题、协议行、两类登录入口与实名认证入口都进 prefab，
    /// 面板高度、按钮可用性与提示文案由 LoginView 在运行时按会话状态填入。
    /// </summary>
    [InitializeOnLoad]
    public static class LoginPrefabSource
    {
        public const string PrefabPath = "Assets/Resources/" + UiScreens.Login + ".prefab";

        static readonly Color Backing = new Color(0.015f, 0.045f, 0.065f);
        static readonly Color PanelColor = new Color(0.055f, 0.12f, 0.16f, 0.96f);
        static readonly Color Field = new Color(0.09f, 0.16f, 0.19f, 1f);
        static readonly Color Primary = new Color(0.80f, 0.66f, 0.36f);
        static readonly Color Secondary = new Color(0.15f, 0.38f, 0.35f);
        static readonly Color Link = new Color(0.42f, 1f, 0.95f);
        static readonly Color Placeholder = new Color(0.72f, 0.79f, 0.77f);

        static LoginPrefabSource()
        {
            ScreenPrefabKit.Register("Login", Build);
        }

        [MenuItem("Gxdl/UI/生成/登录界面", priority = 302)]
        public static void Generate()
        {
            var path = Build();
            if (path != null) EditorUtility.DisplayDialog("生成完成", "已生成\n" + path, "确定");
        }

        public static string Build()
        {
            using (var builder = new UiPrefabBuilder("LoginView",
                new Vector2(UiRootLayout.DesignWidth, UiRootLayout.DesignHeight)))
            {
                var root = builder.Root;
                AnchorUtility.Apply(root, AnchorPreset.Stretch, Vector2.zero, Vector2.zero);
                ScreenPrefabKit.AddRootComponents(root);

                // 背景先铺满，主题图运行时由控制器按资源情况替换。
                var background = builder.AnchoredNode("", "Background", AnchorPreset.Stretch, Vector2.zero)
                    .gameObject.AddComponent<Image>();
                background.color = Backing;
                background.raycastTarget = false;

                // 标题组贴顶居中，与旧实现的 65/48 起始一致。
                var title = builder.TextNode("", "Account Title", 65f, 48f, 620f, 98f, "古 墟 大 陆", 60,
                    ScreenPrefabKit.Gold, TextAnchor.MiddleCenter);
                title.name = "Account Title";
                builder.TextNode("", "Account Subtitle", 100f, 169f, 550f, 42f, "一符破古封 · 一剑问前尘", 25,
                    ScreenPrefabKit.Cream, TextAnchor.MiddleCenter);

                BuildPanel(builder, out var panel);
                BuildFooter(builder);

                var path = builder.Save(PrefabPath);
                AssetDatabase.SaveAssets();
                if (path != null) Bind(path);
                return path;
            }
        }

        static void BuildPanel(UiPrefabBuilder builder, out RectTransform panel)
        {
            // 面板固定 660 宽；高度默认按手机号登录的 690，实名模式由 View 收短。
            panel = builder.Node("", "Account Login", 45f, 290f, 660f, 690f);
            var surface = panel.gameObject.AddComponent<Image>();
            surface.color = PanelColor;
            surface.raycastTarget = true;

            builder.TextNode("Account Login", "Panel Title", 80f, 16f, 500f, 40f, "账 号 登 录", 28, ScreenPrefabKit.Gold);

            // 同意勾选做成透明按钮，文本由 View 按 Consented 改写。
            var consent = builder.Node("Account Login", "Account Consent", 42f, 65f, 292f, 42f);
            var consentCatch = consent.gameObject.AddComponent<Image>();
            consentCatch.color = new Color(0f, 0f, 0f, 0.001f);
            consentCatch.raycastTarget = true;
            consent.gameObject.AddComponent<Button>().targetGraphic = consentCatch;
            builder.TextNode("Account Login/Account Consent", "Label", 0f, 0f, 292f, 42f, "□ 我已阅读并同意", 23,
                ScreenPrefabKit.Cream);

            builder.TextNode("Account Login", "Terms", 326f, 65f, 132f, 42f, "用户协议", 23, Link, TextAnchor.MiddleCenter);
            builder.TextNode("Account Login", "And", 457f, 65f, 30f, 42f, "和", 23, ScreenPrefabKit.Gold);
            builder.TextNode("Account Login", "Privacy", 486f, 65f, 132f, 42f, "隐私政策", 23, Link, TextAnchor.MiddleCenter);

            // 文字节点自己当热区：补一个透明底图并打开射线，按钮才有可点的目标图。
            MakeClickable(builder, "Account Login/Terms");
            MakeClickable(builder, "Account Login/Privacy");

            builder.TextNode("Account Login", "Message", 40f, 110f, 580f, 42f, string.Empty, 21, ScreenPrefabKit.Cream);

            BuildTapLogin(builder);
            BuildPhoneGroup(builder);
            BuildExperienceGroup(builder);
            BuildCertifyGroup(builder);
        }

        static void BuildTapLogin(UiPrefabBuilder builder)
        {
            var tap = builder.Node("Account Login", "Account Tap Login", 55f, 155f, 550f, 103f);
            var image = tap.gameObject.AddComponent<Image>();
            image.color = Primary;
            image.raycastTarget = true;
            tap.gameObject.AddComponent<Button>().targetGraphic = image;
            builder.TextNode("Account Login/Account Tap Login", "Label", 0f, 0f, 550f, 103f, "TapTap 登录", 28,
                ScreenPrefabKit.Gold, TextAnchor.MiddleCenter);
        }

        static void BuildPhoneGroup(UiPrefabBuilder builder)
        {
            // 手机号登录组：输入框 + 获取验证码 + 登录按钮，整组按模式切换显隐。
            var group = builder.Node("Account Login", "Phone Group", 0f, 0f, 660f, 690f);
            group.gameObject.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.001f);

            builder.TextNode("Account Login/Phone Group", "Phone Mode Hint", 157f, 264f, 346f, 42f,
                "手机验证码登录 / 注册", 25, ScreenPrefabKit.Cream, TextAnchor.MiddleCenter);

            Input(builder, "Account Phone", "请输入 11 位手机号码", 55f, 318f, 550f, 11, TouchScreenKeyboardType.PhonePad);
            Input(builder, "Account Code", "6 位验证码", 55f, 408f, 337f, 6, TouchScreenKeyboardType.NumberPad);

            var send = builder.Node("Account Login/Phone Group", "Account Send Code", 410f, 408f, 195f, 72f);
            var sendImage = send.gameObject.AddComponent<Image>();
            sendImage.color = Secondary;
            sendImage.raycastTarget = true;
            send.gameObject.AddComponent<Button>().targetGraphic = sendImage;
            builder.TextNode("Account Login/Phone Group/Account Send Code", "Label", 0f, 0f, 195f, 72f, "获取验证码", 23,
                ScreenPrefabKit.Cream, TextAnchor.MiddleCenter);

            PrimaryButton(builder, "Account Login/Phone Group", "Account Phone Login", "验证并登录", 55f, 508f, 550f, 105f, 28);
            builder.TextNode("Account Login/Phone Group", "Phone Hint", 40f, 625f, 580f, 34f,
                "未注册的手机号验证成功后自动创建账号", 21, ScreenPrefabKit.Cream);
        }

        static void BuildExperienceGroup(UiPrefabBuilder builder)
        {
            // 体验账号组默认隐藏，由 View 根据登录方式切换。
            var group = builder.Node("Account Login", "Experience Group", 0f, 0f, 660f, 690f);
            group.gameObject.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.001f);
            group.gameObject.SetActive(false);

            Input(builder, "Experience User", "请输入体验账号", 55f, 318f, 550f, 32, TouchScreenKeyboardType.Default, "Account Login/Experience Group");
            Input(builder, "Experience Password", "请输入密码", 55f, 408f, 550f, 64, TouchScreenKeyboardType.Default, "Account Login/Experience Group");
            PrimaryButton(builder, "Account Login/Experience Group", "Experience Login", "账号密码登录", 55f, 508f, 550f, 105f, 28);
            builder.TextNode("Account Login/Experience Group", "Experience Hint", 40f, 625f, 580f, 34f,
                "体验账号独立保存进度，首次登录创建角色", 21, ScreenPrefabKit.Cream);
        }

        static void BuildCertifyGroup(UiPrefabBuilder builder)
        {
            var group = builder.Node("Account Login", "Certify Group", 0f, 0f, 660f, 560f);
            group.gameObject.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.001f);
            group.gameObject.SetActive(false);

            builder.TextNode("Account Login/Certify Group", "Certify Hint", 40f, 160f, 580f, 80f,
                "实名认证和防沉迷验证完成后即可进入游戏", 27, ScreenPrefabKit.Cream);
            SecondaryButton(builder, "Account Login/Certify Group", "Retry Certify", "继续实名认证", 55f, 285f, 550f, 78f, 26);
            SecondaryButton(builder, "Account Login/Certify Group", "Switch Account", "退出 / 切换账号", 55f, 395f, 550f, 78f, 26);
        }

        static void BuildFooter(UiPrefabBuilder builder)
        {
            builder.TextNode("", "Account Footer", 65f, 1235f, 620f, 40f,
                "实名信息由 TapTap 官方认证页面处理", 20, ScreenPrefabKit.Cream, TextAnchor.MiddleCenter);
        }

        /// <summary>输入框：外壳 + 文本 + 占位文字，结构与旧 AccountInput 一致。</summary>
        static InputField Input(UiPrefabBuilder builder, string name, string hint,
            float x, float y, float width, int characterLimit, TouchScreenKeyboardType keyboard, string parent = "Account Login/Phone Group")
        {
            var node = builder.Node(parent, name, x, y, width, 72f);
            var background = node.gameObject.AddComponent<Image>();
            background.color = Field;
            background.raycastTarget = true;

            var text = builder.TextNode(parent + "/" + name, "Text", 68f, 0f, width - 86f, 72f, string.Empty, 27,
                ScreenPrefabKit.Cream, TextAnchor.MiddleLeft);
            text.supportRichText = false;
            var placeholder = builder.TextNode(parent + "/" + name, "Placeholder", 68f, 0f, width - 86f, 72f, hint, 24,
                Placeholder, TextAnchor.MiddleLeft);

            var input = node.gameObject.AddComponent<InputField>();
            input.textComponent = text;
            input.placeholder = placeholder;
            input.targetGraphic = background;
            input.transition = Selectable.Transition.ColorTint;
            input.characterLimit = characterLimit;
            input.contentType = InputField.ContentType.Standard;
            input.keyboardType = keyboard;
            input.lineType = InputField.LineType.SingleLine;
            return input;
        }

        static void PrimaryButton(UiPrefabBuilder builder, string parent, string name, string label,
            float x, float y, float width, float height, int size)
        {
            Button(builder, parent, name, label, x, y, width, height, size, Primary, ScreenPrefabKit.Gold);
        }

        static void SecondaryButton(UiPrefabBuilder builder, string parent, string name, string label,
            float x, float y, float width, float height, int size)
        {
            Button(builder, parent, name, label, x, y, width, height, size, Secondary, ScreenPrefabKit.Cream);
        }

        static void Button(UiPrefabBuilder builder, string parent, string name, string label,
            float x, float y, float width, float height, int size, Color fill, Color textColor)
        {
            var node = builder.Node(parent, name, x, y, width, height);
            var image = node.gameObject.AddComponent<Image>();
            image.color = fill;
            image.raycastTarget = true;
            node.gameObject.AddComponent<Button>().targetGraphic = image;
            builder.TextNode(parent + "/" + name, "Label", 0f, 0f, width, height, label, size, textColor,
                TextAnchor.MiddleCenter);
        }

        /// <summary>文字节点自当热区：Text 本身可以接收射线，不能再叠加 Image（同节点只允许一个 Graphic）。</summary>
        static void MakeClickable(UiPrefabBuilder builder, string path)
        {
            var text = builder.GetOrAddComponent<Text>(path);
            text.raycastTarget = true;
            builder.GetOrAddComponent<Button>(path).targetGraphic = text;
        }

        static void Bind(string prefabPath)
        {
            var contents = PrefabUtility.LoadPrefabContents(prefabPath);
            if (contents == null) return;
            try
            {
                var view = contents.GetComponent<LoginView>();
                if (view == null) view = contents.AddComponent<LoginView>();

                var so = new SerializedObject(view);
                Set(so, "consentButton", Find(contents, "Account Login/Account Consent"));
                Set(so, "consentLabel", FindText(contents, "Account Login/Account Consent/Label"));
                Set(so, "termsButton", Find(contents, "Account Login/Terms"));
                Set(so, "privacyButton", Find(contents, "Account Login/Privacy"));
                Set(so, "messageText", FindText(contents, "Account Login/Message"));
                Set(so, "panel", Find(contents, "Account Login"));

                Set(so, "tapLoginButton", Find(contents, "Account Login/Account Tap Login"));
                Set(so, "tapLoginLabel", FindText(contents, "Account Login/Account Tap Login/Label"));

                Set(so, "phoneGroup", Find(contents, "Account Login/Phone Group"));
                Set(so, "phoneInput", FindComponent<InputField>(contents, "Account Login/Phone Group/Account Phone"));
                Set(so, "codeInput", FindComponent<InputField>(contents, "Account Login/Phone Group/Account Code"));
                Set(so, "sendCodeButton", Find(contents, "Account Login/Phone Group/Account Send Code"));
                Set(so, "sendCodeLabel", FindText(contents, "Account Login/Phone Group/Account Send Code/Label"));
                Set(so, "phoneLoginButton", Find(contents, "Account Login/Phone Group/Account Phone Login"));
                Set(so, "phoneHintText", FindText(contents, "Account Login/Phone Group/Phone Hint"));

                Set(so, "experienceGroup", Find(contents, "Account Login/Experience Group"));
                Set(so, "experienceUserInput", FindComponent<InputField>(contents, "Account Login/Experience Group/Experience User"));
                Set(so, "experiencePasswordInput", FindComponent<InputField>(contents, "Account Login/Experience Group/Experience Password"));
                Set(so, "experienceLoginButton", Find(contents, "Account Login/Experience Group/Experience Login"));
                Set(so, "experienceHintText", FindText(contents, "Account Login/Experience Group/Experience Hint"));

                Set(so, "certifyGroup", Find(contents, "Account Login/Certify Group"));
                Set(so, "certifyHintText", FindText(contents, "Account Login/Certify Group/Certify Hint"));
                Set(so, "retryCertifyButton", Find(contents, "Account Login/Certify Group/Retry Certify"));
                Set(so, "switchAccountButton", Find(contents, "Account Login/Certify Group/Switch Account"));

                Set(so, "footerText", FindText(contents, "Account Footer"));
                so.ApplyModifiedPropertiesWithoutUndo();

                PrefabUtility.SaveAsPrefabAsset(contents, prefabPath);
                AssetDatabase.SaveAssets();
                Debug.Log("[Login] 已生成并绑定 " + prefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contents);
            }
        }

        static Component Find(GameObject root, string path)
        {
            var node = root.transform.Find(path);
            return node;
        }

        static Text FindText(GameObject root, string path)
        {
            var node = root.transform.Find(path);
            return node != null ? node.GetComponent<Text>() : null;
        }

        static T FindComponent<T>(GameObject root, string path) where T : Component
        {
            var node = root.transform.Find(path);
            return node != null ? node.GetComponent<T>() : null;
        }

        static void Set(SerializedObject so, string field, Object value)
        {
            var property = so.FindProperty(field);
            if (property == null || value == null) return;
            property.objectReferenceValue = value;
        }
    }
}
