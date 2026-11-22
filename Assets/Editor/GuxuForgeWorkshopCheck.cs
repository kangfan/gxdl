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

public static class GuxuForgeWorkshopCheck {
 static readonly List<string> evidence=new List<string>();static bool running;static int assertions;
 [MenuItem("GXDL/QA/UIStyle Forge")]
 public static void Begin(){if(running||!Application.isPlaying||!GxdlGame.Instance.CanPlay)throw new Exception("Use offline preview with no active review");evidence.Clear();assertions=0;GuxuUI23Review.BeginStyle();running=true;GxdlGame.Instance.StartCoroutine(Guard(Check()));}
 static void Require(bool pass,string message){assertions++;if(!pass)throw new Exception(message);evidence.Add("PASS "+message);}
 static IEnumerator Guard(IEnumerator routine){var stack=new Stack<IEnumerator>();stack.Push(routine);bool passed=false;try{while(stack.Count>0){bool more;object value=null;try{more=stack.Peek().MoveNext();if(more)value=stack.Peek().Current;}catch(Exception error){evidence.Add("FAIL "+error);Debug.LogException(error);yield break;}if(!more){(stack.Pop() as IDisposable)?.Dispose();continue;}if(value is IEnumerator nested)stack.Push(nested);else yield return value;}passed=true;}finally{while(stack.Count>0)(stack.Pop() as IDisposable)?.Dispose();GuxuUI23Review.End();running=false;string report=DateTime.UtcNow.ToString("O")+" "+(passed?"PASS":"FAIL")+" "+assertions+" assertions\nSession: "+GuxuUI23Review.ActiveOutput+"\n"+string.Join("\n",evidence)+"\nRESTORED via isolated review fixture";File.WriteAllText("Reference/UIStyleRework20261111/technical/forge-check.txt",report);File.WriteAllText(GuxuUI23Review.ActiveOutput+"/forge-check.txt",report);}}
 static IEnumerator Action(string action,string name="",string scenario="",int height=1334)=>GuxuUI23Review.Run(new GuxuUI23Review.Command{action=action,name=name,scenario=scenario,height=height});
 static Button Button(string name)=>UnityEngine.Object.FindObjectsOfType<Button>().Single(b=>b.name==name&&b.gameObject.activeInHierarchy);
 static Text Text(string name)=>UnityEngine.Object.FindObjectsOfType<Text>().Single(t=>t.name==name&&t.gameObject.activeInHierarchy);
 static string Save()=>JsonConvert.SerializeObject(GxdlGame.Instance.Rules.Save);
 static void CheckDisplay(GameRules r,int slot){
  int level=r.Save.forgeLevels[slot];var current=GameRules.ForgeBonus(level);var next=GameRules.ForgeBonus(Math.Min(level+1,r.ForgeLimit));
  int[] now={current.attack,current.hp,current.defense},after={next.attack,next.hp,next.defense};
  Require(Enumerable.Range(0,3).All(i=>Text("Forge Current "+i).text=="+"+now[i]&&Text("Forge Next "+i).text=="+"+after[i]),"three current/next values use canonical forge bonus");
  Require(Text("Forge Selection").text.Contains("锻造 "+level+" / "+r.ForgeLimit)&&Text("Forge Selected Tier").text==GuxuUiArt.TierLabel(r.Save.gear[slot].quality,r.Save.gear[slot].level),"selection reads actual slot level, cap and gear tier");
  Require(Text("Forge Ore Cost").text=="玄铁：消耗 "+r.ForgeCost(slot)+" / 拥有 "+r.Save.forgeOre&&Text("Forge Dust Cost").text=="灵尘：消耗 "+r.ForgeDustCost(slot)+" / 拥有 "+r.Save.coins,"both costs and inventories refresh from Rules");
  Require(Text("Expansion Resource Summary").text=="破封符 "+r.Save.peaches+"    灵尘 "+r.Save.coins+"    战力 "+r.Attributes.Power,"wallet and power refresh together");
 }
 static IEnumerator Check(){
  Require(GuxuUiArt.Optional("forge-environment"),"production forge environment imported");
  yield return Action("scenario",scenario:"growth");yield return Action("size",height:1334);var g=GxdlGame.Instance;var r=g.Rules;
  g.ShowGrowthHub();yield return Action("click","Growth Forge");Require(g.ExpansionScreen=="forge"&&GameObject.Find("Forge Workbench")!=null,"growth opens actual forge workshop");
  int ore=r.Save.forgeOre,dust=r.Save.coins,oreCost=r.ForgeCost(0),dustCost=r.ForgeDustCost(0),power=r.Attributes.Power;
  yield return Action("click","Forge Upgrade");Require(r.Save.forgeLevels[0]==1&&r.Save.forgeOre==ore-oreCost&&r.Save.coins==dust-dustCost&&r.Attributes.Power>power,"upgrade spends exact canonical costs and changes attributes");CheckDisplay(r,0);
  var saved=JsonConvert.DeserializeObject<SaveData>(File.ReadAllText(GuxuUI23Review.ActiveOutput+"/player-fixture.json"));Require(saved.forgeLevels[0]==r.Save.forgeLevels[0]&&saved.coins==r.Save.coins,"upgrade persisted only into isolated save");
  string unchanged=Save();
  for(int slot=0;slot<12;slot++){yield return Action("click","Forge Slot "+slot);Require(GameObject.Find("Forge Selected Equipment").GetComponent<Image>().sprite.name=="equipment-"+slot.ToString("00")&&Text("Forge Slot Level "+slot).text.EndsWith("+"+r.Save.forgeLevels[slot])&&unchanged==Save(),"slot "+slot+" changes selected equipment without spending");}
  ore=r.Save.forgeOre;dust=r.Save.coins;oreCost=r.ForgeCost(11);dustCost=r.ForgeDustCost(11);yield return Action("click","Forge Upgrade");Require(r.Save.forgeLevels[11]==1&&r.Save.forgeLevels[0]==1&&r.Save.forgeLevels.Skip(1).Take(10).All(level=>level==0)&&r.Save.forgeOre==ore-oreCost&&r.Save.coins==dust-dustCost,"only selected slot upgrades with its own costs");CheckDisplay(r,11);
  Require(r.Chop(),"actual unseal produces replacement gear");int replaced=r.Save.pending.slot;r.Save.pending.quality=5;r.Save.pending.level=2;EquipmentTiers.Apply(r.Save.pending);if(r.Save.forgeLevels[replaced]==0)Require(r.Forge(replaced),"replacement fixture has a legally forged slot");int forgeLevel=r.Save.forgeLevels[replaced];Require(r.Resolve(true),"actual equip rule replaces the pending relic");g.ShowForge(replaced);yield return Action("inspect");Require(forgeLevel>0&&r.Save.forgeLevels[replaced]==forgeLevel&&Text("Forge Selected Tier").text==GuxuUiArt.TierLabel(5,2),"gear replacement inherits slot forging and updates selected rarity");
  g.ShowForge(11);float footer=-GameObject.Find("Forge Footer").GetComponent<RectTransform>().anchoredPosition.y;yield return Action("size",height:1624);Require(Math.Abs(-GameObject.Find("Forge Footer").GetComponent<RectTransform>().anchoredPosition.y-footer-290)<1&&GameObject.Find("Forge Selected Equipment").GetComponent<RectTransform>().rect.width<=180,"tall screen reanchors footer without stretching equipment");yield return Action("click","Forge Slot 3");CheckDisplay(r,3);yield return Action("size",height:1334);
  yield return Action("click","返回修行");yield return Action("click","Growth Forge");Require(Text("Forge Slot Level 11").text.EndsWith("+1"),"cross-page reentry retains forged levels");
  r.Save.forgeLevels[0]=r.ForgeLimit;g.ShowForge();yield return Action("inspect");unchanged=Save();yield return Action("reject-click","Forge Upgrade");Require(Text("Forge Upgrade Label").text=="已达当前上限"&&!r.Forge(0)&&unchanged==Save(),"current progression cap blocks UI and rule without mutation");
  r.Save.forgeLevels[0]=0;r.Save.coins=0;g.ShowForge();yield return Action("inspect");unchanged=Save();yield return Action("reject-click","Forge Upgrade");Require(!r.Forge(0)&&unchanged==Save(),"insufficient dust does not spend ore");
  r.Save.coins=50000;r.Save.forgeOre=0;g.ShowForge();yield return Action("inspect");unchanged=Save();yield return Action("reject-click","Forge Upgrade");Require(!r.Forge(0)&&unchanged==Save(),"insufficient ore does not spend dust");
  yield return Action("click","获取玄铁");Require(g.ExpansionScreen=="dungeon","ore action opens the real resource dungeon");
  yield return Action("scenario",scenario:"new");g.ShowForge();yield return Action("inspect");unchanged=Save();yield return Action("reject-click","Forge Upgrade");Require(Text("Forge Upgrade Label").text=="第5关后开放"&&!r.Forge(0)&&unchanged==Save(),"new player cannot forge before main5 clear");
  r.Save.stage=6;g.ShowForge();yield return Action("click","Forge Upgrade");Require(r.Save.forgeLevels[0]==1&&Text("Forge Selection").text.Contains("1 / "+r.ForgeLimit),"real main5 gate unlocks with progression-dependent cap");
 }
}
