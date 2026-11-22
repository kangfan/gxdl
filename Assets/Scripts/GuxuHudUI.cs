using System;
using UnityEngine;
using UnityEngine.UI;

namespace Gxdl {
public partial class GxdlGame {
 Text HudText(string value,Transform parent,float x,float y,float width,float height,int size,Color color,TextAnchor align=TextAnchor.MiddleLeft){
  var text=Txt(value,parent,x,y,width,height,size,color,align);var outline=text.gameObject.AddComponent<Outline>();outline.effectColor=new Color(.025f,.075f,.10f,.95f);outline.effectDistance=new Vector2(1.2f,-1.2f);return text;
 }
 Button HudAction(string name,Transform parent,float x,float y,float width,float height,Action action){var button=Btn("",parent,x,y,width,height,action,null,Color.clear);button.name=name;return button;}
 Button HudToggle(string name,string label,Transform parent,float x,float y,float width,float height,bool on,Action action){
  var button=HudAction(name,parent,x,y,width,height,action);
  Art(GuxuUiArt.Optional(on?"hud-toggle-on":"hud-toggle-off"),button.transform,4,(height-24)*.5f,64,24,false).name="Toggle Track";
  var knob=Rect("Toggle Thumb",button.transform,on?47:7,(height-18)*.5f,18,18).gameObject.AddComponent<GuxuPortraitDisc>();knob.raycastTarget=false;knob.color=cream;
  HudText(label,button.transform,80,0,width-84,height,24,cream).name="Toggle Caption";return button;
 }
 void RefreshHudToggle(Button button,bool on,string label){
  if(!button)return;button.transform.Find("Toggle Track").GetComponent<Image>().sprite=GuxuUiArt.Optional(on?"hud-toggle-on":"hud-toggle-off");
  var thumb=(RectTransform)button.transform.Find("Toggle Thumb");thumb.anchoredPosition=new Vector2(on?47:7,thumb.anchoredPosition.y);button.GetComponentInChildren<Text>().text=label;
 }
 void HudPortrait(Transform parent,float x,float y,float size,string name){
  var rim=Rect(name+" Rim",parent,x-2,y-2,size+4,size+4).gameObject.AddComponent<GuxuPortraitDisc>();rim.color=gold;rim.raycastTarget=false;
  var clip=Rect(name+" Mask",parent,x,y,size,size);var disc=clip.gameObject.AddComponent<GuxuPortraitDisc>();disc.color=jade;disc.raycastTarget=false;clip.gameObject.AddComponent<Mask>().showMaskGraphic=true;
  Art(GuxuUiArt.Portrait,clip,-3,-3,size+6,size+6).name=name;
 }
}
}
