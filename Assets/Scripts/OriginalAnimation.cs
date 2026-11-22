using System;
using UnityEngine;
using UnityEngine.UI;
namespace Gxdl {
[Serializable] public class BakedClip { public string name;public int frames,columns;public float duration; }
[Serializable] public class BakedManifest {public string name;public int fps,width,height;public BakedClip[] clips;}
public class OriginalAnimation:MonoBehaviour {
 RawImage image; BakedManifest manifest; BakedClip clip; string folder; float elapsed;public float Speed=1;bool loop;
 public void Load(string name,string animation,bool repeat=true){folder="Original/Animations/"+name+"/";manifest=JsonUtility.FromJson<BakedManifest>(Resources.Load<TextAsset>(folder+"manifest").text);image=GetComponent<RawImage>();Play(animation,repeat);}
 public void Play(string animation,bool repeat=true){clip=Array.Find(manifest.clips,c=>c.name==animation)??manifest.clips[0];image.texture=Resources.Load<Texture2D>(folder+clip.name);elapsed=0;loop=repeat;Render();}
 void Update(){elapsed+=Time.deltaTime*Mathf.Max(0,Speed);Render();}
 void Render(){if(clip==null||image.texture==null)return;int f=(int)(elapsed*manifest.fps);f=loop?f%clip.frames:Math.Min(f,clip.frames-1);float w=(float)manifest.width/image.texture.width,h=(float)manifest.height/image.texture.height;image.uvRect=new Rect(f%clip.columns*w,1-(f/clip.columns+1)*h,w,h);}
}
}
