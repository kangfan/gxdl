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

public static class GuxuChapterMapCheck {
 const string Output="Reference/UIStyleRework20261111/technical";
 static readonly List<string> evidence=new List<string>();
 static bool running;
 static int assertions;
 [MenuItem("GXDL/QA/UIStyle Chapter Map")]
 public static void Begin(){
  if(running||!Application.isPlaying||!GxdlGame.Instance.CanPlay)throw new Exception("Use offline preview with no other review running");
  Directory.CreateDirectory(Output);evidence.Clear();assertions=0;GuxuUI23Review.BeginStyle();running=true;GxdlGame.Instance.StartCoroutine(Guard(Check()));
 }
 static void Require(bool passed,string message){assertions++;if(!passed)throw new Exception(message);evidence.Add("PASS "+message);}
 static IEnumerator Guard(IEnumerator routine){
  var stack=new Stack<IEnumerator>();stack.Push(routine);bool pass=false;
  try{
   while(stack.Count>0){bool more;object current=null;try{more=stack.Peek().MoveNext();if(more)current=stack.Peek().Current;}catch(Exception error){evidence.Add("FAIL "+error);Debug.LogException(error);yield break;}if(!more){(stack.Pop() as IDisposable)?.Dispose();continue;}if(current is IEnumerator nested)stack.Push(nested);else yield return current;}
   pass=true;
  }finally{
   while(stack.Count>0)(stack.Pop() as IDisposable)?.Dispose();GuxuUI23Review.End();running=false;
   string report=DateTime.UtcNow.ToString("O")+" "+(pass?"PASS":"FAIL")+" "+assertions+" assertions\nSession: "+GuxuUI23Review.ActiveOutput+"\n"+string.Join("\n",evidence)+"\nRESTORED via isolated review fixture";
   File.WriteAllText(Output+"/chapter-check.txt",report);File.WriteAllText(GuxuUI23Review.ActiveOutput+"/chapter-check.txt",report);
  }
 }
 static IEnumerator Action(string action,string name="",string scenario="",float delta=0,int height=1334)=>GuxuUI23Review.Run(new GuxuUI23Review.Command{action=action,name=name,scenario=scenario,delta=delta,height=height});
 static Button Button(string name)=>UnityEngine.Object.FindObjectsOfType<Button>().Single(b=>b.gameObject.activeInHierarchy&&b.name==name);
 static Text Text(string name)=>UnityEngine.Object.FindObjectsOfType<Text>().Single(t=>t.gameObject.activeInHierarchy&&t.name==name);
 static ScrollRect Scroll()=>UnityEngine.Object.FindObjectOfType<ScrollRect>();
 static void Rewards(GuxuStage stage,GameRules rules){
  string actual=string.Join("  ",UnityEngine.Object.FindObjectsOfType<Text>().Where(t=>t.gameObject.activeInHierarchy&&t.name.StartsWith("Chapter Reward Value ")).OrderBy(t=>t.name).Select(t=>t.text));
  Require(actual==GuxuStageRules.Reward(stage,!rules.Save.firstClears[stage.id-1]).ToString(),"reward fields match rule for stage "+stage.id);
 }
 static bool Visible(RectTransform rect){
  var camera=rect.GetComponentInParent<Canvas>().worldCamera;var center=RectTransformUtility.WorldToScreenPoint(camera,rect.TransformPoint(rect.rect.center));
  return center.x>=0&&center.y>=0&&center.x<=Screen.width&&center.y<=Screen.height&&rect.GetComponentsInParent<RectMask2D>().All(mask=>RectTransformUtility.RectangleContainsScreenPoint(mask.rectTransform,center,camera));
 }
 static IEnumerator Check(){
  var g=GxdlGame.Instance;var r=g.Rules;var stages=GuxuStageConfig.Load();
  yield return Action("size",height:1334);yield return Action("scenario",scenario:"chapter5");
  Require(Screen.width==750&&Screen.height==1334,"standard viewport 750x1334");
  Require(UnityEngine.Object.FindObjectsOfType<Button>().Count(b=>b.gameObject.activeInHierarchy&&b.name.StartsWith("Stage "))==20,"20 real stage nodes exist");
  Require(Text("Chapter Progress").text=="已通关 4 / 20"&&Text("Chapter Selected Stage").text=="第5关 · 精英","same first-screen progress and selected elite as approved concept");
  Require(!Button("领取章节宝匣").interactable&&Text("Chapter Chest State").text=="宝匣锁定","4/20 chest locked");
  Require(Text("Chapter Selected Enemy").text.StartsWith(stages.stages[4].guardianName),"guardian from stage config");Rewards(stages.stages[4],r);
  Require(Scroll().viewport.rect.height==710,"standard map viewport is 710");
  string save=JsonConvert.SerializeObject(r.Save);
  yield return Action("click","Stage 6");Require(!Button("Chapter Challenge").interactable&&Text("Chapter Reward State").text=="通关第5关后解锁","locked node selection explains real prerequisite");
  Require(Text("Chapter Current Marker").GetComponentInParent<Button>().name=="Stage 5"&&Button("Stage 6").transform.Find("Chapter Selection Glow").gameObject.activeSelf,"shared state art keeps current target separate from selected locked node");
  yield return Action("reject-click","Chapter Challenge");Require(save==JsonConvert.SerializeObject(r.Save),"locked action changes no player resource or progress");
  yield return Action("click","Stage 1");Require(Text("Chapter Reward State").text=="重访奖励 · 首通已领取"&&Button("Chapter Challenge").interactable,"completed stage is revisit");Rewards(stages.stages[0],r);
  yield return Action("click","Stage 5");yield return Action("reject-click","Stage 20");Require(Text("Chapter Selected Stage").text=="第5关 · 精英","offscreen click cannot change selection");
  yield return Action("click","Chapter Challenge");Require(Text("Prep Stage").text==stages.stages[4].name,"single chapter CTA opens matching battle prep");yield return Action("click","Prep Close");
  yield return Action("drag",delta:430);Require(Mathf.Abs(Scroll().content.anchoredPosition.y-430)<5,"430px pointer drag moves map by 430 logical pixels");Require(Text("Chapter Selected Stage").text=="第5关 · 精英","scroll preserves selected stage and fixed detail");
  yield return Action("scroll",delta:-20);Require(Visible((RectTransform)Button("Stage 20").transform),"last stage is reachable by real wheel scrolling");
  yield return Action("click","Stage 20");Require(Text("Chapter Selected Stage").text=="第20关 · Boss"&&!Button("Chapter Challenge").interactable,"last locked Boss details");
  g.ShowWorldMap();g.ShowChapter(0);yield return Action("inspect");Require(Visible((RectTransform)Button("Stage 20").transform),"reentry focuses the selected node");
  yield return Action("size",height:1624);Require(Screen.height==1624&&Mathf.Abs(Scroll().viewport.rect.height-1000)<1,"long screen adds 290 to map viewport");
  Require(Mathf.Abs(((RectTransform)Button("Chapter Challenge").transform).rect.height-80)<.1f,"long screen preserves CTA size");
  yield return Action("scenario",scenario:"chapter5");Require(Visible((RectTransform)Button("Stage 5").transform),"long screen concept state visible");
  typeof(GxdlGame).GetMethod("OpenModal",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(g,new object[]{"长屏遮罩验证",380f});yield return Action("inspect");yield return Action("reject-click","Chapter Back");yield return Action("reject-click","Chapter Challenge");Require(GameObject.Find("Full Canvas Modal Shade").GetComponent<RectTransform>().rect.height>=1624-1,"modal shade covers full tall canvas above and below centered content");yield return Action("click","Close Modal");
  yield return Action("size",height:1334);yield return Action("scenario",scenario:"chapter10");Require(Text("Chapter Selected Stage").text=="第10关 · Boss"&&Button("Chapter Challenge").interactable,"current boss10 selection and CTA");
  yield return Action("scenario",scenario:"chapter15");Require(Text("Chapter Selected Stage").text=="第15关 · 精英"&&Button("Chapter Challenge").interactable,"later elite15 selection and CTA");
  yield return Action("scenario",scenario:"chapter20");Require(Text("Chapter Selected Stage").text=="第20关 · Boss"&&Button("Chapter Challenge").interactable,"current boss20 selection and CTA");
  yield return Action("scenario",scenario:"chapterclear");Require(Button("领取章节宝匣").interactable,"completed chapter chest is claimable");
  var before=JsonConvert.DeserializeObject<SaveData>(JsonConvert.SerializeObject(r.Save));var reward=GameRules.ChapterReward(0);
  yield return Action("click","领取章节宝匣");Require(r.Save.chapterChests[0]&&!Button("已领取").interactable&&Text("Chapter Chest State").text=="已领取","chest state refreshes immediately");
  Require(r.Save.peaches==before.peaches+reward.seals&&r.Save.coins==before.coins+reward.dust&&r.Save.forgeOre==before.forgeOre+reward.ore&&r.Save.insight==before.insight+reward.insight&&r.Save.breakthroughStones==before.breakthroughStones+reward.stones,"chest grant equals shared rule preview");
  save=JsonConvert.SerializeObject(r.Save);yield return Action("reject-click","已领取");Require(!r.ClaimChapter(0)&&save==JsonConvert.SerializeObject(r.Save),"claimed chest cannot double grant");
  yield return Action("click","Chapter Back");Require(Text("World Tokens").text=="破封符 "+r.Save.peaches&&Text("World Dust").text=="灵尘 "+r.Save.coins,"chest balances immediately agree on world map");yield return Action("click","Chapter 1");Require(!Button("已领取").interactable,"claimed chest persists on reentry");
  yield return Action("scenario",scenario:"chapterold");Require(Text("Chapter Selected Stage").text=="第20关 · Boss"&&Text("Chapter Reward State").text.Contains("重访"),"old chapter opens completed final stage for revisit");Rewards(stages.stages[19],r);
  yield return Action("scenario",scenario:"chapterend");Require(Text("Chapter Selected Stage").text=="第200关 · Boss"&&Button("Chapter Challenge").interactable,"final main chapter supports real stage200 revisit");
  yield return Action("click","Chapter Challenge");Require(Text("Prep Stage").text==stages.stages[199].name,"stage200 CTA remains correct");yield return Action("click","Prep Close");
  Require(Directory.GetFiles("ResourceCache/UIStyleRework20261111-ChapterEnvironment-r01/raw","*.png").Length>0,"environment raw source retained");
 }
}
