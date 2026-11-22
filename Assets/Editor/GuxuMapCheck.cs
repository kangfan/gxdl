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
public static class GuxuMapCheck {
 const string Receipt="Logs/guxu-map-check.txt";
 static bool started;
 static double deadline;
 static GuxuMapCheck(){EditorApplication.update+=Monitor;}
 public static void RunBatch(){
  Directory.CreateDirectory("Logs");Directory.CreateDirectory("Assets/Screenshots");
  File.WriteAllText(Receipt,"RUNNING");SessionState.SetBool("GXDL.MapQA",true);
  foreach(var name in new[]{"background"}){
   var importer=(TextureImporter)AssetImporter.GetAtPath("Assets/Resources/Game/Guxu/Map/"+name+".png");
   importer.alphaIsTransparency=name!="background";importer.mipmapEnabled=false;importer.maxTextureSize=name=="background"?2048:512;importer.npotScale=TextureImporterNPOTScale.None;importer.SaveAndReimport();
  }
  GuxuAcceptance.Rules();
  EditorSceneManager.OpenScene("Assets/Scenes/GxdlMain.unity");GuxuAccountChecks.PreviewGameplay();
 }
 static void Monitor(){
  if(!SessionState.GetBool("GXDL.MapQA",false))return;
  if(!started){if(!EditorApplication.isPlaying||GxdlGame.Instance==null)return;started=true;deadline=EditorApplication.timeSinceStartup+70;Run();}
  if(File.ReadAllText(Receipt).StartsWith("FAIL")){SessionState.SetBool("GXDL.MapQA",false);EditorApplication.Exit(1);}
  else if(File.ReadAllText(Receipt).StartsWith("PASS")){SessionState.SetBool("GXDL.MapQA",false);EditorApplication.Exit(0);}
  else if(EditorApplication.timeSinceStartup>deadline){SessionState.SetBool("GXDL.MapQA",false);File.AppendAllText(Receipt,"\nFAIL: timed out; see editor log.");EditorApplication.Exit(1);}
 }
 [MenuItem("GXDL/QA/Ruins Map")]
 public static void Run(){new GameObject("Map QA Runner").AddComponent<GuxuMapCheckRunner>().StartCoroutine(Guard());}
 static Image Stone()=>UnityEngine.Object.FindObjectsOfType<Image>().Single(x=>x.name=="Sealed Stone Layer");
 static Image Relic()=>UnityEngine.Object.FindObjectsOfType<Image>().SingleOrDefault(x=>x.name=="Pending Relic Display");
 static Button Button(string name)=>UnityEngine.Object.FindObjectsOfType<Button>().Single(x=>x.name==name);
 static void Require(bool condition,string message){if(!condition)throw new Exception("Ruins map: "+message);}
 static IEnumerator Guard(){
  var check=Check();while(true){object next;try{if(!check.MoveNext())yield break;next=check.Current;}catch(Exception ex){File.WriteAllText(Receipt,"FAIL: "+ex);Debug.LogException(ex);yield break;}yield return next;}
 }
 static void Click(Button button){
  Canvas.ForceUpdateCanvases();var rect=button.GetComponent<RectTransform>();
  var pointer=new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current){position=RectTransformUtility.WorldToScreenPoint(Camera.main,rect.TransformPoint(rect.rect.center))};
  var hits=new System.Collections.Generic.List<UnityEngine.EventSystems.RaycastResult>();UnityEngine.EventSystems.EventSystem.current.RaycastAll(pointer,hits);
  Require(hits.Count>0&&hits[0].gameObject.GetComponentInParent<Button>()==button,"unobstructed touch target: "+button.name+" at "+pointer.position+" camera "+Camera.main.pixelRect+" hit "+(hits.Count==0?"none":hits[0].gameObject.name));
  UnityEngine.EventSystems.ExecuteEvents.Execute(button.gameObject,pointer,UnityEngine.EventSystems.ExecuteEvents.pointerClickHandler);
 }
 static IEnumerator Check(){
  var game=GxdlGame.Instance;string backup=Newtonsoft.Json.JsonConvert.SerializeObject(game.Rules.Save);
  var camera=Camera.main;var previousTarget=camera.targetTexture;var portrait=new RenderTexture(750,1334,24);camera.targetTexture=portrait;
  try{
   yield return null;
   game.Rules.Save.pending=null;
   Require(!game.IsRuinsRegionUnlocked(-1)&&!game.IsRuinsRegionUnlocked(3),"invalid region indexes rejected");
   foreach(int progress in new[]{1,4,7}){
    game.Rules.Save.stage=progress;game.Home();yield return null;
    for(int i=0;i<3;i++)Require(Button("Ruins Region "+i).interactable==(i*3+1<=progress),"unlock at progress "+progress+" region "+i);
   }
   game.Rules.Save.stage=1;game.Home();yield return null;
   Require(!UnityEngine.Object.FindObjectsOfType<Button>().Any(x=>x.name=="遗址"),"legacy entrance removed");
   Click(Button("Ruins Region 2"));Require(!UnityEngine.Object.FindObjectsOfType<Button>().Any(x=>x.name=="Ruins Challenge"),"locked mountain cannot open challenge");
   game.OpenRuinsRegion(2);Require(!UnityEngine.Object.FindObjectsOfType<Button>().Any(x=>x.name=="Ruins Challenge"),"direct locked route also blocked");
   game.Home();Capture("guxu-map-locked");
   game.Rules.Save.stage=6;game.Home();yield return null;Capture("guxu-map-home");
   Click(Button("古墟"));yield return new WaitForSeconds(.15f);
   var beacons=UnityEngine.Object.FindObjectsOfType<GuxuMapBeacon>();Require(beacons.Count(x=>x.IsHighlighting)==2,"only unlocked mountains highlight");
   var moving=Button("Ruins Region 0").transform.Find("Mountain Spirit Light").GetComponent<GuxuSoftVfx>();float origin=moving.color.a;
   yield return new WaitForSeconds(.2f);Require(Mathf.Abs(origin-moving.color.a)>.01f,"entrance light actually pulses");Capture("guxu-map-highlight");
   Click(Button("古墟"));yield return null;Require(UnityEngine.Object.FindObjectsOfType<GuxuMapBeacon>().Length==3,"repeated tab tap does not duplicate nodes");
   Click(Button("Ruins Region 0"));yield return new WaitForSeconds(.25f);Require(game.CurrentStage.id==3,"old region selects its last room");game.CloseModal();
   Click(Button("Ruins Region 1"));yield return new WaitForSeconds(.25f);Require(game.CurrentStage.id==6,"current region selects current room");Capture("guxu-map-challenge");
   Click(Button("Ruins Challenge"));yield return new WaitForSeconds(.3f);Require(UnityEngine.Object.FindObjectsOfType<Text>().Any(x=>x.text=="灵木守卫"),"correct guardian in battle");Capture("guxu-map-battle");
   Click(Button("返回古墟"));yield return new WaitForSeconds(.3f);Require(game.Rules.Save.stage==6&&UnityEngine.Object.FindObjectsOfType<GuxuMapBeacon>().Length==3,"cancel battle restores map without settlement");
   game.Rules.Save.gear[0].attack=100000;game.Rules.Save.gear[0].hp=100000;game.Rules.Save.gear[0].speed=100000;int seals=game.Rules.Save.peaches;
   Click(Button("Ruins Region 1"));yield return new WaitForSeconds(.25f);Click(Button("Ruins Challenge"));
   float victoryDeadline=Time.unscaledTime+5;while(game.Rules.Save.stage==6&&Time.unscaledTime<victoryDeadline)yield return null;
   yield return new WaitForSeconds(.25f);
   Require(game.Rules.Save.stage==7&&game.Rules.Save.peaches==seals+5,"current victory advances progress and gives one reward");
   var returnButton=UnityEngine.Object.FindObjectsOfType<Button>().Single(x=>x.name=="返回古墟"&&x.transform.parent.name=="遗址通关");Click(returnButton);yield return null;
   Require(Button("Ruins Region 2").interactable,"next mountain unlocks after victory");Capture("guxu-map-all-open");
   Click(Button("Ruins Region 2"));game.Rules.Save.stage=1;game.StartBattle();Require(!UnityEngine.Object.FindObjectsOfType<Button>().Any(x=>x.name=="Battle Speed"),"stale selection cannot enter locked battle");
   game.Home();game.Rules.Save.peaches=Math.Max(10,game.Rules.Save.peaches);game.Chop();yield return new WaitForSeconds(.65f);Capture("guxu-map-fracture");yield return new WaitForSeconds(2);
   Require(game.Rules.Save.pending!=null&&!Stone().enabled&&Relic()!=null,"pending relic still keeps stone hidden");Capture("guxu-map-pending");
   string pending=Newtonsoft.Json.JsonConvert.SerializeObject(game.Rules.Save.pending);Click(Button("Ruins Region 0"));game.CloseModal();game.Home();Require(pending==Newtonsoft.Json.JsonConvert.SerializeObject(game.Rules.Save.pending),"map navigation preserves pending equipment");
   game.ResolveDrop(false);yield return new WaitForSeconds(2.2f);Require(Stone().enabled,"stone returns after resolution");
   File.WriteAllText(Receipt,"PASS: three independent mountains; unlock boundaries 1/4/7; actual UI raycasts; locked/stale routes blocked; ancient-ruins tab highlights only open nodes; repeated taps; region selection; battle cancel; victory reward and new mountain; unseal/pending/resolve regression. "+DateTime.Now.ToString("O"));
  }finally{game.StopAllCoroutines();game.Rules.Save=Newtonsoft.Json.JsonConvert.DeserializeObject<SaveData>(backup);game.Rules.Store();game.Home();camera.targetTexture=previousTarget;portrait.Release();UnityEngine.Object.Destroy(portrait);}
 }
 static void Capture(string name){
  // Batch mode does not resume WaitForEndOfFrame; render the UI camera explicitly.
  Canvas.ForceUpdateCanvases();var camera=Camera.main;var oldTarget=camera.targetTexture;var oldActive=RenderTexture.active;
  var target=new RenderTexture(750,1334,24);var pixels=new Texture2D(750,1334,TextureFormat.RGB24,false);
  try{camera.targetTexture=target;camera.Render();RenderTexture.active=target;pixels.ReadPixels(new Rect(0,0,750,1334),0,0);pixels.Apply();File.WriteAllBytes("Assets/Screenshots/"+name+".png",pixels.EncodeToPNG());}
  finally{camera.targetTexture=oldTarget;RenderTexture.active=oldActive;UnityEngine.Object.Destroy(pixels);target.Release();UnityEngine.Object.Destroy(target);}
 }
}

public sealed class GuxuMapCheckRunner : MonoBehaviour {}
