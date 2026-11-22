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

public static class GuxuTalentsCheck {
 static readonly List<string> evidence=new List<string>();static bool running;static int assertions;
 [MenuItem("GXDL/QA/UIStyle Talents")]
 public static void Begin(){if(running||!Application.isPlaying||!GxdlGame.Instance.CanPlay)throw new Exception("Use offline preview with no active review");evidence.Clear();assertions=0;GuxuUI23Review.BeginStyle();running=true;GxdlGame.Instance.StartCoroutine(Guard(Check()));}
 static void Require(bool pass,string message){assertions++;if(!pass)throw new Exception(message);evidence.Add("PASS "+message);}
 static IEnumerator Guard(IEnumerator routine){
  var stack=new Stack<IEnumerator>();stack.Push(routine);bool passed=false;
  try{while(stack.Count>0){bool more;object value=null;try{more=stack.Peek().MoveNext();if(more)value=stack.Peek().Current;}catch(Exception error){evidence.Add("FAIL "+error);Debug.LogException(error);yield break;}if(!more){(stack.Pop() as IDisposable)?.Dispose();continue;}if(value is IEnumerator nested)stack.Push(nested);else yield return value;}passed=true;}
  finally{while(stack.Count>0)(stack.Pop() as IDisposable)?.Dispose();GuxuUI23Review.End();running=false;string report=DateTime.UtcNow.ToString("O")+" "+(passed?"PASS":"FAIL")+" "+assertions+" assertions\nSession: "+GuxuUI23Review.ActiveOutput+"\n"+string.Join("\n",evidence)+"\nRESTORED via isolated review fixture";File.WriteAllText("Reference/UIStyleRework20261111/technical/talents-check.txt",report);File.WriteAllText(GuxuUI23Review.ActiveOutput+"/talents-check.txt",report);}
 }
 static IEnumerator Action(string action,string name="",string scenario="",int height=1334)=>GuxuUI23Review.Run(new GuxuUI23Review.Command{action=action,name=name,scenario=scenario,height=height});
 static Button Button(string name)=>UnityEngine.Object.FindObjectsOfType<Button>().Single(b=>b.name==name&&b.gameObject.activeInHierarchy);
 static Text Text(string name)=>UnityEngine.Object.FindObjectsOfType<Text>().Single(t=>t.name==name&&t.gameObject.activeInHierarchy);
 static RectTransform Rect(string name)=>GameObject.Find(name).GetComponent<RectTransform>();
 static bool Fits(RectTransform rect){var corners=new Vector3[4];rect.GetWorldCorners(corners);var camera=rect.GetComponentInParent<Canvas>().worldCamera;return corners.Select(c=>RectTransformUtility.WorldToScreenPoint(camera,c)).All(p=>p.x>=0&&p.x<=Screen.width&&p.y>=0&&p.y<=Screen.height);}
 static IEnumerator Check(){
  var g=GxdlGame.Instance;var r=g.Rules;yield return Action("size",height:1334);yield return Action("scenario",scenario:"talentconcept");
  Require(r.Save.insight==998&&r.Save.talents[0]==1&&r.Save.talents[1]==0,"legal concept state uses TrainTalent, not hard-coded UI values");
  Require(Text("Talent Balance").text=="悟道点  998"&&Text("Talent Cost").text=="修习消耗 3 悟道点"&&Text("Talent Effect").text=="每级 "+GameRules.TalentDetails[1],"detail values match current rules and save");
  Require(UnityEngine.Object.FindObjectsOfType<Button>().Count(b=>b.name.StartsWith("Talent ")&&int.TryParse(b.name.Substring(7),out _))==8,"eight viewable nodes and one separate train action");
  Require(UnityEngine.Object.FindObjectOfType<GuxuTalentTrail>().Lit.SequenceEqual(new[]{true,false,false,false,false,false,false}),"only satisfied first prerequisite link lights");
  string save=JsonConvert.SerializeObject(r.Save);yield return Action("click","Talent 3");Require(save==JsonConvert.SerializeObject(r.Save)&&Text("Talent Requirement").text.Contains(GameRules.TalentNames[2])&&!Button("Talent Train").interactable,"locked node selection names exact predecessor and spends nothing");
  yield return Action("reject-click","Talent Train");Require(save==JsonConvert.SerializeObject(r.Save),"locked action changes no data");
  yield return Action("click","Talent 1");int points=r.Save.insight,cost=r.TalentCost(1),critical=r.CombatTraits.criticalPercent;
  yield return Action("click","Talent Train");Require(r.Save.talents[1]==1&&r.Save.insight==points-cost&&r.CombatTraits.criticalPercent==critical+2,"train changes real level, points and critical trait immediately");
  Require(Text("Talent Rank 1").text=="1 / 3"&&Text("Talent Balance").text=="悟道点  "+r.Save.insight,"rank and balance immediately refresh");
  yield return Action("click","Talent 2");Require(Button("Talent Train").interactable,"next node unlocks immediately");
  yield return Action("click","Talent 1");for(int n=0;n<2;n++){points=r.Save.insight;cost=r.TalentCost(1);yield return Action("click","Talent Train");Require(r.Save.insight==points-cost,"level cost reads Rules at upgrade "+n);}
  Require(r.Save.talents[1]==3&&Text("Talent Rank 1").text.Contains("满级")&&!Button("Talent Train").interactable,"max rank is explicit and training disabled");yield return Action("reject-click","Talent Train");
  yield return Action("click","Talent Branch 1");yield return Action("click","Talent 8");yield return Action("click","Talent Train");Require(r.Save.talents[8]==1&&r.Save.talents[1]==3,"second branch trains without resetting first");
  yield return Action("click","Talent Branch 2");yield return Action("click","Talent 16");yield return Action("click","Talent Train");Require(r.Save.talents[16]==1&&r.Save.talents[8]==1,"third branch trains independently");
  int remaining=r.Save.insight;r.Save.insight=0;g.ShowTalents(2);yield return Action("click","Talent 16");save=JsonConvert.SerializeObject(r.Save);yield return Action("reject-click","Talent Train");Require(Text("Talent Requirement").text.Contains("悟道点不足")&&save==JsonConvert.SerializeObject(r.Save)&&!r.TrainTalent(16),"insufficient points explains failure and is atomic");
  r.Save.insight=remaining;g.ShowTalents(2);yield return Action("click","Talent Reset");yield return Action("click","Talent Reset Confirm");Require(r.Save.insight==1000&&r.Save.talents.All(x=>x==0)&&r.CombatTraits.criticalPercent==critical,"reset refunds upgrades across all three branches and refreshes traits");
  yield return Action("scenario",scenario:"talentconcept");float nodeY=-Rect("Talent 7").anchoredPosition.y,footerY=-Rect("Talent Selected Detail").anchoredPosition.y;yield return Action("size",height:1624);
  Require(Mathf.Abs(-Rect("Talent 7").anchoredPosition.y-nodeY-290)<1&&Mathf.Abs(-Rect("Talent Selected Detail").anchoredPosition.y-footerY-290)<1,"tall screen adds 290 to constellation and follows bottom");
  Require(Fits(Rect("Talent 7"))&&Fits(Rect("Talent Train"))&&Mathf.Abs(Rect("Talent 7").rect.height-126)<.1f,"tall screen preserves node size and reachable controls");
  yield return Action("click","Talent 7");Require(Text("Talent Effect").text=="每级 "+GameRules.TalentDetails[7],"last tall node receives real pointer click");
  yield return Action("size",height:1334);Require(GameObject.Find("Talent Selected Ring").transform.parent.name=="Talent 7","resize keeps selected node");
  yield return Action("click","Talent Reset");save=JsonConvert.SerializeObject(r.Save);yield return Action("click","Close Modal");Require(save==JsonConvert.SerializeObject(r.Save),"cancel reset leaves all points and nodes intact");
  yield return Action("click","Talent Reset");yield return Action("click","Talent Reset Confirm");Require(r.Save.talents.All(x=>x==0)&&r.Save.insight==1000,"confirmed reset refunds exact legal concept investment");
  Require(Text("Talent Balance").text=="悟道点  1000"&&!Button("Talent Train").interactable,"reset refreshes wallet and selected prerequisite lock");
  yield return Action("click","Talent 0");yield return Action("click","Talent Train");var attributes=JsonConvert.SerializeObject(r.Attributes);g.ShowGrowthHub();g.ShowTalents();yield return Action("inspect");Require(Text("Talent Rank 0").text=="1 / 3"&&attributes==JsonConvert.SerializeObject(r.Attributes),"reentry retains ranks, selection and derived attributes");
  yield return Action("scenario",scenario:"new");g.ShowTalents(0);yield return Action("click","Talent 0");save=JsonConvert.SerializeObject(r.Save);yield return Action("reject-click","Talent Train");Require(Text("Talent Requirement").text.Contains("第10关")&&save==JsonConvert.SerializeObject(r.Save),"mainline unlock gate remains enforced");
  yield return Action("scenario",scenario:"talentconcept");
 }
}
