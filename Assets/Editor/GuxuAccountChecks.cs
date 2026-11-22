using System;
using System.Collections;
using System.IO;
using System.Linq;
using Gxdl;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public sealed class GuxuAccountBuildCheck:IPreprocessBuildWithReport {
 public int callbackOrder=>-1000;
 public void OnPreprocessBuild(BuildReport report){
  if(report.summary.platform!=BuildTarget.Android)return;
  if(GuxuPhoneTestBuild.ValidateBuild(report))return;
  var config=GuxuAccountConfig.Load();string error=config.Validate();
  if(error!=null)throw new BuildFailedException("账号接入未配置完成："+error+"。配置 Assets/Resources/Game/AccountConfig.json 后再构建。");
  if(PlayerSettings.GetApplicationIdentifier(BuildTargetGroup.Android)!=config.androidPackageName)throw new BuildFailedException("Android 包名与 AccountConfig 不一致，请与 TapTap 后台登记信息保持一致。");
 }
}
[InitializeOnLoad]
public static class GuxuAccountChecks {
 const string OfflineKey="GXDL.Accounts.OfflinePreview",QaKey="GXDL.Accounts.RunQA";
 static bool running;
 static GuxuAccountChecks(){EditorApplication.update+=Monitor;EditorApplication.playModeStateChanged+=state=>{if(state==PlayModeStateChange.EnteredEditMode){SessionState.SetBool(OfflineKey,false);SessionState.SetBool(QaKey,false);running=false;GameRules.SelectAccount("");}};}
 static void Require(bool condition,string label){if(!condition)throw new Exception("Account check failed: "+label);}
 [MenuItem("GXDL/Accounts/Validate Account Rules")]
 public static void Validate(){
  bool offline=SessionState.GetBool(OfflineKey,false);
  try{
   SessionState.SetBool(OfflineKey,false);Require(GuxuAccountSession.RequireLogin,"normal Play requires login");
   SessionState.SetBool(OfflineKey,true);Require(!GuxuAccountSession.RequireLogin,"only explicit Editor preview bypasses login");
  }finally{SessionState.SetBool(OfflineKey,offline);}
  Require(!new GuxuAccountGate().CanPlay,"locked at startup");
  foreach(int code in new[]{1000,1001,1030,1050,1100,1200,9002,-1}){
   var gate=new GuxuAccountGate();gate.Begin();Require(gate.Receive(500)&&gate.CanPlay,"certification opens gate");
   gate.Receive(code);Require(!gate.CanPlay&&!gate.Certifying,"restriction blocks play: "+code);
   gate.Receive(500);Require(!gate.CanPlay,"late success cannot bypass restriction: "+code);
   gate.Begin();gate.Receive(code);Require(!gate.CanPlay,"failed initial certification: "+code);
  }
  var pending=new GuxuAccountGate();pending.Receive(500);Require(!pending.CanPlay,"unsolicited success rejected");pending.Begin();pending.Reset();pending.Receive(500);Require(!pending.CanPlay,"success after logout rejected");
  Require(GuxuAccountConfig.Phone("13800138000")&&!GuxuAccountConfig.Phone("1380013800")&&!GuxuAccountConfig.Phone("１３８００１３８０００"),"phone validation");
  Require(GuxuAccountConfig.Code("000123")&&!GuxuAccountConfig.Code("１２３４５６")&&!GuxuAccountConfig.Code("12345")&&!GuxuAccountConfig.Code("123456\n")&&!GuxuAccountConfig.Phone("13800138000\n"),"OTP is six ASCII digits including leading zeros");
  Require(!GuxuAccountConfig.Https("http://accounts.example.com")&&!GuxuAccountConfig.Https("https://user:secret@accounts.example.com")&&GuxuAccountConfig.Https("https://accounts.example.com"),"HTTPS boundary");
  Require(new GuxuAccountConfig().Validate()!=null,"missing config blocks build");
  string old=GameRules.AccountScope;
  try{
   GameRules.SelectAccount("");string local=GameRules.SavePath;
   GameRules.SelectAccount(GuxuAccountConfig.Hash("tap:client:a"));string first=GameRules.SavePath;
   GameRules.SelectAccount(GuxuAccountConfig.Hash("phone:server:a"));Require(first!=GameRules.SavePath&&local!=first,"provider-scoped saves");
   bool rejected=false;try{GameRules.SelectAccount("../../escape");}catch(ArgumentException){rejected=true;}Require(rejected,"save path traversal rejected");
  }finally{GameRules.SelectAccount(old);}
  Directory.CreateDirectory("Logs/accounts");File.WriteAllText("Logs/accounts/rules.txt","PASS: login by default, explicit Editor-only offline preview, pending, success, all restriction codes, late callbacks, logout, phone/OTP validation, HTTPS, configuration, account save isolation.");
  Debug.Log("[Accounts QA] Rule checks passed.");
 }
 [MenuItem("GXDL/Accounts/Preview Login")]
 public static void Preview(){StartPreview(false);}
 [MenuItem("GXDL/Accounts/Preview Gameplay (Offline)")]
 public static void PreviewGameplay(){StartPreview(true);}
 static void StartPreview(bool offline){if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Stop Play Mode first");SessionState.SetBool(OfflineKey,offline);EditorApplication.isPlaying=true;}
 [MenuItem("GXDL/Accounts/Verify Login Screen")]
 public static void Run(){Validate();if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Stop Play Mode first");Require(GuxuAccountSession.RequireLogin,"ordinary Play returns to login after preview");SessionState.SetBool(QaKey,true);EditorApplication.isPlaying=true;}
 [MenuItem("GXDL/Accounts/Click Tap Login (Editor Test)")]
 public static void ClickTapLogin(){
  Require(EditorApplication.isPlaying&&GxdlGame.Instance&&GxdlGame.Instance.EntryStage==GuxuEntryStage.Login,"start at the login screen");
  var tap=UnityEngine.Object.FindObjectsOfType<Button>().Single(b=>b.name=="Account Tap Login");
  Require(tap.interactable,"Tap button can be clicked");tap.onClick.Invoke();
  var game=GxdlGame.Instance;var account=game.GetComponent<GuxuAccountSession>();
  Require(account.CanPlay&&account.SaveScope==GuxuAccountConfig.Hash("editor:tap-preview"),"test identity selected");
  Require(game.EntryStage==GuxuEntryStage.Loading&&!game.CanPlay&&UnityEngine.Object.FindObjectOfType<GuxuLoadingScreen>(),"actual button starts real Loading");
  Directory.CreateDirectory("Logs/accounts");File.WriteAllText("Logs/accounts/tap-preview.txt","RUNNING");
  new GameObject("Tap Login Check").AddComponent<GuxuAccountCheckRunner>().StartCoroutine(CheckTapLogin(game,account,PlayerPrefs.GetInt(GxdlGame.OpeningStoryKey,0)==0));
 }
 static IEnumerator CheckTapLogin(GxdlGame game,GuxuAccountSession account,bool first){
  bool opening=false;float deadline=Time.realtimeSinceStartup+50;
  while(game&&!game.CanPlay&&Time.realtimeSinceStartup<deadline){opening|=game.EntryStage==GuxuEntryStage.Opening&&game.IsStoryPlaying;yield return null;}
  try{
   Require(game&&game.CanPlay&&game.EntryStage==GuxuEntryStage.Ready,"loading and opening finish at Home");
   Require(opening,"Editor plays opening on every login, including a previously watched account");
   Require(PlayerPrefs.GetInt(GxdlGame.OpeningStoryKey,0)==1,"opening completion saved");
   var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
   foreach(string name in new[]{"sdkReady","hasComplianceSession","initCallbackRegistered"})Require(!(bool)typeof(GuxuAccountSession).GetField(name,flags).GetValue(account),"no SDK initialization: "+name);
   Require(UnityEngine.Object.FindObjectOfType<GuxuLoadingScreen>()==null&&GameObject.Find("Seeker Character Layer"),"loading removed and Home visible");
   File.WriteAllText("Logs/accounts/tap-preview.txt","PASS: clicked actual Account Tap Login; isolated Editor test account; production Loading; "+(first?"natural first opening; ":"previously watched account replays complete opening in Editor; ")+"Home visible; no SDK initialization or compliance session. "+DateTime.Now.ToString("O"));
  }catch(Exception error){File.WriteAllText("Logs/accounts/tap-preview.txt","FAIL: "+error);Debug.LogException(error);}
 }
 static void Monitor(){if(!SessionState.GetBool(QaKey,false)||running||!EditorApplication.isPlaying||!GxdlGame.Instance)return;running=true;new GameObject("Account QA").AddComponent<GuxuAccountCheckRunner>().StartCoroutine(Check());}
 static IEnumerator Check(){
  yield return null;
  try{
   var game=GxdlGame.Instance;Require(!game.CanPlay&&game.Rules==null,"game not loaded before login");
   game.Home();game.Chop();game.ResolveDrop(true);game.StartBattle();Require(game.Rules==null,"gameplay entrypoints cannot bypass login");
   var buttons=UnityEngine.Object.FindObjectsOfType<Button>();
   Require(buttons.Any(b=>b.name=="Account Tap Login")&&buttons.Any(b=>b.name=="Account Phone Login")&&buttons.Any(b=>b.name=="Account Send Code"),"both login methods visible");
   var fields=UnityEngine.Object.FindObjectsOfType<InputField>();Require(fields.Length==2,"independent phone and OTP inputs");
   Require(fields.Single(f=>f.name=="Account Phone").characterLimit==11&&fields.Single(f=>f.name=="Account Code").characterLimit==6,"input limits");
   foreach(string name in new[]{"Account Tap Login","Account Phone Login","Account Send Code"}){
    var button=buttons.Single(b=>b.name==name);var image=(Image)button.targetGraphic;
    Require(button.transition==Selectable.Transition.ColorTint,"live button tint states: "+name);
    var savedColors=button.colors;var immediate=savedColors;immediate.fadeDuration=0;button.colors=immediate;
    var pointer=new PointerEventData(EventSystem.current){button=PointerEventData.InputButton.Left};
    ExecuteEvents.Execute(button.gameObject,pointer,ExecuteEvents.pointerEnterHandler);
    Require(Vector4.Distance(image.canvasRenderer.GetColor(),immediate.highlightedColor*immediate.colorMultiplier)<.01f,"hover tint: "+name);
    ExecuteEvents.Execute(button.gameObject,pointer,ExecuteEvents.pointerDownHandler);
    Require(Vector4.Distance(image.canvasRenderer.GetColor(),immediate.pressedColor*immediate.colorMultiplier)<.01f,"pressed tint: "+name);
    ExecuteEvents.Execute(button.gameObject,pointer,ExecuteEvents.pointerUpHandler);
    ExecuteEvents.Execute(button.gameObject,pointer,ExecuteEvents.pointerExitHandler);EventSystem.current.SetSelectedGameObject(null);
    button.interactable=false;Require(Vector4.Distance(image.canvasRenderer.GetColor(),immediate.disabledColor*immediate.colorMultiplier)<.01f,"disabled tint: "+name);button.interactable=true;button.colors=savedColors;
   }
   var otp=fields.Single(f=>f.name=="Account Code");otp.SetTextWithoutNotify("000123");Require(otp.text=="000123","leading zeros remain editable");otp.SetTextWithoutNotify("");
   Require(buttons.Single(b=>b.name=="Account Send Code").GetComponentsInChildren<Text>().Length==1,"one live countdown label");
   File.WriteAllText("Logs/accounts/login-ui.txt","PASS: ordinary Play starts at login without a login-preview override; login gate, public gameplay guards, both login methods, independent input limits, editable leading-zero OTP, all three buttons actual hover/pressed/disabled color transitions, one live countdown label. No authentication request sent.");
   ScreenCapture.CaptureScreenshot(Path.GetFullPath("Logs/accounts/login-screen.png"));Debug.Log("[Accounts QA] Login screen checks passed.");
  }catch(Exception error){File.WriteAllText("Logs/accounts/login-ui.txt","FAIL: "+error);Debug.LogException(error);}
  yield return new WaitForEndOfFrame();yield return null;EditorApplication.isPlaying=false;
 }
}
public sealed class GuxuAccountCheckRunner:MonoBehaviour {}

