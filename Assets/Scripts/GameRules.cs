using System;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using UnityEngine;

namespace Gxdl {
[Serializable] public class RealmRow { public int id,type,littleType; public string displayName,demonicMax,demonicEachDream,attackBase,hpBase,attackDef,speedBase; }
[Serializable] public class EquipmentRow { public int id,quality,type; public string icon,name; }
[Serializable] public class TreeRow { public int id; public string quality,cost; public int time; }
[Serializable] public class StageRow { public int id,chapter,background,mosterList,mosterLevelList; public string rewards; }
[Serializable] public class MonsterRow { public int id,attackEmp,hpEmp; public string appearanceId,defEmp; }
[Serializable] public class MonsterLevel { public int id; public string attackBase,hpBase,defBase,speedBase; }
[Serializable] public class OriginalConfig {
 public RealmRow[] realms; public EquipmentRow[] equipment; public TreeRow[] trees; public StageRow[] stages; public MonsterRow[] monsters; public MonsterLevel[] monsterLevels;
 public static OriginalConfig Load() => JsonConvert.DeserializeObject<OriginalConfig>(Resources.Load<TextAsset>("Game/OriginalConfig").text);
}
[Serializable] public class Gear { public int id,slot,quality,level,attack,hp,defense,speed,affix,lootBand; public string icon; public int Score => attack*5+hp+defense*5+speed*2; }
[Serializable] public class SaveData {
 public int version=1,level=1,exp=0,peaches=40,coins=300,treeLevel=1,stage=1,skin=0,cuts=0;
 public int keepQuality=3,equipmentSchema=0;
 public Gear[] gear=new Gear[12]; public Gear pending;
 public int expansionSchema,forgeOre,insight,breakthroughStones,towerFloor=1,activeSkill;
 public bool autoSkill=true;
 public int[] forgeLevels=new int[12],talents=new int[24],gemSlots=new int[24],gemInventory=new int[24],dungeonClears=new int[4],dungeonClaims=new int[4];
 public bool[] firstClears=new bool[200],chapterChests=new bool[10];
 public string rewardDay="",nickname="";
 public System.Collections.Generic.List<GuxuRewardReceipt> rewardReceipts;
}
public struct Stats { public int attack,hp,defense,speed; public int Power=>attack*5+hp+defense*5+speed*2; }
public partial class GameRules {
 public readonly OriginalConfig Config; public SaveData Save; readonly System.Random random;
 public GameRules(OriginalConfig config,SaveData save=null,int seed=0) {Config=config;Save=save??new SaveData();random=seed==0?new System.Random():new System.Random(seed);if(Save.gear==null||Save.gear.Length!=12)Save.gear=new Gear[12];for(int i=0;i<12;i++)if(Save.gear[i]==null)Save.gear[i]=MakeGear(i,1,1);if(Save.equipmentSchema==0){if(save!=null&&File.Exists(SavePath))File.Copy(SavePath,SavePath+".before-equipment15",true);foreach(var gear in Save.gear)EquipmentTiers.Apply(gear);if(Save.pending!=null)EquipmentTiers.Apply(Save.pending);Save.equipmentSchema=1;}}
 public RealmRow Realm=>Config.realms[Mathf.Clamp(Save.level-1,0,Config.realms.Length-1)];
 public int ExpNeeded => Math.Max(1,int.Parse(Realm.demonicMax)-(Save.level>1?int.Parse(Config.realms[Save.level-2].demonicMax):0));
 public Stats Attributes=>AttributesWithGear(null);
 public Stats AttributesWithGear(Gear replacement){EnsureExpansion();var s=new Stats{attack=int.Parse(Realm.attackBase),hp=int.Parse(Realm.hpBase),defense=int.Parse(Realm.attackDef),speed=int.Parse(Realm.speedBase)};foreach(var equipped in Save.gear){var g=replacement!=null&&replacement.slot==equipped.slot?replacement:equipped;s.attack+=g.attack;s.hp+=g.hp;s.defense+=g.defense;s.speed+=g.speed;}return CultivationAttributes(s);}
 public string GearName(Gear g)=>EquipmentTiers.Names[Mathf.Clamp(g.slot,0,11)];
 int[] QualityWeights(int treeLevel)=>Config.trees[Mathf.Clamp(treeLevel-1,0,Config.trees.Length-1)].quality.Split('|').Select(int.Parse).ToArray();
 public float[] QualityChances(int treeLevel){var weights=QualityWeights(treeLevel);var result=new float[5];float total=weights.Sum();for(int i=0;i<weights.Length;i++)result[Math.Min(i,4)]+=100f*weights[i]/total;return result;}
 public int TreeUpgradeCost=>int.Parse(Config.trees[Mathf.Clamp(Save.treeLevel-1,0,Config.trees.Length-1)].cost.Split('=')[1]);
 public bool CanUpgradeTree=>Save.treeLevel<Config.trees.Length&&Save.coins>=TreeUpgradeCost;
 public bool UpgradeTree(){if(!CanUpgradeTree)return false;Save.coins-=TreeUpgradeCost;Save.treeLevel++;return true;}
 public int RollQuality(){var weights=QualityWeights(Save.treeLevel);int roll=random.Next(weights.Sum());for(int i=0;i<weights.Length;i++){roll-=weights[i];if(roll<0)return Math.Min(5,i+1);}throw new InvalidOperationException("Invalid quality weights");}
 Gear MakeGear(int slot,int quality,int level){var rows=Config.equipment.Where(e=>e.type==slot+1).ToArray();if(rows.Length==0)throw new InvalidDataException("Missing equipment type "+(slot+1));var row=rows.OrderBy(e=>Math.Abs(e.quality-quality)).First();
 var gear=new Gear{id=row.id,slot=slot,icon=row.icon,quality=Mathf.Clamp(quality,1,5),level=Mathf.Clamp(level,1,3),lootBand=Mathf.Clamp((Save.stage-1)/20,0,9)};EquipmentTiers.Apply(gear);return gear;}
 public bool Chop(){if(Save.pending!=null||Save.peaches<=0)return false;Save.peaches--;Save.cuts++;Save.exp+=int.Parse(Realm.demonicEachDream);Save.pending=MakeGear(random.Next(12),RollQuality(),random.Next(1,4));if(Save.pending.quality>=2)Save.pending.affix=1+random.Next(6);return true;}
 public bool Resolve(bool replace){var g=Save.pending;if(g==null)return false;var old=Save.gear[g.slot];if(replace)Save.gear[g.slot]=g;Save.coins+=(replace?old:g).quality*20;Save.pending=null;return true;}
 public bool Breakthrough(){EnsureExpansion();if(Save.exp<ExpNeeded||Save.level>=Config.realms.Length||Save.breakthroughStones<BreakthroughCost)return false;Save.breakthroughStones-=BreakthroughCost;Save.exp-=ExpNeeded;Save.level++;return true;}
 public Stats Enemy(){var stage=Config.stages[Mathf.Clamp(Save.stage-1,0,Config.stages.Length-1)];var m=Config.monsters.First(x=>x.id==stage.mosterList);var l=Config.monsterLevels.First(x=>x.id==stage.mosterLevelList);return new Stats{attack=int.Parse(l.attackBase)*m.attackEmp/1000,hp=int.Parse(l.hpBase)*m.hpEmp/1000,defense=int.Parse(l.defBase)*int.Parse(m.defEmp)/1000,speed=int.Parse(l.speedBase)};}
 public int Hit(Stats a,Stats b,bool critical)=>Math.Max(1,a.attack-b.defense)*(critical?2:1);
 public bool Critical()=>random.Next(100)<15;
 public void Win(){var stage=Config.stages[Mathf.Clamp(Save.stage-1,0,Config.stages.Length-1)];foreach(var pair in stage.rewards.Split('|')){var p=pair.Split('=');if(p.Length!=2)continue;int count=int.Parse(p[1]);if(p[0]=="100004")Save.peaches+=count;else Save.coins+=count;}Save.stage=Math.Min(Save.stage+1,Config.stages.Length);}
 public static string AccountScope {get;private set;}="";
 public static void SelectAccount(string scope){if(scope==null||scope.Length!=0&&!System.Text.RegularExpressions.Regex.IsMatch(scope,@"\A[a-f0-9]{64}\z"))throw new ArgumentException("Invalid account save scope");AccountScope=scope;}
 public static string SavePath=>Path.Combine(Application.persistentDataPath,AccountScope.Length==0?"gxdl-local-v1.json":"gxdl-account-"+AccountScope+".json");
 readonly string storagePath=SavePath;
 readonly string rewardAccountScope=AccountScope;
 public void Store(){string temp=storagePath+".tmp";File.WriteAllText(temp,JsonConvert.SerializeObject(Save));if(File.Exists(storagePath))File.Replace(temp,storagePath,storagePath+".bak");else File.Move(temp,storagePath);}
 public static SaveData ReadSave(){if(!File.Exists(SavePath))return null;try{var s=JsonConvert.DeserializeObject<SaveData>(File.ReadAllText(SavePath));ValidateSave(s);if(s.expansionSchema==0&&!File.Exists(SavePath+".before-expansion"))File.Copy(SavePath,SavePath+".before-expansion");return s;}catch(Exception e){Debug.LogWarning("存档读取失败，保留原文件并使用新进度："+e.Message);File.Copy(SavePath,SavePath+".invalid",true);return null;}}
 public static void SelfTest(OriginalConfig c){var r=new GameRules(c,null,123);int peaches=r.Save.peaches;Require(r.Chop()&&r.Save.peaches==peaches-1,"chop cost");var pending=r.Save.pending;Require(!r.Chop(),"pending drop blocks second chop");Require(r.Resolve(true)&&r.Save.gear[pending.slot]==pending,"replace exact slot");Require(!r.Resolve(false),"resolve once");r.Save.exp=r.ExpNeeded;int level=r.Save.level;Require(r.Breakthrough()&&r.Save.level==level+1&&r.Save.exp==0,"breakthrough threshold");r.Save.peaches=0;Require(!r.Chop(),"empty inventory");for(int i=0;i<200;i++)Require(r.RollQuality()>=1,"quality weights");var serialized=JsonConvert.SerializeObject(r.Save);Require(JsonConvert.DeserializeObject<SaveData>(serialized).gear.Length==12,"save round trip");Require(r.Enemy().hp>0&&r.Hit(r.Attributes,r.Enemy(),false)>0,"combat stats");r.Save.coins=0;Require(!r.BuyShopGood(0),"shop rejects empty balance");r.Save.coins=ShopGoods[0].Price;int shopPeaches=r.Save.peaches;Require(r.BuyShopGood(0)&&r.Save.peaches==shopPeaches+1&&r.Save.coins==0,"shop buy charges once");Debug.Log("[GXDL TEST] 9 rule checks passed.");}
 static void Require(bool condition,string name){if(!condition)throw new Exception("GXDL test failed: "+name);}
}
}

