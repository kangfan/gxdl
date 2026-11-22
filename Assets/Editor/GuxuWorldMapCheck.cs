using System;
using System.Collections;
using System.IO;
using System.Linq;
using Gxdl;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public static class GuxuWorldMapCheck {
 const string Output="Reference/WorldMap20261109";
 static bool running;
 [MenuItem("GXDL/QA/World Map Art")]
 public static void Run(){
  if(running||!Application.isPlaying||!GxdlGame.Instance)throw new InvalidOperationException("Enter Play mode and wait for the previous check to finish.");
  running=true;Directory.CreateDirectory(Output);File.WriteAllText(Output+"/ui-check.txt","RUNNING");
  typeof(GxdlSetup).GetMethod("SetPortraitGameView",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Static).Invoke(null,null);
  EditorWindow.GetWindow(typeof(Editor).Assembly.GetType("UnityEditor.GameView")).Focus();
  GxdlGame.Instance.StartCoroutine(Guard(Check(GxdlGame.Instance)));
 }
 static IEnumerator Guard(IEnumerator check){
  try{while(true){bool more;try{more=check.MoveNext();}catch(Exception e){File.WriteAllText(Output+"/ui-check.txt","FAIL: "+e);Debug.LogException(e);yield break;}if(!more)break;yield return check.Current;}}
  finally{(check as IDisposable)?.Dispose();running=false;}
 }
 static void Require(bool value,string message){if(!value)throw new Exception("World map: "+message);}
 static Button FindButton(string name)=>UnityEngine.Object.FindObjectsOfType<Button>().Single(b=>b.name==name&&b.gameObject.activeInHierarchy);
 static string Label(string name)=>UnityEngine.Object.FindObjectsOfType<Text>().Single(t=>t.name==name&&t.gameObject.activeInHierarchy).text;
 static void Click(string name){
  var b=FindButton(name);Require(b.interactable,"enabled "+name);Canvas.ForceUpdateCanvases();
  var rect=(RectTransform)b.transform;var camera=b.GetComponentInParent<Canvas>().worldCamera;
  var pointer=new PointerEventData(EventSystem.current){position=RectTransformUtility.WorldToScreenPoint(camera,rect.TransformPoint(rect.rect.center)),button=PointerEventData.InputButton.Left};
  var hits=new System.Collections.Generic.List<RaycastResult>();EventSystem.current.RaycastAll(pointer,hits);
  Require(hits.Count>0&&hits[0].gameObject==b.gameObject,"unobstructed hit area "+name+"; hits: "+string.Join(",",hits.Select(h=>h.gameObject.name))+"; point="+pointer.position+"; screen="+Screen.width+"x"+Screen.height+"; depth="+b.image.depth+"; cull="+b.image.canvasRenderer.cull+"; alpha="+b.image.canvasRenderer.GetAlpha()+"; raycast="+b.image.raycastTarget);
  ExecuteEvents.Execute(b.gameObject,pointer,ExecuteEvents.pointerClickHandler);
 }
 static IEnumerator Check(GxdlGame g){
  var previous=g.Rules.Save;int opening=PlayerPrefs.GetInt(GxdlGame.OpeningStoryKey,-1),first=PlayerPrefs.GetInt(GxdlGame.FirstUnsealStoryKey,-1);
  try{
   PlayerPrefs.SetInt(GxdlGame.OpeningStoryKey,1);PlayerPrefs.SetInt(GxdlGame.FirstUnsealStoryKey,1);g.SkipStory();
   float storyDeadline=Time.realtimeSinceStartup+3;while(g.IsStoryPlaying&&Time.realtimeSinceStartup<storyDeadline)yield return null;
   Require(!g.IsStoryPlaying,"opening cinematic finished");
   Require(GuxuUiArt.Optional("world-layout")!=null,"skin imported");
   int[] stages={1,41,201};string[] files={"01-新玩家","02-第三章解锁","03-全部通关"};
   for(int state=0;state<stages.Length;state++){
    int stage=stages[state],last=Math.Min(10,(stage-1)/20+1);
    g.Rules.Save=new GameRules(g.Rules.Config,new SaveData{stage=stage,equipmentSchema=1,level=20,peaches=40+state,coins=state==2?50000:state*12345}).Save;
    g.Home();g.ShowWorldMap();Require(g.ExpansionScreen=="world","map opened; canPlay="+g.CanPlay+"; story="+g.IsStoryPlaying);yield return new WaitForEndOfFrame();
    Require(g.ExpansionScreen=="world","map survives first render; screen="+g.ExpansionScreen);
    Require(Label("World Progress")=="主线 "+(stage-1)+" / 200 关","live main progress");
    Require(Label("World Tokens")=="破封符 "+(40+state),"live tokens");
    Require(Label("World Dust")=="灵尘 "+g.Rules.Save.coins,"live dust");
    Require(Label("World Power")=="战力 "+g.Rules.Attributes.Power,"live power");
    Require(UnityEngine.Object.FindObjectsOfType<Button>().Count(b=>b.name.StartsWith("Chapter ")&&b.gameObject.activeInHierarchy)==10,"10 chapter hit areas");
    for(int i=1;i<=10;i++){
     Require(FindButton("Chapter "+i).interactable==(i<=last),"chapter lock "+i);
     string expected=i<=last?"已通关 "+Mathf.Clamp(stage-1-(i-1)*20,0,20)+" / 20":"通过第"+((i-1)*20)+"关解锁";
     Require(Label("Chapter Progress "+i)==expected,"chapter progress "+i);
    }
    yield return new WaitForSeconds(.2f);yield return new WaitForEndOfFrame();
    var image=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes(Output+"/"+files[state]+".png",image.EncodeToPNG());UnityEngine.Object.Destroy(image);
    Click("Chapter "+last);Require(g.ExpansionScreen=="chapter","chapter opens");
    Require(UnityEngine.Object.FindObjectsOfType<Button>().Count(b=>b.name.StartsWith("Stage ")&&b.gameObject.activeInHierarchy)==20,"20 stages remain accessible");
   }
   // Graphic raycasts need a rendered frame to populate the new CanvasRenderer depth.
   g.ShowWorldMap();yield return new WaitForEndOfFrame();Click("资源秘境");Require(g.ExpansionScreen=="dungeons","resource dungeons open");
   g.ShowWorldMap();yield return new WaitForEndOfFrame();Click("通天古塔");Require(g.ExpansionScreen=="tower","tower opens");
   g.ShowWorldMap();yield return new WaitForEndOfFrame();Click("World Back");Require(g.ExpansionScreen=="home","back opens home");
   File.WriteAllText(Output+"/ui-check.txt","PASS: imported world skin; 3 progress states (0/40/200 clears); all 10 chapter locks and live progress; live tokens/dust/power; actual pointer raycasts and chapter navigation; 20 stage entries; dungeon/tower/back navigation. Three 750x1334 screenshots. Original player object and story preferences restored; no fixture save written.");
  }finally{
   g.Rules.Save=previous;
   if(opening<0)PlayerPrefs.DeleteKey(GxdlGame.OpeningStoryKey);else PlayerPrefs.SetInt(GxdlGame.OpeningStoryKey,opening);
   if(first<0)PlayerPrefs.DeleteKey(GxdlGame.FirstUnsealStoryKey);else PlayerPrefs.SetInt(GxdlGame.FirstUnsealStoryKey,first);
   g.Home();
  }
 }
}
