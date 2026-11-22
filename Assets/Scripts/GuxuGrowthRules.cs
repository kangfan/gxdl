using System;
using System.IO;
using System.Linq;
using UnityEngine;

namespace Gxdl {
public partial class GameRules {
 public static readonly string[] GemNames={"赤曜·攻击力","碧生·生命值","玄罡·防御力","紫电·会心"};
 public static readonly string[] SkillNames={"御剑诀","玄甲盾","回春术","碎甲剑"};
 public const int SkillCooldownTurns=3, SwordDamagePercent=180, ShieldHpPercent=30, HealHpPercent=25, BreakSwordDamagePercent=150, ArmorBreakTurns=3;
 public static readonly string[] SkillDetails={"造成"+SwordDamagePercent+"%攻击力的伤害，冷却"+SkillCooldownTurns+"回合。","获得"+ShieldHpPercent+"%最大生命值的护盾，冷却"+SkillCooldownTurns+"回合。护盾累计上限为50%最大生命值。","恢复"+HealHpPercent+"%最大生命值，冷却"+SkillCooldownTurns+"回合。","造成"+BreakSwordDamagePercent+"%攻击力的伤害，并使目标破甲"+ArmorBreakTurns+"回合，冷却"+SkillCooldownTurns+"回合。"};
 public static readonly string[] TalentNames={"剑意","会心","追击","锋芒","破阵","剑雨","灵刃","剑心","坚壁","铁骨","回锋","守势","玄甲","荆棘","不屈","镇岳","养气","回春","轻身","滋养","灵息","润脉","蚀灵","长生"};
 public static readonly string[] TalentDetails={"攻击力+2%","会心+2%","连击+2%","会心+2%","破甲+3%","连击+2%","吸收+1%","攻击力+3%","生命值+2%","防御力+3%","反击+2%","防御力+3%","开场护盾+2%","反击+3%","生命值+3%","开场护盾+3%","生命值+2%","回合恢复+1%","敏捷度+3%","治疗效果+5%","吸收+1%","生命值+3%","持续伤害+1%","治疗效果+5%"};
 public static readonly string[] AffixNames={"无词条","会心","连击","反击","复苏","破甲","蚀灵"};
 public int HighestClear=>Math.Min(200,Save.stage-1);
 public int BreakthroughCost=>Save.level>=10&&Save.level%10==0?1+Save.level/10:0;
 public int SocketCount=>HighestClear>=40?2:1;
 public bool GrowthUnlocked(int group)=>HighestClear>=(group==0?5:group==1?10:20);
 public static void ValidateSave(SaveData s){
  if(s==null||s.version!=1||s.level<1||s.level>100||s.stage<1||s.stage>201||s.treeLevel<1||s.treeLevel>20||s.skin<0||s.skin>2||s.expansionSchema>1||s.towerFloor<1||s.towerFloor>101)throw new InvalidDataException("Unsupported save");
  if(s.peaches<0||s.coins<0||s.exp<0||s.forgeOre<0||s.insight<0||s.breakthroughStones<0)throw new InvalidDataException("Negative resource balance");
  CheckArray(s.forgeLevels,12,30);CheckArray(s.talents,24,3);CheckArray(s.gemSlots,24,24);CheckArray(s.gemInventory,24,int.MaxValue);CheckArray(s.dungeonClears,4,10);CheckArray(s.dungeonClaims,4,3);
  ValidateRewardReceipts(s);
 }
 static void CheckArray(int[] array,int length,int max){if(array!=null&&(array.Length!=length||array.Any(x=>x<0||x>max)))throw new InvalidDataException("Invalid growth data");}
 static int[] Resize(int[] source,int count){var result=new int[count];if(source!=null)Array.Copy(source,result,Math.Min(source.Length,count));return result;}
 static bool[] Resize(bool[] source,int count){var result=new bool[count];if(source!=null)Array.Copy(source,result,Math.Min(source.Length,count));return result;}
 public void EnsureExpansion(){
  if(Save.forgeLevels==null||Save.forgeLevels.Length!=12)Save.forgeLevels=Resize(Save.forgeLevels,12);
  if(Save.talents==null||Save.talents.Length!=24)Save.talents=Resize(Save.talents,24);
  if(Save.gemSlots==null||Save.gemSlots.Length!=24)Save.gemSlots=Resize(Save.gemSlots,24);
  if(Save.gemInventory==null||Save.gemInventory.Length!=24)Save.gemInventory=Resize(Save.gemInventory,24);
  if(Save.dungeonClears==null||Save.dungeonClears.Length!=4)Save.dungeonClears=Resize(Save.dungeonClears,4);
  if(Save.dungeonClaims==null||Save.dungeonClaims.Length!=4)Save.dungeonClaims=Resize(Save.dungeonClaims,4);
  if(Save.firstClears==null||Save.firstClears.Length!=200)Save.firstClears=Resize(Save.firstClears,200);
  if(Save.chapterChests==null||Save.chapterChests.Length!=10)Save.chapterChests=Resize(Save.chapterChests,10);
  if(Save.expansionSchema==0){for(int i=0;i<HighestClear;i++)Save.firstClears[i]=true;Save.expansionSchema=1;}
  Save.activeSkill=Mathf.Clamp(Save.activeSkill,0,3);
 }
 public static string Day(DateTime utc)=>utc.ToUniversalTime().AddHours(8).ToString("yyyy-MM-dd",System.Globalization.CultureInfo.InvariantCulture);
 public void RefreshDaily(DateTime utc){EnsureExpansion();string day=Day(utc);if(string.CompareOrdinal(day,Save.rewardDay)>0){Save.rewardDay=day;Array.Clear(Save.dungeonClaims,0,4);}}
 public int ForgeCost(int slot)=>5+Save.forgeLevels[slot]*3;
 public int ForgeDustCost(int slot)=>ForgeCost(slot)*5;
 public static Stats ForgeBonus(int level)=>new Stats{attack=level*8,hp=level*60,defense=level*2};
 public bool CanForge(int slot)=>slot>=0&&slot<Save.forgeLevels.Length&&GrowthUnlocked(0)&&Save.forgeLevels[slot]<ForgeLimit&&Save.forgeOre>=ForgeCost(slot)&&Save.coins>=ForgeDustCost(slot);
 public int ForgeLimit=>Math.Min(30,5+HighestClear/8);
 public bool Forge(int slot){EnsureExpansion();if(!CanForge(slot))return false;Save.forgeOre-=ForgeCost(slot);Save.coins-=ForgeDustCost(slot);Save.forgeLevels[slot]++;return true;}
 public int TalentCost(int node)=>2+node%8+Save.talents[node]*2;
 public bool CanTrainTalent(int node)=>node>=0&&node<24&&GrowthUnlocked(1)&&Save.talents[node]<3&&(node%8==0||Save.talents[node-1]>0)&&Save.insight>=TalentCost(node);
 public bool TrainTalent(int node){EnsureExpansion();if(!CanTrainTalent(node)||Save.insight<TalentCost(node))return false;Save.insight-=TalentCost(node);Save.talents[node]++;return true;}
 public void ResetTalents(){EnsureExpansion();int refund=0;for(int n=0;n<24;n++){for(int r=0;r<Save.talents[n];r++)refund+=2+n%8+r*2;Save.talents[n]=0;}Save.insight=checked(Save.insight+refund);}
 // Each color has six levels; equipment stores inventory index + 1 (zero means empty).
 public bool CanSocketGem(int slot,int socket,int inventoryIndex)=>GrowthUnlocked(2)&&slot>=0&&slot<12&&socket>=0&&socket<SocketCount&&inventoryIndex>=0&&inventoryIndex<24&&Save.gemInventory[inventoryIndex]>0;
 public bool SocketGem(int slot,int socket,int inventoryIndex){EnsureExpansion();if(!CanSocketGem(slot,socket,inventoryIndex))return false;int index=slot*2+socket,old=Save.gemSlots[index];Save.gemInventory[inventoryIndex]--;if(old>0)Save.gemInventory[old-1]++;Save.gemSlots[index]=inventoryIndex+1;return true;}
 public bool UnsocketGem(int slot,int socket){EnsureExpansion();if(slot<0||slot>=12||socket<0||socket>=2)return false;int index=slot*2+socket,old=Save.gemSlots[index];if(old<1||old>24)return false;Save.gemInventory[old-1]++;Save.gemSlots[index]=0;return true;}
 public bool CanCombineGem(int index)=>GrowthUnlocked(2)&&index>=0&&index<24&&index%6<5&&Save.gemInventory[index]>=3;
 public bool CombineGem(int index){EnsureExpansion();if(!CanCombineGem(index))return false;Save.gemInventory[index]-=3;Save.gemInventory[index+1]++;return true;}
 public static string GemLabel(int encoded)=>encoded<1||encoded>24?"空孔":GemNames[(encoded-1)/6]+" "+((encoded-1)%6+1)+"级";
 public static int GemValue(int level)=>1<<(level-1);
 public string GemEffect(int encoded){if(encoded<1||encoded>24)return "可镶嵌灵玉";int index=encoded-1,v=GemValue(index%6+1);return index/6==0?"攻击力 +"+v*8:index/6==1?"生命值 +"+v*65:index/6==2?"防御力 +"+v*3:"会心 +"+Math.Min(8,index%6+1)+"%";}
 Stats CultivationAttributes(Stats s){
  foreach(int lv in Save.forgeLevels){var bonus=ForgeBonus(lv);s.attack+=bonus.attack;s.hp+=bonus.hp;s.defense+=bonus.defense;}
  foreach(int encoded in Save.gemSlots){if(encoded<1||encoded>24)continue;int i=encoded-1,v=GemValue(i%6+1);switch(i/6){case 0:s.attack+=v*8;break;case 1:s.hp+=v*65;break;case 2:s.defense+=v*3;break;}}
  var t=Save.talents;s.attack=s.attack*(100+t[0]*2+t[7]*3)/100;s.hp=s.hp*(100+t[8]*2+t[14]*3+t[16]*2+t[21]*3)/100;s.defense=s.defense*(100+t[9]*3+t[11]*3)/100;s.speed=s.speed*(100+t[18]*3)/100;return s;
 }
 public GuxuCombatTraits CombatTraits {
  get {EnsureExpansion();var t=Save.talents;var a=new GuxuCombatTraits {criticalPercent=15+t[1]*2+t[3]*2,comboPercent=t[2]*2+t[5]*2,counterPercent=t[10]*2+t[13]*3,healPercent=t[17],shieldPercent=t[12]*2+t[15]*3,armorBreakPercent=t[4]*3,lifeStealPercent=t[6]+t[20],dotPercent=t[22],healingBonus=t[19]*5+t[23]*5};
   foreach(var g in Save.gear){int strength=1+g.quality/2;switch(g.affix){case 1:a.criticalPercent+=strength;break;case 2:a.comboPercent+=strength;break;case 3:a.counterPercent+=strength;break;case 4:a.healingBonus+=strength*2;break;case 5:a.armorBreakPercent+=strength;break;case 6:a.dotPercent++;break;}}
   foreach(int gem in Save.gemSlots)if(gem>=19&&gem<=24)a.criticalPercent+=(gem-1)%6+1;
   a.criticalPercent=Math.Min(75,a.criticalPercent);a.comboPercent=Math.Min(60,a.comboPercent);a.counterPercent=Math.Min(50,a.counterPercent);a.dotPercent=Math.Min(6,a.dotPercent);a.armorBreakPercent=Math.Min(50,a.armorBreakPercent);return a;
  }
 }
 public bool CanClaimChapter(int chapter)=>chapter>=0&&chapter<10&&HighestClear>=(chapter+1)*20&&!Save.chapterChests[chapter];
 public static GuxuReward ChapterReward(int chapter){if(chapter<0||chapter>=10)throw new ArgumentOutOfRangeException(nameof(chapter));return new GuxuReward{seals=25+chapter*5,dust=400+chapter*150,ore=25+chapter*10,insight=15+chapter*5,stones=3+chapter};}
 public bool ClaimChapter(int chapter){EnsureExpansion();if(!CanClaimChapter(chapter))return false;GrantRecorded(ChapterReward(chapter),"chapter",chapter,"第"+(chapter+1)+"章任务宝匣");Save.chapterChests[chapter]=true;return true;}
 public void Grant(GuxuReward r){
  ValidateReward(r);
  // Preflight every addition before committing any balance.
  int p=checked(Save.peaches+Math.Max(0,r.seals)),c=checked(Save.coins+Math.Max(0,r.dust)),o=checked(Save.forgeOre+Math.Max(0,r.ore)),i=checked(Save.insight+Math.Max(0,r.insight)),e=checked(Save.exp+Math.Max(0,r.exp)),s=checked(Save.breakthroughStones+Math.Max(0,r.stones));
  int gems=r.gemIndex>=0?checked(Save.gemInventory[r.gemIndex]+r.gems):0;
  Save.peaches=p;Save.coins=c;Save.forgeOre=o;Save.insight=i;Save.exp=e;Save.breakthroughStones=s;if(r.gemIndex>=0)Save.gemInventory[r.gemIndex]=gems;
 }
}
[Serializable] public class GuxuReward {
 public int seals,dust,ore,insight,exp,stones,gems,gemIndex=-1;
 public override string ToString(){var parts=new System.Collections.Generic.List<string>();if(seals>0)parts.Add("破封符 +"+seals);if(dust>0)parts.Add("灵尘 +"+dust);if(ore>0)parts.Add("玄铁 +"+ore);if(insight>0)parts.Add("悟道点 +"+insight);if(exp>0)parts.Add("修为 +"+exp);if(stones>0)parts.Add("突破石 +"+stones);if(gems>0)parts.Add(GameRules.GemLabel(gemIndex+1)+" ×"+gems);return string.Join("  ",parts);}
}
}
