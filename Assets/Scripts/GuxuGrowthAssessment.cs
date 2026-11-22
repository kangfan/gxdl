using System;
using System.Collections.Generic;

namespace Gxdl {
public sealed class GuxuChallengeAssessment {
 public int wins,samples;public float meanRounds,meanWinHealth;
 public string Label=>wins*100>=samples*85?"较稳":wins*100>=samples*40?"可挑战":"建议养成";
}
public partial class GameRules {
 public GuxuChallengeAssessment AssessChallenge(GuxuStage stage){
  var result=new GuxuChallengeAssessment{samples=24};var stats=Attributes;var traits=CombatTraits;var enemy=GuxuStageRules.Enemy(stage,Config);
  for(int i=0;i<result.samples;i++){var battle=new GuxuBattle(stats,enemy,stage,Save.stage,17001+i,traits,Save.activeSkill,true);while(!battle.Completed)battle.NextTurn();result.meanRounds+=battle.Round;if(battle.Won){result.wins++;result.meanWinHealth+=(float)battle.PlayerHp/battle.Player.hp;}}
  result.meanRounds/=result.samples;if(result.wins>0)result.meanWinHealth/=result.wins;return result;
 }
 public string BreakthroughRequirement=>Save.level>=Config.realms.Length?"已达境界上限":Save.exp<ExpNeeded?"还需修为 "+(ExpNeeded-Save.exp)+"，可破封或挑战聚灵洞天":Save.breakthroughStones<BreakthroughCost?"还需突破石 "+(BreakthroughCost-Save.breakthroughStones)+"，可挑战聚灵洞天":"可以突破";
 public static string GrowthChange(Stats before,Stats after,GuxuCombatTraits oldTraits,GuxuCombatTraits now){
  var lines=new List<string>();AddChange(lines,"攻击力",before.attack,after.attack);AddChange(lines,"生命值",before.hp,after.hp);AddChange(lines,"防御力",before.defense,after.defense);AddChange(lines,"敏捷度",before.speed,after.speed);
  int[] a={oldTraits.criticalPercent,oldTraits.comboPercent,oldTraits.counterPercent,oldTraits.healPercent,oldTraits.shieldPercent,oldTraits.armorBreakPercent,oldTraits.lifeStealPercent,oldTraits.dotPercent,oldTraits.healingBonus},b={now.criticalPercent,now.comboPercent,now.counterPercent,now.healPercent,now.shieldPercent,now.armorBreakPercent,now.lifeStealPercent,now.dotPercent,now.healingBonus};
  string[] labels={"会心","连击","反击","回合恢复","开场护盾","破甲","吸收","蚀灵","治疗加成"};for(int i=0;i<a.Length;i++)if(a[i]!=b[i])lines.Add(labels[i]+" "+a[i]+"%→"+b[i]+"%");int delta=after.Power-before.Power;string power="基础战力 "+(delta>0?"+":"")+delta;return lines.Count==0?"操作成功，数据已保存，当前属性不变。":power+"\n"+string.Join(" · ",lines)+"。";
 }
 static void AddChange(List<string> lines,string name,int before,int after){if(before!=after)lines.Add(name+" "+before+"→"+after);}
 public static string GearAffix(Gear gear){int strength=1+gear.quality/2;switch(gear.affix){case 1:return "会心+"+strength+"%";case 2:return "连击+"+strength+"%";case 3:return "反击+"+strength+"%";case 4:return "治疗+"+(strength*2)+"%";case 5:return "破甲+"+strength+"%";case 6:return "蚀灵+1%";default:return "无词条";}}
}
}
