using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Gxdl {
// Only presentation: the three contacts divide one precomputed battle event, never roll damage.
public sealed class GuxuCombatVfx : MonoBehaviour {
 public bool IsPlaying {get;private set;}
 public string Phase {get;private set;}="idle";
 public int ImpactCount {get;private set;}
 public int ActiveVisualCount=>nodes.Count+tails.FindAll(p=>p).Count;
 readonly List<GameObject> nodes=new List<GameObject>();
 readonly List<GuxuParticleFx> tails=new List<GuxuParticleFx>();
 RectTransform root;
 int generation;
 static readonly Color Cyan=new Color(.12f,.88f,1,.86f);
 public static int DamagePart(int total,int index){if(total<0||index<0||index>2)throw new ArgumentOutOfRangeException();int first=total/3;return index<2?first:total-2*first;}
 public void Bind(RectTransform parent){Cancel();root=parent?parent:throw new ArgumentNullException(nameof(parent));}
 public void Cancel(){generation++;StopAllCoroutines();foreach(var n in nodes)if(n){n.SetActive(false);Destroy(n);}foreach(var p in tails)if(p){p.gameObject.SetActive(false);Destroy(p.gameObject);}tails.Clear();nodes.Clear();IsPlaying=false;Phase="idle";}
 void OnDisable(){Cancel();}
 Vector2 Local(Vector3 world)=>root.InverseTransformPoint(world);
 Vector2 Target(Image target)=>Local(target.rectTransform.TransformPoint(new Vector3(target.rectTransform.rect.center.x,target.rectTransform.rect.yMin+target.rectTransform.rect.height*.68f,0)));
 RectTransform Node(string name,Vector2 size){var g=new GameObject(name,typeof(RectTransform),typeof(CanvasRenderer));nodes.Add(g);var r=(RectTransform)g.transform;r.SetParent(root,false);r.anchorMin=r.anchorMax=root.pivot;r.pivot=new Vector2(.5f,.5f);r.sizeDelta=size;GuxuParticleFx.Sort(r,-6);return r;}
 GuxuCombatStroke Stroke(string name){var r=Node(name,new Vector2(500,500));var g=r.gameObject.AddComponent<GuxuCombatStroke>();g.raycastTarget=false;g.color=Cyan;return g;}
 GuxuSoftVfx Blob(string name,bool smoke){var r=Node(name,new Vector2(70,70));var g=r.gameObject.AddComponent<GuxuSoftVfx>();g.Smoke=smoke;g.raycastTarget=false;g.color=Color.clear;return g;}
 public IEnumerator PlayCombo(GuxuCharacterMotion actor,Image target,float speed,Action<int> onImpact=null,Color? tint=null){
  if(IsPlaying)yield break;if(!root||!actor||!target)throw new InvalidOperationException("Bind and live actor/target required.");
  speed=Mathf.Clamp(speed,.1f,5);IsPlaying=true;ImpactCount=0;int ticket=++generation;bool completed=false;tails.RemoveAll(p=>!p);
  var slash=Stroke("Combo 1 Arc Slash");var ring=Stroke("Combo 2 Horizontal Ring");var wave=Stroke("Combo 3 Sword Energy");wave.Projectile=true;
  Color energy=tint??Cyan;var trail=GuxuParticleFx.Create(root,"Particle Combo Charge and Trail",false,-8,true,speed);nodes.Add(trail.gameObject);
  var trailMain=trail.System.main;trailMain.maxParticles=96;
  float nextParticle=0;Vector2 contact=Target(target),launch=Vector2.zero;bool launched=false;
  try{for(float t=0;t<GuxuCharacterMotion.ComboDuration;t+=Time.deltaTime*speed){
   if(!this||!isActiveAndEnabled||ticket!=generation||!actor||!actor.isActiveAndEnabled||!target||!target.gameObject.activeInHierarchy||!root)yield break;
   Vector2 weapon=Local(actor.WeaponWorld);Vector2 palm=Local(actor.HandWorld);contact=Target(target);
   float first=Mathf.Clamp01((t-.34f)/.40f),second=Mathf.Clamp01((t-1.27f)/.55f);
   slash.rectTransform.anchoredPosition=weapon;slash.SetArc(new Vector2(175,190),-100+first*130,170,Mathf.Sin(first*Mathf.PI)*15);slash.color=new Color(energy.r,energy.g,energy.b,Mathf.Sin(first*Mathf.PI)*.85f);
   ring.rectTransform.anchoredPosition=palm;ring.SetArc(new Vector2(235,78),-210+second*230,295,Mathf.Sin(second*Mathf.PI)*14);ring.color=new Color(energy.r,energy.g,energy.b,Mathf.Sin(second*Mathf.PI)*.85f);
   if(t>=2.43f&&!launched){launch=weapon;launched=true;}
   float flight=Mathf.Clamp01((t-2.43f)/(GuxuCharacterMotion.ComboImpact3-2.43f));
   wave.rectTransform.anchoredPosition=Vector2.Lerp(launch,contact,flight);Vector2 direction=contact-launch;wave.rectTransform.localEulerAngles=new Vector3(0,0,Mathf.Atan2(direction.y,direction.x)*Mathf.Rad2Deg);
   wave.color=new Color(energy.r,energy.g,energy.b,t>=2.43f&&t<GuxuCharacterMotion.ComboImpact3?1:0);wave.SetArc(new Vector2(175,65),0,0,0);
   if(t>=nextParticle&&t<GuxuCharacterMotion.ComboImpact3){Vector3 source=t>=2.43f?wave.transform.position:actor.WeaponWorld;trail.EmitWorld(source,Vector3.up*15,new Vector2(t>=2.2f?42:26,t>=2.2f?80:50),energy,.45f,t*180);
    if((t>=.34f&&t<.8f)||(t>=1.27f&&t<1.85f)){float a=(t<1?-100+first*300:-210+second*360)*Mathf.Deg2Rad;Vector2 arc=new Vector2(Mathf.Cos(a)*(t<1?175:235),Mathf.Sin(a)*(t<1?190:78));for(int k=0;k<2;k++)trail.Emit(weapon+arc*(1-k*.15f),new Vector3(-Mathf.Sin(a),Mathf.Cos(a),0)*65,new Vector2(26,75),energy,.48f,a*Mathf.Rad2Deg);}
    nextParticle=t+.025f;}
   Phase=t<.34f?"windup":t<.85f?"arc_slash":t<1.27f?"turn":t<1.9f?"horizontal_slash":t<2.43f?"energy_charge":t<2.67f?"projectile":"recovery";
   float threshold=ImpactCount==0?GuxuCharacterMotion.ComboImpact1:ImpactCount==1?GuxuCharacterMotion.ComboImpact2:GuxuCharacterMotion.ComboImpact3;
   // A slow frame may cross more than one marker: still emit each once in order.
   while(ImpactCount<3&&t>=threshold){var impact=GuxuParticleFx.Impact(root,root.TransformPoint(contact),energy,ImpactCount==2?32:22,ImpactCount==2?1.65f:1.2f,speed);nodes.Add(impact.gameObject);onImpact?.Invoke(ImpactCount);ImpactCount++;threshold=ImpactCount==1?GuxuCharacterMotion.ComboImpact2:GuxuCharacterMotion.ComboImpact3;}
   yield return null;
  }
  if(ticket==generation&&target)while(ImpactCount<3){onImpact?.Invoke(ImpactCount);ImpactCount++;}
  completed=true;
  }finally{if(this&&ticket==generation){foreach(var n in nodes)if(n){var particles=n.GetComponent<GuxuParticleFx>();if(completed&&particles&&!particles.Persistent)tails.Add(particles);else{n.SetActive(false);Destroy(n);}}nodes.Clear();IsPlaying=false;Phase="idle";}}
 }
 public static void ValidateContract(){for(int total=0;total<1000;total++)if(DamagePart(total,0)+DamagePart(total,1)+DamagePart(total,2)!=total)throw new InvalidOperationException("Combo multiplied event damage.");if(!(GuxuCharacterMotion.ComboImpact1<GuxuCharacterMotion.ComboImpact2&&GuxuCharacterMotion.ComboImpact2<GuxuCharacterMotion.ComboImpact3&&GuxuCharacterMotion.ComboImpact3<GuxuCharacterMotion.ComboDuration))throw new InvalidOperationException("Combo timing order invalid.");}
}

[RequireComponent(typeof(CanvasRenderer))]
public sealed class GuxuCombatStroke : MaskableGraphic {
 public bool Projectile;
 Vector2 radius;
 float start,sweep,width;
 public int MeshBuildCount {get;private set;}
 public GuxuCombatStroke(){useLegacyMeshGeneration=false;}
 public void SetArc(Vector2 radii,float startDegrees,float sweepDegrees,float thickness){radius=radii;start=startDegrees;sweep=sweepDegrees;width=thickness;SetVerticesDirty();}
 protected override void OnPopulateMesh(VertexHelper vh){MeshBuildCount++;vh.Clear();if(Projectile){
   Ribbon(vh,new Vector2(-radius.x,0),new Vector2(radius.x*.55f,0),radius.y*.32f,color);
   for(int k=-1;k<=1;k+=2)for(int i=0;i<22;i++){float p=i/22f,q=(i+1)/22f;Vector2 a=new Vector2(-radius.x+p*radius.x*1.5f,k*Mathf.Sin(p*Mathf.PI)*radius.y),b=new Vector2(-radius.x+q*radius.x*1.5f,k*Mathf.Sin(q*Mathf.PI)*radius.y);Ribbon(vh,a,b,(1-p)*8,color);}
   return;
  }
  if(width<=.01f)return;for(int i=0;i<48;i++){float p=i/48f,q=(i+1)/48f,a=(start+sweep*p)*Mathf.Deg2Rad,b=(start+sweep*q)*Mathf.Deg2Rad;Vector2 v=new Vector2(Mathf.Cos(a)*radius.x,Mathf.Sin(a)*radius.y),w=new Vector2(Mathf.Cos(b)*radius.x,Mathf.Sin(b)*radius.y);Ribbon(vh,v,w,width*Mathf.Sin((p+.02f)*Mathf.PI),color);}
 }
 static void Ribbon(VertexHelper vh,Vector2 a,Vector2 b,float thickness,Color tint){Vector2 n=new Vector2(-(b-a).y,(b-a).x).normalized*thickness;int index=vh.currentVertCount;Color edge=tint;edge.a=0;Color core=Color.Lerp(tint,Color.white,.65f);core.a=tint.a;
  vh.AddVert(a-n,edge,Vector2.zero);vh.AddVert(a,core,Vector2.zero);vh.AddVert(a+n,edge,Vector2.zero);vh.AddVert(b-n,edge,Vector2.zero);vh.AddVert(b,core,Vector2.zero);vh.AddVert(b+n,edge,Vector2.zero);
  vh.AddTriangle(index,index+1,index+4);vh.AddTriangle(index,index+4,index+3);vh.AddTriangle(index+1,index+2,index+5);vh.AddTriangle(index+1,index+5,index+4);
 }
}
}
