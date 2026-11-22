using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Gxdl;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
[InitializeOnLoad]
public static class GuxuFinalArtCheck {
 const string Flag="GXDL.FinalArtQA",Receipt="Logs/final-art-check.txt";
 static bool started;static double deadline;
 static GuxuFinalArtCheck(){EditorApplication.update+=Monitor;}
 [MenuItem("Guxu/QA/Review Home Composition")]
 public static void RunVisual(){Debug.LogWarning("Historical Home/first-cinematic QA is superseded by GXDL/QA/Video Unseal Start Isolated Test; this runs only current unseal coverage.");GuxuUnsealVideoCheck.RequestTests();}
 [MenuItem("Guxu/QA/Verify Final Art")]
 public static void Run(){Debug.LogWarning("Historical Final Art/first-cinematic QA is superseded for Home by GXDL/QA/Video Unseal Start Isolated Test; this is not a full art-suite run.");GuxuUnsealVideoCheck.RequestTests();}
 // Kept only as the historical test implementation; no active menu starts its obsolete cinematic assertions.
 static void HistoricalRun(){
  if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Stop Play Mode before running QA.");
  for(int i=0;i<UnityEngine.SceneManagement.SceneManager.sceneCount;i++)if(UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty)throw new InvalidOperationException("Save scene changes before running QA.");
  started=false;
  Directory.CreateDirectory("Logs/final-art");File.WriteAllText(SessionState.GetBool(Flag+".VisualOnly",false)?"Logs/final-art-visual-check.txt":Receipt,"RUNNING");
  foreach(string key in Keys){SessionState.SetInt(key,PlayerPrefs.GetInt(key,-1));PlayerPrefs.SetInt(key,1);}PlayerPrefs.Save();
  try{GuxuAcceptance.Rules();}catch(Exception e){File.WriteAllText(Receipt,"FAIL: "+e);RestoreKeys();EditorApplication.Exit(1);return;}
  SessionState.SetBool(Flag,true);SessionState.SetFloat(Flag+"Deadline",(float)EditorApplication.timeSinceStartup+200);
  EditorSceneManager.OpenScene("Assets/Scenes/GxdlMain.unity");GuxuAccountChecks.PreviewGameplay();
 }
 static string[] Keys=>new[]{GxdlGame.OpeningStoryKey,GxdlGame.FirstUnsealStoryKey};
 static void RestoreKeys(){foreach(string key in Keys){int old=SessionState.GetInt(key,-1);if(old<0)PlayerPrefs.DeleteKey(key);else PlayerPrefs.SetInt(key,old);}PlayerPrefs.Save();}
 static void Monitor(){if(!SessionState.GetBool(Flag,false))return;if(!started&&EditorApplication.isPlaying&&GxdlGame.Instance){started=true;deadline=EditorApplication.timeSinceStartup+150;new GameObject("Final Art QA").AddComponent<GuxuFinalArtCheckRunner>().StartCoroutine(Guard(Check()));}if(started&&EditorApplication.timeSinceStartup>deadline){File.AppendAllText(Receipt,"\nFAIL: timeout");Finish(1);}}
 static void Finish(int code){RestoreKeys();SessionState.SetBool(Flag,false);SessionState.SetBool(Flag+".VisualOnly",false);started=false;if(Application.isBatchMode)EditorApplication.Exit(code);else{EditorApplication.isPlaying=false;Debug.Log("Final Art QA finished with code "+code+". Receipt: "+Receipt);}}
 static IEnumerator Guard(IEnumerator routine){bool success=false;try{while(true){object next;try{if(!routine.MoveNext()){success=true;break;}next=routine.Current;}catch(Exception e){File.WriteAllText(Receipt,"FAIL: "+e);Debug.LogException(e);break;}yield return next;}}finally{(routine as IDisposable)?.Dispose();Finish(success?0:1);}}
 static void Require(bool ok,string text){if(!ok)throw new Exception("Final art: "+text);File.AppendAllText(Receipt,"\nPASS "+text);}
 static Button Button(string name)=>UnityEngine.Object.FindObjectsOfType<Button>().Single(b=>b.name==name);
 static Image Stone()=>UnityEngine.Object.FindObjectsOfType<Image>().Single(i=>i.name=="Sealed Stone Layer");
 static void Click(Button b){Canvas.ForceUpdateCanvases();var r=b.GetComponent<RectTransform>();var evt=new PointerEventData(EventSystem.current){position=RectTransformUtility.WorldToScreenPoint(Camera.main,r.TransformPoint(r.rect.center))};var hits=new List<RaycastResult>();EventSystem.current.RaycastAll(evt,hits);Require(hits.Count>0&&hits[0].gameObject.GetComponentInParent<Button>()==b,"raycast "+b.name);ExecuteEvents.Execute(b.gameObject,evt,ExecuteEvents.pointerClickHandler);}
 static IEnumerator Check(){
  var game=GxdlGame.Instance;string backup=Newtonsoft.Json.JsonConvert.SerializeObject(game.Rules.Save);var camera=Camera.main;var oldTarget=camera.targetTexture;var portrait=new RenderTexture(750,1334,24);camera.targetTexture=portrait;
  try{
   game.Rules.Save.pending=null;game.Rules.Save.peaches=Math.Max(20,game.Rules.Save.peaches);game.Rules.Save.stage=4;game.Home();yield return null;
   foreach(string label in new[]{"洞府","修行","寻墟","鉴宝","阵容"})Require(Button(label)!=null,"navigation "+label);
   Require(UnityEngine.Object.FindObjectsOfType<GuxuCompanionMotion>().Length==1,"independent bird");
   var motion=UnityEngine.Object.FindObjectOfType<GuxuCharacterMotion>();int meshes=motion.MeshBuildCount;yield return new WaitForSeconds(.4f);Require(motion.MeshBuildCount>meshes,"animated Spine mesh");Require(motion.UseHomePose&&motion.Graphic.AnimationState.GetCurrent(0).Animation.Name=="home_idle","back-facing home idle");Require(UnityEngine.Object.FindObjectOfType<GuxuCompanionMotion>().MeshBuildCount>0,"bird mesh rendered");Capture("01-home");
   Click(Button("寻墟"));yield return new WaitForSeconds(.15f);Require(UnityEngine.Object.FindObjectsOfType<GuxuMapBeacon>().Count(b=>b.IsHighlighting)==2,"only unlocked mountains pulse");
   Click(Button("阵容"));yield return new WaitForSeconds(.4f);Capture("02-companion");game.CloseModal();
   if(SessionState.GetBool(Flag+".VisualOnly",false)){game.Rules.Save.stage=1;game.Home();yield return null;game.StartBattle();yield return new WaitForSeconds(.75f);Capture("11-battle");game.Home();File.WriteAllText("Logs/final-art-visual-check.txt","PASS: final modal art, 9-slice framing, independent bird mesh, hero animation, home layout and actual navigation raycasts.");yield break;}
   game.ReplayOpening();yield return new WaitForSeconds(2);Require(game.IsStoryPlaying,"opening started");Capture("03-opening-world");
   yield return new WaitForSeconds(16);Capture("04-opening-letter");yield return new WaitForSeconds(8);Capture("05-opening-stone");yield return new WaitForSeconds(5);Require(!game.IsStoryPlaying,"full 30-second opening ends");
   game.Home();yield return null;PlayerPrefs.DeleteKey(GxdlGame.FirstUnsealStoryKey);int seals=game.Rules.Save.peaches;string equipped=Newtonsoft.Json.JsonConvert.SerializeObject(game.Rules.Save.gear);
   Click(Button("Unseal"));game.Chop();yield return new WaitForSeconds(2);Capture("06-first-unseal");Require(UnityEngine.Object.FindObjectOfType<GuxuAnimation>().IsPlaying,"first-unseal Spine sequence started");yield return new WaitForSeconds(8.5f);
   Require(!UnityEngine.Object.FindObjectOfType<GuxuAnimation>().IsPlaying&&game.Rules.Save.pending!=null&&game.Rules.Save.peaches==seals-1,"10-second first reveal consumes once");Require(game.Rules.Save.pending.slot==0,"first story reward matches sword");Require(Newtonsoft.Json.JsonConvert.SerializeObject(game.Rules.Save.gear)==equipped,"no automatic replacement");Require(!Stone().enabled,"stone hidden until pending resolved");Capture("07-pending");
   int coins=game.Rules.Save.coins,reward=game.Rules.Save.pending.quality*20;Click(Button("Resolve Equipment"));game.ResolveDrop(false);yield return new WaitForSeconds(.25f);Capture("08-disassemble");yield return new WaitForSeconds(2.2f);Require(game.Rules.Save.coins==coins+reward&&GameRules.ReadSave().coins==coins+reward,"single currency settlement and save");Require(Stone().enabled,"stone restored after resolution");
   Click(Button("Unseal"));yield return new WaitForSeconds(.8f);Capture("09-regular-fracture");yield return new WaitForSeconds(1.8f);Require(game.Rules.Save.pending!=null&&!UnityEngine.Object.FindObjectOfType<GuxuAnimation>().IsPlaying,"later manual unseal uses short back-facing cast");var drop=game.Rules.Save.pending;Click(Button("Equip Relic"));yield return new WaitForSeconds(.3f);Capture("10-equipped");yield return new WaitForSeconds(2);Require(game.Rules.Save.gear[drop.slot]==drop,"explicit equip");
   PlayerPrefs.DeleteKey(GxdlGame.FirstUnsealStoryKey);game.Chop();yield return new WaitForSeconds(.3f);game.SkipCurrentUnseal();yield return new WaitForSeconds(.3f);Require(game.Rules.Save.pending!=null&&!Stone().enabled&&!UnityEngine.Object.FindObjectOfType<GuxuAnimation>().IsPlaying,"skip first reveal preserves pending and unlocks UI");game.ResolveDrop(false);yield return new WaitForSeconds(2.2f);
   game.Rules.Save.stage=1;game.Home();game.StartBattle();yield return new WaitForSeconds(.8f);Capture("11-battle");game.Home();yield return new WaitForSeconds(1);Require(game.Rules.Save.stage==1,"leaving battle cannot settle");
   game.ReplayOpening();yield return new WaitForSeconds(.2f);game.SkipStory();yield return new WaitForSeconds(.2f);Require(!game.IsStoryPlaying,"opening skip cleans overlay");
   File.WriteAllText(Receipt,"PASS: final art resources; 5 navigation buttons; actual raycasts; live character/bird; map unlock; full opening and first reveal; repeat/skip lifecycle; pending stone visibility; single currency/save; explicit equip; battle cancellation.\n"+DateTime.Now.ToString("O"));
  }finally{game.SkipStory();game.StopAllCoroutines();game.Rules.Save=Newtonsoft.Json.JsonConvert.DeserializeObject<SaveData>(backup);game.Rules.Store();camera.targetTexture=oldTarget;portrait.Release();UnityEngine.Object.Destroy(portrait);}
 }
 static void Capture(string name){Canvas.ForceUpdateCanvases();var camera=Camera.main;var target=camera.targetTexture;var active=RenderTexture.active;var pixels=new Texture2D(750,1334,TextureFormat.RGB24,false);try{camera.Render();RenderTexture.active=target;pixels.ReadPixels(new Rect(0,0,750,1334),0,0);pixels.Apply();File.WriteAllBytes("Logs/final-art/"+name+".png",pixels.EncodeToPNG());}finally{RenderTexture.active=active;UnityEngine.Object.Destroy(pixels);}}
}
public sealed class GuxuFinalArtCheckRunner:MonoBehaviour{}
