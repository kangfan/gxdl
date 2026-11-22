using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using UnityEngine;

namespace Gxdl {
[Serializable] public class GuxuCombatTraits {
 public int criticalPercent=15,comboPercent,healPercent,counterPercent,shieldPercent,armorBreakPercent,dotPercent,lifeStealPercent,healingBonus;
 public int comboDamagePercent=60,counterDamagePercent=50;
 public GuxuCombatTraits Copy()=>(GuxuCombatTraits)MemberwiseClone();
}
[Serializable] public class GuxuStage {
 public int id,region,room,sourceStageId,sourceMonsterId,sourceMonsterLevelId,sourceBackgroundId,sealReward,dustReward;
 public int enemyScalePermille=1000,mode,contentId,difficulty=1,minProgress=1,waves=1,maxRounds=20,archetype;
 public bool boss;
 public string regionName,name,guardianName,dropBias,sourceRewards,sceneKey,enemyKey,mechanic;
 public Stats enemyStats;
 public GuxuCombatTraits enemyTraits;
}
[Serializable] public class GuxuRegion {public int id;public string name,key,bossName,description,palette,story;}
[Serializable] public class GuxuStageConfig {
 public int version;public string source,localRulesNote;public GuxuStage[] stages,dungeons,tower;public GuxuRegion[] regions;
 public static GuxuStageConfig Load(){var asset=Resources.Load<TextAsset>("Game/Guxu/Stages");if(asset==null)throw new InvalidOperationException("Missing Game/Guxu/Stages.json");return JsonConvert.DeserializeObject<GuxuStageConfig>(asset.text);}
 public GuxuStage ForProgress(int progress)=>stages[Math.Max(0,Math.Min(progress-1,stages.Length-1))];
 public GuxuStage Dungeon(int kind,int tier)=>dungeons.First(x=>x.contentId==kind&&x.difficulty==tier);
}
public enum GuxuBattleEventKind {Attack,Combo,Heal,Counter,Skill,Shield,Poison,Break,Charge,Phase,Wave}
public struct GuxuBattleEvent {
 public int round,amount,playerHp,enemyHp,playerShield,enemyShield,wave;public bool playerActor,critical;public GuxuBattleEventKind kind;public string label,playerStatus,enemyStatus;
}
public sealed class GuxuBattle {
 public readonly GuxuStage Stage;
 public readonly Stats Player,Enemy;
 public readonly int StartedProgress;
 public int PlayerHp {get;private set;}public int EnemyHp {get;private set;}public int Round {get;private set;}public int Wave {get;private set;}=1;
 public int PlayerShield {get;private set;}public int EnemyShield {get;private set;}
 public int SkillCooldown {get;private set;}public int Skill {get;private set;}
 public int DamageDone {get;private set;}public int DamageTaken {get;private set;}public int HealingDone {get;private set;}
 public bool AutoSkill=true;
 public bool Completed=>PlayerHp==0||(EnemyHp==0&&Wave>=Stage.waves)||Round>=Stage.maxRounds;
 public bool Won=>Completed&&PlayerHp>0&&EnemyHp==0&&Wave>=Stage.waves;
 public bool Settled {get;internal set;}
 public string RewardReceiptId {get;internal set;}
 public bool SkillQueued {get;private set;}
 public bool CanQueueSkill=>!Completed&&!SkillQueued&&SkillCooldown<=1;
 public string PlayerStatus=>"护盾 "+PlayerShield+(playerPoison>0?" · 蚀灵 "+playerPoison+"回合":"")+(playerBreak>0?" · 破甲 "+playerBreak+"回合":"");
 public string EnemyStatus=>"护盾 "+EnemyShield+(enemyPoison>0?" · 蚀灵 "+enemyPoison+"回合":"")+(enemyBreak>0?" · 破甲 "+enemyBreak+"回合":"")+(enraged?" · 狂暴":"")+(Stage.boss?" · "+(Round%3==2?"下回合重击":"重击每3回合"):"");
 readonly System.Random random;
 readonly GuxuCombatTraits playerTraits,enemyTraits;
 int playerPoison,enemyPoison,playerDot,enemyDot,playerBreak,enemyBreak;bool enraged;
 public GuxuBattle(Stats player,Stats enemy,GuxuStage stage,int progress,int seed,GuxuCombatTraits traits=null,int skill=0,bool autoSkill=true){
  if(player.hp<=0||enemy.hp<=0)throw new ArgumentOutOfRangeException("Combatants need positive maximum HP");
  Player=player;Enemy=enemy;Stage=stage??throw new ArgumentNullException(nameof(stage));StartedProgress=progress;PlayerHp=player.hp;EnemyHp=enemy.hp;random=new System.Random(seed);playerTraits=(traits??new GuxuCombatTraits()).Copy();enemyTraits=(stage.enemyTraits??new GuxuCombatTraits()).Copy();Skill=Math.Max(0,Math.Min(3,skill));AutoSkill=autoSkill;
  PlayerShield=Player.hp*Math.Min(50,playerTraits.shieldPercent)/100;EnemyShield=Enemy.hp*Math.Min(50,enemyTraits.shieldPercent)/100;
 }
 public bool QueueSkill(){if(!CanQueueSkill)return false;SkillQueued=true;return true;}
 public List<GuxuBattleEvent> NextTurn(){
  var events=new List<GuxuBattleEvent>();if(Completed)return events;
  if(EnemyHp==0){Wave++;EnemyHp=Enemy.hp;EnemyShield=Enemy.hp*enemyTraits.shieldPercent/100;enemyPoison=enemyBreak=0;enraged=false;Add(events,false,GuxuBattleEventKind.Wave,0,false,"第 "+Wave+" 波敌人");}
  Round++;if(SkillCooldown>0)SkillCooldown--;
  if(playerPoison>0){playerPoison--;Hurt(false,playerDot,GuxuBattleEventKind.Poison,false,events,"蚀灵");}
  if(PlayerHp>0&&enemyPoison>0){enemyPoison--;Hurt(true,enemyDot,GuxuBattleEventKind.Poison,false,events,"蚀灵");}
  if(PlayerHp<=0||EnemyHp<=0)return events;
  if(Stage.boss&&!enraged&&EnemyHp<=Enemy.hp/2){enraged=true;Add(events,false,GuxuBattleEventKind.Phase,0,false,"首领进入狂暴 · 伤害提升25%");}
  bool first=Player.speed>=Enemy.speed;Act(first,events);if(PlayerHp>0&&EnemyHp>0)Act(!first,events);
  if(playerBreak>0)playerBreak--;if(enemyBreak>0)enemyBreak--;return events;
 }
 void Act(bool player,List<GuxuBattleEvent> events){
  var traits=player?playerTraits:enemyTraits;
  bool usefulAuto=Skill==2?PlayerHp<=Player.hp*3/4:Skill!=1||PlayerShield<=Player.hp/5;
  if(player&&SkillCooldown==0&&(SkillQueued||AutoSkill&&usefulAuto)){
   SkillQueued=false;SkillCooldown=GameRules.SkillCooldownTurns;
   if(Skill==1){int shield=Math.Min(Player.hp/2-PlayerShield,Player.hp*GameRules.ShieldHpPercent/100);PlayerShield+=shield;Add(events,true,GuxuBattleEventKind.Shield,shield,false,"玄甲盾");}
   else if(Skill==2)Heal(true,Player.hp*GameRules.HealHpPercent/100,events,"回春术");
   else {if(Skill==3){enemyBreak=GameRules.ArmorBreakTurns;Add(events,true,GuxuBattleEventKind.Break,0,false,"碎甲剑 · 破甲"+GameRules.ArmorBreakTurns+"回合");}Strike(true,GuxuBattleEventKind.Skill,Skill==0?GameRules.SwordDamagePercent:GameRules.BreakSwordDamagePercent,events,GameRules.SkillNames[Skill]);}
  }
  if(PlayerHp<=0||EnemyHp<=0)return;
  int percent=!player&&Stage.boss&&Round%3==0?175:100;
  if(!player&&Stage.boss&&Round%3==2)Add(events,false,GuxuBattleEventKind.Charge,0,false,"蓄力 · 下回合重击");
  Strike(player,GuxuBattleEventKind.Attack,percent,events,percent>100?"蓄力重击":"普攻");
  if(PlayerHp>0&&EnemyHp>0&&Roll(traits.comboPercent))Strike(player,GuxuBattleEventKind.Combo,traits.comboDamagePercent,events,"连击");
  if(PlayerHp>0&&EnemyHp>0&&traits.healPercent>0)Heal(player,(player?Player.hp:Enemy.hp)*traits.healPercent/100,events,"复苏");
 }
 void Heal(bool player,int amount,List<GuxuBattleEvent> events,string label){int max=player?Player.hp:Enemy.hp,current=player?PlayerHp:EnemyHp;var traits=player?playerTraits:enemyTraits;int healed=Math.Min(max-current,amount*(100+traits.healingBonus)/100);if(healed<=0)return;if(player){PlayerHp+=healed;HealingDone+=healed;}else EnemyHp+=healed;Add(events,player,GuxuBattleEventKind.Heal,healed,false,label);}
 void Strike(bool player,GuxuBattleEventKind kind,int percent,List<GuxuBattleEvent> events,string label){
  var source=player?Player:Enemy;var target=player?Enemy:Player;var traits=player?playerTraits:enemyTraits;bool critical=Roll(traits.criticalPercent);
  int penetration=traits.armorBreakPercent+((player?enemyBreak:playerBreak)>0?50:0);target.defense=target.defense*(100-Math.Min(80,penetration))/100;
  if(!player&&enraged)percent=percent*125/100;
  int damage=GuxuStageRules.Damage(source,target,percent,critical),dealt=Hurt(player,damage,kind,critical,events,label);
  if(PlayerHp<=0||EnemyHp<=0)return;
  if(traits.lifeStealPercent>0)Heal(player,dealt*traits.lifeStealPercent/100,events,"吸收");
  if(traits.dotPercent>0){if(player){enemyPoison=3;enemyDot=Math.Max(1,Enemy.hp*traits.dotPercent/100);}else {playerPoison=3;playerDot=Math.Max(1,Player.hp*traits.dotPercent/100);}}
  var defender=player?enemyTraits:playerTraits;
  // A counter is one reaction, never a recursive counter chain.
  if(Roll(defender.counterPercent))Hurt(!player,GuxuStageRules.Damage(target,source,defender.counterDamagePercent),GuxuBattleEventKind.Counter,false,events,"反击");
 }
 int Hurt(bool player,int amount,GuxuBattleEventKind kind,bool critical,List<GuxuBattleEvent> events,string label){int shield=player?EnemyShield:PlayerShield,absorbed=Math.Min(shield,amount);amount-=absorbed;if(player)EnemyShield-=absorbed;else PlayerShield-=absorbed;int actual=Math.Min(player?EnemyHp:PlayerHp,amount);if(player){EnemyHp-=actual;DamageDone+=actual;}else{PlayerHp-=actual;DamageTaken+=actual;}Add(events,player,kind,actual,critical,label+(absorbed>0?" · 护盾吸收"+absorbed:""));return actual;}
 bool Roll(int percent)=>percent>0&&random.Next(100)<Math.Min(100,percent);
 void Add(List<GuxuBattleEvent> events,bool player,GuxuBattleEventKind kind,int amount,bool critical,string label){events.Add(new GuxuBattleEvent{round=Round,amount=amount,playerHp=PlayerHp,enemyHp=EnemyHp,playerShield=PlayerShield,enemyShield=EnemyShield,wave=Wave,playerActor=player,kind=kind,critical=critical,label=label,playerStatus=PlayerStatus,enemyStatus=EnemyStatus});}
}
public static class GuxuStageRules {
 public static readonly string[] DungeonNames={"淬兵遗窟","灵玉秘境","悟道幻境","聚灵洞天"};
 public static readonly int[] DungeonUnlocks={5,20,10,8};
 public static Stats Enemy(GuxuStage stage,OriginalConfig config){
  if(stage.enemyStats.hp>0)return stage.enemyStats;
  var m=config.monsters.First(x=>x.id==stage.sourceMonsterId);var l=config.monsterLevels.First(x=>x.id==stage.sourceMonsterLevelId);int scale=Math.Max(1,stage.enemyScalePermille);
  return new Stats{attack=(int)((long)int.Parse(l.attackBase)*m.attackEmp/1000*scale/1000),hp=(int)((long)int.Parse(l.hpBase)*m.hpEmp/1000*scale/1000),defense=(int)((long)int.Parse(l.defBase)*int.Parse(m.defEmp)/1000*scale/1000),speed=int.Parse(l.speedBase)};
 }
 public static int Damage(Stats source,Stats target,int percent=100,bool critical=false)=>(int)Math.Max(1,Math.Min(int.MaxValue,Math.Max(1L,(long)source.attack-target.defense)*Math.Max(0,percent)/100*(critical?2:1)));
 public static bool CanEnter(GuxuStage stage,int progress)=>stage!=null&&stage.mode==0&&stage.id<=progress;
 public static int RequiredProgress(GuxuStage stage)=>stage.mode==1&&stage.contentId>=0&&stage.contentId<DungeonUnlocks.Length?Math.Max(stage.minProgress,DungeonUnlocks[stage.contentId]+(stage.difficulty-1)*20+1):stage.minProgress;
 public static bool CanEnter(GuxuStage stage,SaveData s){if(stage==null)return false;if(stage.mode==2&&stage.difficulty<s.towerFloor)return stage.difficulty<=100;if(RequiredProgress(stage)>s.stage)return false;if(stage.mode==0)return stage.id<=s.stage;if(stage.mode==2)return stage.difficulty<=s.towerFloor&&stage.difficulty<=100;return stage.contentId>=0&&stage.contentId<4&&stage.difficulty<=10&&stage.difficulty<=s.dungeonClears[stage.contentId]+1;}
 public static GuxuBattle Begin(Stats player,GuxuStage stage,OriginalConfig config,int progress,int seed,GuxuCombatTraits traits=null){if(!CanEnter(stage,progress))throw new InvalidOperationException("Stage is locked");return new GuxuBattle(player,Enemy(stage,config),stage,progress,seed,traits);}
 public static GuxuBattle Begin(GameRules rules,GuxuStage stage,int seed){rules.EnsureExpansion();if(!CanEnter(stage,rules.Save))throw new InvalidOperationException("Stage is locked");return new GuxuBattle(rules.Attributes,Enemy(stage,rules.Config),stage,rules.Save.stage,seed,rules.CombatTraits,rules.Save.activeSkill,rules.Save.autoSkill);}
 public static GuxuReward Reward(GuxuStage stage,bool first){
  if(stage.mode==0)return new GuxuReward{seals=first?5+stage.region*2:2+stage.region/2,dust=first?50+stage.id*5:15+stage.region*5,exp=first?20+stage.region*10:5,ore=first&&stage.id>=5?2+stage.region:0,insight=first&&stage.id>=10?1+stage.region/3:0,stones=first&&stage.id%10==0?1+stage.region/3:0};
  int tier=stage.difficulty;
  if(stage.mode==2)return first?new GuxuReward{seals=5+tier/10,dust=100+tier*15,ore=8+tier/3,insight=4+tier/5,stones=tier%10==0?3:0}:new GuxuReward();
  var r=new GuxuReward{dust=50+tier*30};int bonus=first?2:1;
  if(stage.contentId==0)r.ore=(15+tier*8)*bonus;
  if(stage.contentId==1){r.gemIndex=(tier-1)%4*6+Math.Min(3,(tier-1)/3);r.gems=3*bonus;}
  if(stage.contentId==2)r.insight=(8+tier*4)*bonus;
  if(stage.contentId==3){r.exp=(150+tier*80)*bonus;r.stones=(2+tier/2)*bonus;}
  return r;
 }
 public static bool Settle(GameRules rules,GuxuBattle battle,DateTime utc,out GuxuReward reward){
  reward=new GuxuReward();rules.RefreshDaily(utc);var s=rules.Save;
  if(battle==null||!battle.Won||battle.Settled||s.stage!=battle.StartedProgress||!CanEnter(battle.Stage,s))return false;
  var stage=battle.Stage;bool first=stage.mode==0?!s.firstClears[stage.id-1]:stage.mode==2?stage.difficulty==s.towerFloor:stage.difficulty>s.dungeonClears[stage.contentId];
  if(stage.mode==1&&s.dungeonClaims[stage.contentId]>=3)return false;
  reward=Reward(stage,first);battle.RewardReceiptId=rules.GrantRecorded(reward,stage.mode==0?"main":stage.mode==1?"dungeon":"tower",stage.id,stage.name);
  if(stage.mode==0){s.firstClears[stage.id-1]=true;if(stage.id==s.stage)s.stage=Math.Min(201,s.stage+1);}
  if(stage.mode==1){s.dungeonClaims[stage.contentId]++;s.dungeonClears[stage.contentId]=Math.Max(s.dungeonClears[stage.contentId],stage.difficulty);}
  if(stage.mode==2&&first)s.towerFloor=Math.Min(101,s.towerFloor+1);
  battle.Settled=true;return true;
 }
 public static bool Sweep(GameRules rules,GuxuStage stage,DateTime utc,out GuxuReward reward){reward=new GuxuReward();rules.RefreshDaily(utc);if(stage==null||stage.mode!=1||!CanEnter(stage,rules.Save)||rules.Save.dungeonClears[stage.contentId]<stage.difficulty||rules.Save.dungeonClaims[stage.contentId]>=3)return false;reward=Reward(stage,false);rules.GrantRecorded(reward,"dungeon",stage.id,stage.name+" · 速通");rules.Save.dungeonClaims[stage.contentId]++;return true;}
 // Legacy QA callers still use this entry point for main-story battles.
 public static bool TryApplyVictory(SaveData save,GuxuBattle battle,int stageCount){if(save==null||battle==null||!battle.Won||battle.Settled||battle.Stage.mode!=0||save.stage!=battle.StartedProgress||!CanEnter(battle.Stage,save.stage))return false;save.peaches=checked(save.peaches+battle.Stage.sealReward);save.coins=checked(save.coins+battle.Stage.dustReward);if(battle.Stage.id==save.stage)save.stage=Math.Min(stageCount+1,save.stage+1);battle.Settled=true;return true;}
 public static void SelfTest(OriginalConfig original,GuxuStageConfig config){if(config.version!=2||config.stages.Length!=200||config.dungeons.Length!=40||config.tower.Length!=100)throw new Exception("Expansion catalog count");foreach(var s in config.stages.Concat(config.dungeons).Concat(config.tower))if(Enemy(s,original).hp<=0||s.waves<1||s.maxRounds<1)throw new Exception("Invalid encounter "+s.id);}
}
}

