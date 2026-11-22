using System;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine.Networking;

namespace Gxdl {
public sealed class GuxuAccountException:Exception {
 public int RetryAfterSeconds {get;}
 public GuxuAccountException(string message,int retryAfter=0):base(message){RetryAfterSeconds=retryAfter;}
}
public sealed class GuxuPhoneReply {
 public string challengeId,userId,accessToken,code,message;
 public int retryAfterSeconds,expiresInSeconds;
}
public static class GuxuPhoneApi {
 public static async Task<GuxuPhoneReply> Request(string baseUrl,string path,object body=null,string token=null){
  if(!GuxuAccountConfig.Https(baseUrl))throw new GuxuAccountException("手机号登录服务暂未开放，请稍后再试");
  using(var request=new UnityWebRequest(baseUrl.TrimEnd('/')+"/v1/auth/"+path,body==null?"GET":"POST")){
   request.downloadHandler=new DownloadHandlerBuffer();request.timeout=20;request.redirectLimit=0;
   if(body!=null){request.uploadHandler=new UploadHandlerRaw(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(body)));request.SetRequestHeader("Content-Type","application/json");}
   if(!string.IsNullOrEmpty(token))request.SetRequestHeader("Authorization","Bearer "+token);
   var operation=request.SendWebRequest();while(!operation.isDone)await Task.Yield();
   GuxuPhoneReply reply=null;
   try{reply=JsonConvert.DeserializeObject<GuxuPhoneReply>(request.downloadHandler.text);}catch(JsonException){}
   if(request.result!=UnityWebRequest.Result.Success){
    string message=request.responseCode==429?"操作过于频繁，请稍后再试":request.responseCode==401?"验证码错误或登录已失效，请重新登录":"账号服务连接失败，请稍后重试";
    throw new GuxuAccountException(message,Math.Max(0,Math.Min(86400,reply?.retryAfterSeconds??0)));
   }
   if(reply==null)throw new GuxuAccountException("账号服务返回异常，请稍后重试");
   return reply;
  }
 }
}
}
