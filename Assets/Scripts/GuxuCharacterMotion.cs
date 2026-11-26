using System;
using Spine;
using Spine.Unity;
using UnityEngine;
using UnityEngine.UI;
namespace Gxdl {
// Home uses the supplied 2.0 Spine rig; combat keeps its existing rig and actions.
[RequireComponent(typeof(Image))]
public sealed class GuxuCharacterMotion : MonoBehaviour {
 public enum ActionState { Idle, Attack, Hit, Unseal }
 public const float AttackDuration=1f,AttackImpact=.44f,HitDuration=.48f;
 public const float ComboDuration=3f,ComboImpact1=.6f,ComboImpact2=1.6f,ComboImpact3=2.67f;
 public const string HomeCastClip="start_02";
 // The export's remaining 3.47 seconds only rotate the palm by 0.08 degrees.
 public const float HomeCastRelease=.5333f,HomeCastEnd=.8667f;
 public SkeletonGraphic Graphic {get;private set;}
 public int MeshBuildCount {get;private set;}
 public float CastAmount;
 public float AttackTravel;
 [SerializeField] bool useHomePose;
 // Source-pixel offsets on the supplied palm and sole bones; editable without moving the actor.
 [SerializeField] Vector2 homePalmOffset=new Vector2(40.42f,-8.96f);
 [SerializeField] Vector2 homeLeftFootOffset=new Vector2(99.04f,26.28f);
 [SerializeField] Vector2 homeRightFootOffset=new Vector2(98.05f,-29.37f);
 public bool UseHomePose {get=>useHomePose;set{useHomePose=value;ResetPose();}}
 float dashTime=-1,dashSpeed=1,offset,returnVelocity;
 bool comboDash;
 public string AnimationSource=>useHomePose?"Spine-home-2.0":"Spine";
 public string CurrentClip=>Graphic?Graphic.AnimationState.GetCurrent(0)?.Animation.Name:null;
 public int CurrentFrame=>-1;
 public ActionState State {
  get{switch(CurrentClip){case "attack":case "attack_combo_01":return ActionState.Attack;case "hit":return ActionState.Hit;case "unseal":case HomeCastClip:case "home_cast":case "unseal_full":return ActionState.Unseal;default:return ActionState.Idle;}}
 }
 public float ActionProgress {get{var entry=Graphic?Graphic.AnimationState.GetCurrent(0):null;return entry==null?0:Mathf.Clamp01(entry.TrackTime/Mathf.Max(.0001f,entry.AnimationEnd-entry.AnimationStart));}}
 Vector3 BoneWorld(string name,Vector2 sourceOffset=default){
  if(!Graphic||Graphic.Skeleton==null)return transform.position;
  var bone=Graphic.Skeleton.FindBone(name);if(bone==null)return transform.position;
  float x,y;float scale=Graphic.skeletonDataAsset.scale;bone.LocalToWorld(sourceOffset.x*scale,sourceOffset.y*scale,out x,out y);
  var point=new Vector2(x,y)*Graphic.MeshScale+Graphic.MeshOffset;return Graphic.transform.TransformPoint(point);
 }
 public Vector3 HandWorld=>useHomePose?BoneWorld("bone18",homePalmOffset):BoneWorld("palmR");
 public Vector3 WeaponWorld=>useHomePose?HandWorld:BoneWorld("attack_tip");
 public Vector3 FootWorld(bool right)=>useHomePose?BoneWorld(right?"bone24":"bone21",right?homeRightFootOffset:homeLeftFootOffset):BoneWorld(right?"footR":"footL");
 public Vector3 FeetWorld=>(FootWorld(false)+FootWorld(true))*.5f;
 void CountMesh(Spine.Unity.MeshGeneratorBuffers buffers){MeshBuildCount++;}
 void EnsureGraphic(){
  GetComponent<Image>().enabled=false;
  // Old cached Home instances can survive an Editor script reload with the frame overlay still present.
  var frames=transform.Find("Unseal Whole Character Frames");if(frames)frames.gameObject.SetActive(false);
  var asset=useHomePose?GuxuSpineAssets.HomeData:GuxuSpineAssets.Data;
  if(!Graphic)Graphic=GetComponentInChildren<SkeletonGraphic>(true);
  if(!Graphic)Graphic=SkeletonGraphic.NewSkeletonGraphicGameObject(asset,transform,GuxuSpineAssets.Material);
  else if(Graphic.skeletonDataAsset!=asset||Graphic.Skeleton==null){Graphic.skeletonDataAsset=asset;Graphic.Initialize(true);}
  Graphic.material=GuxuSpineAssets.Material;Graphic.allowMultipleCanvasRenderers=true;Graphic.name="Spine Hero";Graphic.raycastTarget=false;
  var rect=Graphic.rectTransform;rect.anchorMin=rect.anchorMax=new Vector2(.5f,0);rect.pivot=new Vector2(.5f,0);
  float height=((RectTransform)transform).rect.height;
  float size=useHomePose?height/(asset.GetSkeletonData(false).Height*asset.scale*CanvasPixelsPerUnit):height/(1448f*.35f*100);
  rect.localScale=Vector3.one*size;
  Graphic.OnPostProcessVertices-=CountMesh;Graphic.OnPostProcessVertices+=CountMesh;
 }
 float CanvasPixelsPerUnit=>Graphic.canvas?Graphic.canvas.referencePixelsPerUnit:100;
 Vector2 RigOrigin {
  get{if(!useHomePose)return new Vector2(0,15);var d=Graphic.Skeleton.Data;float scale=Graphic.skeletonDataAsset.scale*Graphic.rectTransform.localScale.x*CanvasPixelsPerUnit;return new Vector2(-(d.X+d.Width*.5f)*scale,15-d.Y*scale);}
 }
 void Awake(){ResetPose();}
 void Update(){
  if(!Graphic||useHomePose)return;
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
 void OnEnable(){ResetPose();}
 void OnDisable(){if(Graphic){Graphic.AnimationState.ClearTracks();Graphic.gameObject.SetActive(false);}}
 public void PlayAttack(float speed=1)=>Play("attack",speed);
 public void PlayAttackCombo(float speed=1)=>Play("attack_combo_01",speed);
 public void PlayLibraryAction(string name,float speed=1,bool loop=false){
  if(string.IsNullOrEmpty(name)||!name.StartsWith("library_",StringComparison.Ordinal)||Graphic.Skeleton.Data.FindAnimation(name)==null)throw new ArgumentException("Unknown library action: "+name,nameof(name));
  Graphic.gameObject.SetActive(true);speed=Mathf.Clamp(speed,.1f,5);dashTime=-1;offset=0;returnVelocity=0;Graphic.rectTransform.anchoredPosition=RigOrigin;
  var entry=Graphic.AnimationState.SetAnimation(0,name,loop);entry.TimeScale=speed;entry.MixDuration/=speed;
  if(!loop)Graphic.AnimationState.AddAnimation(0,"library_idle",true,0);
 }
 public void PlayHit(float speed=1)=>Play("hit",speed);
 public void PlayUnseal(){
  if(!useHomePose){Play("unseal",1);return;}
  ResetPose();var entry=Graphic.AnimationState.SetAnimation(0,HomeCastClip,false);entry.MixDuration=0;entry.AnimationEnd=HomeCastEnd;
  var idle=Graphic.AnimationState.AddAnimation(0,"idle",true,entry.AnimationEnd-entry.AnimationStart);idle.MixDuration=.06f;
  Graphic.Update(0);Graphic.UpdateMesh();
 }
 public void PlayUnsealFull()=>PlayUnseal();
 public void MarkCastRelease(){
  var entry=Graphic?Graphic.AnimationState.GetCurrent(0):null;
  if(!useHomePose||entry?.Animation.Name!=HomeCastClip)return;
  // Sample the authored release pose exactly, independent of Update/coroutine ordering.
  entry.TrackTime=HomeCastRelease;Graphic.Update(0);Graphic.UpdateMesh();
 }
 void Play(string name,float speed){
  Graphic.gameObject.SetActive(true);
  speed=Mathf.Clamp(speed,.1f,5);comboDash=name=="attack_combo_01";dashTime=(name=="attack"||comboDash)&&AttackTravel>0?0:-1;dashSpeed=speed;var entry=Graphic.AnimationState.SetAnimation(0,name,false);entry.TimeScale=speed;entry.MixDuration/=speed;
  var idle=Graphic.AnimationState.AddAnimation(0,"idle",true,0);idle.TimeScale=1;
 }
 public void ResetPose(){
  EnsureGraphic();CastAmount=0;dashTime=-1;offset=0;returnVelocity=0;
  Graphic.gameObject.SetActive(isActiveAndEnabled);Graphic.rectTransform.anchoredPosition=RigOrigin;
  Graphic.AnimationState.ClearTracks();Graphic.Skeleton.SetToSetupPose();Graphic.AnimationState.SetAnimation(0,"idle",true);Graphic.Update(0);Graphic.UpdateMesh();
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







