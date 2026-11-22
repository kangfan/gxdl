using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Gxdl;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class GuxuDungeonCourtyardCheck {
 static readonly List<string> evidence=new List<string>();static bool running;static int assertions;
 [MenuItem("GXDL/QA/UIStyle Dungeon Courtyard")]
 public static void Begin(){if(running||!Application.isPlaying||!GxdlGame.Instance.CanPlay)throw new Exception("Use offline preview with no active review");evidence.Clear();assertions=0;GuxuUI23Review.BeginStyle();running=true;GxdlGame.Instance.StartCoroutine(Guard(Check()));}
 static void Require(bool pass,string message){assertions++;if(!pass)throw new Exception(message);evidence.Add("PASS "+message);}
 static IEnumerator Guard(IEnumerator routine){var stack=new Stack<IEnumerator>();stack.Push(routine);bool passed=false;try{while(stack.Count>0){bool more;object value=null;try{more=stack.Peek().MoveNext();if(more)value=stack.Peek().Current;}catch(Exception error){evidence.Add("FAIL "+error);Debug.LogException(error);yield break;}if(!more){(stack.Pop() as IDisposable)?.Dispose();continue;}if(value is IEnumerator nested)stack.Push(nested);else yield return value;}passed=true;}finally{while(stack.Count>0)(stack.Pop() as IDisposable)?.Dispose();GuxuUI23Review.End();running=false;string report=DateTime.UtcNow.ToString("O")+" "+(passed?"PASS":"FAIL")+" "+assertions+" assertions\nSession: "+GuxuUI23Review.ActiveOutput+"\n"+string.Join("\n",evidence)+"\nRESTORED via isolated review fixture";File.WriteAllText("Reference/UIStyleRework20261111/technical/dungeon-courtyard-check.txt",report);File.WriteAllText(GuxuUI23Review.ActiveOutput+"/dungeon-courtyard-check.txt",report);}}
 static IEnumerator Action(string action,string name="",string scenario="",int height=1334)=>GuxuUI23Review.Run(new GuxuUI23Review.Command{action=action,name=name,scenario=scenario,height=height});
 static Text Text(string name)=>UnityEngine.Object.FindObjectsOfType<Text>().Single(t=>t.name==name&&t.gameObject.activeInHierarchy);
 static Button Button(string name)=>UnityEngine.Object.FindObjectsOfType<Button>().Single(t=>t.name==name&&t.gameObject.activeInHierarchy);
 static IEnumerator Check(){
  var g=GxdlGame.Instance;var r=g.Rules;
  foreach(string key in new[]{"dungeon-portal-forge","dungeon-portal-gems","dungeon-portal-insight","dungeon-portal-spirit"}){var sprite=GuxuUiArt.Optional(key);Require(sprite&&sprite.texture.width==384&&sprite.texture.height==384,"portal production resolution "+key);}
  yield return Action("scenario",scenario:"growth");g.ShowDungeons();yield return Action("size",height:1334);
  for(int kind=0;kind<4;kind++){Require(Text("Dungeon Progress "+kind).text=="已通关 0 / 10"&&Text("Dungeon Attempts "+kind).text=="今日剩余 3 次"&&Button("Dungeon "+kind).interactable,"real uncompleted progress and attempts "+kind);yield return Action("click","Dungeon "+kind);Require(g.ExpansionScreen=="dungeon"&&UnityEngine.Object.FindObjectsOfType<Text>().Any(t=>t.text==GuxuStageRules.DungeonNames[kind]),"each portal opens correct real difficulty "+kind);yield return Action("click","Difficulty Back");}
  r.Save.dungeonClears[0]=1;g.ShowDungeons();yield return Action("click","Dungeon 0");yield return Action("click","Difficulty Node 1");int coins=r.Save.coins,ore=r.Save.forgeOre;var reward=GuxuStageRules.Reward(GuxuStageConfig.Load().Dungeon(0,1),false);yield return Action("click","Difficulty Sweep");Require(r.Save.dungeonClaims[0]==1&&r.Save.coins==coins+reward.dust&&r.Save.forgeOre==ore+reward.ore,"actual sweep consumes one claim and grants canonical rewards");yield return Action("click","Difficulty Back");Require(Text("Dungeon Progress 0").text=="已通关 1 / 10"&&Text("Dungeon Attempts 0").text=="今日剩余 2 次","return immediately reflects sweep and clear state");
  r.Save.dungeonClaims[0]=3;g.ShowDungeons();yield return Action("size",height:1624);Require(Text("Dungeon Attempts 0").text=="今日剩余 0 次"&&Button("Dungeon 0").interactable,"exhausted courtyard keeps portal inspectable");yield return Action("click","Dungeon 0");Require(!UnityEngine.Object.FindObjectsOfType<Button>().Any(b=>b.interactable&&(b.name=="Difficulty Challenge"||b.name=="Difficulty Sweep")),"exhausted difficulty actions disabled");yield return Action("click","Difficulty Back");
  r.Save.rewardDay=GameRules.Day(DateTime.UtcNow.AddDays(-1));g.ShowDungeons();yield return Action("inspect");Require(r.Save.dungeonClaims.All(x=>x==0)&&Text("Dungeon Attempts 0").text=="今日剩余 3 次","actual day refresh updates all visible attempts");
  yield return Action("scenario",scenario:"new");g.ShowDungeons();yield return Action("inspect");string save=JsonConvert.SerializeObject(r.Save);for(int kind=0;kind<4;kind++){yield return Action("reject-click","Dungeon "+kind);Require(Text("Dungeon Progress "+kind).text=="主线第"+GuxuStageRules.DungeonUnlocks[kind]+"关后开放"&&!Button("Dungeon "+kind).interactable,"locked portal uses canonical unlock condition "+kind);}Require(save==JsonConvert.SerializeObject(r.Save),"locked portals do not mutate save");
  yield return Action("scenario",scenario:"growth");g.ShowDungeons();yield return Action("inspect");yield return Action("click","通天古塔 · 百层试炼");Require(g.ExpansionScreen=="tower","tower secondary entrance works");g.ShowDungeons();yield return Action("click","修行与养成");Require(g.ExpansionScreen=="growth","growth secondary entrance works");
 }
}
