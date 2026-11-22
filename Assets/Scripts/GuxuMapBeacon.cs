using UnityEngine;
using UnityEngine.UI;

namespace Gxdl {
// Light the entrance without pulling its mountain away from the surrounding terrain.
public sealed class GuxuMapBeacon : MonoBehaviour {
 Graphic halo;float phase,highlightUntil;bool unlocked;
 public bool IsHighlighting=>unlocked&&Time.unscaledTime<highlightUntil;
 public void Bind(Graphic light,bool open,int index){
  halo=light;unlocked=open;phase=index*.7f;
  if(!open)halo.color=new Color(.8f,.87f,.92f,.65f);
 }
 public void Highlight(){if(unlocked)highlightUntil=Time.unscaledTime+4.2f;}
 void Update(){
  if(!unlocked||halo==null)return;
  float wave=.5f+.5f*Mathf.Sin(Time.unscaledTime*5.4f+phase);
  float strength=IsHighlighting?Mathf.Clamp01((highlightUntil-Time.unscaledTime)/.5f):0;
  halo.rectTransform.localScale=Vector3.one*(1+wave*.12f*strength);
  var tint=halo.color;tint.a=.06f+wave*.48f*strength;halo.color=tint;
 }
}
}
