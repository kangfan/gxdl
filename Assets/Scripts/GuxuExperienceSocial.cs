using System;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.Networking;

namespace Gxdl {
public sealed partial class GuxuAccountSession {
#if GXDL_PHONE_TEST || UNITY_EDITOR
 string experienceSocialUser,experienceSocialPassword,experienceSocialToken;
 double experienceSocialExpiry;
 const string ExperienceSocialUrl="http://127.0.0.1:18081";
#endif
 public string SocialBaseUrl {
  get {
#if GXDL_PHONE_TEST || UNITY_EDITOR
   if(GuxuPhoneTestLogin.Enabled)return ExperienceSocialUrl;
#endif
   return Config.phoneApiBaseUrl;
  }
 }
 public string SocialToken {
  get {
   if(!CanPlay)return null;
#if GXDL_PHONE_TEST || UNITY_EDITOR
   if(GuxuPhoneTestLogin.Enabled)return Time.realtimeSinceStartupAsDouble<experienceSocialExpiry?experienceSocialToken:null;
#endif
   return phoneToken;
  }
 }
 public static bool SocialAddressAllowed(string address){
#if GXDL_PHONE_TEST || UNITY_EDITOR
  if(GuxuPhoneTestLogin.Enabled&&address==ExperienceSocialUrl)return true;
#endif
  return GuxuAccountConfig.Https(address);
 }
 void RememberExperienceSocial(string username,string password){
#if GXDL_PHONE_TEST || UNITY_EDITOR
  if(GuxuPhoneTestLogin.Enabled){experienceSocialUser=username;experienceSocialPassword=password;experienceSocialToken=null;experienceSocialExpiry=0;}
#endif
 }
 void ClearSocialIdentity(){
#if GXDL_PHONE_TEST || UNITY_EDITOR
  string token=experienceSocialToken;experienceSocialUser=experienceSocialPassword=experienceSocialToken=null;experienceSocialExpiry=0;
  if(!string.IsNullOrEmpty(token))RevokeExperienceSocial(token);
#endif
 }
 public async Task<bool> EnsureSocialSession(){
  if(!CanPlay)return false;
  if(!string.IsNullOrEmpty(SocialToken))return true;
#if GXDL_PHONE_TEST || UNITY_EDITOR
  if(GuxuPhoneTestLogin.Enabled&&!string.IsNullOrEmpty(experienceSocialUser)){
   int attempt=lifetime;
   try{
    var result=await ExperienceSocialRequest("experience",new{username=experienceSocialUser,password=experienceSocialPassword});
    if(!this||attempt!=lifetime||!CanPlay)return false;
    string token=(string)result["accessToken"];int duration=(int?)result["expiresInSeconds"]??0;
    if(!System.Text.RegularExpressions.Regex.IsMatch(token??"",@"\A[A-Za-z0-9_-]{43}\z")||duration<=0||duration>86400)return false;
    experienceSocialToken=token;experienceSocialExpiry=Time.realtimeSinceStartupAsDouble+duration;return true;
   }catch(Exception){return false;}
  }
#else
  await Task.CompletedTask;
#endif
  return false;
 }
#if GXDL_PHONE_TEST || UNITY_EDITOR
 static async Task<JObject> ExperienceSocialRequest(string path,object payload,string token=null){
  if(!GuxuPhoneTestLogin.Enabled)throw new InvalidOperationException("Test social disabled");
  using(var request=new UnityWebRequest(ExperienceSocialUrl+"/v1/auth/"+path,"POST")){
   request.uploadHandler=new UploadHandlerRaw(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(payload)));request.downloadHandler=new DownloadHandlerBuffer();request.timeout=8;request.redirectLimit=0;request.SetRequestHeader("Content-Type","application/json");
   if(token!=null)request.SetRequestHeader("Authorization","Bearer "+token);
   var operation=request.SendWebRequest();while(!operation.isDone)await Task.Yield();
   if(request.result!=UnityWebRequest.Result.Success)throw new InvalidOperationException("Test social unavailable");
   return JObject.Parse(request.downloadHandler.text);
  }
 }
 static async void RevokeExperienceSocial(string token){try{await ExperienceSocialRequest("logout",new{},token);}catch(Exception){}}
#endif
}
}
