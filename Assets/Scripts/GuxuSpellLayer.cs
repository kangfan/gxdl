using UnityEngine;
using UnityEngine.UI;

namespace Gxdl {
[RequireComponent(typeof(CanvasRenderer))]
public sealed class GuxuSpellLayer : MaskableGraphic {
 public int Kind;
 public int VertexCount {get;private set;}
 float phase,time,opacity;
 static readonly Color Gold=new Color(1,.75f,.29f,1),Cream=new Color(1,.96f,.75f,1),Violet=new Color(.77f,.55f,1,1);
 public GuxuSpellLayer(){useLegacyMeshGeneration=false;}
 public void Sample(float normalizedTime,float seconds){phase=Mathf.Clamp01(normalizedTime);time=seconds;SetVerticesDirty();}
 protected override void OnPopulateMesh(VertexHelper vh){
  vh.Clear();opacity=Mathf.Clamp01(phase*12+.03f)*Mathf.Clamp01((1-phase)*7);
  switch(Kind){case 0:Energy(vh);break;case 1:Mandala(vh);break;case 2:Chains(vh);break;case 3:Pillar(vh);break;case 4:Ribbons(vh);break;case 5:Butterflies(vh);break;case 6:Stones(vh);break;case 7:Shock(vh);break;}
  VertexCount=vh.currentVertCount;
 }
 static Vector2 Polar(float a,float r){return new Vector2(Mathf.Cos(a),Mathf.Sin(a))*r;}
 void Tri(VertexHelper v,Vector2 a,Vector2 b,Vector2 c,Color col){col.a*=opacity;int n=v.currentVertCount;v.AddVert(a,col,Vector2.zero);v.AddVert(b,col,Vector2.zero);v.AddVert(c,col,Vector2.zero);v.AddTriangle(n,n+1,n+2);}
 void Quad(VertexHelper v,Vector2 a,Vector2 b,Vector2 c,Vector2 d,Color col){Tri(v,a,b,c,col);Tri(v,a,c,d,col);}
 void Stroke(VertexHelper v,Vector2 a,Vector2 b,float width,Color col,bool glow=true){
  Vector2 n=new Vector2(-(b-a).y,(b-a).x).normalized*width*.5f;
  if(glow){Color soft=col;soft.a*=.12f;Quad(v,a-n*4,b-n*4,b+n*4,a+n*4,soft);soft.a*=1.5f;Quad(v,a-n*2,b-n*2,b+n*2,a+n*2,soft);}
  Quad(v,a-n,b-n,b+n,a+n,col);
 }
 void Ring(VertexHelper v,Vector2 center,Vector2 radius,float width,Color col,float rotation=0,float sweep=6.283185f,int count=64){
  Vector2 previous=center+Vector2.Scale(Polar(rotation,1),radius);
  for(int i=1;i<=count;i++){Vector2 next=center+Vector2.Scale(Polar(rotation+sweep*i/count,1),radius);Stroke(v,previous,next,width,col);previous=next;}
 }
 void Glow(VertexHelper v,Vector2 center,Vector2 size,Color col){
  for(int i=0;i<32;i++){
   int n=v.currentVertCount;Color outer=col;outer.a=0;Color inner=col;inner.a*=opacity;
   v.AddVert(center,inner,Vector2.zero);v.AddVert(center+Vector2.Scale(Polar(i*Mathf.PI/16,1),size),outer,Vector2.zero);v.AddVert(center+Vector2.Scale(Polar((i+1)*Mathf.PI/16,1),size),outer,Vector2.zero);v.AddTriangle(n,n+1,n+2);
  }
 }
 void Rune(VertexHelper v,Vector2 p,float angle,float size,Color col){
  Vector2 up=Polar(angle,size),side=Polar(angle+Mathf.PI*.5f,size*.48f);
  Stroke(v,p-up,p+up,1.1f,col);Stroke(v,p-up*.6f,p+side,1,col);Stroke(v,p+up*.3f-side,p+up*.7f+side,1,col);Stroke(v,p+up*.9f,p+up*.25f+side,1,col);
 }
 void Energy(VertexHelper v){
  float size=16+Mathf.SmoothStep(0,1,Mathf.Clamp01(phase*3))*30;
  Glow(v,Vector2.zero,Vector2.one*size*1.7f,Violet);Glow(v,Vector2.zero,Vector2.one*size,Cream);
  for(int j=0;j<3;j++)Ring(v,Vector2.zero,new Vector2(size*(1+j*.25f),size*(.62f+j*.08f)),1.5f,j==1?Violet:Gold,time*(j%2==0?6:-5)+j*2,4.7f,40);
  for(int i=0;i<14;i++){float p=Mathf.Repeat(phase*2+i*.071f,1);Vector2 pos=Polar(i*2.39996f+time*2,85*(1-p));Glow(v,pos,Vector2.one*(2+p*2),Cream);}
 }
 void Mandala(VertexHelper v){
  float r=160*Mathf.SmoothStep(.05f,1,Mathf.Clamp01(phase*3));
  Ring(v,Vector2.zero,new Vector2(r,r*.62f),2,Gold);Ring(v,Vector2.zero,new Vector2(r*.87f,r*.54f),1,Gold);Ring(v,Vector2.zero,new Vector2(r*.63f,r*.39f),1.6f,Cream);
  for(int j=0;j<2;j++)for(int i=0;i<6;i++){
   float a=i*Mathf.PI/3+time*.18f+j*Mathf.PI/6;
   Vector2 p=Vector2.Scale(Polar(a,r*.61f),new Vector2(1,.62f)),q=Vector2.Scale(Polar(a+Mathf.PI*2/3,r*.61f),new Vector2(1,.62f));Stroke(v,p,q,1,Gold);
  }
  for(int i=0;i<16;i++){float a=i*Mathf.PI/8-time*.12f;Rune(v,Vector2.Scale(Polar(a,r*.75f),new Vector2(1,.62f)),a,7,Gold);}
  Glow(v,Vector2.zero,new Vector2(r*.8f,r*.3f),new Color(1,.7f,.3f,.18f));
 }
 void Chains(VertexHelper v){
  float reveal=Mathf.Clamp01(phase*4),breakage=Mathf.Clamp01((phase-.625f)/.375f);
  for(int diagonal=0;diagonal<2;diagonal++)for(int i=0;i<17;i++){
   float s=(i-8)*20;if(Mathf.Abs(s)>170*reveal)continue;
   float a=diagonal==0?.68f:2.46f;Vector2 axis=Polar(a,1),normal=Polar(a+Mathf.PI*.5f,1);
   Vector2 p=axis*s+normal*Mathf.Sin(i*.7f)*(1-reveal)*22;
   if(breakage>0)p+=axis*Mathf.Sign(s)*(breakage*95)+normal*Mathf.Sin(i*4.2f)*breakage*65+Vector2.down*breakage*breakage*75;
   float turn=a+breakage*Mathf.Sin(i*2)*3;Vector2 previous=p+Polar(turn,12);
   for(int k=1;k<=16;k++){float t=k*Mathf.PI/8;Vector2 next=p+Polar(turn,Mathf.Cos(t)*12)+Polar(turn+Mathf.PI*.5f,Mathf.Sin(t)*(i%2==0?6:3));Stroke(v,previous,next,1.5f,Gold);previous=next;}
  }
  if(breakage>0)Glow(v,Vector2.zero,Vector2.one*(65+breakage*70),new Color(1,.88f,.5f,1-breakage));
 }
 void Pillar(VertexHelper v){
  float h=350*Mathf.Clamp01(phase*5),w=18+Mathf.Sin(phase*Mathf.PI)*22;
  Glow(v,new Vector2(0,h*.45f),new Vector2(w*3,h*.65f),Gold);
  Stroke(v,new Vector2(0,-8),new Vector2(0,h),w,new Color(1,.75f,.3f,.22f));Stroke(v,Vector2.zero,new Vector2(0,h),4,Cream);
  Ring(v,Vector2.zero,new Vector2(90,24),2,Gold);
  for(int i=0;i<11;i++){float p=Mathf.Repeat(time*1.7f+i*.091f,1);Vector2 c=new Vector2(Mathf.Sin(i*4.3f)*55,p*h);Stroke(v,c,c+Vector2.up*(8+i%3*4),1,Gold);}
 }
 void Ribbons(VertexHelper v){
  for(int j=0;j<3;j++){
   Vector2 previous=Vector2.zero;
   for(int i=0;i<=70;i++){float a=i/70f*Mathf.PI*1.8f+time*5+j*2.094f;Vector2 p=new Vector2(Mathf.Cos(a)*105,Mathf.Sin(a)*40+(i/70f-.5f)*90);
    if(i>0)Stroke(v,previous,p,Mathf.Sin(i/71f*Mathf.PI)*4.5f+.3f,j==1?Violet:Gold);previous=p;}
  }
 }
 void Butterflies(VertexHelper v){
  for(int i=0;i<7;i++){
   float a=i*2.39996f+phase*2;Vector2 p=Polar(a,25+phase*(65+i*8))+Vector2.up*phase*55;
   float flap=.16f+Mathf.Abs(Mathf.Sin(time*24+i*1.7f))*.84f;float s=9+i%3*3;
   Glow(v,p,Vector2.one*s*1.5f,new Color(.9f,.72f,1,.25f));
   for(int side=-1;side<=1;side+=2){
    ButterflyWing(v,p,s,side*flap,false);ButterflyWing(v,p,s,side*flap,true);
    Vector2 previous=p+Vector2.up*s*.38f;
    for(int k=1;k<=8;k++){
     float t=k/8f;Vector2 next=p+new Vector2(side*s*(.28f*t-.06f*t*t),s*(.38f+.45f*t-.12f*t*t));
     Stroke(v,previous,next,.38f,Gold,false);previous=next;
    }
    Glow(v,previous,Vector2.one*.8f,Cream);
   }
   Stroke(v,p-Vector2.up*s*.55f,p+Vector2.up*s*.28f,.85f,Gold,false);
   Glow(v,p+Vector2.up*s*.3f,new Vector2(1.1f,1.8f),Cream);
  }
 }
 static Vector2 WingBezier(Vector2 a,Vector2 b,Vector2 c,Vector2 d,float t){float u=1-t;return u*u*u*a+3*u*u*t*b+3*u*t*t*c+t*t*t*d;}
 static Vector2 WingEdge(float t,bool lower){
  float x=Mathf.Min(t,1)*3;int part=Mathf.Min(2,(int)x);x-=part;
  if(!lower){
   if(part==0)return WingBezier(new Vector2(0,.05f),new Vector2(.22f,.6f),new Vector2(.55f,1.25f),new Vector2(.96f,1.15f),x);
   if(part==1)return WingBezier(new Vector2(.96f,1.15f),new Vector2(1.12f,.93f),new Vector2(1.42f,.35f),new Vector2(1.08f,.12f),x);
   return WingBezier(new Vector2(1.08f,.12f),new Vector2(.86f,-.08f),new Vector2(.29f,-.12f),new Vector2(0,.05f),x);
  }
  if(part==0)return WingBezier(Vector2.zero,new Vector2(.35f,.05f),new Vector2(.78f,.04f),new Vector2(.88f,-.28f),x);
  if(part==1)return WingBezier(new Vector2(.88f,-.28f),new Vector2(1.02f,-.62f),new Vector2(.49f,-.67f),new Vector2(.48f,-1.02f),x);
  return WingBezier(new Vector2(.48f,-1.02f),new Vector2(.22f,-.88f),new Vector2(.09f,-.34f),Vector2.zero,x);
 }
 void ButterflyWing(VertexHelper v,Vector2 p,float size,float horizontal,bool lower){
  const int segments=36,bands=3;Vector2 hub=lower?new Vector2(.27f,-.28f):new Vector2(.39f,.4f);
  Vector2 scale=new Vector2(size*horizontal,size);
  Color jade=new Color(.21f,.82f,.66f,.92f),rim=new Color(1,.84f,.42f,.95f);
  int first=v.currentVertCount;
  for(int band=0;band<=bands;band++)for(int i=0;i<=segments;i++){
   float radius=band/(float)bands;Vector2 point=Vector2.Lerp(hub,WingEdge(i/(float)segments,lower),radius);
   Color tint=Color.Lerp(jade,rim,Mathf.Pow(radius,2));tint.a*=opacity;
   v.AddVert(p+Vector2.Scale(point,scale),tint,Vector2.zero);
  }
  for(int band=0;band<bands;band++)for(int i=0;i<segments;i++){
   int a=first+band*(segments+1)+i,b=a+segments+1;v.AddTriangle(a,b,b+1);v.AddTriangle(a,b+1,a+1);
  }
  for(int i=0;i<segments;i++)Stroke(v,p+Vector2.Scale(WingEdge(i/(float)segments,lower),scale),p+Vector2.Scale(WingEdge((i+1)/(float)segments,lower),scale),.42f,Gold,false);
  // Curved veins spread from the thorax; the edge contour is sampled independently of flap width.
  for(int vein=0;vein<4;vein++){
   Vector2 end=WingEdge(.16f+vein*.16f,lower),start=new Vector2(.06f,lower?-.06f:.08f),previous=p+Vector2.Scale(start,scale);
   for(int k=1;k<=10;k++){
    Vector2 point=WingBezier(start,new Vector2(end.x*.25f,end.y*.7f),new Vector2(end.x*.72f,end.y*.96f),end,k/10f);
    Vector2 next=p+Vector2.Scale(point,scale);Stroke(v,previous,next,.3f,new Color(1,.91f,.6f,.85f),false);previous=next;
   }
  }
 }
 void Stones(VertexHelper v){
  for(int i=0;i<14;i++){
   float a=i*2.39996f;Vector2 p=Polar(a,18+phase*(100+i%4*20))+Vector2.down*phase*phase*55;
   float size=8+i%4*4,spin=a+phase*(i%2==0?3:-4);Vector2 a0=p+Polar(spin,size),b=p+Polar(spin+1.2f,size*.7f),c=p+Polar(spin+3.1f,size),d=p+Polar(spin+4.7f,size*.55f);
   Glow(v,p,Vector2.one*size*2,new Color(1,.66f,.25f,.25f));Quad(v,a0,b,c,d,new Color(.3f,.24f,.2f,1));Tri(v,p,a0,b,new Color(.67f,.48f,.25f,1));Stroke(v,a0,b,1,Gold);Stroke(v,b,c,1,Gold);Rune(v,p,spin,size*.45f,Cream);
   Stroke(v,p-Polar(a,12),p-Polar(a,25),1,new Color(1,.83f,.4f,.4f));
  }
 }
 void Shock(VertexHelper v){
  float r=15+210*(1-Mathf.Pow(1-phase,2));
  Ring(v,Vector2.zero,new Vector2(r,r*.4f),3*(1-phase)+.5f,Gold);
  Ring(v,Vector2.zero,new Vector2(r*.85f,r*.34f),1.2f,Violet);
  Glow(v,Vector2.zero,new Vector2(90*(1-phase),45*(1-phase)),new Color(1,.9f,.65f,.8f));
  for(int i=0;i<18;i++){float a=i*Mathf.PI/9;Vector2 p=Vector2.Scale(Polar(a,r),new Vector2(1,.4f));Stroke(v,p,p+Vector2.Scale(Polar(a,8*(1-phase)),new Vector2(1,.4f)),1,Cream);}
 }
}
}
