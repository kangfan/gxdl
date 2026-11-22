using System;
using System.Collections.Generic;
using UnityEngine;

namespace Gxdl {
// Final 2026-11-06 art direction. Rectangles use actual PNG top-left pixel coordinates.
public static class GuxuUiArt {
 public enum Icon { Cultivation, Ruins, Companion, Inventory, Sect, Explore, Codex, SealToken, SpiritDust }
 static readonly Dictionary<string,Sprite> cache=new Dictionary<string,Sprite>();
 static readonly int[] navigationCells={1,2,4,6,0,5,3,7,8};
 static readonly Color[] qualityColors={new Color(.88f,.90f,.89f),new Color(.30f,.78f,.40f),new Color(.27f,.58f,1f),new Color(.69f,.36f,.94f),new Color(1f,.61f,.18f)};
 static readonly string[] qualityNames={"白","绿","蓝","紫","橙"};
 const string ArtRoot="Game/Guxu/Final/";
 public static Sprite Optional(string key,bool sliced=false){
  string path="Game/Guxu/Expansion/UI/"+key,cacheKey=path+(sliced?":sliced":"");
  if(cache.TryGetValue(cacheKey,out var known)&&known)return known;
  var texture=Resources.Load<Texture2D>(path);if(!texture)return null;
  var border=sliced?new Vector4(texture.width*.12f,texture.height*.12f,texture.width*.12f,texture.height*.12f):Vector4.zero;if(sliced&&key.StartsWith("ui23-")){float bx=key=="ui23-panel-modal"?61.5f/512f:31f/256f;float by=key=="ui23-panel-modal"?61.5f/512f:key=="ui23-panel-dark"?31f/256f:key=="ui23-button-flat"||key=="ui23-input-field"?7.5f/48f:7.5f/64f;border=new Vector4(texture.width*bx,texture.height*by,texture.width*bx,texture.height*by);}
  if(sliced&&key=="combat-enemy-frame")border=new Vector4(texture.width*68f/512f,texture.height*18f/128f,texture.width*68f/512f,texture.height*34f/128f);
  if(sliced&&key=="combat-player-frame")border=new Vector4(texture.width*128f/512f,texture.height*16f/96f,texture.width*32f/512f,texture.height*20f/96f);
  if(sliced&&key=="combat-bar-frame")border=new Vector4(texture.width*16f/256f,texture.height*6f/32f,texture.width*16f/256f,texture.height*6f/32f);
  if(sliced&&key=="hud-unseal-button")border=new Vector4(72,14,72,14);
  if(sliced&&key=="hud-health-frame")border=new Vector4(18,14,18,14);
  if(sliced&&key=="panel-inventory")border=new Vector4(32,32,32,32);
  if(sliced&&key=="handbook-panel")border=new Vector4(texture.width*100f/512f,texture.height*120f/512f,texture.width*120f/512f,texture.height*100f/512f);
  if(sliced&&key=="handbook-stat-tag")border=new Vector4(texture.width*56f/256f,texture.height*24f/128f,texture.width*32f/256f,texture.height*56f/128f);
  var sprite=Sprite.Create(texture,new Rect(0,0,texture.width,texture.height),new Vector2(.5f,.5f),100,0,SpriteMeshType.FullRect,border);sprite.name=path;cache[cacheKey]=sprite;return sprite;
 }

 // Bottom navigation uses Sect/Cultivation/Ruins/Codex/Companion: 洞府/修行/寻墟/鉴宝/阵容.
 public static Sprite Navigation(Icon icon){
  int index=(int)icon;if(index<0||index>=navigationCells.Length)throw new ArgumentOutOfRangeException(nameof(icon));
  var item=icon==Icon.SealToken?GuxuItemIcons.Get("resource-seal"):icon==Icon.SpiritDust?GuxuItemIcons.Get("resource-dust"):null;if(item)return item;
  return Optional("nav-"+icon.ToString().ToLowerInvariant())??Cell(ArtRoot+"UI/navigation-atlas",3,3,navigationCells[index]);
 }
 // Gear.slot: sword, crown, wings, boots, belt, wrists, gourd, necklace, jade, banner, pearl, robe.
 public static Sprite Item(int slot)=>GuxuItemIcons.Get("equipment-"+Mathf.Clamp(slot,0,11).ToString("00"))??Optional("item-"+Mathf.Clamp(slot,0,11).ToString("00"))??Cell(ArtRoot+"Items/relic-atlas",4,3,Mathf.Clamp(slot,0,11));
 public static Sprite QualityFrame(int quality){
  int index=Mathf.Clamp(quality,1,5)-1;
  return GuxuItemIcons.Get("quality-"+(index+1))??Optional("quality-"+(index+1))??Region(ArtRoot+"UI/quality-atlas",new Rect(32+425*index,103,414,440),2172,724);
 }
 public static Sprite Portrait=>Optional("portrait")??Region(ArtRoot+"Hero/hero",new Rect(430,4,320,320),1086,1448);
 public static Sprite PrimaryButton=>Optional("button-primary",true)??Region(ArtRoot+"UI/controls-atlas",new Rect(42,70,1452,270),1536,1024);
 public static Sprite SecondaryButton=>Optional("button-secondary",true)??Region(ArtRoot+"UI/controls-atlas",new Rect(42,376,1452,274),1536,1024);
 public static Sprite DecisionPanel=>Optional("panel-decision",true)??Region(ArtRoot+"UI/controls-atlas",new Rect(42,687,1452,275),1536,1024,new Vector4(120,55,120,55));
 public static Sprite ModalPanel=>Optional("panel-modal",true)??Region(ArtRoot+"UI/modal-panel",new Rect(0,0,1024,1024),1024,1024,new Vector4(140,140,140,140));
 public static Sprite BattlePrepButton(bool selected)=>Region("Game/Guxu/Expansion/UI/battle-prep-buttons",new Rect(54,selected?34:362,2060,320),2172,724);
 public static Sprite BattlePrepSkill(int skill)=>Region("Game/Guxu/Expansion/UI/battle-prep-v2-skills",new Rect(Mathf.Clamp(skill,0,3)*543+35,123,474,476),2172,724);
 public static Sprite BattlePrepReward(int kind)=>GuxuItemIcons.Get("resource-"+new[]{"seal","dust","exp","ore","insight","stone"}[kind])??Region("Game/Guxu/Expansion/UI/battle-prep-v2-rewards",new Rect(kind%3*512+72,kind/3*512+50,400,430),1536,1024);
 public static Color QualityColor(int quality)=>qualityColors[Mathf.Clamp(quality,1,5)-1];
 public static string QualityName(int quality)=>qualityNames[Mathf.Clamp(quality,1,5)-1];
 public static string TierLabel(int quality,int level)=>QualityName(quality)+" · "+Mathf.Clamp(level,1,3)+"阶";

 // Normalize against the source dimensions to tolerate Unity max-size texture import.
 static Sprite Region(string path,Rect topRect,float sourceWidth,float sourceHeight,Vector4? border=null){
  string key=path+":"+topRect.ToString();
  if(cache.TryGetValue(key,out var sprite)&&sprite!=null)return sprite;
  if(topRect.xMin<0||topRect.yMin<0||topRect.xMax>sourceWidth||topRect.yMax>sourceHeight)
   throw new ArgumentOutOfRangeException(nameof(topRect),"Sprite rectangle outside source atlas: "+path);
  var texture=Resources.Load<Texture2D>(path);
  if(texture==null)throw new InvalidOperationException("Missing Guxu art: "+path);
  float sx=texture.width/sourceWidth,sy=texture.height/sourceHeight;
  var rect=new Rect(topRect.x*sx,(sourceHeight-topRect.yMax)*sy,topRect.width*sx,topRect.height*sy);
  var edge=border??Vector4.zero;edge=new Vector4(edge.x*sx,edge.y*sy,edge.z*sx,edge.w*sy);
  sprite=Sprite.Create(texture,rect,new Vector2(.5f,.5f),100,0,SpriteMeshType.FullRect,edge);
  sprite.name=key;cache[key]=sprite;return sprite;
 }
 static Sprite Cell(string path,int columns,int rows,int index){
  if(index<0||index>=columns*rows)throw new ArgumentOutOfRangeException(nameof(index));
  string key=path+":"+index;
  if(cache.TryGetValue(key,out var sprite)&&sprite!=null)return sprite;
  var texture=Resources.Load<Texture2D>(path);
  if(texture==null)throw new InvalidOperationException("Missing Guxu art: "+path);
  float width=(float)texture.width/columns,height=(float)texture.height/rows;
  var rect=new Rect(index%columns*width,(rows-1-index/columns)*height,width,height);
  sprite=Sprite.Create(texture,rect,new Vector2(.5f,.5f),100,0,SpriteMeshType.FullRect);
  sprite.name=key;cache[key]=sprite;return sprite;
 }
 public static void Validate(){
  for(int i=0;i<9;i++)Require(Navigation((Icon)i));
  for(int i=0;i<12;i++)Require(Item(i));
  for(int quality=1;quality<=5;quality++){
   Require(QualityFrame(quality));
   for(int level=1;level<=3;level++)if(string.IsNullOrEmpty(TierLabel(quality,level)))throw new InvalidOperationException("Invalid rarity tier");
  }
  Require(PrimaryButton);Require(SecondaryButton);Require(DecisionPanel);Require(ModalPanel);Require(Portrait);
 }
 static void Require(Sprite sprite){if(sprite==null||sprite.rect.width<16||sprite.rect.height<16)throw new InvalidOperationException("Invalid Guxu sprite cell");}
}
}



