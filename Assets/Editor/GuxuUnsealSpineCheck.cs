using System;
using System.Collections;
using System.IO;
using System.Linq;
using Gxdl;
using Spine;
using Spine.Unity;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
[InitializeOnLoad]
public static class GuxuUnsealSpineCheck {
 const string Flag="GXDL.UnsealSpineQA",Receipt="Logs/unseal-spine-check.txt";
 static bool started;static double deadline;
 static GuxuUnsealSpineCheck(){EditorApplication.update+=Monitor;}
 [MenuItem("Guxu/QA/Record Unseal Spine")]
 public static void Run(){Debug.LogWarning("The first-only 10.2s Spine sequence was retired; running current isolated video-unseal QA instead.");GuxuUnsealVideoCheck.RequestTests();}
 // Historical capture implementation retained for old evidence; no active entry starts it.
 static void HistoricalRun(){
  if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Stop Play Mode before running the capture.");
  for(int i=0;i<UnityEngine.SceneManagement.SceneManager.sceneCount;i++)if(UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty)throw new InvalidOperationException("Save scene changes before running QA.");
  started=false;
  Directory.CreateDirectory("Logs/unseal-spine/daily-frames");Directory.CreateDirectory("Logs/unseal-spine/frames");Directory.CreateDirectory("Logs/unseal-spine/keys");File.WriteAllText(Receipt,"RUNNING");
  foreach(var key in new[]{GxdlGame.OpeningStoryKey,GxdlGame.FirstUnsealStoryKey}){SessionState.SetInt(key,PlayerPrefs.GetInt(key,-1));PlayerPrefs.SetInt(key,1);}PlayerPrefs.Save();
  try{GuxuAcceptance.Rules();ValidateClip();}catch(Exception e){File.WriteAllText(Receipt,"FAIL: "+e);Finish(1);return;}
  SessionState.SetBool(Flag,true);EditorSceneManager.OpenScene("Assets/Scenes/GxdlMain.unity");GuxuAccountChecks.PreviewGameplay();
 }
 static void Monitor(){if(!SessionState.GetBool(Flag,false))return;if(!started&&EditorApplication.isPlaying&&GxdlGame.Instance){started=true;deadline=EditorApplication.timeSinceStartup+240;new GameObject("Unseal Spine QA").AddComponent<GuxuUnsealSpineRunner>().StartCoroutine(Guard(Check()));}if(started&&EditorApplication.timeSinceStartup>deadline){File.AppendAllText(Receipt,"\nFAIL: timed out");Finish(1);}}
 static void Finish(int code){foreach(var key in new[]{GxdlGame.OpeningStoryKey,GxdlGame.FirstUnsealStoryKey}){int value=SessionState.GetInt(key,-1);if(value<0)PlayerPrefs.DeleteKey(key);else PlayerPrefs.SetInt(key,value);}PlayerPrefs.Save();SessionState.SetBool(Flag,false);started=false;if(Application.isBatchMode)EditorApplication.Exit(code);else{EditorApplication.isPlaying=false;Debug.Log("Unseal Spine QA finished with code "+code+". Receipt: "+Receipt);}}
 static IEnumerator Guard(IEnumerator routine){bool ok=false;try{while(true){object next;try{if(!routine.MoveNext()){ok=true;break;}next=routine.Current;}catch(Exception e){File.WriteAllText(Receipt,"FAIL: "+e);Debug.LogException(e);break;}yield return next;}}finally{(routine as IDisposable)?.Dispose();Finish(ok?0:1);}}
 static void Require(bool ok,string why){if(!ok)throw new Exception("Unseal Spine: "+why);}
 static void ValidateClip(){
  var data=GuxuSpineAssets.Data.GetSkeletonData(false);var clip=data.FindAnimation("unseal_full");Require(clip!=null&&Mathf.Abs(clip.Duration-10.2f)<.002f,"10.2 second clip");
  var sk=new Skeleton(data);var state=new Spine.AnimationState(GuxuSpineAssets.Data.GetAnimationStateData());state.SetAnimation(0,"unseal_full",false);state.Apply(sk);sk.UpdateWorldTransform(Skeleton.Physics.Update);
  var previous=new Vector2(sk.FindBone("palmR").WorldX,sk.FindBone("palmR").WorldY);float footL=sk.FindBone("footL").WorldY,footR=sk.FindBone("footR").WorldY,maxStep=0,footLX=0,footRX=0;
  for(int i=0;i<1224;i++){state.Update(1/120f);state.Apply(sk);sk.UpdateWorldTransform(Skeleton.Physics.Update);var hand=sk.FindBone("palmR");var point=new Vector2(hand.WorldX,hand.WorldY);Require(!float.IsNaN(point.x)&&!float.IsNaN(point.y),"finite pose");maxStep=Mathf.Max(maxStep,Vector2.Distance(point,previous));previous=point;if(i==179){footLX=sk.FindBone("footL").WorldX;footRX=sk.FindBone("footR").WorldX;}if(i>=180)Require(Mathf.Abs(sk.FindBone("footL").WorldY-footL)<.05f&&Mathf.Abs(sk.FindBone("footR").WorldY-footR)<.05f&&Mathf.Abs(sk.FindBone("footL").WorldX-footLX)<.05f&&Mathf.Abs(sk.FindBone("footR").WorldX-footRX)<.05f,"planted feet after entrance");}
  Require(maxStep<20,"no pose snapping");File.WriteAllText("Logs/unseal-spine/contract.txt","PASS: Spine 4.2 parses; unseal_full duration 10.2s; 1224 samples; fixed feet after 1.5s entrance; finite and continuous hand; maximum 120fps hand step="+maxStep);
 }
 static IEnumerator Check(){
  var game=GxdlGame.Instance;string backup=Newtonsoft.Json.JsonConvert.SerializeObject(game.Rules.Save);int oldCapture=Time.captureFramerate;var camera=Camera.main;var oldTarget=camera.targetTexture;var rt=new RenderTexture(750,1334,24);camera.targetTexture=rt;
  try{
   game.Rules.Save.pending=null;game.Rules.Save.peaches=Math.Max(20,game.Rules.Save.peaches);game.Home();yield return null;
   int seals=game.Rules.Save.peaches;var gear=Newtonsoft.Json.JsonConvert.SerializeObject(game.Rules.Save.gear);Time.captureFramerate=30;
   PlayerPrefs.DeleteKey(GxdlGame.FirstUnsealStoryKey);game.Chop();game.Chop();
   var animation=UnityEngine.Object.FindObjectOfType<GuxuAnimation>();var motion=UnityEngine.Object.FindObjectOfType<GuxuCharacterMotion>();int beforeMesh=motion.MeshBuildCount;
   Require(animation.IsPlaying&&motion.State==GuxuCharacterMotion.ActionState.Unseal,"live Spine sequence started");
   File.WriteAllText("Logs/unseal-spine/frame-times.csv","frame,sequence_seconds\n");
   for(int frame=0;frame<307;frame++){
    yield return null;if(motion&&motion.Graphic){motion.Graphic.Update(0);motion.Graphic.UpdateMesh();}
    Capture("frames/"+frame.ToString("D4"));
    File.AppendAllText("Logs/unseal-spine/frame-times.csv",frame+","+(animation?animation.FullElapsed:10.2f).ToString("F4",System.Globalization.CultureInfo.InvariantCulture)+"\n");
    if(frame==30)Require(motion.MeshBuildCount>beforeMesh,"actual Spine mesh updates");
    if(frame%6==0&&frame>=234&&frame<=282)Capture("keys/pose-"+(frame/30f).ToString("F1",System.Globalization.CultureInfo.InvariantCulture));
    if(frame==264){foreach(var ribbon in UnityEngine.Object.FindObjectsOfType<GuxuSpiritRibbon>()){var mesh=ribbon.canvasRenderer.GetMesh();Require(mesh.vertexCount>100&&mesh.vertices.All(v=>!float.IsNaN(v.x)&&!float.IsNaN(v.y)&&!float.IsInfinity(v.x)&&!float.IsInfinity(v.y)),"finite rendered light ribbon mesh");}}
    if(frame==111)Require(UnityEngine.Object.FindObjectsOfType<GuxuStoneShard>().Any(s=>s.MeshBuildCount>0),"stone fracture rendered");
   }
   yield return new WaitForSeconds(.2f);Time.captureFramerate=oldCapture;
   Require(game.Rules.Save.peaches==seals-1,"one token per full sequence");Require(game.Rules.Save.pending!=null,"pending reward remains");Require(Newtonsoft.Json.JsonConvert.SerializeObject(game.Rules.Save.gear)==gear,"no implicit equip");
   var stone=UnityEngine.Object.FindObjectsOfType<Image>().Single(i=>i.name=="Sealed Stone Layer");Require(!stone.enabled,"stone hidden while reward awaits decision");Capture("pending");
   Require(!UnityEngine.Object.FindObjectsOfType<GuxuStoneShard>().Any(),"fragment cleanup");Require(!UnityEngine.Object.FindObjectsOfType<SkeletonGraphic>().Any(s=>s.name=="Spine Golden Echo"),"echo cleanup");
   int coins=game.Rules.Save.coins,reward=game.Rules.Save.pending.quality*20;game.ResolveDrop(false);game.ResolveDrop(false);yield return new WaitForSeconds(2.2f);Require(game.Rules.Save.coins==coins+reward,"one resolution reward");
   Require(PlayerPrefs.GetInt(GxdlGame.FirstUnsealStoryKey)==1,"completed tutorial persisted");
   Time.captureFramerate=30;int dailySeals=game.Rules.Save.peaches;game.Chop();game.Chop();
   var daily=UnityEngine.Object.FindObjectOfType<GuxuCharacterMotion>();var actor=(RectTransform)daily.transform;
   Vector2 stand=actor.anchoredPosition;Vector3 standScale=actor.localScale;Quaternion standRotation=actor.localRotation;
   Require(daily.Graphic.AnimationState.GetCurrent(0).Animation.Name=="home_cast","later manual cast uses back-facing action");
   Vector3 handStart=daily.transform.parent.InverseTransformPoint(daily.HandWorld);float handTravel=0;
   for(int frame=0;frame<90;frame++){
    yield return null;
    if(daily){daily.Graphic.Update(0);daily.Graphic.UpdateMesh();
     Require(Vector2.Distance(actor.anchoredPosition,stand)<.01f&&Vector3.Distance(actor.localScale,standScale)<.001f&&Quaternion.Angle(actor.localRotation,standRotation)<.01f,"daily stance position scale and orientation fixed");
     handTravel=Mathf.Max(handTravel,Vector3.Distance(handStart,daily.transform.parent.InverseTransformPoint(daily.HandWorld)));
     Require(daily.Graphic.AnimationState.GetCurrent(0).Animation.Name!="unseal_full","daily never invokes cinematic");
    }
    Require(!UnityEngine.Object.FindObjectsOfType<SkeletonGraphic>().Any(g=>g.name=="Spine Golden Echo"),"daily has no cinematic echo");
    Capture("daily-frames/"+frame.ToString("D4"));
    if(frame==9){var glow=UnityEngine.Object.FindObjectsOfType<GuxuSoftVfx>().Single(g=>g.name=="Casting Palm Light");Require(Vector3.Distance(daily.transform.parent.InverseTransformPoint(glow.transform.position),daily.transform.parent.InverseTransformPoint(daily.HandWorld))<8,"palm light follows actual hand");Capture("daily-cast");}
    if(frame==25)Require(UnityEngine.Object.FindObjectsOfType<GuxuStoneShard>().Any(s=>s.MeshBuildCount>0),"daily fracture rendered");
   }
   Time.captureFramerate=oldCapture;Require(handTravel>5,"daily hand visibly moves");Require(game.Rules.Save.pending!=null&&game.Rules.Save.peaches==dailySeals-1,"daily single token and pending loot");
   Require(!UnityEngine.Object.FindObjectOfType<GuxuAnimation>().IsPlaying&&!UnityEngine.Object.FindObjectsOfType<Image>().Single(i=>i.name=="Sealed Stone Layer").enabled,"daily ends quickly and stone stays hidden");
   File.WriteAllText("Logs/unseal-spine/daily-contract.txt","PASS: 90 Unity frames; home_cast; fixed actor transform; hand movement="+handTravel+"; hand-attached glow; fracture; pending stone hidden; single token.");
   var dailyDrop=game.Rules.Save.pending;game.ResolveDrop(true);yield return new WaitForSeconds(2.2f);Require(game.Rules.Save.gear[dailyDrop.slot]==dailyDrop,"daily loot explicitly equipped");
   game.Rules.Save.keepQuality=1;UnityEngine.Object.FindObjectsOfType<Button>().Single(b=>b.name=="自动破封").onClick.Invoke();
   yield return new WaitForSeconds(.2f);Require(UnityEngine.Object.FindObjectOfType<GuxuCharacterMotion>().Graphic.AnimationState.GetCurrent(0).Animation.Name=="home_cast","automatic cast uses same back-facing action");
   yield return new WaitForSeconds(2.5f);Require(game.Rules.Save.pending!=null&&!UnityEngine.Object.FindObjectOfType<GuxuAnimation>().IsPlaying,"auto pauses on kept relic");game.ResolveDrop(false);yield return new WaitForSeconds(2.2f);
   PlayerPrefs.DeleteKey(GxdlGame.FirstUnsealStoryKey);game.Chop();yield return new WaitForSeconds(.3f);game.SkipCurrentUnseal();yield return new WaitForSeconds(.2f);Require(game.Rules.Save.pending!=null&&!UnityEngine.Object.FindObjectOfType<GuxuAnimation>().IsPlaying,"skip keeps pending and clears sequence");game.ResolveDrop(false);yield return new WaitForSeconds(2.2f);
   PlayerPrefs.DeleteKey(GxdlGame.FirstUnsealStoryKey);game.Chop();yield return new WaitForSeconds(5);game.Home();yield return null;Require(!UnityEngine.Object.FindObjectsOfType<SkeletonGraphic>().Any(s=>s.name=="Spine Golden Echo"),"cancel clears live actor echo");Require(game.Rules.Save.pending!=null&&!UnityEngine.Object.FindObjectsOfType<Image>().Single(i=>i.name=="Sealed Stone Layer").enabled,"cancel keeps reward and stone hidden");
   File.WriteAllText(Receipt,"PASS: Spine full 10.2s live playback; 307 tutorial frames + 90 daily frames; first-only tutorial; subsequent manual and automatic back-facing casts; fixed daily stance and hand-following effects; bone continuity; planted feet; fracture; single token; pending stone hiding; single settlement; skip; screen cancellation; cleanup. Save restored after test.");
  }finally{game.SkipCurrentUnseal();game.StopAllCoroutines();game.Rules.Save=Newtonsoft.Json.JsonConvert.DeserializeObject<SaveData>(backup);game.Rules.Store();Time.captureFramerate=oldCapture;camera.targetTexture=oldTarget;rt.Release();UnityEngine.Object.Destroy(rt);}
 }
 static void Capture(string suffix){Canvas.ForceUpdateCanvases();var camera=Camera.main;var prior=RenderTexture.active;var pixels=new Texture2D(750,1334,TextureFormat.RGB24,false);try{camera.Render();RenderTexture.active=camera.targetTexture;pixels.ReadPixels(new Rect(0,0,750,1334),0,0);pixels.Apply();File.WriteAllBytes("Logs/unseal-spine/"+suffix+".png",pixels.EncodeToPNG());}finally{RenderTexture.active=prior;UnityEngine.Object.Destroy(pixels);}}
}
public sealed class GuxuUnsealSpineRunner:MonoBehaviour{}
