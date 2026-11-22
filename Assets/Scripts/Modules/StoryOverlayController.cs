using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Gxdl.Modules
{
    /// <summary>
    /// 开场动画控制器。节点来自 StoryOverlayView prefab，
    /// 这里只负责逐帧推进镜头、字幕、符文与氛围层，不再运行时搭界面。
    /// </summary>
    public sealed class StoryOverlayController : MonoBehaviour
    {
        public const string PrefabPath = "UI/StoryOverlayView";

        StoryOverlayView view;
        GuxuStoryData.Shot[] shots;
        float fadeIn;
        float fadeOut = 1f;

        public bool IsPlaying { get; private set; }

        public StoryOverlayView View => view;

        /// <summary>从 Resources 实例化界面并挂到指定父节点下。</summary>
        public static StoryOverlayController Create(Transform parent)
        {
            var prefab = Resources.Load<GameObject>(PrefabPath);
            if (prefab == null)
            {
                Debug.LogError("[StoryOverlay] 找不到界面 prefab：" + PrefabPath);
                return null;
            }

            var instance = Instantiate(prefab, parent, false);
            instance.name = "Story Overlay";
            var controller = instance.GetComponent<StoryOverlayController>();
            if (controller == null) controller = instance.AddComponent<StoryOverlayController>();
            return controller;
        }

        void Awake()
        {
            view = GetComponent<StoryOverlayView>();
            if (view == null) Debug.LogError("[StoryOverlay] prefab 根节点缺少 StoryOverlayView");
        }

        /// <summary>绑定跳过回调并准备播放。</summary>
        public void Prepare(GuxuStoryData.Shot[] timeline, Action onSkip)
        {
            shots = timeline;
            if (view == null) return;

            var fade = view.RootFade;
            if (fade != null) fade.alpha = 0f;

            if (view.SkipButton != null)
            {
                view.SkipButton.onClick.RemoveAllListeners();
                if (onSkip != null) view.SkipButton.onClick.AddListener(() => onSkip());
            }

            if (view.Incoming != null) view.Incoming.color = Color.clear;
            if (view.Outgoing != null) view.Outgoing.color = Color.clear;
            if (view.Title != null) view.Title.gameObject.SetActive(false);
            IsPlaying = true;
        }

        /// <summary>逐帧推进整段镜头。调用方负责协程生命周期与跳过信号。</summary>
        public IEnumerator Play(Func<bool> skipped, Action<string> onShot, Func<GuxuStoryData.Shot, float, Color> captionTint)
        {
            if (view == null || shots == null) yield break;

            var total = 0f;
            for (var i = 0; i < shots.Length; i++) total += shots[i].duration;

            var elapsedTotal = 0f;
            for (var index = 0; index < shots.Length; index++)
            {
                if (skipped != null && skipped()) yield break;
                var shot = shots[index];

                if (index > 0) CarryIncomingToOutgoing();
                FillIncoming(shot);
                if (onShot != null) onShot(shot.title);

                for (var elapsed = 0f; elapsed < shot.duration; elapsed += Mathf.Max(Time.unscaledDeltaTime, 0.0001f))
                {
                    if (skipped != null && skipped()) yield break;
                    Sample(shot, index, elapsed, elapsedTotal, total);
                    yield return null;
                }

                elapsedTotal += shot.duration;
            }

            IsPlaying = false;
        }

        void CarryIncomingToOutgoing()
        {
            var outgoing = view.Outgoing;
            var incoming = view.Incoming;
            if (outgoing == null || incoming == null) return;

            outgoing.texture = incoming.texture;
            outgoing.uvRect = incoming.uvRect;
            outgoing.color = Color.white;
            outgoing.rectTransform.localScale = incoming.rectTransform.localScale;
            outgoing.rectTransform.anchoredPosition = incoming.rectTransform.anchoredPosition;
        }

        void FillIncoming(GuxuStoryData.Shot shot)
        {
            var incoming = view.Incoming;
            if (incoming == null) return;

            incoming.texture = GuxuStoryData.Texture(shot.atlas);
            incoming.uvRect = GuxuStoryData.Cell(shot.cell, incoming.texture.width, incoming.texture.height);
            view.ApplyShotText(shot.title, shot.speaker, shot.subtitle);

            if (view.Title != null) view.Title.gameObject.SetActive(shot.motion == 4 && shot.atlas == "opening-seeker" && shot.cell == 0);

            var letter = shot.atlas == "opening-seeker" && shot.cell == 0;
            if (view.Runes != null)
            {
                view.Runes.gameObject.SetActive(shot.motion != 0);
                view.Runes.Kind = shot.motion == 1 || letter ? 0 : shot.motion == 2 ? 6 : 1;
            }

            var focus = FocusPosition(shot.focus);
            if (view.Runes != null) view.Runes.rectTransform.anchoredPosition = focus;
            if (view.Glow != null) view.Glow.rectTransform.anchoredPosition = focus;
        }

        void Sample(GuxuStoryData.Shot shot, int index, float elapsed, float elapsedTotal, float total)
        {
            var incoming = view.Incoming;
            var p = Mathf.Clamp01(elapsed / shot.duration);
            var ease = Mathf.SmoothStep(0f, 1f, p);
            var blend = index == 0 ? 1f : Mathf.SmoothStep(0f, 1f, elapsed / 0.65f);

            if (incoming != null)
            {
                incoming.color = new Color(1f, 1f, 1f, blend);
                var pose = GuxuStoryData.CameraPose(shot, elapsed);
                incoming.rectTransform.localScale = Vector3.one * pose.z;
                incoming.rectTransform.anchoredPosition = new Vector2(pose.x, pose.y);
            }

            var life = elapsedTotal + elapsed;
            fadeIn = Mathf.SmoothStep(0f, 1f, life / 0.65f);
            fadeOut = Mathf.SmoothStep(0f, 1f, (total - life) / 0.7f);
            if (view.RootFade != null) view.RootFade.alpha = fadeIn * fadeOut;

            if (view.Captions != null)
            {
                view.Captions.alpha = Mathf.SmoothStep(0f, 1f, (elapsed - 0.25f) / 0.5f)
                    * Mathf.Clamp01((shot.duration - elapsed) / 0.25f);
            }

            if (view.Title != null)
            {
                view.Title.alpha = Mathf.SmoothStep(0f, 1f, (elapsed - 0.4f) / 1.1f)
                    * Mathf.Clamp01((shot.duration - elapsed) / 0.65f);
                if (view.TitleRect != null) view.TitleRect.localScale = Vector3.one * Mathf.Lerp(0.96f, 1.02f, ease);
            }

            var letter = shot.atlas == "opening-seeker" && shot.cell == 0;
            if (view.Runes != null)
            {
                var effectPhase = shot.motion == 2 ? Mathf.Clamp01(elapsed / 3.2f) : Mathf.Lerp(0.2f, 0.72f, p);
                view.Runes.Sample(effectPhase, elapsed * 0.35f);
                var spellScale = shot.motion == 1 ? 1.8f : shot.motion == 2 ? 2.6f : letter ? 0.55f : 0.75f;
                view.Runes.rectTransform.localScale = new Vector3(spellScale, spellScale * (shot.motion == 4 && !letter ? 1.6f : 1f), 1f);
                view.Runes.canvasRenderer.SetAlpha(blend * (shot.motion == 4 ? 0.32f : 0.24f));
            }

            if (view.Glow != null)
            {
                view.Glow.color = shot.motion == 1
                    ? new Color(0.59f, 0.32f, 1f, 0.20f * blend)
                    : new Color(1f, 0.76f, 0.26f, shot.motion == 4 ? (0.13f + 0.09f * Mathf.Sin(elapsed * 2f)) * blend : 0f);
            }

            SampleAtmosphere(shot, life, blend, elapsed);

            if (view.Bloom != null)
            {
                var pulse = shot.motion == 2 ? Mathf.Exp(-elapsed * 4f) * 0.85f
                    : shot.motion == 1 ? Mathf.Pow(Mathf.Max(0f, Mathf.Sin(elapsed * 4f)), 16f) * 0.30f
                    : shot.motion == 3 ? 0.16f + 0.12f * Mathf.Sin(elapsed * 2f) : 0f;
                view.Bloom.color = shot.motion == 1 ? new Color(0.66f, 0.4f, 1f, pulse)
                    : shot.motion == 3 ? new Color(0.42f, 0.85f, 1f, pulse)
                    : new Color(1f, 0.82f, 0.48f, pulse);
                view.Bloom.rectTransform.localScale = Vector3.one * (0.75f + p * 0.6f);
            }

            if (view.Progress != null) view.Progress.sizeDelta = new Vector2(674f * Mathf.Clamp01(life / total), 2f);
        }

        void SampleAtmosphere(GuxuStoryData.Shot shot, float life, float blend, float elapsed)
        {
            var mist = view.Mist;
            if (mist != null)
            {
                for (var i = 0; i < mist.Length; i++)
                {
                    if (mist[i] == null) continue;
                    mist[i].rectTransform.anchoredPosition = new Vector2(-240f + i * 170f + Mathf.Sin(life * 0.14f + i) * 75f, 655f + i % 2 * 130f + Mathf.Cos(life * 0.16f + i) * 18f);
                    mist[i].color = shot.motion == 1 ? new Color(0.34f, 0.20f, 0.50f, 0.18f) : new Color(0.70f, 0.86f, 0.91f, 0.18f);
                }
            }

            var dust = view.Dust;
            if (dust == null) return;
            for (var i = 0; i < dust.Length; i++)
            {
                if (dust[i] == null) continue;
                var x = (i * 131.7f + Mathf.Sin(life * 0.27f + i) * 40f) % 750f;
                var y = (i * 79.3f - life * (15f + i % 5 * 4f) + 3000f) % 900f + 100f;
                dust[i].rectTransform.anchoredPosition = new Vector2(x, y);
                var tint = shot.motion == 1 ? new Color(0.78f, 0.47f, 1f)
                    : shot.motion == 3 ? new Color(0.5f, 0.9f, 1f)
                    : new Color(1f, 0.84f, 0.48f);
                tint.a = (0.2f + 0.45f * Mathf.Pow(Mathf.Sin(life * 1.6f + i), 2f)) * (shot.motion == 0 ? 0.45f : 1f);
                dust[i].color = tint;
            }
        }

        static Vector2 FocusPosition(Vector2 focus) => new Vector2(focus.x * 900f - 450f, focus.y * 1350f - 675f);
    }
}
