using System;
using UnityEngine;
using UnityEngine.UI;

namespace Gxdl {
public partial class GxdlGame {
 static readonly Color[] EncounterColors={new Color(.16f,.37f,.42f),new Color(.15f,.39f,.22f),new Color(.22f,.24f,.26f),new Color(.49f,.18f,.08f),new Color(.22f,.37f,.5f),new Color(.26f,.20f,.43f),new Color(.23f,.34f,.13f),new Color(.42f,.33f,.19f),new Color(.24f,.16f,.32f),new Color(.36f,.35f,.49f),new Color(.43f,.25f,.14f),new Color(.19f,.36f,.32f),new Color(.3f,.36f,.42f),new Color(.16f,.37f,.35f),new Color(.21f,.26f,.34f)};
 void DrawEncounterScene(GuxuStage stage){
  int region=Mathf.Clamp(stage.region-1,0,14);string resource="Game/Guxu/Expansion/Scenes/"+stage.sceneKey;
  if(Resources.Load<Texture2D>(resource)!=null)combatBackdrop=Img(resource,view,0,0,750,1334);
  else {
   combatBackdrop=Img("Game/Guxu/Final/background",view,0,0,750,1334,Color.Lerp(Color.white,EncounterColors[region],.48f));

  }
  combatBackdrop.name="Battle Scenery";combatBackdrop.color=Color.Lerp(combatBackdrop.color,new Color(.30f,.40f,.43f),.32f);
  GuxuParticleFx.Sort(combatBackdrop.transform,-40);
 }
 Image EncounterEnemy(GuxuStage stage,Transform arena){
  string key=stage.mode==0&&!stage.boss?"chapter-"+stage.region.ToString("00")+(stage.room%5==0?"-elite":"-normal"):stage.enemyKey;
  string path="Game/Guxu/Expansion/Enemies/"+key;bool supplied=Resources.Load<Texture2D>(path)!=null;
  float size=stage.boss?390:310;var enemyImage=Img(supplied?path:"Game/Guxu/Final/guardian",arena,stage.boss?365:405,stage.boss?440:500,size,size);enemyImage.preserveAspect=true;
  if(!supplied)enemyImage.color=Color.Lerp(Color.white,EncounterColors[Mathf.Clamp(stage.region-1,0,14)],.25f);
  var aura=Rect("Enemy Spirit Seal",enemyImage.transform,size*.18f,size*.4f,size*.65f,size*.65f).gameObject.AddComponent<GuxuSoftVfx>();aura.raycastTarget=false;aura.color=new Color(.4f,.8f,.9f,stage.boss?.2f:.1f);aura.transform.SetAsFirstSibling();
  if(stage.boss&&GuxuBossAnimations.Find(stage.enemyKey)!=null)enemyImage.gameObject.AddComponent<GuxuBossMotion>().Bind(stage.enemyKey);
  else enemyImage.gameObject.AddComponent<GuxuEnemyIdle>();return enemyImage;
 }
}
// Additional scenery stays behind the combatants and keeps both standing areas clear.
public sealed class GuxuEncounterBackdrop:MaskableGraphic {
 public int Theme;
 protected override void OnPopulateMesh(VertexHelper vh){
  vh.Clear();float h=rectTransform.rect.height;Color dark=Color.Lerp(color,Color.black,.56f);dark.a=.75f;Color light=Color.Lerp(color,Color.white,.35f);light.a=.65f;
  Quad(vh,new Vector2(0,-h),new Vector2(750,-h+130),dark);
  for(int i=0;i<7;i++){float x=25+i*113,top=-(60+(i*53+Theme*17)%145),bottom=-h+112;
   if(Theme==1||Theme==6||Theme==13){Strip(vh,new Vector2(x,bottom),new Vector2(x+22,top),13,dark);Strip(vh,new Vector2(x+17,top-50),new Vector2(x-28,top+36),8,dark);Strip(vh,new Vector2(x+17,top-70),new Vector2(x+63,top+22),8,dark);}
   else if(Theme==3||Theme==4||Theme==7||Theme==11){Triangle(vh,new Vector2(x-22,bottom),new Vector2(x+35,bottom),new Vector2(x+7,top),dark);Strip(vh,new Vector2(x+7,top),new Vector2(x+35,bottom),2,light);}
   else {Quad(vh,new Vector2(x,bottom),new Vector2(x+29,top),dark);Quad(vh,new Vector2(x-8,top-10),new Vector2(x+38,top+9),light);}
  }
  for(int i=0;i<3;i++){float radius=95+i*32;for(int j=0;j<64;j++){float a=j*Mathf.PI/32,b=(j+1)*Mathf.PI/32;Strip(vh,new Vector2(375+Mathf.Cos(a)*radius,-190+Mathf.Sin(a)*radius*.34f),new Vector2(375+Mathf.Cos(b)*radius,-190+Mathf.Sin(b)*radius*.34f),2,light);}}
  for(int i=0;i<12;i++){float x=(i*137+Theme*21)%730+10,y=-40-(i*47+Theme*13)%390;Quad(vh,new Vector2(x,y),new Vector2(x+3,y+9),light);}
 }
 static void Triangle(VertexHelper vh,Vector2 a,Vector2 b,Vector2 c,Color color){int start=vh.currentVertCount;vh.AddVert(a,color,Vector2.zero);vh.AddVert(b,color,Vector2.zero);vh.AddVert(c,color,Vector2.zero);vh.AddTriangle(start,start+1,start+2);}
 static void Quad(VertexHelper vh,Vector2 a,Vector2 b,Color color){int n=vh.currentVertCount;vh.AddVert(a,color,Vector2.zero);vh.AddVert(new Vector2(b.x,a.y),color,Vector2.zero);vh.AddVert(b,color,Vector2.zero);vh.AddVert(new Vector2(a.x,b.y),color,Vector2.zero);vh.AddTriangle(n,n+1,n+2);vh.AddTriangle(n,n+2,n+3);}
 static void Strip(VertexHelper vh,Vector2 a,Vector2 b,float width,Color color){Vector2 d=(b-a).normalized;var n=new Vector2(-d.y,d.x)*width*.5f;int i=vh.currentVertCount;vh.AddVert(a-n,color,Vector2.zero);vh.AddVert(a+n,color,Vector2.zero);vh.AddVert(b+n,color,Vector2.zero);vh.AddVert(b-n,color,Vector2.zero);vh.AddTriangle(i,i+1,i+2);vh.AddTriangle(i,i+2,i+3);}
}
public sealed class GuxuEnemyIdle:MonoBehaviour {
 Vector3 original;void Awake(){original=transform.localScale;}
 void Update(){transform.localScale=new Vector3(original.x*(1+Mathf.Sin(Time.time*2.1f)*.008f),original.y*(1+Mathf.Sin(Time.time*2.1f)*.012f),1);}
}
public sealed class GuxuBattleSound:MonoBehaviour {
 AudioSource source;AudioClip[] clips;
 void Awake(){source=gameObject.AddComponent<AudioSource>();source.volume=.16f;clips=new AudioClip[3];for(int k=0;k<3;k++){const int rate=22050;var data=new float[rate/5];var noise=new System.Random(17);for(int i=0;i<data.Length;i++){float t=(float)i/rate,fade=1f-(float)i/data.Length;float frequency=k==0?180+500*fade:k==1?520+t*1400:130+t*180;data[i]=(Mathf.Sin(t*frequency*Mathf.PI*2)*.65f+(k==0?(float)(noise.NextDouble()-.5)*.5f:0))*fade*fade;}clips[k]=AudioClip.Create("Guxu Battle Cue "+k,data.Length,1,rate,false);clips[k].SetData(data,0);}}
 public void Cue(int kind){source.PlayOneShot(clips[Mathf.Clamp(kind,0,2)]);}
 void OnDestroy(){if(clips!=null)foreach(var clip in clips)if(clip)Destroy(clip);}
}
}
