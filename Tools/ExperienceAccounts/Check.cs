using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Gxdl;
using Newtonsoft.Json;
using UnityEngine;

static class Check {
 static List<string> Results=new List<string>();
 static void Require(bool condition,string label){if(!condition)throw new Exception(label);Results.Add("PASS "+label);}
 static void Reject(Action action,string label){bool rejected=false;try{action();}catch(InvalidDataException){rejected=true;}Require(rejected,label);}
 public static void Run(){
  string evidence=Path.Combine(Resources.Root,"Reference/ExperienceAccounts20261112-r01");
  var config=OriginalConfig.Load();var scopes=new[]{"new","mid","high"}.Select(GuxuExperienceAccounts.Scope).ToArray();
  Require(scopes.Distinct().Count()==3&&!scopes.Contains(GuxuAccountConfig.Hash("device-test:phone:v1")),"three independent scopes preserve legacy phone scope");
  var files=new Dictionary<string,byte[]>();foreach(string scope in scopes){GameRules.SelectAccount(scope);foreach(string path in Directory.GetFiles(Application.persistentDataPath,Path.GetFileName(GameRules.SavePath)+"*"))files[path]=File.ReadAllBytes(path);}
  try{
   foreach(string scope in scopes){GameRules.SelectAccount(scope);foreach(string path in Directory.GetFiles(Application.persistentDataPath,Path.GetFileName(GameRules.SavePath)+"*"))File.Delete(path);}
   GuxuPhoneTestLogin.Enabled=false;Require(GuxuExperienceAccounts.Authenticate("gxdl_new","GxdlNew123!")==null&&GuxuExperienceAccounts.Profile(scopes[0])==null,"disabled test gate rejects credentials and seeded scopes");GuxuPhoneTestLogin.Enabled=true;
   Require(GuxuExperienceAccounts.Authenticate("gxdl_new","wrong")==null&&GuxuExperienceAccounts.Authenticate("gxdl_mid","GxdlNew123!")==null&&GuxuExperienceAccounts.Authenticate("unknown","GxdlHigh123!")==null,"wrong password, cross-password and username rejected");
   string[] profiles={"new","mid","high"},passwords={"GxdlNew123!","GxdlMid123!","GxdlHigh123!"};int[] clears={0,79,159},powers={1462,55008,107272};
   for(int i=0;i<3;i++){
    string profile=profiles[i];Require(GuxuExperienceAccounts.Authenticate("gxdl_"+profile,passwords[i])==profile,"valid credentials select "+profile);GameRules.SelectAccount(scopes[i]);
    var r=GuxuExperienceAccounts.LoadRules(config);r.EnsureExpansion();Require(r.HighestClear==clears[i]&&r.Attributes.Power==powers[i]&&r.Save.gear.Length==12,"actual state and derived power "+profile);Require(File.Exists(GameRules.SavePath),"first login atomically persists "+profile);
    if(profile=="new")Require(r.Save.level==1&&r.Save.peaches==40&&r.Save.coins==300&&r.Save.forgeLevels.All(x=>x==0)&&r.Save.rewardReceipts==null&&r.Save.cuts==0,"new is unmodified production default");
    else Require(r.Save.rewardDay==GameRules.Day(DateTime.UtcNow)&&r.Save.dungeonClaims.All(x=>x==0)&&r.Save.rewardReceipts.All(x=>x.doubled&&x.accountScope==scopes[i]),"next-day login mapped with zero used claims; receipts rebound "+profile);
    string before=JsonConvert.SerializeObject(r.Save);Require(!r.Forge(0)&&JsonConvert.SerializeObject(r.Save)==before,"unavailable forge rejects without cost "+profile);
    int seals=r.Save.peaches;Require(r.Chop()&&r.Save.peaches==seals-1,"real unseal cost "+profile);Require(r.Resolve(false),"real salvage after unseal "+profile);r.Store();string progressed=JsonConvert.SerializeObject(r.Save);
    var reloaded=GuxuExperienceAccounts.LoadRules(config);Require(JsonConvert.SerializeObject(reloaded.Save)==progressed,"relogin persists progress without reseeding "+profile);
    if(reloaded.Save.rewardReceipts!=null){int coins=reloaded.Save.coins;Require(!reloaded.DoubleReward(reloaded.Save.rewardReceipts[0].id)&&reloaded.Save.coins==coins,"no duplicate historical reward "+profile);GameRules.SelectAccount(scopes[(i+1)%3]);Require(!reloaded.DoubleReward(reloaded.Save.rewardReceipts[0].id),"cross-account reward rejected "+profile);GameRules.SelectAccount(scopes[i]);}
   }
   GameRules.SelectAccount(scopes[0]);string file=GameRules.SavePath;File.WriteAllText(file,"broken-save");Reject(()=>GuxuExperienceAccounts.LoadRules(config),"corrupt existing save fails closed");Require(File.ReadAllText(file)=="broken-save"&&File.ReadAllText(file+".invalid")=="broken-save","corrupt data preserved");
   File.Delete(file);Reject(()=>GuxuExperienceAccounts.LoadRules(config),"missing main save with backup refuses reseed");Require(!File.Exists(file),"no replacement save or resource grant after backup-only failure");
   Results.Add("PASS ALL "+DateTimeOffset.Now.ToString("O"));
  }finally{
   foreach(string scope in scopes){GameRules.SelectAccount(scope);foreach(string path in Directory.GetFiles(Application.persistentDataPath,Path.GetFileName(GameRules.SavePath)+"*"))File.Delete(path);}foreach(var file in files)File.WriteAllBytes(file.Key,file.Value);GameRules.SelectAccount("");GuxuPhoneTestLogin.Enabled=true;Results.Add("RESTORED isolated .NET files; no Unity/Android player data touched");File.WriteAllLines(Path.Combine(evidence,"seed-login-dotnet-check.txt"),Results);
  }
  Console.WriteLine(string.Join("\n",Results));
 }
}
