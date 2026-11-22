using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Gxdl {
// Story state is independent from the equipment save. Cinematics never grant or consume rewards.
public partial class GxdlGame {
 public static string OpeningStoryKey=>"Guxu.FinalArt.OpeningSeen.v1"+GameRules.AccountScope;
 public static string FirstUnsealStoryKey=>"Guxu.FinalArt.FirstUnsealSeen.v1"+GameRules.AccountScope;
 public bool IsStoryPlaying=>storyController!=null;
 public bool NeedsFirstUnsealStory=>false;
 Gxdl.Modules.StoryOverlayController storyController;
 bool storySkip;
 int storyGeneration;

 public void PlayOpeningIfNeeded(){
  if(!CanPlay||IsStoryPlaying||busy||battle||modal!=null||Rules.Save.pending!=null||PlayerPrefs.GetInt(OpeningStoryKey,0)!=0)return;
  StartCoroutine(PlayStory(false));
 }
 public void ReplayOpening(){
  if(!CanPlay||IsStoryPlaying||busy||battle)return;
  CloseModal();StartCoroutine(PlayStory(false));
 }
 public void SkipStory(){if(IsStoryPlaying)storySkip=true;}
 public IEnumerator PlayFirstUnsealStory(){yield break;}

 IEnumerator PlayStory(bool firstSeal,Action onCompleted=null){
  if(IsStoryPlaying)yield break;
  GuxuStoryData.Validate();
  string completionKey=firstSeal?FirstUnsealStoryKey:OpeningStoryKey;
  int version=screenVersion,lastTick=Time.frameCount;
  bool priorBusy=busy,accepted=false;
  busy=true;auto=false;storySkip=false;
  var shots=firstSeal?GuxuStoryData.FirstSeal:GuxuStoryData.Opening;
  var controller=Gxdl.Modules.StoryOverlayController.Create(root);
  if(controller==null){busy=priorBusy;yield break;}
  storyController=controller;
  var overlay=controller.transform as RectTransform;
  // The prefab owns its stretched layout; the legacy fitter collapses its zero sizeDelta.
  // Independent lifetime guard also cleans up when a caller stops all game coroutines.
  overlay.gameObject.AddComponent<GuxuStoryLifetime>().Bind(
   ()=>this&&isActiveAndEnabled&&version==screenVersion&&controller==storyController&&Time.frameCount-lastTick<=3,
   ()=>{if(controller!=storyController)return;storyController=null;storySkip=false;if(version==screenVersion)busy=priorBusy;});
  bool firstSealValue=firstSeal;
  controller.Prepare(shots,SkipStory);
  controller.View.SetHeader(firstSealValue?"遗 宝 初 现":"序 章  /  遗 石 初 鸣");
  try{
   var tick=controller.Play(
    ()=>storySkip||version!=screenVersion||!overlay||controller!=storyController,
    title=>{lastTick=Time.frameCount;if(controller.View.Title!=null)controller.View.Title.gameObject.SetActive(!firstSealValue&&title=="序 章  /  遗 石 初 鸣");},
    (shot,elapsed)=>default);
   while(tick.MoveNext()){lastTick=Time.frameCount;yield return tick.Current;}
   accepted=version==screenVersion&&overlay&&controller==storyController;
  }finally{
   if(accepted){PlayerPrefs.SetInt(completionKey,1);PlayerPrefs.Save();onCompleted?.Invoke();}
   if(controller){controller.gameObject.SetActive(false);Destroy(controller.gameObject);}
   if(controller==storyController){storyController=null;storySkip=false;if(version==screenVersion)busy=priorBusy;}
  }
 }
}

public static class GuxuStoryData {
 public sealed class Shot {
  public readonly string atlas,title,speaker,subtitle;
  public readonly int cell,motion;
  public readonly float duration;
  public readonly Vector3 cameraFrom,cameraTo;
  public readonly Vector2 focus;
  public Shot(string a,int c,float d,string t,string s,string body,int m=0,Vector3? from=null,Vector3? to=null,Vector2? light=null){
   atlas=a;cell=c;duration=d;title=t;speaker=s;subtitle=body;motion=m;
   cameraFrom=from??new Vector3(-16,12,1.06f);cameraTo=to??new Vector3(16,-15,1.13f);focus=light??new Vector2(.5f,.44f);
  }
 }
 public static readonly Shot[] Opening={
  new Shot("opening-world",0,4,"云上仙城 · 盛世","旁白","很久以前，这片大陆的城池，建在云上。",0,new Vector3(20,-28,1.14f),new Vector3(-14,24,1.06f)),
  new Shot("opening-world",1,4,"天门开启 · 魔潮入侵","旁白","人们推开天门，等来的却是无尽魔潮。",1,new Vector3(16,-10,1.06f),new Vector3(-25,42,1.18f),new Vector2(.52f,.28f)),
  new Shot("opening-world",2,4,"仙宫崩落 · 古石封存","旁白","仙宫坠落，传承断绝。万千遗宝，封入古石。",2,new Vector3(-22,35,1.15f),new Vector3(18,-26,1.07f)),
  new Shot("opening-world",3,4,"千年之后 · 遗址重现","旁白","千年之后，一位寻墟者，踏入了这片遗址。",0,new Vector3(0,-25,1.05f),new Vector3(14,20,1.12f)),
  new Shot("opening-seeker",0,4,"师父留信 · 符光指路","师父 · 留信","若我未归，便去沉剑遗庭。\n修好鉴宝台，那里有我留下的线索。",4,new Vector3(20,0,1.06f),new Vector3(-38,12,1.15f),new Vector2(.83f,.49f)),
  new Shot("opening-seeker",1,5,"遗墟相遇 · 阿篆苏醒","阿篆 / 寻墟者","阿篆：总算有人来了。你师父青崖呢？\n寻墟者：我来找他。",0,new Vector3(24,-12,1.08f),new Vector3(-24,18,1.12f)),
  new Shot("opening-seeker",2,5,"鉴宝台 · 古石初鸣","阿篆","这是他留给你的古石。\n贴上符咒，试着解开他留下的秘密吧。",4,new Vector3(16,-15,1.05f),new Vector3(-20,25,1.14f),new Vector2(.66f,.33f))
 };
 public static readonly Shot[] FirstSeal={
  new Shot("opening-seeker",2,1.5f,"贴符破封","","符文亮起，飞剑沿石面运行。",4),
  new Shot("first-seal",0,2,"石壳裂开","","古石崩裂，青光涌出。",2),
  new Shot("first-seal",1,1.5f,"遗宝显现","","一柄带着旧剑鸣的断剑缓缓升起。",3),
  new Shot("first-seal",2,1.5f,"记忆闪回","师父 · 残留声音","别来归墟……门还没有——",3),
  new Shot("opening-seeker",3,1.5f,"确认遗宝","主角 / 阿篆","主角：这是师父的剑。\n阿篆：他从不离身的……怎么会被封在这里？"),
  new Shot("first-seal",3,2,"守剑傀儡苏醒","阿篆","退后！先做准备，它还能伤人！",1)
 };
 public static Texture2D Texture(string name){
  var texture=Resources.Load<Texture2D>("Game/Guxu/Final/Story/"+name);
  if(!texture)throw new InvalidOperationException("Missing story art: "+name);
  return texture;
 }
 public static Rect Cell(int index,int width,int height){
  if(index<0||index>3||width<4||height<4)throw new ArgumentOutOfRangeException(nameof(index));
  // Half-pixel inset prevents bilinear sampling from bleeding into the adjacent scene.
  float x=(index%2)*.5f,y=(1-index/2)*.5f;
  return new Rect(x+.5f/width,y+.5f/height,.5f-1f/width,.5f-1f/height);
 }
 public static Vector3 CameraPose(Shot shot,float elapsed){
  var pose=Vector3.Lerp(shot.cameraFrom,shot.cameraTo,Mathf.SmoothStep(0,1,elapsed/shot.duration));
  if(shot.motion==2){float shake=Mathf.Exp(-elapsed*2.8f)*6;pose.x+=Mathf.Sin(elapsed*43)*shake;pose.y+=Mathf.Cos(elapsed*37)*shake;}
  return pose;
 }
 public static void Validate(){
  ValidateShots(Opening,30);ValidateShots(FirstSeal,10);
  foreach(string atlas in new[]{"opening-world","opening-seeker","first-seal"}){
   var texture=Texture(atlas);
   if(texture.width<1024||texture.height<1536)throw new InvalidOperationException("Story atlas import resolution too small: "+atlas);
  }
 }
 static void ValidateShots(Shot[] shots,float expected){
  float total=0;foreach(var shot in shots){if(shot.duration<=0||shot.cell<0||shot.cell>3||string.IsNullOrEmpty(shot.subtitle))throw new InvalidOperationException("Invalid story shot.");total+=shot.duration;}
  if(Mathf.Abs(total-expected)>.001f)throw new InvalidOperationException("Story timeline duration drifted.");
 }
}

public sealed class GuxuStoryLifetime : MonoBehaviour {
 Func<bool> valid;Action cancel;
 public void Bind(Func<bool> isValid,Action onCancel){valid=isValid;cancel=onCancel;}
 void Update(){
  if(valid==null||valid())return;
  var callback=cancel;valid=null;cancel=null;callback?.Invoke();gameObject.SetActive(false);Destroy(gameObject);
 }
}
}


