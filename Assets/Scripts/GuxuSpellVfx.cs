using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Gxdl {
// Independent visual clock: gameplay/rewards are deliberately not driven by these effects.
public sealed class GuxuSpellVfx : MonoBehaviour {
 readonly List<GuxuSpellLayer> layers = new List<GuxuSpellLayer>();
 static readonly float[] Lifetimes={.4f,.53f,.67f,.8f,.4f,.53f,.4f,.53f};
 static readonly float[] Starts={0,.2f,.4f,.9f,.2f,.4f,.9f,.9f};
 static readonly float[] Ends={.9f,1.4f,1.2f,1.4f,1.2f,1.3f,1.4f,1.4f};
 RectTransform root; GuxuCharacterMotion actor; Vector3 target;
 float speed=1; bool looping,composite;
 public bool IsPlaying {get;private set;}
 public float Elapsed {get;private set;}
 public float Duration {get;private set;}
 public int ActiveVisualCount {get {return layers.Count;}}
 public int CurrentVertexCount {get {int n=0;foreach(var l in layers) if(l&&l.gameObject.activeSelf)n+=l.VertexCount;return n;}}
 public void Bind(RectTransform parent){Cancel();root=parent;}
 public void PlayEffect(string key,GuxuCharacterMotion motion,Vector3 targetWorld,float speed=1f,bool loop=false){
  if(key==null||key.Length!=2||key[0]!='E'||key[1]<'1'||key[1]>'8')throw new ArgumentException("Expected E1 through E8",nameof(key));
  Begin(motion,targetWorld,speed,loop,false,key[1]-'1');
 }
 public void PlayBurst(GuxuCharacterMotion motion,Vector3 targetWorld,float speed=1f){Begin(motion,targetWorld,speed,false,true,0);}
 void Begin(GuxuCharacterMotion motion,Vector3 targetWorld,float rate,bool loop,bool burst,int effect){
  if(!root)throw new InvalidOperationException("Bind a UI root before playing spell VFX");
  if(float.IsNaN(rate)||float.IsInfinity(rate)||rate<=0)throw new ArgumentOutOfRangeException(nameof(rate));
  Cancel();actor=motion;target=targetWorld;speed=rate;looping=loop;composite=burst;
  Duration=burst?1.4f:Lifetimes[effect];Elapsed=0;IsPlaying=true;
  for(int i=0;i<(burst?8:1);i++){
   var go=new GameObject("Spell E"+(burst?i+1:effect+1),typeof(RectTransform),typeof(CanvasRenderer));
   var rt=(RectTransform)go.transform;rt.SetParent(root,false);rt.anchorMin=rt.anchorMax=root.pivot;rt.pivot=Vector2.one*.5f;rt.sizeDelta=new Vector2(440,440);
   var layer=go.AddComponent<GuxuSpellLayer>();layer.raycastTarget=false;layer.Kind=burst?i:effect;layers.Add(layer);
  }
  Evaluate();
 }
 void Update(){ManualUpdate(Time.deltaTime);}
 public void ManualUpdate(float deltaTime){
  if(!IsPlaying)return;
  if(float.IsNaN(deltaTime)||float.IsInfinity(deltaTime)||deltaTime<0)throw new ArgumentOutOfRangeException(nameof(deltaTime));
  Elapsed+=deltaTime*speed;
  if(Elapsed>=Duration){if(looping)Elapsed%=Duration;else{Cancel();return;}}
  Evaluate();
 }
 void Evaluate(){
  foreach(var l in layers){
   int k=l.Kind;float start=composite?Starts[k]:0,end=composite?Ends[k]:Duration;
   bool visible=Elapsed>=start&&Elapsed<end;l.gameObject.SetActive(visible);if(!visible)continue;
   Vector3 center=target;
   if(actor&&(k==0||k==4||k==5))center=actor.HandWorld;
   l.rectTransform.anchoredPosition=(Vector2)root.InverseTransformPoint(center);
   l.Sample((Elapsed-start)/(end-start),Elapsed);
  }
 }
 public void Cancel(){
  IsPlaying=false;foreach(var l in layers)if(l){l.gameObject.SetActive(false);if(Application.isPlaying)Destroy(l.gameObject);else DestroyImmediate(l.gameObject);}layers.Clear();
 }
 void OnDestroy(){Cancel();}
 void OnDisable(){Cancel();}
 public static void ValidateContract(){
  for(int i=0;i<8;i++)if(Lifetimes[i]<=0||Starts[i]<0||Ends[i]>1.4f||Ends[i]<=Starts[i])throw new InvalidOperationException("Invalid spell timeline");
 }
}
}
