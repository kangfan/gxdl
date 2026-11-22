using System;
using UnityEngine;
using UnityEngine.UI;

namespace Gxdl {
public partial class GxdlGame {
 readonly int[] difficultySelections=new int[4];
 readonly Vector2[] difficultyPlatforms={new Vector2(364,694),new Vector2(538,590),new Vector2(202,510),new Vector2(547,436),new Vector2(205,372),new Vector2(517,307),new Vector2(244,250),new Vector2(489,192),new Vector2(281,141),new Vector2(446,91)};
 int difficultyKind;RectTransform difficultyViewport,difficultyDetail,difficultyPath,difficultyShade;ScrollRect difficultyScroll;readonly RectTransform[] difficultyNodes=new RectTransform[10];float difficultyShadeBottom;
 void FitDifficultyPage(){
  Canvas.ForceUpdateCanvases();float scale=Mathf.Min(root.rect.width/750f,root.rect.height/1334f),height=root.rect.height/scale;
  view.sizeDelta=new Vector2(750,height);view.anchorMin=view.anchorMax=new Vector2(.5f,.5f);view.pivot=new Vector2(0,1);view.anchoredPosition=new Vector2(-375,height*.5f)*scale;view.localScale=Vector3.one*scale;
  if(difficultyViewport)difficultyViewport.sizeDelta=new Vector2(750,height-605);
  if(difficultyDetail)difficultyDetail.anchoredPosition=new Vector2(0,-(height-425));
  if(difficultyScroll){float factor=Mathf.Max(1,difficultyViewport.rect.height/808);difficultyScroll.content.sizeDelta=new Vector2(750,808*factor);if(difficultyPath){difficultyPath.sizeDelta=new Vector2(750*factor,808*factor);difficultyPath.anchoredPosition=new Vector2((750-750*factor)*.5f,0);}if(difficultyShade)difficultyShade.sizeDelta=new Vector2(750,difficultyShadeBottom*factor);for(int i=0;i<10;i++)if(difficultyNodes[i]){var point=difficultyPlatforms[i]*factor+new Vector2((750-750*factor)*.5f,0);var node=difficultyNodes[i];node.anchoredPosition=new Vector2(point.x-node.rect.width*.5f,-point.y+(node.rect.height-10)*.875f);}}
 }
 // prefab 版秘境详情。宿主未就绪或 prefab 缺失时返回 false，回退旧构建。
 bool ShowDungeonDetailPrefab(int kind){
  if(Gxdl.Core.GameBootstrap.Instance==null||Gxdl.Core.GameBootstrap.Instance.UiModules==null)return false;
  CloseScreen(); // 先收起残留的 prefab 页，避免两页同时在屏。
  var page=OpenScreen<Gxdl.Modules.DungeonDetailView>(Gxdl.Modules.UiScreens.DungeonDetail);if(page==null)return false;
  int lastOpen=0;for(int i=1;i<=10;i++)if(GuxuStageRules.CanEnter(Stages.Dungeon(kind,i),Rules.Save))lastOpen=i;
  int selected=difficultySelections[kind];var tiers=new Gxdl.Modules.DungeonDetailView.TierNodeState[10];
  for(int t=1;t<=10;t++)tiers[t-1]=new Gxdl.Modules.DungeonDetailView.TierNodeState{Unlocked=GuxuStageRules.CanEnter(Stages.Dungeon(kind,t),Rules.Save),Cleared=t<=Rules.Save.dungeonClears[kind],Selected=t==selected};
  if(page.Back)page.Back.onClick.AddListener(ShowDungeons);
  // 奖励记录入口与新页同一口径（dungeon 源）。
  if(page.History)page.History.onClick.AddListener(()=>ShowRewardHistory("dungeon",0));
  page.BindTierCallbacks(SelectDifficulty);page.BindChallenge(()=>ShowBattlePrep(Stages.Dungeon(kind,selected)));
  page.BindSweep(()=>{var stage=Stages.Dungeon(kind,selected);if(GuxuStageRules.Sweep(Rules,stage,DateTime.UtcNow,out var paid)){string id=Rules.LastRewardReceipt.id;Rules.Store();ShowDungeon(kind);ShowRewardReceipt(id);}else{ShowDungeon(kind);Notify("需先通关且有剩余收益次数");}});
  page.Apply(new Gxdl.Modules.DungeonDetailView.DetailHeader{Name=GuxuStageRules.DungeonNames[kind],Attempts="今日剩余 "+(3-Rules.Save.dungeonClaims[kind])+" 次 · 失败不扣次数",ShowHistory=true,
   LockBottom=lastOpen<10?(lastOpen==0?808:(difficultyPlatforms[lastOpen-1].y+difficultyPlatforms[lastOpen].y)*.5f):0f},tiers);
  page.ApplyDetail(DifficultyDetailContent(kind,selected));return true;
 }
 // 底部详情文案与旧 DrawDifficultySelection 完全一致，数值都来自真实规则。
 Gxdl.Modules.DungeonDetailView.DetailContent DifficultyDetailContent(int kind,int tier){
  var stage=Stages.Dungeon(kind,tier);bool first=tier>Rules.Save.dungeonClears[kind],open=GuxuStageRules.CanEnter(stage,Rules.Save),attempts=Rules.Save.dungeonClaims[kind]<3;
  string condition=open?(attempts?stage.waves+"波试炼 · "+stage.maxRounds+"回合上限":"今日收益次数已满 · 北京时间零点刷新"):(GuxuStageRules.RequiredProgress(stage)>200?"主线通关":"通关主线第"+(GuxuStageRules.RequiredProgress(stage)-1)+"关")+(tier>1?" · 前一难度首通":"");
  return new Gxdl.Modules.DungeonDetailView.DetailContent{Title="难度 "+tier+" · "+(!open?"尚未解锁":!attempts?"今日收益已满":first?"可挑战":"已通关"),
   RewardState=first?"首次挑战 · 首通奖励":"已首通 · 常规奖励",Rewards=GuxuStageRules.Reward(stage,first).ToString().Split(new[]{"  "},StringSplitOptions.RemoveEmptyEntries),
   Condition=condition,ChallengeLabel=!open?"尚未解锁":!attempts?"今日次数已满":"挑战",CanChallenge=open&&attempts,CanSweep=open&&!first&&attempts};
 }
 public void ShowDungeon(int kind){
  if(busy||!CanPlay||kind<0||kind>=4)return;Rules.RefreshDaily(DateTime.UtcNow);auto=false;battle=false;Clear();ExpansionScreen="dungeon";Rules.EnsureExpansion();difficultyKind=kind;
  if(difficultySelections[kind]<1)difficultySelections[kind]=Mathf.Clamp(Rules.Save.dungeonClears[kind]+1,1,10);
  if(ShowDungeonDetailPrefab(kind))return;
  difficultyViewport=null;difficultyDetail=null;difficultyScroll=null;difficultyPath=null;difficultyShade=null;for(int i=0;i<10;i++)difficultyNodes[i]=null;fitExpandedPage=FitDifficultyPage;FitDifficultyPage();
  if(difficultySelections[kind]<1)difficultySelections[kind]=Mathf.Clamp(Rules.Save.dungeonClears[kind]+1,1,10);
  Img("",view,0,0,750,view.rect.height,new Color(.035f,.13f,.18f));
  difficultyViewport=Rect("Difficulty Path Viewport",view,0,180,750,view.rect.height-605);var hit=difficultyViewport.gameObject.AddComponent<Image>();hit.color=Color.clear;hit.raycastTarget=true;difficultyViewport.gameObject.AddComponent<RectMask2D>();
  var content=Rect("Difficulty Path Content",difficultyViewport,0,0,750,808);difficultyPath=Art(GuxuUiArt.Optional("difficulty-path"),content,0,0,750,808,false).rectTransform;
  difficultyScroll=difficultyViewport.gameObject.AddComponent<ScrollRect>();difficultyScroll.viewport=difficultyViewport;difficultyScroll.content=content;difficultyScroll.horizontal=false;difficultyScroll.movementType=ScrollRect.MovementType.Clamped;difficultyScroll.scrollSensitivity=75;difficultyScroll.decelerationRate=.06f;
  int lastOpen=0;for(int i=1;i<=10;i++)if(GuxuStageRules.CanEnter(Stages.Dungeon(kind,i),Rules.Save))lastOpen=i;
  if(lastOpen<10){float bottom=lastOpen==0?808:(difficultyPlatforms[lastOpen-1].y+difficultyPlatforms[lastOpen].y)*.5f;var shade=Rect("Difficulty Locked Path",content,0,0,750,bottom).gameObject.AddComponent<GuxuPathShade>();shade.raycastTarget=false;shade.color=new Color(.015f,.045f,.065f,.52f);difficultyShade=shade.rectTransform;difficultyShadeBottom=bottom;}
  for(int tier=1;tier<=10;tier++)DrawDifficultyNode(content,tier);
  Img("",view,0,0,750,180,new Color(.025f,.10f,.14f));SealButton("‹ 返回秘境",view,14,22,177,52,ShowDungeons).name="Difficulty Back";
  Txt(GuxuStageRules.DungeonNames[kind],view,193,19,400,63,43,gold).name="Difficulty Name";Art(GuxuUiArt.Optional(DungeonPortalKeys[kind]),view,633,8,100,100);
  Txt("今日剩余 "+(3-Rules.Save.dungeonClaims[kind])+" 次 · 失败不扣次数",view,60,95,630,34,25,cream).name="Difficulty Attempts";
  Txt("已通关难度可速通",view,40,137,420,30,22,cream);RewardHistoryButton(view,"dungeon",495,127,225);
  difficultyDetail=Rect("Difficulty Selection Detail",view,0,view.rect.height-425,750,425);DrawDifficultySelection();
  FitDifficultyPage();Canvas.ForceUpdateCanvases();difficultyScroll.content.anchoredPosition=Vector2.zero;
 }
 void DrawDifficultyNode(Transform parent,int tier){
  var stage=Stages.Dungeon(difficultyKind,tier);bool open=GuxuStageRules.CanEnter(stage,Rules.Save),cleared=tier<=Rules.Save.dungeonClears[difficultyKind],selected=tier==difficultySelections[difficultyKind];var point=difficultyPlatforms[tier-1];float width=tier<=2?140:tier<=5?120:104,height=width*.75f;
  var b=Btn("",parent,point.x-width*.5f,point.y-height*.875f,width,height+10,()=>SelectDifficulty(tier),null,Color.clear);b.name="Difficulty Node "+tier;b.transition=Selectable.Transition.None;difficultyNodes[tier-1]=(RectTransform)b.transform;
  var plate=Art(GuxuUiArt.Optional("difficulty-node"),b.transform,0,0,width,height);plate.name="Difficulty Node Art";plate.color=open?Color.white:new Color(.63f,.71f,.72f);
  var selectedRing=Art(GuxuUiArt.Optional("state-selected"),b.transform,-8,-8,width+16,height+16,false);selectedRing.name="Difficulty Selection";selectedRing.gameObject.SetActive(selected);
  Txt(tier.ToString(),b.transform,12,height*(tier<=2?.15f:.10f),width-24,tier<=2?44:36,tier<=2?32:27,cream).name="Difficulty Tier "+tier;
  if(cleared||!open)Art(GuxuUiArt.Optional(cleared?"state-cleared":"state-lock"),b.transform,width*.5f-14,height-(tier<=2?44:34),28,30);
  else Txt("可挑战",b.transform,10,height-(tier<=2?44:34),width-20,29,18,gold);
 }
 void SelectDifficulty(int tier){
  difficultySelections[difficultyKind]=tier;
  var prefab=activeModule as Gxdl.Modules.DifficultyView;
  if(prefab!=null){prefab.SetSelected(tier);prefab.FocusTier(tier);prefab.ApplyDetail(DifficultyViewDetail(difficultyKind,tier));return;}
  foreach(Transform child in difficultyScroll.content){var ring=child.Find("Difficulty Selection");if(ring)ring.gameObject.SetActive(child.name=="Difficulty Node "+tier);}DrawDifficultySelection();
 }
 // prefab 版难度选择。宿主未就绪或 prefab 缺失时返回 false，回退旧构建。
 bool DrawDifficultySelectionPrefab(){
  if(Gxdl.Core.GameBootstrap.Instance==null||Gxdl.Core.GameBootstrap.Instance.UiModules==null)return false;
  CloseScreen(); // 先收起残留的 prefab 页，避免两页同时在屏。
  var page=OpenScreen<Gxdl.Modules.DifficultyView>(Gxdl.Modules.UiScreens.Difficulty);if(page==null)return false;
  int kind=difficultyKind,selected=difficultySelections[kind];
  var tiers=new Gxdl.Modules.DifficultyView.TierState[10];
  for(int t=1;t<=10;t++)tiers[t-1]=new Gxdl.Modules.DifficultyView.TierState{Open=GuxuStageRules.CanEnter(Stages.Dungeon(kind,t),Rules.Save),Cleared=t<=Rules.Save.dungeonClears[kind]};
  if(page.Back)page.Back.onClick.AddListener(ShowDungeons);
  page.BindTierCallbacks(SelectDifficulty);
  page.ApplyHeader(GuxuStageRules.DungeonNames[kind],"今日剩余 "+(3-Rules.Save.dungeonClaims[kind])+" 次 · 失败不扣次数");
  page.SetPath(tiers,selected);page.ApplyDetail(DifficultyViewDetail(kind,selected));
  return true;
 }
 Gxdl.Modules.DifficultyView.Detail DifficultyViewDetail(int kind,int tier){
  var stage=Stages.Dungeon(kind,tier);bool first=tier>Rules.Save.dungeonClears[kind],open=GuxuStageRules.CanEnter(stage,Rules.Save),attempts=Rules.Save.dungeonClaims[kind]<3;
  string condition=open?(attempts?stage.waves+"波试炼 · "+stage.maxRounds+"回合上限":"今日收益次数已满 · 北京时间零点刷新"):(GuxuStageRules.RequiredProgress(stage)>200?"主线通关":"通关主线第"+(GuxuStageRules.RequiredProgress(stage)-1)+"关")+(tier>1?" · 前一难度首通":"");
  return new Gxdl.Modules.DifficultyView.Detail{Title="难度 "+tier+" · "+(!open?"尚未解锁":!attempts?"今日收益已满":first?"可挑战":"已通关"),
   RewardState=first?"首次挑战 · 首通奖励":"已首通 · 常规奖励",Rewards=RewardCells(GuxuStageRules.Reward(stage,first)),Condition=condition,
   CanChallenge=open&&attempts,ChallengeLabel=!open?"尚未解锁":!attempts?"今日次数已满":"挑战",CanSweep=open&&!first&&attempts};
 }
 void DrawDifficultySelection(){
  if(DrawDifficultySelectionPrefab())return;
  foreach(Transform child in difficultyDetail){child.gameObject.SetActive(false);Destroy(child.gameObject);}
  int tier=difficultySelections[difficultyKind];var stage=Stages.Dungeon(difficultyKind,tier);bool first=tier>Rules.Save.dungeonClears[difficultyKind],open=GuxuStageRules.CanEnter(stage,Rules.Save),attempts=Rules.Save.dungeonClaims[difficultyKind]<3;
  var paper=Art(GuxuUiArt.Optional("ui23-panel-modal",true),difficultyDetail,16,0,718,415,false);paper.pixelsPerUnitMultiplier=2;
  Art(GuxuUiArt.SecondaryButton,difficultyDetail,170,-10,410,58,false);Txt("难度 "+tier+" · "+(!open?"尚未解锁":!attempts?"今日收益已满":first?"可挑战":"已通关"),difficultyDetail,180,-4,390,46,28,cream).name="Difficulty Selection Title";
  Txt(first?"首次挑战 · 首通奖励":"已首通 · 常规奖励",difficultyDetail,60,62,630,36,26,ink).name="Difficulty Reward State";
  var reward=GuxuStageRules.Reward(stage,first);int count=reward.ToString().Split(new[]{"  "},StringSplitOptions.RemoveEmptyEntries).Length;RewardItems(reward,difficultyDetail,61,112,count==2?942:628,23,ink,"Difficulty Reward",true);
  string condition=open?(attempts?stage.waves+"波试炼 · "+stage.maxRounds+"回合上限":"今日收益次数已满 · 北京时间零点刷新"):(GuxuStageRules.RequiredProgress(stage)>200?"主线通关":"通关主线第"+(GuxuStageRules.RequiredProgress(stage)-1)+"关")+(tier>1?" · 前一难度首通":"");
  Txt(condition,difficultyDetail,55,208,640,52,23,ink).name="Difficulty Condition";
  var challenge=SealButton(!open?"尚未解锁":!attempts?"今日次数已满":"挑战",difficultyDetail,68,292,345,80,()=>ShowBattlePrep(stage),true);challenge.name="Difficulty Challenge";challenge.interactable=open&&attempts;
  var sweep=SealButton("速通",difficultyDetail,443,292,235,80,()=>{if(GuxuStageRules.Sweep(Rules,stage,DateTime.UtcNow,out var paid)){string id=Rules.LastRewardReceipt.id;Rules.Store();ShowDungeon(difficultyKind);ShowRewardReceipt(id);}else{ShowDungeon(difficultyKind);Notify("需先通关且有剩余收益次数");}});sweep.name="Difficulty Sweep";sweep.interactable=open&&!first&&attempts;
 }
}
[RequireComponent(typeof(CanvasRenderer))]
public sealed class GuxuPathShade:MaskableGraphic {
 protected override void OnPopulateMesh(VertexHelper mesh){mesh.Clear();var r=rectTransform.rect;float split=Mathf.Max(r.yMin,r.yMin+64);Color clear=color;clear.a=0;Vector2[] positions={new Vector2(r.xMin,r.yMax),new Vector2(r.xMax,r.yMax),new Vector2(r.xMin,split),new Vector2(r.xMax,split),new Vector2(r.xMin,r.yMin),new Vector2(r.xMax,r.yMin)};for(int i=0;i<6;i++)mesh.AddVert(positions[i],i<4?color:clear,Vector2.zero);mesh.AddTriangle(0,1,2);mesh.AddTriangle(2,1,3);mesh.AddTriangle(2,3,4);mesh.AddTriangle(4,3,5);}
}
}
