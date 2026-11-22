using UnityEngine;
using UnityEngine.UI;

namespace Gxdl {
// The wing vertices rotate around their own shoulder hinges; the body is not a flapping billboard.
[RequireComponent(typeof(CanvasRenderer))]
public sealed class GuxuCompanionMotion : MaskableGraphic {
 Texture2D bird;
 float phase,flyUntil;
 public bool Flying;
 public int MeshBuildCount {get;private set;}
 public int LastVertexCount {get;private set;}
 public GuxuCompanionMotion(){useLegacyMeshGeneration=false;}
 public override Texture mainTexture=>bird?bird:Texture2D.whiteTexture;
 protected override void Awake(){base.Awake();raycastTarget=false;bird=Resources.Load<Texture2D>("Game/Guxu/Final/Bird/amon");if(!bird)Debug.LogError("Missing final mechanical bird texture.");SetAllDirty();}
 public void SetFlying(bool value){Flying=value;}
 public void FlapFor(float seconds=1.5f){flyUntil=Time.unscaledTime+Mathf.Max(0,seconds);}
 void Update(){phase+=Time.unscaledDeltaTime;SetVerticesDirty();}
 static float S(float a,float b,float v)=>Mathf.SmoothStep(0,1,Mathf.InverseLerp(a,b,v));
 static Vector2 Rotate(Vector2 p,Vector2 pivot,float degrees){float r=degrees*Mathf.Deg2Rad;var d=p-pivot;return pivot+new Vector2(d.x*Mathf.Cos(r)-d.y*Mathf.Sin(r),d.x*Mathf.Sin(r)+d.y*Mathf.Cos(r));}
 protected override void OnPopulateMesh(VertexHelper vh){
  vh.Clear();LastVertexCount=0;if(!bird)return;MeshBuildCount++;
  var bounds=GetPixelAdjustedRect();float scale=Mathf.Min(bounds.width/1536f,bounds.height/1024f);
  Vector2 offset=bounds.center-new Vector2(768,512)*scale;
  bool fly=Flying||Time.unscaledTime<flyUntil;
  float wave=Mathf.Sin(phase*(fly?12f:2.7f)),angle=wave*(fly?18f:3.6f);
  const int cols=40,rows=28;
  for(int y=0;y<=rows;y++)for(int x=0;x<=cols;x++){
   float u=(float)x/cols,v=(float)y/rows;var p=new Vector2(u*1536,v*1024);
   // Coordinates are bottom-left. Tail feathers stay independent of the right wing.
   float wingY=S(.38f,.60f,v);
   float left=(1-S(.27f,.39f,u))*wingY;
   float right=S(.51f,.69f,u)*wingY;
   Vector2 delta=(Rotate(p,new Vector2(555,548),-angle)-p)*left+(Rotate(p,new Vector2(812,548),angle)-p)*right;
   float tail=S(.57f,.85f,u)*(1-S(.35f,.48f,v));
   delta.y+=Mathf.Sin(phase*2.5f+u*3)*7*tail;
   float lift=fly?Mathf.Sin(phase*3.4f)*12:0;
   var vertex=UIVertex.simpleVert;vertex.color=color;vertex.uv0=new Vector2(u,v);vertex.position=offset+(p+delta+Vector2.up*lift)*scale;vh.AddVert(vertex);
  }
  for(int y=0;y<rows;y++)for(int x=0;x<cols;x++){int a=y*(cols+1)+x,b=a+cols+1;vh.AddTriangle(a,b,a+1);vh.AddTriangle(a+1,b,b+1);}
  LastVertexCount=vh.currentVertCount;
 }
}
}
