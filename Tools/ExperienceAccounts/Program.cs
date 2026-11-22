using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Gxdl;
using Newtonsoft.Json;
using UnityEngine;

class Program {
 static string Output;static OriginalConfig Config;static GuxuStageConfig Stages;static int battleSeed=42,days=1,attempts;
 static Dictionary<string,int> Actions=new Dictionary<string,int>(),Battles=new Dictionary<string,int>();
 static Dictionary<string,long[]> Income=new Dictionary<string,long[]>(),Expense=new Dictionary<string,long[]>();
 static List<object> Events=new List<object>(),Profiles=new List<object>();static long[] Initial;
 static string Hash(string value)=>Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();
 static long[] Balance(GameRules r){var s=r.Save;return new long[]{s.peaches,s.coins,s.exp,s.forgeOre,s.insight,s.breakthroughStones}.Concat(Enumerable.Range(0,24).Select(i=>(long)s.gemInventory[i]+s.gemSlots.Count(x=>x==i+1))).ToArray();}
 static bool Track(GameRules r,string action,Func<bool> run){var before=Balance(r);if(!run())return false;if(!Actions.ContainsKey(action)){Actions[action]=0;Income[action]=new long[30];Expense[action]=new long[30];}Actions[action]++;var after=Balance(r);for(int i=0;i<30;i++){long d=after[i]-before[i];if(d>=0)Income[action][i]+=d;else Expense[action][i]-=d;}Events.Add(new{index=Events.Count,day=days,stage=r.Save.stage,action,delta=after.Zip(before,(a,b)=>a-b).ToArray()});return true;}
 static double Rate(GameRules r,GuxuStage stage,int count=16){int wins=0;for(int i=0;i<count;i++){var b=new GuxuBattle(r.Attributes,GuxuStageRules.Enemy(stage,Config),stage,r.Save.stage,17001+i,r.CombatTraits,r.Save.activeSkill,true);while(!b.Completed)b.NextTurn();if(b.Won)wins++;}return wins/(double)count;}
 static bool Fight(GameRules r,GuxuStage stage,DateTime day){
  string kind=stage.mode==0?(stage.id==r.Save.stage?"main":"main-replay"):stage.mode==1?"dungeon-"+stage.contentId:"tower";
  int seed=battleSeed++;var b=GuxuStageRules.Begin(r,stage,seed);while(!b.Completed)b.NextTurn();string outcome=kind+(b.Won?"-won":"-lost");Battles[outcome]=Battles.TryGetValue(outcome,out int n)?n+1:1;
  Events.Add(new{index=Events.Count,day=days,action="battle",mode=stage.mode,id=stage.id,seed,won=b.Won,rounds=b.Round});
  if(!Track(r,kind+"-reward",()=>GuxuStageRules.Settle(r,b,day,out _)))return false;
  if(b.RewardReceiptId!=null&&!Track(r,kind+"-double",()=>r.DoubleReward(b.RewardReceiptId)))throw new Exception("Double reward rejected");return true;
 }
 static void Improve(GameRules r){
  while(Track(r,"realm",r.Breakthrough)){}
  int desired=Math.Min(12,2+r.HighestClear/20);while(r.Save.treeLevel<desired&&r.CanUpgradeTree&&r.Save.coins-r.TreeUpgradeCost>=300)Track(r,"appraisal",r.UpgradeTree);
  for(int pass=0;pass<400;pass++){if(r.Save.stage<=10&&r.Save.forgeLevels.Sum()>=Math.Max(0,(r.Save.stage-5)*2))break;int slot=Enumerable.Range(0,12).OrderBy(x=>r.Save.forgeLevels[x]).FirstOrDefault(x=>r.CanForge(x),-1);if(slot<0)break;Track(r,"forge-"+slot,()=>r.Forge(slot));}
  for(int pass=0;pass<80;pass++){int node=Enumerable.Range(0,24).OrderBy(x=>x/8==0?0:1).ThenBy(x=>r.Save.talents[x]).ThenBy(x=>x%8).FirstOrDefault(x=>r.CanTrainTalent(x),-1);if(node<0)break;Track(r,"talent-"+node,()=>r.TrainTalent(node));}
  if(!r.GrowthUnlocked(2))return;
  for(int slot=0;slot<12;slot++)for(int socket=0;socket<r.SocketCount;socket++)if(r.Save.gemSlots[slot*2+socket]==0){int gem=Enumerable.Range(0,24).OrderByDescending(x=>x%6).FirstOrDefault(x=>r.Save.gemInventory[x]>0,-1);if(gem>=0)Track(r,"gem-socket",()=>r.SocketGem(slot,socket,gem));}
  for(int n=0;n<24;n++)while(r.CanCombineGem(n))Track(r,"gem-combine",()=>r.CombineGem(n));
  for(int n=23;n>=0;n--)while(r.Save.gemInventory[n]>0){int dest=Enumerable.Range(0,12*r.SocketCount).Select(x=>(x/r.SocketCount)*2+x%r.SocketCount).FirstOrDefault(x=>r.Save.gemSlots[x]>0&&(r.Save.gemSlots[x]-1)/6==n/6&&(r.Save.gemSlots[x]-1)%6<n%6,-1);if(dest<0)break;Track(r,"gem-replace",()=>r.SocketGem(dest/2,dest%2,n));}
 }
 static void Chop(GameRules r,int count){for(int i=0;i<count&&Track(r,"unseal",r.Chop);i++){bool replace=r.AttributesWithGear(r.Save.pending).Power>r.Attributes.Power;Track(r,replace?"equip":"salvage",()=>r.Resolve(replace));while(Track(r,"realm",r.Breakthrough)){};}}
 static void Side(GameRules r,DateTime day){
  r.RefreshDaily(day);for(int kind=0;kind<4;kind++)while(r.Save.dungeonClaims[kind]<3){var stage=Stages.dungeons.Where(s=>s.contentId==kind&&GuxuStageRules.CanEnter(s,r.Save)).OrderByDescending(s=>s.difficulty).FirstOrDefault(s=>Rate(r,s)>=.75);if(stage==null||!Fight(r,stage,day))break;Improve(r);}
  int budget=10;while(budget-->0&&r.Save.towerFloor<=100){var stage=Stages.tower[r.Save.towerFloor-1];if(!GuxuStageRules.CanEnter(stage,r.Save)||Rate(r,stage)<.75||!Fight(r,stage,day))break;Improve(r);}
 }
 static void Export(GameRules r,string profile){
  var final=Balance(r);for(int i=0;i<30;i++)if(Initial[i]+Income.Values.Sum(x=>x[i])-Expense.Values.Sum(x=>x[i])!=final[i])throw new Exception("Unbalanced resource "+i);
  string scope=Hash("device-test:experience:v1:"+profile);var copy=JsonConvert.DeserializeObject<SaveData>(JsonConvert.SerializeObject(r.Save));
  if(copy.rewardReceipts!=null)for(int i=0;i<copy.rewardReceipts.Count;i++){copy.rewardReceipts[i].accountScope=scope;copy.rewardReceipts[i].id=Hash(scope+":"+i+":"+copy.rewardReceipts[i].source+":"+copy.rewardReceipts[i].sourceId).Substring(0,32);}
  if(profile!="new"){
   string originalScope=GameRules.AccountScope;var before=Newtonsoft.Json.Linq.JObject.FromObject(copy);
   try{GameRules.SelectAccount(scope);var nextDay=new GameRules(Config,copy);nextDay.RefreshDaily(DateTime.ParseExact(copy.rewardDay,"yyyy-MM-dd",System.Globalization.CultureInfo.InvariantCulture,System.Globalization.DateTimeStyles.AssumeUniversal).ToUniversalTime().AddDays(1));copy=nextDay.Save;}
   finally{GameRules.SelectAccount(originalScope);}
   var after=Newtonsoft.Json.Linq.JObject.FromObject(copy);before.Remove("rewardDay");before.Remove("dungeonClaims");after.Remove("rewardDay");after.Remove("dungeonClaims");
   if(!Newtonsoft.Json.Linq.JToken.DeepEquals(before,after)||copy.dungeonClaims.Any(x=>x!=0))throw new Exception("Next-day refresh changed growth or resources");
  }
  GameRules.ValidateSave(copy);if(copy.rewardReceipts!=null&&copy.rewardReceipts.Any(x=>!x.doubled))throw new Exception("Unclaimed generated bonus");
  string json=JsonConvert.SerializeObject(copy,Formatting.Indented);File.WriteAllText(Path.Combine(Resources.Root,"Assets/Resources/Game/ExperienceAccounts",profile+".json"),json);
  Profiles.Add(new{profile,scope,saveHash=Hash(json),copy.level,highestClear=r.HighestClear,nextStage=copy.stage,power=r.Attributes.Power,attributes=r.Attributes,traits=r.CombatTraits,appraisal=copy.treeLevel,forge=copy.forgeLevels,talents=copy.talents,gemSlots=copy.gemSlots,dungeonClears=copy.dungeonClears,dungeonClaims=copy.dungeonClaims,towerFloor=copy.towerFloor,activeSkill=copy.activeSkill,firstClears=copy.firstClears.Count(x=>x),chapterChests=copy.chapterChests,simulatedDay=days,progressionDay=days,loginDay=profile=="new"?days:days+1,dayRefreshAction=profile=="new"?0:1,rewardDay=copy.rewardDay,cuts=copy.cuts,gear=copy.gear,ledger=new{initial=Initial,final,income=Income.ToDictionary(x=>x.Key,x=>x.Value.ToArray()),expense=Expense.ToDictionary(x=>x.Key,x=>x.Value.ToArray()),balanced=true},actions=Actions.ToDictionary(x=>x.Key,x=>x.Value),battles=Battles.ToDictionary(x=>x.Key,x=>x.Value),eventCount=Events.Count,nextChallengeWinRate=Rate(r,Stages.stages[copy.stage-1],64)});
  Console.WriteLine(profile+": Lv"+r.Save.level+" cleared="+r.HighestClear+" power="+r.Attributes.Power+" forge="+r.Save.forgeLevels.Sum()+" day="+days);
 }
 static void Main(string[] args){
  Resources.Root=Environment.GetEnvironmentVariable("GXDL_ROOT")??"G:/demos/gxdl";Output=Path.Combine(Resources.Root,"Reference/ExperienceAccounts20261112-r01");Directory.CreateDirectory(Output);Directory.CreateDirectory(Application.persistentDataPath);
  if(args.Length>0&&args[0]=="check"){Check.Run();return;}
  if(args.Length>0&&args[0]=="unlocks"){UnlockCheck.Run();return;}
  Config=OriginalConfig.Load();Stages=GuxuStageConfig.Load();GameRules.SelectAccount(Hash("experience-generator:v1"));var r=new GameRules(Config,null,42);r.EnsureExpansion();Initial=Balance(r);Export(r,"new");
  DateTime day=new DateTime(2026,11,12,0,0,0,DateTimeKind.Utc);int consecutive=0;
  while(r.Save.stage<=160&&attempts<2500){int stage=r.Save.stage;Chop(r,2);Improve(r);Side(r,day);if(stage==80||stage==160){Export(r,stage==80?"mid":"high");if(stage==160)break;}attempts++;
   if(Fight(r,Stages.stages[stage-1],day)){consecutive=0;if(stage%20==0){if(Track(r,"chapter-reward",()=>r.ClaimChapter(stage/20-1)))Track(r,"chapter-double",()=>r.DoubleReward(r.LastRewardReceipt.id));day=day.AddDays(1);days++;}continue;}
   consecutive++;if(consecutive<=3&&r.Save.peaches>=5){Chop(r,5);continue;}
   if(consecutive<=8&&Fight(r,Stages.stages[Math.Max(0,stage-3)],day)){Chop(r,5);continue;}
   throw new Exception("Normal trajectory stuck at "+stage+"; do not inject resources");
  }
  if(Profiles.Count!=3)throw new Exception("Missing checkpoints");
  foreach(var record in Profiles){
   var summary=Newtonsoft.Json.Linq.JObject.FromObject(record);string profile=(string)summary["profile"];
   var saved=Newtonsoft.Json.Linq.JObject.Parse(File.ReadAllText(Path.Combine(Resources.Root,"Assets/Resources/Game/ExperienceAccounts",profile+".json")));
   foreach(var pair in new[]{("level","level"),("nextStage","stage"),("appraisal","treeLevel"),("forge","forgeLevels"),("talents","talents"),("gemSlots","gemSlots"),("dungeonClears","dungeonClears"),("dungeonClaims","dungeonClaims"),("towerFloor","towerFloor"),("activeSkill","activeSkill"),("chapterChests","chapterChests"),("gear","gear")})
    if(!Newtonsoft.Json.Linq.JToken.DeepEquals(summary[pair.Item1],saved[pair.Item2]))throw new Exception("Profile summary drift: "+profile+"/"+pair.Item1);
  }
  var sources=new[]{"Assets/Scripts/GameRules.cs","Assets/Scripts/EquipmentTiers.cs","Assets/Scripts/GuxuGrowthRules.cs","Assets/Scripts/GuxuStageRules.cs","Assets/Scripts/GuxuRewardReceipts.cs","Assets/Scripts/GuxuGrowthAssessment.cs","Assets/Resources/Game/OriginalConfig.json","Assets/Resources/Game/Guxu/Stages.json"}.Select(path=>new{path,sha256=Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(Path.Combine(Resources.Root,path))))}).ToArray();
  File.WriteAllText(Path.Combine(Output,"generated-profiles.json"),JsonConvert.SerializeObject(new{generatedAt=DateTimeOffset.Now.ToString("O"),seed=42,policy="Default new save; two unseals per main attempt, affordable growth, one modeled day per 20 cleared stages, all recorded double bonuses claimed. New profile is untouched default. Mid/high are before stages 80/160. No injected resources or config edits.",resources=new[]{"seals","dust","exp","ore","insight","stones"}.Concat(Enumerable.Range(0,24).Select(i=>"gem-"+i)),sources,profiles=Profiles},Formatting.Indented));
  File.WriteAllText(Path.Combine(Output,"trajectory.json"),JsonConvert.SerializeObject(Events,Formatting.Indented));
 }
}
