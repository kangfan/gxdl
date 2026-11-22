using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Gxdl {
public partial class GxdlGame:MonoBehaviour {
 public static GxdlGame Instance;public GameRules Rules {get;private set;}
 GuxuBackgroundMusic backgroundMusic;
 int fittedScreenWidth,fittedScreenHeight;
 Action fitExpandedPage;
 Font font;RectTransform root,view,modal,toastPanel;Text toast,coinText;OriginalAnimation hero,enemy,tree;int screenVersion;bool busy,auto,battle;float nextAuto;int fightSpeed=1;Dictionary<string,Sprite> sprites=new Dictionary<string,Sprite>();
 static readonly string[] slots={"兵器","头冠","羽翼","靴子","腰带","护腕","葫芦","项链","玉佩","幡旗","宝珠","道袍"};
 static readonly string[] quality={"","白·凡品","绿·良品","蓝·上品","紫·史诗","橙·传说"};
 readonly Color ink=new Color(.16f,.24f,.22f),cream=new Color(.94f,.94f,.87f),gold=new Color(.86f,.78f,.54f);
 [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)] static void Bootstrap(){if(FindObjectOfType<GxdlGame>()==null)new GameObject("古墟大陆").AddComponent<GxdlGame>();}
 void Awake(){Instance=this;Application.targetFrameRate=60;Application.runInBackground=true;Screen.orientation=ScreenOrientation.Portrait;font=Resources.Load<Font>("Original/GameFont");
 var canvas=new GameObject("Game Canvas",typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));var c=canvas.GetComponent<Canvas>();c.renderMode=RenderMode.ScreenSpaceCamera;c.worldCamera=Camera.main;c.planeDistance=10;var sc=canvas.GetComponent<CanvasScaler>();sc.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;sc.referenceResolution=new Vector2(750,1334);sc.matchWidthOrHeight=.5f;
 backgroundMusic=gameObject.AddComponent<GuxuBackgroundMusic>();root=canvas.GetComponent<RectTransform>();if(FindObjectOfType<EventSystem>()==null)new GameObject("Event System",typeof(EventSystem),typeof(StandaloneInputModule));InitializeModules();InitializeAccounts();}
 // prefab 界面的宿主。旧界面路径保持不变，两者共用同一个 Canvas。
 // GameEntry 在 AfterSceneLoad 才建组合根，顺序不保证早于本方法，这里兜底自建。
 void InitializeModules(){var bootstrap=Gxdl.Core.GameBootstrap.Instance;if(bootstrap==null)bootstrap=new GameObject("Game Bootstrap").AddComponent<Gxdl.Core.GameBootstrap>();bootstrap.InitializeUi(root,font);}
 void Update(){UpdateAccountUI();backgroundMusic.SetBattle(CanPlay&&battle);if(!CanPlay)return;if(auto&&!busy&&!battle&&modal==null&&Time.time>=nextAuto){nextAuto=Time.time+1.3f;Chop();}}
 void LateUpdate(){RefreshViewportLayout();}
 RectTransform Rect(string name,Transform parent,float x,float y,float w,float h){var g=new GameObject(name,typeof(RectTransform));var r=g.GetComponent<RectTransform>();r.SetParent(parent,false);r.anchorMin=r.anchorMax=new Vector2(0,1);r.pivot=new Vector2(0,1);r.anchoredPosition=new Vector2(x,-y);r.sizeDelta=new Vector2(w,h);return r;}
 Sprite Sprite(string path){if(sprites.TryGetValue(path,out var s))return s;var t=Resources.Load<Texture2D>(path);if(t==null){Debug.LogWarning("Missing image: "+path);return null;}s=UnityEngine.Sprite.Create(t,new Rect(0,0,t.width,t.height),new Vector2(.5f,.5f),100);sprites[path]=s;return s;}
 Image Img(string path,Transform parent,float x,float y,float w,float h,Color? color=null){var r=Rect(path,parent,x,y,w,h);var im=r.gameObject.AddComponent<Image>();im.color=color??Color.white;im.raycastTarget=false;if(!string.IsNullOrEmpty(path))im.sprite=Sprite(path);return im;}
 Text Txt(string value,Transform p,float x,float y,float w,float h,int size=26,Color? color=null,TextAnchor align=TextAnchor.MiddleCenter){var r=Rect(value,p,x,y,w,h);var t=r.gameObject.AddComponent<Text>();t.font=font;t.text=GuxuDisplayText.Normalize(value);t.fontSize=size;t.color=color??cream;t.alignment=align;t.raycastTarget=false;t.horizontalOverflow=HorizontalWrapMode.Wrap;t.verticalOverflow=VerticalWrapMode.Truncate;return t;}
 Button Btn(string value,Transform p,float x,float y,float w,float h,Action action,string asset=null,Color? color=null,int size=26){var im=Img(asset??"",p,x,y,w,h,color??(asset==null?new Color(.15f,.38f,.35f,.94f):Color.white));im.name=value.Length>0?value:"Button";if(value.Length>0&&asset==null&&im.color.a>.05f){var art=PageArt("button-flat");if(art){im.sprite=art;im.type=Image.Type.Sliced;if(RedesignedPage)im.pixelsPerUnitMultiplier=art.rect.height/h;im.color=color.HasValue?Color.Lerp(Color.white,color.Value,.35f):Color.white;}}im.raycastTarget=true;var b=im.gameObject.AddComponent<Button>();b.targetGraphic=im;b.onClick.AddListener(()=>action());if(value.Length>0)Txt(value,im.transform,0,0,w,h,size);return b;}
 OriginalAnimation Anim(string name,string clip,Transform p,float x,float y,float w,float h,bool loop=true){var r=Rect(name,p,x,y,w,h);r.gameObject.AddComponent<RawImage>().raycastTarget=false;var a=r.gameObject.AddComponent<OriginalAnimation>();a.Load(name,clip,loop);return a;}
 void Clear(){fitExpandedPage=null;screenVersion++;if(toastPanel!=null)Destroy(toastPanel.gameObject);if(toast!=null)Destroy(toast.gameObject);if(view!=null){view.gameObject.SetActive(false);Destroy(view.gameObject);}CloseModal();view=Rect("Screen",root,0,0,750,1334);FitPage(view);toast=null;toastPanel=null;}
 void FitPage(RectTransform page){if(page==view&&fitExpandedPage!=null){fitExpandedPage();return;}Canvas.ForceUpdateCanvases();float scale=Mathf.Min(root.rect.width/750f,root.rect.height/1334f);page.anchorMin=page.anchorMax=new Vector2(.5f,.5f);page.pivot=new Vector2(0,1);page.anchoredPosition=new Vector2(-375,667)*scale;page.localScale=Vector3.one*scale;if(page==modal)FitModalShade();}
 void FitModalShade(){if(!modal)return;var shade=modal.Find("Full Canvas Modal Shade") as RectTransform;if(shade)shade.sizeDelta=root.rect.size/modal.localScale.x;}
 void RefreshViewportLayout(){
  if(!root||(fittedScreenWidth==Screen.width&&fittedScreenHeight==Screen.height))return;
  fittedScreenWidth=Screen.width;fittedScreenHeight=Screen.height;
  if(view)FitPage(view);if(modal)FitPage(modal);if(toastPanel){FitPage(toastPanel);FitGemFeedback();}
 }
 Color Grade(int q)=>GuxuAnimation.QualityColors[Mathf.Clamp(q-1,0,4)];
 Image ItemCell(Sprite item,Transform p,float x,float y,float size,int quality=1){
  var cell=Rect("Item Cell",p,x,y,size,size);var background=Art(GuxuItemIcons.Get("slot-background"),cell,0,0,size,size,false);background.name="Item Slot Background";
  var icon=Art(item,cell,3,3,size-6,size-6);Art(GuxuUiArt.QualityFrame(quality),cell,0,0,size,size,false).name="Item Quality Frame";return icon;
 }
 void GearCard(Gear g,Transform p,float x,float y,float w,float h,Action action=null){
  var r=Rect("Gear",p,x,y,w,h);float size=Mathf.Min(w,h);var icon=ItemCell(GuxuUiArt.Item(g.slot),r,(w-size)*.5f,0,size,g.quality);icon.transform.parent.Find("Item Slot Background").name="Gear Quality Background";
  Img("",r,w-56,5,50,27,new Color(.02f,.04f,.05f,.82f));Txt(g.level+"阶",r,w-56,4,50,29,21,cream).name="Gear Tier";
  if(action!=null){var hit=r.gameObject.AddComponent<Image>();hit.color=Color.clear;var b=r.gameObject.AddComponent<Button>();b.targetGraphic=hit;b.onClick.AddListener(()=>action());}
 }
 RectTransform OpenModal(string title,float height=640){
  CloseModal();modal=Rect("Modal",root,0,0,750,1334);FitPage(modal);var shade=Img("",modal,0,0,750,1334,new Color(.01f,.05f,.07f,.83f));shade.name="Full Canvas Modal Shade";shade.raycastTarget=true;shade.rectTransform.anchorMin=shade.rectTransform.anchorMax=shade.rectTransform.pivot=new Vector2(.5f,.5f);shade.rectTransform.anchoredPosition=Vector2.zero;FitModalShade();
  float y=(1334-height)*.5f;var panel=Rect(title,modal,35,y,680,height);
  var art=Art(GuxuUiArt.Optional("ui23-panel-modal",true)??GuxuUiArt.ModalPanel,panel,0,0,680,height,false);art.name="Modal Paper";art.pixelsPerUnitMultiplier=2.5f*art.sprite.rect.width/1024f;
  Txt(title,panel,55,25,515,54,32,ink,TextAnchor.MiddleLeft).name="Modal Header";Img("",panel,45,88,590,2,new Color(.44f,.39f,.25f,.5f)).name="Modal Separator";
  var close=Btn("×",panel,580,27,55,55,CloseModal,null,new Color(.24f,.34f,.31f),32);close.name="Close Modal";UiFeedback.Pop(panel);return panel;
 }
 public void CloseModal(){RestorePrepSource();if(modal!=null){modal.gameObject.SetActive(false);Destroy(modal.gameObject);modal=null;}}
 void Collection(string title,string body){if(busy)return;auto=false;var p=OpenModal(title,380);Txt(body,p,40,105,600,150,28,ink);Btn("返回修行",p,190,280,300,60,CloseModal,null);}

 string Describe(Gear g)=>$"{quality[Mathf.Clamp(g.quality,1,5)]} · {Rules.GearName(g)}  {g.level}阶\n攻击力 +{g.attack}    生命值 +{g.hp}\n防御力 +{g.defense}    敏捷度 +{g.speed}\n词条：{GameRules.AffixNames[Mathf.Clamp(g.affix,0,6)]} · 遗域等级 {g.lootBand+1}\n槽位锻造 +{Rules.Save.forgeLevels[g.slot]}";
 public void Chop(){if(!CanPlay||busy||battle||modal!=null)return;if(Rules.Save.pending!=null){Notify("请先装备或分解当前遗宝");return;}if(Rules.Save.peaches<=0){auto=false;Notify("破封符不足，完成冒险可获得破封符");return;}if(!Rules.Chop())return;/* Tutorial weapon retains its rolled tier with weapon-slot stat weights. */if(PlayerPrefs.GetInt(FirstUnsealStoryKey,0)==0){Rules.Save.pending.slot=0;EquipmentTiers.Apply(Rules.Save.pending);}busy=true;Rules.Store();RefreshHomeBalances();StartCoroutine(ChopAnimation());}
 public void ResolveDrop(bool replace){if(!CanPlay)return;var pending=Rules.Save.pending;int before=Rules.Save.coins,powerBefore=Rules.Attributes.Power;if(!Rules.Resolve(replace))return;int reward=Rules.Save.coins-before,powerDelta=Rules.Attributes.Power-powerBefore;Rules.Store();resolvingDrop=true;Home();if(pending!=null)StartCoroutine(FinalRelicFeedback(pending,replace));StartCoroutine(ResolveDropPresentation(before,reward));Notify(replace?"装备替换成功 · 旧装备已分解":"装备已分解 · 灵尘 +"+reward);ShowPowerChange(powerDelta);}
 void ShowPowerChange(int delta){
  if(delta==0||seeker==null)return;
  var r=seeker.rectTransform;
  var label=Txt("基础战力 "+(delta>0?"+":"")+delta,view,r.anchoredPosition.x+r.sizeDelta.x*.36f,-r.anchoredPosition.y-22,240,46,32,delta>0?new Color(.42f,1f,.55f):new Color(1f,.37f,.30f),TextAnchor.MiddleLeft);
  label.name="Combat Power Change";label.horizontalOverflow=HorizontalWrapMode.Overflow;
  var outline=label.gameObject.AddComponent<Outline>();outline.effectColor=new Color(.04f,.12f,.15f,.95f);outline.effectDistance=new Vector2(1.5f,-1.5f);
  UiFeedback.Float(label,delta>0?64:-48,1.4f);
 }
 IEnumerator ResolveDropPresentation(int before,int reward){
  try{yield return CollectSpiritStones(before,reward);}
  finally{resolvingDrop=false;busy=false;nextAuto=Time.time+.25f;if(view)RefreshSealPresentation();}
 }
 IEnumerator CollectSpiritStones(int before,int reward){int version=screenVersion;busy=true;coinText.text=Rules.Save.coins.ToString();var stones=new Image[10];Vector2 start=view.InverseTransformPoint(ancientStone.rectTransform.TransformPoint(ancientStone.rectTransform.rect.center));Vector2 destination=view.InverseTransformPoint(spiritDustIcon.rectTransform.TransformPoint(spiritDustIcon.rectTransform.rect.center));float fallTime=0;var rise=new float[10];var drop=new float[10];var spread=new float[10];for(int i=0;i<stones.Length;i++){rise[i]=30+(i*17%51);drop[i]=30+(i*11%31);spread[i]=(i%2==0?-1:1)*(30+i*29%81);fallTime=Mathf.Max(fallTime,SpiritStoneMotion.Duration(rise[i],drop[i]));stones[i]=Art(GuxuUiArt.Navigation(GuxuUiArt.Icon.SpiritDust),view,start.x,-start.y,32,32);stones[i].rectTransform.pivot=new Vector2(.5f,.5f);stones[i].name="Spirit Stone Drop";}
 float duration=fallTime+.45f;
 for(float elapsed=0;elapsed<duration;elapsed+=Time.deltaTime){if(version!=screenVersion)yield break;for(int i=0;i<stones.Length;i++){var r=stones[i].rectTransform;if(elapsed<fallTime){r.anchoredPosition=start+SpiritStoneMotion.Sample(elapsed,spread[i],rise[i],drop[i]);stones[i].color=new Color(1,1,1,Mathf.Clamp01(elapsed/.15f));}else{float fly=elapsed-fallTime;var landing=start+new Vector2(spread[i],-drop[i]);r.anchoredPosition=Vector2.Lerp(landing,destination,SpiritStoneMotion.EaseIn(fly/.3f));stones[i].color=new Color(1,1,1,1-Mathf.Clamp01((fly-.3f)/.15f));}}
 float progress=Mathf.Clamp01((elapsed-fallTime)/.3f);coinText.text=Rules.Save.coins.ToString();coinText.color=progress>0?gold:cream;yield return null;}
 if(version!=screenVersion)yield break;foreach(var stone in stones)if(stone!=null)Destroy(stone.gameObject);coinText.text=Rules.Save.coins.ToString();coinText.color=cream;UiFeedback.Float(Txt("+"+reward,view,510,100,180,42,29,gold),45,.85f);busy=false;nextAuto=Time.time+.25f;}
  void Breakthrough(){if(busy)return;int powerBefore=Rules.Attributes.Power;if(Rules.Breakthrough()){Rules.Store();Home();StartCoroutine(FinalRelicFeedback(Rules.Save.gear[0],true));Notify("渡劫成功！境界提升");ShowPowerChange(Rules.Attributes.Power-powerBefore);}else Notify(Rules.BreakthroughRequirement);}

 public void Skins(){if(!CanPlay||busy)return;auto=false;Clear();Img("Original/newGui/Character/bg/bg",view,0,0,750,1334);Txt("幻 境 阁",view,160,45,430,80,47,gold);Btn("返回",view,25,42,120,62,Home);string[] names={"少年秦羽","新生","岚羽"};string selected=Rules.Save.skin==0?"hero":Rules.Save.skin==1?"pig":"skinPreview";Anim(selected,Rules.Save.skin==2?"stand":"idle",view,70,160,610,600);Txt(names[Rules.Save.skin],view,120,715,510,63,35,gold);Txt("本地外观试穿",view,150,780,450,40,25,cream);for(int i=0;i<3;i++){int n=i;var card=Btn("",view,37+i*235,875,208,265,()=>{Rules.Save.skin=n;Rules.Store();Skins();},null,new Color(.17f,.31f,.29f,.9f));Anim(i==0?"hero":i==1?"pig":"skinPreview",i==2?"stand":"idle",card.transform,5,0,198,206);Txt(names[i],card.transform,4,207,200,49,26,n==Rules.Save.skin?gold:cream);}Btn("使用此外观",view,170,1195,410,80,()=>{Rules.Store();Home();},null);}
 public void ToggleBattleSpeed(){fightSpeed=fightSpeed%3+1;if(hero!=null)hero.Speed=fightSpeed;if(enemy!=null)enemy.Speed=fightSpeed;var button=view.Find("Battle Speed");if(button!=null)button.GetComponentInChildren<Text>().text="×"+fightSpeed+"  加速";}
 void FitGemFeedback(){if(ExpansionScreen!="gems"||!gemStage||!toastPanel)return;var panel=toastPanel.Find("Status Toast") as RectTransform;if(panel)panel.position=gemStage.TransformPoint(new Vector3(45,-4,0));}
 void Notify(string message){if(toastPanel!=null)Destroy(toastPanel.gameObject);toastPanel=Rect("Status Toast Layer",root,0,0,750,1334);FitPage(toastPanel);bool gems=ExpansionScreen=="gems"&&gemStage;float height=gems?50:100;var panel=Frame("Status Toast",toastPanel,45,1090,660,height,new Color(.02f,.08f,.1f,.97f));toast=Txt(message,panel,16,8,628,height-16,gems?22:26,cream);if(gems){toast.resizeTextForBestFit=true;toast.resizeTextMinSize=16;toast.resizeTextMaxSize=22;}FitGemFeedback();Destroy(toastPanel.gameObject,2.5f);}
 void OnApplicationPause(bool pause){if(pause&&Rules!=null)Rules.Store();}
 void OnApplicationQuit(){if(Rules!=null)Rules.Store();}
}
}








