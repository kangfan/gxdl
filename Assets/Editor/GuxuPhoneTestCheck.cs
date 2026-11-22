using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Gxdl;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[InitializeOnLoad]
public static class GuxuPhoneTestCheck {
 const string Key="GXDL.Accounts.PhoneTest",RunKey="GXDL.Accounts.PhoneTestQA";
 const string Receipt=GuxuPhoneTestBuild.Evidence+"/phone-test-qa.txt";
 static bool running;
 static GuxuPhoneTestCheck(){EditorApplication.update+=Monitor;EditorApplication.playModeStateChanged+=Restore;}
 [MenuItem("GXDL/Accounts/Verify Phone Test Login")]
 public static void Run(){
  if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Stop Play Mode first.");
  string backupDirectory=GuxuPhoneTestBuild.Evidence+"/qa-save-backup-"+Guid.NewGuid().ToString("N");Directory.CreateDirectory(backupDirectory);SessionState.SetString(RunKey+".backup",backupDirectory);
  string previous=GameRules.AccountScope;GameRules.SelectAccount(GuxuAccountConfig.Hash(GuxuPhoneTestLogin.Identity));
  SessionState.SetString(RunKey+".path",GameRules.SavePath);SessionState.SetString(RunKey+".scope",previous);
  foreach(string path in Directory.GetFiles(Path.GetDirectoryName(GameRules.SavePath),Path.GetFileName(GameRules.SavePath)+"*"))File.Copy(path,backupDirectory+"/"+Path.GetFileName(path),true);
  foreach(string key in new[]{GxdlGame.OpeningStoryKey,GxdlGame.FirstUnsealStoryKey}){SessionState.SetInt(RunKey+key,PlayerPrefs.GetInt(key,-1));PlayerPrefs.DeleteKey(key);}
  GameRules.SelectAccount(previous);PlayerPrefs.Save();File.WriteAllText(Receipt,"RUNNING "+DateTimeOffset.Now.ToString("O")+"\n");
  SessionState.SetBool(Key,true);SessionState.SetBool(RunKey,true);SessionState.SetFloat(RunKey+".deadline",(float)EditorApplication.timeSinceStartup+75);GuxuAccountChecks.Preview();
 }
 static void Monitor(){
  if(!SessionState.GetBool(RunKey,false))return;
  if(EditorApplication.isPlaying&&GxdlGame.Instance&&GxdlGame.Instance.GetComponent<GuxuAccountSession>()&&GxdlGame.Instance.EntryStage==GuxuEntryStage.Login&&!running){running=true;new GameObject("Phone Test QA").AddComponent<GuxuAccountCheckRunner>().StartCoroutine(Guard(Check()));}
  if(EditorApplication.timeSinceStartup>SessionState.GetFloat(RunKey+".deadline",0)){File.AppendAllText(Receipt,"FAIL timeout\n");EditorApplication.isPlaying=false;}
 }
 static void Restore(PlayModeStateChange state){
  if(state!=PlayModeStateChange.EnteredEditMode)return;
  SessionState.SetBool(Key,false);
  if(!SessionState.GetBool(RunKey,false))return;
  string path=SessionState.GetString(RunKey+".path","");
  foreach(string candidate in Directory.GetFiles(Path.GetDirectoryName(path),Path.GetFileName(path)+"*"))File.Delete(candidate);
  var backups=Directory.GetFiles(SessionState.GetString(RunKey+".backup",""));
  foreach(string backup in backups)File.Copy(backup,Path.Combine(Path.GetDirectoryName(path),Path.GetFileName(backup)),true);
  Require(Directory.GetFiles(Path.GetDirectoryName(path),Path.GetFileName(path)+"*").Length==backups.Length&&backups.All(backup=>File.ReadAllBytes(backup).SequenceEqual(File.ReadAllBytes(Path.Combine(Path.GetDirectoryName(path),Path.GetFileName(backup))))),"original test save file set and bytes restored");
  GameRules.SelectAccount(GuxuAccountConfig.Hash(GuxuPhoneTestLogin.Identity));
  foreach(string key in new[]{GxdlGame.OpeningStoryKey,GxdlGame.FirstUnsealStoryKey}){int saved=SessionState.GetInt(RunKey+key,-1);if(saved<0)PlayerPrefs.DeleteKey(key);else PlayerPrefs.SetInt(key,saved);}
  GameRules.SelectAccount(SessionState.GetString(RunKey+".scope",""));PlayerPrefs.Save();SessionState.SetBool(RunKey,false);running=false;
  File.AppendAllText(Receipt,"RESTORED test account save, story flags, test switch and Editor state "+DateTimeOffset.Now.ToString("O")+"\n");
 }
 static IEnumerator Guard(IEnumerator routine){
  var stack=new Stack<IEnumerator>();stack.Push(routine);
  try{while(stack.Count>0){var current=stack.Peek();bool more;object next;try{more=current.MoveNext();next=more?current.Current:null;}catch(Exception error){File.AppendAllText(Receipt,"FAIL "+error+"\n");Debug.LogException(error);yield break;}if(!more){(stack.Pop() as IDisposable)?.Dispose();continue;}if(next is IEnumerator nested){stack.Push(nested);continue;}yield return next;}}
  finally{while(stack.Count>0)(stack.Pop() as IDisposable)?.Dispose();EditorApplication.isPlaying=false;}
 }
 static void Require(bool value,string label){if(!value)throw new Exception(label);File.AppendAllText(Receipt,"PASS "+label+"\n");}
 static void Click(string name){
  var button=UnityEngine.Object.FindObjectsOfType<Button>().Single(b=>b.name==name);Canvas.ForceUpdateCanvases();
  var canvas=button.GetComponentInParent<Canvas>();var rect=(RectTransform)button.transform;
  var point=RectTransformUtility.WorldToScreenPoint(canvas.renderMode==RenderMode.ScreenSpaceOverlay?null:canvas.worldCamera,rect.TransformPoint(rect.rect.center));
  var pointer=new PointerEventData(EventSystem.current){position=point,button=PointerEventData.InputButton.Left};var hits=new List<RaycastResult>();EventSystem.current.RaycastAll(pointer,hits);
  Require(button.interactable&&hits.Count>0&&hits[0].gameObject.GetComponentInParent<Button>()==button,"click reachable: "+name);ExecuteEvents.Execute(button.gameObject,pointer,ExecuteEvents.pointerClickHandler);
 }
 static void Input(string name,string value)=>UnityEngine.Object.FindObjectsOfType<InputField>().Single(f=>f.name==name).text=value;
 static IEnumerator Until(Func<bool> condition,string label){float end=Time.realtimeSinceStartup+15;while(!condition()&&Time.realtimeSinceStartup<end)yield return null;Require(condition(),label);}
 static IEnumerator Check(){
  yield return null;var game=GxdlGame.Instance;var account=game.GetComponent<GuxuAccountSession>();
  Require(GuxuPhoneTestLogin.Enabled&&!game.CanPlay&&game.EntryStage==GuxuEntryStage.Login,"test enabled but startup still requires login");
  Require(!GuxuPhoneTestLogin.Accepts("13527645605","123456\n")&&!GuxuPhoneTestLogin.Accepts("13527645605","１２３４５６")&&!GuxuPhoneTestLogin.Accepts(null,"123456"),"strict credential validation");
  account.LoginTap();Require(!account.CanPlay,"TapTap cannot bypass designated test phone");yield return null;
  Input("Account Phone","13800138000");Input("Account Code","123456");Click("Account Phone Login");Require(!account.CanPlay&&game.EntryStage==GuxuEntryStage.Login,"wrong phone rejected");yield return null;
  Input("Account Phone","13527645605");Input("Account Code","654321");Click("Account Phone Login");Require(!account.CanPlay&&game.EntryStage==GuxuEntryStage.Login,"wrong code rejected");yield return null;
  Click("Account Send Code");Require(account.Message.Contains("不发送短信")&&!account.IsBusy,"get code honestly reports no SMS");yield return null;
  Input("Account Code","123456");Click("Account Phone Login");Require(account.CanPlay&&!game.CanPlay&&game.EntryStage==GuxuEntryStage.Loading,"fixed credentials immediately enter real Loading without a challenge");
  string scope=account.SaveScope;Require(scope==GuxuAccountConfig.Hash(GuxuPhoneTestLogin.Identity)&&scope!=GuxuAccountConfig.Hash("editor:tap-preview"),"stable test identity isolated from other accounts");
  yield return Until(()=>game.EntryStage==GuxuEntryStage.Opening&&game.IsStoryPlaying,"Loading completes before first opening");yield return new WaitForSecondsRealtime(.3f);Click("Skip Story");yield return Until(()=>game.CanPlay,"skip opening enters Home");
  foreach(string field in new[]{"sdkReady","hasComplianceSession","initCallbackRegistered"})Require(!(bool)typeof(GuxuAccountSession).GetField(field,BindingFlags.Instance|BindingFlags.NonPublic).GetValue(account),"no native authentication: "+field);
  long coins=game.Rules.Save.coins;game.Rules.Store();
  account.SendMessage("OnApplicationPause",true);account.SendMessage("OnApplicationPause",false);Require(game.CanPlay,"pause and resume retain local test session");
  account.Logout();Require(!game.CanPlay&&game.EntryStage==GuxuEntryStage.Login,"logout returns to locked login");yield return null;
  Input("Account Phone","13527645605");Input("Account Code","123456");Click("Account Phone Login");yield return Until(()=>game.EntryStage==GuxuEntryStage.Opening,"returning Editor login replays opening");yield return new WaitForSecondsRealtime(.3f);Click("Skip Story");yield return Until(()=>game.CanPlay,"second login completes");
  Require(account.SaveScope==scope&&game.Rules.Save.coins==coins,"same test account reloads its saved progress");
  Require(GuxuAccountConfig.Load().Validate()!=null&&!PlayerSettings.GetScriptingDefineSymbolsForGroup(BuildTargetGroup.Android).Split(';').Contains(GuxuPhoneTestBuild.Define),"release config gate unchanged and test define not persisted");
  File.AppendAllText(Receipt,"PASS ALL "+DateTimeOffset.Now.ToString("O")+"\n");
 }
}
