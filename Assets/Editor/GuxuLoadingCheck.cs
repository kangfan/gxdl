using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Gxdl;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Object=UnityEngine.Object;

[InitializeOnLoad]
public static class GuxuLoadingCheck {
 const string Key="GXDL.LoadingQA.",Output="Reference/Loading20261110";
 const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
 static bool running;
 static GuxuLoadingCheck(){EditorApplication.update+=Monitor;EditorApplication.playModeStateChanged+=Restore;}
 [MenuItem("Guxu/Preview/Loading Screen (72%)")]
 public static void Preview(){Begin(false);}
 [MenuItem("Guxu/QA/Verify Loading Screen")]
 public static void Run(){Begin(true);}
 static void Begin(bool test){
  if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Stop Play Mode first.");
  Directory.CreateDirectory(Output);SessionState.SetBool(Key+"active",true);SessionState.SetBool(Key+"test",test);
  if(test){File.WriteAllText(Output+"/validation.txt","RUNNING");SessionState.SetString(Key+"scope",GuxuAccountConfig.Hash("loading-qa:"+Guid.NewGuid()));}
  GuxuAccountChecks.Preview();
 }
 static void Monitor(){
  if(!SessionState.GetBool(Key+"active",false)||!EditorApplication.isPlaying||!GxdlGame.Instance||running)return;
  running=true;
  if(SessionState.GetBool(Key+"test",false))new GameObject("Loading QA").AddComponent<GuxuLoadingCheckRunner>().StartCoroutine(Guard(Check()));
  else{var screen=Create(GxdlGame.Instance);screen.SetProgress(.72f,"正在加载场景与角色…");}
 }
 static void Restore(PlayModeStateChange state){
  if(state!=PlayModeStateChange.EnteredEditMode||!SessionState.GetBool(Key+"active",false))return;
  if(SessionState.GetBool(Key+"test",false)){
   string previous=GameRules.AccountScope;
   try{GameRules.SelectAccount(SessionState.GetString(Key+"scope",""));
    foreach(string suffix in new[]{"",".tmp",".bak",".before-equipment15",".before-expansion",".invalid"})if(File.Exists(GameRules.SavePath+suffix))File.Delete(GameRules.SavePath+suffix);
    PlayerPrefs.DeleteKey(GxdlGame.OpeningStoryKey);PlayerPrefs.DeleteKey(GxdlGame.FirstUnsealStoryKey);PlayerPrefs.Save();
   }finally{GameRules.SelectAccount(previous);}
   if(File.ReadAllText(Output+"/validation.txt")=="RUNNING")File.WriteAllText(Output+"/validation.txt","FAIL: interrupted");
  }
  SessionState.SetBool(Key+"active",false);SessionState.SetBool(Key+"test",false);running=false;
 }
 static object Invoke(GxdlGame game,string method,params object[] args)=>typeof(GxdlGame).GetMethod(method,Private).Invoke(game,args);
 static GuxuLoadingScreen Create(GxdlGame game){
  Invoke(game,"Clear");var parent=(RectTransform)typeof(GxdlGame).GetField("view",Private).GetValue(game);parent.name="Loading Art Preview";
  var args=new object[]{parent,null};Invoke(game,"CreateEntryLoadingView",args);return (GuxuLoadingScreen)args[1];
 }
 static void Require(bool value,string message){if(!value)throw new Exception("Loading QA: "+message);}
 static IEnumerator Until(Func<bool> condition,string message,float timeout=8){
  float end=Time.realtimeSinceStartup+timeout;while(!condition()&&Time.realtimeSinceStartup<end)yield return null;Require(condition(),message);
 }
 static void ClickOpeningSkip(){
  Canvas.ForceUpdateCanvases();var button=GameObject.Find("Skip Story").GetComponent<Button>();
  var label=button.GetComponentInChildren<Text>();var rect=(RectTransform)button.transform;
  Require(button.interactable&&label.text=="跳过动画"&&label.preferredWidth<=label.rectTransform.rect.width,"readable skip button is available");
  var pointer=new PointerEventData(EventSystem.current){button=PointerEventData.InputButton.Left,position=RectTransformUtility.WorldToScreenPoint(Camera.main,rect.TransformPoint(rect.rect.center))};
  var hits=new List<RaycastResult>();EventSystem.current.RaycastAll(pointer,hits);
  Require(hits.Count>0&&hits[0].gameObject.GetComponentInParent<Button>()==button,"skip button receives pointer input");
  ExecuteEvents.Execute(button.gameObject,pointer,ExecuteEvents.pointerClickHandler);
 }
 static IEnumerator Guard(IEnumerator routine){
  var stack=new Stack<IEnumerator>();stack.Push(routine);
  try{while(stack.Count>0){
   var current=stack.Peek();bool more;object next;
   try{more=current.MoveNext();next=more?current.Current:null;}catch(Exception error){File.WriteAllText(Output+"/validation.txt","FAIL: "+error);Debug.LogException(error);yield break;}
   if(!more){(stack.Pop() as IDisposable)?.Dispose();continue;}if(next is IEnumerator nested){stack.Push(nested);continue;}yield return next;
  }}finally{while(stack.Count>0)(stack.Pop() as IDisposable)?.Dispose();EditorApplication.isPlaying=false;}
 }
 static IEnumerator Check(){
  var game=GxdlGame.Instance;yield return null;var screen=Create(game);var camera=Camera.main;
  float originalScale=Time.timeScale;var previous=camera.targetTexture;RenderTexture target=null;
  try{
   target=new RenderTexture(750,1334,24);target.Create();camera.targetTexture=target;Time.timeScale=0;yield return null;
   Require(!game.CanPlay&&game.Rules==null,"preview keeps authentication gate closed");
   screen.SetProgress(0,"正在读取游戏配置…");Capture(target,"loading-000");
   Require(!screen.IsComplete&&screen.DisplayedProgress==0,"zero is empty and incomplete");
   screen.SetProgress(.25f,"正在读取角色进度…");yield return Until(()=>screen.DisplayedProgress==.25f,"25 percent at timeScale zero");Capture(target,"loading-025");
   Vector2 petal=GameObject.Find("Loading Petal 0").GetComponent<RectTransform>().anchoredPosition;
   screen.SetProgress(.72f,"正在加载场景与角色…");yield return Until(()=>screen.DisplayedProgress==.72f,"72 percent");Capture(target,"loading-072");
   Require(petal!=GameObject.Find("Loading Petal 0").GetComponent<RectTransform>().anchoredPosition,"petals animate unscaled");
   screen.SetProgress(.1f,"正在加载场景与角色…");yield return new WaitForSecondsRealtime(.2f);
   Require(screen.DisplayedProgress==.72f&&!screen.IsComplete,"no regression, invented progress or early completion");
   bool invalid=false;try{screen.SetProgress(float.NaN,"");}catch(ArgumentOutOfRangeException){invalid=true;}Require(invalid,"invalid progress rejected");
   foreach(var size in new[]{new Vector2Int(1080,2340),new Vector2Int(1600,900)}){
    camera.targetTexture=null;target.Release();Object.Destroy(target);target=new RenderTexture(size.x,size.y,24);target.Create();camera.targetTexture=target;
    yield return null;Canvas.ForceUpdateCanvases();yield return null;
    var bg=GameObject.Find("Loading Background").GetComponent<RectTransform>();var canvas=(RectTransform)bg.GetComponentInParent<Canvas>().transform;
    float scale=screen.transform.localScale.x;Require(bg.rect.width*scale>=canvas.rect.width-.1f&&bg.rect.height*scale>=canvas.rect.height-.1f,"background covers "+size);
    foreach(string name in new[]{"Loading Logo","Loading Frame","Loading Phase"}){
     var corners=new Vector3[4];GameObject.Find(name).GetComponent<RectTransform>().GetWorldCorners(corners);
     foreach(var corner in corners){var local=canvas.InverseTransformPoint(corner);Require(canvas.rect.Contains(local),name+" stays visible at "+size);}
    }
    Capture(target,"loading-"+size.x+"x"+size.y);
   }
   camera.targetTexture=null;target.Release();Object.Destroy(target);target=new RenderTexture(750,1334,24);target.Create();camera.targetTexture=target;yield return null;
   screen.SetProgress(.999f,"正在准备进入古墟…");yield return Until(()=>screen.DisplayedProgress==.999f,"almost complete");
   Require(GameObject.Find("Loading Percent").GetComponent<Text>().text=="99%"&&!screen.IsComplete,"never displays 100 before target one");
   screen.SetProgress(1,"加载完成");Require(!screen.IsComplete,"completion waits for visible fill");yield return Until(()=>screen.IsComplete,"100 percent acknowledgement");Capture(target,"loading-100");
   Require(GameObject.Find("Loading Fill").GetComponent<Image>().fillAmount==1,"full bar at completion");
   Invoke(game,"RenderAccountState");yield return null;Require(!screen&&!Object.FindObjectOfType<GuxuLoadingScreen>(),"view and particles destroyed with page");
   Time.timeScale=originalScale;
   var account=game.GetComponent<GuxuAccountSession>();string scope=SessionState.GetString(Key+"scope","");
   typeof(GuxuAccountSession).GetField("<SaveScope>k__BackingField",Private).SetValue(account,scope);
   var gate=(GuxuAccountGate)typeof(GuxuAccountSession).GetField("gate",Private).GetValue(account);
   gate.Begin();gate.Receive(500);Invoke(game,"RenderAccountState");
   Require(game.EntryStage==GuxuEntryStage.Loading&&Object.FindObjectOfType<GuxuLoadingScreen>()&&!game.CanPlay,"real flow uses production loading view");
   yield return Until(()=>game.EntryStage!=GuxuEntryStage.Loading,"real resource loading completes",20);
   Require(game.EntryStage==GuxuEntryStage.Opening&&game.IsStoryPlaying&&!game.CanPlay,"first entry transitions to opening after completion");
   yield return new WaitForSecondsRealtime(.8f);Capture(target,"opening-skip");
   ClickOpeningSkip();yield return Until(()=>game.CanPlay,"opening skip button enters Home");
   gate.Receive(1030);Invoke(game,"RenderAccountState");gate.Begin();gate.Receive(500);Invoke(game,"RenderAccountState");
   yield return Until(()=>game.IsStoryPlaying,"returning account also plays opening in Editor",20);Require(!game.CanPlay,"opening blocks Home for returning account");
   yield return new WaitForSecondsRealtime(.8f);ClickOpeningSkip();yield return Until(()=>game.CanPlay,"returning account enters Home after clicking skip");
   gate.Receive(1030);Invoke(game,"RenderAccountState");gate.Begin();gate.Receive(500);Invoke(game,"RenderAccountState");
   var cancelled=Object.FindObjectOfType<GuxuLoadingScreen>();gate.Receive(1030);Invoke(game,"RenderAccountState");yield return null;
   Require(!cancelled&&game.EntryStage==GuxuEntryStage.Login&&!game.CanPlay,"restriction cancels loading and restores login");
   File.WriteAllText(Output+"/validation.txt","PASS: original sheet assets; 0/25/72/100 screenshots; monotonic target-bound progress; 99% until completed; unscaled fill/petals; 100% acknowledgement; 750x1334,1080x2340,1600x900 coverage; page cleanup; real resource loading with production UI -> opening -> Home; readable skip-animation button receives actual pointer raycast and enters Home; Editor returning account replays opening and skip remains usable; restriction cancels loading. Editor-only isolated account gate; no authentication or SMS request. "+DateTime.Now.ToString("O"));
   Debug.Log("[Loading QA] All checks passed.");
  }finally{Time.timeScale=originalScale;camera.targetTexture=previous;if(target){target.Release();Object.Destroy(target);}}
 }
 static void Capture(RenderTexture target,string name){
  var previous=RenderTexture.active;var pixels=new Texture2D(target.width,target.height,TextureFormat.RGB24,false);
  try{Canvas.ForceUpdateCanvases();Camera.main.Render();RenderTexture.active=target;pixels.ReadPixels(new Rect(0,0,target.width,target.height),0,0);pixels.Apply();File.WriteAllBytes(Output+"/"+name+".png",pixels.EncodeToPNG());}
  finally{RenderTexture.active=previous;Object.Destroy(pixels);}
 }
}
public sealed class GuxuLoadingCheckRunner:MonoBehaviour{}
