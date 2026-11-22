using System;
using System.Collections.Generic;
using Gxdl.Modules;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Gxdl.EditorTools
{
    /// <summary>
    /// 界面 prefab 的通用骨架构造器。
    ///
    /// 各界面只描述自己的节点，通用的这几件事在这里做一次：
    /// 根节点锚点与 UiRootLayout / SafeAreaFitter、背景层、
    /// 顶栏返回按钮与标题、资源栏。
    ///
    /// 与 StoryOverlayPrefabSource 同一条思路：静态骨架写死，
    /// 运行时数值一律由 Controller 通过 View 字段填入。
    /// </summary>
    public static class ScreenPrefabKit
    {
        public static readonly Color Gold = new Color(0.86f, 0.78f, 0.54f);
        public static readonly Color Cream = new Color(0.94f, 0.94f, 0.87f);
        public static readonly Color Panel = new Color(0.055f, 0.12f, 0.16f, 0.96f);

        /// <summary>
        /// 建立界面根骨架：根节点 + 背景 + 顶栏。
        /// 调用方在返回的 builder 上继续添加自己页面的内容。
        /// </summary>
        public static UiPrefabBuilder Create(
            string name,
            string backgroundKey,
            string title,
            string subtitle,
            out RectTransform contentRoot)
        {
            var builder = new UiPrefabBuilder(name, new Vector2(UiRootLayout.DesignWidth, UiRootLayout.DesignHeight));
            var root = builder.Root;
            AnchorUtility.Apply(root, AnchorPreset.Stretch, Vector2.zero, Vector2.zero);
            AddRootComponents(root);

            // 背景层：优先用主题图，缺失时退回纯色，保证 prefab 永远可生成。
            var background = builder.ImageNode("", "Background", 0f, 0f,
                UiRootLayout.DesignWidth, UiRootLayout.DesignHeight, new Color(0.015f, 0.045f, 0.065f));
            if (!string.IsNullOrEmpty(backgroundKey))
            {
                var sprite = LoadSprite(backgroundKey);
                if (sprite != null)
                {
                    background.sprite = sprite;
                    background.color = Color.white;
                    background.preserveAspect = false;
                }
            }

            BuildHeader(builder, title, subtitle);

            contentRoot = builder.Node("", "Content", 0f, 200f,
                UiRootLayout.DesignWidth, UiRootLayout.DesignHeight - 200f);
            return builder;
        }

        public static void AddRootComponents(RectTransform root)
        {
            if (root.GetComponent<UiRootLayout>() == null) root.gameObject.AddComponent<UiRootLayout>();
            if (root.GetComponent<SafeAreaFitter>() == null) root.gameObject.AddComponent<SafeAreaFitter>();
        }

        static void BuildHeader(UiPrefabBuilder builder, string title, string subtitle)
        {
            var header = builder.Node("", "Header", 0f, 0f, UiRootLayout.DesignWidth, 196f);
            header.gameObject.AddComponent<Image>().color = new Color(0.01f, 0.05f, 0.07f, 0.68f);

            var back = builder.Node("Header", "Back", 22f, 28f, 120f, 58f);
            var backImage = back.gameObject.AddComponent<Image>();
            backImage.color = new Color(0.15f, 0.38f, 0.35f, 0.94f);
            backImage.raycastTarget = true;
            var backButton = back.gameObject.AddComponent<Button>();
            backButton.targetGraphic = backImage;
            builder.TextNode("Header/Back", "Label", 0f, 0f, 120f, 58f, "‹ 古墟", 26, Cream, TextAnchor.MiddleCenter);

            builder.TextNode("Header", "Title", 150f, 27f, 440f, 62f, title, 35, Gold);
            builder.TextNode("Header", "Subtitle", 25f, 102f, 700f, 65f, subtitle, 23, Cream);
            builder.ImageNode("Header", "Rule", 25f, 176f, 700f, 2f, new Color(0.6f, 0.5f, 0.3f, 0.6f));
        }

        /// <summary>
        /// 批量生成入口：遍历注册表，逐个调用各生成器。
        /// 任一界面失败不影响其余界面，末尾汇总报告。
        /// </summary>
        public static string BuildAll()
        {
            var built = new List<string>();
            var failed = new List<string>();
            foreach (var entry in AllScreens)
            {
                try
                {
                    var path = entry.Value();
                    if (path != null) built.Add(path);
                    else failed.Add(entry.Key + "：生成器返回空");
                }
                catch (Exception error)
                {
                    failed.Add(entry.Key + "：" + error.GetType().Name + " " + error.Message);
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            foreach (var item in failed) Debug.LogError("[ScreenPrefabKit] " + item);
            var report = $"已生成 {built.Count} 个界面，失败 {failed.Count} 个";
            if (failed.Count > 0) report += "\n" + string.Join("\n", failed);
            return report;
        }

        /// <summary>界面名 → 生成函数。新增界面时在此登记一行。</summary>
        public static readonly Dictionary<string, Func<string>> AllScreens =
            new Dictionary<string, Func<string>>();

        public static void Register(string key, Func<string> generator)
        {
            AllScreens[key] = generator;
        }

        static Sprite LoadSprite(string resourcesPath)
        {
            var texture = Resources.Load<Texture2D>(resourcesPath);
            if (texture == null) return null;
            return Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100f);
        }
    }
}
