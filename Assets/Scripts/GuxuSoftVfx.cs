using UnityEngine;
using UnityEngine.UI;

namespace Gxdl {
// Vertex-alpha effects keep the UI animation independent of render-pipeline particle shaders.
[RequireComponent(typeof(CanvasRenderer))]
public sealed class GuxuSoftVfx : MaskableGraphic {
 public bool Smoke;
 public float Seed;
 public int MeshBuildCount { get; private set; }
 public GuxuSoftVfx(){useLegacyMeshGeneration=false;}
 protected override void OnPopulateMesh(VertexHelper vh){
  MeshBuildCount++;vh.Clear();var rect=rectTransform.rect;
  if(!Smoke){Blob(vh,rect.center,rect.size*.5f,color);return;}
  for(int i=0;i<6;i++){
   float angle=i*2.39996f+Seed,r=.16f+(i%3)*.045f;
   Vector2 offset=new Vector2(Mathf.Cos(angle)*rect.width*r,Mathf.Sin(angle)*rect.height*r);
   Color shade=Color.Lerp(color,new Color(.88f,.79f,.61f,color.a),Mathf.Clamp01(offset.y/rect.height+.25f));shade.a*=.65f;
   Blob(vh,rect.center+offset,rect.size*(.25f+i%3*.025f),shade);
  }
 }
 static void Blob(VertexHelper vh,Vector2 center,Vector2 radius,Color tint){
  const int segments=24,rings=5;int first=vh.currentVertCount;
  vh.AddVert(center,tint,Vector2.zero);
  for(int ring=1;ring<=rings;ring++){
   float r=ring/(float)rings;Color shade=tint;shade.a*=Mathf.Pow(1-r*r,2);
   for(int i=0;i<segments;i++){float a=i*Mathf.PI*2/segments;vh.AddVert(center+new Vector2(Mathf.Cos(a)*radius.x,Mathf.Sin(a)*radius.y)*r,shade,Vector2.zero);}
  }
  for(int i=0;i<segments;i++)vh.AddTriangle(first,first+1+i,first+1+(i+1)%segments);
  for(int ring=0;ring<rings-1;ring++)for(int i=0;i<segments;i++){
   int a=first+1+ring*segments+i,b=first+1+ring*segments+(i+1)%segments;
   vh.AddTriangle(a,b,b+segments);vh.AddTriangle(a,b+segments,a+segments);
  }
 }
}
}
