using System;
using System.Collections;
using System.IO;
using System.Linq;
using Gxdl;
using Newtonsoft.Json;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.UI;

public static class GuxuUI23Check {
 static bool running; static bool tall; static bool forgeOnly; static int priorSize;
 static string Output="Reference/UIRedesign20261110/before";
 [MenuItem("GXDL/QA/UI23 Capture Before")]
 public static void Before(){Output="Reference/UIRedesign20261110/before";Run();}
 [MenuItem("GXDL/QA/UI23 Capture After")]
 public static void After(){forgeOnly=false;tall=false;Output="Reference/UIRedesign20261110/after";Run();}
 [MenuItem("GXDL/QA/UI23 Capture Tall")] public static void Tall(){forgeOnly=false;tall=true;Output="Reference/UIRedesign20261110/after-tall";Run();}
 [MenuItem("GXDL/QA/UI23 Forge R02")] public static void ForgeR02(){forgeOnly=true;tall=false;Output="Reference/UIRedesign20261110/candidate-r02";Run();}
 [MenuItem("GXDL/QA/UI23 Working R02")] public static void WorkingR02(){forgeOnly=false;tall=false;Output="Reference/UIRedesign20261110/working-r02";Run();}
 [MenuItem("GXDL/QA/UI23 Final R02")] public static void FinalR02(){forgeOnly=false;tall=false;Output="Reference/UIRedesign20261110/final-r02";Run();}
 [MenuItem("GXDL/QA/UI23 Final Tall R02")] public static void FinalTallR02(){forgeOnly=false;tall=true;Output="Reference/UIRedesign20261110/final-r02-tall";Run();}
 [MenuItem("GXDL/QA/UI23 Accepted R02")] public static void AcceptedR02(){forgeOnly=false;tall=false;Output="Reference/UIRedesign20261110/accepted-r02";Run();}
 [MenuItem("GXDL/QA/UI23 Accepted Tall R02")] public static void AcceptedTallR02(){forgeOnly=false;tall=true;Output="Reference/UIRedesign20261110/accepted-r02-tall";Run();}
 [MenuItem("GXDL/QA/UI23 Login Before")]
 public static void LoginBefore(){Output="Reference/UIRedesign20261110/before";Login();}
 [MenuItem("GXDL/QA/UI23 Login After")]
 public static void LoginAfter(){Output="Reference/UIRedesign20261110/after";Login();}
 static void Login(){EditorWindow.GetWindow(typeof(Editor).Assembly.GetType("UnityEditor.GameView")).Focus();Directory.CreateDirectory(Output+"/Screenshots");GxdlGame.Instance.StartCoroutine(Capture("27-login"));}
 public static void Run(){if(running)throw new Exception("Expansion QA already running");var g=GxdlGame.Instance;if(!Application.isPlaying||!g)throw new Exception("Enter Play mode first");running=true;Directory.CreateDirectory(Output+"/Screenshots");Directory.CreateDirectory(Output+"/QA");File.WriteAllText(Output+"/ui-check.txt","RUNNING");
  var window=EditorWindow.GetWindow(typeof(Editor).Assembly.GetType("UnityEditor.GameView"));priorSize=(int)window.GetType().GetProperty("selectedSizeIndex",System.Reflection.BindingFlags.Public|System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).GetValue(window);
  typeof(GuxuBattlePreparationCheck).GetMethod("SetSize",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Static).Invoke(null,new object[]{750,tall?1624:1334});EditorWindow.GetWindow(typeof(Editor).Assembly.GetType("UnityEditor.GameView")).Focus();g.StartCoroutine(Guard(Check(g)));
 }
 static IEnumerator Guard(IEnumerator inner){var stack=new System.Collections.Generic.Stack<IEnumerator>();stack.Push(inner);try{while(stack.Count>0){object current=null;bool moved;try{moved=stack.Peek().MoveNext();if(moved)current=stack.Peek().Current;}catch(Exception e){File.WriteAllText(Output+"/ui-check.txt","FAIL: "+e);Debug.LogException(e);break;}if(!moved){(stack.Pop() as IDisposable)?.Dispose();continue;}if(current is IEnumerator child){stack.Push(child);continue;}yield return current;}}finally{while(stack.Count>0)(stack.Pop() as IDisposable)?.Dispose();running=false;}}
 static void Require(bool condition,string message){if(!condition)throw new Exception("Expansion UI: "+message);}
 static Button Button(string name)=>UnityEngine.Object.FindObjectsOfType<Button>().First(x=>x.name==name&&x.gameObject.activeInHierarchy);
 static IEnumerator Click(string name){
 yield return new WaitForEndOfFrame();
 var b=Button(name);Require(b.interactable,"button enabled: "+name);Canvas.ForceUpdateCanvases();var rect=(RectTransform)b.transform;
 var pointer=new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current){position=RectTransformUtility.WorldToScreenPoint(b.GetComponentInParent<Canvas>().worldCamera,rect.TransformPoint(rect.rect.center)),button=UnityEngine.EventSystems.PointerEventData.InputButton.Left};
 var hits=new System.Collections.Generic.List<UnityEngine.EventSystems.RaycastResult>();UnityEngine.EventSystems.EventSystem.current.RaycastAll(pointer,hits);
 Require(hits.Count>0&&UnityEngine.EventSystems.ExecuteEvents.GetEventHandler<UnityEngine.EventSystems.IPointerClickHandler>(hits[0].gameObject)==b.gameObject,"pointer unobstructed: "+name);
 UnityEngine.EventSystems.ExecuteEvents.Execute(b.gameObject,pointer,UnityEngine.EventSystems.ExecuteEvents.pointerClickHandler);
}
 static void Open(GxdlGame game,string method,params object[] args){typeof(GxdlGame).GetMethod(method,System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(game,args);}
 static IEnumerator Capture(string name){yield return new WaitForSeconds(.3f);var toast=GameObject.Find("Status Toast");if(toast)toast.SetActive(false);yield return new WaitForEndOfFrame();var image=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes(Output+"/Screenshots/"+name+".png",image.EncodeToPNG());UnityEngine.Object.Destroy(image);
 var images=UnityEngine.Object.FindObjectsOfType<Image>().Where(i=>i.gameObject.activeInHierarchy&&i.sprite&&i.sprite.name.Contains("ui23-")).Select(i=>new {name=i.name,sprite=i.sprite.name,texture=new[]{i.sprite.texture.width,i.sprite.texture.height},border=new[]{i.sprite.border.x,i.sprite.border.y,i.sprite.border.z,i.sprite.border.w},i.sprite.pixelsPerUnit,i.pixelsPerUnitMultiplier,displayRect=new[]{i.rectTransform.rect.width,i.rectTransform.rect.height},clickRect=i.GetComponent<Button>()?new[]{i.rectTransform.rect.width,i.rectTransform.rect.height}:null,interactable=i.GetComponent<Button>()&&i.GetComponent<Button>().interactable}).ToArray();
 var texts=UnityEngine.Object.FindObjectsOfType<Text>().Where(t=>t.gameObject.activeInHierarchy).Select(t=>new{name=t.name,value=t.text}).ToArray();
 Directory.CreateDirectory(Output+"/QA");File.WriteAllText(Output+"/QA/"+name+"-bindings.json",JsonConvert.SerializeObject(new{checkedAt=DateTime.UtcNow.ToString("O"),viewport=new[]{Screen.width,Screen.height},textures=images,texts},Formatting.Indented));
}
 static IEnumerator WaitBattle(GxdlGame g){float deadline=Time.realtimeSinceStartup+90;while(g.ActiveBattle!=null&&Time.realtimeSinceStartup<deadline)yield return null;Require(g.ActiveBattle==null,"battle completes within 90 seconds");}
 static IEnumerator Check(GxdlGame g){
  var original=g.Rules;var savePath=GameRules.SavePath;var originalBytes=File.Exists(savePath)?File.ReadAllBytes(savePath):null;
  var fixture=new GameRules(original.Config,new SaveData{equipmentSchema=1},42);
  var fixturePath=Path.GetFullPath(Output+"/QA/fixture.json");
  typeof(GameRules).GetField("storagePath",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).SetValue(fixture,fixturePath);
  typeof(GxdlGame).GetProperty("Rules").SetValue(g,fixture);
  var gameView=EditorWindow.GetWindow(typeof(Editor).Assembly.GetType("UnityEditor.GameView"));
  var sizeProperty=gameView.GetType().GetProperty("selectedSizeIndex",System.Reflection.BindingFlags.Public|System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance);int previousSize=priorSize;
  string backup=JsonConvert.SerializeObject(original.Save);File.WriteAllText(Output+"/QA/player-before-ui-test.json",backup);int opening=PlayerPrefs.GetInt(GxdlGame.OpeningStoryKey,-1),first=PlayerPrefs.GetInt(GxdlGame.FirstUnsealStoryKey,-1);
  try {
   PlayerPrefs.SetInt(GxdlGame.OpeningStoryKey,1);PlayerPrefs.SetInt(GxdlGame.FirstUnsealStoryKey,1);g.SkipStory();g.Home();
   g.Rules.Save=new GameRules(g.Rules.Config,new SaveData{equipmentSchema=1,stage=201,level=60,coins=50000,forgeOre=3000,insight=1000,breakthroughStones=100}).Save;g.Rules.EnsureExpansion();g.Home();yield return null;
   g.ShowWorldMap();Require(UnityEngine.Object.FindObjectsOfType<Button>().Count(b=>b.name.StartsWith("Chapter "))==10,"ten chapter buttons");yield return Capture("01-world-map");
   yield return Click("Chapter 10");Require(UnityEngine.Object.FindObjectsOfType<Button>().Count(b=>b.name.StartsWith("Stage "))==20,"twenty selectable chapter stages");yield return Capture("02-chapter-200");yield return Click("Stage 200");yield return Click("Chapter Challenge");yield return Capture("03-battle-preparation");g.CloseModal();
   g.ShowGrowthHub();yield return Capture("04-growth-hub");g.ShowForge();int forge=g.Rules.Save.forgeLevels[0];yield return Capture("05-forging-before");File.WriteAllText(Output+"/QA/05-before-state.json",JsonConvert.SerializeObject(new{player=g.Rules.Save,attributes=g.Rules.Attributes},Formatting.Indented));yield return Click("Forge Upgrade");Require(g.Rules.Save.forgeLevels[0]==forge+1,"forge UI changes real level");yield return Capture("05-forging");File.WriteAllText(Output+"/QA/05-after-state.json",JsonConvert.SerializeObject(new{player=g.Rules.Save,attributes=g.Rules.Attributes},Formatting.Indented));if(forgeOnly){File.WriteAllText(Output+"/ui-check.txt",DateTime.UtcNow.ToString("O")+" PASS: 05 real pointer forge upgrade; isolated save; candidate r02 composition.");yield break;}
   g.ShowTalents(0);yield return Click("Talent 0");yield return Click("Talent Train");Require(g.Rules.Save.talents[0]==1,"talent button spends real resources");yield return Capture("06-talents");
   g.Rules.Save.gemInventory[0]=6;g.ShowGems(0,0,0);yield return Click("Gem Inventory 0");UnityEngine.Object.FindObjectsOfType<Button>().First(b=>b.name=="Gem Equip"&&b.interactable).onClick.Invoke();Require(g.Rules.Save.gemSlots[0]==1&&g.Rules.Save.gemInventory[0]==5,"socket UI transfers gem");UnityEngine.Object.FindObjectsOfType<Button>().First(b=>b.name=="Gem Combine"&&b.interactable).onClick.Invoke();Require(g.Rules.Save.gemInventory[1]==1,"combine UI produces next level");yield return Capture("07-gems");
   g.ShowRealmPage();yield return Capture("08-realm");g.ShowDungeons();yield return Capture("09-dungeons");g.ShowDungeon(1);yield return Capture("10-difficulty");g.ShowTower();yield return Capture("11-tower");
   var config=GuxuStageConfig.Load();g.Rules.Save.stage=200;g.Rules.Save.gear[0].attack=1000000;g.Rules.Save.gear[0].hp=1000000;g.Rules.Save.gear[0].speed=10000;g.Rules.Save.activeSkill=0;g.Rules.Save.autoSkill=true;g.ShowBattlePrep(config.stages[199]);yield return Click("Ruins Challenge");g.ToggleBattleSpeed();g.ToggleBattleSpeed();yield return WaitBattle(g);Require(g.Rules.Save.stage==201,"final main battle reaches 201 sentinel");Require(JsonConvert.DeserializeObject<SaveData>(File.ReadAllText(fixturePath)).stage==201,"final progress survives disk reload");yield return Capture("12-final-victory");
   g.CloseModal();g.ShowDungeons();g.ShowBattlePrep(config.Dungeon(1,1));yield return Click("Ruins Challenge");yield return WaitBattle(g);Require(g.Rules.Save.dungeonClears[1]==1&&g.Rules.Save.dungeonClaims[1]==1,"three-wave dungeon first clear settles once");
   g.CloseModal();g.ShowDungeon(1);UnityEngine.Object.FindObjectsOfType<Button>().First(b=>b.name=="Difficulty Sweep"&&b.interactable).onClick.Invoke();Require(g.Rules.Save.dungeonClaims[1]==2,"sweep UI spends one claim");
   g.Rules.Save.gear[0].attack=500;g.Rules.Save.activeSkill=1;g.Rules.Save.autoSkill=false;g.ShowBattlePrep(config.stages[159]);yield return Click("Ruins Challenge");yield return new WaitForSeconds(.1f);yield return Click("Cast Skill");yield return new WaitForSeconds(1.2f);yield return Capture("13-battle-shield");int progress=g.Rules.Save.stage;yield return Click("退出挑战");yield return new WaitForSeconds(1);Require(g.Rules.Save.stage==progress&&g.ActiveBattle==null,"cancelled battle cannot settle later");
   g.ShowBattlePrep(config.stages[199]);yield return Click("Ruins Challenge");yield return new WaitForSeconds(.7f);yield return Capture("14-boss-arena");yield return Click("退出挑战");
   g.Rules.Save.level=1;foreach(var gear in g.Rules.Save.gear){gear.attack=1;gear.hp=0;gear.defense=0;gear.speed=0;}Array.Clear(g.Rules.Save.forgeLevels,0,12);Array.Clear(g.Rules.Save.talents,0,24);Array.Clear(g.Rules.Save.gemSlots,0,24);g.ShowBattlePrep(config.stages[199]);yield return Click("Ruins Challenge");yield return WaitBattle(g);yield return Capture("26-defeat");Require(g.Rules.Save.stage==201,"defeat does not change progress");
   g.Rules.Save=new GameRules(g.Rules.Config,new SaveData{equipmentSchema=1,stage=41,level=20,coins=21000,peaches=120,forgeOre=80,insight=30,breakthroughStones=8}).Save;g.Rules.EnsureExpansion();g.Home();yield return Capture("15-home");
   g.Chop();g.Chop();g.SkipCurrentUnseal();yield return new WaitForSeconds(3.2f);Require(g.Rules.Save.pending!=null&&g.Rules.Save.peaches==119,"unseal spends one token despite double input");g.Home();yield return Capture("16-loot-decision");int dustBefore=g.Rules.Save.coins,expected=g.Rules.Save.pending.quality*20;yield return Click("Resolve Equipment");g.ResolveDrop(false);yield return new WaitForSeconds(2.2f);Require(g.Rules.Save.coins==dustBefore+expected&&g.Rules.Save.pending==null,"disassembly pays once");
   Open(g,"ShowBag");yield return Capture("17-backpack");g.CloseModal();Open(g,"ShowGear",0);yield return Capture("18-equipment-detail");g.CloseModal();Open(g,"AutoSettings");yield return Capture("19-auto-rules");g.CloseModal();Open(g,"TreeUpgrade");yield return Capture("20-appraisal");g.CloseModal();Open(g,"ShowCodex");yield return Capture("21-codex");g.CloseModal();Open(g,"ShowTierTable",0);yield return Capture("22-quality-data");g.CloseModal();Open(g,"ShowFinalResidence");yield return Capture("23-residence");g.CloseModal();Open(g,"ShowFinalCompanion");yield return Capture("24-companion");g.CloseModal();g.ShowLoadout();yield return Capture("25-loadout");
   File.WriteAllText(Output+"/ui-check.txt",DateTime.UtcNow.ToString("O")+" PASS: isolated save; 10 chapters / 200 stages, growth pages, forge, talent, socket, combine, 200th victory and disk save, 3-wave dungeon, sweep, manual shield, cancellation, defeat, unseal, disassembly, all original system panels; 26 screenshots. Player state restored after QA.");
  }finally {
   typeof(GxdlGame).GetProperty("Rules").SetValue(g,original);sizeProperty.SetValue(gameView,previousSize);Require(originalBytes==null?!File.Exists(savePath):originalBytes.SequenceEqual(File.ReadAllBytes(savePath)),"original player save bytes unchanged");
   if(opening<0)PlayerPrefs.DeleteKey(GxdlGame.OpeningStoryKey);else PlayerPrefs.SetInt(GxdlGame.OpeningStoryKey,opening);if(first<0)PlayerPrefs.DeleteKey(GxdlGame.FirstUnsealStoryKey);else PlayerPrefs.SetInt(GxdlGame.FirstUnsealStoryKey,first);PlayerPrefs.Save();g.Home();running=false;
  }
 }
}








