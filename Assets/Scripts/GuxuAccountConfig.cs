using System;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;

namespace Gxdl {
[Serializable]
public sealed class GuxuAccountConfig {
 public string clientId="",clientToken="",androidPackageName="com.localrebuild.guxudalu";
 public int ageRating; // Set only to the submission age confirmed by the owner.
 public string phoneApiBaseUrl="",termsUrl="",privacyUrl="",policyVersion="1";
 public static GuxuAccountConfig Load(){var asset=Resources.Load<TextAsset>("Game/AccountConfig");return asset?JsonUtility.FromJson<GuxuAccountConfig>(asset.text):new GuxuAccountConfig();}
 public string Validate(bool includePhone=true){
  if(string.IsNullOrWhiteSpace(clientId)||string.IsNullOrWhiteSpace(clientToken))return "请配置 TapTap Client ID 和 Client Token";
  if(!Https(termsUrl)||!Https(privacyUrl)||string.IsNullOrWhiteSpace(policyVersion))return "请配置用户协议、隐私政策的 HTTPS 地址及版本";
  if(includePhone&&!Https(phoneApiBaseUrl))return "请配置短信账号服务的 HTTPS 地址";
  if(ageRating!=8&&ageRating!=12&&ageRating!=16)return "请配置已确认的适龄级别";
  if(string.IsNullOrWhiteSpace(androidPackageName))return "请配置 TapTap 后台登记的 Android 包名";
  return null;
 }
 public static bool Https(string value){return Uri.TryCreate(value,UriKind.Absolute,out var uri)&&uri.Scheme==Uri.UriSchemeHttps&&!string.IsNullOrEmpty(uri.Host)&&string.IsNullOrEmpty(uri.UserInfo)&&string.IsNullOrEmpty(uri.Query)&&string.IsNullOrEmpty(uri.Fragment);}
 public static bool Phone(string value)=>value!=null&&value.Length==11&&Regex.IsMatch(value,@"\A1[3-9][0-9]{9}\z");
 public static bool Code(string value)=>value!=null&&value.Length==6&&Regex.IsMatch(value,@"\A[0-9]{6}\z");
 public static string Hash(string value){using(var sha=SHA256.Create())return BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(value))).Replace("-","").ToLowerInvariant();}
}

// Only a success callback for a pending certification can open the game.
public sealed class GuxuAccountGate {
 public bool CanPlay {get;private set;}
 public bool Certifying {get;private set;}
 public void Begin(){CanPlay=false;Certifying=true;}
 public bool Receive(int code){
  if(code==500){if(!Certifying)return false;Certifying=false;CanPlay=true;return true;}
  if(code==1000||code==1001||code==1030||code==1050||code==1100||code==1200||code==9002||code<0){Reset();return true;}
  return false;
 }
 public void Reset(){CanPlay=false;Certifying=false;}
}
}
