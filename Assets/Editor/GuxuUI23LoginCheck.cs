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
using UnityEngine.EventSystems;
using UnityEngine.UI;

public static class GuxuUI23LoginCheck {
 static string Output="Reference/UIRedesign20261110/login-r02";
 const string Consent="GXDL.Privacy.Accepted";
 static readonly BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
 [MenuItem("GXDL/QA/UI23 Login States")]
 public static void Run()=>RunAt("Reference/UIRedesign20261110/login-r02");
 [MenuItem("GXDL/QA/UIStyle Login States")]
 public static void Style(){string output="Reference/UIStyleRework20261111/sessions/login-"+DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fff");RunAt(output);File.WriteAllText("Reference/UIStyleRework20261111/login-session.txt",Path.GetFullPath(output));}
 static void RunAt(string output){if(!Application.isPlaying||GxdlGame.Instance.EntryStage!=GuxuEntryStage.Login)throw new Exception("Start ordinary login Play first.");Output=output;new GameObject("UI23 Login QA").AddComponent<GuxuAccountCheckRunner>().StartCoroutine(Guard(Check()));}
 static IEnumerator Guard(IEnumerator routine){
  var stack=new Stack<IEnumerator>();stack.Push(routine);
  try{while(stack.Count>0){bool moved;object current=null;try{moved=stack.Peek().MoveNext();if(moved)current=stack.Peek().Current;}catch(Exception e){File.WriteAllText(Output+"/failure.txt",e.ToString());Debug.LogException(e);yield break;}if(!moved){(stack.Pop() as IDisposable)?.Dispose();continue;}if(current is IEnumerator child)stack.Push(child);else yield return current;}}
  finally{while(stack.Count>0)(stack.Pop() as IDisposable)?.Dispose();}
 }
 static void Require(bool value,string message){if(!value)throw new Exception("Login binding QA: "+message);}
 static InputField Field(string name)=>UnityEngine.Object.FindObjectsOfType<InputField>().Single(f=>f.name==name);
 static Button Button(string name)=>UnityEngine.Object.FindObjectsOfType<Button>().Single(f=>f.name==name);
 static IEnumerator Shot(string name){yield return new WaitForEndOfFrame();var texture=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes(Output+"/"+name+".png",texture.EncodeToPNG());UnityEngine.Object.Destroy(texture);}
 static IEnumerator Click(string name){
  yield return new WaitForEndOfFrame();var b=Button(name);Require(b.interactable,"enabled "+name);Canvas.ForceUpdateCanvases();var rect=(RectTransform)b.transform;var pointer=new PointerEventData(EventSystem.current){position=RectTransformUtility.WorldToScreenPoint(b.GetComponentInParent<Canvas>().worldCamera,rect.TransformPoint(rect.rect.center)),button=PointerEventData.InputButton.Left};var hits=new List<RaycastResult>();EventSystem.current.RaycastAll(pointer,hits);Require(hits.Count>0&&ExecuteEvents.GetEventHandler<IPointerClickHandler>(hits[0].gameObject)==b.gameObject,"unblocked "+name);ExecuteEvents.Execute(b.gameObject,pointer,ExecuteEvents.pointerClickHandler);yield return null;
 }
 static IEnumerator Check(){
  Directory.CreateDirectory(Output);bool had=PlayerPrefs.HasKey(Consent);string original=PlayerPrefs.GetString(Consent,"");var account=UnityEngine.Object.FindObjectOfType<GuxuAccountSession>();bool consent=account.Consented;
  var window=EditorWindow.GetWindow(typeof(Editor).Assembly.GetType("UnityEditor.GameView"));var sizeProperty=window.GetType().GetProperty("selectedSizeIndex",BindingFlags.Public|Private);int priorSize=(int)sizeProperty.GetValue(window);
  double originalResend=(double)typeof(GuxuAccountSession).GetField("resendAt",Private).GetValue(account);
  var records=new List<object>();bool pass=false;
  try{
   foreach(int height in new[]{1334,1624}){
    typeof(GuxuBattlePreparationCheck).GetMethod("SetSize",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{750,height});yield return null;yield return null;
    account.SetConsent(false);typeof(GxdlGame).GetMethod("RenderAccountState",Private).Invoke(GxdlGame.Instance,null);
    Field("Account Phone").text="";Field("Account Code").text="";yield return Shot("27-login-"+height);
    Field("Account Phone").text="100";Field("Account Code").text="000123";yield return Click("Account Send Code");
    Require(account.Message.Contains("同意")&&!account.IsBusy&&!account.CanPlay,"real missing-consent error");yield return Shot("27-consent-error-"+height);
    yield return Click("Account Consent");Require(account.Consented&&account.Message=="请选择登录方式"&&!UnityEngine.Object.FindObjectsOfType<Text>().Any(t=>t.gameObject.activeInHierarchy&&t.text=="请先阅读并同意用户协议和隐私政策"),"consent checkbox and stale error update together");yield return Shot("27-consent-cleared-"+height);Field("Account Phone").text="100";yield return Click("Account Send Code");
    Require(account.Message.Contains("11位")&&!account.IsBusy&&!account.CanPlay,"real invalid-phone error without SMS");Require(Field("Account Code").text=="000123","input drafts and leading zero retained");yield return Shot("27-invalid-phone-"+height);
    Field("Account Phone").text="13800000000";Field("Account Code").text="000123";yield return Click("Account Phone Login");
    Require(account.Message.Contains("获取验证码")&&!account.CanPlay,"real no-challenge login rejection");yield return Shot("27-no-challenge-"+height);
    // Only the local cooldown clock is seeded; no SMS/auth request or successful account state is synthesized.
    typeof(GuxuAccountSession).GetField("resendAt",Private).SetValue(account,Time.realtimeSinceStartupAsDouble+2.5);
    yield return null;int remaining=account.ResendSeconds;Require(!Button("Account Send Code").interactable&&Button("Account Send Code").GetComponentInChildren<Text>().text==remaining+"秒后重发","countdown label binds clock and disables action");yield return Shot("27-cooldown-fixture-"+height);yield return new WaitForSeconds(2.7f);
    Require(account.ResendSeconds==0&&Button("Account Send Code").interactable,"cooldown re-enables exactly when clock expires");
    records.Add(new{height,missingConsent="actual handler",consentRefresh="checked state and stale error updated together",invalidPhone="actual handler; no network",missingChallenge="actual handler; no network",countdown="isolated local resendAt clock fixture; no SMS sent",authenticated=account.CanPlay});
   }
   pass=true;
  }finally{
   typeof(GuxuAccountSession).GetField("resendAt",Private).SetValue(account,originalResend);account.SetConsent(consent);if(had)PlayerPrefs.SetString(Consent,original);else PlayerPrefs.DeleteKey(Consent);PlayerPrefs.Save();sizeProperty.SetValue(window,priorSize);typeof(GxdlGame).GetMethod("RenderAccountState",Private).Invoke(GxdlGame.Instance,null);
   File.WriteAllText(Output+"/login-states.json",JsonConvert.SerializeObject(new{status=pass?"PASS":"FAIL",checkedAt=DateTime.UtcNow.ToString("O"),consentRestored=PlayerPrefs.HasKey(Consent)==had&&PlayerPrefs.GetString(Consent,"")==original,records,authentication="not attempted; formal Android SDK/server auth remains unchanged"},Formatting.Indented));
  }
 }
}


