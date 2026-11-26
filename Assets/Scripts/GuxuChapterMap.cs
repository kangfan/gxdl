using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace Gxdl {
public partial class GxdlGame {
 int chapterRegion,chapterProgress;
 readonly int[] chapterSelections=new int[10];
 RectTransform chapterViewport,chapterDetail,chapterBackdrop;
 ScrollRect chapterScroll;
 Material chapterLockedMaterial;
 readonly Vector2[] chapterPlatforms={new Vector2(152,73),new Vector2(375,121),new Vector2(185,210),new Vector2(542,273),new Vector2(265,371),new Vector2(541,449),new Vector2(336,550)};
 readonly int[] chapterLaterPlatforms={0,1,4,5,6};
 const float ChapterContentHeight=2800;

 Vector2 ChapterPoint(int index){
  int tile=index<7?0:1+(index-7)/5,slot=index<7?index:chapterLaterPlatforms[(index-7)%5];var point=chapterPlatforms[slot];point.y+=45+tile*720;return point;
 }
 string ChapterKind(GuxuStage stage)=>stage.boss?"首领":stage.room%5==0?"精英":stage.waves>1?"双波试炼":"遗域探索";
 void FitChapterPage(){
  Canvas.ForceUpdateCanvases();float scale=Mathf.Min(root.rect.width/750f,root.rect.height/1334f),height=root.rect.height/scale;
  view.sizeDelta=new Vector2(750,height);view.anchorMin=view.anchorMax=new Vector2(.5f,.5f);view.pivot=new Vector2(0,1);view.anchoredPosition=new Vector2(-375,height*.5f)*scale;view.localScale=Vector3.one*scale;
  if(chapterBackdrop)chapterBackdrop.sizeDelta=new Vector2(750,height);
  if(chapterViewport)chapterViewport.sizeDelta=new Vector2(750,height-624);
  if(chapterDetail)chapterDetail.anchoredPosition=new Vector2(0,-(height-394));
 }
 // 奖励图标口径与旧 RewardItems 一致：0符 1尘 3铁 4悟 2修为 5石，-1 为宝石。
 Gxdl.Modules.DifficultyView.RewardCell[] RewardCells(GuxuReward reward){
  var labels=reward.ToString().Split(new[]{"  "},StringSplitOptions.RemoveEmptyEntries);
  int[] counts={reward.seals,reward.dust,reward.ore,reward.insight,reward.exp,reward.stones,reward.gems},icons={0,1,3,4,2,5,-1};var cells=new System.Collections.Generic.List<Gxdl.Modules.DifficultyView.RewardCell>();
  for(int i=0;i<counts.Length;i++)if(counts[i]>0)cells.Add(new Gxdl.Modules.DifficultyView.RewardCell{Label=cells.Count<labels.Length?labels[cells.Count]:null,IconKind=icons[i],GemIndex=icons[i]<0?reward.gemIndex+1:0});
  return cells.ToArray();
 }
 Gxdl.Modules.ChapterMapView.ChapterNodeState ChapterNode(int id){
  var stage=Stages.stages[id-1];bool elite=stage.room%5==0&&!stage.boss,cleared=Rules.Save.firstClears[stage.id-1],open=GuxuStageRules.CanEnter(stage,Rules.Save),current=stage.id==Rules.Save.stage;
  // 旧节点文字：普通关固定显示关号，精英/首领与状态同放在标签里。
  string label=stage.boss?"第"+stage.id+"关 · 首领":elite?"第"+stage.id+"关 · 精英":cleared?"已通关":current?"当前":stage.waves>1?"双波试炼":open?"可挑战":"未解锁";
  return new Gxdl.Modules.ChapterMapView.ChapterNodeState{Label=label,Unlocked=open,Cleared=cleared,Current=current};
 }
 Gxdl.Modules.ChapterMapView.ChapterDetail ChapterDetailOf(int id){
  var stage=Stages.stages[id-1];bool open=GuxuStageRules.CanEnter(stage,Rules.Save),first=!Rules.Save.firstClears[stage.id-1];
  return new Gxdl.Modules.ChapterMapView.ChapterDetail{Title="第"+stage.id+"关 · "+ChapterKind(stage),Enemy=stage.guardianName+" · "+stage.waves+"波敌人",
   RewardState=open?(first?"首通奖励":"重访奖励 · 首通已领取"):"通关第"+(stage.id-1)+"关后解锁",
   Rewards=GuxuStageRules.Reward(stage,first).ToString().Split(new[]{"  "},StringSplitOptions.RemoveEmptyEntries),
   ChallengeLabel=!open?"尚未解锁":(first?"挑战第":"重访第")+stage.id+"关",CanEnter=open};
 }
 // prefab 版章节地图。宿主未就绪或 prefab 缺失时返回 false，回退旧构建。
 bool ShowChapterMapPrefab(int regionIndex){
  if(Gxdl.Core.GameBootstrap.Instance==null||Gxdl.Core.GameBootstrap.Instance.UiModules==null)return false;
  CloseScreen(); // 先收起残留的 prefab 页，避免两页同时在屏。
  var page=OpenScreen<Gxdl.Modules.ChapterMapView>(Gxdl.Modules.UiScreens.ChapterMap);if(page==null)return false;
  int first=regionIndex*20+1,count=Mathf.Clamp(Rules.HighestClear-regionIndex*20,0,20);var region=Stages.regions[regionIndex];
  bool claimed=Rules.Save.chapterChests[regionIndex],ready=Rules.CanClaimChapter(regionIndex);
  var nodes=new Gxdl.Modules.ChapterMapView.ChapterNodeState[20];for(int i=0;i<20;i++)nodes[i]=ChapterNode(first+i);
  if(page.Back){page.Back.onClick.RemoveAllListeners();page.Back.onClick.AddListener(ShowWorldMap);}
  if(page.Chest){page.Chest.onClick.RemoveAllListeners();page.Chest.onClick.AddListener(()=>{
   if(Rules.ClaimChapter(regionIndex)){string id=Rules.LastRewardReceipt.id;Rules.Store();ShowChapter(regionIndex);ShowRewardReceipt(id);}
   else{ShowChapter(regionIndex);Notify("通关本章后可领取一次");}
  });}
  page.BindNodeCallbacks(SelectChapterStage);page.BindChallenge(()=>ShowBattlePrep(Stages.stages[chapterSelections[regionIndex]-1]));page.BindGrowth(ShowGrowthHub);
  page.Apply(regionIndex,new Gxdl.Modules.ChapterMapView.ChapterHeader{Name=region.name,Story=region.story,Progress="已通关 "+count+" / 20",ClearedCount=count,ChestReady=ready,ChestState=claimed?"已领取":ready?"可领取":"宝匣锁定"},nodes);
  page.SetTerrainTint(regionIndex==0?Color.white:EncounterColors[regionIndex]);page.SetHintVisible(true);
  page.Select(chapterSelections[regionIndex]-first);page.Focus(chapterSelections[regionIndex]-first);page.ApplyDetail(ChapterDetailOf(chapterSelections[regionIndex]));
  return true;
 }
 void DrawChapterMap(int regionIndex){
  auto=false;battle=false;busy=false;Clear();ExpansionScreen="chapter";Rules.EnsureExpansion();
  chapterRegion=regionIndex;fitExpandedPage=FitChapterPage;chapterViewport=null;chapterDetail=null;chapterBackdrop=null;chapterScroll=null;FitChapterPage();
  int first=regionIndex*20+1,last=first+19;
  if(chapterSelections[regionIndex]<first||chapterSelections[regionIndex]>last||chapterProgress!=Rules.Save.stage)chapterSelections[regionIndex]=Mathf.Clamp(Rules.Save.stage,first,last);
  chapterProgress=Rules.Save.stage;
  if(ShowChapterMapPrefab(regionIndex))return;
  var scenery=GuxuUiArt.Optional("chapter-map-segment");
  chapterBackdrop=Img("",view,0,0,750,view.rect.height,new Color(.055f,.16f,.23f)).rectTransform;
  chapterBackdrop.name="Chapter Scenery";
  chapterViewport=Rect("Chapter Map Viewport",view,0,230,750,view.rect.height-624);
  var hit=chapterViewport.gameObject.AddComponent<Image>();hit.color=Color.clear;hit.raycastTarget=true;chapterViewport.gameObject.AddComponent<RectMask2D>();
  var content=Rect("Chapter Map Content",chapterViewport,0,0,750,ChapterContentHeight);
  chapterScroll=chapterViewport.gameObject.AddComponent<ScrollRect>();chapterScroll.viewport=chapterViewport;chapterScroll.content=content;chapterScroll.horizontal=false;chapterScroll.vertical=true;chapterScroll.movementType=ScrollRect.MovementType.Clamped;chapterScroll.scrollSensitivity=90;chapterScroll.decelerationRate=.06f;
  for(int tile=0;tile<4;tile++){var terrain=Art(scenery,content,0,45+tile*720,750,720,false);terrain.name="Chapter Terrain "+tile;terrain.color=chapterRegion==0?Color.white:Color.Lerp(Color.white,EncounterColors[chapterRegion],.22f);}
  if(scenery)for(int tile=0;tile<4;tile++){
   var mist=Rect("Chapter Cloud Transition",content,0,tile==0?0:45+tile*720-80,750,tile==0?90:160).gameObject.AddComponent<GuxuChapterMist>();mist.Source=scenery.texture;mist.raycastTarget=false;
  }
  for(int i=0;i<20;i++)DrawChapterNode(content,Stages.stages[first+i-1],i);
  DrawChapterHeader();
  chapterDetail=Rect("Chapter Selection Detail",view,0,view.rect.height-394,750,394);DrawChapterSelection();
  var hint=Txt("滑动探索  ▾",view,260,view.rect.height-444,230,34,24,new Color(.12f,.25f,.28f));hint.name="Chapter Scroll Hint";hint.rectTransform.anchorMin=hint.rectTransform.anchorMax=Vector2.zero;hint.rectTransform.anchoredPosition=new Vector2(260,444);var hintShadow=hint.gameObject.AddComponent<Shadow>();hintShadow.effectColor=Color.white;hintShadow.effectDistance=new Vector2(1,-1);
  chapterScroll.onValueChanged.AddListener(_=>hint.gameObject.SetActive(chapterScroll.content.anchoredPosition.y<ChapterContentHeight-chapterViewport.rect.height-4));
  FocusChapterSelection();
  hint.gameObject.SetActive(chapterScroll.content.anchoredPosition.y<ChapterContentHeight-chapterViewport.rect.height-4);
 }
 void DrawChapterHeader(){
  var r=Stages.regions[chapterRegion];int count=Mathf.Clamp(Rules.HighestClear-chapterRegion*20,0,20);
  var headerColor=new Color(.035f,.13f,.20f);Img("",view,0,0,750,230,chapterRegion==0?headerColor:Color.Lerp(headerColor,EncounterColors[chapterRegion],.4f));
  SealButton("‹ 十域地图",view,16,22,175,52,ShowWorldMap).name="Chapter Back";
  Txt(r.name,view,205,10,520,64,43,new Color(1f,.87f,.57f)).name="Chapter Name";
  Txt(r.story,view,100,76,600,38,26,cream);
  Art(GuxuUiArt.SecondaryButton,view,250,115,250,42,false);
  RewardHistoryButton(view,"main",16,115,210);
  Txt("已通关 "+count+" / 20",view,250,114,250,42,27,cream).name="Chapter Progress";
  Img("",view,70,194,537,3,new Color(.41f,.55f,.56f));Img("",view,70,194,537*count/20f,3,new Color(1f,.82f,.45f));
  for(int i=0;i<4;i++){
   int room=(i+1)*5;float x=70+537*room/20f-41;bool cleared=count>=room;
   var icon=Art(GuxuUiArt.Optional(i%2==0?"chapter-node-elite":"chapter-node-boss"),view,x+14,150,54,54);icon.color=cleared?new Color(1f,.91f,.63f):new Color(.52f,.67f,.69f);
   Txt(room+"关",view,x-5,199,92,30,23,cleared?gold:cream);
  }
  bool claimed=Rules.Save.chapterChests[chapterRegion],ready=Rules.CanClaimChapter(chapterRegion);
  var chest=Btn("",view,653,145,82,79,()=>{if(Rules.ClaimChapter(chapterRegion)){string id=Rules.LastRewardReceipt.id;Rules.Store();ShowChapter(chapterRegion);ShowRewardReceipt(id);}else{ShowChapter(chapterRegion);Notify("通关本章后可领取一次");}},null,Color.clear);
  chest.name=claimed?"已领取":"领取章节宝匣";chest.interactable=ready;
  var chestIcon=Art(GuxuItemIcons.Get("resource-chest"),chest.transform,12,0,58,58);chestIcon.name="Chapter Chest Icon";chestIcon.color=ready?Color.white:new Color(.55f,.66f,.67f);
  if(!ready)ChapterStatus(chest.transform,56,29,claimed);
  Txt(claimed?"已领取":ready?"可领取":"宝匣锁定",chest.transform,-13,56,108,26,21,ready?gold:cream).name="Chapter Chest State";
 }
 void DrawChapterNode(Transform parent,GuxuStage stage,int index){
  var point=ChapterPoint(index);bool elite=stage.room%5==0&&!stage.boss,cleared=Rules.Save.firstClears[stage.id-1],open=GuxuStageRules.CanEnter(stage,Rules.Save),current=stage.id==Rules.Save.stage,selected=stage.id==chapterSelections[chapterRegion];
  float width=stage.boss?210:elite?146:120,height=stage.boss?210:elite?183:120,hitWidth=stage.boss?240:elite?224:158;
  if(current){var halo=Rect("Chapter Current Halo",parent,point.x-135,point.y-49,270,106).gameObject.AddComponent<GuxuSoftVfx>();halo.color=new Color(1f,.74f,.20f,.85f);halo.raycastTarget=false;}
  var button=Btn("",parent,point.x-hitWidth*.5f,point.y+8-height,hitWidth,height+46,()=>SelectChapterStage(stage.id),null,Color.clear);button.name="Stage "+stage.id;button.transition=Selectable.Transition.None;
  var selectedArt=GuxuUiArt.Optional("state-selected");if(selectedArt){float glowWidth=elite||stage.boss?270:180;var selection=Art(selectedArt,button.transform,(hitWidth-glowWidth)*.5f,height-48,glowWidth,88,false);selection.name="Chapter Selection Glow";selection.gameObject.SetActive(selected);}
  var art=Art(GuxuUiArt.Optional(stage.boss?"chapter-node-boss":elite?"chapter-node-elite":"chapter-node-normal"),button.transform,(hitWidth-width)*.5f,0,width,height);
  art.name="Chapter Node Art "+stage.id;art.color=!open?new Color(.68f,.77f,.80f):cleared?new Color(.75f,1f,.93f):Color.white;
  if(!open){if(!chapterLockedMaterial)chapterLockedMaterial=new Material(Resources.Load<Shader>("Game/Guxu/ChapterStone"));art.material=chapterLockedMaterial;}
  var outline=art.gameObject.AddComponent<Outline>();outline.effectColor=new Color(1f,.78f,.31f,.85f);outline.effectDistance=new Vector2(3,3);outline.enabled=selected;
  if(!elite&&!stage.boss){Txt("第"+stage.id+"关",button.transform,0,21,hitWidth,40,30,open?cream:new Color(.85f,.9f,.91f));if(cleared||!open)ChapterStatus(button.transform,hitWidth*.5f-16,57,cleared);}
  string label=stage.boss?"第"+stage.id+"关 · 首领":elite?"第"+stage.id+"关 · 精英":cleared?"已通关":current?"当前":stage.waves>1?"双波试炼":open?"可挑战":"未解锁";
  if(elite||stage.boss)Art(GuxuUiArt.SecondaryButton,button.transform,0,height-5,hitWidth,39,false).color=open?Color.white:new Color(.65f,.73f,.75f);
  Txt(label,button.transform,elite||stage.boss?4:19,elite||stage.boss?height-5:height-28,elite||stage.boss?hitWidth-8:120,elite||stage.boss?39:28,elite||stage.boss?27:21,cream).name="Chapter Node Label "+stage.id;
  if((elite||stage.boss)&&(cleared||!open))ChapterStatus(button.transform,hitWidth-35,height-43,cleared);
  if(current){var pointer=GuxuUiArt.Optional("state-current");if(pointer){Art(pointer,button.transform,-48,height-100,42,58);Txt("当\n前",button.transform,-46,height-96,38,48,23,new Color(.30f,.15f,.025f)).name="Chapter Current Marker";}else{Art(GuxuUiArt.PrimaryButton,button.transform,-32,height-97,103,47,false);Txt("当前",button.transform,-27,height-94,93,41,26,new Color(.30f,.15f,.025f)).name="Chapter Current Marker";}}
 }
 void ChapterStatus(Transform parent,float x,float y,bool cleared){
  var sprite=GuxuUiArt.Optional(cleared?"state-cleared":"state-lock");if(sprite){Art(sprite,parent,x,y,cleared?32:28,35).name=cleared?"Chapter Cleared Mark":"Chapter Lock Mark";return;}
  Color color=cleared?new Color(1f,.93f,.65f):new Color(.93f,.87f,.69f);
  if(cleared){var a=Img("",parent,x,y+13,12,4,color);a.rectTransform.localEulerAngles=new Vector3(0,0,-44);var b=Img("",parent,x+8,y+21,25,4,color);b.rectTransform.localEulerAngles=new Vector3(0,0,48);return;}
  Img("",parent,x+3,y+12,23,20,color);Img("",parent,x+7,y+2,15,4,color);Img("",parent,x+7,y+5,4,9,color);Img("",parent,x+18,y+5,4,9,color);Img("",parent,x+13,y+18,3,7,new Color(.18f,.27f,.29f));
 }
 void SelectChapterStage(int id){
  if(id<=chapterRegion*20||id>(chapterRegion+1)*20)return;
  chapterSelections[chapterRegion]=id;
  var prefab=activeModule as Gxdl.Modules.ChapterMapView;
  if(prefab!=null){prefab.Select(id-chapterRegion*20-1);prefab.Focus(id-chapterRegion*20-1);prefab.ApplyDetail(ChapterDetailOf(id));return;}
  foreach(var button in chapterScroll.content.GetComponentsInChildren<Button>()){
   var outline=button.GetComponentInChildren<Outline>();if(outline)outline.enabled=button.name=="Stage "+id;
   var glow=button.transform.Find("Chapter Selection Glow");if(glow)glow.gameObject.SetActive(button.name=="Stage "+id);
  }
  DrawChapterSelection();
 }
 void FocusChapterSelection(){
  Canvas.ForceUpdateCanvases();var point=ChapterPoint(chapterSelections[chapterRegion]-chapterRegion*20-1);
  chapterScroll.StopMovement();chapterScroll.content.anchoredPosition=new Vector2(0,Mathf.Clamp(point.y-chapterViewport.rect.height*.68f,0,ChapterContentHeight-chapterViewport.rect.height));
 }
 void DrawChapterSelection(){
  foreach(Transform child in chapterDetail){child.gameObject.SetActive(false);Destroy(child.gameObject);}
  var stage=Stages.stages[chapterSelections[chapterRegion]-1];bool open=GuxuStageRules.CanEnter(stage,Rules.Save),first=!Rules.Save.firstClears[stage.id-1];
  var surface=Art(GuxuUiArt.Optional("ui23-panel-modal",true)??GuxuUiArt.ModalPanel,chapterDetail,16,0,718,384,false);surface.pixelsPerUnitMultiplier=2;
  Art(GuxuUiArt.SecondaryButton,chapterDetail,177,-12,396,62,false);
  Txt("第"+stage.id+"关 · "+ChapterKind(stage),chapterDetail,182,-10,386,57,33,cream).name="Chapter Selected Stage";
  Txt(stage.guardianName+" · "+stage.waves+"波敌人",chapterDetail,45,59,660,39,27,ink).name="Chapter Selected Enemy";
  Txt(open?(first?"首通奖励":"重访奖励 · 首通已领取"):"通关第"+(stage.id-1)+"关后解锁",chapterDetail,45,102,660,31,24,ink).name="Chapter Reward State";
  RewardItems(GuxuStageRules.Reward(stage,first),chapterDetail,65,139,636,24,ink,"Chapter Reward",true);
  var challenge=SealButton(!open?"尚未解锁":(first?"挑战第":"重访第")+stage.id+"关",chapterDetail,238,282,444,80,()=>ShowBattlePrep(stage),true);challenge.name="Chapter Challenge";challenge.interactable=open;
  var growth=Btn("修行养成 ›",chapterDetail,42,290,174,64,ShowGrowthHub,null,Color.clear,26);growth.GetComponentInChildren<Text>().color=ink;growth.name="Chapter Growth";
 }
 void RewardItems(GuxuReward reward,Transform parent,float x,float y,float width,int fontSize=24,Color? color=null,string prefix="Reward",bool balanceFour=false){
  var labels=reward.ToString().Split(new[]{"  "},StringSplitOptions.RemoveEmptyEntries);
  int columns=balanceFour&&labels.Length==4?2:3;
  int[] counts={reward.seals,reward.dust,reward.ore,reward.insight,reward.exp,reward.stones,reward.gems},icons={0,1,3,4,2,5,-1};int item=0;
  for(int i=0;i<counts.Length;i++){
   if(counts[i]<=0)continue;float left=x+item%columns*width/columns,top=y+item/columns*57;
   ItemCell(icons[i]<0?GuxuItemIcons.Gem(reward.gemIndex+1):GuxuUiArt.BattlePrepReward(icons[i]),parent,left,top,47).name=prefix+" Icon "+item;
   var value=Txt(labels[item],parent,left+50,top,width/columns-50,47,fontSize,color??cream,TextAnchor.MiddleLeft);value.name=prefix+" Value "+item;value.verticalOverflow=VerticalWrapMode.Overflow;item++;
  }
 }
}
}
