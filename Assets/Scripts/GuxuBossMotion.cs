using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Gxdl {
[RequireComponent(typeof(Image))]
public sealed class GuxuBossMotion:MonoBehaviour {
 public GuxuBossDefinition Definition {get;private set;}
 public string State {get;private set;}="idle";
 public int FrameIndex {get;private set;}
 public int ImpactCount {get;private set;}
 public int SpriteChanges {get;private set;}
 public string CurrentAttack {get;private set;}="";
 public bool IsAttacking=>State=="attack";
 Image body,blend;RectTransform rect;Vector2 origin;Vector3 scale;float hitUntil,hitDuration;int generation,attackCursor;
 GuxuBossVfx effect;
 GuxuParticleFx particles;
 public bool Bind(string key){
  Definition=GuxuBossAnimations.Find(key);if(Definition==null)return false;
  body=GetComponent<Image>();rect=body.rectTransform;origin=rect.anchoredPosition;scale=rect.localScale;body.color=Color.white;body.preserveAspect=true;
  var node=new GameObject("Boss Pose Blend",typeof(RectTransform),typeof(CanvasRenderer),typeof(Image));node.transform.SetParent(transform,false);
  blend=node.GetComponent<Image>();blend.raycastTarget=false;blend.preserveAspect=true;blend.rectTransform.anchorMin=Vector2.zero;blend.rectTransform.anchorMax=Vector2.one;blend.rectTransform.offsetMin=blend.rectTransform.offsetMax=Vector2.zero;blend.color=Color.clear;
  SetFrame(Definition.idleFrame);return true;
 }
 public int NextAttack(bool heavy){if(Definition.attacks.Length==1)return 0;return heavy?Definition.attacks.Length-1:attackCursor++%(Definition.attacks.Length-1);}
 void SetFrame(int index){if(body.sprite!=Definition.Frame(index))SpriteChanges++;FrameIndex=index;body.sprite=Definition.Frame(index);}
 void LateUpdate(){
  if(Definition==null||IsAttacking||State=="defeated")return;
  float hit=hitDuration>0?Mathf.Clamp01((hitUntil-Time.time)/hitDuration):0;
  State=hit>0?"hit":"idle";SetFrame(Definition.idleFrame);blend.color=Color.clear;
  rect.anchoredPosition=origin+Vector2.right*hit*15;
  rect.localScale=Vector3.Scale(scale,new Vector3(1+Mathf.Sin(Time.time*2.1f)*.006f,1+Mathf.Sin(Time.time*2.1f)*.01f,1));
  body.color=Color.Lerp(Color.white,new Color(1,.6f,.6f),hit*.55f);
 }
 public void Hit(float speed=1){if(State=="defeated")return;hitDuration=.2f/Mathf.Max(.1f,speed);hitUntil=Time.time+hitDuration;}
 public void Defeat(){Cancel();State="defeated";}
 public void Cancel(){
  generation++;if(effect){effect.gameObject.SetActive(false);Destroy(effect.gameObject);effect=null;}
  if(particles){particles.gameObject.SetActive(false);Destroy(particles.gameObject);particles=null;}
  if(rect){rect.anchoredPosition=origin;rect.localScale=scale;}if(blend)blend.color=Color.clear;if(body)body.color=Color.white;
  State="idle";hitUntil=0;CurrentAttack="";
 }
 void OnDisable(){Cancel();}
 public IEnumerator PlayAttack(int index,Image target,RectTransform canvas,float speed,Action<int> onImpact=null){
  if(Definition==null||!target||!canvas)yield break;Cancel();int ticket=generation;
  speed=Mathf.Clamp(speed,.1f,5);var attack=Definition.attacks[Mathf.Clamp(index,0,Definition.attacks.Length-1)];
  State="attack";CurrentAttack=attack.name;ImpactCount=0;
  var node=new GameObject("Boss VFX "+Definition.key,typeof(RectTransform),typeof(CanvasRenderer));node.transform.SetParent(canvas,false);
  var r=(RectTransform)node.transform;r.anchorMin=r.anchorMax=canvas.pivot;r.pivot=Vector2.one*.5f;r.sizeDelta=canvas.rect.size;
  effect=node.AddComponent<GuxuBossVfx>();effect.raycastTarget=false;effect.Bind(Definition,attack);
  GuxuParticleFx.Sort(node.transform,-6);
  particles=GuxuParticleFx.Create(canvas,"Particle Boss "+attack.effect,false,-8,true,speed);float nextParticle=0;
  Vector2 start=canvas.InverseTransformPoint(rect.TransformPoint(rect.rect.center));
  Vector2 destination=canvas.InverseTransformPoint(target.rectTransform.TransformPoint(target.rectTransform.rect.center));
  try{
   for(float t=0;;t=Mathf.Min(attack.duration,t+Time.deltaTime*speed)){
    if(!this||!isActiveAndEnabled||ticket!=generation||!target||!target.gameObject.activeInHierarchy||!canvas)yield break;
    int k=attack.FrameAt(t);SetFrame(attack.frames[k]);
    // Generated poses are held on their timing keys; short blends soften transitions without inventing damage events.
    float mix=k>0?Mathf.Clamp01((t-attack.times[k])/.045f):1;
    body.color=new Color(1,1,1,mix);blend.sprite=Definition.Frame(k>0?attack.frames[k-1]:attack.frames[k]);blend.color=new Color(1,1,1,1-mix);
    float p=t/attack.duration,travel=attack.travel*Mathf.SmoothStep(0,1,p/.4f)*(1-Mathf.SmoothStep(0,1,(p-.73f)/.27f));
    rect.anchoredPosition=origin+Vector2.left*travel;rect.localScale=scale;
    effect.Sample(t,start+Vector2.left*travel,destination);
    if(t>=nextParticle&&t<attack.impacts[0]){float flight=Mathf.Clamp01(t/Mathf.Max(.01f,attack.impacts[0]));var point=Vector2.Lerp(start+Vector2.left*travel,destination,flight);for(int side=-1;side<=1;side++){var offset=Vector2.up*(side*Mathf.Sin(flight*Mathf.PI)*65);particles.EmitWorld(canvas.TransformPoint(point+offset),Vector3.up*(attack.effect=="poison"?35:side*20),new Vector2(34,75),Definition.Color,.5f,t*240);}nextParticle=t+.035f;}
    while(ImpactCount<attack.impacts.Length&&t>=attack.impacts[ImpactCount]){int contact=ImpactCount++;GuxuParticleFx.Impact(canvas,GuxuParticleFx.Body(target),Definition.Color,index==Definition.attacks.Length-1?38:24,index==Definition.attacks.Length-1?1.9f:1.35f,speed);onImpact?.Invoke(contact);}
    if(t>=attack.duration)break;yield return null;
   }
  }finally{if(this&&ticket==generation){Cancel();SetFrame(Definition.idleFrame);}}
 }
}
}
