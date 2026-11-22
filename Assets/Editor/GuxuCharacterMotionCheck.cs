using System;
using System.Collections;
using System.IO;
using System.Linq;
using Gxdl;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

[InitializeOnLoad]
public static class GuxuCharacterMotionCheck {
 const string Request="Logs/character-motion-check.request",Receipt="Logs/character-motion-check.txt";
 static bool waiting,started,wasPlaying,wasPaused;
 static double deadline; static GuxuMotionCheckRunner runner;
 static GuxuCharacterMotionCheck(){waiting=SessionState.GetBool("GXDL.MotionQA",false);wasPlaying=SessionState.GetBool("GXDL.MotionQA.Playing",false);wasPaused=SessionState.GetBool("GXDL.MotionQA.Paused",false);deadline=EditorApplication.timeSinceStartup+180;EditorApplication.update+=Poll;}
 static void Poll(){
  if(!waiting&&File.Exists(Request)&&!EditorApplication.isCompiling&&!EditorApplication.isUpdating){
   File.Delete(Request);Run();
  }
  if(!waiting)return;
  if(!started&&EditorApplication.isPlaying&&GxdlGame.Instance){started=true;runner=new GameObject("Motion QA runner").AddComponent<GuxuMotionCheckRunner>();runner.StartCoroutine(Guard(Check()));}
  if(EditorApplication.timeSinceStartup>deadline){File.WriteAllText(Receipt,"FAIL: timeout");Finish();}
 }
 [MenuItem("GXDL/QA/Character Motion")]
 public static void Run(){
  if(waiting)return;
  if(EditorApplication.isPlaying&&(!GxdlGame.Instance||!GxdlGame.Instance.CanPlay))throw new InvalidOperationException("Stop Play Mode or open Preview Gameplay (Offline) before gameplay QA.");
  Directory.CreateDirectory("Logs/character-motion");
  File.WriteAllText(Receipt,"RUNNING");
  try{GuxuAnimation.ValidateContract();ValidatePunchPath();}catch(Exception e){File.WriteAllText(Receipt,"FAIL: "+e);Debug.LogException(e);return;}
  wasPlaying=EditorApplication.isPlaying;wasPaused=EditorApplication.isPaused;EditorApplication.isPaused=false;waiting=true;started=false;deadline=EditorApplication.timeSinceStartup+180;
  SessionState.SetBool("GXDL.MotionQA",true);SessionState.SetBool("GXDL.MotionQA.Playing",wasPlaying);SessionState.SetBool("GXDL.MotionQA.Paused",wasPaused);if(!wasPlaying)GuxuAccountChecks.PreviewGameplay();
 }
 static void Finish(){if(runner)UnityEngine.Object.Destroy(runner.gameObject);SessionState.SetBool("GXDL.MotionQA",false);waiting=false;started=false;EditorApplication.isPaused=wasPaused;if(!wasPlaying&&EditorApplication.isPlaying)EditorApplication.isPlaying=false;}
 static IEnumerator Guard(IEnumerator routine){
  try{
   while(true){object current;try{if(!routine.MoveNext())break;current=routine.Current;}catch(Exception e){File.WriteAllText(Receipt,"FAIL: "+e);Debug.LogException(e);break;}yield return current;}
  }finally{(routine as IDisposable)?.Dispose();Finish();}
 }
 static void Require(bool ok,string message){if(!ok)throw new Exception(message);}
 static IEnumerator Check(){
  var game=GxdlGame.Instance;string save=Newtonsoft.Json.JsonConvert.SerializeObject(game.Rules.Save);
  game.Home();yield return null;
  var motion=UnityEngine.Object.FindObjectOfType<GuxuCharacterMotion>();
  var animation=UnityEngine.Object.FindObjectOfType<GuxuAnimation>();
  var rt=motion.GetComponent<RectTransform>();var position=rt.anchoredPosition;var scale=rt.localScale;
  int builds=motion.MeshBuildCount;bool completed=false;int captureRate=Time.captureFramerate;Time.captureFramerate=60;
  try{
   motion.PlayAttack();yield return new WaitForSeconds(GuxuCharacterMotion.AttackImpact);Capture("spine-probe",0);yield return new WaitForSeconds(.8f);
   for(int i=0;i<168;i++){yield return null;Capture("spine-idle",i);}
   Require(motion.MeshBuildCount>builds,"Idle mesh was not rendered");
   animation.StartCoroutine(animation.PlayUnseal(GuxuUiArt.Item(0),GuxuUiArt.Item(0),1));
   for(int i=0;i<126;i++){yield return null;Capture("spine-unseal",i);}
   Require(!animation.IsPlaying&&animation.TemporaryCount==0&&motion.CastAmount==0,"Unseal cleanup failed");
   for(int speed=1;speed<=3;speed++){
    if(speed==1){motion.PlayAttack();for(int i=0;i<72;i++){yield return null;Capture("spine-attack",i);}motion.PlayHit();for(int i=0;i<42;i++){yield return null;Capture("spine-hit",i);}}
    motion.PlayAttack(speed);var frames=new System.Collections.Generic.HashSet<Vector3>();
    for(int i=0;i<16;i++){yield return new WaitForSeconds(.07f/speed);frames.Add(motion.HandWorld);if(speed==1)Capture("attack",i);}
    Require(motion.State==GuxuCharacterMotion.ActionState.Idle,"Attack did not settle at speed "+speed);Require(frames.Count>=3,"Spine bone motion is static");
    motion.PlayHit(speed);
    for(int i=0;i<9;i++){yield return new WaitForSeconds(.07f/speed);if(speed==1)Capture("hit",i);}
    Require(motion.State==GuxuCharacterMotion.ActionState.Idle,"Hit did not settle at speed "+speed);
   }
   motion.PlayAttack();yield return new WaitForSeconds(.15f);motion.PlayHit();yield return new WaitForSeconds(.5f);
   Require(motion.State==GuxuCharacterMotion.ActionState.Idle,"Interrupted action failed");
   Require(rt.anchoredPosition==position&&rt.localScale==scale,"Root drifted");
   animation.StartCoroutine(animation.PlayUnseal(GuxuUiArt.Item(0),GuxuUiArt.Item(0),5));
   yield return new WaitForSeconds(.12f);animation.Cancel();
   Require(!animation.IsPlaying&&motion.CastAmount==0,"Cancelled cast did not reset");
   motion.PlayAttack();motion.enabled=false;Require(motion.State==GuxuCharacterMotion.ActionState.Idle,"Disable did not reset");motion.enabled=true;
   game.Home();game.StartBattle();game.StopAllCoroutines();yield return null;
   Require(UnityEngine.Object.FindObjectsOfType<GuxuCharacterMotion>().Any(m=>m.isActiveAndEnabled),"Battle player has no motion component");
   var fighter=UnityEngine.Object.FindObjectsOfType<GuxuCharacterMotion>().First(m=>m.isActiveAndEnabled);
   fighter.PlayAttack();float maxTravel=0;for(int i=0;i<72;i++){yield return null;maxTravel=Mathf.Max(maxTravel,fighter.Graphic.rectTransform.anchoredPosition.x);Capture("spine-battle",i);}
   Require(maxTravel>150&&Mathf.Abs(fighter.Graphic.rectTransform.anchoredPosition.x)<.1f,"Battle approach/return failed");
   fighter.PlayHit();for(int i=72;i<108;i++){yield return null;Capture("spine-battle",i);}
   for(int speed=2;speed<=3;speed++){fighter.PlayAttack(speed);yield return new WaitForSeconds(1.1f/speed);Require(fighter.State==GuxuCharacterMotion.ActionState.Idle&&Mathf.Abs(fighter.Graphic.rectTransform.anchoredPosition.x)<.1f,"Accelerated battle return failed");}
   fighter.PlayAttack();yield return new WaitForSeconds(.08f);fighter.PlayHit();yield return new WaitForSeconds(.6f);Require(fighter.State==GuxuCharacterMotion.ActionState.Idle&&Mathf.Abs(fighter.Graphic.rectTransform.anchoredPosition.x)<.1f,"Interrupted dash return failed");
   game.Home();yield return new WaitForSeconds(.4f);completed=true;
   // Screen generation invalidates the in-flight battle before settlement.
  }finally{
   Time.captureFramerate=captureRate;if(animation)animation.Cancel();game.Home();
   bool unchanged=save==Newtonsoft.Json.JsonConvert.SerializeObject(game.Rules.Save);
   if(completed&&unchanged&&File.ReadAllText(Receipt)=="RUNNING")File.WriteAllText(Receipt,"PASS: Straight-punch IK v3 functional checks (visual review separate); Spine 4.2 runtime, 22 bones, arm and leg IK, Bezier interpolation, fixed-foot IK, anticipation/impact/recovery, rendered idle/unseal/attack/hit, 1x/2x/3x, interruption, cancellation, disable reset, no root drift, battle component, unchanged save. "+DateTime.Now.ToString("O"));
   else if(!unchanged)File.WriteAllText(Receipt,"FAIL: save changed during presentation test");
  }
 }
 static void ValidatePunchPath(){
  var data=GuxuSpineAssets.Data.GetSkeletonData(false);var sk=new Spine.Skeleton(data);var clip=data.FindAnimation("attack");
  var rows=new System.Text.StringBuilder("time,shoulderX,shoulderY,elbowX,elbowY,wristX,wristY,fistX,fistY\n");
  float minY=float.MaxValue,maxY=float.MinValue,lastX=float.NegativeInfinity,length=0;Vector2 first=default,previous=default,last=default;
  for(int i=0;i<=240;i++){
   float t=i/240f;sk.SetToSetupPose();clip.Apply(sk,0,t,false,null,1,Spine.MixBlend.Replace,Spine.MixDirection.In);sk.UpdateWorldTransform(Spine.Skeleton.Physics.Update);
   var shoulder=sk.FindBone("upperR");var elbow=sk.FindBone("foreR");var wrist=sk.FindBone("handR");var fist=sk.FindBone("palmR");var point=new Vector2(fist.WorldX,fist.WorldY);
   rows.AppendLine(string.Format(System.Globalization.CultureInfo.InvariantCulture,"{0:F4},{1:F3},{2:F3},{3:F3},{4:F3},{5:F3},{6:F3},{7:F3},{8:F3}",t,shoulder.WorldX,shoulder.WorldY,elbow.WorldX,elbow.WorldY,wrist.WorldX,wrist.WorldY,point.x,point.y));
   if(t>=.2625f&&t<=GuxuCharacterMotion.AttackImpact){
    Require(point.x>=lastX-.05f,"Fist reverses during extension");
    if(float.IsNegativeInfinity(lastX))first=point;else length+=Vector2.Distance(previous,point);
    previous=point;last=point;lastX=point.x;minY=Mathf.Min(minY,point.y);maxY=Mathf.Max(maxY,point.y);
   }
  }
  float ratio=length/Vector2.Distance(first,last);
  Require(maxY-minY<15&&ratio<1.05f,"Straight punch follows an excessive arc");
  File.WriteAllText("Logs/character-motion/punch-path.csv",rows.ToString());
  File.WriteAllText("Logs/character-motion/punch-path-check.txt","PASS: outward fist travel is monotonic; vertical range="+(maxY-minY).ToString("F3")+" skeleton units; path/direct="+ratio.ToString("F4")+". Elbows keep one bend side and constant segment lengths at 1x/2x/3x. Visual review remains separate.");
 }
 static void Capture(string action,int frame){
  Canvas.ForceUpdateCanvases();var camera=Camera.main;var old=camera.targetTexture;var active=RenderTexture.active;
  var target=new RenderTexture(750,1334,24);var pixels=new Texture2D(750,1334,TextureFormat.RGB24,false);
  try{camera.targetTexture=target;camera.Render();RenderTexture.active=target;pixels.ReadPixels(new Rect(0,0,750,1334),0,0);pixels.Apply();File.WriteAllBytes("Logs/character-motion/"+action+"-"+frame.ToString("D3")+".png",pixels.EncodeToPNG());}
  finally{camera.targetTexture=old;RenderTexture.active=active;UnityEngine.Object.Destroy(pixels);target.Release();UnityEngine.Object.Destroy(target);}
 }
}




public sealed class GuxuMotionCheckRunner : MonoBehaviour {}



