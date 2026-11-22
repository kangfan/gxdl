using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Gxdl;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.UI;

// Bounded realtime sampling for an already isolated Review fixture. Never changes time or gameplay state.
internal static class GuxuMotionReviewCapture {
 internal static object FrameEnd=>Application.isBatchMode?null:new WaitForEndOfFrame();
 internal static Texture2D Capture(){
  if(!Application.isBatchMode)return ScreenCapture.CaptureScreenshotAsTexture();
  // Editor batchmode does not dispatch WaitForEndOfFrame; render the live scene camera instead.
  Canvas.ForceUpdateCanvases();var camera=Camera.main;if(!camera)throw new InvalidOperationException("Live capture requires the gameplay camera.");
  var target=RenderTexture.GetTemporary(Screen.width,Screen.height,24);var previous=camera.targetTexture;var active=RenderTexture.active;
  try{camera.targetTexture=target;camera.Render();RenderTexture.active=target;var image=new Texture2D(Screen.width,Screen.height,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,Screen.width,Screen.height),0,0);image.Apply();return image;}
  finally{camera.targetTexture=previous;RenderTexture.active=active;RenderTexture.ReleaseTemporary(target);}
 }
 internal static IEnumerator Record(string output,float seconds,int fps,string playerBefore){
  Directory.CreateDirectory(output);var frames=new List<object>();double start=Time.realtimeSinceStartupAsDouble,next=0;bool completed=false;
  try{
   while(Time.realtimeSinceStartupAsDouble-start<seconds){
    yield return FrameEnd;double elapsed=Time.realtimeSinceStartupAsDouble-start;if(elapsed>=seconds)break;
    if(elapsed>=next){
     string file=frames.Count.ToString("0000")+".jpg";var texture=Capture();
     try{File.WriteAllBytes(output+"/"+file,texture.EncodeToJPG(90));}finally{UnityEngine.Object.Destroy(texture);}
     frames.Add(new{file,atUtc=DateTime.UtcNow.ToString("O"),elapsedSeconds=elapsed,unityFrame=Time.frameCount,gameTime=Time.time,visual=Sample()});
     next=(Math.Floor(elapsed*fps)+1)/fps;
    }
    yield return null;
   }
   completed=true;
  }finally{
   var game=GxdlGame.Instance;
   File.WriteAllText(output+"/capture.json",JsonConvert.SerializeObject(new{completed,requestedSeconds=seconds,requestedFps=fps,actualElapsedSeconds=Time.realtimeSinceStartupAsDouble-start,sampling=Application.isBatchMode?"Live Editor Play camera renders after pointer events, sampled in batchmode; encoding/rendering adds overhead. Not native-window interaction or device performance.":"Realtime WaitForEndOfFrame sampled frames; JPEG encoding adds overhead. No fixed timestep or captureFramerate changes; not a native-frame/performance benchmark.",viewport=new[]{Screen.width,Screen.height},playerBefore=JsonConvert.DeserializeObject<SaveData>(playerBefore),playerAfter=game?game.Rules?.Save:null,frames},Formatting.Indented));
  }
 }
 static float[] Point(Vector3 world){var p=RectTransformUtility.WorldToScreenPoint(Camera.main,world);return new[]{p.x,p.y};}
 static float[] BonePoint(GuxuCharacterMotion actor,string name){var bone=actor.Graphic.Skeleton.FindBone(name);return bone==null?null:Point(actor.Graphic.transform.TransformPoint(new Vector3(bone.WorldX,bone.WorldY,0)*actor.Graphic.MeshScale));}
 static object Sample(){
  var game=GxdlGame.Instance;var unseal=UnityEngine.Object.FindObjectOfType<GuxuAnimation>();var stone=GameObject.Find("Sealed Stone Layer");
  var particles=UnityEngine.Object.FindObjectsOfType<ParticleSystem>();
  return new{screen=game?game.ExpansionScreen:null,
   unseal=unseal?new{unseal.Phase,unseal.FullElapsed,unseal.IsPlaying,unseal.TemporaryCount,unseal.ReleaseAt,unseal.RuneContactAt,unseal.FractureAt,unseal.StoneVisible}:null,
   actors=UnityEngine.Object.FindObjectsOfType<GuxuCharacterMotion>().Where(a=>a.Graphic).Select(a=>new{a.name,state=a.State.ToString(),a.ActionProgress,animationSource=a.AnimationSource,clip=a.CurrentClip,frameIndex=a.CurrentFrame,handScreen=Point(a.HandWorld),weaponScreen=Point(a.WeaponWorld),leftFootScreen=Point(a.FootWorld(false)),rightFootScreen=Point(a.FootWorld(true))}).ToArray(),
   bosses=UnityEngine.Object.FindObjectsOfType<GuxuBossMotion>().Select(b=>new{b.name,b.State,b.FrameIndex,b.ImpactCount,b.CurrentAttack,rectBottomScreen=Point(((RectTransform)b.transform).TransformPoint(new Vector3(((RectTransform)b.transform).rect.center.x,((RectTransform)b.transform).rect.yMin,0)))}).ToArray(),
   stoneCenterScreen=stone?Point(((RectTransform)stone.transform).TransformPoint(((RectTransform)stone.transform).rect.center)):null,
   effectCounts=new{particleSystems=particles.Length,particles=particles.Sum(p=>p.particleCount),softUi=UnityEngine.Object.FindObjectsOfType<GuxuSoftVfx>().Length,runes=UnityEngine.Object.FindObjectsOfType<GuxuRuneRing>().Length,ribbons=UnityEngine.Object.FindObjectsOfType<GuxuSpiritRibbon>().Length,spellLayers=UnityEngine.Object.FindObjectsOfType<GuxuSpellLayer>().Length,comboStrokes=UnityEngine.Object.FindObjectsOfType<GuxuCombatStroke>().Length,bossEffects=UnityEngine.Object.FindObjectsOfType<GuxuBossVfx>().Length,stoneShards=UnityEngine.Object.FindObjectsOfType<GuxuStoneShard>().Length},
   nativeParticles=particles.Select(p=>new{p.name,p.particleCount,p.isPlaying,persistent=p.GetComponent<GuxuParticleFx>()?.Persistent,peakCount=p.GetComponent<GuxuParticleFx>()?.PeakCount,sort=p.GetComponent<ParticleSystemRenderer>().sortingOrder}).ToArray(),
   footClouds=UnityEngine.Object.FindObjectsOfType<GuxuFootCloud>().Select(c=>new{c.name,anchorScreen=Point(c.AnchorWorld)}).ToArray(),
   simulationState=game&&game.ActiveBattle!=null?new{game.ActiveBattle.Round,game.ActiveBattle.PlayerHp,game.ActiveBattle.EnemyHp,game.ActiveBattle.PlayerShield,game.ActiveBattle.EnemyShield,game.ActiveBattle.SkillCooldown,game.ActiveBattle.Skill,game.ActiveBattle.SkillQueued,game.ActiveBattle.DamageDone,game.ActiveBattle.DamageTaken,game.ActiveBattle.HealingDone}:null,
   displayedHud=UnityEngine.Object.FindObjectsOfType<Text>().Where(t=>t.name=="Battle Statistics"||t.name=="Player Health Value"||t.name=="Enemy Health Value").Select(t=>new{t.name,t.text}).ToArray(),
   displayedState=UnityEngine.Object.FindObjectsOfType<GuxuBattleParticles>().Select(b=>new{b.DisplayedPlayerShield,b.DisplayedEnemyShield}).ToArray(),
   combo=UnityEngine.Object.FindObjectsOfType<GuxuCombatVfx>().Select(c=>new{c.name,c.Phase,c.ImpactCount,c.IsPlaying,c.ActiveVisualCount}).ToArray()};
 }
}
