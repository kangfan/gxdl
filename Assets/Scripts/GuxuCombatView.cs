using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Gxdl {
public partial class GxdlGame {
 GuxuStageConfig stageConfig;
 GuxuStage selectedStage;
 GuxuStageConfig Stages=>stageConfig??(stageConfig=GuxuStageConfig.Load());
 public GuxuStage CurrentStage=>selectedStage??Stages.ForProgress(Rules.Save.stage);
 public GuxuBattle ActiveBattle {get;private set;}
 Image playerShieldBar,enemyShieldBar,combatBackdrop,combatSkillIcon,combatCooldownRing;Text battleWarning;
 RectTransform combatArena,combatLower;
 GuxuBattleParticles combatParticles;
 Text combatPlayerStatus,combatEnemyStatus;
 int displayedDamageDone,displayedDamageTaken,displayedHealingDone;
 const float PlayerHealthWidth=270,EnemyHealthWidth=550;
 public bool IsRuinsRegionUnlocked(int index)=>index>=0&&index<10&&Rules.Save.stage>index*20;
 public void Adventure()=>ShowWorldMap();
 public void OpenRuinsRegion(int index)=>ShowChapter(index);
 public void StartBattle(){
  if(busy||!CanPlay)return;Rules.RefreshDaily(DateTime.UtcNow);var stage=CurrentStage;
  if(!GuxuStageRules.CanEnter(stage,Rules.Save)){Notify("该挑战尚未解锁");return;}
  if(stage.mode==1&&Rules.Save.dungeonClaims[stage.contentId]>=3){Notify("今日收益次数已用尽，明日再来");return;}
  auto=false;battle=true;busy=true;Clear();ExpansionScreen="battle";hero=null;enemy=null;combatArena=null;combatLower=null;combatBackdrop=null;displayedDamageDone=displayedDamageTaken=displayedHealingDone=0;DrawEncounterScene(stage);
  combatArena=Rect("Battle Arena",view,0,0,750,1334);
  GuxuParticleFx.Sort(combatArena,-20);
  var simulation=GuxuStageRules.Begin(Rules,stage,Environment.TickCount);ActiveBattle=simulation;
  var player=Img("Game/Guxu/Final/Hero/hero",combatArena,-20,470,390,390);player.preserveAspect=true;player.gameObject.AddComponent<GuxuCharacterMotion>().AttackTravel=.34f;
  var guardian=EncounterEnemy(stage,combatArena);player.transform.SetSiblingIndex(guardian.transform.GetSiblingIndex()+1);
  GuxuFootCloud.Attach(player);GuxuFootCloud.Attach(guardian);
  combatParticles=view.gameObject.AddComponent<GuxuBattleParticles>();combatParticles.Bind(player,guardian,simulation.PlayerShield,simulation.EnemyShield);
  var upperHaze=Rect("Battle Navigation Soft Backing",view,0,-65,750,300).gameObject.AddComponent<GuxuSoftVfx>();upperHaze.raycastTarget=false;upperHaze.color=new Color(.025f,.10f,.15f,.65f);
  HudText(stage.regionName+" · "+(stage.boss?"首领试炼":stage.dropBias),view,142,8,460,52,27,gold,TextAnchor.MiddleCenter).name="Battle Region";
  var exit=HudAction("退出挑战",view,18,0,116,88,()=>{ActiveBattle=null;busy=false;battle=false;ReturnToEncounter(stage);});HudText("‹ 退出",exit.transform,0,8,116,52,27,cream,TextAnchor.MiddleCenter);
  var speed=HudAction("Battle Speed",view,606,0,138,88,ToggleBattleSpeed);HudText("×"+fightSpeed+" 加速",speed.transform,0,8,138,52,27,cream,TextAnchor.MiddleCenter);
  var enemyPortrait=Art(guardian.sprite,view,42,98,88,88);enemyPortrait.name="Battle Enemy Portrait";enemyPortrait.color=guardian.color;
  HudText(stage.guardianName,view,145,67,550,43,34,gold,TextAnchor.MiddleCenter).name="Battle Enemy Name";
  var right=HudText("",view,145,107,550,34,26,cream,TextAnchor.MiddleCenter);right.name="Enemy Health Value";
  BattleTrack(view,145,144,EnemyHealthWidth,14);
  var rh=Img("",view,145,146,EnemyHealthWidth,10,new Color(.95f,.72f,.28f));rh.name="Enemy Health Bar";
  enemyShieldBar=Img("",view,145,164,EnemyHealthWidth*simulation.EnemyShield/simulation.Enemy.hp,6,new Color(.26f,.75f,.94f));enemyShieldBar.name="Enemy Shield Bar";
  var rs=HudText("",view,145,174,550,32,23,cream,TextAnchor.MiddleCenter);rs.name="Enemy Combat Status";
  var round=HudText("第 0 / "+stage.maxRounds+" 回合 · 第 1 / "+stage.waves+" 波",view,130,211,490,38,25,gold,TextAnchor.MiddleCenter);round.name="Battle Round";
  var eventLabel=HudText(stage.mechanic,view,55,252,640,36,23,cream,TextAnchor.MiddleCenter);eventLabel.name="Battle Event";
  battleWarning=HudText("",view,45,291,660,39,25,new Color(1,.67f,.34f),TextAnchor.MiddleCenter);battleWarning.name="Boss Warning";
  combatLower=Rect("Battle Lower HUD",view,0,1054,750,280);
  var haze=Rect("Battle Controls Soft Backing",combatLower,-30,-5,810,310).gameObject.AddComponent<GuxuSoftVfx>();haze.raycastTarget=false;haze.color=new Color(.025f,.10f,.15f,.78f);
  HudPortrait(combatLower,48,8,78,"Battle Player Portrait");
  HudText("寻墟者",combatLower,149,4,125,38,28,gold);
  BattleTrack(combatLower,270,24,PlayerHealthWidth,14);
  var lh=Img("",combatLower,270,26,PlayerHealthWidth,10,new Color(.20f,.87f,.60f));lh.name="Player Health Bar";
  var left=HudText("",combatLower,570,9,174,38,24,cream);left.name="Player Health Value";
  playerShieldBar=Img("",combatLower,270,44,PlayerHealthWidth*simulation.PlayerShield/simulation.Player.hp,7,new Color(.26f,.75f,.94f));playerShieldBar.name="Player Shield Bar";
  Art(GuxuUiArt.Optional("effect-shield")??GuxuUiArt.BattlePrepSkill(1),combatLower,148,53,29,29).name="Player Shield Icon";
  var ls=HudText("",combatLower,185,52,530,36,24,cream);ls.name="Player Combat Status";
  combatPlayerStatus=ls;combatEnemyStatus=rs;
  var skillState=HudText("",combatLower,261,154,240,36,25,cream);skillState.name="Battle Skill State";
  Button skill=null,autoButton=null;
  skill=HudAction("Cast Skill",combatLower,132,96,366,132,()=>{if(simulation.QueueSkill())Notify("已准备，将在下一次出手释放");else Notify("技能冷却中或已准备");RefreshBattleControls(simulation,skillState,skill,autoButton);});
  var seal=Art(GuxuUiArt.Optional("hud-skill-seal"),skill.transform,10,10,104,104);seal.name="Skill Seal Art";
  combatCooldownRing=Art(GuxuUiArt.Optional("state-selected"),combatLower,140,104,108,108);combatCooldownRing.name="Skill Cooldown Ring";combatCooldownRing.type=Image.Type.Filled;combatCooldownRing.fillMethod=Image.FillMethod.Radial360;combatCooldownRing.fillOrigin=2;combatCooldownRing.fillClockwise=false;
  string[] skillIcons={"effect-attack","effect-shield","effect-healing","effect-break"};
  combatSkillIcon=Art(GuxuUiArt.Optional(skillIcons[simulation.Skill])??GuxuUiArt.BattlePrepSkill(simulation.Skill),combatLower,154,114,80,80);combatSkillIcon.name="Battle Skill Icon";
  HudText(GameRules.SkillNames[simulation.Skill],combatLower,261,111,230,40,29,gold).name="Battle Skill Name";
  autoButton=HudToggle("Auto Skill","自动技能",combatLower,503,111,220,104,simulation.AutoSkill,()=>{simulation.AutoSkill=!simulation.AutoSkill;Rules.Save.autoSkill=simulation.AutoSkill;Rules.Store();RefreshBattleControls(simulation,skillState,skill,autoButton);});
  Img("",combatLower,40,232,670,1,new Color(.79f,.68f,.40f,.55f));
  var brief=HudText("输出 0    承伤 0    治疗 0",combatLower,38,238,674,34,23,cream,TextAnchor.MiddleCenter);brief.name="Battle Statistics";
  fitExpandedPage=FitCombatPage;FitCombatPage();
  StartCoroutine(ShowExpansionBattle(simulation,player,guardian,left,right,ls,rs,lh,rh,round,eventLabel,skillState,skill,autoButton,brief));
 }
 void BattleTrack(Transform parent,float x,float y,float width,float height){
  Img("",parent,x,y,width,height,new Color(.02f,.07f,.09f,.82f));var frame=GuxuUiArt.Optional("hud-health-frame",true);if(frame){var art=Art(frame,parent,x-18,y+(height-38)*.5f,width+36,38,false);art.pixelsPerUnitMultiplier=1;art.name="HUD Health Frame";}
 }
 void FitCombatPage(){
  Canvas.ForceUpdateCanvases();float scale=Mathf.Min(root.rect.width/750f,root.rect.height/1334f),height=root.rect.height/scale;
  view.sizeDelta=new Vector2(750,height);view.anchorMin=view.anchorMax=new Vector2(.5f,.5f);view.pivot=new Vector2(0,1);view.anchoredPosition=new Vector2(-375,height*.5f)*scale;view.localScale=Vector3.one*scale;
  if(combatArena)combatArena.anchoredPosition=new Vector2(0,-(height-1334)*.5f);
  if(combatLower)combatLower.anchoredPosition=new Vector2(0,-(height-combatLower.rect.height));
  if(combatBackdrop&&combatBackdrop.sprite){var source=combatBackdrop.sprite.rect;float cover=Mathf.Max(750/source.width,height/source.height);var size=new Vector2(source.width,source.height)*cover;combatBackdrop.rectTransform.sizeDelta=size;combatBackdrop.rectTransform.anchoredPosition=new Vector2((750-size.x)*.5f,(size.y-height)*.5f);}
 }
 void ReturnToEncounter(GuxuStage stage){selectedStage=null;if(stage.mode==1)ShowDungeon(stage.contentId);else if(stage.mode==2)ShowTower();else ShowChapter(stage.region-1);}
 IEnumerator ShowExpansionBattle(GuxuBattle simulation,Image player,Image guardian,Text left,Text right,Text leftStatus,Text rightStatus,Image leftBar,Image rightBar,Text round,Text eventLabel,Text skillState,Button skill,Button autoButton,Text brief){
  int version=screenVersion;var motion=player.GetComponent<GuxuCharacterMotion>();var bossMotion=guardian.GetComponent<GuxuBossMotion>();var effects=view.gameObject.AddComponent<GuxuCombatVfx>();effects.Bind(view);var sound=view.gameObject.AddComponent<GuxuBattleSound>();
  left.text=simulation.PlayerHp+" / "+simulation.Player.hp;right.text=simulation.EnemyHp+" / "+simulation.Enemy.hp;
  while(!simulation.Completed){
   leftStatus.text=simulation.PlayerStatus;rightStatus.text=simulation.EnemyStatus;
   RefreshBattleControls(simulation,skillState,skill,autoButton);
   // A short decision window lets manual skill requests be queued between turns.
   yield return new WaitForSeconds(.55f/fightSpeed);if(version!=screenVersion)yield break;
   var events=simulation.NextTurn();round.text="第 "+simulation.Round+" / "+simulation.Stage.maxRounds+" 回合 · 第 "+simulation.Wave+" / "+simulation.Stage.waves+" 波";
   foreach(var e in events){
    if(version!=screenVersion)yield break;int speed=fightSpeed;combatParticles.Speed=speed;bool heal=e.kind==GuxuBattleEventKind.Heal,shield=e.kind==GuxuBattleEventKind.Shield;
    bool warning=e.kind==GuxuBattleEventKind.Charge||e.kind==GuxuBattleEventKind.Phase;
    if(battleWarning)battleWarning.text=warning?e.label:simulation.Stage.boss&&e.round%3==2?"预警 · 下一回合敌方重击":"";
    eventLabel.text=warning?"":(e.playerActor?"我方 · ":"敌方 · ")+e.label;eventLabel.color=cream;
    if(e.kind==GuxuBattleEventKind.Wave){guardian.color=Color.white;PulseBattle(guardian,new Color(.6f,.8f,1));yield return new WaitForSeconds(.45f/speed);}
    else if(e.kind==GuxuBattleEventKind.Charge||e.kind==GuxuBattleEventKind.Phase||e.kind==GuxuBattleEventKind.Break){if(e.kind==GuxuBattleEventKind.Break)combatParticles.Event(e,1.25f);else combatParticles.Warning(e.playerActor?player:guardian,new Color(1,.40f,.15f,.85f));UpdateCombatBars(e,left,right,leftBar,rightBar,simulation,speed);sound.Cue(2);yield return new WaitForSeconds(.4f/speed);}
    else if(heal||shield){combatParticles.Event(e);UpdateCombatBars(e,left,right,leftBar,rightBar,simulation,speed);if(e.playerActor)RefreshBattleControls(simulation,skillState,skill,autoButton);AddDisplayedStatistics(e,brief);sound.Cue(1);BattleFloat(e,heal?"恢复 +":"护盾 +");yield return new WaitForSeconds(.45f/speed);}
    else if(e.playerActor&&(e.kind==GuxuBattleEventKind.Skill||e.kind==GuxuBattleEventKind.Combo)){
     int displayed=0;motion.PlayAttackCombo(speed);yield return effects.PlayCombo(motion,guardian,speed,index=>{if(version!=screenVersion)return;int part=GuxuCombatVfx.DamagePart(e.amount,index);displayed+=part;if(bossMotion)bossMotion.Hit(speed);else UiFeedback.Hit(guardian,1,.14f/speed);sound.Cue(0);var contact=e;contact.amount=part;contact.enemyHp=e.enemyHp+e.amount-displayed;BattleFloat(contact,e.critical?"会心 -":"-");UpdateCombatBars(contact,left,right,leftBar,rightBar,simulation,speed);if(e.kind==GuxuBattleEventKind.Skill)RefreshBattleControls(simulation,skillState,skill,autoButton);AddDisplayedStatistics(contact,brief);},e.kind==GuxuBattleEventKind.Skill&&simulation.Skill==3?new Color(.75f,.35f,1,.9f):new Color(.2f,.9f,1,.9f));
    }else if(!e.playerActor&&bossMotion&&(e.kind==GuxuBattleEventKind.Attack||e.kind==GuxuBattleEventKind.Combo||e.kind==GuxuBattleEventKind.Counter||e.kind==GuxuBattleEventKind.Skill)){
     int index=bossMotion.NextAttack(e.kind==GuxuBattleEventKind.Attack&&simulation.Round%3==0),displayed=0;var clip=bossMotion.Definition.attacks[index];eventLabel.text="敌方 · "+clip.name;
     yield return bossMotion.PlayAttack(index,player,view,speed,hit=>{if(version!=screenVersion)return;int part=GuxuBossAnimations.DamagePart(e.amount,hit,clip.impacts.Length);displayed+=part;motion.PlayHit(speed);sound.Cue(0);var contact=e;contact.amount=part;contact.playerHp=e.playerHp+e.amount-displayed;BattleFloat(contact,e.critical?"会心 -":"-");UpdateCombatBars(contact,left,right,leftBar,rightBar,simulation,speed);AddDisplayedStatistics(contact,brief);});
    }else{
     if(e.playerActor){if(e.kind!=GuxuBattleEventKind.Poison){motion.PlayAttack(speed);combatParticles.Swing(motion);}}else if(e.kind!=GuxuBattleEventKind.Poison)UiFeedback.Hit(guardian,-2,.3f/speed);
     yield return new WaitForSeconds((e.kind==GuxuBattleEventKind.Poison?.18f:GuxuCharacterMotion.AttackImpact)/speed);if(version!=screenVersion)yield break;
     if(e.playerActor){if(bossMotion)bossMotion.Hit(speed);else UiFeedback.Hit(guardian,1,.2f/speed);}else motion.PlayHit(speed);
     combatParticles.Event(e);UpdateCombatBars(e,left,right,leftBar,rightBar,simulation,speed);AddDisplayedStatistics(e,brief);sound.Cue(0);BattleFloat(e,e.kind==GuxuBattleEventKind.Poison?"蚀灵 -":e.kind==GuxuBattleEventKind.Counter?"反击 -":e.critical?"会心 -":"-");yield return new WaitForSeconds(.32f/speed);
    }
    if(version!=screenVersion)yield break;UpdateCombatBars(e,left,right,leftBar,rightBar,simulation,speed);leftStatus.text=e.playerStatus;rightStatus.text=e.enemyStatus;
    RefreshBattleControls(simulation,skillState,skill,autoButton);
   }
  }
  if(version!=screenVersion)yield break;skill.interactable=false;
  if(simulation.Won&&bossMotion)bossMotion.Defeat();
  var defeated=simulation.Won?guardian:player;for(float t=0;t<.4f;t+=Time.deltaTime){if(version!=screenVersion)yield break;defeated.color=new Color(defeated.color.r,defeated.color.g,defeated.color.b,1-t/.4f);yield return null;}
  busy=false;battle=false;ActiveBattle=null;var reward=new GuxuReward();bool paid=simulation.Won&&GuxuStageRules.Settle(Rules,simulation,DateTime.UtcNow,out reward);if(paid)Rules.Store();
  ShowBattleResult(simulation,reward,paid);
 }
 void RefreshBattleControls(GuxuBattle simulation,Text state,Button skill,Button automatic){
  int remaining=Math.Max(0,simulation.SkillCooldown-1);
  state.text=simulation.SkillQueued?"已准备":remaining==0?"下次出手就绪":"冷却 "+remaining+" 回合";
  skill.interactable=!simulation.AutoSkill&&simulation.CanQueueSkill;
  RefreshHudToggle(automatic,simulation.AutoSkill,"自动技能");
  if(combatCooldownRing){combatCooldownRing.enabled=remaining>0&&combatCooldownRing.sprite!=null;combatCooldownRing.fillAmount=remaining/(float)Math.Max(1,GameRules.SkillCooldownTurns-1);}
  if(combatSkillIcon)combatSkillIcon.color=remaining>0?new Color(.58f,.66f,.67f):Color.white;
 }
 void AddDisplayedStatistics(GuxuBattleEvent e,Text brief){
  // NextTurn already contains future hits; count only the amount presented at this contact.
  if(e.kind==GuxuBattleEventKind.Heal){if(e.playerActor)displayedHealingDone+=e.amount;}
  else if(e.kind==GuxuBattleEventKind.Attack||e.kind==GuxuBattleEventKind.Combo||e.kind==GuxuBattleEventKind.Counter||e.kind==GuxuBattleEventKind.Skill||e.kind==GuxuBattleEventKind.Poison){if(e.playerActor)displayedDamageDone+=e.amount;else displayedDamageTaken+=e.amount;}
  brief.text="输出 "+displayedDamageDone+"  承伤 "+displayedDamageTaken+"  治疗 "+displayedHealingDone;
 }
 static string UnlockMessage(int id)=>id==5?"新开放：槽位锻造 / 淬兵遗窟":id==8?"新开放：聚灵洞天 / 修为与突破石":id==10?"新开放：三系天赋 / 悟道幻境":id==15?"新开放：通天古塔":id==20?"新开放：灵玉镶嵌 / 灵玉秘境":id==40?"新开放：第二灵玉孔":"";
 void UpdateCombatBars(GuxuBattleEvent e,Text left,Text right,Image lb,Image rb,GuxuBattle sim,int speed){left.text=e.playerHp+" / "+sim.Player.hp;right.text=e.enemyHp+" / "+sim.Enemy.hp;if(combatPlayerStatus)combatPlayerStatus.text=e.playerStatus;if(combatEnemyStatus)combatEnemyStatus.text=e.enemyStatus;UiFeedback.Bar(lb.rectTransform,PlayerHealthWidth*e.playerHp/sim.Player.hp,.12f/speed);UiFeedback.Bar(rb.rectTransform,EnemyHealthWidth*e.enemyHp/sim.Enemy.hp,.12f/speed);if(playerShieldBar)playerShieldBar.rectTransform.sizeDelta=new Vector2(PlayerHealthWidth*e.playerShield/sim.Player.hp,7);if(enemyShieldBar)enemyShieldBar.rectTransform.sizeDelta=new Vector2(EnemyHealthWidth*e.enemyShield/sim.Enemy.hp,6);if(combatParticles)combatParticles.SetShields(e.playerShield,e.enemyShield);}
 void BattleFloat(GuxuBattleEvent e,string prefix){if(e.amount<=0)return;bool restore=e.kind==GuxuBattleEventKind.Heal||e.kind==GuxuBattleEventKind.Shield;bool onPlayer=restore?e.playerActor:!e.playerActor;float offset=combatArena?-combatArena.anchoredPosition.y:0;UiFeedback.Float(Txt(prefix+e.amount,view,onPlayer?25:425,(onPlayer?570:550)+offset,300,55,e.critical?38:31,restore?new Color(.35f,1,.65f):gold),65,.65f/fightSpeed);}
 void PulseBattle(Image target,Color color){var r=Rect("Battle impact",view,0,0,120,120);r.position=target.rectTransform.TransformPoint(target.rectTransform.rect.center);var glow=r.gameObject.AddComponent<GuxuSoftVfx>();glow.raycastTarget=false;glow.color=new Color(color.r,color.g,color.b,.7f);UiFeedback.Float(glow,28,.35f/fightSpeed);}
}
}

