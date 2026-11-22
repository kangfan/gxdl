using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using Gxdl;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;

// Internal login remains real; this fixture only backs up and restores local test data.
[InitializeOnLoad] public static class GuxuFeedbackSocialReview {
 const string Active="GXDL.Feedback.SocialReview",PhoneTest="GXDL.Accounts.PhoneTest",Offline="GXDL.Accounts.OfflinePreview";
 [Serializable] sealed class Pref {public string key;public bool exists;public int value;}
 [Serializable] sealed class Account {public string path,backup;public string[] files;public Pref[] prefs;}
 [Serializable] sealed class Snapshot {public string scope,privacy;public bool hadPrivacy,offline;public int gameView;public Account[] accounts;}
 static readonly BindingFlags Flags=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
 static GuxuFeedbackSocialReview(){EditorApplication.playModeStateChanged+=s=>{if(s==PlayModeStateChange.EnteredEditMode&&!string.IsNullOrEmpty(SessionState.GetString(Active,"")))EditorApplication.delayCall+=Restore;};}
 [MenuItem("GXDL/QA/Submission Social Preview")]
 public static void Begin(){
  if(EditorApplication.isPlayingOrWillChangePlaymode||SessionState.GetBool(PhoneTest,false)||!string.IsNullOrEmpty(SessionState.GetString(Active,"")))throw new InvalidOperationException("Stop Play and finish prior internal-test fixtures first.");
  string directory=Path.GetFullPath("Reference/SubmissionFeedback20261117/social-review/"+DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fff"));Directory.CreateDirectory(directory);
  var window=EditorWindow.GetWindow(typeof(Editor).Assembly.GetType("UnityEditor.GameView"));var size=window.GetType().GetProperty("selectedSizeIndex",Flags);
  var snapshot=new Snapshot{scope=GameRules.AccountScope,hadPrivacy=PlayerPrefs.HasKey("GXDL.Privacy.Accepted"),privacy=PlayerPrefs.GetString("GXDL.Privacy.Accepted",""),offline=SessionState.GetBool(Offline,false),gameView=(int)size.GetValue(window)};
  var records=new List<Account>();
  try{
   foreach(string scope in new[]{GuxuExperienceAccounts.Scope("new"),GuxuExperienceAccounts.Scope("mid"),GuxuExperienceAccounts.Scope("high"),GuxuAccountConfig.Hash(GuxuPhoneTestLogin.Identity)}){
    GameRules.SelectAccount(scope);string path=GameRules.SavePath,backup=Path.Combine(directory,scope);Directory.CreateDirectory(backup);
    string[] files=Directory.Exists(Path.GetDirectoryName(path))?Directory.GetFiles(Path.GetDirectoryName(path),Path.GetFileName(path)+"*"):new string[0];
    foreach(string file in files)File.Copy(file,Path.Combine(backup,Path.GetFileName(file)));
    records.Add(new Account{path=path,backup=backup,files=files.Select(Path.GetFileName).ToArray(),prefs=new[]{GxdlGame.OpeningStoryKey,GxdlGame.FirstUnsealStoryKey}.Select(k=>new Pref{key=k,exists=PlayerPrefs.HasKey(k),value=PlayerPrefs.GetInt(k,0)}).ToArray()});
   }
  }finally{GameRules.SelectAccount(snapshot.scope);}
  snapshot.accounts=records.ToArray();File.WriteAllText(Path.Combine(directory,"recovery.json"),JsonConvert.SerializeObject(snapshot,Formatting.Indented));
  SessionState.SetString(Active,directory);SessionState.SetBool(PhoneTest,true);File.WriteAllText("Reference/SubmissionFeedback20261117/social-review-session.txt",directory);GuxuAccountChecks.Preview();
 }
 [MenuItem("GXDL/QA/Submission Social Preview End")]
 public static void End(){if(EditorApplication.isPlaying){GuxuUI23Review.End();EditorApplication.isPlaying=false;}else Restore();}
 static void Restore(){
  string directory=SessionState.GetString(Active,"");if(string.IsNullOrEmpty(directory))return;
  var snapshot=JsonConvert.DeserializeObject<Snapshot>(File.ReadAllText(Path.Combine(directory,"recovery.json")));
  foreach(var account in snapshot.accounts){
   string parent=Path.GetFullPath(Path.GetDirectoryName(account.path)),persistent=Path.GetFullPath(Application.persistentDataPath);
   if(!string.Equals(parent,persistent,StringComparison.OrdinalIgnoreCase)||!Path.GetFileName(account.path).StartsWith("gxdl-account-"))throw new InvalidOperationException("Unexpected social fixture restore path");
   foreach(string file in Directory.Exists(parent)?Directory.GetFiles(parent,Path.GetFileName(account.path)+"*"):new string[0])if(!account.files.Contains(Path.GetFileName(file)))File.Delete(file);
   foreach(string file in account.files){string original=Path.Combine(parent,file),backup=Path.Combine(account.backup,file);File.Copy(backup,original,true);if(!File.ReadAllBytes(backup).SequenceEqual(File.ReadAllBytes(original)))throw new IOException("Social review save restore mismatch");}
   foreach(var pref in account.prefs){if(pref.exists)PlayerPrefs.SetInt(pref.key,pref.value);else PlayerPrefs.DeleteKey(pref.key);}
  }
  if(snapshot.hadPrivacy)PlayerPrefs.SetString("GXDL.Privacy.Accepted",snapshot.privacy);else PlayerPrefs.DeleteKey("GXDL.Privacy.Accepted");PlayerPrefs.Save();
  var window=EditorWindow.GetWindow(typeof(Editor).Assembly.GetType("UnityEditor.GameView"));window.GetType().GetProperty("selectedSizeIndex",Flags).SetValue(window,snapshot.gameView);
  GameRules.SelectAccount(snapshot.scope);SessionState.SetBool(PhoneTest,false);SessionState.SetBool(Offline,snapshot.offline);SessionState.EraseString(Active);
  File.WriteAllText(Path.Combine(directory,"restored.txt"),"RESTORED original test save file set/bytes, four account story preferences, consent, scope, GameView and test switches. "+DateTimeOffset.Now.ToString("O"));
 }
}
