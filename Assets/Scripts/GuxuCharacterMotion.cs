using System;
using Spine;
using Spine.Unity;
using UnityEngine;
using UnityEngine.UI;
namespace Gxdl {
// Home uses approved original-character poses for idle/casting; combat keeps the existing Spine rig.
[RequireComponent(typeof(Image))]
public sealed class GuxuCharacterMotion : MonoBehaviour {
 public enum ActionState { Idle, Attack, Hit, Unseal }
 public const float AttackDuration=1f,AttackImpact=.44f,HitDuration=.48f;
 public const float ComboDuration=3f,ComboImpact1=.6f,ComboImpact2=1.6f,ComboImpact3=2.67f;
 public SkeletonGraphic Graphic {get;private set;}
 public int MeshBuildCount {get;private set;}
 public float CastAmount;
 public float AttackTravel;
 bool useHomePose;
 public bool UseHomePose {get=>useHomePose;set{if(useHomePose==value)return;useHomePose=value;if(Graphic)ResetPose();}}
 string IdleClip=>useHomePose?"home_idle":"idle";
 float dashTime=-1,dashSpeed=1,offset,returnVelocity;
 bool comboDash;
 GuxuCastFrames castFrames;bool frameFallback;
 public string AnimationSource=>castFrames!=null&&castFrames.Visible?"whole-character-frames":frameFallback?"Spine-safe-fallback":"Spine";
 public string CurrentClip=>castFrames!=null&&castFrames.Visible?castFrames.Clip:Graphic.AnimationState.GetCurrent(0)?.Animation.Name;
 public int CurrentFrame=>castFrames!=null&&castFrames.Visible?castFrames.Index:-1;
 public ActionState State {
  get{if(castFrames!=null&&castFrames.Playing)return ActionState.Unseal;var entry=Graphic?Graphic.AnimationState.GetCurrent(0):null;
   switch(entry?.Animation.Name){case "attack":case "attack_combo_01":return ActionState.Attack;case "hit":return ActionState.Hit;case "unseal":case "home_cast":case "unseal_full":return ActionState.Unseal;default:return ActionState.Idle;}}
 }
 public float ActionProgress {get{if(castFrames!=null&&castFrames.Playing)return castFrames.Progress;var entry=Graphic?Graphic.AnimationState.GetCurrent(0):null;return entry==null?0:Mathf.Clamp01(entry.TrackTime/entry.Animation.Duration);}}
 public Vector3 HandWorld {
  get{if(castFrames!=null&&castFrames.Visible)return castFrames.HandWorld;var clip=Graphic.AnimationState.GetCurrent(0)?.Animation.Name;var hand=Graphic.Skeleton.FindBone(useHomePose&&(clip=="home_cast"||clip=="home_idle")?"home_palm_r":"palmR");return Graphic.transform.TransformPoint(new Vector3(hand.WorldX,hand.WorldY,0)*Graphic.MeshScale);}
 }
 public Vector3 WeaponWorld {get{if(castFrames!=null&&castFrames.Visible)return castFrames.HandWorld;var bone=Graphic.Skeleton.FindBone("attack_tip");return Graphic.transform.TransformPoint(new Vector3(bone.WorldX,bone.WorldY,0)*Graphic.MeshScale);}}
 public Vector3 FootWorld(bool right){if(castFrames!=null&&castFrames.Visible)return castFrames.FootWorld(right);var clip=Graphic.AnimationState.GetCurrent(0)?.Animation.Name;bool home=useHomePose&&(clip=="home_cast"||clip=="home_idle");var bone=Graphic.Skeleton.FindBone(home?(right?"home_foot_r":"home_foot_l"):(right?"footR":"footL"));return Graphic.transform.TransformPoint(new Vector3(bone.WorldX,bone.WorldY-(home?bone.Data.Y:0),0)*Graphic.MeshScale);}
 public Vector3 FeetWorld=>(FootWorld(false)+FootWorld(true))*.5f;
 void Awake(){
  var image=GetComponent<Image>();image.enabled=false;
  Graphic=SkeletonGraphic.NewSkeletonGraphicGameObject(GuxuSpineAssets.Data,transform,GuxuSpineAssets.Material);
  Graphic.allowMultipleCanvasRenderers=true;Graphic.name="Spine Hero";Graphic.raycastTarget=false;
  var rect=Graphic.rectTransform;rect.anchorMin=rect.anchorMax=new Vector2(.5f,0);rect.pivot=new Vector2(.5f,0);rect.anchoredPosition=new Vector2(0,15);
  float size=((RectTransform)transform).rect.height/(1448f*.35f*100);rect.localScale=Vector3.one*size;
  Graphic.OnPostProcessVertices+=buffers=>MeshBuildCount++;
  Graphic.AnimationState.SetAnimation(0,IdleClip,true);Graphic.Update(0);Graphic.UpdateMesh();
 }
 void Update(){
  if(!Graphic)return;
  if(castFrames!=null&&castFrames.Visible){castFrames.Tick();return;}
  if(dashTime>=0){
   dashTime+=Time.deltaTime*dashSpeed;
   // Open space for the ranged finisher after the two close slashes.
   float envelope=comboDash?Mathf.SmoothStep(0,1,(dashTime-.14f)/.28f)*(1-Mathf.SmoothStep(0,1,(dashTime-1.82f)/.38f)):
    dashTime<.14f?Mathf.SmoothStep(0,1,dashTime/.14f):1-Mathf.SmoothStep(0,1,(dashTime-.52f)/.38f);
   offset=AttackTravel*((RectTransform)transform).rect.width*envelope;
   if(dashTime>=(comboDash?ComboDuration:.90f)){dashTime=-1;offset=0;}
  }else offset=Mathf.SmoothDamp(offset,0,ref returnVelocity,.08f);
  Graphic.rectTransform.anchoredPosition=new Vector2(offset,15);
 }
 void OnEnable(){if(Graphic){Graphic.gameObject.SetActive(true);ResetPose();}}
 void OnDisable(){castFrames?.Stop();if(Graphic){Graphic.AnimationState.ClearTracks();Graphic.gameObject.SetActive(false);}}
 public void PlayAttack(float speed=1)=>Play("attack",speed);
 public void PlayAttackCombo(float speed=1)=>Play("attack_combo_01",speed);
 public void PlayLibraryAction(string name,float speed=1,bool loop=false){
  if(string.IsNullOrEmpty(name)||!name.StartsWith("library_",StringComparison.Ordinal)||Graphic.Skeleton.Data.FindAnimation(name)==null)throw new ArgumentException("Unknown library action: "+name,nameof(name));
  castFrames?.Stop();frameFallback=false;Graphic.gameObject.SetActive(true);
  speed=Mathf.Clamp(speed,.1f,5);dashTime=-1;offset=0;returnVelocity=0;Graphic.rectTransform.anchoredPosition=new Vector2(0,15);
  var entry=Graphic.AnimationState.SetAnimation(0,name,loop);entry.TimeScale=speed;entry.MixDuration/=speed;
  if(!loop)Graphic.AnimationState.AddAnimation(0,"library_idle",true,0);
 }
 public void PlayHit(float speed=1)=>Play("hit",speed);
 public void PlayUnseal(){if(useHomePose)PlayArmCast();else Play("unseal",1);}
 public void PlayUnsealFull()=>PlayUnseal();
 // The home cast is an arm-only performance: torso, pelvis and legs hold the idle pose while the rig's
 // right-arm chain throws the seal. The approved frame strip moved the whole body, so it is no longer used.
 void PlayArmCast(){
  castFrames?.Stop();frameFallback=false;
  dashTime=-1;offset=0;returnVelocity=0;CastAmount=0;
  // Reset only the rig, never through ResetPose: that re-enables the idle frame image and would leave
  // two whole characters on screen while the arm clip plays.
  Graphic.gameObject.SetActive(true);Graphic.rectTransform.anchoredPosition=new Vector2(0,15);
  Graphic.AnimationState.ClearTracks();Graphic.Skeleton.SetToSetupPose();
  var entry=Graphic.AnimationState.SetAnimation(0,"home_cast",false);entry.TimeScale=1;entry.MixDuration=0;
  Graphic.AnimationState.AddAnimation(0,"home_idle",true,0);
  Graphic.Update(0);Graphic.UpdateMesh();
 }
 void PlayCastFrames(bool full){ResetPose();var foot=FeetWorld;try{if(castFrames==null)castFrames=new GuxuCastFrames((RectTransform)transform);castFrames.Play(full,foot);Graphic.gameObject.SetActive(false);}catch(InvalidOperationException e){Debug.LogWarning("Approved cast sequence unavailable; using limited natural Spine fallback: "+e.Message);Play(full?"unseal_full":"home_cast",1);frameFallback=true;}}
 public void MarkCastRelease()=>castFrames?.Release();
 void Play(string name,float speed){
  castFrames?.Stop();frameFallback=false;Graphic.gameObject.SetActive(true);
  speed=Mathf.Clamp(speed,.1f,5);comboDash=name=="attack_combo_01";dashTime=(name=="attack"||comboDash)&&AttackTravel>0?0:-1;dashSpeed=speed;var entry=Graphic.AnimationState.SetAnimation(0,name,false);entry.TimeScale=speed;entry.MixDuration/=speed;
  var idle=Graphic.AnimationState.AddAnimation(0,IdleClip,true,0);idle.TimeScale=1;
 }
 public void ResetPose(){
  castFrames?.Stop();frameFallback=false;CastAmount=0;dashTime=-1;offset=0;returnVelocity=0;if(!Graphic)return;Graphic.gameObject.SetActive(isActiveAndEnabled);Graphic.rectTransform.anchoredPosition=new Vector2(0,15);
  Graphic.AnimationState.ClearTracks();Graphic.Skeleton.SetToSetupPose();Graphic.AnimationState.SetAnimation(0,IdleClip,true);Graphic.Update(0);Graphic.UpdateMesh();
  if(useHomePose&&isActiveAndEnabled)try{var foot=FeetWorld;if(castFrames==null)castFrames=new GuxuCastFrames((RectTransform)transform);castFrames.Idle(foot);Graphic.gameObject.SetActive(false);}catch(InvalidOperationException e){frameFallback=true;Debug.LogWarning("Original character home pose unavailable: "+e.Message);}
 }
 public static void ValidateContract(){
  var data=GuxuSpineAssets.Data.GetSkeletonData(false);
  if(data.Bones.Count<26)throw new InvalidOperationException("Incomplete approved long-robe Spine rig.");
  foreach(string name in new[]{"hairMid","hairTip","ribbon","robeLTip","robeRTip","sashTip","sleeveL","sleeveR"})
   if(data.FindBone(name)==null)throw new InvalidOperationException("Missing independent cloth/hair bone: "+name);
  foreach(string clip in new[]{"idle","attack","unseal","hit"}){
   if(data.FindAnimation(clip)==null)throw new InvalidOperationException("Missing action: "+clip);
   for(int speed=1;speed<=3;speed++){
    var skeleton=new Skeleton(data);var state=new Spine.AnimationState(GuxuSpineAssets.Data.GetAnimationStateData());
    state.SetAnimation(0,"idle",true);state.Apply(skeleton);skeleton.UpdateWorldTransform(Skeleton.Physics.Update);
    var palm=skeleton.FindBone("palmR");Vector2 previous=new Vector2(palm.WorldX,palm.WorldY);int moving=0;
    var entry=state.SetAnimation(0,clip,clip=="idle");entry.TimeScale=speed;state.AddAnimation(0,"idle",true,0);
    for(int frame=0;frame<360;frame++){
     state.Update(1/(240f*speed));state.Apply(skeleton);skeleton.UpdateWorldTransform(Skeleton.Physics.Update);
     var point=new Vector2(palm.WorldX,palm.WorldY);float step=Vector2.Distance(previous,point);
     if(float.IsNaN(point.x)||float.IsNaN(point.y)||step>38)throw new InvalidOperationException("Discontinuous Spine hand: "+clip+" speed="+speed+" frame="+frame+" time="+(frame/240f)+" step="+step+" previous="+previous+" current="+point);
     if(step>(clip=="idle"?.0001f:.01f))moving++;previous=point;
     foreach(string side in new[]{"L","R"}){
      var foot=skeleton.FindBone("foot"+side);
      if(Mathf.Abs(foot.WorldY-foot.Data.Y-skeleton.RootBone.WorldY)>.01f)throw new InvalidOperationException("Foot moved off the ground.");
     }
    }
    if(moving<15)throw new InvalidOperationException("Spine action is not continuously interpolated: "+clip);
   }
  }
  var idleSkeleton=new Skeleton(data);var idleState=new Spine.AnimationState(GuxuSpineAssets.Data.GetAnimationStateData());idleState.SetAnimation(0,"idle",true);
  idleState.Apply(idleSkeleton);float hair=idleSkeleton.FindBone("hairTip").Rotation,robe=idleSkeleton.FindBone("robeLTip").Rotation;
  idleState.Update(1.2f);idleState.Apply(idleSkeleton);
  if(Mathf.Abs(idleSkeleton.FindBone("hairTip").Rotation-hair)<.1f||Mathf.Abs(idleSkeleton.FindBone("robeLTip").Rotation-robe)<.1f)throw new InvalidOperationException("Idle hair and robe are not independently animated.");
 }
}
}







