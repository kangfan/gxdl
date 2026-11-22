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

public static class GuxuBattlePrepStyleCheck {
 static readonly List<string> evidence=new List<string>();static bool running;static int assertions;
 [MenuItem("GXDL/QA/UIStyle Battle Preparation")]
 public static void Begin(){if(running||!Application.isPlaying||!GxdlGame.Instance.CanPlay)throw new Exception("Use offline preview with no active review");evidence.Clear();assertions=0;GuxuUI23Review.BeginStyle();running=true;GxdlGame.Instance.StartCoroutine(Guard(Check()));}
 static void Require(bool pass,string message){assertions++;if(!pass)throw new Exception(message);evidence.Add("PASS "+message);}
 static IEnumerator Guard(IEnumerator routine){var stack=new Stack<IEnumerator>();stack.Push(routine);bool passed=false;try{while(stack.Count>0){bool more;object value=null;try{more=stack.Peek().MoveNext();if(more)value=stack.Peek().Current;}catch(Exception error){evidence.Add("FAIL "+error);Debug.LogException(error);yield break;}if(!more){(stack.Pop() as IDisposable)?.Dispose();continue;}if(value is IEnumerator nested)stack.Push(nested);else yield return value;}passed=true;}finally{while(stack.Count>0)(stack.Pop() as IDisposable)?.Dispose();GuxuUI23Review.End();running=false;string report=DateTime.UtcNow.ToString("O")+" "+(passed?"PASS":"FAIL")+" "+assertions+" assertions\nSession: "+GuxuUI23Review.ActiveOutput+"\n"+string.Join("\n",evidence)+"\nRESTORED via isolated review fixture";File.WriteAllText("Reference/UIStyleRework20261111/technical/battle-prep-check.txt",report);File.WriteAllText(GuxuUI23Review.ActiveOutput+"/battle-prep-check.txt",report);}}
 static IEnumerator Action(string action,string name="",string scenario="",int height=1334)=>GuxuUI23Review.Run(new GuxuUI23Review.Command{action=action,name=name,scenario=scenario,height=height});
 static Button Button(string name)=>UnityEngine.Object.FindObjectsOfType<Button>().Single(b=>b.name==name&&b.gameObject.activeInHierarchy);
 static Text Text(string name)=>UnityEngine.Object.FindObjectsOfType<Text>().Single(t=>t.name==name&&t.gameObject.activeInHierarchy);
 static IEnumerator Check(){
  var sprite=GuxuUiArt.Optional("battle-prep-panel");Require(sprite&&sprite.texture.width==750&&sprite.texture.height==1334,"new transparent panel imported at runtime display resolution");
  var g=GxdlGame.Instance;var r=g.Rules;yield return Action("size",height:1334);yield return Action("scenario",scenario:"chapter5");yield return Action("click","Chapter Challenge");
  Require(GameObject.Find("Battle Preparation Skin").GetComponent<Image>().sprite==sprite,"actual panel uses new art rather than old baked chapter skin");Require(g.ExpansionScreen=="chapter"&&GameObject.Find("Chapter Map Viewport"),"real current chapter remains as modal source");
  Require(GameObject.Find("Chapter Selection Detail").GetComponent<CanvasGroup>().alpha==0,"underlying chapter detail and CTA hidden during prep");
  Require(!UnityEngine.Object.FindObjectsOfType<Button>().Any(b=>b.name=="Prep Growth Footer"||b.name=="Prep Continue"||b.name=="Prep Back"),"duplicate footer and old outer navigation removed");
  var stage=GuxuStageConfig.Load().stages[4];var enemy=GuxuStageRules.Enemy(stage,r.Config);Require(Text("Prep Stage").text==stage.name&&Text("Prep Guardian").text==stage.guardianName&&Text("Prep Enemy Attack").text=="攻击 "+enemy.attack,"selected encounter and real enemy fields preserved");
  for(int i=0;i<4;i++){yield return Action("click","Skill Choice "+i);Require(r.Save.activeSkill==i&&Text("Prep Skill Detail").text==GameRules.SkillDetails[i],"skill choice and description refresh "+i);}
  bool automatic=r.Save.autoSkill;yield return Action("click","Prep Auto Skill");Require(r.Save.autoSkill!=automatic&&Text("Prep Skill Mode").text.Contains(r.Save.autoSkill?"自动":"手动"),"auto toggle updates real save and label");
  yield return Action("size",height:1624);Require(r.Save.activeSkill==3&&GameObject.Find("Battle Preparation"),"resize while modal open preserves selection");
  var shade=GameObject.Find("Full Canvas Modal Shade").GetComponent<Image>();Require(shade.color.a<1&&shade.rectTransform.rect.height>=1623,"full tall canvas has translucent input-blocking shade");string save=JsonConvert.SerializeObject(r.Save);yield return Action("reject-click","Chapter Back");yield return Action("reject-click","Chapter Challenge");Require(save==JsonConvert.SerializeObject(r.Save),"top and bottom underlying chapter controls cannot mutate state through modal");
  yield return Action("click","Skill Choice 1");yield return Action("click","Prep Close");Require(g.ExpansionScreen=="chapter"&&!GameObject.Find("Battle Preparation")&&Text("Chapter Selected Stage").text=="第5关 · 精英","close returns to same selected chapter node");
  Require(GameObject.Find("Chapter Selection Detail").GetComponent<CanvasGroup>().alpha==1,"chapter detail visibility restored after close");
  yield return Action("click","Chapter Challenge");yield return Action("click","Prep Growth");Require(g.ExpansionScreen=="growth"&&!GameObject.Find("Battle Preparation"),"single growth action navigates normally");g.ShowBattlePrep(stage);yield return Action("inspect");yield return Action("click","Prep Close");Require(g.ExpansionScreen=="growth","modal source is real growth page when opened there");yield return Action("size",height:1334);
 }
}
