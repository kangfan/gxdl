using UnityEngine;

namespace Gxdl {
// One player survives UI rebuilds; transitions do not depend on game coroutines.
[DisallowMultipleComponent]
public sealed class GuxuBackgroundMusic : MonoBehaviour {
 const float Volume = .18f;
 const float FadeSeconds = .35f;
 AudioSource source;
 AudioClip home, combat, desired;

 void Awake() {
  source = gameObject.AddComponent<AudioSource>();
  source.playOnAwake = false;
  source.loop = true;
  source.spatialBlend = 0f;
  source.volume = 0f;
  home = Resources.Load<AudioClip>("Game/Guxu/Audio/bgm-home");
  combat = Resources.Load<AudioClip>("Game/Guxu/Audio/bgm-battle");
  if (!home || !combat) Debug.LogWarning("[Guxu BGM] Missing home or battle music.");
  desired = home;
 }

 public void SetBattle(bool inBattle) {
  // If a clip is missing, keep the available track instead of going silent.
  desired = inBattle && combat ? combat : home;
 }

 void Update() {
  if (!source || !desired) return;
  float step = Volume * Time.unscaledDeltaTime / FadeSeconds;
  if (source.clip != desired) {
   source.volume = Mathf.MoveTowards(source.volume, 0f, step);
   if (source.volume > 0f) return;
   source.Stop();
   source.clip = desired;
   source.Play();
  } else {
   source.volume = Mathf.MoveTowards(source.volume, Volume, step);
  }
 }

 void OnDisable() { if (source) source.Pause(); }
 void OnEnable() { if (source) source.UnPause(); }
 void OnDestroy() { if (source) Destroy(source); }
}
}
