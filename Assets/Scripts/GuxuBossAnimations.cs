using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;

namespace Gxdl {
[Serializable] public sealed class GuxuBossAttack {
 public string name,effect;public float duration,travel;public int[] frames;public float[] times,impacts;
 public int FrameAt(float time){int i=0;while(i+1<times.Length&&time>=times[i+1])i++;return i;}
}
[Serializable] public sealed class GuxuBossDefinition {
 public string key,name,atlas,theme;public int columns,rows,idleFrame;public float[] tint;public GuxuBossAttack[] attacks;
 [JsonIgnore] public Color Color=>new Color(tint[0],tint[1],tint[2],1);
 [JsonIgnore] Sprite[] sprites;
 public Sprite Frame(int index){
  if(sprites==null){
   var texture=Resources.Load<Texture2D>(atlas);if(!texture)throw new InvalidOperationException("Missing boss atlas: "+atlas);
   sprites=new Sprite[columns*rows];
   for(int i=0;i<sprites.Length;i++){
    int x=Mathf.FloorToInt(i%columns*texture.width/(float)columns),right=Mathf.FloorToInt((i%columns+1)*texture.width/(float)columns);
    int top=Mathf.FloorToInt(i/columns*texture.height/(float)rows),bottom=Mathf.FloorToInt((i/columns+1)*texture.height/(float)rows);
    sprites[i]=Sprite.Create(texture,new Rect(x,texture.height-bottom,right-x,bottom-top),Vector2.one*.5f,100,0,SpriteMeshType.FullRect);
    sprites[i].name=key+"/frame-"+i;
   }
  }
  return sprites[index];
 }
}
public static class GuxuBossAnimations {
 static Dictionary<string,GuxuBossDefinition> bosses;
 public static IEnumerable<GuxuBossDefinition> All {get{Load();return bosses.Values;}}
 static void Load(){
  if(bosses!=null)return;
  var file=Resources.Load<TextAsset>("Game/Guxu/BossAnimations");if(!file)throw new InvalidOperationException("Missing BossAnimations.json");
  var definitions=JsonConvert.DeserializeObject<GuxuBossDefinition[]>(file.text);var loaded=new Dictionary<string,GuxuBossDefinition>();
  foreach(var b in definitions){Validate(b);loaded.Add(b.key,b);}bosses=loaded;
 }
 public static GuxuBossDefinition Find(string key){Load();return key!=null&&bosses.TryGetValue(key,out var b)?b:null;}
 public static int DamagePart(int total,int index,int count){if(total<0||count<1||index<0||index>=count)throw new ArgumentOutOfRangeException();int part=total/count;return index==count-1?total-part*(count-1):part;}
 public static void Validate(GuxuBossDefinition b){
  if(b==null||string.IsNullOrEmpty(b.key)||string.IsNullOrEmpty(b.atlas)||b.columns<1||b.rows<1||b.idleFrame<0||b.idleFrame>=b.columns*b.rows||b.tint==null||b.tint.Length!=3||b.attacks==null||b.attacks.Length==0)throw new InvalidOperationException("Invalid boss definition");
  foreach(var a in b.attacks){
   if(a.duration<=0||a.frames==null||a.times==null||a.frames.Length<2||a.frames.Length!=a.times.Length||a.times[0]!=0||Mathf.Abs(a.times[a.times.Length-1]-a.duration)>.001f||a.impacts==null||a.impacts.Length==0)throw new InvalidOperationException("Invalid boss clip: "+b.key+"/"+a.name);
   for(int i=0;i<a.frames.Length;i++)if(a.frames[i]<0||a.frames[i]>=b.rows*b.columns||float.IsNaN(a.times[i])||float.IsInfinity(a.times[i])||(i>0&&a.times[i]<=a.times[i-1]))throw new InvalidOperationException("Invalid boss keyframe: "+a.name);
   for(int i=0;i<a.impacts.Length;i++)if(float.IsNaN(a.impacts[i])||float.IsInfinity(a.impacts[i])||a.impacts[i]<0||a.impacts[i]>=a.duration||(i>0&&a.impacts[i]<=a.impacts[i-1]))throw new InvalidOperationException("Invalid boss contact: "+a.name);
  }
 }
}
}
