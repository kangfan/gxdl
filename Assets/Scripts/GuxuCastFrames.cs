using System;
using UnityEngine;
using UnityEngine.UI;
namespace Gxdl {
// Whole-character poses share a fixed floor; particle anchors belong to the displayed frame.
public sealed class GuxuCastFrames {
 [Serializable] public sealed class Frame {public string key;public float[] palm,leftFoot,rightFoot;}
 [Serializable] public sealed class Data {public float visibleHeightPixels;public int releaseFrame,idleFrame;public string talismanKey;public float[] ordinaryTimes,fullTimes;public Frame[] frames;}
 public const string ResourcePath="Game/Guxu/Vfx/Actions/OriginalHero/unseal-frames";
 static Data data;static Sprite[] sprites;static Sprite talisman;
 readonly Image image;readonly RectTransform parent;Vector2 floor,referenceFoot;float elapsed,started;
 public bool Playing {get;private set;}
 public bool Visible=>image&&image.enabled;
 public int Index {get;private set;}=-1;
 public string Clip=>Playing?"unseal_original":"unseal_original_idle";
 public float Progress=>Playing?Mathf.Clamp01(elapsed/data.ordinaryTimes[data.frames.Length-1]):0;
 public static float ReleaseTime=>data==null?.667f:data.ordinaryTimes[data.releaseFrame];
 public static float Duration=>data==null?2.45f:data.ordinaryTimes[data.frames.Length-1]+.08f;
 public static Sprite Talisman=>talisman;
 public GuxuCastFrames(RectTransform actor){
  if(data==null){var text=Resources.Load<TextAsset>(ResourcePath);if(!text)throw new InvalidOperationException("Missing approved unseal frame metadata.");var loaded=JsonUtility.FromJson<Data>(text.text);
   if(loaded==null||loaded.frames==null||loaded.frames.Length<8||loaded.visibleHeightPixels<=0||loaded.releaseFrame<0||loaded.releaseFrame>=loaded.frames.Length||loaded.ordinaryTimes==null||loaded.fullTimes==null||loaded.ordinaryTimes.Length!=loaded.frames.Length||loaded.fullTimes.Length!=loaded.frames.Length)throw new InvalidOperationException("Invalid unseal frame metadata.");
   if(loaded.idleFrame<0||loaded.idleFrame>=loaded.frames.Length)throw new InvalidOperationException("Invalid home idle frame.");
   sprites=new Sprite[loaded.frames.Length];for(int i=0;i<sprites.Length;i++){var f=loaded.frames[i];if(f==null||!ValidAnchor(f.palm)||!ValidAnchor(f.leftFoot)||!ValidAnchor(f.rightFoot)||(i>0&&(loaded.ordinaryTimes[i]<=loaded.ordinaryTimes[i-1]||loaded.fullTimes[i]<=loaded.fullTimes[i-1])))throw new InvalidOperationException("Invalid actual frame anchors/timing.");var texture=Resources.Load<Texture2D>(f.key);if(!texture)throw new InvalidOperationException("Missing approved action frame: "+f.key);sprites[i]=Sprite.Create(texture,new Rect(0,0,texture.width,texture.height),Vector2.zero,100);sprites[i].name="Unseal Frame "+i;}
   var sealTexture=Resources.Load<Texture2D>(loaded.talismanKey);if(!sealTexture)throw new InvalidOperationException("Missing video talisman.");talisman=Sprite.Create(sealTexture,new Rect(0,0,sealTexture.width,sealTexture.height),new Vector2(.5f,.5f),100);data=loaded;
  }
  parent=actor;var node=new GameObject("Unseal Whole Character Frames",typeof(RectTransform),typeof(CanvasRenderer),typeof(Image));node.transform.SetParent(actor,false);image=node.GetComponent<Image>();image.raycastTarget=false;image.enabled=false;image.rectTransform.anchorMin=image.rectTransform.anchorMax=actor.pivot;image.rectTransform.pivot=Vector2.zero;
 }
 static bool ValidAnchor(float[] a)=>a!=null&&a.Length==2&&a[0]>=0&&a[0]<=1&&a[1]>=0&&a[1]<=1;
 // The cast is an arm performance; the feet stay planted. Re-centring every frame on that frame's own
 // foot midpoint moved the whole picture by up to three pixels as the anchors drift, which read as the
 // lower body swaying. One reference foot is captured per playback and reused for every frame.
 static Vector2 FootOf(Frame f)=>new Vector2((f.leftFoot[0]+f.rightFoot[0])*.5f,(f.leftFoot[1]+f.rightFoot[1])*.5f);
 public void Play(bool complete,Vector3 footWorld){floor=parent.InverseTransformPoint(footWorld);referenceFoot=FootOf(data.frames[0]);elapsed=0;started=Time.time;Playing=true;image.enabled=true;Show(0);}
 public void Idle(Vector3 footWorld){floor=parent.InverseTransformPoint(footWorld);referenceFoot=FootOf(data.frames[data.idleFrame]);Playing=false;image.enabled=true;Show(data.idleFrame);}
 public void Tick(){if(!Playing)return;elapsed=Time.time-started;var times=data.ordinaryTimes;int index=0;while(index+1<times.Length&&elapsed>=times[index+1])index++;Show(index);if(elapsed>=Duration){Playing=false;Show(data.idleFrame);}}
 public void Release(){if(!Playing)return;elapsed=Mathf.Max(Time.time-started,ReleaseTime);started=Time.time-elapsed;Tick();}
 void Show(int index){Index=index;image.sprite=sprites[index];float scale=parent.rect.height*.99f/data.visibleHeightPixels;var size=new Vector2(image.sprite.rect.width,image.sprite.rect.height)*scale;image.rectTransform.sizeDelta=size;
  image.rectTransform.anchoredPosition=floor-Vector2.Scale(referenceFoot,size);
 }
 public Vector3 HandWorld=>Anchor(data.frames[Index].palm);
 public Vector3 FootWorld(bool right)=>Anchor(right?data.frames[Index].rightFoot:data.frames[Index].leftFoot);
 Vector3 Anchor(float[] point)=>image.rectTransform.TransformPoint(Vector2.Scale(new Vector2(point[0],point[1]),image.rectTransform.rect.size));
 public void Stop(){Playing=false;if(image)image.enabled=false;Index=-1;}
}
}
