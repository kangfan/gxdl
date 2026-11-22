using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Gxdl;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Object=UnityEngine.Object;

[InitializeOnLoad]
public static class GuxuOpeningCheck {
 const string Prefix="GXDL.OpeningQA.",Output="Logs/opening",Receipt=Output+"/check.txt";
 static readonly string[] Keys={GxdlGame.OpeningStoryKey,GxdlGame.FirstUnsealStoryKey};
 static readonly string[] Suffixes={"",".bak",".tmp",".invalid",".before-equipment15"};
 static bool started;
 static GuxuOpeningCheck(){EditorApplication.update+=Monitor;EditorApplication.playModeStateChanged+=OnMode;}

 [MenuItem("Guxu/Preview/Opening Animation")]
 public static void Preview(){
  if(EditorApplication.isPlaying){GxdlGame.Instance.ReplayOpening();return;}
  Require(UnityEngine.SceneManagement.SceneManager.GetActiveScene().path=="Assets/Scenes/GxdlMain.unity","open GxdlMain first");
  SessionState.SetBool(Prefix+"preview",true);GuxuAccountChecks.PreviewGameplay();
 }
 [MenuItem("Guxu/QA/Verify Opening Animation")]
 public static void Run(){
  Require(!EditorApplication.isPlayingOrWillChangePlaymode,"stop Play Mode first");
  Require(UnityEngine.SceneManagement.SceneManager.GetActiveScene().path=="Assets/Scenes/GxdlMain.unity","open GxdlMain first");
  GuxuStoryCheck.ValidateContract();Directory.CreateDirectory(Output);File.WriteAllText(Receipt,"RUNNING "+DateTime.Now.ToString("O"));
  SessionState.SetString(Prefix+"path",GameRules.SavePath);
  foreach(var suffix in Suffixes){var path=GameRules.SavePath+suffix;SessionState.SetBool(Prefix+"exists"+suffix,File.Exists(path));if(File.Exists(path))SessionState.SetString(Prefix+"bytes"+suffix,Convert.ToBase64String(File.ReadAllBytes(path)));}
  foreach(var key in Keys){SessionState.SetBool(Prefix+key+"exists",PlayerPrefs.HasKey(key));SessionState.SetInt(Prefix+key,PlayerPrefs.GetInt(key));PlayerPrefs.SetInt(key,1);}
  PlayerPrefs.Save();SessionState.SetBool(Prefix+"active",true);SessionState.SetFloat(Prefix+"deadline",(float)EditorApplication.timeSinceStartup+100);started=false;GuxuAccountChecks.PreviewGameplay();
 }
 static void Monitor(){
  if(SessionState.GetBool(Prefix+"preview",false)&&EditorApplication.isPlaying&&GxdlGame.Instance){SessionState.SetBool(Prefix+"preview",false);GxdlGame.Instance.ReplayOpening();}
  if(!SessionState.GetBool(Prefix+"active",false))return;
  if(!started&&EditorApplication.isPlaying&&GxdlGame.Instance){started=true;new GameObject("Opening QA").AddComponent<GuxuOpeningCheckRunner>().StartCoroutine(Guard(Check()));}
  if(EditorApplication.timeSinceStartup>SessionState.GetFloat(Prefix+"deadline",0)){File.AppendAllText(Receipt,"\nFAIL timeout");EditorApplication.isPlaying=false;}
 }
 static void OnMode(PlayModeStateChange state){
  if(state!=PlayModeStateChange.EnteredEditMode||!SessionState.GetBool(Prefix+"active",false))return;
  string save=SessionState.GetString(Prefix+"path",GameRules.SavePath);
  foreach(var suffix in Suffixes){string path=save+suffix;if(SessionState.GetBool(Prefix+"exists"+suffix,false))File.WriteAllBytes(path,Convert.FromBase64String(SessionState.GetString(Prefix+"bytes"+suffix,"")));else if(File.Exists(path))File.Delete(path);}
  foreach(var key in Keys){if(SessionState.GetBool(Prefix+key+"exists",false))PlayerPrefs.SetInt(key,SessionState.GetInt(Prefix+key,0));else PlayerPrefs.DeleteKey(key);}PlayerPrefs.Save();
  if(File.ReadAllText(Receipt).StartsWith("RUNNING"))File.AppendAllText(Receipt,"\nCANCELLED before completion");
  File.AppendAllText(Receipt,"\nRESTORED exact save bytes, sidecars and story preferences.");SessionState.SetBool(Prefix+"active",false);started=false;
 }
 static IEnumerator Guard(IEnumerator routine){
  try{while(true){object next;try{if(!routine.MoveNext())break;next=routine.Current;}catch(Exception e){File.WriteAllText(Receipt,"FAIL "+e);Debug.LogException(e);break;}yield return next;}}
  finally{(routine as IDisposable)?.Dispose();EditorApplication.isPlaying=false;}
 }
 static void Require(bool valid,string message){if(!valid)throw new InvalidOperationException("Opening QA: "+message);}
 static Button Skip()=>Object.FindObjectsOfType<Button>().Single(b=>b.name=="Skip Story");
 static void Click(Button button){
  Canvas.ForceUpdateCanvases();var r=(RectTransform)button.transform;
  var e=new PointerEventData(EventSystem.current){position=RectTransformUtility.WorldToScreenPoint(Camera.main,r.TransformPoint(r.rect.center))};
  var hits=new List<RaycastResult>();EventSystem.current.RaycastAll(e,hits);
  Require(hits.Count>0&&hits[0].gameObject.GetComponentInParent<Button>()==button,"skip receives actual raycast");ExecuteEvents.Execute(button.gameObject,e,ExecuteEvents.pointerClickHandler);
 }
 static IEnumerator Check(){
  var game=GxdlGame.Instance;string original=JsonConvert.SerializeObject(game.Rules.Save);float scale=Time.timeScale;
  var camera=Camera.main;var previous=camera.targetTexture;var target=new RenderTexture(750,1334,24);target.Create();camera.targetTexture=target;
  try{
   game.Rules.Save.pending=null;game.Home();yield return null;
   string unchanged=JsonConvert.SerializeObject(game.Rules.Save);PlayerPrefs.DeleteKey(GxdlGame.OpeningStoryKey);float began=Time.realtimeSinceStartup;game.PlayOpeningIfNeeded();
   Require(game.IsStoryPlaying,"automatic first play");game.PlayOpeningIfNeeded();game.ReplayOpening();
   var skipArt=Skip().GetComponent<Image>();
   Require(skipArt.sprite&&skipArt.sprite.texture==Resources.Load<Texture2D>("Game/Guxu/Expansion/UI/button-story-skip"),"dedicated skip button art is loaded");
   Require(skipArt.rectTransform.sizeDelta==new Vector2(162,52)&&skipArt.color==Color.white,"skip keeps compact dimensions and original art colors");
   Require(Object.FindObjectsOfType<GuxuStoryLifetime>().Length==1,"reentrant requests create one overlay");
   game.Chop();Require(JsonConvert.SerializeObject(game.Rules.Save)==unchanged,"game actions blocked during cinematic");
   Time.timeScale=0;var pose=Object.FindObjectsOfType<RawImage>().Single(i=>i.name=="Incoming Story Art").rectTransform.anchoredPosition;
   yield return new WaitForSecondsRealtime(.8f);
   Require(Object.FindObjectsOfType<RawImage>().Single(i=>i.name=="Incoming Story Art").rectTransform.anchoredPosition!=pose,"unscaled animation advances while timeScale is zero");Time.timeScale=scale;
   var seen=new HashSet<string>();float end=Time.realtimeSinceStartup+32;
   while(game.IsStoryPlaying&&Time.realtimeSinceStartup<end){
    var chapter=Object.FindObjectsOfType<Text>().Single(t=>t.name=="Story Chapter");
    if(seen.Add(chapter.text)){
     if(seen.Count>1)Require(Object.FindObjectsOfType<RawImage>().Single(i=>i.name=="Outgoing Story Art").color.a==1,"outgoing frame stays opaque during crossfade");
     yield return new WaitForSecondsRealtime(1.25f);Canvas.ForceUpdateCanvases();
     var text=Object.FindObjectsOfType<Text>().Single(t=>t.name=="Story Subtitle");
     Require(text.preferredHeight<=text.rectTransform.rect.height,"subtitle fits: "+chapter.text);
     Capture(target,seen.Count.ToString("00"));
    }
    yield return null;
   }
   Require(!game.IsStoryPlaying&&seen.Count==7,"all seven shots finish naturally");
   Require(Mathf.Abs(Time.realtimeSinceStartup-began-30)<.7f,"30-second runtime duration");
   Require(PlayerPrefs.GetInt(GxdlGame.OpeningStoryKey)==1,"completion persists");game.PlayOpeningIfNeeded();Require(!game.IsStoryPlaying,"seen opening does not auto-repeat");
   game.ReplayOpening();yield return new WaitForSecondsRealtime(.2f);Click(Skip());yield return null;yield return null;
   Require(!game.IsStoryPlaying&&!Object.FindObjectsOfType<GuxuStoryLifetime>().Any(),"skip destroys overlay and effects");
   PlayerPrefs.DeleteKey(GxdlGame.OpeningStoryKey);game.ReplayOpening();yield return null;game.StopAllCoroutines();
   for(int i=0;i<7;i++)yield return null;
   Require(!game.IsStoryPlaying&&PlayerPrefs.GetInt(GxdlGame.OpeningStoryKey)==0,"stopped coroutine cleans up without marking seen");
   game.ReplayOpening();Require(game.IsStoryPlaying,"input unlocked after interruption");game.Home();yield return null;yield return null;
   Require(!game.IsStoryPlaying,"screen change cancels opening");
   game.ReplayOpening();yield return null;game.enabled=false;yield return null;yield return null;game.enabled=true;
   Require(!game.IsStoryPlaying,"disabled owner cancels opening");
   Require(JsonConvert.SerializeObject(game.Rules.Save)==unchanged,"opening, replay, skip and cancellation preserve gameplay state");
   File.WriteAllText(Receipt,"PASS "+DateTime.Now.ToString("O")+"\n30-second complete seven-shot opening; first-play and seen flag; frame-safe crossfades; portrait coverage; readable subtitles; unscaled playback; duplicate prevention; input blocking; real skip raycast; replay; coroutine stop; screen switch; owner disable; unchanged gameplay state. Seven screenshots in Logs/opening.");
  }finally{game.enabled=true;game.StopAllCoroutines();game.Rules.Save=JsonConvert.DeserializeObject<SaveData>(original);Time.timeScale=scale;camera.targetTexture=previous;target.Release();Object.Destroy(target);}
 }
 static void Capture(RenderTexture target,string name){
  var previous=RenderTexture.active;var pixels=new Texture2D(target.width,target.height,TextureFormat.RGB24,false);
  try{Camera.main.Render();RenderTexture.active=target;pixels.ReadPixels(new Rect(0,0,target.width,target.height),0,0);pixels.Apply();File.WriteAllBytes(Output+"/"+name+".png",pixels.EncodeToPNG());}
  finally{RenderTexture.active=previous;Object.Destroy(pixels);}
 }
}
public sealed class GuxuOpeningCheckRunner:MonoBehaviour{}
