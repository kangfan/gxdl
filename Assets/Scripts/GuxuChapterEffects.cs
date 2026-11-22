using UnityEngine;
using UnityEngine.UI;

namespace Gxdl {
[RequireComponent(typeof(CanvasRenderer))]
public sealed class GuxuChapterMist:MaskableGraphic {
 public Texture Source;
 public override Texture mainTexture=>Source?Source:Texture2D.whiteTexture;
 protected override void OnPopulateMesh(VertexHelper mesh){
  mesh.Clear();var rect=rectTransform.rect;
  for(int i=0;i<16;i++){
   float a=i/16f,b=(i+1)/16f;var lower=color;var upper=color;lower.a*=Mathf.Sin(a*Mathf.PI);upper.a*=Mathf.Sin(b*Mathf.PI);
   int start=mesh.currentVertCount;
   mesh.AddVert(new Vector3(rect.xMin,Mathf.Lerp(rect.yMin,rect.yMax,a)),lower,new Vector2(0,a*.13f));mesh.AddVert(new Vector3(rect.xMax,Mathf.Lerp(rect.yMin,rect.yMax,a)),lower,new Vector2(.34f,a*.13f));
   mesh.AddVert(new Vector3(rect.xMax,Mathf.Lerp(rect.yMin,rect.yMax,b)),upper,new Vector2(.34f,b*.13f));mesh.AddVert(new Vector3(rect.xMin,Mathf.Lerp(rect.yMin,rect.yMax,b)),upper,new Vector2(0,b*.13f));mesh.AddTriangle(start,start+1,start+2);mesh.AddTriangle(start,start+2,start+3);
  }
 }
}
}
