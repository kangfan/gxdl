using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Gxdl;
using Newtonsoft.Json;
using UnityEditor;
using UnityEditor.Media;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public static class GuxuIterationCheck {
 static string Output="Reference/Iteration20261110";
 static bool running;
 static int assertions;
 static readonly List<string> evidence=new List<string>();
 static readonly BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
 static void Require(bool ok,string message){assertions++;if(!ok)throw new Exception("Iteration QA: "+message);}
 static void Note(string message){evidence.Add(message);File.WriteAllLines(Output+"/runtime-progress.txt",evidence);}
 static void Start(IEnumerator routine,string label,string output="Reference/Iteration20261110"){
  if(running||!Application.isPlaying||!GxdlGame.Instance||GxdlGame.Instance.Rules==null)throw new InvalidOperationException("Enter local Play mode first and wait for other QA to finish.");
  Output=output;Directory.CreateDirectory(Output);running=true;assertions=0;evidence.Clear();Note("RUNNING: "+label+" "+DateTimeOffset.Now.ToString("O"));GxdlGame.Instance.StartCoroutine(Guard(routine,label));
 }
 static IEnumerator Guard(IEnumerator routine,string label){
  var stack=new Stack<IEnumerator>();stack.Push(routine);bool passed=false;
  try{while(stack.Count>0){bool more;object current=null;try{more=stack.Peek().MoveNext();if(more)current=stack.Peek().Current;}catch(Exception e){Note("FAIL: "+e);Debug.LogException(e);yield break;}if(!more){(stack.Pop() as IDisposable)?.Dispose();continue;}if(current is IEnumerator inner)stack.Push(inner);else yield return current;}passed=true;}
  finally{while(stack.Count>0)(stack.Pop() as IDisposable)?.Dispose();running=false;if(passed)Note("PASS: "+label+"; "+assertions+" assertions.");File.WriteAllLines(Output+"/"+label+".txt",evidence);}
 }
 sealed class Fixture:IDisposable {
  public readonly GxdlGame game;public readonly GameRules rules;
  readonly GameRules original;readonly string path,openingKey,firstKey;readonly byte[] bytes;readonly int opening,first,size,speed;readonly bool hadOpening,hadFirst;readonly EditorWindow window;readonly PropertyInfo sizeProperty;
  public Fixture(){
   game=GxdlGame.Instance;original=game.Rules;path=GameRules.SavePath;bytes=File.Exists(path)?File.ReadAllBytes(path):null;speed=(int)typeof(GxdlGame).GetField("fightSpeed",Private).GetValue(game);
   openingKey=GxdlGame.OpeningStoryKey;firstKey=GxdlGame.FirstUnsealStoryKey;hadOpening=PlayerPrefs.HasKey(openingKey);hadFirst=PlayerPrefs.HasKey(firstKey);opening=PlayerPrefs.GetInt(openingKey,0);first=PlayerPrefs.GetInt(firstKey,0);
   window=EditorWindow.GetWindow(typeof(Editor).Assembly.GetType("UnityEditor.GameView"));sizeProperty=window.GetType().GetProperty("selectedSizeIndex",BindingFlags.Public|Private);size=(int)sizeProperty.GetValue(window);
   rules=new GameRules(original.Config,new SaveData{stage=201,level=40,equipmentSchema=1,coins=50000,forgeOre=3000,insight=1000,breakthroughStones=100});rules.EnsureExpansion();
   typeof(GameRules).GetField("storagePath",Private).SetValue(rules,Path.GetFullPath(Output+"/runtime-fixture.json"));PlayerPrefs.SetInt(openingKey,1);PlayerPrefs.SetInt(firstKey,1);typeof(GxdlGame).GetProperty("Rules").SetValue(game,rules);
  }
  public void Dispose(){
   try{typeof(GxdlGame).GetProperty("Rules").SetValue(game,original);game.Home();}
   finally{
    try{if(hadOpening)PlayerPrefs.SetInt(openingKey,opening);else PlayerPrefs.DeleteKey(openingKey);if(hadFirst)PlayerPrefs.SetInt(firstKey,first);else PlayerPrefs.DeleteKey(firstKey);PlayerPrefs.Save();}
    finally{sizeProperty.SetValue(window,size);if(game)typeof(GxdlGame).GetField("fightSpeed",Private).SetValue(game,speed);}
   }
   Require(bytes==null?!File.Exists(path):File.Exists(path)&&bytes.SequenceEqual(File.ReadAllBytes(path)),"original player save bytes unchanged");
  }
 }
 static void SetSize(int w,int h)=>typeof(GuxuBattlePreparationCheck).GetMethod("SetSize",BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,new object[]{w,h});
 static bool FitsViewport(RectTransform page){var corners=new Vector3[4];page.GetWorldCorners(corners);var camera=page.GetComponentInParent<Canvas>().worldCamera;return corners.All(c=>{var p=RectTransformUtility.WorldToScreenPoint(camera,c);return p.x>=-1&&p.y>=-1&&p.x<=Screen.width+1&&p.y<=Screen.height+1;});}
 static Button Find(string name,int index=0)=>UnityEngine.Object.FindObjectsOfType<Button>().Where(x=>x.name==name&&x.gameObject.activeInHierarchy).OrderByDescending(x=>x.transform.position.y).ThenBy(x=>x.transform.position.x).ElementAt(index);
 static IEnumerator Click(string name,int index=0){
  yield return new WaitForEndOfFrame();var b=Find(name,index);Require(b.interactable,"enabled button "+name);var rect=(RectTransform)b.transform;
  var pointer=new PointerEventData(EventSystem.current){position=RectTransformUtility.WorldToScreenPoint(b.GetComponentInParent<Canvas>().worldCamera,rect.TransformPoint(rect.rect.center)),button=PointerEventData.InputButton.Left};
  var hits=new List<RaycastResult>();EventSystem.current.RaycastAll(pointer,hits);Require(hits.Count>0&&ExecuteEvents.GetEventHandler<IPointerClickHandler>(hits[0].gameObject)==b.gameObject,"unobstructed pointer "+name);
  ExecuteEvents.Execute(b.gameObject,pointer,ExecuteEvents.pointerDownHandler);yield return null;ExecuteEvents.Execute(b.gameObject,pointer,ExecuteEvents.pointerUpHandler);ExecuteEvents.Execute(b.gameObject,pointer,ExecuteEvents.pointerClickHandler);yield return null;
 }
 static IEnumerator Capture(string name){yield return new WaitForEndOfFrame();var tex=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes(Output+"/"+name+".png",tex.EncodeToPNG());UnityEngine.Object.Destroy(tex);}
 static bool Fits(RectTransform rect){var corners=new Vector3[4];rect.GetWorldCorners(corners);var camera=rect.GetComponentInParent<Canvas>().worldCamera;return corners.Select(c=>RectTransformUtility.WorldToScreenPoint(camera,c)).All(p=>p.x>=-1&&p.y>=-1&&p.x<=Screen.width+1&&p.y<=Screen.height+1);}
 static IEnumerator Ready(GxdlGame g){g.SkipStory();float deadline=Time.realtimeSinceStartup+4;while(g.IsStoryPlaying&&Time.realtimeSinceStartup<deadline)yield return null;Require(!g.IsStoryPlaying,"story ended");g.Home();yield return null;}
 [MenuItem("GXDL/QA/Iteration Preflight")]
 public static void Preflight()=>Start(Inspect(),"preflight");
 static IEnumerator Inspect(){
  using(var f=new Fixture()){
   var g=f.game;yield return Ready(g);SetSize(750,1624);yield return null;yield return null;
   Action[] pages={g.Home,g.ShowWorldMap,()=>g.ShowChapter(9),g.ShowDungeons,()=>g.ShowDungeon(0),g.ShowTower,g.ShowRealmPage,()=>g.ShowTalents(),()=>g.ShowGems(),g.ShowLoadout,g.ShowGrowthHub,()=>g.ShowForge()};
   foreach(var page in pages){page();yield return new WaitForEndOfFrame();var overflow=UnityEngine.Object.FindObjectsOfType<Button>().Where(b=>b.gameObject.activeInHierarchy&&!Fits((RectTransform)b.transform)).Select(b=>b.name).ToArray();Note(g.ExpansionScreen+" offscreen buttons="+string.Join(",",overflow));}
   g.ShowWorldMap();yield return Capture("before-tall-world");
   var encoder=typeof(Editor).Assembly.GetType("UnityEditor.Media.MediaEncoder");Note("Encoder constructors: "+string.Join("; ",encoder.GetConstructors().Select(c=>c.ToString())));
   var track=typeof(Editor).Assembly.GetType("UnityEditor.Media.VideoTrackAttributes");Note("Video fields: "+string.Join("; ",track.GetFields().Select(x=>x.Name+"="+x.FieldType.FullName)));
  }
 }

 [MenuItem("GXDL/QA/UI23 Data Binding")]
 public static void DataBinding()=>Start(BindingRun(),"ui23-data-binding");
 static string TextOf(string name)=>GameObject.Find(name).GetComponent<Text>().text;
 static bool HasText(string text)=>UnityEngine.Object.FindObjectsOfType<Text>().Any(t=>t.gameObject.activeInHierarchy&&t.text==text);
 static void CheckFooter(GameRules r)=>Require(TextOf("Expansion Resource Summary")=="破封符 "+r.Save.peaches+"    灵尘 "+r.Save.coins+"    战力 "+r.Attributes.Power,"page resource/power summary matches Rules");
 static IEnumerator Blocked(string name,GameRules r){
  yield return new WaitForEndOfFrame();var b=Find(name);Require(!b.interactable,"disabled UI "+name);string snapshot=JsonConvert.SerializeObject(r.Save);
  ExecuteEvents.Execute(b.gameObject,new PointerEventData(EventSystem.current),ExecuteEvents.pointerClickHandler);yield return null;
  Require(snapshot==JsonConvert.SerializeObject(r.Save),"disabled input cannot mutate "+name);
 }
 static IEnumerator BindingRun(){
  using(var f=new Fixture()){
   var g=f.game;var r=f.rules;yield return Ready(g);SetSize(750,1334);yield return null;yield return null;
   Fresh(f,201,10);g.ShowForge();int ore=r.Save.forgeOre,dust=r.Save.coins,cost=r.ForgeCost(0),dustCost=r.ForgeDustCost(0);var old=r.Attributes;
   yield return Click("Forge Upgrade");
   var bonus=GameRules.ForgeBonus(1);Require(r.Save.forgeLevels[0]==1&&r.Save.forgeOre==ore-cost&&r.Save.coins==dust-dustCost,"forge exact real cost");
   Require(r.Attributes.attack==old.attack+bonus.attack&&r.Attributes.hp==old.hp+bonus.hp&&r.Attributes.defense==old.defense+bonus.defense,"forge unified bonus applies");
   Require(TextOf("Forge Selection").Contains("1 / "+r.ForgeLimit)&&TextOf("Forge Ore Cost")=="玄铁：消耗 "+r.ForgeCost(0)+" / 拥有 "+r.Save.forgeOre,"forge preview refreshed");CheckFooter(r);
   g.Home();g.ShowForge();CheckFooter(r);Require(TextOf("Forge Slot Level 0").EndsWith("+1"),"forge reentry retained");
   r.Save.coins=0;g.ShowForge();yield return Blocked("Forge Upgrade",r);string state=JsonConvert.SerializeObject(r.Save);Require(!r.Forge(0)&&state==JsonConvert.SerializeObject(r.Save),"forge shortage atomic Rules");
   r.Save.coins=50000;g.ShowTalents(0);int points=r.Save.insight,talentCost=r.TalentCost(0);yield return Click("Talent 0");yield return Click("Talent Train");Require(r.Save.insight==points-talentCost&&HasText("悟道点  "+r.Save.insight),"talent points refresh");
   r.Save.insight=0;g.ShowTalents();yield return Blocked("Talent Train",r);state=JsonConvert.SerializeObject(r.Save);Require(!r.TrainTalent(0)&&state==JsonConvert.SerializeObject(r.Save),"talent shortage atomic");
   r.Save.gemInventory[0]=6;g.ShowGems(0,0,0);yield return Click("Gem Inventory 0");yield return Click("Gem Equip");Require(r.Save.gemSlots[0]==1&&r.Save.gemInventory[0]==5&&TextOf("Gem Stock 0")=="5"&&HasText(GameRules.GemLabel(1)),"gem socket inventory/effect refresh");
   yield return Click("Gem Combine");Require(r.Save.gemInventory[0]==2&&r.Save.gemInventory[1]==1&&TextOf("Gem Stock 1")=="1","gem combine refresh");yield return Click("Gem Remove");Require(r.Save.gemSlots[0]==0&&r.Save.gemInventory[0]==3&&HasText("空孔"),"gem unsocket refresh");
   r.Save.exp=r.ExpNeeded;r.Save.breakthroughStones=0;g.ShowRealmPage();yield return Blocked("突破石不足",r);r.Save.breakthroughStones=r.BreakthroughCost;g.ShowRealmPage();yield return Click("境界突破");Require(HasText(r.Realm.displayName)&&HasText("第 "+r.Save.level+" 重修行")&&HasText("修为  "+r.Save.exp+" / "+r.ExpNeeded),"realm name/level/exp refresh");CheckFooter(r);
   g.ShowLoadout();int equippedSkill=r.Save.activeSkill;yield return Click("Loadout Skill 3");Require(r.Save.activeSkill==equippedSkill,"skill preview spends no state");if(equippedSkill!=3)yield return Click("Loadout Equip");Require(r.Save.activeSkill==3&&GameObject.Find("Loadout Equipped Flag 3")&&!Find("Loadout Equip").interactable,"skill one equipped");g.Home();g.ShowLoadout();Require(GameObject.Find("Loadout Equipped Flag 3"),"skill reentry");
   g.Home();typeof(GxdlGame).GetMethod("TreeUpgrade",Private).Invoke(g,null);dust=r.Save.coins;int treeCost=r.TreeUpgradeCost;yield return Click("Appraisal Upgrade");Require(r.Save.treeLevel==2&&r.Save.coins==dust-treeCost,"appraisal real cost");Require(TextOf("Spirit Stone Balance")=="灵尘 "+r.Save.coins&&HasText("鉴宝台 2 级"),"appraisal modal and underlying HUD refresh together");Require(HasText("升级消耗 "+r.TreeUpgradeCost+" 灵尘   /   拥有 "+r.Save.coins),"appraisal next cost real");g.CloseModal();
   r.Save.coins=0;typeof(GxdlGame).GetMethod("TreeUpgrade",Private).Invoke(g,null);yield return Blocked("Appraisal Upgrade",r);state=JsonConvert.SerializeObject(r.Save);Require(!r.UpgradeTree()&&state==JsonConvert.SerializeObject(r.Save),"appraisal shortage atomic");g.CloseModal();
   Fresh(f,41,20);yield return Click("Unseal");Require(TextOf("Seal Token Balance")==r.Save.peaches.ToString(),"unseal token HUD updates before animation");Require(TextOf("Seal Token Cost")=="破封符 ×1  ·  剩余 "+r.Save.peaches,"unseal cost label updates before animation");g.SkipCurrentUnseal();yield return new WaitForSeconds(3.3f);var pending=r.Save.pending;dust=r.Save.coins;yield return Click("Equip Relic");Require(r.Save.gear[pending.slot]==pending&&TextOf("Spirit Stone Balance")=="灵尘 "+r.Save.coins,"equip and HUD immediately real during animation");yield return new WaitForSeconds(2.3f);
   yield return Click("Unseal");g.SkipCurrentUnseal();yield return new WaitForSeconds(3.3f);yield return Click("Resolve Equipment");Require(TextOf("Spirit Stone Balance")=="灵尘 "+r.Save.coins,"disassemble HUD immediately real during animation");yield return new WaitForSeconds(2.3f);

   Fresh(f,201,20);
   foreach(int band in new[]{0,3}){
    for(int slot=0;slot<12;slot++){
     r.Save.gear[slot].lootBand=band;g.Home();typeof(GxdlGame).GetMethod("ShowTierTable",Private).Invoke(g,new object[]{slot});
     int rowIndex=0;foreach(var row in EquipmentTiers.All){var actual=new Gear{slot=slot,quality=row.quality,level=row.level,lootBand=band};EquipmentTiers.Apply(actual);Require(TextOf("TierTable Value "+slot+" "+rowIndex+" 1")==actual.attack.ToString()&&TextOf("TierTable Value "+slot+" "+rowIndex+" 2")==actual.hp.ToString()&&TextOf("TierTable Value "+slot+" "+rowIndex+" 3")==actual.defense.ToString(),"quality table real slot/band "+slot+"/"+band+"/"+rowIndex);rowIndex++;}g.CloseModal();
    }
   }
   var catalog=GuxuStageConfig.Load();
   for(int kind=0;kind<4;kind++){
    r.Save.dungeonClears[kind]=0;g.ShowDungeon(kind);yield return Click("Difficulty Node 1");Require(GuxuStageRules.Reward(catalog.Dungeon(kind,1),true).ToString().Split(new[]{"  "},StringSplitOptions.RemoveEmptyEntries).All(HasText),"first-clear reward preview "+kind);
    r.Save.dungeonClears[kind]=1;g.ShowDungeon(kind);yield return Click("Difficulty Node 1");Require(GuxuStageRules.Reward(catalog.Dungeon(kind,1),false).ToString().Split(new[]{"  "},StringSplitOptions.RemoveEmptyEntries).All(HasText),"repeat reward preview "+kind);
    for(int tier=1;tier<=10;tier++){var stage=catalog.Dungeon(kind,tier);r.Save.dungeonClears[kind]=tier-1;r.Save.stage=GuxuStageRules.RequiredProgress(stage)-1;Require(!GuxuStageRules.CanEnter(stage,r.Save),"tier progress lower bound "+kind+"/"+tier);r.Save.stage++;Require(GuxuStageRules.CanEnter(stage,r.Save),"tier progress exact unlock "+kind+"/"+tier);}r.Save.stage=201;
   }
   r.Save.talents[0]=3;r.Save.talents[7]=3;var replacement=new Gear{slot=0,quality=2,level=2,lootBand=3};EquipmentTiers.Apply(replacement);string untouched=JsonConvert.SerializeObject(r.Save);var predicted=r.AttributesWithGear(replacement);Require(untouched==JsonConvert.SerializeObject(r.Save),"gear preview never mutates save");r.Save.pending=replacement;r.Resolve(true);Require(predicted.Power==r.Attributes.Power,"preview includes talent and equals actual equipped power");
   Note("360 selected-slot/band/quality table rows; first/replay dungeon rewards; 40 tier unlock boundaries; equipment preview with talents passed.");

   g.Home();typeof(GxdlGame).GetMethod("ShowBag",Private).Invoke(g,null);var originalView=(RectTransform)typeof(GxdlGame).GetField("view",Private).GetValue(g);var originalModal=(RectTransform)typeof(GxdlGame).GetField("modal",Private).GetValue(g);string beforeResize=JsonConvert.SerializeObject(r.Save);
   foreach(int height in new[]{1624,1334}){SetSize(750,height);yield return null;yield return null;Require(originalView==(RectTransform)typeof(GxdlGame).GetField("view",Private).GetValue(g)&&originalModal==(RectTransform)typeof(GxdlGame).GetField("modal",Private).GetValue(g)&&FitsViewport(originalView)&&FitsViewport(originalModal)&&beforeResize==JsonConvert.SerializeObject(r.Save),"viewport resize keeps existing home/modal in bounds without changing player state: "+height);}g.CloseModal();
   Note("Forge/talent/gem/realm/skill/appraisal/equip/disassemble show actual rule state immediately; cross-page reentry and resource-shortage atomicity passed. Unseal cost text updates before animation; live viewport resizing preserves page/modal and save.");yield return Capture("ui23-data-binding-final");
  }
 }

 [MenuItem("GXDL/QA/Iteration Full Runtime")]
 public static void Full()=>Start(FullRun(),"runtime-check");
 [MenuItem("GXDL/QA/Submission Feedback Regression")]
 public static void SubmissionFeedback()=>Start(SubmissionFeedbackRun(),"settlement-regression","Reference/SubmissionFeedback20261117/technical/"+DateTime.UtcNow.ToString("yyyyMMdd-HHmmss"));
 static void SubmissionVictoryBuild(Fixture f,int progress){
  CombatBuild(f,201);f.rules.Save.stage=progress;
  for(int i=0;i<f.rules.Save.firstClears.Length;i++)f.rules.Save.firstClears[i]=i<progress-1;
  // Navigation regression needs deterministic real victories; this is not a balance fixture.
  f.rules.Save.gear[0].attack=1000000;f.rules.Save.gear[0].hp=1000000;f.rules.Save.gear[0].defense=1000000;
  typeof(GxdlGame).GetField("fightSpeed",Private).SetValue(f.game,3);
 }
 static IEnumerator SubmissionFeedbackRun(){
  using(var f=new Fixture()){
   var g=f.game;var r=f.rules;var catalog=GuxuStageConfig.Load();yield return Ready(g);SetSize(750,1334);yield return null;
   foreach(int id in new[]{20,199,200}){
    SubmissionVictoryBuild(f,id);g.ShowChapter((id-1)/20);
    yield return Fight(f,catalog.stages[id-1]);string settled=JsonConvert.SerializeObject(r.Save);
    if(id==200){yield return Click("返回");Require(g.ExpansionScreen=="chapter","final stage returns to operable chapter");}
    else{
     yield return Click("继续挑战");Require(TextOf("Prep Stage")==catalog.stages[id].name,"next preparation is next stage");
     yield return Click("Prep Close");Require(g.ExpansionScreen=="chapter","closing continuation restores chapter after "+id);
     yield return Click("Stage "+(id+1));yield return Click("Chapter Challenge");yield return Click("Prep Close");
     Require(g.ExpansionScreen=="chapter","reopening and cancelling remains operable after "+id);
    }
    Require(JsonConvert.SerializeObject(r.Save)==settled,"continuation/cancel neither repays nor mutates reward after "+id);yield return Capture("main-"+id+"-returned");
   }
   SubmissionVictoryBuild(f,201);r.Save.dungeonClears[0]=0;g.ShowDungeon(0);
   yield return Fight(f,catalog.Dungeon(0,1));string dungeon=JsonConvert.SerializeObject(r.Save);yield return Click("继续挑战");yield return Click("Prep Close");
   Require(g.ExpansionScreen=="dungeon","dungeon continuation close restores dungeon");Require(JsonConvert.SerializeObject(r.Save)==dungeon&&r.Save.dungeonClaims[0]==1,"dungeon cancel spends no extra daily claim");
   SubmissionVictoryBuild(f,201);r.Save.towerFloor=1;g.ShowTower();
   yield return Fight(f,catalog.tower[0]);string tower=JsonConvert.SerializeObject(r.Save);yield return Click("继续挑战");yield return Click("Prep Close");
   Require(g.ExpansionScreen=="tower"&&JsonConvert.SerializeObject(r.Save)==tower,"tower continuation close restores tower with no repeated reward");
   Fresh(f,201);typeof(GxdlGame).GetField("fightSpeed",Private).SetValue(g,3);r.RefreshDaily(DateTime.UtcNow);g.ShowChapter(9);string defeat=JsonConvert.SerializeObject(r.Save);
   yield return Fight(f,catalog.stages[199],false);yield return Click("再次挑战");yield return Click("Prep Close");
   Require(g.ExpansionScreen=="chapter"&&JsonConvert.SerializeObject(r.Save)==defeat,"defeat retry cancellation restores chapter without changing progress");
   Note("Actual battle settlement -> pointer continuation/retry -> close -> usable origin, chapter boundary, final stage, dungeon daily claim, tower and defeat checked.");
  }
 }
 static void Fresh(Fixture f,int stage=1,int level=1){f.game.Home();f.rules.Save=new GameRules(f.rules.Config,new SaveData{stage=stage,level=level,equipmentSchema=1,coins=50000,peaches=100,forgeOre=3000,insight=1000,breakthroughStones=100},42).Save;f.rules.EnsureExpansion();f.game.Home();}
 static void CombatBuild(Fixture f,int stage,int branch=0){
  Fresh(f,stage,Mathf.Clamp(stage/2,1,100));var r=f.rules;
  for(int i=0;i<12;i++){var gear=r.Save.gear[i];gear.quality=Mathf.Clamp(1+stage/35,1,5);gear.level=3;gear.lootBand=Mathf.Clamp((stage-1)/20,0,9);EquipmentTiers.Apply(gear);r.Save.forgeLevels[i]=stage>=6?Math.Min(20,stage/10):0;if(stage>=21)r.Save.gemSlots[i*2]=(i%3)*6+Math.Min(5,1+stage/50);if(stage>=41)r.Save.gemSlots[i*2+1]=(i%3)*6+Math.Min(5,1+stage/50);}
  for(int n=0;n<8;n++)r.Save.talents[branch*8+n]=stage>=11?Math.Min(3,stage/40+1):0;r.Save.activeSkill=branch;r.Save.autoSkill=true;
 }
 static IEnumerator Fight(Fixture f,GuxuStage stage,bool expectWin=true){
  f.game.ShowBattlePrep(stage);yield return Click("Ruins Challenge");var b=f.game.ActiveBattle;Require(b!=null,"real battle started "+stage.id);
  float deadline=Time.realtimeSinceStartup+100;while(f.game.ActiveBattle!=null&&Time.realtimeSinceStartup<deadline)yield return null;
  Require(f.game.ActiveBattle==null,"battle completes "+stage.id);Require(b.Won==expectWin,"battle result "+stage.id);Require(b.Settled==expectWin,"battle settlement "+stage.id);Note("Battle "+stage.name+": "+(b.Won?"WIN":"DEFEAT")+", rounds="+b.Round+", wave="+b.Wave);yield return null;
 }
 static IEnumerator FullRun(){
  using(var f=new Fixture()){
   var g=f.game;var r=f.rules;var catalog=GuxuStageConfig.Load();yield return Ready(g);SetSize(750,1334);yield return null;yield return null;
   Fresh(f);g.ShowWorldMap();yield return new WaitForEndOfFrame();Require(Find("Chapter 1").interactable&&!Find("Chapter 2").interactable,"new player chapter locks");
   g.ShowDungeons();for(int k=0;k<4;k++)Require(!Find("Dungeon "+k).interactable,"new player dungeon lock "+k);g.ShowForge();Require(!Find("Forge Upgrade").interactable,"new player forge lock");g.ShowTalents();Require(!Find("Talent Train").interactable,"new player talent lock");g.ShowGems();Require(!Find("Gem Equip").interactable,"new player gem lock");g.ShowTower();Require(!Find("Tower Challenge").interactable,"tower lock");Note("New-player unlock gates passed.");
   g.Home();int seals=r.Save.peaches;yield return Click("Unseal");g.Chop();g.SkipCurrentUnseal();yield return new WaitForSeconds(3.3f);Require(r.Save.pending!=null&&r.Save.peaches==seals-1,"unseal double input spends once");var drop=r.Save.pending;yield return Click("Equip Relic");yield return new WaitForSeconds(2.3f);Require(r.Save.gear[drop.slot]==drop&&r.Save.pending==null,"explicit equip");
   yield return Click("Unseal");g.SkipCurrentUnseal();yield return new WaitForSeconds(3.3f);int dust=r.Save.coins,reward=r.Save.pending.quality*20;yield return Click("Resolve Equipment");g.ResolveDrop(false);yield return new WaitForSeconds(2.3f);Require(r.Save.coins==dust+reward&&r.Save.pending==null,"disassembly pays once");
   r.Save.keepQuality=1;g.Home();yield return Click("自动破封");yield return new WaitForSeconds(5);Require(r.Save.pending!=null,"auto unseal pauses for retained drop");g.ResolveDrop(false);yield return new WaitForSeconds(2.3f);Note("Unseal / equip / disassemble / retention passed.");
   Fresh(f,41,10);r.Save.exp=r.ExpNeeded;r.Save.breakthroughStones=0;g.ShowRealmPage();Require(!Find("突破石不足").interactable,"realm shortage button disabled");Require(r.Save.level==10&&r.Save.exp==r.ExpNeeded,"realm shortage atomic");r.Save.breakthroughStones=2;g.ShowRealmPage();yield return Click("境界突破");Require(r.Save.level==11&&r.Save.exp==0&&r.Save.breakthroughStones==0,"major realm consumes correct resources");
   g.ShowForge();for(int i=0;i<12;i++){yield return Click("Forge Slot "+i);int power=r.Attributes.Power;yield return Click("Forge Upgrade");Require(r.Save.forgeLevels[i]==1&&r.Attributes.Power==power+110,"forge real slot "+i);}
   int insight=r.Save.insight;for(int branch=0;branch<3;branch++){g.ShowTalents(branch);for(int n=0;n<8;n++){yield return Click("Talent "+(branch*8+n));yield return Click("Talent Train");}}Require(r.Save.talents.All(x=>x==1),"all 24 nodes trained");yield return Click("Talent Reset");yield return Click("Talent Reset Confirm");Require(r.Save.insight==insight&&r.Save.talents.All(x=>x==0),"talent reset full refund");
   for(int c=0;c<4;c++){r.Save.gemInventory[c*6]=9;g.ShowGems(c,0,c);yield return Click("Gem Inventory "+(c*6));yield return Click("Gem Equip");Require(r.Save.gemSlots[c*2]==c*6+1,"socket color "+c);yield return Click("Gem Combine");Require(r.Save.gemInventory[c*6+1]==1,"combine color "+c);yield return Click("Gem Inventory "+(c*6+1));yield return Click("Gem Equip");Require(r.Save.gemSlots[c*2]==c*6+2&&r.Save.gemInventory[c*6]==6,"replace returns gem "+c);yield return Click("Gem Remove");Require(r.Save.gemSlots[c*2]==0&&r.Save.gemInventory[c*6+1]==1,"remove color "+c);}
   g.ShowGems(0,1,0);yield return Click("Gem Inventory 1");yield return Click("Gem Equip");Require(r.Save.gemSlots[1]==2,"second socket UI");Note("Four growth groups / 12 slots / 24 talents / 4 gem colors passed.");
   Fresh(f,201,50);for(int c=0;c<10;c++){g.ShowWorldMap();yield return Click("Chapter "+(c+1));Require(UnityEngine.Object.FindObjectsOfType<Button>().Count(b=>b.name.StartsWith("Stage ")&&b.gameObject.activeInHierarchy)==20,"chapter has 20 stages "+c);yield return Click("领取章节宝匣");Require(r.Save.chapterChests[c]&&!Find("已领取").interactable,"chapter chest once "+c);yield return Click("Stage "+(c*20+20));yield return Click("Chapter Challenge");Require(GameObject.Find("Prep Stage").GetComponent<Text>().text==catalog.stages[c*20+19].name,"chapter prep route "+c);yield return Click("Prep Close");}Note("10 chapters / 200 visible stages / 10 unique chests passed.");
   g.ShowBattlePrep(catalog.stages[199]);for(int s=0;s<4;s++){yield return Click("Skill Choice "+s);Require(r.Save.activeSkill==s,"skill choice "+s);}yield return Click("Prep Auto Skill");Require(!r.Save.autoSkill,"manual mode stored");yield return Click("Prep Auto Skill");g.CloseModal();
   typeof(GxdlGame).GetField("fightSpeed",Private).SetValue(g,4);
   for(int chapter=0;chapter<10;chapter++){int id=(chapter+1)*20;CombatBuild(f,id);g.ShowChapter(chapter);yield return Fight(f,catalog.stages[id-1]);Require(r.Save.stage==id+1&&r.Save.firstClears[id-1],"main progress advanced "+id);if(id==200){Require(!Find("主线已通关").interactable,"completed main no endless next");yield return Capture("main-200-victory");}g.CloseModal();}
   for(int k=0;k<4;k++){
    CombatBuild(f,201);r.Save.dungeonClears[k]=9;g.ShowDungeon(k);yield return Fight(f,catalog.Dungeon(k,10));Require(r.Save.dungeonClears[k]==10&&r.Save.dungeonClaims[k]==1,"dungeon high tier first clear "+k);g.CloseModal();g.ShowDungeon(k);yield return Click("Difficulty Node 10");yield return Click("Difficulty Sweep");yield return Click("Difficulty Sweep");Require(r.Save.dungeonClaims[k]==3&&!Find("Difficulty Sweep").interactable,"daily cap through sweep UI "+k);yield return Capture("dungeon-"+k+"-daily-cap");
   }
   CombatBuild(f,201);r.Save.towerFloor=100;g.ShowTower();yield return Fight(f,catalog.tower[99]);Require(r.Save.towerFloor==101&&!Find("百层已通关").interactable,"tower completes at 100");g.CloseModal();g.ShowTower();int ore=r.Save.forgeOre;yield return Fight(f,catalog.tower[99]);Require(r.Save.forgeOre==ore,"tower replay does not pay twice");g.CloseModal();
   Fresh(f,201);r.RefreshDaily(DateTime.UtcNow);g.ShowChapter(9);string before=JsonConvert.SerializeObject(r.Save);yield return Fight(f,catalog.stages[199],false);Require(before==JsonConvert.SerializeObject(r.Save),"defeat changes no progression/resources");g.CloseModal();
   CombatBuild(f,100);r.Save.autoSkill=false;g.ShowBattlePrep(catalog.stages[99]);yield return Click("Ruins Challenge");yield return Click("Cast Skill");Require(g.ActiveBattle.SkillQueued||g.ActiveBattle.SkillCooldown>0,"manual skill queued");yield return Click("Auto Skill");Require(g.ActiveBattle.AutoSkill&&!Find("Cast Skill").interactable&&Find("Auto Skill").GetComponentInChildren<Text>().text.Contains("开"),"auto controls update immediately");int progress=r.Save.stage;yield return Click("退出挑战");yield return new WaitForSeconds(1);Require(g.ActiveBattle==null&&r.Save.stage==progress,"cancel cannot settle later");Note("4 resource dungeons, sweep caps, tower 100/replay, defeat, manual skills and cancellation passed.");
   CombatBuild(f,201);foreach(int height in new[]{1334,1624}){
    SetSize(750,height);yield return null;yield return null;
    Action[] pages={g.Home,g.ShowWorldMap,()=>g.ShowChapter(9),g.ShowDungeons,()=>g.ShowDungeon(0),g.ShowTower,g.ShowRealmPage,()=>g.ShowTalents(),()=>g.ShowGems(),g.ShowLoadout,g.ShowGrowthHub,()=>g.ShowForge()};
    foreach(var page in pages){page();yield return new WaitForEndOfFrame();foreach(var button in UnityEngine.Object.FindObjectsOfType<Button>().Where(b=>b.gameObject.activeInHierarchy))Require(Fits((RectTransform)button.transform),"visible button "+button.name+" on "+g.ExpansionScreen+" at "+height);}
    g.ShowWorldMap();yield return Capture("world-"+height);yield return Click("Chapter 10");yield return Click("Stage 200");yield return Click("Chapter Challenge");Require(Fits((RectTransform)GameObject.Find("Battle Preparation").transform),"fitted battle prep");yield return Click("Ruins Challenge");Require(Fits((RectTransform)Find("Battle Speed").transform),"battle speed visible");yield return Click("退出挑战");
    foreach(string method in new[]{"ShowBag","AutoSettings","TreeUpgrade","ShowCodex","ShowFinalResidence","ShowFinalCompanion"}){g.Home();typeof(GxdlGame).GetMethod(method,Private).Invoke(g,null);yield return new WaitForSeconds(.25f);Require(GameObject.Find("Modal")!=null,"panel opens "+method);foreach(var b in UnityEngine.Object.FindObjectsOfType<Button>().Where(b=>b.gameObject.activeInHierarchy&&b.transform.IsChildOf(GameObject.Find("Modal").transform)))Require(Fits((RectTransform)b.transform),"modal button visible "+method);g.CloseModal();}
    g.Home();g.ReplayOpening();yield return new WaitForSeconds(.3f);Require(g.IsStoryPlaying&&Fits((RectTransform)Find("Skip Story").transform),"opening story skip fits");yield return Click("Skip Story");yield return Ready(g);
   }
   Note("12 screens + modal panels + battle + story passed at 750x1334 and 750x1624.");
   var saved=JsonConvert.DeserializeObject<SaveData>(File.ReadAllText(Output+"/runtime-fixture.json"));GameRules.ValidateSave(saved);Note("Isolated save reload validated. Original player state restored by fixture.");
  }
 }
 [MenuItem("GXDL/QA/Iteration Record Review")]
 public static void RecordReview()=>Start(Review(),"video-check");
 [MenuItem("GXDL/QA/Iteration Presentation")]
 public static void Presentation()=>Start(PresentationRun(),"presentation-check");
 static IEnumerator PresentationRun(){
  using(var f=new Fixture()){
   yield return Ready(f.game);var g=f.game;
   foreach(int progress in new[]{1,21,201}){Fresh(f,progress);yield return null;var labels=GameObject.Find("Main Quest").GetComponentsInChildren<Text>();Require(labels.Any(t=>t.text.Contains(progress>200?"十域主线已通关":"第 "+progress+" / 200 关")),"main quest live progress "+progress);yield return Click("›");Require(g.ExpansionScreen=="world","main quest routes to world");}
   foreach(int height in new[]{1334,1624}){
    SetSize(750,height);yield return null;yield return null;Fresh(f,41,10);
    var gear=JsonConvert.DeserializeObject<Gear>(JsonConvert.SerializeObject(f.rules.Save.gear[0]));gear.attack+=20;f.rules.Save.pending=gear;g.Home();yield return Click("Equip Relic");yield return new WaitForSeconds(.1f);
    Require(Fits((RectTransform)GameObject.Find("Status Toast").transform),"toast panel fits "+height);var toast=GameObject.Find("Status Toast").GetComponentInChildren<Text>();Require(toast&&Fits(toast.rectTransform),"equip message remains inside toast "+height);
    var seal=GameObject.Find("Sealed Stone Layer").GetComponent<RectTransform>();var camera=seal.GetComponentInParent<Canvas>().worldCamera;var origin=RectTransformUtility.WorldToScreenPoint(camera,seal.TransformPoint(seal.rect.center));
    var stones=UnityEngine.Object.FindObjectsOfType<Image>().Where(x=>x.name=="Spirit Stone Drop").ToArray();Require(stones.Length==10,"10 resource particles");foreach(var stone in stones){var pos=RectTransformUtility.WorldToScreenPoint(camera,stone.transform.position);Require(Vector2.Distance(pos,origin)<220,"resource particles originate at seal, preserve top-left coordinates");}
    yield return Capture("equip-feedback-"+height);yield return new WaitForSeconds(2.5f);
    Action[] pages={g.Home,g.ShowWorldMap,()=>g.ShowChapter(2),g.ShowDungeons,()=>g.ShowDungeon(0),g.ShowTower,g.ShowRealmPage,()=>g.ShowTalents(),()=>g.ShowGems(),g.ShowLoadout,g.ShowGrowthHub,()=>g.ShowForge()};
    foreach(var page in pages){page();yield return new WaitForEndOfFrame();foreach(var b in UnityEngine.Object.FindObjectsOfType<Button>().Where(b=>b.gameObject.activeInHierarchy))Require(Fits((RectTransform)b.transform),"button in frame "+g.ExpansionScreen+" / "+b.name);}
    g.ShowWorldMap();yield return Capture("world-"+height);Require(Find("Chapter 2").image.canvasRenderer.GetColor().a<.01f,"map hit areas do not flash opaque on entry");yield return Click("Chapter 2");yield return Click("Stage 40");yield return Click("Chapter Challenge");yield return Click("Skill Choice 1");yield return Click("Ruins Challenge");yield return new WaitForSeconds(.7f);yield return Capture("battle-"+height);yield return Click("退出挑战");
    g.Home();g.ReplayOpening();yield return new WaitForSeconds(.2f);Require(Fits((RectTransform)Find("Skip Story").transform),"story skip fits");yield return Click("Skip Story");yield return Ready(g);
    Note("Visual feedback, resource particles, 12 pages, battle and story at 750x"+height+" passed.");
   }
  }
 }
 static IEnumerator Review(){
  using(var f=new Fixture()){
   yield return Ready(f.game);SetSize(750,1334);yield return null;yield return null;
   var recorder=new GameObject("Iteration Recorder").AddComponent<GuxuIterationRecorder>();
   try{
    recorder.Begin(Path.GetFullPath(Output+"/古墟大陆-系统与战斗验收.mp4"));
    var g=f.game;var r=f.rules;var catalog=GuxuStageConfig.Load();
    Fresh(f,41,10);recorder.Caption("隔离测试存档 · 破封 / 装备 / 四组养成");g.Home();yield return new WaitForSeconds(2);yield return Click("Unseal");yield return new WaitForSeconds(4);yield return Click("Equip Relic");yield return new WaitForSeconds(2.5f);
    yield return Click("修炼");yield return new WaitForSeconds(2);yield return Click("Growth Forge");yield return Click("Forge Slot 0");yield return Click("Forge Upgrade");yield return new WaitForSeconds(2);yield return Click("返回修行");yield return Click("Growth Realm");r.Save.exp=r.ExpNeeded;g.ShowRealmPage();yield return new WaitForSeconds(1);yield return Click("境界突破");yield return new WaitForSeconds(2);
    g.ShowGrowthHub();yield return Click("Growth Talents");yield return Click("Talent 0");yield return Click("Talent Train");yield return Click("Talent 1");yield return Click("Talent Train");yield return new WaitForSeconds(2);g.ShowGrowthHub();yield return Click("Growth Gems");r.Save.gemInventory[0]=6;g.ShowGems(0,0,0);yield return Click("Gem Inventory 0");yield return Click("Gem Equip");yield return Click("Gem Combine");yield return new WaitForSeconds(2);
    recorder.Caption("隔离测试存档 · 十域主线 / 章节解锁 / Boss");CombatBuild(f,40);g.ShowWorldMap();yield return new WaitForSeconds(3);yield return Click("Chapter 2");yield return new WaitForSeconds(2);yield return Click("Stage 40");yield return Click("Chapter Challenge");yield return new WaitForSeconds(3);typeof(GxdlGame).GetField("fightSpeed",Private).SetValue(g,2);yield return Fight(f,catalog.stages[39]);yield return new WaitForSeconds(3);yield return Click("返回");yield return Click("领取章节宝匣");yield return new WaitForSeconds(2);
    string[] labels={"淬兵遗窟 · 玄铁 / 锻造","灵玉秘境 · 三波 / 镶嵌","悟道幻境 · 悟道点 / 天赋","聚灵洞天 · 12回合 / 境界"};
    for(int k=0;k<4;k++){
     CombatBuild(f,GuxuStageRules.DungeonUnlocks[k]+1);recorder.Caption("隔离测试存档 · "+labels[k]);g.ShowDungeons();yield return new WaitForSeconds(2);yield return Click("Dungeon "+k);yield return new WaitForSeconds(1);yield return Fight(f,catalog.Dungeon(k,1));yield return new WaitForSeconds(2);yield return Click("返回");yield return Click("Difficulty Node 1");yield return Click("Difficulty Sweep");yield return new WaitForSeconds(2);
    }
    recorder.Caption("隔离测试存档 · 手动护盾 / 切换自动 / 中途退出");CombatBuild(f,100,1);r.Save.autoSkill=false;g.ShowBattlePrep(catalog.stages[99]);yield return new WaitForSeconds(2);yield return Click("Ruins Challenge");yield return Click("Cast Skill");yield return new WaitForSeconds(6);yield return Click("Auto Skill");yield return new WaitForSeconds(1);yield return Click("退出挑战");yield return new WaitForSeconds(2);
    recorder.Caption("隔离测试存档 · 通天古塔第100层 / 最终结算");CombatBuild(f,200);r.Save.towerFloor=100;g.ShowTower();yield return new WaitForSeconds(2);yield return Fight(f,catalog.tower[99]);yield return new WaitForSeconds(3);g.CloseModal();g.ShowTower();yield return new WaitForSeconds(2);
    recorder.Caption("隔离测试存档 · 主线第200关 / 通关状态");CombatBuild(f,200);g.ShowWorldMap();yield return new WaitForSeconds(2);g.ShowChapter(9);yield return Fight(f,catalog.stages[199]);yield return new WaitForSeconds(4);yield return Click("返回");yield return Click("领取章节宝匣");g.ShowWorldMap();yield return new WaitForSeconds(3);
    Require(recorder.Frames>300&&recorder.Error==null,"encoded continuous runtime frames");Note("Recorded "+recorder.Frames+" frames at 30 fps, 750x1334. No audio track. Test fixture, no player save changes.");
   }finally{recorder.Finish();UnityEngine.Object.Destroy(recorder.gameObject);}
  }
 }
}

public sealed class GuxuIterationRecorder:MonoBehaviour {
 MediaEncoder encoder;int captureRate,targetRate;Text caption;GameObject overlay;string output;
 readonly List<object> chapters=new List<object>();
 public int Frames{get;private set;}public string Error{get;private set;}
 public void Begin(string path){
  output=path;captureRate=Time.captureFramerate;targetRate=Application.targetFrameRate;Time.captureFramerate=30;Application.targetFrameRate=30;
  var video=new VideoTrackAttributes{frameRate=new MediaRational(30),width=(uint)Screen.width,height=(uint)Screen.height,includeAlpha=false,bitRateMode=VideoBitrateMode.High};encoder=new MediaEncoder(path,video);
  overlay=new GameObject("QA Recording Label",typeof(Canvas),typeof(CanvasScaler));var canvas=overlay.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=30000;var scaler=overlay.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(750,1334);
  var panel=new GameObject("QA Label",typeof(RectTransform),typeof(Image));var rect=(RectTransform)panel.transform;rect.SetParent(overlay.transform,false);rect.anchorMin=new Vector2(0,1);rect.anchorMax=new Vector2(1,1);rect.pivot=new Vector2(.5f,1);rect.sizeDelta=new Vector2(0,29);rect.anchoredPosition=Vector2.zero;var image=panel.GetComponent<Image>();image.color=new Color(0,.03f,.06f,.85f);image.raycastTarget=false;
  var label=new GameObject("Caption",typeof(RectTransform),typeof(Text));var textRect=(RectTransform)label.transform;textRect.SetParent(rect,false);textRect.anchorMin=Vector2.zero;textRect.anchorMax=Vector2.one;textRect.offsetMin=textRect.offsetMax=Vector2.zero;caption=label.GetComponent<Text>();caption.font=Resources.Load<Font>("Original/GameFont");caption.fontSize=20;caption.alignment=TextAnchor.MiddleCenter;caption.color=Color.white;caption.raycastTarget=false;StartCoroutine(Capture());
 }
 public void Caption(string label){caption.text=label;chapters.Add(new{seconds=Frames/30.0,label});}
 IEnumerator Capture(){while(encoder!=null){yield return new WaitForEndOfFrame();if(encoder==null)yield break;Texture2D frame=null;try{frame=ScreenCapture.CaptureScreenshotAsTexture();if(!encoder.AddFrame(frame))throw new Exception("Encoder rejected frame");Frames++;}catch(Exception e){Error=e.ToString();Debug.LogException(e);yield break;}finally{if(frame)Destroy(frame);}}}
 public void Finish(){StopAllCoroutines();encoder?.Dispose();encoder=null;Time.captureFramerate=captureRate;Application.targetFrameRate=targetRate;if(overlay)Destroy(overlay);if(output!=null)File.WriteAllText(Path.ChangeExtension(output,"chapters.json"),JsonConvert.SerializeObject(new{fps=30,width=750,height=1334,frames=Frames,seconds=Frames/30.0,error=Error,chapters},Formatting.Indented));}
 void OnDestroy(){if(encoder!=null)Finish();}
}





