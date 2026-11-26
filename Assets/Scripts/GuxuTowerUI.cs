using System;
using UnityEngine;
using UnityEngine.UI;

namespace Gxdl {
public partial class GxdlGame {
 bool towerFromDungeons;
 int towerPage=-1,towerSelection,towerKnownProgress;RectTransform towerViewport,towerDetail,towerIndex;ScrollRect towerScroll;Image towerBackdrop;
 const float TowerFloorStep=190,TowerContentHeight=2012;
 void FitTowerPage(){
  Canvas.ForceUpdateCanvases();float scale=Mathf.Min(root.rect.width/750f,root.rect.height/1334f),height=root.rect.height/scale;
  view.sizeDelta=new Vector2(750,height);view.anchorMin=view.anchorMax=new Vector2(.5f,.5f);view.pivot=new Vector2(0,1);view.anchoredPosition=new Vector2(-375,height*.5f)*scale;view.localScale=Vector3.one*scale;
  if(towerBackdrop){float width=750*height/1334;towerBackdrop.rectTransform.sizeDelta=new Vector2(width,height);towerBackdrop.rectTransform.anchoredPosition=new Vector2((750-width)*.5f,0);}
  if(towerViewport)towerViewport.sizeDelta=new Vector2(575,height-605);
  if(towerDetail)towerDetail.anchoredPosition=new Vector2(0,-(height-425));
  if(towerIndex)towerIndex.anchoredPosition=new Vector2(584,-(205+Mathf.Max(0,height-1334)*.25f));
 }
 // prefab 版古塔。宿主未就绪或 prefab 缺失时返回 false，回退旧构建。
 bool ShowTowerPrefab(){
  var bootstrap=Gxdl.Core.GameBootstrap.Instance;if(bootstrap==null||bootstrap.UiModules==null)return false;
  auto=false;battle=false;ExpansionScreen="tower";
  var page=OpenScreen<Gxdl.Modules.TowerView>(Gxdl.Modules.UiScreens.Tower);if(page==null)return false;
  int selection=Mathf.Clamp(towerSelection,towerPage*10+1,towerPage*10+10),next=towerPage<9?(towerPage+1)*10+1:0;
  if(page.Back){page.Back.onClick.RemoveAllListeners();page.Back.onClick.AddListener(()=>{if(towerFromDungeons)ShowDungeons();else ShowWorldMap();});}
  if(page.RewardHistory){page.RewardHistory.onClick.RemoveAllListeners();page.RewardHistory.onClick.AddListener(()=>ShowRewardHistory("tower",0));}
  page.BindFloorCallbacks(floor=>{SelectTowerFloor(floor);});
  // 索引跳转沿用旧 FocusTowerFloor；翻段调用 ShowTower 复用本分支。
  page.BindMarkerCallbacks(floor=>{SelectTowerFloor(floor);page.ScrollToFloor(floor-(towerPage*10+1));});
  page.BindSegment(
   ()=>{if(towerPage<=0)return;towerPage--;towerSelection=towerPage*10+1;ShowTower();},
   ()=>{if(towerPage>=9)return;towerPage++;towerSelection=towerPage*10+1;ShowTower();});
  if(page.Growth)page.Growth.onClick.AddListener(ShowGrowthHub);
  if(page.Challenge){page.Challenge.onClick.RemoveAllListeners();page.Challenge.onClick.AddListener(()=>ShowBattlePrep(Stages.tower[Mathf.Clamp(towerSelection,1,100)-1]));}
  page.SetHeader(towerFromDungeons?"‹ 返回秘境":"‹ 返回寻墟","通天古塔","已登临 "+(Rules.Save.towerFloor-1)+" / 100 层",Rules.Save.towerFloor>100?"百层已通关":"当前目标 · 第 "+Rules.Save.towerFloor+" 层","滑动攀登");
  page.SetFloors(TowerFloorCells(selection));page.SetMarkers(TowerMarkerCells());
  page.ApplyFloorSkin(GuxuUiArt.Optional("tower-tier"));
  page.SetSegment((towerPage*10+1)+"—"+(towerPage*10+10)+"层",towerPage>0?"‹ "+((towerPage-1)*10+1)+"—"+(towerPage*10):"已到底层",towerPage>0,next>0?next+"—"+(next+9)+" ›":"已到百层",towerPage<9);
  page.SetDetail(TowerDetailCell(selection));page.ScrollToFloor(selection-(towerPage*10+1));
  // 记录本页对应的进度段，翻段后 ShowTower 不再把页码重置回当前进度。
  towerKnownProgress=Rules.Save.towerFloor;
  return true;
 }
 Gxdl.Modules.TowerView.FloorCell[] TowerFloorCells(int selection){
  var floors=new Gxdl.Modules.TowerView.FloorCell[10];
  for(int index=0;index<10;index++){
   int floor=towerPage*10+index+1;var stage=Stages.tower[floor-1];bool open=GuxuStageRules.CanEnter(stage,Rules.Save),cleared=floor<Rules.Save.towerFloor;
   floors[index]=new Gxdl.Modules.TowerView.FloorCell{Label="第 "+floor+" 层",Unlocked=open,Cleared=cleared,Selected=floor==selection,Boss=stage.boss,Target=open&&!cleared&&floor==selection};
  }
  return floors;
 }
 Gxdl.Modules.TowerView.MarkerCell[] TowerMarkerCells(){
  var markers=new Gxdl.Modules.TowerView.MarkerCell[10];
  for(int index=0;index<10;index++){
   int floor=towerPage*10+index+1;
   markers[index]=new Gxdl.Modules.TowerView.MarkerCell{Label=(floor%10==0?"◆ ":"")+floor+"层"+(floor%10==0?" Boss":""),Current=floor==Rules.Save.towerFloor};
  }
  return markers;
 }
 Gxdl.Modules.TowerView.DetailCell TowerDetailCell(int floor){
  var stage=Stages.tower[floor-1];bool open=GuxuStageRules.CanEnter(stage,Rules.Save),first=floor>=Rules.Save.towerFloor;
  string enemyPath="Game/Guxu/Expansion/Enemies/"+stage.enemyKey;bool supplied=Resources.Load<Texture2D>(enemyPath);var definition=stage.boss?GuxuBossAnimations.Find(stage.enemyKey):null;
  var portrait=definition!=null?definition.Frame(definition.idleFrame):Sprite(supplied?enemyPath:"Game/Guxu/Final/guardian");
  string condition=!open&&Rules.Save.stage<GuxuStageRules.RequiredProgress(stage)?"通关主线第"+(GuxuStageRules.RequiredProgress(stage)-1)+"关后开放":!open?"通关第"+(floor-1)+"层后可挑战":first?"每10层首领 · 首通奖励仅领取一次":"重访不再发放首通奖励";
  return new Gxdl.Modules.TowerView.DetailCell{Title="第 "+floor+" 层 · "+(!open?"尚未解锁":first?"当前目标":"已通关"),EnemyName=stage.guardianName+(stage.boss?" · Boss":""),
   Reward=first?GuxuStageRules.Reward(stage,true).ToString():"本层首通奖励已领取",Condition=condition,
   ChallengeLabel=!open?"尚未解锁":(first?"挑战第":"重访第")+floor+"层",Portrait=portrait,ChallengeEnabled=open};
 }
 public void ShowTower(){
  if(busy||!CanPlay)return;if(ExpansionScreen!="tower"&&ExpansionScreen!="battle")towerFromDungeons=ExpansionScreen=="dungeons";
  // prefab 页不依赖 towerPage 缓存，分段翻页由索引按钮直接调用本方法。
  if(towerPage<0||towerKnownProgress!=Rules.Save.towerFloor){towerSelection=Mathf.Min(100,Rules.Save.towerFloor);towerPage=(towerSelection-1)/10;}
  if(ShowTowerPrefab())return;
  DrawTowerPage(towerPage,towerSelection);
 }
 void DrawTowerPage(int page,int selection){
  auto=false;battle=false;Clear();ExpansionScreen="tower";Rules.EnsureExpansion();towerPage=Mathf.Clamp(page,0,9);towerSelection=Mathf.Clamp(selection,towerPage*10+1,towerPage*10+10);towerKnownProgress=Rules.Save.towerFloor;
  towerViewport=null;towerDetail=null;towerIndex=null;towerScroll=null;towerBackdrop=null;fitExpandedPage=FitTowerPage;FitTowerPage();towerBackdrop=Img("Game/Guxu/Final/background",view,0,0,750,1334);
  towerViewport=Rect("Tower Climb Viewport",view,0,180,575,view.rect.height-605);var hit=towerViewport.gameObject.AddComponent<Image>();hit.color=Color.clear;hit.raycastTarget=true;towerViewport.gameObject.AddComponent<RectMask2D>();
  var content=Rect("Tower Climb Content",towerViewport,0,0,575,TowerContentHeight);towerScroll=towerViewport.gameObject.AddComponent<ScrollRect>();towerScroll.viewport=towerViewport;towerScroll.content=content;towerScroll.horizontal=false;towerScroll.movementType=ScrollRect.MovementType.Clamped;towerScroll.scrollSensitivity=90;towerScroll.decelerationRate=.06f;
  for(int index=9;index>=0;index--)DrawTowerFloor(content,towerPage*10+index+1,index);
  Img("",view,0,0,750,180,new Color(.025f,.10f,.14f,.96f));SealButton(towerFromDungeons?"‹ 返回秘境":"‹ 返回寻墟",view,14,24,175,52,()=>{if(towerFromDungeons)ShowDungeons();else ShowWorldMap();}).name="Tower Back";
  Txt("通天古塔",view,200,12,490,63,43,gold);Txt("已登临 "+(Rules.Save.towerFloor-1)+" / 100 层",view,200,78,490,40,28,cream).name="Tower Progress";
  Txt(Rules.Save.towerFloor>100?"百层已通关":"当前目标 · 第 "+Rules.Save.towerFloor+" 层",view,50,130,345,35,24,gold).name="Tower Current Target";RewardHistoryButton(view,"tower",480,122,245);
  towerIndex=Rect("Tower Segment Index",view,584,205,150,650);Frame("Tower Index Surface",towerIndex,0,0,150,650,panelDark);
  for(int index=9;index>=0;index--){int floor=towerPage*10+index+1;float y=16+(9-index)*43;var marker=Btn((floor%10==0?"◆ ":"")+floor+"层"+(floor%10==0?" Boss":""),towerIndex,10,y,130,41,()=>{SelectTowerFloor(floor);FocusTowerFloor();},null,Color.clear,22);marker.name="Tower Marker "+floor;marker.GetComponentInChildren<Text>().color=floor==Rules.Save.towerFloor?gold:cream;}
  Txt("滑动攀登",towerIndex,10,455,130,33,22,cream);Txt((towerPage*10+1)+"—"+(towerPage*10+10)+"层",towerIndex,10,499,130,33,24,gold).name="Tower Segment Label";
  var previous=SealButton(towerPage>0?"‹ "+((towerPage-1)*10+1)+"—"+(towerPage*10):"已到底层",towerIndex,8,545,134,42,()=>DrawTowerPage(towerPage-1,towerPage*10));previous.name="Tower Previous Segment";previous.interactable=towerPage>0;
  var next=SealButton(towerPage<9?((towerPage+1)*10+1)+"—"+((towerPage+2)*10)+" ›":"已到百层",towerIndex,8,594,134,42,()=>DrawTowerPage(towerPage+1,towerPage*10+11));next.name="Tower Next Segment";next.interactable=towerPage<9;
  towerDetail=Rect("Tower Selection Detail",view,0,view.rect.height-425,750,425);DrawTowerSelection();FitTowerPage();FocusTowerFloor();
 }
 void DrawTowerFloor(Transform parent,int floor,int index){
  var stage=Stages.tower[floor-1];bool open=GuxuStageRules.CanEnter(stage,Rules.Save),cleared=floor<Rules.Save.towerFloor;float y=50+(9-index)*TowerFloorStep;
  var b=Btn("",parent,18,y,540,TowerFloorStep,()=>SelectTowerFloor(floor),null,Color.clear);b.name="Tower Floor "+floor;b.transition=Selectable.Transition.None;
  var art=Art(GuxuUiArt.Optional("tower-tier"),b.transform,0,0,540,210.9375f,false);art.name="Tower Floor Art";art.color=open?Color.white:new Color(.59f,.68f,.70f);
  var ring=Art(GuxuUiArt.Optional("state-selected"),b.transform,160,162,220,60,false);ring.name="Tower Selection Glow";ring.gameObject.SetActive(floor==towerSelection);
  Txt("第 "+floor+" 层",b.transform,190,7,160,34,24,stage.boss?gold:cream);
  if(stage.boss)Art(GuxuUiArt.Optional("chapter-node-boss"),b.transform,226,62,88,93);
  if(cleared||!open)Art(GuxuUiArt.Optional(cleared?"state-cleared":"state-lock"),b.transform,254,105,35,44);
  else Txt("当前目标",b.transform,198,108,150,34,23,gold);
 }
 void SelectTowerFloor(int floor){
  var page=activeModule as Gxdl.Modules.TowerView;
  if(page!=null){
   // prefab 页没有 towerScroll；直接刷新这一段的十层与底部详情。
   towerSelection=Mathf.Clamp(floor,1,100);
   page.SetFloors(TowerFloorCells(towerSelection));
   page.SetDetail(TowerDetailCell(towerSelection));
   return;
  }
  towerSelection=floor;foreach(Transform child in towerScroll.content){var ring=child.Find("Tower Selection Glow");if(ring)ring.gameObject.SetActive(child.name=="Tower Floor "+floor);}DrawTowerSelection();
 }
 void FocusTowerFloor(){Canvas.ForceUpdateCanvases();float y=50+(9-(towerSelection-1)%10)*TowerFloorStep+100;towerScroll.StopMovement();towerScroll.content.anchoredPosition=new Vector2(0,Mathf.Clamp(y-towerViewport.rect.height*.72f,0,Mathf.Max(0,TowerContentHeight-towerViewport.rect.height)));}
 void DrawTowerSelection(){
  foreach(Transform child in towerDetail){child.gameObject.SetActive(false);Destroy(child.gameObject);}
  var stage=Stages.tower[towerSelection-1];bool open=GuxuStageRules.CanEnter(stage,Rules.Save),first=towerSelection>=Rules.Save.towerFloor;
  var paper=Art(GuxuUiArt.Optional("ui23-panel-modal",true),towerDetail,16,0,718,415,false);paper.pixelsPerUnitMultiplier=2;
  Art(GuxuUiArt.SecondaryButton,towerDetail,165,-10,420,58,false);Txt("第 "+towerSelection+" 层 · "+(!open?"尚未解锁":first?"当前目标":"已通关"),towerDetail,177,-4,396,47,28,cream).name="Tower Selection Title";
  string enemyPath="Game/Guxu/Expansion/Enemies/"+stage.enemyKey;bool supplied=Resources.Load<Texture2D>(enemyPath);var definition=stage.boss?GuxuBossAnimations.Find(stage.enemyKey):null;
  var portrait=definition!=null?definition.Frame(definition.idleFrame):Sprite(supplied?enemyPath:"Game/Guxu/Final/guardian");var enemyPortrait=Art(portrait,towerDetail,50,72,146,160);enemyPortrait.name="Tower Enemy Portrait";if(definition==null&&!supplied)enemyPortrait.color=Color.Lerp(Color.white,EncounterColors[Mathf.Clamp(stage.region-1,0,14)],.25f);
  Txt(stage.guardianName+(stage.boss?" · Boss":""),towerDetail,220,65,455,40,28,ink,TextAnchor.MiddleLeft).name="Tower Enemy Name";
  var reward=GuxuStageRules.Reward(stage,first);if(first)RewardItems(reward,towerDetail,220,117,480,22,ink,"Tower Reward",true);else Txt("本层首通奖励已领取",towerDetail,220,127,440,64,25,ink,TextAnchor.MiddleLeft);
  string condition=!open&&Rules.Save.stage<GuxuStageRules.RequiredProgress(stage)?"通关主线第"+(GuxuStageRules.RequiredProgress(stage)-1)+"关后开放":!open?"通关第"+(towerSelection-1)+"层后可挑战":first?"每10层首领 · 首通奖励仅领取一次":"重访不再发放首通奖励";
  Txt(condition,towerDetail,60,237,630,38,23,ink).name="Tower Condition";
  SealButton("调整养成",towerDetail,55,304,240,74,ShowGrowthHub).name="Tower Growth";var challenge=SealButton(!open?"尚未解锁":(first?"挑战第":"重访第")+towerSelection+"层",towerDetail,327,304,368,74,()=>ShowBattlePrep(stage),true);challenge.name="Tower Challenge";challenge.interactable=open;
 }
}
}
