using System;
using UnityEngine;

namespace Gxdl {
// The original white-haired character, repainted into keyed joint poses from the video reference.
public static class GuxuCharacterPoses {
 static readonly Sprite[][] clips=new Sprite[3][];
 static readonly string[] names={"attack","hit","unseal"};
 static readonly float[][] feetX={new[]{294f,258,231,281,264,249},new[]{294f,259,248,270,263,253},new[]{273f,261,257,262,265,274}};
 static readonly float[][] feetY={new[]{475f,475,475,475,475,475},new[]{475f,476,476,476,476,476},new[]{482f,481,482,483,482,482}};
 static readonly Vector2[] spellHands={new Vector2(.60f,.66f),new Vector2(.50f,.575f),new Vector2(.47f,.75f),new Vector2(.77f,.78f),new Vector2(.55f,.70f),new Vector2(.60f,.66f)};
 public static Sprite Get(int clip,int frame){
  // Always return the same neutral frame, avoiding face/scale changes between generated sheets.
  if(frame==0||frame==5){clip=0;frame=0;}
  if(clips[clip]==null){
   var atlas=Resources.Load<Texture2D>("Game/Guxu/CharacterMotion/"+names[clip]);
   if(!atlas||atlas.width!=1536||atlas.height!=1024)throw new InvalidOperationException("Invalid character pose atlas: "+names[clip]);
   clips[clip]=new Sprite[6];
   for(int i=0;i<6;i++){
    var sprite=Sprite.Create(atlas,new Rect(i%3*512,(1-i/3)*512,512,512),new Vector2(.5f,.5f),100,0,SpriteMeshType.FullRect);
    sprite.name=names[clip]+"-"+i;clips[clip][i]=sprite;
   }
  }
  return clips[clip][frame];
 }
 public static Vector2 Alignment(int clip,int frame){
  if(frame==0||frame==5){clip=0;frame=0;}
  return new Vector2(256-feetX[clip][frame],feetY[clip][frame]-475)/512f;
 }
 public static Vector2 Hand(int frame)=>spellHands[Mathf.Clamp(frame,0,5)];
 public static void Validate(){
  for(int clip=0;clip<3;clip++)for(int frame=0;frame<6;frame++){
   if(Get(clip,frame).rect.size!=new Vector2(512,512))throw new InvalidOperationException("Pose frame size mismatch");
   if(Alignment(clip,frame).magnitude>.15f)throw new InvalidOperationException("Pose registration out of bounds");
  }
 }
}
}
