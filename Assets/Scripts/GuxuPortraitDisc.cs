using UnityEngine;
using UnityEngine.UI;

namespace Gxdl {
// A geometric stencil mask avoids inventing a separate painted asset for a simple circular portrait.
[RequireComponent(typeof(CanvasRenderer))]
public sealed class GuxuPortraitDisc:MaskableGraphic {
 public GuxuPortraitDisc(){useLegacyMeshGeneration=false;}
 protected override void OnPopulateMesh(VertexHelper vh){
  vh.Clear();var bounds=GetPixelAdjustedRect();var center=bounds.center;var radius=bounds.size*.5f;
  vh.AddVert(center,color,Vector2.zero);
  const int segments=64;
  for(int i=0;i<segments;i++){float angle=i*Mathf.PI*2/segments;vh.AddVert(center+new Vector2(Mathf.Cos(angle)*radius.x,Mathf.Sin(angle)*radius.y),color,Vector2.zero);}
  for(int i=0;i<segments;i++)vh.AddTriangle(0,i+1,(i+1)%segments+1);
 }
}
}
