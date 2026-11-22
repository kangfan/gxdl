using System.Collections;
using UnityEngine;
using UnityEngine.UI;
namespace Gxdl {
public partial class GxdlGame {
 IEnumerator FinalRelicFeedback(Gear gear,bool equipped){
  int version=screenVersion;var parent=Rect("Relic Settlement Glow",view,0,0,750,1334);
  var light=Rect("Jade Energy",parent,seeker.rectTransform.anchoredPosition.x,-seeker.rectTransform.anchoredPosition.y,seeker.rectTransform.rect.width,seeker.rectTransform.rect.height).gameObject.AddComponent<GuxuSoftVfx>();light.raycastTarget=false;
  var icon=Art(GuxuUiArt.Item(gear.slot),parent,401,592,135,135);var r=icon.rectTransform;r.pivot=new Vector2(.5f,.5f);
  Vector2 start=view.InverseTransformPoint(ancientStone.rectTransform.TransformPoint(ancientStone.rectTransform.rect.center));Vector2 end=view.InverseTransformPoint(seeker.rectTransform.TransformPoint(seeker.rectTransform.rect.center));Color tint=GuxuUiArt.QualityColor(gear.quality);
  for(float t=0;t<.85f;t+=Time.deltaTime){if(version!=screenVersion)yield break;float p=Mathf.Clamp01(t/.85f);float ease=p*p*(3-2*p);r.anchoredPosition=equipped?Vector2.Lerp(start,end,ease)+Vector2.up*(Mathf.Sin(p*Mathf.PI)*70):start+Vector2.up*(Mathf.Sin(p*Mathf.PI)*35);r.localScale=Vector3.one*(equipped?1-.8f*ease:1+.25f*ease);icon.color=new Color(1,1,1,1-ease);light.color=new Color(tint.r,tint.g,tint.b,equipped?Mathf.Sin(p*Mathf.PI)*.45f:0);yield return null;}
  if(parent)Destroy(parent.gameObject);
 }
}
}
