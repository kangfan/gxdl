using System;
using System.IO;
using UnityEditor;
using UnityEngine;
public sealed class GuxuFinalArtImporter:AssetPostprocessor {
 [Serializable] sealed class Atlas {public int width,height;public Entry[] entries;}
 [Serializable] sealed class Entry {public string key;public int x,y,width,height;}
 void OnPreprocessTexture(){
  if(assetPath.StartsWith("Assets/Resources/Game/Guxu/UI/")){
   var legacy=(TextureImporter)assetImporter;legacy.mipmapEnabled=false;legacy.npotScale=TextureImporterNPOTScale.None;legacy.wrapMode=TextureWrapMode.Clamp;legacy.maxTextureSize=System.IO.Path.GetFileName(assetPath).StartsWith("button-")?512:1024;return;
  }
  if(!assetPath.StartsWith("Assets/Resources/Game/Guxu/Final/")&&!assetPath.StartsWith("Assets/Resources/Game/Guxu/Expansion/")&&!assetPath.StartsWith("Assets/Resources/Game/Guxu/Vfx/"))return;
  var t=(TextureImporter)assetImporter;t.textureType=TextureImporterType.Default;t.alphaSource=TextureImporterAlphaSource.FromInput;t.alphaIsTransparency=true;t.mipmapEnabled=false;t.npotScale=TextureImporterNPOTScale.None;t.maxTextureSize=4096;t.textureCompression=TextureImporterCompression.Uncompressed;t.wrapMode=TextureWrapMode.Clamp;
  if(assetPath.StartsWith("Assets/Resources/Game/Guxu/Vfx/"))t.maxTextureSize=assetPath.StartsWith("Assets/Resources/Game/Guxu/Vfx/Actions/")?1024:256;
  if(assetPath.StartsWith("Assets/Resources/Game/Guxu/Vfx/Actions/Video/")){
   t.textureCompression=TextureImporterCompression.CompressedHQ;t.compressionQuality=100;
   var android=t.GetPlatformTextureSettings("Android");android.overridden=true;android.maxTextureSize=1024;android.format=TextureImporterFormat.ASTC_6x6;android.compressionQuality=100;t.SetPlatformTextureSettings(android);
  }
  if(assetPath.StartsWith("Assets/Resources/Game/Guxu/Final/UI/"))t.maxTextureSize=assetPath.EndsWith("modal-panel.png")?512:1024;
  if(assetPath.StartsWith("Assets/Resources/Game/Guxu/Expansion/UI/ui23-"))t.maxTextureSize=assetPath.EndsWith("ui23-panel-modal.png")?512:256;
  if(assetPath.StartsWith("Assets/Resources/Game/Guxu/Expansion/UI/chapter-"))t.maxTextureSize=assetPath.EndsWith("chapter-map-segment.png")?1024:512;
  string name=Path.GetFileNameWithoutExtension(assetPath);
  if(name.StartsWith("talent-")&&name.Length==9&&char.IsDigit(name[7])&&char.IsDigit(name[8])||name.StartsWith("feedback-neutral-"))t.maxTextureSize=128;
  if(name.StartsWith("home-entry-"))t.maxTextureSize=256;
  if(assetPath.StartsWith("Assets/Resources/Game/Guxu/Expansion/Enemies/chapter-"))t.maxTextureSize=512;
  if(assetPath.StartsWith("Assets/Resources/Game/Guxu/Expansion/Scenes/"))t.maxTextureSize=2048;
  if(name.StartsWith("hud-"))t.maxTextureSize=256;
  if(name.StartsWith("effect-")||name=="talent-node-base")t.maxTextureSize=256;
  if(name=="talent-environment"||name=="battle-prep-panel"||name=="gem-workbench")t.maxTextureSize=2048;
  if(name=="gem-socket-base")t.maxTextureSize=256;
  if(name=="forge-environment")t.maxTextureSize=2048;
  if(name=="difficulty-path")t.maxTextureSize=1024;
  if(name=="difficulty-node")t.maxTextureSize=256;
  if(name=="tower-tier"||name=="loadout-altar")t.maxTextureSize=512;
  if(name.StartsWith("combat-")||name.StartsWith("settlement-")||name=="handbook-panel"||name.StartsWith("dungeon-portal-"))t.maxTextureSize=512;
  if(name=="combat-skill-seal"||name=="combat-bar-frame"||name.StartsWith("state-")||name=="handbook-stat-tag"||name=="handbook-residence-insert")t.maxTextureSize=256;
  if(assetPath!="Assets/Resources/"+Gxdl.GuxuItemIcons.Path+".png")return;
  var atlas=JsonUtility.FromJson<Atlas>(File.ReadAllText(System.IO.Path.ChangeExtension(assetPath,"json")));
  bool modern=atlas.width==903&&atlas.height==903&&atlas.entries.Length==90;
  if(!modern&&(atlas.width!=2172||atlas.height!=724||atlas.entries.Length!=84))throw new InvalidDataException("Unexpected item icon atlas dimensions or count.");
  t.maxTextureSize=modern?1024:4096;
  t.textureType=TextureImporterType.Sprite;t.spriteImportMode=SpriteImportMode.Multiple;t.spritePixelsPerUnit=100;
  var sheet=new SpriteMetaData[atlas.entries.Length];var names=new System.Collections.Generic.HashSet<string>();
  for(int i=0;i<sheet.Length;i++){
   var e=atlas.entries[i];if(!names.Add(e.key)||e.x<0||e.y<0||e.width<=0||e.height<=0||e.x+e.width>atlas.width||e.y+e.height>atlas.height||(modern&&(e.width!=125||e.height!=125)))throw new InvalidDataException("Invalid icon cell: "+e.key);
   sheet[i]=new SpriteMetaData{name=e.key,rect=new Rect(e.x,atlas.height-e.y-e.height,e.width,e.height),alignment=(int)SpriteAlignment.Center,pivot=new Vector2(.5f,.5f)};
  }
  // Unity 2022's importer still supports this serialized Sprite Editor sheet format.
#pragma warning disable CS0618
  t.spritesheet=sheet;
#pragma warning restore CS0618
 }
}

