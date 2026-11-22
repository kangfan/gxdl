using Gxdl.Modules;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Gxdl.EditorTools
{
    /// <summary>
    /// 战斗 HUD prefab 生成器。
    /// 只还原 GuxuCombatView.StartBattle 的静态布局：血条轨道、数值文本、按钮。
    /// 逐帧数值由 BattleHudView.Set* 写入，战斗逻辑与动画不进入 prefab。
    /// </summary>
    [InitializeOnLoad]
    public static class BattleHudPrefabSource
    {
        public const string PrefabPath = "Assets/Resources/" + UiScreens.BattleHud + ".prefab";

        const float PlayerBarWidth = BattleHudView.PlayerHealthWidth;
        const float EnemyBarWidth = BattleHudView.EnemyHealthWidth;

        static readonly Color Cream = new Color(0.94f, 0.94f, 0.87f);
        static readonly Color Gold = new Color(0.86f, 0.78f, 0.54f);
        static readonly Color PlayerHp = new Color(0.20f, 0.87f, 0.60f);
        static readonly Color EnemyHp = new Color(0.95f, 0.72f, 0.28f);
        static readonly Color ShieldTint = new Color(0.26f, 0.75f, 0.94f);

        static BattleHudPrefabSource()
        {
            ScreenPrefabKit.Register("BattleHud", Build);
        }

        [MenuItem("Gxdl/UI/生成/战斗 HUD", priority = 303)]
        public static void Generate()
        {
            var path = Build();
            if (path != null) EditorUtility.DisplayDialog("生成完成", "已生成\n" + path, "确定");
        }

        public static string Build()
        {
            using (var builder = new UiPrefabBuilder("BattleHudView",
                new Vector2(UiRootLayout.DesignWidth, UiRootLayout.DesignHeight)))
            {
                var root = builder.Root;
                AnchorUtility.Apply(root, AnchorPreset.Stretch, Vector2.zero, Vector2.zero);
                ScreenPrefabKit.AddRootComponents(root);

                BuildTop(builder);
                BuildLower(builder);

                var path = builder.Save(PrefabPath);
                AssetDatabase.SaveAssets();
                if (path != null) Bind(path);
                return path;
            }
        }

        static void BuildTop(UiPrefabBuilder builder)
        {
            // 顶部柔化底板贴顶，宽度随屏幕。
            var haze = builder.AnchoredNode("", "Battle Navigation Soft Backing", AnchorPreset.StretchTop,
                new Vector2(0f, 300f), new Vector2(0f, 65f));
            var hazeImage = haze.gameObject.AddComponent<Image>();
            hazeImage.color = new Color(0.025f, 0.10f, 0.15f, 0.65f);
            hazeImage.raycastTarget = false;

            var region = builder.TextNode("", "Battle Region", 142f, 8f, 460f, 52f, string.Empty, 27, Gold,
                TextAnchor.MiddleCenter);
            AddOutline(region);

            AddAction(builder, "Battle Exit", 18f, 0f, 116f, 88f);
            AddOutline(builder.TextNode("Battle Exit", "Label", 0f, 8f, 116f, 52f, "‹ 退出", 27, Cream,
                TextAnchor.MiddleCenter));

            AddAction(builder, "Battle Speed", 606f, 0f, 138f, 88f);
            AddOutline(builder.TextNode("Battle Speed", "Label", 0f, 8f, 138f, 52f, "×1 加速", 27, Cream,
                TextAnchor.MiddleCenter));

            var portrait = builder.ImageNode("", "Battle Enemy Portrait", 42f, 98f, 88f, 88f,
                new Color(0.08f, 0.24f, 0.23f));
            portrait.raycastTarget = false;

            var name = builder.TextNode("", "Battle Enemy Name", 145f, 67f, 550f, 43f, string.Empty, 34, Gold,
                TextAnchor.MiddleCenter);
            AddOutline(name);
            var value = builder.TextNode("", "Enemy Health Value", 145f, 107f, 550f, 34f, string.Empty, 26, Cream,
                TextAnchor.MiddleCenter);
            AddOutline(value);

            BuildTrack(builder, "", 145f, 144f, EnemyBarWidth, 14f);
            var bar = builder.ImageNode("", "Enemy Health Bar", 145f, 146f, EnemyBarWidth, 10f, EnemyHp);
            bar.type = Image.Type.Filled;
            bar.fillMethod = Image.FillMethod.Horizontal;
            var shield = builder.ImageNode("", "Enemy Shield Bar", 145f, 164f, EnemyBarWidth, 6f, ShieldTint);
            shield.rectTransform.pivot = new Vector2(0f, 0.5f);
            shield.rectTransform.sizeDelta = new Vector2(0f, EnemyBarWidth);

            var status = builder.TextNode("", "Enemy Combat Status", 145f, 174f, 550f, 32f, string.Empty, 23, Cream,
                TextAnchor.MiddleCenter);
            AddOutline(status);

            var round = builder.TextNode("", "Battle Round", 130f, 211f, 490f, 38f, string.Empty, 25, Gold,
                TextAnchor.MiddleCenter);
            AddOutline(round);

            var eventLabel = builder.TextNode("", "Battle Event", 55f, 252f, 640f, 36f, string.Empty, 23, Cream,
                TextAnchor.MiddleCenter);
            AddOutline(eventLabel);

            var warning = builder.TextNode("", "Boss Warning", 45f, 291f, 660f, 39f, string.Empty, 25,
                new Color(1f, 0.67f, 0.34f), TextAnchor.MiddleCenter);
            AddOutline(warning);
        }

        static void BuildLower(UiPrefabBuilder builder)
        {
            var lower = builder.AnchoredNode("", "Battle Lower HUD", AnchorPreset.StretchBottom, new Vector2(0f, 280f));
            var haze = builder.Node("Battle Lower HUD", "Battle Controls Soft Backing", -30f, -5f, 810f, 310f);
            var hazeImage = haze.gameObject.AddComponent<Image>();
            hazeImage.color = new Color(0.025f, 0.10f, 0.15f, 0.78f);
            hazeImage.raycastTarget = false;

            AddPortrait(builder, "Battle Player Portrait", 48f, 8f, 78f);
            var name = builder.TextNode("Battle Lower HUD", "Player Name", 149f, 4f, 125f, 38f, "寻墟者", 28, Gold);
            AddOutline(name);

            BuildTrack(builder, "Battle Lower HUD", 270f, 24f, PlayerBarWidth, 14f);
            var bar = builder.ImageNode("Battle Lower HUD", "Player Health Bar", 270f, 26f, PlayerBarWidth, 10f, PlayerHp);
            bar.type = Image.Type.Filled;
            bar.fillMethod = Image.FillMethod.Horizontal;

            var value = builder.TextNode("Battle Lower HUD", "Player Health Value", 570f, 9f, 174f, 38f, string.Empty, 24,
                Cream);
            AddOutline(value);

            // 护盾条初始宽度为 0，高度字段承载满宽基准，由 SetShield 按比例回填。
            var shield = builder.ImageNode("Battle Lower HUD", "Player Shield Bar", 270f, 44f, PlayerBarWidth, 7f,
                ShieldTint);
            shield.rectTransform.pivot = new Vector2(0f, 0.5f);
            shield.rectTransform.sizeDelta = new Vector2(0f, PlayerBarWidth);

            builder.ImageNode("Battle Lower HUD", "Player Shield Icon", 148f, 53f, 29f, 29f, ShieldTint).raycastTarget = false;
            var status = builder.TextNode("Battle Lower HUD", "Player Combat Status", 185f, 52f, 530f, 36f, string.Empty, 24,
                Cream);
            AddOutline(status);

            var state = builder.TextNode("Battle Lower HUD", "Battle Skill State", 261f, 154f, 240f, 36f, string.Empty, 25,
                Cream);
            AddOutline(state);

            AddAction(builder, "Battle Lower HUD", "Cast Skill", 132f, 96f, 366f, 132f);
            builder.ImageNode("Battle Lower HUD/Cast Skill", "Skill Seal Art", 10f, 10f, 104f, 104f,
                new Color(0.12f, 0.22f, 0.22f, 0.94f)).raycastTarget = false;

            // 冷却环按 0..1 填充，锚在底层 HUD 而不是按钮内，原界面即如此。
            var ring = builder.ImageNode("Battle Lower HUD", "Skill Cooldown Ring", 140f, 104f, 108f, 108f, Color.clear);
            ring.type = Image.Type.Filled;
            ring.fillMethod = Image.FillMethod.Radial360;
            ring.fillOrigin = 2;
            ring.fillClockwise = false;
            ring.enabled = false;
            ring.raycastTarget = false;

            var icon = builder.ImageNode("Battle Lower HUD", "Battle Skill Icon", 154f, 114f, 80f, 80f, Color.white);
            icon.raycastTarget = false;
            var skillName = builder.TextNode("Battle Lower HUD", "Battle Skill Name", 261f, 111f, 230f, 40f, string.Empty, 29,
                Gold);
            AddOutline(skillName);

            BuildAutoToggle(builder);

            builder.ImageNode("Battle Lower HUD", "Statistics Rule", 40f, 232f, 670f, 1f, new Color(0.79f, 0.68f, 0.40f, 0.55f));
            var stats = builder.TextNode("Battle Lower HUD", "Battle Statistics", 38f, 238f, 674f, 34f, string.Empty, 23,
                Cream, TextAnchor.MiddleCenter);
            AddOutline(stats);
        }

        static void BuildAutoToggle(UiPrefabBuilder builder)
        {
            AddAction(builder, "Battle Lower HUD", "Auto Skill", 503f, 111f, 220f, 104f);
            var track = builder.ImageNode("Battle Lower HUD/Auto Skill", "Toggle Track", 4f, 40f, 64f, 24f,
                new Color(0.12f, 0.22f, 0.22f, 0.94f));
            track.raycastTarget = false;

            var thumb = builder.Node("Battle Lower HUD/Auto Skill", "Toggle Thumb", 47f, 43f, 18f, 18f);
            var disc = thumb.gameObject.AddComponent<GuxuPortraitDisc>();
            disc.color = Cream;
            disc.raycastTarget = false;

            builder.TextNode("Battle Lower HUD/Auto Skill", "Toggle Caption", 80f, 0f, 136f, 104f, "自动技能", 24, Cream);
        }

        static void BuildTrack(UiPrefabBuilder builder, string parent, float x, float y, float width, float height)
        {
            var track = builder.ImageNode(parent, parent.Length == 0 ? "Enemy Health Track" : "Player Health Track",
                x, y, width, height, new Color(0.02f, 0.07f, 0.09f, 0.82f));
            track.raycastTarget = false;
            var frame = builder.ImageNode(parent, parent.Length == 0 ? "Enemy Health Frame" : "Player Health Frame",
                x - 18f, y + (height - 38f) * 0.5f, width + 36f, 38f, new Color(0.15f, 0.30f, 0.30f, 0.35f));
            frame.raycastTarget = false;
        }

        /// <summary>圆形头像：外圈金环 + 内盘裁切，立绘由控制器填入。</summary>
        static void AddPortrait(UiPrefabBuilder builder, string name, float x, float y, float size)
        {
            var rim = builder.Node("Battle Lower HUD", name + " Rim", x - 2f, y - 2f, size + 4f, size + 4f);
            var rimDisc = rim.gameObject.AddComponent<GuxuPortraitDisc>();
            rimDisc.color = Gold;
            rimDisc.raycastTarget = false;

            var mask = builder.Node("Battle Lower HUD", name + " Mask", x, y, size, size);
            var disc = mask.gameObject.AddComponent<GuxuPortraitDisc>();
            disc.color = new Color(0.10f, 0.30f, 0.28f);
            disc.raycastTarget = false;
            mask.gameObject.AddComponent<Mask>().showMaskGraphic = true;

            builder.ImageNode("Battle Lower HUD/" + name + " Mask", "Art", -3f, -3f, size + 6f, size + 6f, Color.white)
                .raycastTarget = false;
        }

        static void AddAction(UiPrefabBuilder builder, string parent, string name, float x, float y, float width, float height)
        {
            var node = builder.Node(parent, name, x, y, width, height);
            var image = node.gameObject.AddComponent<Image>();
            image.color = Color.clear;
            image.raycastTarget = true;
            node.gameObject.AddComponent<Button>().targetGraphic = image;
        }

        static void AddAction(UiPrefabBuilder builder, string name, float x, float y, float width, float height)
        {
            AddAction(builder, "", name, x, y, width, height);
        }

        static void AddOutline(Text text)
        {
            if (text == null) return;
            var outline = text.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(0.025f, 0.075f, 0.10f, 0.95f);
            outline.effectDistance = new Vector2(1.2f, -1.2f);
        }

        static void Bind(string prefabPath)
        {
            var contents = PrefabUtility.LoadPrefabContents(prefabPath);
            if (contents == null) return;
            try
            {
                var view = contents.GetComponent<BattleHudView>();
                if (view == null) view = contents.AddComponent<BattleHudView>();

                var so = new SerializedObject(view);
                Set(so, "regionText", FindText(contents, "Battle Region"));
                Set(so, "exitButton", Get<Button>(contents, "Battle Exit"));
                Set(so, "speedButton", Get<Button>(contents, "Battle Speed"));
                Set(so, "speedText", FindText(contents, "Battle Speed/Label"));
                Set(so, "enemyPortrait", Get<Image>(contents, "Battle Enemy Portrait"));
                Set(so, "enemyNameText", FindText(contents, "Battle Enemy Name"));
                Set(so, "enemyHealthText", FindText(contents, "Enemy Health Value"));
                Set(so, "enemyHealthBar", Get<Image>(contents, "Enemy Health Bar"));
                Set(so, "enemyShieldBar", Get<Image>(contents, "Enemy Shield Bar"));
                Set(so, "enemyStatusText", FindText(contents, "Enemy Combat Status"));
                Set(so, "roundText", FindText(contents, "Battle Round"));
                Set(so, "eventText", FindText(contents, "Battle Event"));
                Set(so, "warningText", FindText(contents, "Boss Warning"));
                Set(so, "playerHealthBar", Get<Image>(contents, "Battle Lower HUD/Player Health Bar"));
                Set(so, "playerShieldBar", Get<Image>(contents, "Battle Lower HUD/Player Shield Bar"));
                Set(so, "playerHealthText", FindText(contents, "Battle Lower HUD/Player Health Value"));
                Set(so, "playerStatusText", FindText(contents, "Battle Lower HUD/Player Combat Status"));
                Set(so, "skillButton", Get<Button>(contents, "Battle Lower HUD/Cast Skill"));
                Set(so, "skillIcon", Get<Image>(contents, "Battle Lower HUD/Battle Skill Icon"));
                Set(so, "cooldownRing", Get<Image>(contents, "Battle Lower HUD/Skill Cooldown Ring"));
                Set(so, "skillNameText", FindText(contents, "Battle Lower HUD/Battle Skill Name"));
                Set(so, "skillStateText", FindText(contents, "Battle Lower HUD/Battle Skill State"));
                Set(so, "autoSkillButton", Get<Button>(contents, "Battle Lower HUD/Auto Skill"));
                Set(so, "autoToggleTrack", Get<Image>(contents, "Battle Lower HUD/Auto Skill/Toggle Track"));
                Set(so, "autoToggleThumb", Find(contents, "Battle Lower HUD/Auto Skill/Toggle Thumb"));
                Set(so, "autoToggleCaption", FindText(contents, "Battle Lower HUD/Auto Skill/Toggle Caption"));
                Set(so, "statisticsText", FindText(contents, "Battle Lower HUD/Battle Statistics"));
                so.ApplyModifiedPropertiesWithoutUndo();

                PrefabUtility.SaveAsPrefabAsset(contents, prefabPath);
                AssetDatabase.SaveAssets();
                Debug.Log("[BattleHud] 已生成并绑定 " + prefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contents);
            }
        }

        static Transform Find(GameObject root, string path) => root.transform.Find(path);

        static Text FindText(GameObject root, string path)
        {
            var node = Find(root, path);
            return node != null ? node.GetComponent<Text>() : null;
        }

        /// <summary>按路径取组件；节点或组件缺失时返回 null，绑定阶段只跳过不中断。</summary>
        static T Get<T>(GameObject root, string path) where T : Component
        {
            var node = Find(root, path);
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
