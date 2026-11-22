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

public static class GuxuGrowthBalanceCheck {
 const string Root="Reference/GrowthBalance20261111";
 const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
 static readonly List<string> checks=new List<string>();
 static string Json(object o)=>JsonConvert.SerializeObject(o);
 static void Check(bool ok,string name){if(!ok)throw new Exception("Growth QA: "+name);checks.Add("PASS "+name);}
 static GameRules Fresh(OriginalConfig config,int stage=1)=>new GameRules(config,new SaveData{equipmentSchema=1,stage=stage},42);
 static GuxuBattle Win(GameRules r,GuxuStage s){var b=new GuxuBattle(new Stats{attack=1000000,hp=1000000,defense=100000,speed=10000},GuxuStageRules.Enemy(s,r.Config),s,r.Save.stage,42);while(!b.Completed)b.NextTurn();Check(b.Won,"test victory "+s.id);return b;}
 [MenuItem("GXDL/QA/Growth Rules")]
 public static void Rules(){
  Directory.CreateDirectory(Root);checks.Clear();string scope=GameRules.AccountScope;var config=OriginalConfig.Load();var catalog=GuxuStageConfig.Load();
  try{
   Check(catalog.stages.Length==200&&catalog.dungeons.Length==40&&catalog.tower.Length==100,"catalog 200/40/100");EquipmentTiers.Validate();GuxuStageRules.SelfTest(config,catalog);Check(true,"all 340 encounters have positive stats and valid rounds/waves");
   var r=Fresh(config);r.EnsureExpansion();float previous=0;for(int level=1;level<=20;level++){var chance=r.QualityChances(level);float expected=chance.Select((p,i)=>p*(i+1)).Sum();Check(Math.Abs(chance.Sum()-100)<.001f&&expected>previous,"appraisal meaningful probability "+level);previous=expected;}
   for(int level=10;level<100;level+=10)Check(int.Parse(config.realms[level].attackBase)-int.Parse(config.realms[level-1].attackBase)>1,"breakthrough meaningful attack "+level);
   string before=Json(r.Save);var assessment=r.AssessChallenge(catalog.stages[0]);Check(before==Json(r.Save)&&assessment.samples==24,"prediction leaves save unchanged");Check(Json(assessment)==Json(r.AssessChallenge(catalog.stages[0])),"fixed-seed prediction repeatable");var same=Fresh(config);r.Chop();same.Chop();Check(Json(r.Save.pending)==Json(same.Save.pending),"prediction does not consume loot RNG");
   r=Fresh(config);r.EnsureExpansion();var day=new DateTime(2026,11,11,0,0,0,DateTimeKind.Utc);var main=Win(r,catalog.stages[0]);Check(GuxuStageRules.Settle(r,main,day,out var baseMain),"main base settled");int seals=r.Save.peaches;Check(r.DoubleReward(main.RewardReceiptId)&&r.Save.peaches==seals+baseMain.seals&&r.Save.stage==2,"main double exact snapshot without progress");before=Json(r.Save);Check(!r.DoubleReward(main.RewardReceiptId)&&before==Json(r.Save),"repeat double no mutation");Check(!GuxuStageRules.Settle(r,main,day,out _),"battle settled once");
   var replay=Win(r,catalog.stages[0]);Check(GuxuStageRules.Settle(r,replay,day,out var replayReward)&&replay.RewardReceiptId!=main.RewardReceiptId&&replayReward.seals<baseMain.seals,"legal repeat has new receipt and repeat reward");
   r=Fresh(config,21);r.EnsureExpansion();Check(r.ClaimChapter(0),"chapter base claimed");string chapter=r.LastRewardReceipt.id;before=Json(r.Save);Check(!r.ClaimChapter(0)&&before==Json(r.Save),"chapter base once");Check(r.DoubleReward(chapter)&&r.Save.chapterChests[0],"chapter double does not reset claim");
   for(int kind=0;kind<4;kind++){
    r=Fresh(config,201);r.EnsureExpansion();var dungeon=catalog.Dungeon(kind,1);var battle=Win(r,dungeon);Check(GuxuStageRules.Settle(r,battle,day,out var reward),"dungeon base "+kind);var snapshot=Json(reward);Check(r.DoubleReward(battle.RewardReceiptId)&&r.Save.dungeonClaims[kind]==1&&Json(r.LastRewardReceipt.basis)==snapshot,"dungeon double snapshot and quota "+kind);
    Check(GuxuStageRules.Sweep(r,dungeon,day,out var sweep)&&r.Save.dungeonClaims[kind]==2,"sweep base "+kind);var id=r.LastRewardReceipt.id;Check(r.DoubleReward(id)&&r.Save.dungeonClaims[kind]==2,"sweep double no extra quota "+kind);Check(GuxuStageRules.Sweep(r,dungeon,day,out _)&&!GuxuStageRules.Sweep(r,dungeon,day,out _),"daily cap unchanged "+kind);
   }
   r=Fresh(config,201);r.EnsureExpansion();var tower=Win(r,catalog.tower[0]);Check(GuxuStageRules.Settle(r,tower,day,out _)&&r.DoubleReward(tower.RewardReceiptId)&&r.Save.towerFloor==2,"tower first double");var repeatTower=Win(r,catalog.tower[0]);int receipts=r.Save.rewardReceipts.Count;Check(GuxuStageRules.Settle(r,repeatTower,day,out _)&&repeatTower.RewardReceiptId==null&&r.Save.rewardReceipts.Count==receipts,"tower repeat no reward receipt");
   r.Save.towerFloor=80;r.Save.stage=16;Check(GuxuStageRules.CanEnter(catalog.tower[78],r.Save)&&!GuxuStageRules.CanEnter(catalog.tower[79],r.Save),"old tower cleared revisit retained, new floor gated");
   r=Fresh(config);r.EnsureExpansion();var loss=new GuxuBattle(new Stats{attack=1,hp=1},new Stats{attack=100,hp=100},catalog.stages[0],1,42);while(!loss.Completed)loss.NextTurn();r.RefreshDaily(day);before=Json(r.Save);Check(!GuxuStageRules.Settle(r,loss,day,out _)&&before==Json(r.Save),"defeat no rewards or receipt");
   string idOverflow=r.GrantRecorded(new GuxuReward{seals=10,dust=20},"main",1,"overflow");r.Save.coins=int.MaxValue;before=Json(r.Save);try{r.DoubleReward(idOverflow);throw new Exception("Expected overflow");}catch(OverflowException){}Check(before==Json(r.Save),"overflow preflight protects every resource and claim");
   r=Fresh(config);r.EnsureExpansion();string huge=r.GrantRecorded(new GuxuReward{dust=int.MaxValue/2+1},"main",1,"huge");r.Save.coins=0;before=Json(r.Save);try{r.DoubleReward(huge);throw new Exception("Expected total overflow");}catch(OverflowException){}Check(before==Json(r.Save),"total x2 overflow rejected before grant");
   r=Fresh(config);r.EnsureExpansion();foreach(var reward in new[]{new GuxuReward{ore=-1},new GuxuReward{gems=1,gemIndex=-1},new GuxuReward{gems=1,gemIndex=24}}){before=Json(r.Save);try{r.GrantRecorded(reward,"main",1,"bad");throw new Exception("Expected invalid reward");}catch(InvalidDataException){}Check(before==Json(r.Save),"invalid reward no partial grant");}
   GameRules.SelectAccount(new string('a',64));var a=Fresh(config);a.EnsureExpansion();string aid=a.GrantRecorded(new GuxuReward{seals=7},"main",1,"A");GameRules.SelectAccount(new string('b',64));var b=Fresh(config);b.EnsureExpansion();b.GrantRecorded(new GuxuReward{seals=8},"main",1,"B");before=Json(b.Save);Check(!a.DoubleReward(aid)&&!b.DoubleReward(aid)&&before==Json(b.Save),"old A rules/button cannot grant B reward");GameRules.SelectAccount(new string('a',64));Check(a.DoubleReward(aid),"same account pending valid after switch back");
   string path=Path.GetFullPath(Root+"/reward-roundtrip.json");typeof(GameRules).GetField("storagePath",Private).SetValue(a,path);a.Store();var loaded=JsonConvert.DeserializeObject<SaveData>(File.ReadAllText(path));GameRules.ValidateSave(loaded);var restored=new GameRules(config,loaded,42);Check(!restored.DoubleReward(aid),"persisted double cannot repeat after reload");
   var old=new SaveData{equipmentSchema=1,stage=101,level=40};var oldRules=new GameRules(config,old,42);oldRules.EnsureExpansion();Check(oldRules.Save.rewardReceipts==null,"old save has no retroactive double receipts");
   string keep=a.GrantRecorded(new GuxuReward{seals=1},"main",1,"keep pending");for(int i=0;i<120;i++){string next=a.GrantRecorded(new GuxuReward{seals=1},"main",1,"retain");a.DoubleReward(next);}a.GrantRecorded(new GuxuReward{seals=1},"main",1,"trigger pruning");Check(a.Save.rewardReceipts.Count(x=>x.doubled)<=64&&a.FindRewardReceipt(keep)!=null&&a.DoubleReward(keep),"actual compaction retains old pending receipt and it remains claimable");
   GameRules.ValidateSave(a.Save);
   var samples=new List<object>();foreach(string name in new[]{"growth-mid","growth-late","growth-double","growth-failure"}){var save=JsonConvert.DeserializeObject<SaveData>(File.ReadAllText(Root+"/simulation/"+name+".json"));var sample=new GameRules(config,save,42);sample.EnsureExpansion();foreach(var stage in new[]{catalog.stages[sample.Save.stage-1],catalog.tower[0],catalog.tower[49],catalog.tower[99]}.Concat(catalog.dungeons.Where(x=>x.difficulty==1||x.difficulty==5||x.difficulty==10))){for(int skill=0;skill<4;skill++){sample.Save.activeSkill=skill;var result=sample.AssessChallenge(stage);samples.Add(new{name,stage.mode,stage.id,stage.contentId,stage.difficulty,skill,result});}}}File.WriteAllText(Root+"/runtime-combat-samples.json",JsonConvert.SerializeObject(new{at=DateTime.UtcNow.ToString("O"),samples},Formatting.Indented));Check(samples.Count==256,"Unity runtime 256 representative encounter/skill samples");
   checks.Add("PASS ALL "+checks.Count+" checks; financial/rule fixtures only, no player save written");
  }catch(Exception e){checks.Add("FAIL "+e);Debug.LogException(e);throw;}
  finally{GameRules.SelectAccount(scope);File.WriteAllLines(Root+"/technical-rules.txt",new[]{DateTime.UtcNow.ToString("O")}.Concat(checks));}
 }
 [MenuItem("GXDL/QA/Growth UI")]
 public static void UI(){GuxuUI23Review.BeginGrowth();GxdlGame.Instance.StartCoroutine(UIGuard(UIRun(),"technical-ui.txt"));}
 [MenuItem("GXDL/QA/Growth Auto Stop")]
 public static void AutoStop(){GuxuUI23Review.BeginGrowth();GxdlGame.Instance.StartCoroutine(UIGuard(StopRun(),"technical-auto-stop.txt"));}
 [MenuItem("GXDL/QA/Growth Refresh Hints")]
 public static void RefreshHints(){GuxuUI23Review.BeginGrowth();GxdlGame.Instance.StartCoroutine(UIGuard(RefreshHintsRun(),"technical-refresh-hints.txt"));}
 static IEnumerator UIGuard(IEnumerator routine,string output){
  var stack=new Stack<IEnumerator>();stack.Push(routine);bool pass=false;
  try{while(stack.Count>0){object current=null;bool moved;try{moved=stack.Peek().MoveNext();if(moved)current=stack.Peek().Current;}catch(Exception e){File.WriteAllText(Root+"/"+output,"FAIL "+DateTime.UtcNow.ToString("O")+"\n"+e);Debug.LogException(e);yield break;}if(!moved){(stack.Pop() as IDisposable)?.Dispose();continue;}if(current is IEnumerator inner)stack.Push(inner);else yield return current;}pass=true;}
  finally{while(stack.Count>0)(stack.Pop() as IDisposable)?.Dispose();GuxuUI23Review.End();if(pass)File.WriteAllText(Root+"/"+output,"PASS "+DateTime.UtcNow.ToString("O")+"\n"+string.Join("\n",checks)+"\nEvidence "+GuxuUI23Review.ActiveOutput);}
 }
 static IEnumerator Command(string action,string name="",int height=1334)=>GuxuUI23Review.Run(new GuxuUI23Review.Command{action=action,name=name,height=height});
 static IEnumerator RefreshHintsRun(){
  checks.Clear();var g=GxdlGame.Instance;var r=g.Rules;var version=typeof(GxdlGame).GetField("screenVersion",Private);var advice=typeof(GxdlGame).GetMethod("DefeatAdvice",Private);
  foreach(int height in new[]{1334,1624}){
   yield return GuxuUI23Review.Run(new GuxuUI23Review.Command{action="scenario",scenario="growth-reward"});Check(Json(r.Save)==Json(JsonConvert.DeserializeObject<SaveData>(File.ReadAllText(Root+"/actual-newbie-post6.json"))),"reward scenario exact actual snapshot "+height);yield return Command("size",height:height);
   foreach(string source in new[]{"main","dungeon","tower"}){
    if(source=="tower"){yield return GuxuUI23Review.Run(new GuxuUI23Review.Command{action="scenario",scenario="growth-late"});g.ShowTower();}else if(source=="main")g.ShowChapter(0);else g.ShowDungeon(0);
    yield return null;var button=GameObject.Find("Reward History "+source).GetComponent<Button>();var label=button.GetComponentInChildren<Text>();var scrolls=UnityEngine.Object.FindObjectsOfType<ScrollRect>();foreach(var scroll in scrolls)scroll.StopMovement();var positions=scrolls.Select(s=>s.content.anchoredPosition).ToArray();int initialVersion=(int)version.GetValue(g);string selection=Json(typeof(GxdlGame).GetField("chapterSelections",Private).GetValue(g));
    int count=r.Save.rewardReceipts.Count(x=>!x.doubled&&(x.source==source||source=="main"&&x.source=="chapter"));Check(count>0&&label.text=="▷ 奖励加倍 "+count,"initial visible count "+source+" "+height);
    int claims=source=="tower"?1:count;
    for(int i=0;i<claims;i++){
     yield return Command("click","Reward History "+source);yield return Command("click","Reward Receipt 0");yield return Command("click","Reward Double");count--;
     string expected="▷ 奖励加倍"+(count>0?" "+count:"");Check(label&&label.text==expected&&(int)version.GetValue(g)==initialVersion,"claim updates source in place "+source+" remaining="+count+" "+height);
     yield return Command("click","Close Modal");yield return Command("click","Close Modal");Check(label.text==expected&&GameObject.Find("Reward History "+source).GetComponent<Button>()==button,"close retains current count and same button "+source+" remaining="+count+" "+height);
    }
    Check(selection==Json(typeof(GxdlGame).GetField("chapterSelections",Private).GetValue(g))&&scrolls.Select((s,i)=>Vector2.Distance(s.content.anchoredPosition,positions[i])<.1f).All(x=>x),"selection and scroll preserved "+source+" "+height);
   }
   yield return GuxuUI23Review.Run(new GuxuUI23Review.Command{action="scenario",scenario="growth-hint"});Check(Json(r.Save)==Json(JsonConvert.DeserializeObject<SaveData>(File.ReadAllText(Root+"/actual-newbie-failure5.json")))&&PlayerPrefs.GetInt(GxdlGame.FirstUnsealStoryKey,0)==1,"hint scenario exact actual snapshot "+height);g.ShowChapter(0);yield return Command("click","Chapter Challenge");yield return Command("click","Ruins Challenge");float deadline=Time.realtimeSinceStartup+40;while(g.ActiveBattle!=null&&Time.realtimeSinceStartup<deadline)yield return null;yield return new WaitForSeconds(.2f);
   var hint=GameObject.Find("Settlement Advice")?.GetComponent<Text>();Check(hint&&hint.text.Contains("继续破封")&&hint.text.Contains("修行突破")&&!hint.text.Contains("锻造")&&!hint.text.Contains("天赋")&&!hint.text.Contains("灵玉"),"actual stage5 defeat shows available early growth only "+height);yield return Command("inspect");yield return Command("click","返回");
  }
  var original=Json(r.Save);var battle=GuxuStageRules.Begin(r,GuxuStageConfig.Load().ForProgress(1),42);r.Save.exp=r.ExpNeeded;r.Save.breakthroughStones=r.BreakthroughCost;Check(((string)advice.Invoke(g,new object[]{battle})).StartsWith("已可突破境界"),"ready breakthrough takes priority");
  r.Save.exp=0;r.Save.stage=10;r.Save.forgeOre=20;Check(((string)advice.Invoke(g,new object[]{battle})).Contains("锻造")&&!((string)advice.Invoke(g,new object[]{battle})).Contains("天赋"),"before clear10 never recommends talents");
  r.Save.stage=20;r.Save.insight=2;Check(((string)advice.Invoke(g,new object[]{battle})).Contains("坚壁天赋")&&!((string)advice.Invoke(g,new object[]{battle})).Contains("灵玉"),"available talent and no locked gem advice");
  r.Save.stage=21;r.Save.insight=0;r.Save.gemInventory[6]=1;Check(((string)advice.Invoke(g,new object[]{battle})).Contains("碧生灵玉"),"unlocked owned life gem advice");r.Save.gemInventory[6]=0;Check(!((string)advice.Invoke(g,new object[]{battle})).Contains("灵玉"),"no gem inventory does not suggest socketing");r.Save=JsonConvert.DeserializeObject<SaveData>(original);
 }
 static IEnumerator StopRun(){
  checks.Clear();var g=GxdlGame.Instance;var r=g.Rules;var auto=typeof(GxdlGame).GetField("auto",Private);var busy=typeof(GxdlGame).GetField("busy",Private);var version=typeof(GxdlGame).GetField("screenVersion",Private);
  foreach(int height in new[]{1334,1624})foreach(bool first in new[]{true,false}){
   yield return GuxuUI23Review.Run(new GuxuUI23Review.Command{action="scenario",scenario="growth-new"});if(!first)PlayerPrefs.SetInt(GxdlGame.FirstUnsealStoryKey,1);g.Home();yield return Command("size",height:height);
   int seals=r.Save.peaches,cuts=r.Save.cuts;yield return Command("click","自动破封");Check((bool)auto.GetValue(g)&&(bool)busy.GetValue(g)&&r.Save.cuts==cuts+1,"auto entered current animation "+height+" first="+first);
   string pending=Json(r.Save.pending);int currentVersion=(int)version.GetValue(g);Check(GameObject.Find("停止自动").GetComponent<Button>().interactable,"stop enabled during animation "+height+" first="+first);
   yield return Command("click","停止自动");Check(!(bool)auto.GetValue(g)&&(int)version.GetValue(g)==currentVersion,"pointer stop immediate without rebuilding active animation "+height+" first="+first);var animation=UnityEngine.Object.FindObjectOfType<GuxuAnimation>();if(animation&&animation.IsPlaying&&animation.RuneContactAt<0)Check(animation.StoneVisible,"stop preserves visible stone before rune contact "+height+" first="+first);yield return Command("reject-click","自动破封");
   float deadline=Time.realtimeSinceStartup+18;while((bool)busy.GetValue(g)&&Time.realtimeSinceStartup<deadline)yield return null;Check(!(bool)busy.GetValue(g),"current animation completes after stop "+height+" first="+first);yield return new WaitForSeconds(2);yield return Command("inspect");
   Check(r.Save.cuts==cuts+1&&r.Save.peaches==seals-1&&pending==Json(r.Save.pending),"exactly one seal spent and same pending gear retained "+height+" first="+first);yield return Command("reject-click","自动破封");
  }
  yield return GuxuUI23Review.Run(new GuxuUI23Review.Command{action="scenario",scenario="growth-resume"});var expected=JsonConvert.DeserializeObject<SaveData>(File.ReadAllText(Root+"/actual-newbie-before-stop-bug.json"));Check(Json(r.Save)==Json(expected)&&PlayerPrefs.GetInt(GxdlGame.FirstUnsealStoryKey,0)==1,"actual nine-cut resume exact save and first story completed");
 }
 static IEnumerator UIRun(){
  checks.Clear();var g=GxdlGame.Instance;var r=g.Rules;var catalog=GuxuStageConfig.Load();
  foreach(int height in new[]{1334,1624}){
   yield return Command("size",height:height);r.Save=Fresh(r.Config,21).Save;r.EnsureExpansion();g.ShowChapter(0);yield return Command("click","领取章节宝匣");int before=r.Save.peaches;var receipt=r.LastRewardReceipt;yield return Command("click","Reward Double");Check(r.Save.peaches==before+receipt.basis.seals&&receipt.doubled,"chapter pointer double "+height);yield return Command("reject-click","Reward Double");yield return Command("click","Close Modal");g.ShowChapter(0);yield return Command("click","Reward History main");yield return Command("click","Reward Receipt 0");Check(UnityEngine.Object.FindObjectsOfType<Text>().Any(t=>t.name=="Reward Receipt Extra"&&t.text.Contains("额外已领取")),"reentered receipt claimed "+height);yield return Command("click","Close Modal");g.CloseModal();
   r.Save=Fresh(r.Config,201).Save;r.EnsureExpansion();r.Save.dungeonClears[1]=1;g.ShowDungeon(1);yield return Command("click","Difficulty Node 1");yield return Command("click","Difficulty Sweep");var gemReceipt=r.LastRewardReceipt;int stock=r.Save.gemInventory[gemReceipt.basis.gemIndex];yield return Command("click","Reward Double");Check(r.Save.gemInventory[gemReceipt.basis.gemIndex]==stock+gemReceipt.basis.gems&&r.Save.dungeonClaims[1]==1,"gem sweep same type and no extra quota "+height);yield return Command("click","Close Modal");
   r.Save=Fresh(r.Config).Save;r.EnsureExpansion();g.ShowChapter(0);yield return Command("click","Chapter Challenge");string beforeSave=Json(r.Save);yield return Command("click","Skill Choice 1");Check(r.Save.activeSkill==1&&GameObject.Find("Prep Assessment"),"skill choice updates assessment "+height);yield return Command("click","Skill Choice 0");yield return Command("click","Ruins Challenge");float deadline=Time.realtimeSinceStartup+40;while(g.ActiveBattle!=null&&Time.realtimeSinceStartup<deadline)yield return null;yield return new WaitForSeconds(.2f);Check(r.LastRewardReceipt!=null&&r.LastRewardReceipt.source=="main","actual UI battle creates main receipt "+height);yield return Command("click","Reward Double");Check(r.LastRewardReceipt.doubled,"main settlement pointer double "+height);yield return Command("reject-click","Reward Double");yield return Command("click","返回");
  }
 }
}
