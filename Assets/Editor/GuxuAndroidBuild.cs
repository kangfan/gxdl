using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class GuxuAndroidBuild {
 [MenuItem("GXDL/Build Android APK")]
 public static void Build(){
  GuxuPhoneTestBuild.Phase("build-start");
  if(!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Android,BuildTarget.Android))throw new Exception("Android build support is missing.");
  Directory.CreateDirectory("Builds/Android");Directory.CreateDirectory("Logs");
  GuxuAcceptance.Rules();
  GuxuAppIcon.Verify();
  GuxuPhoneTestBuild.Phase("rules-and-icon-passed");
  PlayerSettings.companyName="LocalRebuild";PlayerSettings.productName="古墟大陆";
  if(GuxuPhoneTestBuild.Active)PlayerSettings.productName="古墟大陆·测试";
  PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Android,GuxuPhoneTestBuild.Active?GuxuPhoneTestBuild.PackageName:Gxdl.GuxuAccountConfig.Load().androidPackageName);
  PlayerSettings.bundleVersion=GuxuPhoneTestBuild.Active?"0.1.5-Feedback20261117":"0.1.3";PlayerSettings.Android.bundleVersionCode=GuxuPhoneTestBuild.Active?6:4;
  PlayerSettings.defaultInterfaceOrientation=UIOrientation.Portrait;
  PlayerSettings.Android.minSdkVersion=AndroidSdkVersions.AndroidApiLevel26;
  PlayerSettings.Android.targetSdkVersion=(AndroidSdkVersions)35;
  PlayerSettings.SetScriptingBackend(BuildTargetGroup.Android,ScriptingImplementation.IL2CPP);
  PlayerSettings.Android.targetArchitectures=AndroidArchitecture.ARM64|AndroidArchitecture.ARMv7;
  PlayerSettings.Android.useAPKExpansionFiles=false;
  EditorUserBuildSettings.buildAppBundle=false;
  EditorUserBuildSettings.exportAsGoogleAndroidProject=false;
  var oldSdk=UnityEditor.Android.AndroidExternalToolsSettings.sdkRootPath;
  var oldNdk=UnityEditor.Android.AndroidExternalToolsSettings.ndkRootPath;
  var oldJdk=UnityEditor.Android.AndroidExternalToolsSettings.jdkRootPath;
  try{
   UnityEditor.Android.AndroidExternalToolsSettings.sdkRootPath="G:/learn/carrom-analysis/android-build/SDK";
   UnityEditor.Android.AndroidExternalToolsSettings.ndkRootPath="G:/learn/carrom-analysis/android-build/NDK/android-ndk-r23b";
   UnityEditor.Android.AndroidExternalToolsSettings.jdkRootPath="G:/learn/carrom-analysis/android-build/JDK";
   GuxuPhoneTestBuild.Phase("native-dependency-resolution-start");
   if(Application.isBatchMode&&!GooglePlayServices.PlayServicesResolver.ResolveSync(true))throw new Exception("Android native dependency resolution failed; inspect the resolver log before building.");
   GuxuPhoneTestBuild.Phase("native-dependency-resolution-complete");
   AssetDatabase.SaveAssets();
   var path=GuxuPhoneTestBuild.Active?GuxuPhoneTestBuild.OutputPath:"Builds/Android/GuxuDalu-0.1.3.apk";
   GuxuPhoneTestBuild.Phase("BuildPlayer-start");
   var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{"Assets/Scenes/GxdlMain.unity"},locationPathName=path,target=BuildTarget.Android,options=BuildOptions.CompressWithLz4|(GuxuPhoneTestBuild.Active?BuildOptions.Development:BuildOptions.None),extraScriptingDefines=GuxuPhoneTestBuild.Active?new[]{GuxuPhoneTestBuild.Define}:Array.Empty<string>()});
   var summary=report.summary;
   var messages=report.steps.SelectMany(s=>s.messages).Where(m=>m.type==LogType.Warning||m.type==LogType.Error).Select(m=>m.content).ToArray();
   File.WriteAllText(GuxuPhoneTestBuild.Active?GuxuPhoneTestBuild.Evidence+"/build-report.json":"Logs/guxu-android-build.json",Newtonsoft.Json.JsonConvert.SerializeObject(new{timestamp=DateTimeOffset.Now.ToString("O"),result=summary.result.ToString(),errors=summary.totalErrors,warnings=summary.totalWarnings,bytes=summary.totalSize,seconds=summary.totalTime.TotalSeconds,path=Path.GetFullPath(path),messages},Newtonsoft.Json.Formatting.Indented));
   if(summary.result!=BuildResult.Succeeded||!File.Exists(path))throw new Exception("Android build failed: "+summary.result);
  }finally{
   UnityEditor.Android.AndroidExternalToolsSettings.sdkRootPath=oldSdk;
   UnityEditor.Android.AndroidExternalToolsSettings.ndkRootPath=oldNdk;
   UnityEditor.Android.AndroidExternalToolsSettings.jdkRootPath=oldJdk;
  }
 }
}

