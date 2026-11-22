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

public static class GuxuUIStyleResultHandbookCheck {
 const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
 static readonly List<string> evidence=new List<string>();static bool running;static int assertions;
 [MenuItem("GXDL/QA/UIStyle Handbook")]
 public static void Handbook()=>Begin(HandbookRun(),"handbook");
 [MenuItem("GXDL/QA/UIStyle Settlement")]
 public static void Settlement()=>Begin(SettlementRun(),"settlement");
 static void Begin(IEnumerator routine,string name){if(running||!Application.isPlaying||!GxdlGame.Instance.CanPlay)throw new Exception("Use offline preview with no active review");evidence.Clear();assertions=0;GuxuUI23Review.BeginStyle();running=true;GxdlGame.Instance.StartCoroutine(Guard(routine,name));}
 static void Require(bool passed,string message){assertions++;if(!passed)throw new Exception(message);evidence.Add("PASS "+message);}
 static IEnumerator Guard(IEnumerator routine,string name){
  var stack=new Stack<IEnumerator>();stack.Push(routine);bool passed=false;
  try{while(stack.Count>0){bool more;object value=null;try{more=stack.Peek().MoveNext();if(more)value=stack.Peek().Current;}catch(Exception error){evidence.Add("FAIL "+error);Debug.LogException(error);yield break;}if(!more){(stack.Pop() as IDisposable)?.Dispose();continue;}if(value is IEnumerator inner)stack.Push(inner);else yield return value;}passed=true;}
  finally{while(stack.Count>0)(stack.Pop() as IDisposable)?.Dispose();GuxuUI23Review.End();running=false;string report=DateTime.UtcNow.ToString("O")+" "+(passed?"PASS":"FAIL")+" "+assertions+" assertions\nSession: "+GuxuUI23Review.ActiveOutput+"\n"+string.Join("\n",evidence)+"\nRESTORED via isolated review fixture";Directory.CreateDirectory("Reference/UIStyleRework20261111/technical");File.WriteAllText("Reference/UIStyleRework20261111/technical/"+name+"-check.txt",report);File.WriteAllText(GuxuUI23Review.ActiveOutput+"/"+name+"-check.txt",report);}
 }
 static IEnumerator Action(string action,string name="",string scenario="",int height=1334)=>GuxuUI23Review.Run(new GuxuUI23Review.Command{action=action,name=name,scenario=scenario,height=height});
 static void Open(string method)=>typeof(GxdlGame).GetMethod(method,Private).Invoke(GxdlGame.Instance,null);
 static Button Button(string name)=>UnityEngine.Object.FindObjectsOfType<Button>().Single(b=>b.gameObject.activeInHierarchy&&b.name==name);
 static Text Text(string name)=>UnityEngine.Object.FindObjectsOfType<Text>().Single(t=>t.gameObject.activeInHierarchy&&t.name==name);
 static string Save()=>JsonConvert.SerializeObject(GxdlGame.Instance.Rules.Save);
 static bool Fits(RectTransform rect){var corners=new Vector3[4];rect.GetWorldCorners(corners);var camera=rect.GetComponentInParent<Canvas>().worldCamera;return corners.All(c=>{var p=RectTransformUtility.WorldToScreenPoint(camera,c);return p.x>=-1&&p.y>=-1&&p.x<=Screen.width+1&&p.y<=Screen.height+1;});}
 static void CheckModal(){var modal=GameObject.Find("Modal").transform;Require(UnityEngine.Object.FindObjectsOfType<Button>().Where(b=>b.gameObject.activeInHierarchy&&b.transform.IsChildOf(modal)).All(b=>Fits((RectTransform)b.transform)),"all modal actions fit current viewport");}
 static IEnumerator HandbookRun(){
  foreach(string key in new[]{"handbook-panel","handbook-stat-tag","handbook-residence-insert"})Require(GuxuUiArt.Optional(key)!=null,"production handbook asset imported: "+key);
  yield return Action("scenario",scenario:"growth");var g=GxdlGame.Instance;var r=g.Rules;
  foreach(int height in new[]{1334,1624}){
   yield return Action("size",height:height);g.Home();Open("ShowFinalResidence");yield return Action("inspect");CheckModal();
   Require(Text("Handbook 当前境界").text==r.Realm.displayName&&Text("Handbook 寻墟战力").text==r.Attributes.Power.ToString(),"residence reads real realm and derived power at "+height);
   yield return Action("click","整理行囊");Require(GameObject.Find("行囊 · 当前装备")!=null,"residence bag action opens actual equipment");g.CloseModal();Open("ShowFinalCompanion");yield return Action("inspect");CheckModal();
   Require(Text("Companion Availability").text=="伙伴数值养成尚未开放"&&Text("Companion Status").text=="同行中","companion has real availability and no fake progression");
   yield return Action("click","查看随身装备");Require(GameObject.Find("行囊 · 当前装备")!=null,"companion action routes to actual equipment");g.CloseModal();Open("AutoSettings");yield return Action("inspect");CheckModal();
   int before=r.Save.keepQuality;yield return Action("click","Quality Choice 5");Require(r.Save.keepQuality==5&&Text("Auto Quality Threshold").text.Contains("及以上"),"quality choice refreshes saved threshold immediately");g.CloseModal();Open("AutoSettings");Require(r.Save.keepQuality==5,"threshold persists on reentry");yield return Action("click","Quality Choice "+before);yield return Action("click","确认");
  }
  Open("AutoSettings");for(int q=1;q<=5;q++){yield return Action("click","Quality Choice "+q);Require(r.Save.keepQuality==q&&Text("Auto Quality Threshold").text.Contains(GuxuUiArt.QualityName(q))&&g.ShouldKeep(new Gear{quality=q,slot=0}),"quality "+q+" refreshes its label and actual retention rule");}
  yield return Action("click","Quality Choice 1");yield return Action("click","确认");g.Home();string gear=JsonConvert.SerializeObject(r.Save.gear);int seals=r.Save.peaches;
  yield return Action("click","自动破封");float deadline=Time.realtimeSinceStartup+12;while(Time.realtimeSinceStartup<deadline&&!UnityEngine.Object.FindObjectsOfType<Button>().Any(b=>b.gameObject.activeInHierarchy&&b.name=="Equip Relic"))yield return null;
  Require(r.Save.pending!=null&&!(bool)typeof(GxdlGame).GetField("auto",Private).GetValue(g)&&r.Save.peaches==seals-1&&JsonConvert.SerializeObject(r.Save.gear)==gear,"actual automatic unseal pauses for retained relic without equipping");
  g.Home();Open("ShowFinalResidence");string beforeReplay=Save();yield return Action("click","重温开场");Require(g.IsStoryPlaying,"residence replays actual opening");yield return Action("click","Skip Story");yield return new WaitForSeconds(.5f);Require(!g.IsStoryPlaying&&Save()==beforeReplay,"skipping replay changes no game resources or equipment");
 }
 static int[] Wallet(GameRules r)=>new[]{r.Save.peaches,r.Save.coins,r.Save.forgeOre,r.Save.insight,r.Save.exp,r.Save.breakthroughStones};
 static IEnumerator SettlementRun(){
  foreach(string key in new[]{"settlement-victory-emblem","settlement-defeat-emblem"})Require(GuxuUiArt.Optional(key)!=null,"production result asset imported: "+key);
  yield return Action("size",height:1334);yield return Action("scenario",scenario:"finalboss");var g=GxdlGame.Instance;var r=g.Rules;var stage=GuxuStageConfig.Load().stages[199];
  typeof(GxdlGame).GetField("fightSpeed",Private).SetValue(g,4);int[] before=Wallet(r);var expected=GuxuStageRules.Reward(stage,true);
  g.ShowBattlePrep(stage);yield return Action("click","Ruins Challenge");var battle=g.ActiveBattle;Require(battle!=null,"actual final boss battle starts");float deadline=Time.realtimeSinceStartup+100;while(g.ActiveBattle!=null&&Time.realtimeSinceStartup<deadline)yield return null;
  Require(battle.Completed&&battle.Won&&battle.Settled,"actual final boss reaches paid victory");yield return Action("inspect");CheckModal();
  Require(GameObject.Find("Battle Lower HUD")==null&&GameObject.Find("Battle Enemy HUD")==null&&GameObject.Find("Battle Scenery")!=null,"settlement retains the scene while removing ended battle controls");
  int[] reward={expected.seals,expected.dust,expected.ore,expected.insight,expected.exp,expected.stones};Require(Wallet(r).Select((value,index)=>value-before[index]==reward[index]).All(x=>x),"all six actual rewards paid exactly once");
  Require(r.Save.stage==201&&!Button("主线已通关").interactable,"completed main shows terminal status without stage 201 challenge");
  for(int i=0;i<6;i++)Require(Text("Settlement Reward Value "+i).text=="+"+reward[i],"reward display matches actual grant "+i);
  Require(Text("Settlement Stat 0").text==battle.Round.ToString()&&Text("Settlement Stat 1").text==battle.DamageDone.ToString()&&Text("Settlement Stat 2").text==battle.DamageTaken.ToString()&&Text("Settlement Stat 3").text==battle.HealingDone.ToString(),"all four statistics read completed battle");
  string paidSave=Save();Require(!GuxuStageRules.Settle(r,battle,DateTime.UtcNow,out var duplicate)&&Save()==paidSave,"settlement rejects duplicate reward with unchanged save");
  yield return Action("size",height:1624);CheckModal();yield return Action("click","Close Modal");Require(g.ExpansionScreen=="chapter"&&Save()==paidSave,"result close returns to the actual chapter without paying rewards again");
  yield return Action("scenario",scenario:"defeat");r.RefreshDaily(DateTime.UtcNow);string beforeDefeat=Save();g.ShowBattlePrep(stage);yield return Action("click","Ruins Challenge");battle=g.ActiveBattle;deadline=Time.realtimeSinceStartup+60;while(g.ActiveBattle!=null&&Time.realtimeSinceStartup<deadline)yield return null;
  Require(battle.Completed&&!battle.Won&&Save()==beforeDefeat,"actual defeat changes no resources or progression");yield return Action("inspect");CheckModal();Require(GameObject.Find("Settlement Reward Icon 0")==null&&Text("Settlement Advice").text.Length>0,"defeat shows concrete advice and no invented rewards");
  yield return Action("size",height:1334);CheckModal();yield return Action("click","再次挑战");Require(g.CurrentStage.id==200&&GameObject.Find("Battle Preparation")!=null,"retry prepares the same actual encounter");g.CloseModal();g.Home();
 }
}
