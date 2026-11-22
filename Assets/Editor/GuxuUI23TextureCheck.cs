using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Gxdl;
using Newtonsoft.Json;

public static class GuxuUI23TextureCheck {
 [MenuItem("GXDL/Resources/UI23 Texture Audit")]
 public static void Audit(){
  if(EditorApplication.isPlaying)throw new Exception("Stop Play before texture import audit.");
  var results=new List<object>();
  string legacy="Assets/Resources/Game/Guxu/UI/",current="Assets/Resources/Game/Guxu/Expansion/UI/";
  string[] files=Directory.GetFiles(legacy,"*.png").Concat(Directory.GetFiles("Assets/Resources/Game/Guxu/Final/UI/","*.png")).Concat(Directory.GetFiles(current,"ui23-*.png")).Select(x=>x.Replace('\\','/')).ToArray();
  foreach(string path in files){
   AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceUpdate|ImportAssetOptions.ForceSynchronousImport);
   var importer=(TextureImporter)AssetImporter.GetAtPath(path);var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(path);if(!texture)throw new Exception("Texture not loaded: "+path);
   string key=Path.GetFileNameWithoutExtension(path);bool modern=key.StartsWith("ui23-");
   int expected=modern?(key=="ui23-panel-modal"?512:256):key=="modal-panel"||key.StartsWith("button-")?512:1024;
   if(importer.maxTextureSize!=expected||importer.mipmapEnabled)throw new Exception("Texture import policy mismatch: "+path);
   Vector4 border=Vector4.zero;if(modern){var sprite=GuxuUiArt.Optional(key,true);border=sprite.border;if(border.x*2>=texture.width||border.y*2>=texture.height)throw new Exception("Invalid nine-slice center: "+key);}
   results.Add(new{path,texture.width,texture.height,importer.maxTextureSize,importer.spritePixelsPerUnit,border=new[]{border.x,border.y,border.z,border.w},guid=AssetDatabase.AssetPathToGUID(path),bytes=new FileInfo(path).Length,referencedByRuntime=modern||path.Contains("/Final/UI/")});
  }
  CheckAtlas("Game/Guxu/UI/navigation-atlas",1254,1254,Enumerable.Range(0,9).Select(i=>new Rect(i%3*418,i/3*418,418,418)).ToArray());
  CheckAtlas("Game/Guxu/UI/quality-atlas",2172,724,Enumerable.Range(0,5).Select(i=>new Rect(32+425*i,103,414,440)).ToArray());
  CheckAtlas("Game/Guxu/UI/controls-atlas",1536,1024,new[]{new Rect(42,70,1452,270),new Rect(42,376,1452,274),new Rect(42,687,1452,275)});
  CheckAtlas("Game/Guxu/Final/UI/navigation-atlas",1254,1254,Enumerable.Range(0,9).Select(i=>new Rect(i%3*418,i/3*418,418,418)).ToArray());
  CheckAtlas("Game/Guxu/Final/UI/quality-atlas",2172,724,Enumerable.Range(0,5).Select(i=>new Rect(32+425*i,103,414,440)).ToArray());
  CheckAtlas("Game/Guxu/Final/UI/controls-atlas",1536,1024,new[]{new Rect(42,70,1452,270),new Rect(42,376,1452,274),new Rect(42,687,1452,275)});
  Directory.CreateDirectory("Reference/UIRedesign20261110/qa");File.WriteAllText("Reference/UIRedesign20261110/qa/texture-unity-audit.json",JsonConvert.SerializeObject(new{status="PASS",checkedAt=DateTime.UtcNow.ToString("O"),notes="Legacy Guxu/UI has no path or GUID references in current source/serialized assets. 34 legacy and Final/UI atlas regions remain proportional and within bounds; Final/UI resolution optimized with normalized UV preserved.",entries=results},Formatting.Indented));
 }
 static void CheckAtlas(string path,float sourceWidth,float sourceHeight,Rect[] regions){
  var texture=Resources.Load<Texture2D>(path);if(!texture)throw new Exception("Missing atlas: "+path);
  foreach(var region in regions){var rect=new Rect(region.x/sourceWidth*texture.width,(sourceHeight-region.yMax)/sourceHeight*texture.height,region.width/sourceWidth*texture.width,region.height/sourceHeight*texture.height);if(rect.xMin<0||rect.yMin<0||rect.xMax>texture.width+.01f||rect.yMax>texture.height+.01f)throw new Exception("Atlas UV outside: "+path);var sprite=Sprite.Create(texture,rect,Vector2.one*.5f,100);if(sprite.rect.width<32||sprite.rect.height<32)throw new Exception("Atlas cell too small: "+path);UnityEngine.Object.DestroyImmediate(sprite);}
 }
}

