using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Gxdl;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public static class GuxuGrowthHubCheck {
 const string Output="Reference/GrowthHub20261109";
 static bool running;
 [MenuItem("GXDL/QA/Growth Hub Art")]
 public static void Run(){
  if(running||!Application.isPlaying||!GxdlGame.Instance)throw new InvalidOperationException("Enter Play mode first.");
  running=true;Directory.CreateDirectory(Output);File.WriteAllText(Output+"/ui-check.txt","RUNNING");GxdlGame.Instance.StartCoroutine(Guard(Check(GxdlGame.Instance)));
 }
 static IEnumerator Guard(IEnumerator check){
  var stack=new Stack<IEnumerator>();stack.Push(check);
  try{while(stack.Count>0){bool more;object current=null;try{more=stack.Peek().MoveNext();if(more)current=stack.Peek().Current;}catch(Exception e){File.WriteAllText(Output+"/ui-check.txt","FAIL: "+e);Debug.LogException(e);yield break;}if(!more){(stack.Pop() as IDisposable)?.Dispose();continue;}if(current is IEnumerator inner)stack.Push(inner);else yield return current;}}
  finally{while(stack.Count>0)(stack.Pop() as IDisposable)?.Dispose();running=false;}
 }
 static void Require(bool ok,string message){if(!ok)throw new Exception("Growth hub: "+message);}
 static string Label(string name)=>UnityEngine.Object.FindObjectsOfType<Text>().Single(t=>t.name==name&&t.gameObject.activeInHierarchy).text;
 static void Click(string name){
  var b=UnityEngine.Object.FindObjectsOfType<Button>().Single(x=>x.name==name&&x.gameObject.activeInHierarchy);Require(b.interactable,"enabled "+name);Canvas.ForceUpdateCanvases();
  var r=(RectTransform)b.transform;var camera=b.GetComponentInParent<Canvas>().worldCamera;
  var pointer=new PointerEventData(EventSystem.current){position=RectTransformUtility.WorldToScreenPoint(camera,r.TransformPoint(r.rect.center)),button=PointerEventData.InputButton.Left};
  var hits=new List<RaycastResult>();EventSystem.current.RaycastAll(pointer,hits);Require(hits.Count>0&&hits[0].gameObject==b.gameObject,"unobstructed click "+name);
  ExecuteEvents.Execute(b.gameObject,pointer,ExecuteEvents.pointerClickHandler);
 }
 static IEnumerator Capture(string name){yield return new WaitForEndOfFrame();var image=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes(Output+"/"+name+".png",image.EncodeToPNG());UnityEngine.Object.Destroy(image);}
 static void SetSize(int w,int h){typeof(GuxuBattlePreparationCheck).GetMethod("SetSize",BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,new object[]{w,h});}
 static void CheckLabels(GameRules rules){
  Require(Label("Growth Balance Forge")=="玄铁 "+rules.Save.forgeOre,"forge ore");
  Require(Label("Growth Detail Realm")=="修为 "+rules.Save.exp+" / "+rules.ExpNeeded+" · 突破石 "+rules.Save.breakthroughStones,"cultivation and stones");
  Require(Label("Growth Balance Realm")==rules.Realm.displayName,"realm name");
  Require(Label("Growth Balance Talents")=="可用悟道点 "+rules.Save.insight,"insight");
  Require(Label("Growth Balance Gems")=="库存 "+rules.Save.gemInventory.Sum()+" 枚","gem inventory");
  Require(Label("Growth Tokens")=="破封符 "+rules.Save.peaches&&Label("Growth Dust")=="灵尘 "+rules.Save.coins&&Label("Growth Power")=="战力 "+rules.Attributes.Power,"live wallet and power");
 }
 static IEnumerator Check(GxdlGame g){
  var original=g.Rules;var property=typeof(GxdlGame).GetProperty("Rules");string path=GameRules.SavePath;byte[] disk=File.Exists(path)?File.ReadAllBytes(path):null;
  int opening=PlayerPrefs.GetInt(GxdlGame.OpeningStoryKey,-1),first=PlayerPrefs.GetInt(GxdlGame.FirstUnsealStoryKey,-1);
  var window=EditorWindow.GetWindow(typeof(Editor).Assembly.GetType("UnityEditor.GameView"));var sizeProperty=window.GetType().GetProperty("selectedSizeIndex",BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic);int originalSize=(int)sizeProperty.GetValue(window);
  try{
   PlayerPrefs.SetInt(GxdlGame.OpeningStoryKey,1);PlayerPrefs.SetInt(GxdlGame.FirstUnsealStoryKey,1);g.SkipStory();
   float deadline=Time.realtimeSinceStartup+3;while(g.IsStoryPlaying&&Time.realtimeSinceStartup<deadline)yield return null;Require(!g.IsStoryPlaying,"opening finished");
   var rules=new GameRules(original.Config,new SaveData{stage=201,level=60,equipmentSchema=1,coins=50000,peaches=40,forgeOre=3000,insight=1000,breakthroughStones=100});rules.EnsureExpansion();
   typeof(GameRules).GetField("storagePath",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(rules,Path.GetFullPath(Output+"/fixture-save.json"));property.SetValue(g,rules);
   SetSize(750,1334);yield return null;yield return null;g.Home();yield return new WaitForEndOfFrame();Click("修炼");yield return new WaitForEndOfFrame();
   Require(g.ExpansionScreen=="growth"&&GameObject.Find("Growth Hub Skin"),"home opens new hub");CheckLabels(rules);yield return Capture("01-reference-growth-hub");
   string[] buttons={"Growth Forge","Growth Realm","Growth Talents","Growth Gems","Growth Dungeons","Growth Loadout","Growth Back"};
   string[] screens={"forge","realm","talents","gems","dungeons","loadout","home"};
   for(int i=0;i<buttons.Length;i++){g.ShowGrowthHub();yield return new WaitForEndOfFrame();Click(buttons[i]);Require(g.ExpansionScreen==screens[i],"navigation "+buttons[i]);}
   g.ShowGrowthHub();yield return new WaitForEndOfFrame();Click("Growth Forge");yield return new WaitForEndOfFrame();
   int ore=rules.Save.forgeOre,level=rules.Save.forgeLevels[0];Click("Forge Upgrade");Require(rules.Save.forgeLevels[0]==level+1&&rules.Save.forgeOre<ore,"real forge action");
   yield return new WaitForEndOfFrame();Click("返回修行");yield return new WaitForEndOfFrame();CheckLabels(rules);
   rules.Save.exp=123;rules.Save.insight=4321;rules.Save.breakthroughStones=87;rules.Save.gemInventory[0]=7;rules.Save.gemInventory[6]=8;rules.Save.coins=123456;rules.Save.peaches=76;
   g.ShowGrowthHub();yield return new WaitForEndOfFrame();CheckLabels(rules);yield return Capture("02-updated-resources");
   g.ShowBattlePrep(GuxuStageConfig.Load().stages[0]);yield return new WaitForEndOfFrame();Click("Prep Growth");Require(g.ExpansionScreen=="growth"&&!GameObject.Find("Battle Preparation"),"battle preparation opens growth without stale modal");
   var beginner=new GameRules(original.Config,new SaveData{equipmentSchema=1,stage=1,level=1,peaches=0,coins=0});beginner.EnsureExpansion();rules.Save=beginner.Save;
   g.ShowGrowthHub();yield return new WaitForEndOfFrame();CheckLabels(rules);yield return Capture("03-new-player");
   Click("Growth Forge");Require(!UnityEngine.Object.FindObjectsOfType<Button>().Single(b=>b.name=="Forge Upgrade"&&b.gameObject.activeInHierarchy).interactable,"new player forge unlock preserved");
   SetSize(750,1624);yield return null;yield return null;g.ShowGrowthHub();yield return new WaitForEndOfFrame();CheckLabels(rules);
   var rect=(RectTransform)GameObject.Find("Growth Hub").transform;var corners=new Vector3[4];rect.GetWorldCorners(corners);var camera=rect.GetComponentInParent<Canvas>().worldCamera;
   foreach(var corner in corners){var point=RectTransformUtility.WorldToScreenPoint(camera,corner);Require(point.x>=-1&&point.y>=-1&&point.x<=Screen.width+1&&point.y<=Screen.height+1,"tall phone fits");}
   yield return Capture("04-tall-phone");Click("Growth Loadout");Require(g.ExpansionScreen=="loadout","tall phone pointer target");
   Require(disk==null?!File.Exists(path):disk.SequenceEqual(File.ReadAllBytes(path)),"player save unchanged");
   File.WriteAllText(Output+"/ui-check.txt","PASS: reference layout; live ore, EXP, stones, realm, insight, gems, tokens, dust and power; all seven navigation buttons via real raycasts; actual forge updates returned balances; changed resources and new-player states; progression guard; battle-prep integration; 750x1334 and 750x1624 layouts. Fixture writes isolated to workspace; player save unchanged.");
  }finally{
   property.SetValue(g,original);
   if(opening<0)PlayerPrefs.DeleteKey(GxdlGame.OpeningStoryKey);else PlayerPrefs.SetInt(GxdlGame.OpeningStoryKey,opening);
   if(first<0)PlayerPrefs.DeleteKey(GxdlGame.FirstUnsealStoryKey);else PlayerPrefs.SetInt(GxdlGame.FirstUnsealStoryKey,first);
   sizeProperty.SetValue(window,originalSize);g.Home();
  }
 }
}
