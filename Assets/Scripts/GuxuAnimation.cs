using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Gxdl {
// Presentation only: no inventory, reward or save mutations belong here.
public sealed partial class GuxuAnimation : MonoBehaviour {
 public bool IsPlaying { get; private set; }
 public string Phase { get; private set; } = "idle";
 public int TemporaryCount => temporary.Count;
 public const float CommonDuration = 2.45f;
 public const float RareDuration = 3.01f;
 public static readonly Color[] QualityColors = {Color.white,new Color(.40f,.86f,.45f),new Color(.28f,.65f,1),new Color(.75f,.40f,1),new Color(1,.65f,.19f)};
 Image character,stone;
 GuxuCharacterMotion characterMotion;
 RectTransform layer;
 Vector2 characterOrigin,stoneOrigin;
 Vector3 characterScale,stoneScale;
 Quaternion characterRotation,stoneRotation;
 Color characterColor,stoneColor;
 int generation;
 bool skip;
 bool stoneVisibleAtRest;
 public float ReleaseAt {get;private set;}=-1;
 public float RuneContactAt {get;private set;}=-1;
 public float FractureAt {get;private set;}=-1;
 public bool StoneVisible=>stone&&stone.enabled&&stone.color.a>0;
 // The hand is drawn up to the chest first, then punched forward from there: the palm settles in front
 // of the ribcage at 0.34s and leaves the fingers level at 0.44s.
 public const float CastRelease=.44f;
 public const float CastFlight=.30f;
 public static float FullRelease=>CastRelease;
 public static float FullImpact=>CastRelease+CastFlight;
 readonly List<GameObject> temporary = new List<GameObject>();

 public void Bind(Image characterImage,Image stoneImage,RectTransform effectsParent){
  Cancel();character=characterImage;stone=stoneImage;layer=effectsParent;
  if(!character||!stone||!layer)throw new ArgumentException("Animation requires separate character, stone and effect layers.");
  characterOrigin=character.rectTransform.anchoredPosition;stoneOrigin=stone.rectTransform.anchoredPosition;
  characterScale=character.rectTransform.localScale;stoneScale=stone.rectTransform.localScale;
  characterRotation=character.rectTransform.localRotation;stoneRotation=stone.rectTransform.localRotation;
  characterColor=character.color;stoneColor=stone.color;
  stoneVisibleAtRest=stone.enabled;
  characterMotion=character.GetComponent<GuxuCharacterMotion>();if(!characterMotion)characterMotion=character.gameObject.AddComponent<GuxuCharacterMotion>();
  characterMotion.UseHomePose=true;
 }
 void Update(){
  if(IsPlaying||!character||!stone)return;
  // The monolith is grounded; movement is local to cloth/hair rather than scaling the whole figure.
  stone.rectTransform.anchoredPosition=stoneOrigin;
 }
 public void SkipReveal(){skip=true;}
 public void Cancel(){generation++;StopAllCoroutines();Cleanup();Restore();IsPlaying=false;Phase="idle";skip=false;}
 void OnDisable(){Cancel();}
 void Restore(){
  if(characterMotion)characterMotion.ResetPose();
  if(character){character.rectTransform.anchoredPosition=characterOrigin;character.rectTransform.localScale=characterScale;character.rectTransform.localRotation=characterRotation;character.color=characterColor;}
  if(stone){stone.rectTransform.anchoredPosition=stoneOrigin;stone.rectTransform.localScale=stoneScale;stone.rectTransform.localRotation=stoneRotation;stone.color=stoneColor;stone.enabled=stoneVisibleAtRest;}
 }
 void BeginStonePresentation(){stoneVisibleAtRest=stone.enabled;Restore();stone.enabled=true;ReleaseAt=RuneContactAt=FractureAt=-1;}
 void Cleanup(){foreach(var go in temporary)if(go){go.SetActive(false);Destroy(go);}temporary.Clear();}
 bool Valid(int ticket)=>this&&isActiveAndEnabled&&generation==ticket&&character&&stone&&layer;

 static Vector2 FragmentTravel(Vector2 direction,int index,float t){
  // Exponential drag makes the impulse quick; gravity keeps the heavy chunks falling after it.
  float distance=(65+index*23%55)*(1-Mathf.Exp(-3.8f*t));
  return new Vector2(direction.x*distance*.55f,direction.y*distance*.50f+65*t-195*t*t);
 }
 static Vector2 Curve(Vector2 a,Vector2 b,Vector2 c,float t){float q=1-t;return q*q*a+2*q*t*b+t*t*c;}
 static void SealPose(Image sword,Vector2 a,Vector2 b,Vector2 c,float t){
  var r=sword.rectTransform;r.anchoredPosition=Curve(a,b,c,t);
  r.localEulerAngles=new Vector3(0,0,Mathf.Lerp(-12,12,t));
 }
 Vector2 CharacterHand(){return layer.InverseTransformPoint(characterMotion.HandWorld);}
 Vector2 Center(RectTransform rect){return layer.InverseTransformPoint(rect.TransformPoint(rect.rect.center));}
 RectTransform Node(string name,Vector2 position,Vector2 size){
  var go=new GameObject(name,typeof(RectTransform),typeof(CanvasRenderer));temporary.Add(go);var r=(RectTransform)go.transform;r.SetParent(layer,false);r.anchorMin=r.anchorMax=layer.pivot;r.pivot=new Vector2(.5f,.5f);r.anchoredPosition=position;r.sizeDelta=size;GuxuParticleFx.Sort(r,-5);return r;
 }
 Image SpriteImage(string name,Sprite sprite,Vector2 pos,Vector2 size){var r=Node(name,pos,size);var im=r.gameObject.AddComponent<Image>();im.sprite=sprite;im.preserveAspect=true;im.raycastTarget=false;return im;}
 GuxuRuneRing Ring(string name,Vector2 pos,Vector2 size,Color color){var r=Node(name,pos,size);var ring=r.gameObject.AddComponent<GuxuRuneRing>();ring.color=color;ring.raycastTarget=false;return ring;}
 GuxuSoftVfx Soft(string name,Vector2 pos,Vector2 size,Color color,bool smoke=false,float seed=0){var r=Node(name,pos,size);var effect=r.gameObject.AddComponent<GuxuSoftVfx>();effect.Smoke=smoke;effect.Seed=seed;effect.color=color;effect.raycastTarget=false;return effect;}
 GuxuStoneShard[] SplitStone(){
  var sprite=stone.sprite;if(!sprite)throw new InvalidOperationException("Stone sprite is required for fracture.");
  Vector2 size=stone.rectTransform.rect.size;float fit=Mathf.Min(size.x/sprite.rect.width,size.y/sprite.rect.height);size=new Vector2(sprite.rect.width,sprite.rect.height)*fit;
  Vector2 center=Center(stone.rectTransform);var triangles=FractureTriangles();var pieces=new GuxuStoneShard[triangles.Length];
  for(int i=0;i<pieces.Length;i++){var triangle=triangles[i];Vector2 centroid=Vector2.zero;foreach(var point in triangle)centroid+=point;centroid/=triangle.Length;
   var r=Node("Stone Fragment "+i,center+Vector2.Scale(centroid-new Vector2(.5f,.5f),size),size);
   var shard=r.gameObject.AddComponent<GuxuStoneShard>();shard.raycastTarget=false;shard.Configure(sprite,triangle,centroid,size);pieces[i]=shard;
  }return pieces;
 }
 static Vector2[][] FractureTriangles(){
  // A clipped Voronoi partition gives solid irregular chunks, not a triangular grid of paper shards.
  Vector2[] seeds={new Vector2(.12f,.12f),new Vector2(.40f,.08f),new Vector2(.76f,.10f),new Vector2(.94f,.25f),
   new Vector2(.08f,.40f),new Vector2(.29f,.34f),new Vector2(.52f,.27f),new Vector2(.72f,.40f),
   new Vector2(.18f,.61f),new Vector2(.43f,.56f),new Vector2(.58f,.45f),new Vector2(.93f,.58f),
   new Vector2(.06f,.84f),new Vector2(.34f,.83f),new Vector2(.58f,.74f),new Vector2(.76f,.91f),new Vector2(.96f,.87f),new Vector2(.55f,.98f)};
  var result=new List<Vector2[]>();for(int i=0;i<seeds.Length;i++){
   var polygon=new List<Vector2>{Vector2.zero,Vector2.right,Vector2.one,Vector2.up};
   for(int j=0;j<seeds.Length;j++){if(i==j)continue;var next=new List<Vector2>();Vector2 normal=seeds[j]-seeds[i],mid=(seeds[j]+seeds[i])*.5f;
    for(int k=0;k<polygon.Count;k++){Vector2 a=polygon[k],b=polygon[(k+1)%polygon.Count];float da=Vector2.Dot(a-mid,normal),db=Vector2.Dot(b-mid,normal);
     if(da<=0)next.Add(a);if((da<=0)!=(db<=0))next.Add(Vector2.LerpUnclamped(a,b,da/(da-db)));
    }polygon=next;
   }result.Add(polygon.ToArray());
  }return result.ToArray();
 }
 public static void ValidateContract(){
  GuxuCharacterMotion.ValidateContract();
  if(QualityColors.Length!=5||RareDuration<=CommonDuration)throw new InvalidOperationException("Quality/timing contract invalid.");
  if(Curve(Vector2.zero,Vector2.one,new Vector2(2,0),0)!=Vector2.zero||Curve(Vector2.zero,Vector2.one,new Vector2(2,0),1)!=new Vector2(2,0))throw new InvalidOperationException("Sword path endpoints invalid.");
  float area=0;foreach(var poly in FractureTriangles()){if(poly.Length<3)throw new InvalidOperationException("Empty stone shard.");for(int i=1;i<poly.Length-1;i++){var a=poly[i]-poly[0];var b=poly[i+1]-poly[0];float cross=a.x*b.y-a.y*b.x;if(cross<=0)throw new InvalidOperationException("Inverted stone shard.");area+=cross*.5f;}}if(Mathf.Abs(area-1)>.0001f)throw new InvalidOperationException("Stone fracture coverage invalid.");
  if(FragmentTravel(Vector2.right,0,0)!=Vector2.zero||FragmentTravel(Vector2.right,0,.74f).y>=0||FragmentTravel(Vector2.right,0,.74f).x>=200)throw new InvalidOperationException("Stone impulse/gravity bounds invalid.");
  var end=SpiritStoneMotion.Sample(2,90,60,45);if((end-new Vector2(90,-45)).sqrMagnitude>.001f)throw new InvalidOperationException("Dust landing regression.");
 }
}

// Textured front face and a dark extruded rim preserve the stone silhouette and add thickness.
[RequireComponent(typeof(CanvasRenderer))]
public sealed class GuxuStoneShard : MaskableGraphic {
 public int MeshBuildCount { get; private set; }
 public float Radius { get; private set; }
 public GuxuStoneShard(){useLegacyMeshGeneration=false;}
 Texture source;
 Vector2[] positions,uv;
 public override Texture mainTexture => source?source:Texture2D.whiteTexture;
 public void Configure(Sprite sprite,Vector2[] triangle,Vector2 centroid,Vector2 displaySize){
  source=sprite.texture;positions=new Vector2[triangle.Length];uv=new Vector2[triangle.Length];for(int i=0;i<triangle.Length;i++){
   positions[i]=Vector2.Scale(triangle[i]-centroid,displaySize);Radius=Mathf.Max(Radius,positions[i].magnitude+9);
   uv[i]=new Vector2((sprite.rect.x+triangle[i].x*sprite.rect.width)/sprite.texture.width,(sprite.rect.y+triangle[i].y*sprite.rect.height)/sprite.texture.height);
  }SetVerticesDirty();SetMaterialDirty();
 }
 protected override void OnPopulateMesh(VertexHelper vh){
  MeshBuildCount++;vh.Clear();if(positions==null)return;int count=positions.Length;
  Color dark=new Color(color.r*.30f,color.g*.34f,color.b*.38f,color.a);
  for(int i=0;i<count;i++)vh.AddVert(positions[i]+new Vector2(5,-7),dark,uv[i]);
  for(int i=1;i<count-1;i++)vh.AddTriangle(0,i,i+1);
  for(int i=0;i<count;i++)vh.AddVert(positions[i],color,uv[i]);
  for(int i=0;i<count;i++){int j=(i+1)%count;vh.AddTriangle(i,j,count+j);vh.AddTriangle(i,count+j,count+i);}
  for(int i=1;i<count-1;i++)vh.AddTriangle(count,count+i,count+i+1);
 }
}

// A lightweight code-drawn magic seal; no imported shader or particle package required.
[RequireComponent(typeof(CanvasRenderer))]
public sealed class GuxuRuneRing : MaskableGraphic {
 public int MeshBuildCount { get; private set; }
 public GuxuRuneRing(){useLegacyMeshGeneration=false;}
 protected override void OnPopulateMesh(VertexHelper vh){MeshBuildCount++;vh.Clear();var rect=rectTransform.rect;Vector2 center=rect.center;float radius=Mathf.Min(rect.width,rect.height)*.5f;
  for(int i=0;i<64;i++){float a=i*Mathf.PI*2/64,b=(i+1)*Mathf.PI*2/64;float outer=radius,inner=radius*.97f;
   Quad(vh,center+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*inner,center+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*outer,center+new Vector2(Mathf.Cos(b),Mathf.Sin(b))*outer,center+new Vector2(Mathf.Cos(b),Mathf.Sin(b))*inner);
   if(i%8==0){float m=(a+b)*.5f;Vector2 outward=new Vector2(Mathf.Cos(m),Mathf.Sin(m)),side=new Vector2(-outward.y,outward.x);Vector2 point=center+outward*radius*.86f;Quad(vh,point-outward*radius*.09f,point+side*radius*.035f,point+outward*radius*.09f,point-side*radius*.035f);}
  }
 }
 void Quad(VertexHelper vh,Vector2 a,Vector2 b,Vector2 c,Vector2 d){int n=vh.currentVertCount;vh.AddVert(a,color,Vector2.zero);vh.AddVert(b,color,Vector2.zero);vh.AddVert(c,color,Vector2.zero);vh.AddVert(d,color,Vector2.zero);vh.AddTriangle(n,n+1,n+2);vh.AddTriangle(n,n+2,n+3);}
}
}


