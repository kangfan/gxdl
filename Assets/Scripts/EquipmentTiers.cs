using System;
using System.Linq;
using Newtonsoft.Json;
using UnityEngine;
namespace Gxdl {
public static class EquipmentTiers {
 public static readonly string[] Names={"青霜剑","玉霄冠","流云翼","踏云靴","玄玉带","镇岳护腕","灵玉葫芦","星河坠","清心佩","招灵幡","凝元珠","太清法袍"};
 [Serializable] public class Tier { public int quality,level,attack,hp,defense,speed; }
 [Serializable] class Table {public Tier[] tiers;}
 static Tier[] rows;
 public static Tier[] All=>rows??(rows=JsonConvert.DeserializeObject<Table>(Resources.Load<TextAsset>("Game/Guxu/EquipmentTiers").text).tiers);
 public static void Apply(Gear gear){gear.quality=Mathf.Clamp(gear.quality,1,5);gear.level=Mathf.Clamp(gear.level,1,3);var row=All.Single(x=>x.quality==gear.quality&&x.level==gear.level);float band=1+Mathf.Clamp(gear.lootBand,0,9)*.3f;bool offense=gear.slot==0||gear.slot==5||gear.slot==7||gear.slot==9;gear.attack=Mathf.RoundToInt(row.attack*band*(offense?1.4f:.8f));gear.hp=Mathf.RoundToInt(row.hp*band*(offense?.75f:1.125f));gear.defense=Mathf.RoundToInt(row.defense*band*(offense?.8f:1.1f));gear.speed=gear.slot==3||gear.slot==2?gear.quality*2:0;}
 public static void Validate(){if(All.Length!=15)throw new Exception("Expected 15 equipment tiers");int previous=0;for(int q=1;q<=5;q++)for(int level=1;level<=3;level++){var row=All.Single(x=>x.quality==q&&x.level==level);if(row.attack<=previous||row.hp<=0||row.defense<=0||row.speed!=0)throw new Exception("Invalid tier progression");previous=row.attack;}}
}
}
