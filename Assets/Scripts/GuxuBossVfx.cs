using UnityEngine;
using UnityEngine.UI;

namespace Gxdl {
public sealed class GuxuBossVfx:MaskableGraphic {
 GuxuBossDefinition boss;GuxuBossAttack attack;Vector2 source,target;float time;Image[] summons;
 public void Bind(GuxuBossDefinition definition,GuxuBossAttack clip){
  boss=definition;attack=clip;color=boss.Color;raycastTarget=false;
  if(clip.effect=="summon"){
   summons=new Image[3];for(int i=0;i<3;i++){var go=new GameObject("Summoned Spider "+i,typeof(RectTransform),typeof(CanvasRenderer),typeof(Image));go.transform.SetParent(transform,false);var image=go.GetComponent<Image>();image.sprite=boss.Frame(boss.idleFrame);image.preserveAspect=true;image.raycastTarget=false;image.rectTransform.anchorMin=image.rectTransform.anchorMax=Vector2.one*.5f;image.rectTransform.sizeDelta=new Vector2(100,100);image.color=Color.clear;summons[i]=image;}
  }
 }
 public void Sample(float elapsed,Vector2 from,Vector2 to){time=elapsed;source=from;target=to;SetVerticesDirty();if(summons!=null){float p=Mathf.Clamp01((time/attack.duration-.4f)/.4f),a=Mathf.Sin(p*Mathf.PI);for(int i=0;i<summons.Length;i++){summons[i].rectTransform.anchoredPosition=Vector2.Lerp(source,target,p)+new Vector2((i-1)*65,-130+i%2*15);summons[i].color=new Color(1,1,1,a);}}}
 protected override void OnPopulateMesh(VertexHelper vh){
  vh.Clear();if(boss==null||attack==null)return;
  float first=attack.impacts[0],charge=Mathf.Clamp01(time/Mathf.Max(.01f,first)),tail=Mathf.Clamp01((attack.duration-time)/.2f);Color tint=boss.Color;
  Vector2 hand=source+new Vector2(-80,30),ground=target+Vector2.down*150;
  if(time<first){float a=Mathf.Sin(charge*Mathf.PI)*.7f;Ring(vh,hand,new Vector2(18+charge*25,13+charge*20),0,360,2,Fade(tint,a));Blob(vh,hand,16+charge*18,Fade(tint,a*.45f));}
  string kind=attack.effect;
  if(kind=="slash"||kind=="charge"){
   float p=Mathf.Clamp01((time-first+.32f)/.48f),a=Mathf.Sin(p*Mathf.PI)*tail;
   Ring(vh,Vector2.Lerp(source,target,.48f),new Vector2(Mathf.Abs(source.x-target.x)*.55f+40,70),35+p*140,260,14*a,Fade(tint,a));
   if(kind=="charge")for(int i=0;i<6;i++)Blob(vh,source+new Vector2(30+i*20,-130),35,Fade(tint,a*(1-i/7f)*.35f));
  }else if(kind=="projectile"||kind=="chain"){
   float p=Mathf.Clamp01((time-first+.36f)/.36f);if(time<first&&p>0){Vector2 point=Vector2.Lerp(hand,target,p);Blob(vh,point,25,Fade(tint,.9f));
    if(kind=="chain"){for(int i=0;i<14;i++){Vector2 q=Vector2.Lerp(hand,point,i/14f);Ring(vh,q+Vector2.up*Mathf.Sin(i*.7f)*8,new Vector2(9,5),0,360,2,Fade(tint,.8f));}}
    else {Line(vh,point,point+Vector2.right*65,7,Fade(tint,.45f));if(boss.theme=="ice")Spike(vh,point,Vector2.left,27,Fade(tint,1));}
   }
  }else if(kind=="rain"||kind=="lightning"||kind=="meteor"){
   foreach(float impact in attack.impacts)for(int i=0;i<5;i++){
    float age=time-impact+i*.018f,p=Mathf.Clamp01((age+.28f)/.28f);Vector2 end=ground+new Vector2((i-2)*47,8);
    if(age<0&&age>-.28f){Vector2 point=end+Vector2.up*(1-p)*320;Line(vh,point,point+new Vector2(kind=="meteor"?65:0,75),kind=="meteor"?10:3,Fade(tint,p));}
    if(age>=0&&age<.2f){float a=1-age/.2f;if(kind=="lightning"){Vector2 prev=end+Vector2.up*330;for(int j=1;j<=9;j++){Vector2 next=end+new Vector2(Mathf.Sin(j*8+i*2)*24,330-j*330/9f);Line(vh,prev,next,4,Fade(tint,a));prev=next;}}Blob(vh,end,40,Fade(tint,a*.8f));}
   }
  }else if(kind=="domain"||kind=="cage"||kind=="web"||kind=="vortex"||kind=="spirit"){
   float p=Mathf.Clamp01((time-first+.45f)/.45f),a=p*tail;Vector2 center=Vector2.Lerp(source,target,.55f)+Vector2.down*150;float radius=240*p;
   Ring(vh,center,new Vector2(radius,45*p),0,360,4,Fade(tint,a*.8f));Ring(vh,center,new Vector2(radius*.8f,34*p),0,360,2,Fade(tint,a*.5f));
   for(int i=0;i<12;i++){float angle=i*Mathf.PI/6+time*.7f;Vector2 q=center+new Vector2(Mathf.Cos(angle)*radius,Mathf.Sin(angle)*45*p);
    if(kind=="web")Line(vh,center,q,2,Fade(tint,a*.65f));
    else if(kind=="vortex")Ring(vh,center+Vector2.up*(i%3*55),new Vector2(radius*(.7f+i%3*.1f),30),time*130+i*30,190,6,Fade(tint,a*.5f));
    else Line(vh,q,q+Vector2.up*(kind=="spirit"?210:135)*p,3,Fade(tint,a*.6f));
   }
   if(kind=="spirit")Blob(vh,source+Vector2.up*140,95,Fade(tint,a*.22f));
  }else if(kind=="poison"||kind=="summon"){
   float a=Mathf.Clamp01((time-first+.4f)/.4f)*tail;for(int i=0;i<9;i++){Vector2 q=ground+new Vector2(Mathf.Sin(i*2.4f)*145,Mathf.Cos(i*1.7f)*20+time*8);Blob(vh,q,40+i%3*14,Fade(tint,a*.22f));}
  }
  foreach(float impact in attack.impacts){
   float age=time-impact;if(age<0||age>.45f)continue;float p=age/.45f,a=(1-p)*tail;
   Blob(vh,target,22+75*p,Fade(tint,a*.6f));Ring(vh,ground,new Vector2(30+180*p,10+35*p),0,360,3,Fade(tint,a*.8f));
   for(int i=0;i<10;i++){float angle=i*2.399f;Vector2 q=target+new Vector2(Mathf.Cos(angle),Mathf.Sin(angle))*(15+p*(55+i%3*20))-Vector2.up*p*p*30;Line(vh,q,q+new Vector2(Mathf.Cos(angle),Mathf.Sin(angle))*7,2,Fade(tint,a));}
   if(kind=="slam"||kind=="roots")for(int i=0;i<7;i++){
    Vector2 q=ground+new Vector2((i-3)*37,Mathf.Sin(i)*8);float h=(60+i%3*34)*Mathf.Sin(p*Mathf.PI);
    if(boss.theme=="fire"){Blob(vh,q+Vector2.up*h*.4f,h*.5f,Fade(tint,a*.45f));Spike(vh,q,Vector2.up,h,Fade(tint,a));}
    else if(boss.theme=="wood"){Line(vh,q,q+new Vector2(Mathf.Sin(i)*25,h),7,Fade(tint,a));Line(vh,q+Vector2.up*h*.5f,q+new Vector2(22,h*.8f),4,Fade(tint,a));}
    else Spike(vh,q,Vector2.up,h,Fade(tint,a));
   }
  }
 }
 static Color Fade(Color c,float a){c.a=Mathf.Clamp01(a);return c;}
 static void Line(VertexHelper vh,Vector2 a,Vector2 b,float width,Color tint){
  if(width<=0||tint.a<=0)return;Vector2 n=new Vector2(a.y-b.y,b.x-a.x).normalized*width;int i=vh.currentVertCount;Color edge=Fade(tint,0),core=Color.Lerp(tint,Color.white,.58f);core.a=tint.a;
  vh.AddVert(a-n,edge,Vector2.zero);vh.AddVert(a,core,Vector2.zero);vh.AddVert(a+n,edge,Vector2.zero);vh.AddVert(b-n,edge,Vector2.zero);vh.AddVert(b,core,Vector2.zero);vh.AddVert(b+n,edge,Vector2.zero);
  vh.AddTriangle(i,i+1,i+4);vh.AddTriangle(i,i+4,i+3);vh.AddTriangle(i+1,i+2,i+5);vh.AddTriangle(i+1,i+5,i+4);
 }
 static void Ring(VertexHelper vh,Vector2 center,Vector2 radius,float start,float sweep,float width,Color tint){for(int i=0;i<40;i++){float a=(start+sweep*i/40)*Mathf.Deg2Rad,b=(start+sweep*(i+1)/40)*Mathf.Deg2Rad;Line(vh,center+new Vector2(Mathf.Cos(a)*radius.x,Mathf.Sin(a)*radius.y),center+new Vector2(Mathf.Cos(b)*radius.x,Mathf.Sin(b)*radius.y),width,tint);}}
 static void Blob(VertexHelper vh,Vector2 center,float radius,Color tint){if(tint.a<=0)return;int n=vh.currentVertCount;vh.AddVert(center,tint,Vector2.zero);for(int i=0;i<=24;i++){float a=i*Mathf.PI/12;vh.AddVert(center+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*radius,Fade(tint,0),Vector2.zero);if(i>0)vh.AddTriangle(n,n+i,n+i+1);}}
 static void Spike(VertexHelper vh,Vector2 origin,Vector2 direction,float length,Color tint){Vector2 side=new Vector2(-direction.y,direction.x)*length*.15f;int i=vh.currentVertCount;vh.AddVert(origin-side,tint,Vector2.zero);vh.AddVert(origin+direction*length,Color.Lerp(tint,Color.white,.65f),Vector2.zero);vh.AddVert(origin+side,tint,Vector2.zero);vh.AddTriangle(i,i+1,i+2);}
}
}
