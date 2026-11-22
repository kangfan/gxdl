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
public static class GuxuFeedbackCheck {
 const string Receipt="Logs/guxu-feedback.txt";
 static bool started;
 static double deadline;
 static GuxuFeedbackCheck(){EditorApplication.update+=Monitor;}
 public static void RunBatch(){
  Directory.CreateDirectory("Logs");Directory.CreateDirectory("Assets/Screenshots");
  File.WriteAllText(Receipt,"RUNNING");SessionState.SetBool("GXDL.FeedbackQA",true);
  EditorSceneManager.OpenScene("Assets/Scenes/GxdlMain.unity");GuxuAccountChecks.PreviewGameplay();
 }
 static void Monitor(){
  if(!SessionState.GetBool("GXDL.FeedbackQA",false))return;
  if(!started){if(!EditorApplication.isPlaying||GxdlGame.Instance==null)return;started=true;deadline=EditorApplication.timeSinceStartup+70;Run();}
  if(File.ReadAllText(Receipt).StartsWith("PASS")){SessionState.SetBool("GXDL.FeedbackQA",false);EditorApplication.Exit(0);}
  else if(EditorApplication.timeSinceStartup>deadline){SessionState.SetBool("GXDL.FeedbackQA",false);File.AppendAllText(Receipt,"\nFAIL: timed out; see editor log.");EditorApplication.Exit(1);}
 }
 [MenuItem("GXDL/QA/Power and Quality Feedback")]
 public static void Run(){GxdlGame.Instance.StartCoroutine(Check());}
 static Image Stone()=>UnityEngine.Object.FindObjectsOfType<Image>().Single(x=>x.name=="Sealed Stone Layer");
 static Image Relic()=>UnityEngine.Object.FindObjectsOfType<Image>().SingleOrDefault(x=>x.name=="Pending Relic Display");
 static Button Button(string name)=>UnityEngine.Object.FindObjectsOfType<Button>().Single(x=>x.name==name);
 static void Require(bool condition,string message){if(!condition)throw new Exception("Feedback: "+message);}
 static Text Power()=>UnityEngine.Object.FindObjectsOfType<Text>().SingleOrDefault(x=>x.name=="Combat Power Change");
 static void Invoke(GxdlGame game,string method)=>typeof(GxdlGame).GetMethod(method,System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).Invoke(game,null);
 static Gear Copy(Gear g)=>Newtonsoft.Json.JsonConvert.DeserializeObject<Gear>(Newtonsoft.Json.JsonConvert.SerializeObject(g));
 static IEnumerator Check(){
  var game=GxdlGame.Instance;string backup=Newtonsoft.Json.JsonConvert.SerializeObject(game.Rules.Save);
  try{
   game.Rules.Save.pending=null;game.Home();
   for(int i=0;i<12;i++){game.Rules.Save.gear[i].quality=i%5+1;game.Rules.Save.gear[i].level=i%3+1;}
   Invoke(game,"ShowBag");
   var fills=UnityEngine.Object.FindObjectsOfType<Image>().Where(x=>x.name=="Gear Quality Background").ToArray();
   Require(fills.Length==12,"every inventory slot has a shared dark background");
   for(int q=1;q<=5;q++)Require(fills.Any(x=>x.sprite==GuxuItemIcons.Get("slot-background")&&!x.raycastTarget&&x.transform.GetSiblingIndex()==0&&x.transform.parent.Find("Item Quality Frame").GetComponent<Image>().sprite==GuxuUiArt.QualityFrame(q)),"dark slot with true quality border: "+q);
   yield return new WaitForSeconds(.25f);Capture("guxu-quality-backgrounds");game.CloseModal();
   var original=Copy(game.Rules.Save.gear[0]);
   game.Rules.Save.pending=Copy(original);game.Rules.Save.pending.attack+=20;game.Rules.Save.pending.quality=4;game.Home();
   Require(Power()==null,"preview alone does not change power");
   Capture("guxu-quality-pending");
   int before=game.Rules.Attributes.Power;game.ResolveDrop(true);
   var label=Power();Require(label!=null&&label.text=="战力 +100"&&game.Rules.Attributes.Power==before+100,"actual equip delta");
   Require(label.color.g>label.color.r,"increase is green");float y=label.rectTransform.anchoredPosition.y;
   yield return new WaitForSeconds(.3f);Require(label.rectTransform.anchoredPosition.y>y,"increase moves up");Capture("guxu-power-increase");
   yield return new WaitForSeconds(1.9f);Require(Power()==null,"number expires");
   game.Rules.Save.pending=Copy(original);game.ResolveDrop(true);label=Power();
   Require(label!=null&&label.text=="战力 -100"&&label.color.r>label.color.g,"decrease is red and signed");y=label.rectTransform.anchoredPosition.y;
   yield return new WaitForSeconds(.3f);Require(label.rectTransform.anchoredPosition.y<y,"decrease moves down");Capture("guxu-power-decrease");
   yield return new WaitForSeconds(1.9f);
   game.Rules.Save.pending=Copy(original);game.ResolveDrop(true);Require(Power()==null,"equal power has no number");yield return new WaitForSeconds(2.2f);
   game.Rules.Save.pending=Copy(original);game.Rules.Save.pending.attack+=50;game.ResolveDrop(false);Require(Power()==null,"dismantle has no number");yield return new WaitForSeconds(2.2f);
   game.Rules.Save.level=1;game.Rules.Save.exp=game.Rules.ExpNeeded;before=game.Rules.Attributes.Power;Invoke(game,"Breakthrough");
   int delta=game.Rules.Attributes.Power-before;Require(delta>0&&Power()!=null&&Power().text=="战力 +"+delta,"breakthrough uses actual power delta");
   game.Home();Require(Power()==null,"screen rebuild cleans up number");
   File.WriteAllText(Receipt,"PASS: five quality fills in inventory, pending item; actual equip +100/up/green, -100/down/red; expiry, equal power, dismantle, breakthrough, screen cleanup. "+DateTime.Now.ToString("O"));
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
