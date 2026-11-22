using System.Collections;
using UnityEngine;
namespace Gxdl {
public sealed partial class GuxuAnimation {
 GuxuParticleFx Particles(string name,bool cloud=false){var p=GuxuParticleFx.Create(layer,name,cloud,-8,true);temporary.Add(p.gameObject);return p;}
 void ContactParticles(Vector2 hit){var sparks=GuxuParticleFx.Impact(layer,layer.TransformPoint(hit),new Color(1,.82f,.40f,.95f),30,1.45f);temporary.Add(sparks.gameObject);}
 GuxuCastRuneField Runes(){var r=Node("Cast Hand Runes",CharacterHand(),new Vector2(220,220));var field=r.gameObject.AddComponent<GuxuCastRuneField>();return field;}
 public IEnumerator PlayUnseal(Sprite swordSprite,Sprite relicSprite,int quality){
  if(IsPlaying)yield break;if(!character||!stone||!layer||!relicSprite)throw new System.InvalidOperationException("Bind and relic required.");
  BeginStonePresentation();characterMotion.PlayUnseal();IsPlaying=true;skip=false;int ticket=++generation;bool completed=false;
  Vector2 hit=Center(stone.rectTransform);var glow=QualityColors[Mathf.Clamp(quality-1,0,4)];
  var halo=Ring("Seal Rune Ring",hit,new Vector2(300,300),Color.clear);var charge=Ring("Casting Ring",CharacterHand(),new Vector2(95,95),Color.clear);
  var seal=SpriteImage("Flying Seal",GuxuCastFrames.Talisman??GuxuUiArt.Navigation(GuxuUiArt.Icon.SealToken),CharacterHand(),new Vector2(96,96));seal.color=Color.clear;
  var runes=Runes();
  var particles=Particles("Particle Casting and Talisman Trail");
  try{
   Phase="charge";float emit=0;
   for(float t=0;t<CastRelease;t+=Time.deltaTime){if(!Valid(ticket))yield break;float p=t/CastRelease;var hand=CharacterHand();charge.rectTransform.anchoredPosition=hand;charge.color=new Color(1,.83f,.35f,p*.65f);charge.rectTransform.localScale=Vector3.one*(.5f+p*.5f);halo.color=new Color(1,.79f,.3f,p*.22f);
    seal.rectTransform.anchoredPosition=hand;seal.color=new Color(1,1,1,Mathf.InverseLerp(Mathf.Max(0,CastRelease-.17f),CastRelease,t));
    runes.rectTransform.anchoredPosition=hand;runes.SetState(p,0,t);
    if(t>=emit){float a=t*18;var d=new Vector3(Mathf.Cos(a),Mathf.Sin(a),0);particles.EmitWorld(characterMotion.HandWorld-layer.TransformVector(d*38),d*95,new Vector2(22,45),new Color(1,.88f,.5f,.9f),.38f,t*200);emit+=.025f;}yield return null;
   }
   Phase="seal";characterMotion.MarkCastRelease();ReleaseAt=Time.time;Vector2 launch=CharacterHand();
   // The throw is flat, so the seal leaves on a level line and only settles onto the stone at the end;
   // a high arc would fight the horizontal arm action.
   Vector2 control=launch+Vector2.right*(hit.x-launch.x)*.72f+Vector2.up*Mathf.Min(26f,(hit.y-launch.y)*.35f);
   emit=0;charge.gameObject.SetActive(false);
   for(float t=0;t<CastFlight;t+=Time.deltaTime){if(!Valid(ticket))yield break;float p=t/CastFlight;seal.color=Color.white;SealPose(seal,launch,control,hit,p);
    // The runes leave the palm on the release frame and ride the same arc as the seal, then burn out on contact.
    runes.rectTransform.anchoredPosition=Curve(launch,control,hit,p);runes.SetState(1,p,t);
    if(t>=emit){particles.EmitWorld(seal.transform.position,Vector3.left*25,new Vector2(30,60),new Color(1,.81f,.3f,.9f),.38f,-35);emit+=.018f;}yield return null;
   }
   seal.rectTransform.anchoredPosition=hit;RuneContactAt=Time.time;FractureAt=RuneContactAt;Phase="fracture";seal.gameObject.SetActive(false);runes.gameObject.SetActive(false);ContactParticles(hit);
   var pieces=SplitStone();var origins=new Vector2[pieces.Length];for(int i=0;i<pieces.Length;i++)origins[i]=pieces[i].rectTransform.anchoredPosition;stone.enabled=false;
   var relic=SpriteImage("Revealed Relic",relicSprite,hit,new Vector2(150,150));relic.color=Color.clear;
   for(float t=0;t<.58f;t+=Time.deltaTime){if(!Valid(ticket))yield break;
    for(int i=0;i<pieces.Length;i++){float age=Mathf.Max(0,t-i%4*.009f);var r=pieces[i].rectTransform;r.anchoredPosition=origins[i]+FragmentTravel((origins[i]-hit).normalized,i,age);r.localEulerAngles=new Vector3(0,0,(i%2==0?-1:1)*(80+i*37%190)*age);r.localScale=Vector3.one*(1-.42f*Mathf.Clamp01(age/.65f));pieces[i].color=new Color(.90f,.87f,.8f,1-Mathf.SmoothStep(0,1,(t-.34f)/.32f));}
    halo.color=new Color(1,.88f,.5f,Mathf.Clamp01(1-t/.22f)*.7f);halo.rectTransform.localScale=Vector3.one*(1+t);
    relic.color=new Color(1,1,1,Mathf.SmoothStep(0,1,(t-.18f)/.32f));relic.rectTransform.localScale=Vector3.one*Mathf.Lerp(.55f,1,Mathf.SmoothStep(0,1,t/.5f));relic.rectTransform.anchoredPosition=hit+Vector2.up*20*Mathf.Clamp01(t/.58f);yield return null;
   }
   // The arm clip runs 1.40s; release plus flight plus the fracture beat lands inside it, so the hold is
   // a fixed beat and the recovery wait below keeps the pose honest instead of padding to a frame clock.
   Phase="reveal";float hold=.27f+(quality>=4?.56f:0);
   for(float t=0;t<hold&&!skip;t+=Time.deltaTime){if(!Valid(ticket))yield break;halo.color=new Color(glow.r,glow.g,glow.b,.20f);halo.rectTransform.localScale=Vector3.one*(.75f+.03f*Mathf.Sin(t*5));yield return null;}
   // A rare-loot skip may shorten only the extra hold, never cut the character's recovery pose.
   while(characterMotion.State==GuxuCharacterMotion.ActionState.Unseal){if(!Valid(ticket))yield break;yield return null;}
   completed=true;
  }finally{if(this&&generation==ticket){Cleanup();Restore();if(completed&&stone)stone.enabled=false;IsPlaying=false;Phase="idle";skip=false;}}
 }
}
}
