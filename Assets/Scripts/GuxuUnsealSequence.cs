using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using Spine.Unity;

namespace Gxdl {
public sealed partial class GuxuAnimation : MonoBehaviour {
 // Legacy QA/API names remain source compatible; every unseal now uses the ordinary sequence.
 public const float FullDuration=CommonDuration;
 public float FullElapsed {get;private set;}
 public IEnumerator PlayUnsealFull(Sprite swordSprite,Sprite relicSprite,int quality){
  FullElapsed=0;yield return PlayUnseal(swordSprite,relicSprite,quality);FullElapsed=FullDuration;
 }
}
// Tapered elliptical light trails: independent animated UI meshes, not baked frames.
public sealed class GuxuSpiritRibbon:MaskableGraphic {
 float flow;
 public GuxuSpiritRibbon(){useLegacyMeshGeneration=false;}
 public void SetFlow(float value){flow=value;SetVerticesDirty();}
 protected override void OnPopulateMesh(VertexHelper vh){
  vh.Clear();var rect=rectTransform.rect;float rx=rect.width*.5f,ry=rect.height*.5f;
  const int steps=80;
  for(int glow=2;glow>=0;glow--)for(int i=0;i<steps;i++){
   float u=i/(float)steps,v=(i+1f)/steps;
   float a=flow+u*Mathf.PI*1.75f,b=flow+v*Mathf.PI*1.75f;
   Vector2 p=new Vector2(Mathf.Cos(a)*rx,Mathf.Sin(a)*ry),q=new Vector2(Mathf.Cos(b)*rx,Mathf.Sin(b)*ry);
   Vector2 n=new Vector2(ry*Mathf.Cos(a),rx*Mathf.Sin(a)).normalized,m=new Vector2(ry*Mathf.Cos(b),rx*Mathf.Sin(b)).normalized;
   float width=glow==2?10:glow==1?4:1.2f;
   float wp=Mathf.Pow(Mathf.Max(0,Mathf.Sin(u*Mathf.PI)),1.2f)*width,wq=Mathf.Pow(Mathf.Max(0,Mathf.Sin(v*Mathf.PI)),1.2f)*width;
   Color tint=glow==0?Color.Lerp(color,Color.white,.55f):color;tint.a=color.a*(glow==2?.08f:glow==1?.22f:.9f);
   int first=vh.currentVertCount;vh.AddVert(p-n*wp,tint,Vector2.zero);vh.AddVert(p+n*wp,tint,Vector2.zero);vh.AddVert(q+m*wq,tint,Vector2.zero);vh.AddVert(q-m*wq,tint,Vector2.zero);
   vh.AddTriangle(first,first+1,first+2);vh.AddTriangle(first,first+2,first+3);
  }
 }
}
}
