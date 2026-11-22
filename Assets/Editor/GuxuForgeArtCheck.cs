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

public static class GuxuForgeArtCheck {
 const string Output="Reference/Forge20261109";
 static readonly string[] SlotNames={"兵器","头冠","羽翼","靴子","腰带","护腕","葫芦","项链","玉佩","幡旗","宝珠","道袍"};
 static bool running;
 [MenuItem("GXDL/QA/Forge Art")]
 public static void Run(){
  if(running||!Application.isPlaying||!GxdlGame.Instance)throw new InvalidOperationException("Enter Play mode first.");
  running=true;Directory.CreateDirectory(Output);File.WriteAllText(Output+"/ui-check.txt","RUNNING");GxdlGame.Instance.StartCoroutine(Guard(Check(GxdlGame.Instance)));
 }
 static IEnumerator Guard(IEnumerator check){
  var stack=new Stack<IEnumerator>();stack.Push(check);
  try{while(stack.Count>0){bool more;object current=null;try{more=stack.Peek().MoveNext();if(more)current=stack.Peek().Current;}catch(Exception e){File.WriteAllText(Output+"/ui-check.txt","FAIL: "+e);Debug.LogException(e);yield break;}if(!more){(stack.Pop() as IDisposable)?.Dispose();continue;}if(current is IEnumerator inner)stack.Push(inner);else yield return current;}}
  finally{while(stack.Count>0)(stack.Pop() as IDisposable)?.Dispose();running=false;}
 }
 static void Require(bool ok,string message){if(!ok)throw new Exception("Forge art: "+message);}
 static Text Label(string name)=>UnityEngine.Object.FindObjectsOfType<Text>().Single(t=>t.name==name&&t.gameObject.activeInHierarchy);
 static Button Find(string name)=>UnityEngine.Object.FindObjectsOfType<Button>().Single(b=>b.name==name&&b.gameObject.activeInHierarchy);
 static void Click(string name,bool enabled=true){
  var b=Find(name);Require(b.interactable==enabled,"button state "+name);Canvas.ForceUpdateCanvases();var r=(RectTransform)b.transform;
  var pointer=new PointerEventData(EventSystem.current){position=RectTransformUtility.WorldToScreenPoint(b.GetComponentInParent<Canvas>().worldCamera,r.TransformPoint(r.rect.center)),button=PointerEventData.InputButton.Left};
  var hits=new List<RaycastResult>();EventSystem.current.RaycastAll(pointer,hits);Require(hits.Count>0&&hits[0].gameObject==b.gameObject,"unobstructed hit "+name);ExecuteEvents.Execute(b.gameObject,pointer,ExecuteEvents.pointerClickHandler);
 }
 static IEnumerator Capture(string name){var toast=GameObject.Find("Status Toast");if(toast)toast.SetActive(false);yield return new WaitForEndOfFrame();var image=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes(Output+"/"+name+".png",image.EncodeToPNG());UnityEngine.Object.Destroy(image);}
 static void SetSize(int w,int h)=>typeof(GuxuBattlePreparationCheck).GetMethod("SetSize",BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,new object[]{w,h});
 static void CheckLabels(GameRules r,int slot){
  int level=r.Save.forgeLevels[slot],cost=r.ForgeCost(slot);
  Require(Label("Forge Selection").text==SlotNames[slot]+" · 锻造 "+level+" / "+r.ForgeLimit,"selected slot and cap");
  Require(Label("Forge Bonus").text=="当前加成：攻击 +"+(level*8)+"  生命 +"+(level*60)+"  防御 +"+(level*2),"live bonus");
  Require(Label("Forge Ore Cost").text=="本次消耗：玄铁 "+cost+" / 拥有 "+r.Save.forgeOre,"ore cost");Require(Label("Forge Dust Cost").text=="灵尘 "+(cost*5)+" / 拥有 "+r.Save.coins,"dust cost");
  Require(Label("Forge Tokens").text=="破封符 "+r.Save.peaches&&Label("Forge Dust").text=="灵尘 "+r.Save.coins&&Label("Forge Power").text=="战力 "+r.Attributes.Power,"live wallet and power");
  for(int i=0;i<12;i++){Require(Label("Forge Slot Level "+i).text==SlotNames[i]+" +"+r.Save.forgeLevels[i],"slot level "+i);Require(Label("Forge Tier "+i).text==GuxuUiArt.TierLabel(r.Save.gear[i].quality,r.Save.gear[i].level),"rarity and tier "+i);}
 }
 static IEnumerator Check(GxdlGame g){
  var original=g.Rules;var property=typeof(GxdlGame).GetProperty("Rules");string path=GameRules.SavePath;byte[] disk=File.Exists(path)?File.ReadAllBytes(path):null;
  int opening=PlayerPrefs.GetInt(GxdlGame.OpeningStoryKey,-1),first=PlayerPrefs.GetInt(GxdlGame.FirstUnsealStoryKey,-1);
  var window=EditorWindow.GetWindow(typeof(Editor).Assembly.GetType("UnityEditor.GameView"));var sizeProperty=window.GetType().GetProperty("selectedSizeIndex",BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic);int previousSize=(int)sizeProperty.GetValue(window);
  try{
   PlayerPrefs.SetInt(GxdlGame.OpeningStoryKey,1);PlayerPrefs.SetInt(GxdlGame.FirstUnsealStoryKey,1);g.SkipStory();float deadline=Time.realtimeSinceStartup+3;while(g.IsStoryPlaying&&Time.realtimeSinceStartup<deadline)yield return null;Require(!g.IsStoryPlaying,"opening finished");
   var rules=new GameRules(original.Config,new SaveData{equipmentSchema=1,stage=201,level=60,coins=50000,forgeOre=3000,peaches=40});rules.EnsureExpansion();
   typeof(GameRules).GetField("storagePath",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(rules,Path.GetFullPath(Output+"/fixture-save.json"));property.SetValue(g,rules);
   SetSize(750,1334);yield return null;yield return null;g.Home();g.ShowGrowthHub();yield return new WaitForEndOfFrame();Click("Growth Forge");yield return new WaitForEndOfFrame();
   Require(g.ExpansionScreen=="forge"&&GameObject.Find("Forge Skin"),"growth opens forge");CheckLabels(rules,0);
   int power=rules.Attributes.Power;Click("Forge Upgrade");yield return new WaitForEndOfFrame();
   Require(rules.Save.forgeLevels[0]==1&&rules.Save.forgeOre==2995&&rules.Save.coins==49975&&rules.Attributes.Power==power+110,"exact first forge cost and stats");CheckLabels(rules,0);yield return Capture("01-reference-forge");
   for(int i=0;i<12;i++){Click("Forge Slot "+i);yield return new WaitForEndOfFrame();CheckLabels(rules,i);}
   int ore=rules.Save.forgeOre,dust=rules.Save.coins;Click("Forge Upgrade");yield return new WaitForEndOfFrame();
   Require(rules.Save.forgeLevels[11]==1&&rules.Save.forgeLevels[0]==1&&rules.Save.forgeLevels.Skip(1).Take(10).All(x=>x==0),"only selected slot forged");Require(rules.Save.forgeOre==ore-5&&rules.Save.coins==dust-25,"selected slot cost");
   Require(JsonConvert.DeserializeObject<SaveData>(File.ReadAllText(Output+"/fixture-save.json")).forgeLevels[11]==1,"forge persisted in isolated fixture");
   for(int i=0;i<12;i++){rules.Save.gear[i].quality=i%5+1;rules.Save.gear[i].level=i%3+1;EquipmentTiers.Apply(rules.Save.gear[i]);}
   rules.Save.gear[11]=new Gear{slot=11,quality=5,level=2};EquipmentTiers.Apply(rules.Save.gear[11]);g.ShowForge(11);yield return new WaitForEndOfFrame();CheckLabels(rules,11);Require(rules.Save.forgeLevels[11]==1,"equipment replacement inherits slot level");yield return Capture("02-rarity-and-inherited-forging");
   rules.Save.forgeLevels[7]=rules.ForgeLimit;g.ShowForge(7);yield return new WaitForEndOfFrame();CheckLabels(rules,7);Click("Forge Upgrade",false);Require(rules.Save.forgeLevels[7]==rules.ForgeLimit&&Label("Forge Upgrade Label").text=="已达当前上限","cap blocks pointer action");yield return Capture("03-forge-limit");
   rules.Save.coins=0;rules.Save.forgeOre=0;g.ShowForge(0);yield return new WaitForEndOfFrame();int before=rules.Save.forgeLevels[0];Click("Forge Upgrade");Require(rules.Save.forgeLevels[0]==before&&rules.Save.coins==0&&rules.Save.forgeOre==0,"insufficient materials never deducted");Require(Label("Forge Ore Cost").color.g<.7f&&Label("Forge Dust Cost").color.g<.7f,"shortage highlighted");yield return Capture("04-insufficient-materials");
   Click("获取玄铁");Require(g.ExpansionScreen=="dungeon","ore dungeon opens");g.ShowForge();yield return new WaitForEndOfFrame();Click("返回修行");Require(g.ExpansionScreen=="growth","return to growth");g.ShowForge();yield return new WaitForEndOfFrame();Click("Forge Back");Require(g.ExpansionScreen=="home","back to home");
   rules.Save=new GameRules(original.Config,new SaveData{equipmentSchema=1}).Save;rules.EnsureExpansion();g.ShowForge();yield return new WaitForEndOfFrame();Click("Forge Upgrade",false);Require(rules.Save.forgeLevels[0]==0&&Label("Forge Upgrade Label").text=="第5关后开放","progress lock");yield return Capture("05-locked-new-player");
   SetSize(750,1624);yield return null;yield return null;g.ShowForge(11);yield return new WaitForEndOfFrame();CheckLabels(rules,11);
   var rect=(RectTransform)GameObject.Find("Forge Screen").transform;var corners=new Vector3[4];rect.GetWorldCorners(corners);var camera=rect.GetComponentInParent<Canvas>().worldCamera;
   foreach(var corner in corners){var point=RectTransformUtility.WorldToScreenPoint(camera,corner);Require(point.x>=-1&&point.y>=-1&&point.x<=Screen.width+1&&point.y<=Screen.height+1,"tall phone fits");}
   yield return Capture("06-tall-phone");Click("Forge Slot 3");yield return new WaitForEndOfFrame();CheckLabels(rules,3);
   Require(disk==null?!File.Exists(path):disk.SequenceEqual(File.ReadAllBytes(path)),"player save unchanged");
   File.WriteAllText(Output+"/ui-check.txt","PASS: 12 slot hit areas and live selection; dynamic rarity/tier/slot levels; exact forge costs and +110 power per level; only selected slot changes; isolated persistence; equipment replacement inherits forging; cap/progression/insufficient-material guards; live wallet; dungeon/growth/home navigation; 750x1334 and 750x1624 layouts. Player save unchanged; fixture writes isolated to workspace.");
  }finally{
   property.SetValue(g,original);if(opening<0)PlayerPrefs.DeleteKey(GxdlGame.OpeningStoryKey);else PlayerPrefs.SetInt(GxdlGame.OpeningStoryKey,opening);if(first<0)PlayerPrefs.DeleteKey(GxdlGame.FirstUnsealStoryKey);else PlayerPrefs.SetInt(GxdlGame.FirstUnsealStoryKey,first);sizeProperty.SetValue(window,previousSize);g.Home();
  }
 }
}
