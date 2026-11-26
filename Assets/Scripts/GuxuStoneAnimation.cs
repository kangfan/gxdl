using System;
using UnityEngine;
using UnityEngine.UI;

namespace Gxdl {
// The whole canvas stays registered across frames; the artist's floating motion is inside each image.
[DisallowMultipleComponent, RequireComponent(typeof(Image))]
public sealed class GuxuStoneAnimation : MonoBehaviour {
 [Serializable] sealed class Sequence {public float framesPerSecond;public int impactFrame;public string[] frames;}
 const string ResourcePath="Game/Guxu/Final/StoneIdle/sequence";
 const string FractureResourcePath="Game/Guxu/Final/StoneShatter/sequence";
 static Sprite[] frames,fractureFrames;
 static float framesPerSecond,fractureFramesPerSecond;
 static int impactFrame;
 Image image;
 float elapsed;
 bool usingFractureFrames;
 public bool IsReady=>frames!=null&&image&&image.sprite;
 public Sprite CurrentSprite=>image?image.sprite:null;
 public bool HasFractured {get;private set;}
 public float FractureImpactDuration=>fractureFrames==null?0:(fractureFrames.Length-impactFrame)/fractureFramesPerSecond;

 public void Initialize(){
  image=GetComponent<Image>();image.preserveAspect=true;image.raycastTarget=false;
  try{if(frames==null)frames=LoadFrames(ResourcePath,out framesPerSecond,out _);ResetIdle();}
  catch(Exception error) when(error is InvalidOperationException||error is ArgumentException){image.sprite=null;image.enabled=false;Debug.LogError(error.Message,this);}
  try{if(fractureFrames==null)fractureFrames=LoadFrames(FractureResourcePath,out fractureFramesPerSecond,out impactFrame);}
  catch(Exception error) when(error is InvalidOperationException||error is ArgumentException){Debug.LogWarning("Stone fracture sequence unavailable; using geometric fallback. "+error.Message,this);}
 }

 static Sprite[] LoadFrames(string path,out float rate,out int impact){
  var text=Resources.Load<TextAsset>(path);
  if(!text)throw new InvalidOperationException("Missing stone sequence: "+path);
  var sequence=JsonUtility.FromJson<Sequence>(text.text);
  if(sequence==null||sequence.frames==null||sequence.frames.Length==0||sequence.framesPerSecond<=0||float.IsNaN(sequence.framesPerSecond)||float.IsInfinity(sequence.framesPerSecond)||sequence.impactFrame<0||sequence.impactFrame>=sequence.frames.Length)
   throw new InvalidOperationException("Invalid stone sequence: "+path);
  var textures=new Texture2D[sequence.frames.Length];
  for(int i=0;i<textures.Length;i++){
   textures[i]=Resources.Load<Texture2D>(sequence.frames[i]);
   if(!textures[i]||(i>0&&(textures[i].width!=textures[0].width||textures[i].height!=textures[0].height)))
    throw new InvalidOperationException("Missing or inconsistent stone frame: "+sequence.frames[i]);
  }
  var loaded=new Sprite[textures.Length];
  for(int i=0;i<loaded.Length;i++){
   var texture=textures[i];loaded[i]=Sprite.Create(texture,new Rect(0,0,texture.width,texture.height),new Vector2(.5f,.5f),100,0,SpriteMeshType.FullRect);
   loaded[i].name=path+"/"+i.ToString("D2");
  }
  rate=sequence.framesPerSecond;impact=sequence.impactFrame;return loaded;
 }

 void SetSize(Vector2 size){var rect=image.rectTransform;rect.anchoredPosition+=Vector2.Scale(rect.pivot-new Vector2(.5f,.5f),size-rect.rect.size);rect.sizeDelta=size;}
 public void ResetIdle(){usingFractureFrames=false;HasFractured=false;elapsed=0;if(!image)return;SetSize(new Vector2(256,256));if(frames!=null)image.sprite=frames[0];}
 public bool BeginFracture(){
  if(frames==null||fractureFrames==null)Initialize();
  if(!IsReady||fractureFrames==null)return false;
  usingFractureFrames=true;HasFractured=false;
  // The shatter canvas adds transparent padding; keep the same source-pixel scale and center.
  SetSize(new Vector2(256f*fractureFrames[0].rect.width/frames[0].rect.width,256f*fractureFrames[0].rect.height/frames[0].rect.height));
  image.sprite=fractureFrames[0];return true;
 }
 public void ShowFractureLeadIn(float flightProgress){
  if(!usingFractureFrames)return;
  image.sprite=fractureFrames[Mathf.Clamp((int)(Mathf.Clamp01(flightProgress)*impactFrame),0,Mathf.Max(0,impactFrame-1))];
 }
 public void ShowFractureImpact(float time){
  if(!usingFractureFrames)return;
  HasFractured=true;image.sprite=fractureFrames[Mathf.Clamp(impactFrame+(int)(time*fractureFramesPerSecond),impactFrame,fractureFrames.Length-1)];
 }
 // Script reloads clear static sprites while keeping the active Home and its Image alive.
 void OnEnable(){Initialize();}
 void OnDisable(){ResetIdle();}
 void Update(){
  if(!image||!image.enabled||frames==null||usingFractureFrames)return;
  elapsed=Mathf.Repeat(elapsed+Time.deltaTime,frames.Length/framesPerSecond);
  image.sprite=frames[Mathf.Min((int)(elapsed*framesPerSecond),frames.Length-1)];
 }
}
}
