using System;
using UnityEngine;
using UnityEngine.UI;

namespace Gxdl {
public partial class GxdlGame {
 partial void CreateEntryLoadingView(RectTransform parent,ref IGuxuEntryLoadingView loadingView){
  const string path="Game/Guxu/Expansion/UI/Loading/";
  var background=Art(Sprite(path+"background"),parent,0,0,750,1334,false);
  background.name="Loading Background";background.raycastTarget=true;
  background.rectTransform.anchorMin=background.rectTransform.anchorMax=background.rectTransform.pivot=Vector2.one*.5f;
  background.rectTransform.anchoredPosition=Vector2.zero;
  var logoTexture=Resources.Load<Texture2D>(path+"logo-clean");float logoHeight=logoTexture.height*.82f;
  var chineseLogo=UnityEngine.Sprite.Create(logoTexture,new Rect(0,logoTexture.height-logoHeight,logoTexture.width,logoHeight),Vector2.one*.5f,100);
  var logo=Art(chineseLogo,parent,160,65,430,162,false);logo.name="Loading Logo";
  var petals=new Image[12];
  for(int i=0;i<petals.Length;i++){
   float size=13+i%4*4;petals[i]=Art(Sprite(path+"petal"),parent,0,0,size,size,false);petals[i].name="Loading Petal "+i;
  }
  Txt("加载中……",parent,260,1035,230,43,29,new Color(1,.91f,.64f)).name="Loading Label";
  Art(Sprite(path+"divider"),parent,193,1047,78,18,false);
  Art(Sprite(path+"divider"),parent,479,1047,78,18,false);
  Art(Sprite(path+"frame-clean"),parent,65,1088,620,68,false).name="Loading Frame";
  var track=Rect("Loading Track",parent,110,1107,530,29);track.gameObject.AddComponent<RectMask2D>();
  var fill=Art(Sprite(path+"fill"),track,0,0,530,29,false);fill.name="Loading Fill";
  fill.type=Image.Type.Filled;fill.fillMethod=Image.FillMethod.Horizontal;fill.fillOrigin=(int)Image.OriginHorizontal.Left;fill.fillAmount=0;
  var head=Art(Sprite(path+"head"),track,-124,-6,132,41,false);head.name="Loading Progress Glow";
  var percent=Txt("0%",parent,285,1092,180,58,24,new Color(1,.97f,.84f));percent.name="Loading Percent";
  var shadow=percent.gameObject.AddComponent<Shadow>();shadow.effectColor=new Color(.02f,.12f,.16f,.95f);shadow.effectDistance=new Vector2(1,-1);
  var phase=Txt("",parent,90,1160,570,48,21,new Color(.98f,.96f,.88f));phase.name="Loading Phase";
  shadow=phase.gameObject.AddComponent<Shadow>();shadow.effectColor=new Color(.06f,.20f,.34f,.85f);shadow.effectDistance=new Vector2(0,-1);
  var screen=parent.gameObject.AddComponent<GuxuLoadingScreen>();
  screen.Initialize(background,fill,head,percent,phase,petals);loadingView=screen;
 }
}

// Presentation only: resource readiness and page transitions belong to GuxuEntryFlow.
public sealed class GuxuLoadingScreen:MonoBehaviour,IGuxuEntryLoadingView {
 Image background,fill,head;
 Text percent,phase;
 Image[] petals;
 RectTransform page,canvas;
 float target,displayed,completeFor,elapsed;
 public float DisplayedProgress=>displayed;
 public bool IsComplete=>isActiveAndEnabled&&target==1&&displayed==1&&completeFor>=.35f;

 public void Initialize(Image background,Image fill,Image head,Text percent,Text phase,Image[] petals){
  this.background=background;this.fill=fill;this.head=head;this.percent=percent;this.phase=phase;this.petals=petals;
  page=(RectTransform)transform;canvas=(RectTransform)GetComponentInParent<Canvas>().transform;
  RefreshBackground();Render();
 }
 public void SetProgress(float progress,string message){
  if(float.IsNaN(progress)||float.IsInfinity(progress))throw new ArgumentOutOfRangeException(nameof(progress));
  target=Mathf.Max(target,Mathf.Clamp01(progress));phase.text=message??"";
 }
 void Update(){
  if(!fill)return;
  float dt=Time.unscaledDeltaTime;elapsed+=dt;
  displayed=Mathf.MoveTowards(displayed,target,dt*.5f);
  if(displayed==1&&target==1)completeFor+=dt;
  Render();RefreshBackground();
  for(int i=0;i<petals.Length;i++){
   float cycle=Mathf.Repeat(elapsed*(.035f+i%3*.008f)+i*.137f,1);
   var rect=petals[i].rectTransform;
   rect.anchoredPosition=new Vector2(Mathf.Repeat(91+i*131+cycle*190,790)-20+Mathf.Sin(elapsed*.5f+i)*28,60-cycle*1430);
   rect.localEulerAngles=new Vector3(0,0,i*47+elapsed*(11+i%4*4));
   petals[i].color=new Color(1,1,1,Mathf.Sin(cycle*Mathf.PI)*(.45f+i%3*.15f));
  }
 }
 void Render(){
  fill.fillAmount=displayed;percent.text=(displayed==1?100:Mathf.FloorToInt(displayed*100))+"%";
  head.rectTransform.anchoredPosition=new Vector2(530*displayed-124,6);
  head.color=new Color(1,1,1,displayed>0&&displayed<1?.72f+.2f*Mathf.Sin(elapsed*4):0);
 }
 void RefreshBackground(){
  if(!background||!background.sprite)return;
  // FitPage keeps controls in the safe portrait area; artwork also covers wider/taller viewports.
  float scale=Mathf.Min(canvas.rect.width/750f,canvas.rect.height/1334f);
  if(scale<=0)return;
  page.localScale=Vector3.one*scale;page.anchoredPosition=new Vector2(-375,667)*scale;
  var size=background.sprite.rect.size;
  float cover=Mathf.Max(canvas.rect.width/scale/size.x,canvas.rect.height/scale/size.y);
  background.rectTransform.sizeDelta=size*cover;
 }
}
}
