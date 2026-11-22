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

public static class GuxuGemsCheck {
 static readonly List<string> evidence=new List<string>();static bool running;static int assertions;
 [MenuItem("GXDL/QA/UIStyle Gems")]
 public static void Begin(){if(running||!Application.isPlaying||!GxdlGame.Instance.CanPlay)throw new Exception("Use offline preview with no active review");evidence.Clear();assertions=0;GuxuUI23Review.BeginStyle();running=true;GxdlGame.Instance.StartCoroutine(Guard(Check()));}
 static void Require(bool pass,string message){assertions++;if(!pass)throw new Exception(message);evidence.Add("PASS "+message);}
 static IEnumerator Guard(IEnumerator routine){var stack=new Stack<IEnumerator>();stack.Push(routine);bool passed=false;try{while(stack.Count>0){bool more;object value=null;try{more=stack.Peek().MoveNext();if(more)value=stack.Peek().Current;}catch(Exception error){evidence.Add("FAIL "+error);Debug.LogException(error);yield break;}if(!more){(stack.Pop() as IDisposable)?.Dispose();continue;}if(value is IEnumerator nested)stack.Push(nested);else yield return value;}passed=true;}finally{while(stack.Count>0)(stack.Pop() as IDisposable)?.Dispose();GuxuUI23Review.End();running=false;string report=DateTime.UtcNow.ToString("O")+" "+(passed?"PASS":"FAIL")+" "+assertions+" assertions\nSession: "+GuxuUI23Review.ActiveOutput+"\n"+string.Join("\n",evidence)+"\nRESTORED via isolated review fixture";File.WriteAllText("Reference/UIStyleRework20261111/technical/gems-check.txt",report);File.WriteAllText(GuxuUI23Review.ActiveOutput+"/gems-check.txt",report);}}
 static IEnumerator Action(string action,string name="",string scenario="",int height=1334)=>GuxuUI23Review.Run(new GuxuUI23Review.Command{action=action,name=name,scenario=scenario,height=height});
 static Button Button(string name)=>UnityEngine.Object.FindObjectsOfType<Button>().Single(b=>b.name==name&&b.gameObject.activeInHierarchy);
 static Text Text(string name)=>UnityEngine.Object.FindObjectsOfType<Text>().Single(t=>t.name==name&&t.gameObject.activeInHierarchy);
 static RectTransform Rect(string name)=>GameObject.Find(name).GetComponent<RectTransform>();
 static IEnumerator Check(){
  Require(GuxuUiArt.Optional("gem-workbench")&&GuxuUiArt.Optional("gem-socket-base"),"production workbench and socket imported");var g=GxdlGame.Instance;var r=g.Rules;
  yield return Action("size",height:1334);yield return Action("scenario",scenario:"gemconcept");
  Require(r.Save.gemSlots[0]==1&&r.Save.gemSlots[1]==8&&r.Save.gemInventory[7]==5,"concept uses legal two-socket operations");Require(Text("Gem Selected Effect").text==r.GemEffect(8)&&Text("Gem Selected Stock").text=="拥有 5","selected life2 detail is bound to actual rules");
  Require(Text("Gem Total Inventory").text=="库存 "+r.Save.gemInventory.Sum()+" 枚","header total equals actual inventory");Require(UnityEngine.Object.FindObjectsOfType<Button>().Count(b=>b.name.StartsWith("Gem Slot "))==12&&UnityEngine.Object.FindObjectsOfType<Button>().Count(b=>b.name.StartsWith("Gem Inventory "))==6,"12 equipment slots and 6 inventory choices");
  string save=JsonConvert.SerializeObject(r.Save);yield return Action("click","Gem Inventory 6");yield return Action("click","Gem Socket 0");Require(save==JsonConvert.SerializeObject(r.Save),"changing level and target socket spends nothing");
  int hp=r.Attributes.hp,attack=r.Attributes.attack,total=r.Save.gemInventory.Sum();yield return Action("click","Gem Equip");Require(r.Save.gemSlots[0]==7&&r.Save.gemInventory[6]==5&&r.Save.gemInventory[0]==6&&r.Save.gemInventory.Sum()==total,"replacement returns prior gem and transfers chosen gem");Require(r.Attributes.hp>hp&&r.Attributes.attack<attack,"replacement updates actual derived stats");
  yield return Action("click","Gem Remove");Require(r.Save.gemSlots[0]==0&&r.Save.gemInventory[6]==6&&r.Save.gemInventory.Sum()==total+1&&!Button("Gem Remove").interactable,"free unsocket returns inventory and refreshes remove action");
  int next=r.Save.gemInventory[7];total=r.Save.gemInventory.Sum();yield return Action("click","Gem Combine");Require(r.Save.gemInventory[6]==3&&r.Save.gemInventory[7]==next+1&&r.Save.gemInventory.Sum()==total-2,"3-to-1 consumes three and grants one");Require(Text("Gem Stock 6").text=="3"&&Text("Gem Stock 7").text==r.Save.gemInventory[7].ToString()&&Text("Gem Total Inventory").text=="库存 "+r.Save.gemInventory.Sum()+" 枚","stock rows, selection and total refresh immediately");
  yield return Action("click","Gem Inventory 11");save=JsonConvert.SerializeObject(r.Save);yield return Action("reject-click","Gem Combine");Require(Button("Gem Combine").GetComponentInChildren<Text>().text=="最高级"&&!r.CombineGem(11)&&save==JsonConvert.SerializeObject(r.Save),"level6 explicitly max and cannot produce level7");
  r.Save.gemInventory[11]=0;g.ShowGems(0,0,1);yield return Action("inspect");save=JsonConvert.SerializeObject(r.Save);yield return Action("reject-click","Gem Equip");Require(Text("Gem Selected Stock").text=="拥有 0"&&!r.SocketGem(0,0,11)&&save==JsonConvert.SerializeObject(r.Save),"zero inventory disables equip without mutation");
  yield return Action("scenario",scenario:"gemconcept");yield return Action("click","Gem Slot 11");yield return Action("click","Gem Socket 1");yield return Action("click","Gem Type 3");yield return Action("click","Gem Inventory 18");yield return Action("click","Gem Equip");Require(r.Save.gemSlots[23]==19&&r.Save.gemSlots[0]==1&&r.Save.gemSlots[1]==8,"different slot and socket update only correct save index");
  int critical=r.CombatTraits.criticalPercent;yield return Action("click","Gem Remove");Require(r.Save.gemSlots[23]==0&&r.CombatTraits.criticalPercent<critical,"violet gem removal refreshes combat trait");
  yield return Action("scenario",scenario:"gemconcept");float footer=-Rect("Gem Selection Detail").anchoredPosition.y;yield return Action("size",height:1624);Require(Mathf.Abs(-Rect("Gem Selection Detail").anchoredPosition.y-footer-290)<1&&Mathf.Abs(Rect("Gem Socket 1").rect.width-170)<.1f,"tall screen adds space without scaling sockets or footer");yield return Action("click","Gem Inventory 11");yield return Action("click","Gem Slot 11");yield return Action("size",height:1334);Require(GameObject.Find("Gem Selected Socket").transform.parent.name=="Gem Socket 1"&&Text("Gem Selected Stock").text=="拥有 "+r.Save.gemInventory[11],"resize preserves context and controls remain clickable");
  g.ShowGrowthHub();Require(Text("Growth Balance Gems").text=="库存 "+r.Save.gemInventory.Sum()+" 枚","cross-page wallet reflects actual gem inventory");g.ShowGems();yield return Action("inspect");Require(Text("Gem Selected Effect").text==r.GemEffect(12),"reentry preserves selected gem context");
  yield return Action("scenario",scenario:"new");r.Save.gemInventory[0]=3;g.ShowGems(0,0,0);yield return Action("click","Gem Inventory 0");save=JsonConvert.SerializeObject(r.Save);yield return Action("reject-click","Gem Equip");yield return Action("reject-click","Gem Combine");Require(!r.CanSocketGem(0,0,0)&&!r.CanCombineGem(0)&&save==JsonConvert.SerializeObject(r.Save),"20-stage unlock gate enforced for socket and combine");
  r.Save.stage=21;g.ShowGems(0,1,0);yield return Action("inspect");Require(!Button("Gem Equip").interactable&&Button("Gem Equip").GetComponentInChildren<Text>().text.Contains("40关"),"second socket keeps real 40-stage gate");r.Save.stage=41;g.ShowGems(0,1,0);yield return Action("click","Gem Equip");Require(r.Save.gemSlots[1]==1,"second socket becomes usable at actual progress40");yield return Action("scenario",scenario:"gemconcept");
 }
}
