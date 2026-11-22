using System;
using UnityEngine;
using UnityEngine.UI;

namespace Gxdl {
// Native renderers sit between a few scene canvases; the normal HUD/modal canvas stays above them.
public sealed class GuxuParticleFx:MonoBehaviour {
 public ParticleSystem System {get;private set;}
 public bool Persistent {get;private set;}
 public int PeakCount {get;private set;}
 public float Speed {get=>System.main.simulationSpeed;set{var m=System.main;m.simulationSpeed=value;}}
 float age,lifetime;
 static Material cloudMaterial,wispMaterial;
 public static void Sort(Transform node,int order){var c=node.GetComponent<Canvas>();if(!c)c=node.gameObject.AddComponent<Canvas>();c.overrideSorting=true;c.sortingOrder=order;}
 static Material Material(bool cloud){
  var known=cloud?cloudMaterial:wispMaterial;if(known)return known;
  var shader=Resources.Load<Shader>("Game/Guxu/Vfx/GuxuParticle");var texture=Resources.Load<Texture2D>("Game/Guxu/Vfx/particle-"+(cloud?"cloud":"wisp"));
  if(!shader||!texture)throw new InvalidOperationException("Missing native Guxu particle shader/texture.");
  var result=new Material(shader){name=cloud?"Guxu Cloud Particles":"Guxu Light Particles",mainTexture=texture};if(cloud)cloudMaterial=result;else wispMaterial=result;return result;
 }
 public static GuxuParticleFx Create(Transform parent,string label,bool cloud=false,int order=-10,bool persistent=false,float speed=1){
  var node=new GameObject(label);node.transform.SetParent(parent,false);var fx=node.AddComponent<GuxuParticleFx>();fx.Persistent=persistent;
  var ps=node.AddComponent<ParticleSystem>();fx.System=ps;ps.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
  var main=ps.main;main.playOnAwake=false;main.loop=true;main.simulationSpace=ParticleSystemSimulationSpace.Local;main.scalingMode=ParticleSystemScalingMode.Hierarchy;main.maxParticles=persistent?40:96;main.startSpeed=0;main.startLifetime=1;main.startSize3D=true;main.startSizeX=1;main.startSizeY=1;main.startSizeZ=1;main.simulationSpeed=speed;
  var emission=ps.emission;emission.enabled=false;var shape=ps.shape;shape.enabled=false;
  var color=ps.colorOverLifetime;color.enabled=true;var gradient=new Gradient();gradient.SetKeys(new[]{new GradientColorKey(Color.white,0),new GradientColorKey(Color.white,1)},new[]{new GradientAlphaKey(0,0),new GradientAlphaKey(1,.12f),new GradientAlphaKey(.75f,.48f),new GradientAlphaKey(0,1)});color.color=gradient;
  var size=ps.sizeOverLifetime;size.enabled=true;size.size=new ParticleSystem.MinMaxCurve(1,new AnimationCurve(new Keyframe(0,.65f),new Keyframe(.2f,1),new Keyframe(1,1.35f)));
  var renderer=ps.GetComponent<ParticleSystemRenderer>();renderer.sharedMaterial=Material(cloud);renderer.sortingOrder=order;renderer.renderMode=ParticleSystemRenderMode.Billboard;renderer.alignment=ParticleSystemRenderSpace.View;renderer.maxParticleSize=1;
  ps.Play();return fx;
 }
 public void Emit(Vector3 local,Vector3 velocity,Vector2 size,Color tint,float life,float rotation=0){
  var p=new ParticleSystem.EmitParams{position=local,velocity=velocity,startSize3D=new Vector3(size.x,size.y,1),startColor=tint,startLifetime=life,rotation=rotation};System.Emit(p,1);lifetime=Mathf.Max(lifetime,age+life+.08f);
 }
 public void EmitWorld(Vector3 world,Vector3 localVelocity,Vector2 size,Color tint,float life,float rotation=0)=>Emit(transform.InverseTransformPoint(world),localVelocity,size,tint,life,rotation);
 public static GuxuParticleFx Burst(Transform parent,Vector3 world,Color tint,int count=14,float strength=1,float speed=1,bool cloud=false){
  var fx=Create(parent,cloud?"Particle Impact Dust":"Particle Impact Sparks",cloud,-8,false,speed);fx.transform.position=world;
  for(int i=0;i<count;i++){float a=i*2.399963f;var direction=new Vector3(Mathf.Cos(a),Mathf.Sin(a),0);float v=(110+i*37%155)*strength;var size=cloud?new Vector2(125,64)*strength:new Vector2(13+i%3*5,38+i%4*9)*strength;fx.Emit(direction*(cloud?24:5),direction*v,size,tint,(cloud?.85f:.48f)+i%4*.075f,a*Mathf.Rad2Deg);}
  return fx;
 }
 public static GuxuParticleFx Impact(Transform parent,Vector3 world,Color tint,int count,float strength=1,float speed=1){
  var fx=Burst(parent,world,tint,count,strength,speed);
  // A moving ring and a separate translucent dust layer give the contact a visible wake.
  for(int i=0;i<20;i++){float a=i*Mathf.PI*2/20;var d=new Vector3(Mathf.Cos(a),Mathf.Sin(a)*.65f,0);fx.Emit(d*25*strength,d*240*strength,new Vector2(24,65)*strength,tint,.65f,a*Mathf.Rad2Deg+90);}
  var dust=Create(fx.transform,"Particle Impact Cloud Wake",true,-9,false,speed);
  for(int i=0;i<8;i++){float a=i*Mathf.PI/4;var d=new Vector3(Mathf.Cos(a),Mathf.Sin(a)*.5f,0);dust.Emit(d*18,d*90*strength,new Vector2(150,85)*strength,new Color(tint.r,tint.g,tint.b,.24f),.95f,a*Mathf.Rad2Deg);}
  fx.lifetime=Mathf.Max(fx.lifetime,1.15f);return fx;
 }
 public static Vector3 Body(Image image){var rect=image.rectTransform;return rect.TransformPoint(new Vector3(rect.rect.center.x,rect.rect.yMin+rect.rect.height*.62f,0));}
 void Update(){if(!System)return;PeakCount=Mathf.Max(PeakCount,System.particleCount);age+=Time.deltaTime*Speed;if(!Persistent&&age>lifetime&&System.particleCount==0)Destroy(gameObject);}
 void OnDisable(){if(System)System.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);}
}

// Local simulation moves every live puff with the actual skeleton, including its internal dash.
[DefaultExecutionOrder(100)]
public sealed class GuxuFootCloud:MonoBehaviour {
 GuxuCharacterMotion actor;Image body;GuxuParticleFx back,front;float next;
 public Vector3 AnchorWorld {get;private set;}
 public static GuxuFootCloud Attach(Image image){var cloud=image.gameObject.AddComponent<GuxuFootCloud>();cloud.body=image;cloud.actor=image.GetComponent<GuxuCharacterMotion>();cloud.back=GuxuParticleFx.Create(image.transform.parent,"Particle Foot Cloud Rear",true,-30,true);cloud.front=GuxuParticleFx.Create(image.transform.parent,"Particle Foot Cloud Front",true,-10,true);cloud.LateUpdate();for(int i=0;i<5;i++)cloud.Puff(i*.7f);return cloud;}
 void LateUpdate(){
  if(!body||!back||!front)return;AnchorWorld=actor?actor.FeetWorld:Foot(body);
  back.transform.position=front.transform.position=AnchorWorld;
  if(Time.time>=next){Puff(Time.time);next=Time.time+.42f;}
 }
 void Puff(float phase){float width=actor?(actor.UseHomePose?300:235):Mathf.Clamp(body.rectTransform.rect.width*.67f,210,340);float height=actor&&actor.UseHomePose?205:190;float x=Mathf.Sin(phase*1.25f)*width*.22f;
  float s=Mathf.Abs(body.rectTransform.localScale.x);width*=s;height*=s;
  back.Emit(new Vector3(x,height*.60f,0),new Vector3(15,3,0),new Vector2(width*1.15f,height*1.14f),new Color(.60f,.84f,.93f,.34f),2.6f,Mathf.Sin(phase)*6);
  front.Emit(new Vector3(-x,height*.36f,0),new Vector3(-18,2,0),new Vector2(width,height),new Color(.84f,.96f,1,.32f),2.25f,Mathf.Sin(phase)*4);
  front.Emit(new Vector3(x*.6f,height*.03f,0),new Vector3(12,2,0),new Vector2(width*.95f,height*.70f),new Color(.79f,.93f,1,.32f),2.3f,-Mathf.Sin(phase)*5);
  front.Emit(new Vector3(x*.9f,height*.69f,0),new Vector3(-10,5,0),new Vector2(width*.68f,height*.84f),new Color(.82f,.95f,1,.22f),2.1f,Mathf.Cos(phase)*7);
 }
 public static Vector3 Foot(Image image){
  var r=image.rectTransform;var sprite=image.sprite;if(!sprite)return r.TransformPoint(new Vector3(r.rect.center.x,r.rect.yMin,0));
  var anchor=GuxuVfxAnchors.ForSprite(sprite);float scale=Mathf.Min(r.rect.width/sprite.rect.width,r.rect.height/sprite.rect.height);var size=sprite.rect.size*scale;
  return r.TransformPoint(r.rect.center+new Vector2((anchor.x-.5f)*size.x,(anchor.y-.5f)*size.y));
 }
 void OnDisable(){if(back){back.gameObject.SetActive(false);Destroy(back.gameObject);}if(front){front.gameObject.SetActive(false);Destroy(front.gameObject);}}
}
}
