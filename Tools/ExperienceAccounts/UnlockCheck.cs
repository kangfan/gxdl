using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Gxdl;
using Newtonsoft.Json;
using UnityEngine;

static class UnlockCheck {
 static void Require(bool condition,string label){if(!condition)throw new Exception(label);}
 static SaveData Copy(SaveData s)=>JsonConvert.DeserializeObject<SaveData>(JsonConvert.SerializeObject(s));
 public static void Run(){
  var config=OriginalConfig.Load();var catalog=GuxuStageConfig.Load();var profiles=new List<object>();
  Require(catalog.stages.Length==200&&catalog.dungeons.Length==40&&catalog.tower.Length==100,"catalog counts");
  foreach(string profile in new[]{"new","mid","high"}){
   GameRules.SelectAccount(GuxuExperienceAccounts.Scope(profile));
   var seed=JsonConvert.DeserializeObject<SaveData>(Resources.Load<TextAsset>("Game/ExperienceAccounts/"+profile).text);
   var r=new GameRules(config,seed);r.EnsureExpansion();var s=r.Save;
   string unchanged=JsonConvert.SerializeObject(s);
   Require(s.firstClears.Select((clear,index)=>clear==(index<s.stage-1)).All(x=>x),profile+" continuous main first clears");
   var main=catalog.stages.Select(stage=>new{id=stage.id,required=GuxuStageRules.RequiredProgress(stage),cleared=s.firstClears[stage.id-1],unlocked=GuxuStageRules.CanEnter(stage,s)}).ToArray();
   Require(main.All(x=>x.unlocked==(x.id<=s.stage)&&(!x.cleared||x.unlocked)),profile+" main gate");
   // Use the saved simulated day to test today's quota without refreshing it or mutating a seed.
   DateTime day=string.IsNullOrEmpty(s.rewardDay)?DateTime.UtcNow:DateTime.ParseExact(s.rewardDay,"yyyy-MM-dd",System.Globalization.CultureInfo.InvariantCulture,System.Globalization.DateTimeStyles.AssumeUniversal).ToUniversalTime();
   var dungeons=catalog.dungeons.Select(stage=>{
    int kind=stage.contentId;bool cleared=stage.difficulty<=s.dungeonClears[kind],unlocked=GuxuStageRules.CanEnter(stage,s);
    Require(!cleared||unlocked,profile+" cleared dungeon must be unlocked "+stage.id);
    Require(unlocked==(s.stage>=GuxuStageRules.RequiredProgress(stage)&&stage.difficulty<=s.dungeonClears[kind]+1),profile+" dungeon gate "+stage.id);
    var today=new GameRules(config,Copy(s));today.RefreshDaily(day);string beforeSweep=JsonConvert.SerializeObject(today.Save);bool swept=GuxuStageRules.Sweep(today,stage,day,out _);
    bool expected=unlocked&&cleared&&s.dungeonClaims[kind]<3;
    Require(swept==expected,profile+" actual sweep gate "+stage.id);
    if(swept)Require(today.Save.dungeonClaims[kind]==s.dungeonClaims[kind]+1,profile+" sweep quota "+stage.id);
    else Require(JsonConvert.SerializeObject(today.Save)==beforeSweep,profile+" failed sweep unchanged "+stage.id);
    var tomorrow=new GameRules(config,Copy(s));bool nextDaySweep=GuxuStageRules.Sweep(tomorrow,stage,day.AddDays(1),out _);
    Require(nextDaySweep==(unlocked&&cleared),profile+" next-day sweep gate "+stage.id);
    return new{id=stage.id,kind,name=stage.name,tier=stage.difficulty,required=GuxuStageRules.RequiredProgress(stage),cleared,unlocked,dailyClaims=s.dungeonClaims[kind],rewardEligible=unlocked&&s.dungeonClaims[kind]<3,sweepEligible=swept,sweepEligibleNextDay=nextDaySweep};
   }).ToArray();
   var tower=catalog.tower.Select(stage=>new{id=stage.id,floor=stage.difficulty,required=GuxuStageRules.RequiredProgress(stage),cleared=stage.difficulty<s.towerFloor,unlocked=GuxuStageRules.CanEnter(stage,s)}).ToArray();
   Require(tower.All(x=>x.unlocked==(x.floor<s.towerFloor||x.floor==s.towerFloor&&x.required<=s.stage)),profile+" tower gate");
   var chapters=Enumerable.Range(0,10).Select(index=>{
    int required=(index+1)*20;bool claimed=s.chapterChests[index],complete=r.HighestClear>=required,canClaim=r.CanClaimChapter(index);
    Require(!claimed||complete,profile+" uncompleted chapter not preclaimed");Require(canClaim==(complete&&!claimed),profile+" chapter eligibility");
    var clone=new GameRules(config,Copy(s));Require(clone.ClaimChapter(index)==canClaim,profile+" actual chapter claim");
    return new{chapter=index+1,requiredClears=required,completed=complete,claimed,canClaim};
   }).ToArray();
   Require(JsonConvert.SerializeObject(s)==unchanged,profile+" checks preserve source state");
   var summary=new{mainCleared=main.Count(x=>x.cleared),mainUnlocked=main.Count(x=>x.unlocked),dungeonClears=s.dungeonClears,dungeonUnlocked=Enumerable.Range(0,4).Select(k=>dungeons.Count(x=>x.kind==k&&x.unlocked)).ToArray(),dailyClaims=s.dungeonClaims,sweepEligibleToday=dungeons.Count(x=>x.sweepEligible),towerCleared=tower.Count(x=>x.cleared),towerUnlocked=tower.Count(x=>x.unlocked),chaptersClaimed=chapters.Count(x=>x.claimed)};
   profiles.Add(new{profile,scope=GameRules.AccountScope,summary,main,dungeons,tower,chapters});Console.WriteLine(profile+" "+JsonConvert.SerializeObject(summary));
  }
  GameRules.SelectAccount("");string output=Path.Combine(Resources.Root,"Reference/ExperienceAccounts20261112-r01/unlock-matrix.json");
  File.WriteAllText(output,JsonConvert.SerializeObject(new{status="PASS",created=DateTimeOffset.Now,method="Production CanEnter/RequiredProgress, Sweep and ClaimChapter on deep copies; no stored state modified",profiles},Formatting.Indented));Console.WriteLine("PASS "+output);
 }
}
