using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Gxdl;

public class GxdlAssetImport:AssetPostprocessor {
 void OnPreprocessTexture(){if(!assetPath.StartsWith("Assets/Resources/Original/")&&!assetPath.StartsWith("Assets/Resources/Game/UI/"))return;var importer=(TextureImporter)assetImporter;importer.textureType=TextureImporterType.Default;importer.mipmapEnabled=false;importer.alphaIsTransparency=true;importer.maxTextureSize=8192;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.filterMode=FilterMode.Bilinear;}
}
public static class GxdlSetup {
 [MenuItem("GXDL/Prepare Game")]
 public static void Prepare(){GameRules.SelfTest(OriginalConfig.Load());PlayerSettings.productName="寻道大千 · Unity 本地版";PlayerSettings.companyName="LocalRebuild";PlayerSettings.defaultScreenWidth=750;PlayerSettings.defaultScreenHeight=1334;PlayerSettings.defaultIsNativeResolution=false;PlayerSettings.fullScreenMode=FullScreenMode.Windowed;PlayerSettings.defaultInterfaceOrientation=UIOrientation.Portrait;
 var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);var camera=new GameObject("Main Camera").AddComponent<Camera>();camera.tag="MainCamera";camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.08f,.18f,.16f);camera.orthographic=true;camera.gameObject.AddComponent<AudioListener>();new GameObject("Game").AddComponent<GxdlGame>();Directory.CreateDirectory("Assets/Scenes");EditorSceneManager.SaveScene(scene,"Assets/Scenes/GxdlMain.unity");EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene("Assets/Scenes/GxdlMain.unity",true)};AssetDatabase.SaveAssets();SetPortraitGameView();Debug.Log("[GXDL] Main scene ready.");}
 static void SetPortraitGameView(){var asm=typeof(Editor).Assembly;var sizesType=asm.GetType("UnityEditor.GameViewSizes");var singleton=typeof(ScriptableSingleton<>).MakeGenericType(sizesType);var sizes=singleton.GetProperty("instance").GetValue(null);var group=sizesType.GetMethod("GetGroup").Invoke(sizes,new object[]{0});var groupType=group.GetType();var sizeType=asm.GetType("UnityEditor.GameViewSize");var kindType=asm.GetType("UnityEditor.GameViewSizeType");var size=System.Activator.CreateInstance(sizeType,new object[]{System.Enum.ToObject(kindType,1),750,1334,"GXDL Portrait"});groupType.GetMethod("AddCustomSize").Invoke(group,new[]{size});int index=(int)groupType.GetMethod("GetTotalCount").Invoke(group,null)-1;var window=EditorWindow.GetWindow(asm.GetType("UnityEditor.GameView"));window.GetType().GetProperty("selectedSizeIndex",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.Public|System.Reflection.BindingFlags.NonPublic).SetValue(window,index);}
 [MenuItem("GXDL/Validate Rules")] public static void Validate(){GameRules.SelfTest(OriginalConfig.Load());}
 [MenuItem("GXDL/Build Windows Preview")] public static void Build(){var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{"Assets/Scenes/GxdlMain.unity"},locationPathName="Builds/Windows/Gxdl.exe",target=BuildTarget.StandaloneWindows64,options=BuildOptions.Development});if(report.summary.result!=UnityEditor.Build.Reporting.BuildResult.Succeeded)throw new System.Exception("Preview build failed: "+report.summary.result);Debug.Log("[GXDL] Preview build succeeded.");}
}
