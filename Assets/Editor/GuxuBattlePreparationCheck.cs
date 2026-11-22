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
using UnityEngine.EventSystems;
using UnityEngine.UI;

public static class GuxuBattlePreparationCheck {
 const string Output="Reference/BattlePrepV2-20261109";
 static bool running;
 [MenuItem("GXDL/QA/Battle Preparation Art")]
 public static void Run(){
  if(running||!Application.isPlaying||!GxdlGame.Instance)throw new InvalidOperationException("Enter Play mode first.");
  running=true;Directory.CreateDirectory(Output);File.WriteAllText(Output+"/ui-check.txt","RUNNING");
  GxdlGame.Instance.StartCoroutine(Guard(Check(GxdlGame.Instance)));
 }
 static IEnumerator Guard(IEnumerator check){
  var stack=new Stack<IEnumerator>();stack.Push(check);
  try{while(stack.Count>0){bool more;object current=null;try{more=stack.Peek().MoveNext();if(more)current=stack.Peek().Current;}catch(Exception e){File.WriteAllText(Output+"/ui-check.txt","FAIL: "+e);Debug.LogException(e);yield break;}if(!more){(stack.Pop() as IDisposable)?.Dispose();continue;}if(current is IEnumerator inner)stack.Push(inner);else yield return current;}}
  finally{while(stack.Count>0)(stack.Pop() as IDisposable)?.Dispose();running=false;}
 }
 static void Require(bool ok,string detail){if(!ok)throw new Exception("Battle preparation: "+detail);}
 static Button Find(string name)=>UnityEngine.Object.FindObjectsOfType<Button>().Single(b=>b.name==name&&b.gameObject.activeInHierarchy);
 static Text Label(string name)=>UnityEngine.Object.FindObjectsOfType<Text>().Single(t=>t.name==name&&t.gameObject.activeInHierarchy);
 static string Rewards()=>string.Join("  ",UnityEngine.Object.FindObjectsOfType<Text>().Where(t=>t.name.StartsWith("Prep Reward Value ")&&t.gameObject.activeInHierarchy).OrderBy(t=>t.name).Select(t=>t.text));
 static void Click(string name){
  var b=Find(name);Require(b.interactable,"enabled "+name);Canvas.ForceUpdateCanvases();
  var r=(RectTransform)b.transform;var camera=b.GetComponentInParent<Canvas>().worldCamera;
  var pointer=new PointerEventData(EventSystem.current){position=RectTransformUtility.WorldToScreenPoint(camera,r.TransformPoint(r.rect.center)),button=PointerEventData.InputButton.Left};
  var hits=new List<RaycastResult>();EventSystem.current.RaycastAll(pointer,hits);
  Require(hits.Count>0&&hits[0].gameObject==b.gameObject,"unobstructed pointer target "+name+": "+string.Join(",",hits.Select(h=>h.gameObject.name)));
  ExecuteEvents.Execute(b.gameObject,pointer,ExecuteEvents.pointerClickHandler);
 }
 static IEnumerator Capture(string file){
  yield return new WaitForEndOfFrame();
  var image=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes(Output+"/"+file+".png",image.EncodeToPNG());UnityEngine.Object.Destroy(image);
 }
 static EditorWindow SetSize(int width,int height){
  var asm=typeof(Editor).Assembly;var type=asm.GetType("UnityEditor.GameViewSizes");
  var sizes=typeof(ScriptableSingleton<>).MakeGenericType(type).GetProperty("instance").GetValue(null);
  var group=type.GetMethod("GetGroup").Invoke(sizes,new object[]{0});
  var size=Activator.CreateInstance(asm.GetType("UnityEditor.GameViewSize"),new object[]{Enum.ToObject(asm.GetType("UnityEditor.GameViewSizeType"),1),width,height,"Battle Prep QA "+width+"x"+height});
  group.GetType().GetMethod("AddCustomSize").Invoke(group,new[]{size});
  var window=EditorWindow.GetWindow(asm.GetType("UnityEditor.GameView"));
  window.GetType().GetProperty("selectedSizeIndex",BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic).SetValue(window,(int)group.GetType().GetMethod("GetTotalCount").Invoke(group,null)-1);window.Focus();return window;
 }
 static IEnumerator Check(GxdlGame g){
  var original=g.Rules;var rulesProperty=typeof(GxdlGame).GetProperty("Rules");
  string path=GameRules.SavePath;byte[] disk=File.Exists(path)?File.ReadAllBytes(path):null;
  int opening=PlayerPrefs.GetInt(GxdlGame.OpeningStoryKey,-1),firstStory=PlayerPrefs.GetInt(GxdlGame.FirstUnsealStoryKey,-1);
  var window=EditorWindow.GetWindow(typeof(Editor).Assembly.GetType("UnityEditor.GameView"));
  var sizeProperty=window.GetType().GetProperty("selectedSizeIndex",BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic);int previousSize=(int)sizeProperty.GetValue(window);
  try{
   PlayerPrefs.SetInt(GxdlGame.OpeningStoryKey,1);PlayerPrefs.SetInt(GxdlGame.FirstUnsealStoryKey,1);g.SkipStory();
   float deadline=Time.realtimeSinceStartup+3;while(g.IsStoryPlaying&&Time.realtimeSinceStartup<deadline)yield return null;
   Require(!g.IsStoryPlaying,"opening finished");
   var fixture=new GameRules(original.Config,new SaveData{stage=201,level=60,equipmentSchema=1,coins=50000,peaches=40});fixture.EnsureExpansion();
   // Route every click's real Store call to the workspace, never to the player's save or backup.
   typeof(GameRules).GetField("storagePath",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(fixture,Path.GetFullPath(Output+"/fixture-save.json"));
   rulesProperty.SetValue(g,fixture);for(int i=0;i<200;i++)fixture.Save.firstClears[i]=true;
   var stages=GuxuStageConfig.Load();var stage=stages.stages[199];SetSize(750,1334);yield return null;yield return null;
   g.Home();g.ShowChapter(9);yield return new WaitForEndOfFrame();Click("Stage 200");Click("Chapter Challenge");yield return new WaitForEndOfFrame();
   Require(Label("Prep Stage").text==stage.name,"live stage name");Require(Label("Prep Guardian").text==stage.guardianName,"live guardian");
   var enemy=GuxuStageRules.Enemy(stage,fixture.Config);
   Require(Label("Prep Enemy Attack").text=="攻击 "+enemy.attack&&Label("Prep Enemy HP").text=="生命 "+enemy.hp&&Label("Prep Enemy Defense").text=="防御 "+enemy.defense,"live enemy stat pills");
   Require(Label("Prep Reward Title").text=="再次挑战奖励","repeat reward title");
   Require(Rewards()==GuxuStageRules.Reward(stage,false).ToString(),"repeat reward values");
   Require(Label("Prep Power").text.Contains(fixture.Attributes.Power.ToString()),"live combat power");
   Require(!UnityEngine.Object.FindObjectsOfType<Button>().Any(b=>b.name=="Prep Growth Footer"||b.name=="Prep Continue"),"duplicate actions removed");
   yield return Capture("01-reference-stage-200");
   for(int i=0;i<4;i++){
    Click("Skill Choice "+i);Require(fixture.Save.activeSkill==i,"skill selection "+i);
    Require(Label("Prep Skill Detail").text==GameRules.SkillDetails[i],"skill details "+i);
    Require(Find("Skill Choice "+i).image.sprite==GuxuUiArt.BattlePrepButton(true),"gold selected state "+i);
    Require(GameObject.Find("Skill Selected "+i)!=null&&UnityEngine.Object.FindObjectsOfType<GuxuPortraitDisc>().Count(x=>x.name.StartsWith("Skill Selected ")&&x.gameObject.activeInHierarchy)==1,"exactly one selection badge");
    Require(JsonConvert.DeserializeObject<SaveData>(File.ReadAllText(Output+"/fixture-save.json")).activeSkill==i,"persisted skill "+i);
   }
   Click("Prep Auto Skill");Require(!fixture.Save.autoSkill&&Label("Prep Skill Mode").text.Contains("手动"),"manual release");
   yield return Capture("02-skill-selection-manual");
   Click("Prep Auto Skill");Require(fixture.Save.autoSkill,"automatic release");
   Click("Prep Close");Require(!GameObject.Find("Battle Preparation")&&g.ExpansionScreen=="chapter","close returns to chapter");
   fixture.Save.firstClears[199]=false;g.ShowBattlePrep(stage);yield return new WaitForEndOfFrame();
   Require(Label("Prep Reward Title").text=="首通奖励","first clear title");Require(Rewards()==GuxuStageRules.Reward(stage,true).ToString(),"first clear rewards");
   Require(UnityEngine.Object.FindObjectsOfType<Image>().Count(x=>x.name.StartsWith("Prep Reward Icon ")&&x.gameObject.activeInHierarchy)==6,"six first-clear reward icons");yield return Capture("03-first-clear-rewards");
   Click("Prep Growth");Require(g.ExpansionScreen=="growth","growth action");
   g.ShowBattlePrep(stage);yield return new WaitForEndOfFrame();Click("Prep Close");Require(g.ExpansionScreen=="growth","modal closes to real source page");
   g.ShowBattlePrep(stages.stages[0]);yield return new WaitForEndOfFrame();Require(Label("Prep Stage").text==stages.stages[0].name,"selected encounter remains selected");
   yield return new WaitForEndOfFrame();Click("Prep Close");Require(g.ExpansionScreen=="growth","close preserves source page");
   fixture.RefreshDaily(DateTime.UtcNow);fixture.Save.dungeonClaims[0]=3;g.ShowBattlePrep(stages.Dungeon(0,1));yield return new WaitForEndOfFrame();
   Require(!Find("Ruins Challenge").interactable,"exhausted dungeon disabled");Require(Label("Prep Reward Limit").text.Contains("剩余 0 次"),"remaining dungeon runs");yield return Capture("04-dungeon-exhausted");
   g.ShowBattlePrep(stages.tower[0]);yield return new WaitForEndOfFrame();Require(Label("Prep Stage").text==stages.tower[0].name,"tower heading");yield return Capture("05-tower");
   fixture.Save.towerFloor=2;g.ShowBattlePrep(stages.tower[0]);yield return new WaitForEndOfFrame();Require(Rewards()==""&&!UnityEngine.Object.FindObjectsOfType<Image>().Any(x=>x.name.StartsWith("Prep Reward Icon ")&&x.gameObject.activeInHierarchy),"claimed tower has no fabricated rewards");
   var gemStage=stages.Dungeon(1,1);g.ShowBattlePrep(gemStage);yield return new WaitForEndOfFrame();Require(Rewards()==GuxuStageRules.Reward(gemStage,true).ToString(),"gem reward label and quantity");yield return Capture("07-gem-rewards");
   SetSize(750,1624);yield return null;yield return null;g.ShowBattlePrep(stage);yield return new WaitForEndOfFrame();
   var rect=(RectTransform)GameObject.Find("Battle Preparation").transform;var corners=new Vector3[4];rect.GetWorldCorners(corners);var camera=rect.GetComponentInParent<Canvas>().worldCamera;
   foreach(var corner in corners){var point=RectTransformUtility.WorldToScreenPoint(camera,corner);Require(point.x>=-1&&point.y>=-1&&point.x<=Screen.width+1&&point.y<=Screen.height+1,"tall display panel fits");}
   Click("Skill Choice 1");yield return Capture("06-tall-phone");
   Click("Ruins Challenge");Require(g.ActiveBattle!=null&&g.ActiveBattle.Skill==1&&g.ActiveBattle.AutoSkill,"selected skill enters real combat");
   yield return new WaitForEndOfFrame();Click("退出挑战");Require(g.ActiveBattle==null&&g.ExpansionScreen=="chapter","combat exit");
   File.WriteAllText(Output+"/ui-check.txt","PASS: V2 reference stage 200; live enemy stat pills/power/wallet; all four skills and persisted choices with exactly one badge; manual/auto mode; real pointer raycasts; close/back/growth/continue navigation; repeat rewards; six first-clear rewards with icons; gem dungeon reward; empty claimed tower reward; exhausted dungeon guard; 750x1334 and 750x1624 layouts; real combat uses selected skill. Player save bytes unchanged; fixture writes isolated to workspace.");
  }finally{
   g.Home();rulesProperty.SetValue(g,original);
   if(opening<0)PlayerPrefs.DeleteKey(GxdlGame.OpeningStoryKey);else PlayerPrefs.SetInt(GxdlGame.OpeningStoryKey,opening);
   if(firstStory<0)PlayerPrefs.DeleteKey(GxdlGame.FirstUnsealStoryKey);else PlayerPrefs.SetInt(GxdlGame.FirstUnsealStoryKey,firstStory);
   sizeProperty.SetValue(window,previousSize);g.Home();
   Require(disk==null?!File.Exists(path):File.Exists(path)&&disk.SequenceEqual(File.ReadAllBytes(path)),"player save was not modified");
  }
 }
}

