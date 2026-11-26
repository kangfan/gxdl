using System;
using System.IO;
using Gxdl;
using UnityEditor;
using UnityEngine;

public static class GuxuStoryCheck {
 [MenuItem("GXDL/QA/Final Story Contract")]
 public static void ValidateContract(){
  GuxuStoryData.Validate();
  Require(GuxuStoryData.Opening.Length==7,"opening has seven shots");
  Require(GuxuStoryData.FirstSeal.Length==6,"retained historical first-seal atlas has six shots; no live first-unseal cinematic");
  foreach(var shots in new[]{GuxuStoryData.Opening,GuxuStoryData.FirstSeal})foreach(var shot in shots){
   var texture=GuxuStoryData.Texture(shot.atlas);var uv=GuxuStoryData.Cell(shot.cell,texture.width,texture.height);
   Require(uv.xMin>=0&&uv.yMin>=0&&uv.xMax<=1&&uv.yMax<=1,"UV stays inside atlas");
   Require(uv.width<.5f&&uv.height<.5f,"atlas cell excludes neighboring pixels");
   for(int sample=0;sample<=60;sample++){
    var pose=GuxuStoryData.CameraPose(shot,shot.duration*sample/60);
    Require(900*pose.z/2>=375+Mathf.Abs(pose.x)&&1350*pose.z/2>=667+Mathf.Abs(pose.y),"camera covers portrait viewport throughout "+shot.title);
   }
  }
  Require(GxdlGame.OpeningStoryKey!=GxdlGame.FirstUnsealStoryKey,"independent completion state");
  Directory.CreateDirectory("Logs");
  File.WriteAllText("Logs/guxu-final-story-contract.txt","PASS: opening and retained historical storyboard asset contract; atlas UV and camera coverage at 61 samples per shot. First-unseal cinematic is retired; actual ordinary action and legacy reward flag are checked by Video Unseal QA. "+DateTime.Now.ToString("O"));
 }
 static void Require(bool value,string message){if(!value)throw new Exception("Final story: "+message);}
}

public sealed class GuxuStoryTextureImport : AssetPostprocessor {
 void OnPreprocessTexture(){
  if(!assetPath.StartsWith("Assets/Resources/Game/Guxu/Final/Story/",StringComparison.Ordinal))return;
  var importer=(TextureImporter)assetImporter;
  importer.textureType=TextureImporterType.Default;importer.sRGBTexture=true;
  importer.maxTextureSize=2048;importer.mipmapEnabled=false;importer.wrapMode=TextureWrapMode.Clamp;
  importer.textureCompression=TextureImporterCompression.Uncompressed;importer.filterMode=FilterMode.Bilinear;
  GuxuFinalArtImporter.ApplyBuildCompression(importer);
 }
}
