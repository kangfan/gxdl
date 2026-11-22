using System;
using System.Threading.Tasks;
using TapSDK.Core;
using TapSDK.Login;
using TapSDK.Compliance;
using UnityEngine;

namespace Gxdl {
public sealed partial class GuxuAccountSession:MonoBehaviour,ITapInitCallback {
 const string ConsentRequiredMessage="请先阅读并同意用户协议和隐私政策";
 public event Action Changed;
 public GuxuAccountConfig Config {get;private set;}
 public string Message {get;private set;}="请选择登录方式";
 public string SaveScope {get;private set;}="";
 public string DisplayName {get;private set;}="";
 public bool IsBusy {get;private set;}

 public bool CanPlay=>gate.CanPlay&&!checkingSession;
 public bool CanCertify=>!string.IsNullOrEmpty(playerId)&&!IsBusy&&!ending;
 public int ResendSeconds=>Math.Max(0,(int)Math.Ceiling(resendAt-Time.realtimeSinceStartupAsDouble));
 public bool Consented {get;private set;}
 readonly GuxuAccountGate gate=new GuxuAccountGate();
 string playerId,phoneToken,challengeId,challengePhone;
 double resendAt,sessionExpiresAt,certifyDeadline,exitDeadline;
 bool sdkReady,hasComplianceSession,ending,checkingSession,initCallbackRegistered,quitting;
 int lifetime;
 TaskCompletionSource<bool> initialization;
 static GuxuAccountSession current;

 public static bool RequireLogin {
  get {
#if UNITY_EDITOR
   return !UnityEditor.SessionState.GetBool("GXDL.Accounts.OfflinePreview",false);
#else
   return true;
#endif
  }
 }
 void Awake(){current=this;Config=GuxuAccountConfig.Load();Consented=PlayerPrefs.GetString("GXDL.Privacy.Accepted","")==ConsentKey;}
 string ConsentKey=>GuxuAccountConfig.Hash(Config.clientId+"|"+Config.policyVersion+"|"+Config.termsUrl+"|"+Config.privacyUrl);
 public void SetConsent(bool value){Consented=value;if(value&&Message==ConsentRequiredMessage)Message="请选择登录方式";if(value&&GuxuAccountConfig.Https(Config.termsUrl)&&GuxuAccountConfig.Https(Config.privacyUrl))PlayerPrefs.SetString("GXDL.Privacy.Accepted",ConsentKey);else PlayerPrefs.DeleteKey("GXDL.Privacy.Accepted");PlayerPrefs.Save();}
 void Notify(string message){Message=message;Changed?.Invoke();}
 void RequireConsent(){if(!Consented)throw new GuxuAccountException(ConsentRequiredMessage);}
#pragma warning disable CS1998
 async Task EnsureSdk(){
  RequireConsent();var error=Config.Validate(false);if(error!=null)throw new GuxuAccountException("登录服务暂未开放，请稍后再试");
#if UNITY_EDITOR
  throw new GuxuAccountException("实名认证需在 Android 真机验证，编辑器仅预览登录界面");
#else
  if(Application.platform!=RuntimePlatform.Android)throw new GuxuAccountException("当前版本的账号认证仅支持 Android");
  if(sdkReady)return;
  initialization=new TaskCompletionSource<bool>();initCallbackRegistered=true;TapTapSDK.AddInitCallback(this);
  try{
   TapTapSDK.Init(new TapTapSdkOptions{clientId=Config.clientId,clientToken=Config.clientToken,region=TapTapRegionType.CN,screenOrientation=0,enableLog=false},
    new TapTapSdkBaseOptions[]{new TapTapComplianceOption{showSwitchAccount=true,useAgeRange=false}});
   if(await Task.WhenAny(initialization.Task,Task.Delay(30000))!=initialization.Task)throw new GuxuAccountException("登录服务初始化超时，请重试");
   await initialization.Task;
   if(!this)return;
   sdkReady=true;TapTapCompliance.RegisterComplianceCallback(ComplianceCallback);
  }finally{initCallbackRegistered=false;TapTapSDK.RemoveInitCallback(this);}
#endif
 }
 public void OnInitSuccess(){initialization?.TrySetResult(true);}
 public void OnInitFail(int code,string message){initialization?.TrySetException(new GuxuAccountException("登录服务初始化失败（"+code+"），请稍后重试"));}
 static void ComplianceCallback(int code,string extra){if(current)current.OnCompliance(code);}
#if UNITY_EDITOR
 public void LoginEditorPreview(){
  if(IsBusy||ending||CanPlay||CanCertify)return;
  playerId="editor:tap-preview";DisplayName="编辑器测试玩家";
  SaveScope=GuxuAccountConfig.Hash(playerId);phoneToken=null;
  gate.Begin();gate.Receive(500);Notify("测试登录成功");
 }
#endif
 public async void LoginTap(){
  if(GuxuPhoneTestLogin.Enabled){Notify("此测试包请使用指定手机号和测试验证码登录");return;}
  if(IsBusy||ending||CanPlay||CanCertify)return;IsBusy=true;int attempt=lifetime;Notify("正在连接 TapTap……");
  try{
   await EnsureSdk();if(!this||attempt!=lifetime)return;
   var account=await TapTapLogin.Instance.GetCurrentTapAccount();
   if(account==null)account=await TapTapLogin.Instance.LoginWithScopes(new[]{TapTapLogin.TAP_LOGIN_SCOPE_BASIC_INFO});
   if(!this||attempt!=lifetime)return;
   if(account==null||string.IsNullOrWhiteSpace(account.openId))throw new GuxuAccountException("未获取到有效账号，请重试");
   playerId=account.openId;DisplayName="TapTap 玩家";phoneToken=null;
   SaveScope=GuxuAccountConfig.Hash("tap:"+Config.clientId+":"+playerId);StartCertification();
  }catch(TaskCanceledException){if(this&&attempt==lifetime){IsBusy=false;Notify("已取消登录");}}
  catch(Exception error){Fail(error,attempt);}
 }
 public void LoginExperience(string username,string password){
  if(!GuxuPhoneTestLogin.Enabled||IsBusy||ending||CanPlay||CanCertify)return;
  string profile=GuxuExperienceAccounts.Authenticate(username,password);
  if(profile==null){Notify("体验账号或密码不正确");return;}
  RememberExperienceSocial(username,password);
  playerId=GuxuExperienceAccounts.Identity(profile);DisplayName=GuxuExperienceAccounts.Label(profile);
  SaveScope=GuxuExperienceAccounts.Scope(profile);phoneToken=null;challengeId=null;challengePhone=null;
  gate.Begin();gate.Receive(500);Notify("体验账号登录成功");
 }
 public async void SendCode(string phone){
  if(IsBusy||ending||ResendSeconds>0)return;int attempt=lifetime;IsBusy=true;Notify("正在发送验证码……");
  try{
   if(GuxuPhoneTestLogin.Enabled){
    if(!GuxuPhoneTestLogin.AcceptsPhone(phone))throw new GuxuAccountException("此测试包仅支持指定测试手机号");
    IsBusy=false;Notify("测试模式不发送短信，请使用测试验证码登录");return;
   }
   RequireConsent();if(!GuxuAccountConfig.Phone(phone))throw new GuxuAccountException("请输入正确的11位手机号码");
   var reply=await GuxuPhoneApi.Request(Config.phoneApiBaseUrl,"sms/send",new{phone});
   if(!this||attempt!=lifetime)return;
   if(string.IsNullOrEmpty(reply.challengeId)||reply.challengeId.Length>64)throw new GuxuAccountException("验证码发送结果异常，请重试");
   challengeId=reply.challengeId;challengePhone=phone;resendAt=Time.realtimeSinceStartupAsDouble+Math.Max(60,Math.Min(86400,reply.retryAfterSeconds));
   IsBusy=false;Notify("验证码已发送，请查收短信");
  }catch(Exception error){if(error is GuxuAccountException known)resendAt=Time.realtimeSinceStartupAsDouble+known.RetryAfterSeconds;Fail(error,attempt);}
 }
 public async void LoginPhone(string phone,string code){
  if(IsBusy||ending||CanPlay||CanCertify)return;int attempt=lifetime;IsBusy=true;Notify("正在验证手机号……");
  try{
   if(GuxuPhoneTestLogin.Enabled){
    if(!GuxuPhoneTestLogin.Accepts(phone,code))throw new GuxuAccountException("测试手机号或验证码不正确");
    RememberExperienceSocial(phone,code);
    playerId=GuxuPhoneTestLogin.Identity;DisplayName="真机测试玩家";SaveScope=GuxuAccountConfig.Hash(playerId);
    phoneToken=null;challengeId=null;challengePhone=null;IsBusy=false;
    gate.Begin();gate.Receive(500);Notify("测试登录成功");return;
   }
   RequireConsent();if(!GuxuAccountConfig.Phone(phone)||!GuxuAccountConfig.Code(code))throw new GuxuAccountException("请输入正确的手机号和6位验证码");
   if(string.IsNullOrEmpty(challengeId)||phone!=challengePhone)throw new GuxuAccountException("请先为当前手机号获取验证码");
   await EnsureSdk();if(!this||attempt!=lifetime)return;
   var reply=await GuxuPhoneApi.Request(Config.phoneApiBaseUrl,"sms/login",new{phone,code,challengeId});
   if(!this||attempt!=lifetime)return;
   if(!Guid.TryParse(reply.userId,out _)||string.IsNullOrEmpty(reply.accessToken)||!System.Text.RegularExpressions.Regex.IsMatch(reply.accessToken,@"\A[A-Za-z0-9_-]{43}\z")||reply.expiresInSeconds<=0||reply.expiresInSeconds>86400)throw new GuxuAccountException("账号服务返回异常，请重试");
   TapTapLogin.Instance.Logout();phoneToken=reply.accessToken;sessionExpiresAt=Time.realtimeSinceStartupAsDouble+reply.expiresInSeconds;
   playerId="phone:"+reply.userId;DisplayName=phone.Substring(0,3)+"****"+phone.Substring(7);challengeId=null;challengePhone=null;
   SaveScope=GuxuAccountConfig.Hash("phone:"+Config.phoneApiBaseUrl.TrimEnd('/')+":"+reply.userId);StartCertification();
  }catch(Exception error){Fail(error,attempt);}
 }
 public void RetryCertification(){if(!CanCertify)return;if(!Consented){Notify("请先阅读并同意用户协议和隐私政策");return;}StartCertification();}
 void StartCertification(){
  IsBusy=true;hasComplianceSession=true;gate.Begin();certifyDeadline=Time.realtimeSinceStartupAsDouble+180;Notify("请完成实名认证与防沉迷验证");
  try{TapTapCompliance.Startup(playerId);}catch(Exception error){gate.Reset();IsBusy=false;Notify("认证启动失败，请退出账号后重试");Debug.LogWarning("[Accounts] Certification startup failed: "+error.GetType().Name);}
 }
 void OnCompliance(int code){
  if(ending){if(code==1000){ending=false;IsBusy=false;Notify("已退出账号，请重新登录");}return;}
  if(string.IsNullOrEmpty(playerId))return;
  if(!gate.Receive(code))return;
  IsBusy=false;
  if(code==500){Notify("认证通过");return;}
  if(code==1000||code==1001){var token=phoneToken;ClearIdentity();hasComplianceSession=false;if(token!=null)RevokePhone(token);if(sdkReady)TapTapLogin.Instance.Logout();Notify(code==1001?"请选择其他账号登录":"认证已退出，请重新登录");return;}
  string message=code==1030?"当前时段不可游戏，请在允许的时间登录":code==1050?"今日可玩时长已用完":code==1100?"当前账号未达到本游戏的可玩年龄":code==9002?"已取消实名认证，可重新认证":code==1200?"认证服务连接失败，请检查网络后重试":"认证失败，请重新尝试";
  Notify(message);
 }
 public void Logout(){
  if(IsBusy||ending)return;
  var token=phoneToken;ClearIdentity();IsBusy=hasComplianceSession;ending=hasComplianceSession;exitDeadline=Time.realtimeSinceStartupAsDouble+15;
  Notify(ending?"正在退出账号……":"已退出账号，请重新登录");
  if(token!=null)RevokePhone(token);
  if(sdkReady){TapTapLogin.Instance.Logout();if(hasComplianceSession)TapTapCompliance.Exit();}
  hasComplianceSession=false;
 }
 async void RevokePhone(string token){try{await GuxuPhoneApi.Request(Config.phoneApiBaseUrl,"logout",new{},token);}catch{Debug.LogWarning("[Accounts] Remote logout unavailable; local credential cleared.");}}
 void ClearIdentity(){lifetime++;ClearSocialIdentity();gate.Reset();checkingSession=false;playerId=null;phoneToken=null;SaveScope="";DisplayName="";challengeId=null;challengePhone=null;}
 void Fail(Exception error,int attempt){if(!this||attempt!=lifetime)return;IsBusy=false;gate.Reset();Notify(error is GuxuAccountException?error.Message:"登录失败，请稍后重试");Debug.LogWarning("[Accounts] Request failed: "+error.GetType().Name);}
 void Update(){
  if(gate.Certifying&&Time.realtimeSinceStartupAsDouble>=certifyDeadline){gate.Reset();IsBusy=false;Logout();}
  if(ending&&Time.realtimeSinceStartupAsDouble>=exitDeadline){exitDeadline=double.PositiveInfinity;Notify("退出认证超时，请重启游戏后登录");}
  if(phoneToken!=null&&Time.realtimeSinceStartupAsDouble>=sessionExpiresAt){IsBusy=false;Logout();}
 }
 async void OnApplicationPause(bool paused){
  if(paused||phoneToken==null||!gate.CanPlay||checkingSession)return;
  int attempt=lifetime;checkingSession=true;Changed?.Invoke();
  try{var reply=await GuxuPhoneApi.Request(Config.phoneApiBaseUrl,"session",null,phoneToken);if(!this||attempt!=lifetime)return;if("phone:"+reply.userId!=playerId||reply.expiresInSeconds<=0)throw new GuxuAccountException("登录已失效");checkingSession=false;Changed?.Invoke();}
  catch{if(this&&attempt==lifetime){checkingSession=false;IsBusy=false;Logout();}}
 }
 void OnApplicationQuit(){quitting=true;}
 void OnDestroy(){lifetime++;if(current==this)current=null;if(quitting)return;if(initCallbackRegistered)TapTapSDK.RemoveInitCallback(this);if(sdkReady&&hasComplianceSession)TapTapCompliance.Exit();}
}
}

