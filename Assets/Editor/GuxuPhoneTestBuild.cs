using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class GuxuPhoneTestBuild {
 public const string Define="GXDL_PHONE_TEST",PackageName="com.localrebuild.guxudalu.phonetest";
 public const string Evidence="Reference/SubmissionFeedback20261117/build";
 public const string OutputPath="Builds/Android/GuxuDalu-0.1.5-Feedback20261117.apk";
 public static bool Active {get;private set;}
 public static void Phase(string phase){Directory.CreateDirectory(Evidence);File.AppendAllText(Evidence+"/build-phases.txt",DateTimeOffset.Now.ToString("O")+" "+phase+" platform="+EditorUserBuildSettings.activeBuildTarget+"\n");}
 public static bool ValidateBuild(BuildReport report){
  if(PlayerSettings.GetScriptingDefineSymbolsForGroup(BuildTargetGroup.Android).Split(';').Contains(Define))throw new BuildFailedException("Do not persist GXDL_PHONE_TEST in PlayerSettings; use the dedicated phone-test build menu.");
  if(!Active)return false;
  if((report.summary.options&BuildOptions.Development)==0||PlayerSettings.GetApplicationIdentifier(BuildTargetGroup.Android)!=PackageName)throw new BuildFailedException("Phone test requires a Development build with the isolated test package name.");
  return true;
 }
 [MenuItem("GXDL/Build Android Phone Test APK")]
 public static void Build(){
  if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Stop Play Mode first.");
  Directory.CreateDirectory(Evidence);
  string company=PlayerSettings.companyName,product=PlayerSettings.productName,version=PlayerSettings.bundleVersion,id=PlayerSettings.GetApplicationIdentifier(BuildTargetGroup.Android);
  int versionCode=PlayerSettings.Android.bundleVersionCode;
  var orientation=PlayerSettings.defaultInterfaceOrientation;var min=PlayerSettings.Android.minSdkVersion;var target=PlayerSettings.Android.targetSdkVersion;
  var backend=PlayerSettings.GetScriptingBackend(BuildTargetGroup.Android);var architectures=PlayerSettings.Android.targetArchitectures;
  bool expansion=PlayerSettings.Android.useAPKExpansionFiles,bundle=EditorUserBuildSettings.buildAppBundle,export=EditorUserBuildSettings.exportAsGoogleAndroidProject;
  var oldHttp=PlayerSettings.insecureHttpOption;
  try{Active=true;PlayerSettings.insecureHttpOption=InsecureHttpOption.DevelopmentOnly;GuxuAndroidBuild.Build();}
  catch(Exception error){File.WriteAllText(Evidence+"/build-exception.txt",DateTimeOffset.Now.ToString("O")+"\n"+error);throw;}
  finally{
   Active=false;PlayerSettings.insecureHttpOption=oldHttp;PlayerSettings.companyName=company;PlayerSettings.productName=product;PlayerSettings.bundleVersion=version;
   PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Android,id);PlayerSettings.Android.bundleVersionCode=versionCode;
   PlayerSettings.defaultInterfaceOrientation=orientation;PlayerSettings.Android.minSdkVersion=min;PlayerSettings.Android.targetSdkVersion=target;
   PlayerSettings.SetScriptingBackend(BuildTargetGroup.Android,backend);PlayerSettings.Android.targetArchitectures=architectures;
   PlayerSettings.Android.useAPKExpansionFiles=expansion;EditorUserBuildSettings.buildAppBundle=bundle;EditorUserBuildSettings.exportAsGoogleAndroidProject=export;
   AssetDatabase.SaveAssets();
  }
 }
}
