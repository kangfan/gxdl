using UnityEngine;
using UnityEngine.UI;

namespace Gxdl {
// Section: runtime cast runes.
// The approved action frames contain no painted rune; the seal glyphs are drawn here at play time
// so they can track the palm bone of the displayed frame instead of being burned into the art.
[RequireComponent(typeof(CanvasRenderer))]
public sealed class GuxuCastRuneField : MaskableGraphic {
 public int VertexCount {get;private set;}
 public int MeshBuildCount {get;private set;}
 public const int GlyphCount=5;
 // 0..1 during charge, held at 1 while flying.
 public float Charge {get;set;}
 // 0..1 after the release frame; at 1 the runes have reached the stone.
 public float Flight {get;set;}
 // Free-running seconds used for spin, independent of the frame clock.
 public float Spin {get;set;}
 static readonly float[] GlyphPhase={0f,.34f,.55f,.78f,.9f};
 static readonly Color Gold=new Color(1,.75f,.29f,1),Cream=new Color(1,.96f,.75f,1),Rim=new Color(.42f,.16f,.02f,1);

 // 构造函数里不能碰 gameObject：raycastTarget 的 setter 会去取 Canvas。
 public GuxuCastRuneField(){useLegacyMeshGeneration=false;}

 protected override void Awake(){base.Awake();raycastTarget=false;}

 public void SetState(float charge,float flight,float spin){
  Charge=Mathf.Clamp01(charge);Flight=Mathf.Clamp01(flight);Spin=spin;SetVerticesDirty();
 }

 protected override void OnPopulateMesh(VertexHelper vh){
  MeshBuildCount++;vh.Clear();VertexCount=0;
  float charge=Mathf.Clamp01(Charge),flight=Mathf.Clamp01(Flight);
  float gather=Mathf.SmoothStep(0,1,charge);
  for(int i=0;i<GlyphCount;i++){
   float stagger=Mathf.Clamp01((charge-GlyphPhase[i]*.45f)/.55f);
   if(stagger<=0)continue;
   // Picked between two fingers: the glyphs settle into a short vertical stack pinched in the hand,
   // then leave as one flat bundle because the throw itself is horizontal.
   float settle=Mathf.Lerp(56,15,gather);
   float slot=(i-(GlyphCount-1)*.5f);
   Vector2 center=new Vector2(Mathf.Sin(Spin*.6f)*settle*.3f,slot*settle*.78f);
   center.y+=Mathf.Cos(Spin*.9f)*settle*.22f;
   // On release the bundle trails one step behind the hand instead of orbiting it.
   center-=new Vector2(flight*10f,0);
   float size=Mathf.Lerp(8f,11f,gather)*(1+flight*.15f);
   float alpha=Mathf.Clamp01(stagger*1.6f);
   Color ink=Color.Lerp(Gold,Cream,.5f+.25f*Mathf.PerlinNoise(i*.73f,Spin*.9f));ink.a=alpha;
   Rune(vh,center,Mathf.PI*.5f+Mathf.Sin(Spin*.7f)*.12f,size,ink);
   // A thin pinch line through the stack reads as the fingers holding it, and it stays only while held.
   if(flight<=0&&i==0){
    Color ring=Gold;ring.a=alpha*.5f;
    Stroke(vh,new Vector2(-settle*.75f,0),new Vector2(settle*.75f,0),1.1f,ring,false);
   }
  }
  VertexCount=vh.currentVertCount;
 }

 static Vector2 Polar(float a,float r){return new Vector2(Mathf.Cos(a),Mathf.Sin(a))*r;}
 void Tri(VertexHelper v,Vector2 a,Vector2 b,Vector2 c,Color col){int n=v.currentVertCount;v.AddVert(a,col,Vector2.zero);v.AddVert(b,col,Vector2.zero);v.AddVert(c,col,Vector2.zero);v.AddTriangle(n,n+1,n+2);}
 void Quad(VertexHelper v,Vector2 a,Vector2 b,Vector2 c,Vector2 d,Color col){Tri(v,a,b,c,col);Tri(v,a,c,d,col);}
 // Soft halo, dark rim, bright core: the same stroke idiom as GuxuSpellLayer, but with a rim so the
 // mark survives on the bright sky as well as on the dark monolith.
 void Stroke(VertexHelper v,Vector2 a,Vector2 b,float width,Color col,bool glow=true){
  Vector2 n=new Vector2(-(b-a).y,(b-a).x).normalized*width*.5f;
  if(glow&&col.a>0){
   Color soft=col;soft.a*=.16f;Quad(v,a-n*3.4f,b-n*3.4f,b+n*3.4f,a+n*3.4f,soft);
   Color rim=Rim;rim.a*=.62f*col.a;Quad(v,a-n*1.9f,b-n*1.9f,b+n*1.9f,a+n*1.9f,rim);
  }
  Quad(v,a-n,b-n,b+n,a+n,col);
 }
 void Ring(VertexHelper v,Vector2 center,Vector2 radius,float width,Color col,float rotation=0,float sweep=6.283185f,int count=48){
  Vector2 previous=center+Vector2.Scale(Polar(rotation,1),radius);
  for(int i=1;i<=count;i++){Vector2 next=center+Vector2.Scale(Polar(rotation+sweep*i/count,1),radius);Stroke(v,previous,next,width,col,false);previous=next;}
 }
 void Rune(VertexHelper v,Vector2 p,float angle,float size,Color col){
  Vector2 up=Polar(angle,size),side=Polar(angle+Mathf.PI*.5f,size*.48f);
  Stroke(v,p-up,p+up,2.4f,col);Stroke(v,p-up*.6f,p+side,2f,col);Stroke(v,p+up*.3f-side,p+up*.7f+side,2f,col);Stroke(v,p+up*.9f,p+up*.25f+side,2f,col);
 }
}
}
