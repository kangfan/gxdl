using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasRenderer))]
public sealed class GuxuStoryShade : MaskableGraphic {
 public bool Top;
 protected override void Awake(){base.Awake();raycastTarget=false;}
 protected override void OnPopulateMesh(VertexHelper vh){
  vh.Clear();var r=rectTransform.rect;
  for(int i=0;i<=12;i++){
   float p=i/12f;var tint=new Color(.015f,.035f,.055f,Top?.72f*p:.96f*Mathf.Pow(1-p,.62f));
   vh.AddVert(new Vector3(r.xMin,Mathf.Lerp(r.yMin,r.yMax,p)),tint,Vector2.zero);
   vh.AddVert(new Vector3(r.xMax,Mathf.Lerp(r.yMin,r.yMax,p)),tint,Vector2.zero);
   if(i>0){int n=i*2;vh.AddTriangle(n-2,n,n+1);vh.AddTriangle(n-2,n+1,n-1);}
  }
 }
}
