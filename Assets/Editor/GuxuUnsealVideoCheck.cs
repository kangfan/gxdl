using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Gxdl;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

// Explicitly armed local QA control for this iteration while the Unity MCP is unavailable.
[InitializeOnLoad] public static class GuxuUnsealVideoCheck {
 public static string Root=>SessionState.GetString("GXDL.UnsealVideo.Output","Reference/UnsealVideo20261112-r01");
 const string Armed="GXDL.UnsealVideo.Armed",StartReview="GXDL.UnsealVideo.StartReview",StartTest="GXDL.UnsealVideo.StartTest";
 const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
 static double next;static bool running;static readonly List<string> checks=new List<string>();
 [Serializable] sealed class Request {public string action;}
 static GuxuUnsealVideoCheck(){EditorApplication.update+=Poll;EditorApplication.playModeStateChanged+=state=>{if(state!=PlayModeStateChange.EnteredEditMode)return;string output=SessionState.GetString("GXDL.UnsealVideo.Recovery","");if(string.IsNullOrEmpty(output))return;try{var checkpoint=JsonConvert.DeserializeObject<Recovery>(File.ReadAllText(output+"/recovery-before.json"));if(checkpoint.saveExisted)File.Copy(output+"/original-player-save.bin",checkpoint.savePath,true);else if(File.Exists(checkpoint.savePath))File.Delete(checkpoint.savePath);RestoreOpening();File.WriteAllText(output+"/post-stop-restored.txt",DateTime.UtcNow.ToString("O")+" original save bytes restored after OnApplicationQuit; opening preference restored.");SessionState.EraseString("GXDL.UnsealVideo.Recovery");Status("restored-edit-mode");}catch(Exception e){File.WriteAllText(Root+"/post-stop-error.txt",e.ToString());Debug.LogException(e);}};}
 sealed class Recovery {public string savePath;public bool saveExisted;}
 [MenuItem("GXDL/QA/Video Unseal Activate")]
 public static void Activate(){Directory.CreateDirectory(Root);SessionState.SetBool(Armed,true);Status("ready");}
 [MenuItem("GXDL/QA/Original Hero Unseal Activate")]
 public static void ActivateOriginalHero(){SessionState.SetString("GXDL.UnsealVideo.Output","Reference/UnsealOriginalHero20261113-r01");Directory.CreateDirectory(Root);string snapshot=Root+"/original-prefs-before-play.json";if(!File.Exists(snapshot))File.WriteAllText(snapshot,JsonConvert.SerializeObject(new{at=DateTime.UtcNow.ToString("O"),scope=GameRules.AccountScope,savePath=GameRules.SavePath,prefs=new[]{GxdlGame.OpeningStoryKey,GxdlGame.FirstUnsealStoryKey}.Select(key=>new{key,exists=PlayerPrefs.HasKey(key),value=PlayerPrefs.GetInt(key,0)}).ToArray()},Formatting.Indented));Activate();}
 [MenuItem("GXDL/QA/Icon125 Activate %#i")]
 public static void ActivateIcon125(){SessionState.SetString("GXDL.UnsealVideo.Output","Reference/IconRedesign125-20261116-r01/technical/runtime");Directory.CreateDirectory(Root);File.WriteAllText(Root+"/original-prefs-before-play.json",JsonConvert.SerializeObject(new{at=DateTime.UtcNow.ToString("O"),scope=GameRules.AccountScope,savePath=GameRules.SavePath,prefs=new[]{GxdlGame.OpeningStoryKey,GxdlGame.FirstUnsealStoryKey}.Select(key=>new{key,exists=PlayerPrefs.HasKey(key),value=PlayerPrefs.GetInt(key,0)}).ToArray()},Formatting.Indented));Activate();}
 [MenuItem("GXDL/QA/Video Unseal Start Isolated Test")]
 public static void RequestTests(){if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Stop the current preview before isolated video QA.");Activate();File.WriteAllText(Root+"/editor-command.json","{\"action\":\"test\"}");}
 static void Status(string result)=>File.WriteAllText(Root+"/editor-status.json",JsonConvert.SerializeObject(new{at=DateTime.UtcNow.ToString("O"),result,project=Application.dataPath,scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene().path,playing=EditorApplication.isPlaying,compiling=EditorApplication.isCompiling,review=GuxuUI23Review.ActiveOutput},Formatting.Indented));
 static void Poll(){
  if(!SessionState.GetBool(Armed,false)||EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.timeSinceStartup<next)return;next=EditorApplication.timeSinceStartup+.2;
  try{
   if(SessionState.GetBool(StartReview,false)&&EditorApplication.isPlaying&&GxdlGame.Instance&&GxdlGame.Instance.CanPlay){SessionState.SetBool(StartReview,false);Begin();if(SessionState.GetBool(StartTest,false)){SessionState.SetBool(StartTest,false);Run();}Status("review-started");}
   var path=Root+"/editor-command.json";if(!File.Exists(path))return;string json=File.ReadAllText(path);var command=JsonConvert.DeserializeObject<Request>(json);File.WriteAllText(Root+"/editor-command-last.json",json);File.Delete(path);
   if(command.action=="status")Status("observed");
   else if(command.action=="icons-verify"){GuxuItemIconCheck.Verify();Status("icons-verified");}
   else if(command.action=="review-current"){if(!EditorApplication.isPlaying||!GxdlGame.Instance||!GxdlGame.Instance.CanPlay)throw new InvalidOperationException("Current gameplay preview required.");Begin();Status("review-started-current-play");}
   else if(command.action=="review-end-current"){GuxuUI23Review.End();SessionState.EraseString("GXDL.UnsealVideo.Recovery");Status("review-restored-current-play");}
   else if(command.action=="refresh"){if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Refresh requires edit mode.");AssetDatabase.Refresh();Status("refreshed");}
   else if(command.action=="start"||command.action=="test"){
    if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Start requires edit mode.");
    var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();if(scene.path!="Assets/Scenes/GxdlMain.unity"){if(scene.isDirty)throw new InvalidOperationException("Preserve the modified scene before starting QA.");UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/GxdlMain.unity");}
    SessionState.SetBool("GXDL.UnsealVideo.HadOpening",PlayerPrefs.HasKey(GxdlGame.OpeningStoryKey));SessionState.SetInt("GXDL.UnsealVideo.Opening",PlayerPrefs.GetInt(GxdlGame.OpeningStoryKey,0));
    PlayerPrefs.SetInt(GxdlGame.OpeningStoryKey,1);PlayerPrefs.Save();SessionState.SetBool(StartReview,true);SessionState.SetBool(StartTest,command.action=="test");GuxuAccountChecks.PreviewGameplay();
   }else if(command.action=="command")GuxuUI23Review.Execute();
   else if(command.action=="end"){GuxuUI23Review.End();RestoreOpening();EditorApplication.isPlaying=false;Status("ended");}
   else if(command.action=="deactivate"){if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("End review before deactivating.");SessionState.SetBool(Armed,false);Status("deactivated");}
   else throw new ArgumentException("Unknown bounded video QA command.");
  }catch(Exception e){File.WriteAllText(Root+"/editor-error.txt",DateTime.UtcNow.ToString("O")+"\n"+e);Debug.LogException(e);}
 }
 static void RestoreOpening(){if(SessionState.GetBool("GXDL.UnsealVideo.HadOpening",false))PlayerPrefs.SetInt(GxdlGame.OpeningStoryKey,SessionState.GetInt("GXDL.UnsealVideo.Opening",0));else PlayerPrefs.DeleteKey(GxdlGame.OpeningStoryKey);PlayerPrefs.Save();}
 static void Begin(){string output=Root+"/sessions/"+DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fff");typeof(GuxuUI23Review).GetMethod("BeginAt",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{output,"growth-new"});SessionState.SetString("GXDL.UnsealVideo.Recovery",output);File.WriteAllText(Root+"/review-session.txt",Path.GetFullPath(output));}
 static void Check(bool condition,string message){if(!condition)throw new Exception("Video QA: "+message);checks.Add("PASS "+message);}
 static string Json(object value)=>JsonConvert.SerializeObject(value);
 static IEnumerator Command(string action,string name="",int height=1334)=>GuxuUI23Review.Run(new GuxuUI23Review.Command{action=action,name=name,height=height});
 [MenuItem("GXDL/QA/Video Unseal Run")]
 public static void Run(){if(running)throw new InvalidOperationException("Video QA already running.");running=true;checks.Clear();GxdlGame.Instance.StartCoroutine(Guard());}
 static IEnumerator Guard(){
  var stack=new Stack<IEnumerator>();stack.Push(Cases());bool passed=false;
  try{while(stack.Count>0){object current=null;bool more;try{more=stack.Peek().MoveNext();if(more)current=stack.Peek().Current;}catch(Exception e){File.WriteAllText(Root+"/technical-result.txt","FAIL "+DateTime.UtcNow.ToString("O")+"\n"+string.Join("\n",checks)+"\n"+e);Debug.LogException(e);yield break;}if(!more){(stack.Pop() as IDisposable)?.Dispose();continue;}if(current is IEnumerator inner)stack.Push(inner);else yield return current;}passed=true;}
  finally{while(stack.Count>0)(stack.Pop() as IDisposable)?.Dispose();running=false;try{GuxuUI23Review.End();}finally{RestoreOpening();EditorApplication.isPlaying=false;}if(passed)File.WriteAllText(Root+"/technical-result.txt","PASS "+DateTime.UtcNow.ToString("O")+"\n"+string.Join("\n",checks)+"\nEvidence "+GuxuUI23Review.ActiveOutput);}
 }
 static IEnumerator Cases(){
  var g=GxdlGame.Instance;var r=g.Rules;var sequence=JsonUtility.FromJson<GuxuCastFrames.Data>(Resources.Load<TextAsset>(GuxuCastFrames.ResourcePath).text);var busy=typeof(GxdlGame).GetField("busy",Private);var auto=typeof(GxdlGame).GetField("auto",Private);
  foreach(int height in new[]{1334,1624})foreach(bool first in new[]{true,false}){
   yield return GuxuUI23Review.Run(new GuxuUI23Review.Command{action="scenario",scenario="growth-new"});if(!first)PlayerPrefs.SetInt(GxdlGame.FirstUnsealStoryKey,1);g.Home();yield return Command("size",height:height);
   var motion=UnityEngine.Object.FindObjectOfType<GuxuCharacterMotion>();Check(motion.AnimationSource=="whole-character-frames"&&motion.CurrentClip=="unseal_original_idle","original-character Home idle "+height+" first="+first);Check(!g.NeedsFirstUnsealStory,"first cinematic disabled "+first);yield return g.PlayFirstUnsealStory();Check(!g.IsStoryPlaying,"legacy first-story entry is no-op");
   int seals=r.Save.peaches,cuts=r.Save.cuts;var expected=new GameRules(r.Config,JsonConvert.DeserializeObject<SaveData>(Json(r.Save)),42);expected.Chop();if(first){expected.Save.pending.slot=0;EquipmentTiers.Apply(expected.Save.pending);}
   yield return Command("click","Unseal");Check(Json(r.Save.pending)==Json(expected.Save.pending),"original roll and first-weapon policy "+height+" first="+first);var animation=UnityEngine.Object.FindObjectOfType<GuxuAnimation>();Check(animation&&animation.IsPlaying&&animation.StoneVisible,"stone intact before launch "+height+" first="+first);Check(!GameObject.Find("Skip Unseal")||r.Save.pending.quality>=4,"no first-only skip");
   float start=Time.time,deadline=Time.time+5;bool contact=false,pose=false;while((bool)busy.GetValue(g)&&Time.time<deadline){if(animation){pose|=motion.CurrentFrame>=sequence.releaseFrame;if(animation.RuneContactAt>=0){contact=true;Check(animation.RuneContactAt==animation.FractureAt&&!animation.StoneVisible,"rune impact and fracture same frame");break;}}yield return null;}
   while((bool)busy.GetValue(g)&&Time.time<deadline)yield return null;
   Check(contact&&pose&&!(bool)busy.GetValue(g)&&Time.time-start<4,"ordinary duration including first "+height+" first="+first);Check(r.Save.peaches==seals-1&&r.Save.cuts==cuts+1&&r.Save.pending!=null,"one token one pending "+height+" first="+first);Check(PlayerPrefs.GetInt(GxdlGame.FirstUnsealStoryKey,0)==1,"one-time reward flag retained");
   yield return Command("inspect");yield return Command("click","Resolve Equipment");yield return new WaitForSeconds(1.5f);Check(r.Save.pending==null,"decompose clears pending");
   expected.Resolve(false);expected.Chop();yield return Command("click","自动破封");Check((bool)auto.GetValue(g),"auto begins");Check(Json(r.Save.pending)==Json(expected.Save.pending),"second drop follows normal slot/tier/affix roll");int autoCuts=r.Save.cuts,autoSeals=r.Save.peaches;string pending=Json(r.Save.pending);yield return Command("click","停止自动");Check(!(bool)auto.GetValue(g),"auto stops during action");deadline=Time.time+5;while((bool)busy.GetValue(g)&&Time.time<deadline)yield return null;yield return new WaitForSeconds(1.5f);Check(r.Save.cuts==autoCuts&&r.Save.peaches==autoSeals&&Json(r.Save.pending)==pending,"stop retains paid drop without extra cut");yield return Command("inspect");
  }
  yield return GuxuUI23Review.Run(new GuxuUI23Review.Command{action="scenario",scenario="growth-new"});r.Save.peaches=0;g.Home();yield return Command("reject-click","Unseal");Check(r.Save.cuts==0&&r.Save.pending==null,"insufficient seals cannot start");
  yield return Command("inspect");Check(UnityEngine.Object.FindObjectsOfType<GuxuParticleFx>().All(p=>p.name.Contains("Cloud")),"no leftover transient unseal particles");
  yield return GuxuUI23Review.Run(new GuxuUI23Review.Command{action="scenario",scenario="growth-new"});r.Save.keepQuality=1;yield return Command("click","自动破封");yield return new WaitForSeconds(4);Check(!(bool)auto.GetValue(g)&&r.Save.cuts==1&&r.Save.peaches==39&&r.Save.pending!=null,"automatic retention stops with exactly one paid reward");yield return Command("click","Equip Relic");yield return new WaitForSeconds(1.5f);Check(r.Save.pending==null,"equipment selection resolves once");yield return Command("inspect");
  yield return GuxuUI23Review.Run(new GuxuUI23Review.Command{action="scenario",scenario="growth-new"});yield return Command("click","Unseal");string cancelledPending=Json(r.Save.pending);g.Home();yield return new WaitForSeconds(3);Check(r.Save.cuts==1&&r.Save.peaches==39&&Json(r.Save.pending)==cancelledPending&&PlayerPrefs.GetInt(GxdlGame.FirstUnsealStoryKey,0)==0,"cancel/rebuild preserves paid pending and original completion-flag timing");Check(!UnityEngine.Object.FindObjectsOfType<GuxuAnimation>().Any(a=>a.IsPlaying)&&UnityEngine.Object.FindObjectsOfType<GuxuParticleFx>().All(p=>p.name.Contains("Cloud")),"cancel/rebuild clears transient action");yield return Command("inspect");
  var data=JsonUtility.FromJson<GuxuCastFrames.Data>(Resources.Load<TextAsset>(GuxuCastFrames.ResourcePath).text);Check(data.frames.All(f=>!f.key.StartsWith("Game/Guxu/Vfx/Actions/Video/")),"original-character resources only; reference-video actor not bound");long memory=0;
  foreach(var frame in data.frames){var texture=Resources.Load<Texture2D>(frame.key);var importer=(TextureImporter)AssetImporter.GetAtPath("Assets/Resources/"+frame.key+".png");Check(texture.width==512&&texture.height==768&&!importer.isReadable&&!importer.mipmapEnabled&&importer.maxTextureSize<=1024,"bounded non-readable original-character frame "+frame.key);memory+=UnityEngine.Profiling.Profiler.GetRuntimeMemorySizeLong(texture);}
  File.WriteAllText(Root+"/runtime-texture-memory.json",JsonConvert.SerializeObject(new{at=DateTime.UtcNow.ToString("O"),frames=data.frames.Length,textureBytesEditor=memory,rgbaUpperBoundBytes=data.frames.Length*512L*768*4,referenceVideoActorLoaded=Resources.FindObjectsOfTypeAll<Texture2D>().Where(t=>AssetDatabase.GetAssetPath(t).StartsWith("Assets/Resources/Game/Guxu/Vfx/Actions/Video/unseal-")).Select(t=>t.name).ToArray(),note="Editor texture allocations and RGBA upper-bound estimate for reused original frames; not device peak or GPU performance."},Formatting.Indented));
 }
}
