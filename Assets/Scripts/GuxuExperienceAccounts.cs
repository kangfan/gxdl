using System;
using System.IO;
using Newtonsoft.Json;
using UnityEngine;

namespace Gxdl {
public static class GuxuExperienceAccounts {
 public static string Identity(string profile)=>"device-test:experience:v1:"+profile;
 public static string Scope(string profile)=>GuxuAccountConfig.Hash(Identity(profile));
 public static string Authenticate(string username,string password){
#if GXDL_PHONE_TEST || UNITY_EDITOR
  if(!GuxuPhoneTestLogin.Enabled)return null;
  if(username=="gxdl_new"&&password=="GxdlNew123!")return "new";
  if(username=="gxdl_mid"&&password=="GxdlMid123!")return "mid";
  if(username=="gxdl_high"&&password=="GxdlHigh123!")return "high";
#endif
  return null;
 }
 public static string Profile(string scope){
  if(!GuxuPhoneTestLogin.Enabled)return null;
  foreach(string profile in new[]{"new","mid","high"})if(Scope(profile)==scope)return profile;
  return null;
 }
 public static string Label(string profile)=>profile=="new"?"新手体验账号":profile=="mid"?"中级体验账号":"高级体验账号";
 public static GameRules LoadRules(OriginalConfig config){
  string profile=Profile(GameRules.AccountScope);
  if(profile==null)return new GameRules(config,GameRules.ReadSave());
  if(File.Exists(GameRules.SavePath)){
   var existing=GameRules.ReadSave();
   if(existing==null)throw new InvalidDataException("体验账号存档损坏，已保留原文件，请恢复备份后重试。");
   return new GameRules(config,existing);
  }
  // An interrupted first write must be recovered deliberately, never replaced by a fresh grant.
  if(Directory.Exists(Path.GetDirectoryName(GameRules.SavePath))&&Directory.GetFiles(Path.GetDirectoryName(GameRules.SavePath),Path.GetFileName(GameRules.SavePath)+".*").Length>0)throw new InvalidDataException("体验账号存在未恢复的存档文件，保留原文件供恢复。");
  SaveData initial=null;
  if(profile!="new"){
   var asset=Resources.Load<TextAsset>("Game/ExperienceAccounts/"+profile);
   if(!asset)throw new InvalidDataException("Missing experience account: "+profile);
   initial=JsonConvert.DeserializeObject<SaveData>(asset.text);GameRules.ValidateSave(initial);
   if(initial.rewardReceipts!=null)foreach(var receipt in initial.rewardReceipts)if(receipt.accountScope!=GameRules.AccountScope)throw new InvalidDataException("Experience receipt scope mismatch");
   initial.rewardDay=GameRules.Day(DateTime.UtcNow);
  }
  var rules=new GameRules(config,initial);rules.EnsureExpansion();rules.Store();return rules;
 }
}
}
