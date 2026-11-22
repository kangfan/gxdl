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

public static class GuxuRefinementCheck {
 static readonly List<string> evidence=new List<string>();static bool running;static int assertions;
 [MenuItem("GXDL/QA/UIStyle Refined Panels")]
 public static void Begin(){if(running||!Application.isPlaying||!GxdlGame.Instance.CanPlay)throw new Exception("Use offline preview with no active review");evidence.Clear();assertions=0;GuxuUI23Review.BeginStyle();running=true;GxdlGame.Instance.StartCoroutine(Guard(Check()));}
 static void Require(bool pass,string message){assertions++;if(!pass)throw new Exception(message);evidence.Add("PASS "+message);}
 static IEnumerator Guard(IEnumerator routine){var stack=new Stack<IEnumerator>();stack.Push(routine);bool passed=false;try{while(stack.Count>0){bool more;object value=null;try{more=stack.Peek().MoveNext();if(more)value=stack.Peek().Current;}catch(Exception error){evidence.Add("FAIL "+error);Debug.LogException(error);yield break;}if(!more){(stack.Pop() as IDisposable)?.Dispose();continue;}if(value is IEnumerator nested)stack.Push(nested);else yield return value;}passed=true;}finally{while(stack.Count>0)(stack.Pop() as IDisposable)?.Dispose();GuxuUI23Review.End();running=false;string report=DateTime.UtcNow.ToString("O")+" "+(passed?"PASS":"FAIL")+" "+assertions+" assertions\nSession: "+GuxuUI23Review.ActiveOutput+"\n"+string.Join("\n",evidence)+"\nRESTORED via isolated review fixture";File.WriteAllText("Reference/UIStyleRework20261111/technical/refined-panels-check.txt",report);File.WriteAllText(GuxuUI23Review.ActiveOutput+"/refined-panels-check.txt",report);}}
 static IEnumerator Action(string action,string name="",string scenario="",int height=1334)=>GuxuUI23Review.Run(new GuxuUI23Review.Command{action=action,name=name,scenario=scenario,height=height});
 static void Open(string name,params object[] args)=>typeof(GxdlGame).GetMethod(name,BindingFlags.NonPublic|BindingFlags.Instance).Invoke(GxdlGame.Instance,args);
 static Text Text(string name)=>UnityEngine.Object.FindObjectsOfType<Text>().Single(t=>t.name==name&&t.gameObject.activeInHierarchy);
 static Button Button(string name)=>UnityEngine.Object.FindObjectsOfType<Button>().Single(b=>b.name==name&&b.gameObject.activeInHierarchy);
 static bool Has(string value)=>UnityEngine.Object.FindObjectsOfType<Text>().Any(t=>t.gameObject.activeInHierarchy&&t.text==value);
 static bool Fits(RectTransform r){var corners=new Vector3[4];r.GetWorldCorners(corners);var camera=r.GetComponentInParent<Canvas>().worldCamera;return corners.Select(c=>RectTransformUtility.WorldToScreenPoint(camera,c)).All(p=>p.x>=0&&p.x<=Screen.width&&p.y>=0&&p.y<=Screen.height);}
 static void CheckModal(){var modal=GameObject.Find("Modal").transform;Require(UnityEngine.Object.FindObjectsOfType<Button>().Where(b=>b.gameObject.activeInHierarchy&&b.transform.IsChildOf(modal)).All(b=>Fits((RectTransform)b.transform)),"all modal controls fit viewport");}
 static IEnumerator Check(){
  var g=GxdlGame.Instance;var r=g.Rules;yield return Action("scenario",scenario:"growth");
  foreach(int height in new[]{1334,1624}){
   yield return Action("size",height:height);g.ShowRealmPage();yield return Action("inspect");
   Require(Text("Realm Name").text==r.Realm.displayName&&Text("Realm Experience").text=="修为  "+r.Save.exp+" / "+r.ExpNeeded&&Text("Realm Stone Cost").text=="突破石  "+r.Save.breakthroughStones+" / 需要 "+r.BreakthroughCost,"realm fields use current rules at "+height);
   int level=r.Save.level,exp=r.Save.exp,expCost=r.ExpNeeded,stones=r.Save.breakthroughStones,stoneCost=r.BreakthroughCost;
   if(exp<expCost){r.Save.exp=expCost;exp=expCost;g.ShowRealmPage();}
   yield return Action("click","境界突破");Require(r.Save.level==level+1&&r.Save.exp==exp-expCost&&r.Save.breakthroughStones==stones-stoneCost&&Text("Realm Name").text==r.Realm.displayName,"actual breakthrough pays canonical cost and immediately refreshes realm");
   if(height==1624)r.Save.gear[3].affix=3;
   g.Home();Open("ShowGear",height==1334?0:3);yield return Action("inspect");CheckModal();var gear=r.Save.gear[height==1334?0:3];Require(Has("+"+gear.attack)&&Has("+"+gear.hp)&&Has("+"+gear.defense)&&Has("+"+gear.speed)&&Text("Equipment Affix").text==GameRules.AffixNames[Mathf.Clamp(gear.affix,0,6)],"equipment values and affix use selected actual item");
   g.CloseModal();Open("ShowCodex");yield return Action("inspect");CheckModal();Require(UnityEngine.Object.FindObjectsOfType<Button>().Count(b=>b.gameObject.activeInHierarchy&&b.name.StartsWith("Codex "))==12,"all twelve codex items remain interactive");
   int slot=height==1334?0:3;yield return Action("click","Codex "+slot);CheckModal();int rowIndex=0;
   foreach(var row in EquipmentTiers.All){var preview=new Gear{slot=slot,quality=row.quality,level=row.level,lootBand=r.Save.gear[slot].lootBand};EquipmentTiers.Apply(preview);string[] values={GuxuUiArt.TierLabel(row.quality,row.level),preview.attack.ToString(),preview.hp.ToString(),preview.defense.ToString()};for(int c=0;c<4;c++)Require(Text("TierTable Value "+slot+" "+rowIndex+" "+c).text==values[c],"quality table real value "+slot+"/"+rowIndex+"/"+c);rowIndex++;}
   Require(rowIndex==15,"five qualities retain all three tiers");yield return Action("click","返回图鉴");Require(GameObject.Find("遗宝图鉴 · 五品质三阶"),"quality table returns to codex");g.CloseModal();Open("TreeUpgrade");yield return Action("inspect");CheckModal();
   int tree=r.Save.treeLevel,coins=r.Save.coins,cost=r.TreeUpgradeCost;yield return Action("click","Appraisal Upgrade");Require(r.Save.treeLevel==tree+1&&r.Save.coins==coins-cost&&Text("Spirit Stone Balance").text=="灵尘 "+r.Save.coins,"appraisal upgrade refreshes modal and underlying home wallet");
   var current=r.QualityChances(r.Save.treeLevel);var next=r.QualityChances(r.Save.treeLevel+1);for(int i=0;i<5;i++)Require(Text("Appraisal Current "+i).text==current[i].ToString("0.#")+"%"&&Text("Appraisal Next "+i).text==next[i].ToString("0.#")+"%","appraisal row uses canonical current and next chances "+i);
   Require(Text("Appraisal Cost").text=="升级消耗 "+r.TreeUpgradeCost+" 灵尘   /   拥有 "+r.Save.coins,"next upgrade cost and balance immediately refresh");g.CloseModal();
  }
  r.Save.coins=0;g.Home();Open("TreeUpgrade");string save=JsonConvert.SerializeObject(r.Save);yield return Action("reject-click","Appraisal Upgrade");Require(save==JsonConvert.SerializeObject(r.Save)&&!Button("Appraisal Upgrade").interactable,"insufficient appraisal funds reject without mutation");g.CloseModal();
  r.Save.level=10;r.Save.exp=r.ExpNeeded;r.Save.breakthroughStones=0;g.ShowRealmPage();save=JsonConvert.SerializeObject(r.Save);yield return Action("reject-click","突破石不足");Require(save==JsonConvert.SerializeObject(r.Save),"breakthrough shortage rejects without spending experience");
 }
}
