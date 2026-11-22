using UnityEngine;

namespace Gxdl {
// shortcut: fixed credentials are only for the explicitly selected device-test build.
public static class GuxuPhoneTestLogin {
 public static bool Enabled {
  get {
#if GXDL_PHONE_TEST
   return Debug.isDebugBuild&&Application.identifier=="com.localrebuild.guxudalu.phonetest";
#elif UNITY_EDITOR
   return UnityEditor.SessionState.GetBool("GXDL.Accounts.PhoneTest",false);
#else
   return false;
#endif
  }
 }
 public static bool AcceptsPhone(string phone){
#if GXDL_PHONE_TEST || UNITY_EDITOR
  return Enabled&&phone=="13527645605";
#else
  return false;
#endif
 }
 public static bool Accepts(string phone,string code){
#if GXDL_PHONE_TEST || UNITY_EDITOR
  return AcceptsPhone(phone)&&code=="123456";
#else
  return false;
#endif
 }
 public const string Identity="device-test:phone:v1";
}
}
