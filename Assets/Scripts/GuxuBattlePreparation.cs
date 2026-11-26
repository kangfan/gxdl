using System;
using UnityEngine;
using UnityEngine.UI;

namespace Gxdl {
public partial class GxdlGame {
 CanvasGroup prepSourceDetail;float prepSourceAlpha;
 void RestorePrepSource(){if(prepSourceDetail)prepSourceDetail.alpha=prepSourceAlpha;prepSourceDetail=null;}
 void DrawBattlePreparation(GuxuStage stage){
  CloseModal();Canvas.ForceUpdateCanvases();
  if(ExpansionScreen=="chapter"&&chapterDetail){prepSourceDetail=chapterDetail.GetComponent<CanvasGroup>();if(!prepSourceDetail)prepSourceDetail=chapterDetail.gameObject.AddComponent<CanvasGroup>();prepSourceAlpha=prepSourceDetail.alpha;prepSourceDetail.alpha=0;}
  if(DrawBattlePreparationPrefab(stage))return;
  modal=Rect("Modal",root,0,0,750,1334);FitPage(modal);
  var shade=Img("",modal,0,0,750,1334,new Color(.025f,.075f,.10f,.74f));shade.name="Full Canvas Modal Shade";shade.raycastTarget=true;
  shade.rectTransform.anchorMin=shade.rectTransform.anchorMax=shade.rectTransform.pivot=new Vector2(.5f,.5f);shade.rectTransform.anchoredPosition=Vector2.zero;FitModalShade();
  var p=Rect("Battle Preparation",modal,0,0,940,1672);
  p.anchorMin=p.anchorMax=p.pivot=new Vector2(.5f,.5f);p.anchoredPosition=Vector2.zero;
  // Fit the reference composition as a unit, keeping every hit area inside tall/narrow displays.
  p.localScale=Vector3.one*Mathf.Min(750f/940f,1334f/1672f);
  Art(GuxuUiArt.Optional("battle-prep-panel")??GuxuUiArt.Optional("battle-prep-v2-layout"),p,0,0,940,1672,false).name="Battle Preparation Skin";
  var dark=new Color(.08f,.12f,.12f);var paleGold=new Color(1f,.88f,.58f);
  PrepText(stage.name,p,267,152,470,62,44,paleGold).name="Prep Stage";
  PrepHit("Prep Close",p,784,167,70,69,()=>{CloseModal();if(ExpansionScreen=="battle")ReturnToEncounter(stage);});
  PrepText(stage.guardianName,p,305,368,330,62,48,dark).name="Prep Guardian";
  Img("",p,157,428,40,40,new Color(.08f,.24f,.23f));Art(GuxuUiArt.Optional("feedback-neutral-trait")??GuxuUiArt.Optional("effect-shield"),p,159,429,36,36);
  PrepText("敌方特性",p,198,430,147,36,25,cream);
  var stats=GuxuStageRules.Enemy(stage,Rules.Config);
  PrepText(stage.mechanic,p,198,473,557,32,27,dark,TextAnchor.MiddleLeft).name="Prep Mechanic";
  PrepText(stage.waves+"波敌人 · "+stage.maxRounds+"回合上限",p,198,503,557,30,26,dark,TextAnchor.MiddleLeft);
  PrepText("攻击力 "+stats.attack,p,214,544,132,37,28,dark,TextAnchor.MiddleLeft).name="Prep Enemy Attack";
  PrepText("生命值 "+stats.hp,p,431,544,144,37,28,dark,TextAnchor.MiddleLeft).name="Prep Enemy HP";
  PrepText("防御力 "+stats.defense,p,665,544,125,37,28,dark,TextAnchor.MiddleLeft).name="Prep Enemy Defense";
  bool first=stage.mode==0?!Rules.Save.firstClears[stage.id-1]:stage.mode==1?Rules.Save.dungeonClears[stage.contentId]<stage.difficulty:Rules.Save.towerFloor==stage.difficulty;
  PrepText(first?"首通奖励":"再次挑战奖励",p,198,606,164,37,25,cream,TextAnchor.MiddleCenter).name="Prep Reward Title";
  if(stage.mode==1)PrepText("今日剩余 "+(3-Rules.Save.dungeonClaims[stage.contentId])+" 次",p,407,606,355,37,25,dark,TextAnchor.MiddleRight).name="Prep Reward Limit";
  var reward=GuxuStageRules.Reward(stage,first);
  var labels=reward.ToString().Split(new[]{"  "},StringSplitOptions.RemoveEmptyEntries);
  int[] counts={reward.seals,reward.dust,reward.ore,reward.insight,reward.exp,reward.stones,reward.gems},art={0,1,3,4,2,5,-1};
  bool twoRows=labels.Length>3;int item=0;
  for(int i=0;i<counts.Length;i++){
   if(counts[i]<=0)continue;
   float x=(twoRows?165:197)+item%3*216,y=twoRows?648+item/3*34:650,size=twoRows?36:54;
   ItemCell(art[i]<0?GuxuItemIcons.Gem(reward.gemIndex+1):GuxuUiArt.BattlePrepReward(art[i]),p,x,y,size).name="Prep Reward Icon "+item;
   PrepText(labels[item],p,x+size,y,twoRows?177:143,size,twoRows?25:29,dark,TextAnchor.MiddleLeft).name="Prep Reward Value "+item;item++;
  }
  if(labels.Length==0)PrepText("本层首通奖励已领取",p,200,651,540,48,27,dark);
  PrepText("出战技能",p,200,724,115,39,27,cream);
  PrepText("天赋、装备词条与灵玉自动生效。",p,381,725,329,38,25,dark,TextAnchor.MiddleLeft);

  var choices=new Button[4];var icons=new Image[4];var checks=new RectTransform[4];
  var detail=PrepText("",p,190,986,565,62,23,dark,TextAnchor.MiddleCenter);detail.name="Prep Skill Detail";
  var estimate=PrepText("",p,155,1381,650,42,29,dark);estimate.name="Prep Assessment";
  PrepText("当前配装 / 自动释放抽样估计，手动时机影响结果",p,145,1425,670,46,24,dark).name="Prep Assessment Conditions";
  Action refreshSkills=()=>{
   for(int i=0;i<4;i++){bool selected=Rules.Save.activeSkill==i;choices[i].image.sprite=GuxuUiArt.BattlePrepButton(selected);icons[i].color=selected?paleGold:new Color(.67f,.96f,1f);checks[i].gameObject.SetActive(selected);}
   detail.text=GameRules.SkillDetails[Rules.Save.activeSkill];
   var assessment=Rules.AssessChallenge(stage);estimate.text=assessment.Label+" · "+assessment.wins+" / "+assessment.samples+" 场通过";
  };
  for(int i=0;i<4;i++){
   int skill=i;float x=134+i%2*345,y=798+i/2*99;
   var im=Art(GuxuUiArt.BattlePrepButton(false),p,x,y,330,94,false);im.name="Skill Choice "+i;im.raycastTarget=true;
   var b=im.gameObject.AddComponent<Button>();b.targetGraphic=im;
   b.onClick.AddListener(()=>{Rules.Save.activeSkill=skill;Rules.Store();refreshSkills();});choices[i]=b;
   icons[i]=Art(GuxuUiArt.BattlePrepSkill(i),im.transform,22,-5,104,104);
   checks[i]=Rect("Skill Selected "+i,im.transform,284,1,40,40);
   var rim=checks[i].gameObject.AddComponent<GuxuPortraitDisc>();rim.color=new Color(1f,.88f,.52f);rim.raycastTarget=false;
   var center=Rect("Gold",checks[i],3,3,34,34).gameObject.AddComponent<GuxuPortraitDisc>();center.color=new Color(.72f,.42f,.06f);center.raycastTarget=false;
   PrepText("✓",checks[i],4,0,32,37,31,Color.white);
   PrepText(GameRules.SkillNames[i],im.transform,135,11,164,68,36,cream,TextAnchor.MiddleLeft);
  }
  refreshSkills();
  var mode=PrepText("",p,250,1050,460,43,27,cream,TextAnchor.MiddleCenter);mode.name="Prep Skill Mode";
  Action refreshMode=()=>mode.text=Rules.Save.autoSkill?"技能：自动释放（点击切换）":"技能：手动释放（点击切换）";refreshMode();
  PrepHit("Prep Auto Skill",p,180,1046,581,54,()=>{Rules.Save.autoSkill=!Rules.Save.autoSkill;Rules.Store();refreshMode();});
  PrepText("基础战力",p,399,1111,142,39,30,dark);
  PrepText(Rules.Attributes.Power.ToString(),p,437,1148,158,55,42,new Color(.12f,.32f,.30f),TextAnchor.MiddleLeft).name="Prep Power";
  var traits=Rules.CombatTraits;
  PrepText("会心 "+traits.criticalPercent+"%",p,247,1210,131,41,29,dark);
  PrepText("连击 "+traits.comboPercent+"%",p,448,1210,116,41,29,dark);
  PrepText("反击 "+traits.counterPercent+"%",p,642,1210,116,41,29,dark);
  PrepText("调整养成",p,157,1300,270,54,34,cream);
  PrepHit("Prep Growth",p,134,1290,315,74,ShowGrowthHub);
  bool available=stage.mode!=1||Rules.Save.dungeonClaims[stage.contentId]<3;
  PrepText(available?"开始挑战":"今日次数已满",p,490,1300,305,54,34,new Color(.18f,.09f,.02f));
  var enter=PrepHit("Ruins Challenge",p,474,1290,339,74,StartBattle);enter.interactable=available;
  if(!available)Img("",p,481,1297,323,59,new Color(.03f,.06f,.08f,.35f));
 }
 Text PrepText(string value,Transform p,float x,float y,float w,float h,int size,Color color,TextAnchor alignment=TextAnchor.MiddleCenter){
  var label=Txt(value,p,x,y,w,h,size,color,alignment);label.resizeTextForBestFit=true;label.resizeTextMinSize=Mathf.Max(18,size*2/3);label.resizeTextMaxSize=size;return label;
 }
 Button PrepHit(string name,Transform p,float x,float y,float w,float h,Action action){
  var b=Btn("",p,x,y,w,h,action,null,Color.white);b.name=name;
  var colors=b.colors;colors.normalColor=colors.selectedColor=colors.disabledColor=Color.clear;
  colors.highlightedColor=new Color(1,1,1,.07f);colors.pressedColor=new Color(.05f,.02f,0,.22f);colors.fadeDuration=0;b.colors=colors;return b;
 }
 // 关闭战前准备后回到进入本页的来源，避免关闭后整屏空白。
 void CloseBattlePrep(GuxuStage stage){
  CloseScreen();CloseModal();
  switch(ExpansionScreen){
   case "chapter":ShowChapter(chapterRegion);break;
   case "dungeon":ShowDungeon(difficultyKind);break;
   case "tower":ShowTower();break;
   case "battle":ReturnToEncounter(stage);break;
   default:Home();break;
  }
 }
 // prefab 版战前准备。宿主未就绪或 prefab 缺失时返回 false，回退旧弹窗。
 bool DrawBattlePreparationPrefab(GuxuStage stage){  CloseModal();
  var page=OpenScreen<Gxdl.Modules.BattlePrepView>(Gxdl.Modules.UiScreens.BattlePrep);
  if(page==null)return false;
  // 选技能只重刷技能区，不重建整页，避免图标闪烁。
  page.BindSkillCallbacks(skill=>{
   Rules.Save.activeSkill=skill;Rules.Store();page.SetSkill(skill);
   page.Apply(new Gxdl.Modules.BattlePrepView.Model{SelectedSkill=skill,SkillDetail=GameRules.SkillDetails[skill]});
  });
  if(page.Close){page.Close.onClick.RemoveAllListeners();page.Close.onClick.AddListener(()=>CloseBattlePrep(stage));}
  if(page.Growth){page.Growth.onClick.RemoveAllListeners();page.Growth.onClick.AddListener(()=>{CloseScreen();ShowGrowthHub();});}
  if(page.AutoSkill){page.AutoSkill.onClick.RemoveAllListeners();page.AutoSkill.onClick.AddListener(()=>{Rules.Save.autoSkill=!Rules.Save.autoSkill;Rules.Store();page.SetSkillMode(Rules.Save.autoSkill);});}
  if(page.Challenge){page.Challenge.onClick.RemoveAllListeners();page.Challenge.onClick.AddListener(StartBattle);}
  page.Apply(BattlePrepModel(stage));
  return true;
 }
 // 数值与旧弹窗同源：敌人数值走 GuxuStageRules.Enemy，奖励走 GuxuStageRules.Reward。
 Gxdl.Modules.BattlePrepView.Model BattlePrepModel(GuxuStage stage){
  var stats=GuxuStageRules.Enemy(stage,Rules.Config);
  bool first=stage.mode==0?!Rules.Save.firstClears[stage.id-1]:stage.mode==1?Rules.Save.dungeonClears[stage.contentId]<stage.difficulty:Rules.Save.towerFloor==stage.difficulty;
  var reward=GuxuStageRules.Reward(stage,first);
  var labels=reward.ToString().Split(new[]{"  "},StringSplitOptions.RemoveEmptyEntries);
  var cells=new System.Collections.Generic.List<Gxdl.Modules.BattlePrepView.RewardCell>();
  int[] counts={reward.seals,reward.dust,reward.ore,reward.insight,reward.exp,reward.stones,reward.gems},art={0,1,3,4,2,5,-1};
  for(int i=0;i<counts.Length;i++){
   if(counts[i]<=0)continue;
   cells.Add(new Gxdl.Modules.BattlePrepView.RewardCell{Label=labels[cells.Count],Value="",IconKind=art[i],GemIndex=art[i]<0?reward.gemIndex:-1});
  }
  var assessment=Rules.AssessChallenge(stage);
  bool available=stage.mode!=1||Rules.Save.dungeonClaims[stage.contentId]<3;
  var traits=Rules.CombatTraits;
  return new Gxdl.Modules.BattlePrepView.Model{
   StageName=stage.name,Guardian=stage.guardianName,Mechanic=stage.mechanic,
   WaveLabel=stage.waves+"波敌人 · "+stage.maxRounds+"回合上限",
   EnemyAttack="攻击力 "+stats.attack,EnemyHp="生命值 "+stats.hp,EnemyDefense="防御力 "+stats.defense,
   RewardTitle=first?"首通奖励":"再次挑战奖励",
   RewardLimit=stage.mode==1?"今日剩余 "+(3-Rules.Save.dungeonClaims[stage.contentId])+" 次":"",
   Rewards=cells.ToArray(),SelectedSkill=Rules.Save.activeSkill,SkillDetail=GameRules.SkillDetails[Rules.Save.activeSkill],
   AutoSkill=Rules.Save.autoSkill,
   Assessment=assessment.Label+" · "+assessment.wins+" / "+assessment.samples+" 场通过",
   Power=Rules.Attributes.Power.ToString(),Critical="会心 "+traits.criticalPercent+"%",
   Combo="连击 "+traits.comboPercent+"%",Counter="反击 "+traits.counterPercent+"%",
   CanChallenge=available,ChallengeLabel=available?"开始挑战":"今日次数已满"};
 }
}
}
