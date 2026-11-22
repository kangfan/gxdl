using System;
using System.Collections;
using System.IO;
using System.Linq;
using Gxdl;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

[InitializeOnLoad]
public static class GuxuSealVisibilityCheck {
 const string Receipt="Logs/guxu-seal-visibility.txt";
 static bool started;
 static double deadline;
 static GuxuSealVisibilityCheck(){EditorApplication.update+=Monitor;}
 public static void RunBatch(){
  Directory.CreateDirectory("Logs");Directory.CreateDirectory("Assets/Screenshots");
  File.WriteAllText(Receipt,"RUNNING");SessionState.SetBool("GXDL.SealQA",true);
  EditorSceneManager.OpenScene("Assets/Scenes/GxdlMain.unity");GuxuAccountChecks.PreviewGameplay();
 }
 static void Monitor(){
  if(!SessionState.GetBool("GXDL.SealQA",false))return;
  if(!started){if(!EditorApplication.isPlaying||GxdlGame.Instance==null)return;started=true;deadline=EditorApplication.timeSinceStartup+70;Run();}
  if(File.ReadAllText(Receipt).StartsWith("PASS")){SessionState.SetBool("GXDL.SealQA",false);EditorApplication.Exit(0);}
  else if(EditorApplication.timeSinceStartup>deadline){SessionState.SetBool("GXDL.SealQA",false);File.AppendAllText(Receipt,"\nFAIL: timed out; see editor log.");EditorApplication.Exit(1);}
 }
 [MenuItem("GXDL/QA/Seal Visibility")]
 public static void Run(){GxdlGame.Instance.StartCoroutine(Check());}
 static Image Stone()=>UnityEngine.Object.FindObjectsOfType<Image>().Single(x=>x.name=="Sealed Stone Layer");
 static Image Relic()=>UnityEngine.Object.FindObjectsOfType<Image>().SingleOrDefault(x=>x.name=="Pending Relic Display");
 static Button Button(string name)=>UnityEngine.Object.FindObjectsOfType<Button>().Single(x=>x.name==name);
 static void Require(bool condition,string message){if(!condition)throw new Exception("Seal visibility: "+message);}
 static IEnumerator Check(){
  var game=GxdlGame.Instance;string backup=Newtonsoft.Json.JsonConvert.SerializeObject(game.Rules.Save);
  try{
   game.Rules.Save.pending=null;game.Rules.Save.peaches=Math.Max(10,game.Rules.Save.peaches);game.Home();
   Require(Stone().enabled,"stone before unseal");
   Button("Unseal").onClick.Invoke();yield return new WaitForSeconds(2.5f);
   Require(game.Rules.Save.pending!=null&&!Stone().enabled&&Relic()!=null,"pending relic replaces stone after fracture");
   Require(!Button("Unseal").interactable,"next unseal disabled");
   int seals=game.Rules.Save.peaches;game.Chop();Require(game.Rules.Save.peaches==seals,"no next seal consumption");
   yield return new WaitForSeconds(2);Require(!Stone().enabled,"waiting does not restore stone");
   game.Home();Require(!Stone().enabled&&Relic()!=null,"home refresh retains pending presentation");
   game.Rules.Store();game.Rules.Save=GameRules.ReadSave();game.Home();
   Require(!Stone().enabled&&Relic()!=null,"reloaded save retains pending presentation");
   Capture("guxu-pending-stone-hidden");yield return null;
   int coins=game.Rules.Save.coins,reward=game.Rules.Save.pending.quality*20;
   Button("Resolve Equipment").onClick.Invoke();game.ResolveDrop(false);
   Require(!Stone().enabled,"stone hidden throughout dismantle feedback");
   yield return new WaitForSeconds(2.2f);
   Require(Stone().enabled&&game.Rules.Save.pending==null&&Relic()==null&&Button("Unseal").interactable,"restore after dismantle completes");
   Require(game.Rules.Save.coins==coins+reward,"single dismantle settlement");
   game.Chop();yield return new WaitForSeconds(2.5f);var gear=game.Rules.Save.pending;
   Button("Equip Relic").onClick.Invoke();Require(!Stone().enabled,"stone hidden throughout equip feedback");
   yield return new WaitForSeconds(2.2f);Require(Stone().enabled&&game.Rules.Save.gear[gear.slot]==gear,"restore after equip completes");
   Capture("guxu-resolved-stone-restored");yield return null;
   // A screen rebuild during collection must not leave the restored home permanently locked.
   game.Chop();yield return new WaitForSeconds(2.5f);game.ResolveDrop(false);game.Home();
   yield return new WaitForSeconds(.3f);Require(Stone().enabled&&Button("Unseal").interactable,"cancelled collection releases presentation lock");
   File.WriteAllText(Receipt,"PASS: fracture/wait/home refresh/save reload hide stone and show relic; dismantle/equip restore only on completion; duplicate settlement and cancelled collection checked. "+DateTime.Now.ToString("O"));
  }finally{game.Rules.Save=Newtonsoft.Json.JsonConvert.DeserializeObject<SaveData>(backup);game.Rules.Store();game.Home();}
 }
 static void Capture(string name){
  // Batch mode does not resume WaitForEndOfFrame; render the UI camera explicitly.
  Canvas.ForceUpdateCanvases();var camera=Camera.main;var oldTarget=camera.targetTexture;var oldActive=RenderTexture.active;
  var target=new RenderTexture(750,1334,24);var pixels=new Texture2D(750,1334,TextureFormat.RGB24,false);
  try{camera.targetTexture=target;camera.Render();RenderTexture.active=target;pixels.ReadPixels(new Rect(0,0,750,1334),0,0);pixels.Apply();File.WriteAllBytes("Assets/Screenshots/"+name+".png",pixels.EncodeToPNG());}
  finally{camera.targetTexture=oldTarget;RenderTexture.active=oldActive;UnityEngine.Object.Destroy(pixels);target.Release();UnityEngine.Object.Destroy(target);}
 }
}
