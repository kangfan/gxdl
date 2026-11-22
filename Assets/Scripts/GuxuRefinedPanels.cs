using System;
using UnityEngine;
using UnityEngine.UI;

namespace Gxdl {
public partial class GxdlGame {
 void AttributeRow(Transform parent,string label,string value,string icon,float x,float y,float width){
  var sprite=GuxuUiArt.Optional(icon);if(sprite)Art(sprite,parent,x,y+7,36,36);
  Txt(label,parent,x+47,y,width-47,27,22,ink,TextAnchor.MiddleLeft);
  Txt(value,parent,x+47,y+29,width-47,36,30,ink,TextAnchor.MiddleLeft);
 }
 public void ShowRealmPage(){
  if(busy||!CanPlay)return;ExpansionPage("境界修炼","破封与聚灵洞天积累修为 · 每十级突破大境界","realm");var a=Rules.Attributes;
  var realmShade=Rect("Realm Name Backing",view,170,225,410,260).gameObject.AddComponent<GuxuSoftVfx>();realmShade.raycastTarget=false;realmShade.color=new Color(.015f,.08f,.10f,.88f);
  Txt("境 界",view,225,240,300,45,25,gold);Txt(Rules.Realm.displayName,view,75,307,600,82,48,cream).name="Realm Name";Txt("第 "+Rules.Save.level+" 重修行",view,200,408,350,42,25,gold);
  var progress=Frame("Realm Progress",view,55,485,640,140,panelDark);
  Art(GuxuItemIcons.Get("resource-exp"),progress,22,14,45,45);Txt("修为  "+Rules.Save.exp+" / "+Rules.ExpNeeded,progress,80,15,532,38,26,cream,TextAnchor.MiddleLeft).name="Realm Experience";
  Img("",progress,28,62,584,10,new Color(.02f,.08f,.10f));Img("",progress,28,62,584*Mathf.Clamp01((float)Rules.Save.exp/Rules.ExpNeeded),10,gold);
  Art(GuxuItemIcons.Get("resource-stone"),progress,22,84,45,45);Txt("突破石  "+Rules.Save.breakthroughStones+" / 需要 "+Rules.BreakthroughCost,progress,80,87,532,35,25,cream,TextAnchor.MiddleLeft).name="Realm Stone Cost";
  bool ready=Rules.Save.level<Rules.Config.realms.Length&&Rules.Save.exp>=Rules.ExpNeeded&&Rules.Save.breakthroughStones>=Rules.BreakthroughCost;
  string label=Rules.Save.level>=Rules.Config.realms.Length?"已达境界上限":Rules.Save.exp<Rules.ExpNeeded?"修为不足":Rules.Save.breakthroughStones<Rules.BreakthroughCost?"突破石不足":"境界突破";
  var advance=SealButton(label,view,55,658,640,80,()=>GrowthAction(Rules.Breakthrough,ShowRealmPage,Rules.BreakthroughRequirement),true);advance.interactable=ready;
  var stats=PaperCard(view,"Realm Attributes",55,778,640,250);
  AttributeRow(stats,"攻击力",a.attack.ToString(),"effect-attack",44,24,265);AttributeRow(stats,"生命值",a.hp.ToString(),"effect-health",340,24,265);
  AttributeRow(stats,"防御力",a.defense.ToString(),"effect-defense",44,112,265);AttributeRow(stats,"敏捷度",a.speed.ToString(),"effect-speed",340,112,265);
  SealButton("获取修为",view,65,1050,290,66,()=>ShowDungeon(3));SealButton("返回修行",view,395,1050,290,66,ShowGrowthHub);
  var adviceShade=Rect("Realm Advice Backing",view,65,1122,620,94).gameObject.AddComponent<GuxuSoftVfx>();adviceShade.raycastTarget=false;adviceShade.color=new Color(.015f,.08f,.10f,.90f);
  Txt("敏捷度决定出手顺序。",stats,40,205,560,35,23,ink);
  Txt(Rules.BreakthroughRequirement+"。",view,65,1132,620,72,24,cream).name="Realm Requirement";
 }
 void ShowGear(int slot){
  if(busy)return;auto=false;var g=Rules.Save.gear[slot];int affix=Mathf.Clamp(g.affix,0,6);float height=affix==0?690:725;var p=OpenModal(Rules.GearName(g),height);
  GearCard(g,p,55,112,165,165);Txt(quality[Mathf.Clamp(g.quality,1,5)],p,250,122,370,42,29,ink,TextAnchor.MiddleLeft);Txt(slots[slot]+" · "+g.level+"阶 · 遗域等级 "+(g.lootBand+1),p,250,178,370,42,24,ink,TextAnchor.MiddleLeft);
  Txt("槽位锻造 +"+Rules.Save.forgeLevels[slot],p,250,232,370,36,24,ink,TextAnchor.MiddleLeft);
  Img("",p,50,302,580,1,new Color(.44f,.39f,.25f,.3f));
  AttributeRow(p,"攻击力","+"+g.attack,"effect-attack",57,321,270);AttributeRow(p,"生命值","+"+g.hp,"effect-health",355,321,270);
  AttributeRow(p,"防御力","+"+g.defense,"effect-defense",57,407,270);AttributeRow(p,"敏捷度","+"+g.speed,"effect-speed",355,407,270);
  Txt("战斗词条",p,57,489,566,30,22,ink,TextAnchor.MiddleLeft);Txt(GameRules.AffixNames[affix],p,57,523,566,affix==0?36:71,27,ink,TextAnchor.MiddleLeft).name="Equipment Affix";
  SealButton("槽位锻造",p,45,height-105,275,65,()=>{CloseModal();ShowForge(slot);},true);SealButton("灵玉镶嵌",p,360,height-105,275,65,()=>{CloseModal();ShowGems(slot);});
 }
 void TreeUpgrade(){
  if(busy)return;auto=false;var p=OpenModal("鉴宝台升级",740);var skin=GuxuUiArt.Optional("handbook-panel",true);if(skin){var surface=p.Find("Modal Paper").GetComponent<Image>();surface.sprite=skin;surface.pixelsPerUnitMultiplier=2;var header=p.Find("Modal Header").GetComponent<RectTransform>();header.anchoredPosition=new Vector2(82,-28);header.sizeDelta=new Vector2(482,54);p.Find("Modal Separator").gameObject.SetActive(false);}
  int index=Mathf.Clamp(Rules.Save.treeLevel-1,0,Rules.Config.trees.Length-1);int cost=Rules.TreeUpgradeCost;bool capped=index>=Rules.Config.trees.Length-1;
  Art(GuxuUiArt.Navigation(GuxuUiArt.Icon.Codex),p,62,111,84,84);Txt("鉴宝台 "+Rules.Save.treeLevel+" 级",p,168,114,430,43,32,ink,TextAnchor.MiddleLeft);Txt("修复鉴宝台，提升高品质遗宝的鉴出概率",p,168,163,430,40,22,ink,TextAnchor.MiddleLeft);
  var current=Rules.QualityChances(Rules.Save.treeLevel);var next=Rules.QualityChances(Rules.Save.treeLevel+1);
  Txt("品质",p,67,233,175,34,23,ink,TextAnchor.MiddleLeft);Txt("当前",p,265,233,150,34,23,ink);Txt("→",p,411,233,36,34,25,ink);Txt(capped?"已达上限":"下一级",p,451,233,150,34,23,ink);
  Img("",p,58,272,564,1,new Color(.44f,.39f,.25f,.45f));
  for(int i=0;i<5;i++){
   float y=284+i*48;var tag=GuxuUiArt.Optional("handbook-stat-tag",true);if(tag){var image=Art(tag,p,55,y,188,43,false);image.pixelsPerUnitMultiplier=2;}
   Img("",p,90,y+11,4,22,GuxuUiArt.QualityColor(i+1));Txt(quality[i+1],p,103,y,136,43,23,ink,TextAnchor.MiddleLeft);
   Txt(current[i].ToString("0.#")+"%",p,265,y,150,43,25,ink).name="Appraisal Current "+i;
   Txt(next[i].ToString("0.#")+"%",p,451,y,150,43,25,next[i]>current[i]?new Color(.13f,.42f,.31f):ink).name="Appraisal Next "+i;
   Img("",p,58,y+47,564,1,new Color(.44f,.39f,.25f,.16f));
  }
  Art(GuxuItemIcons.Get("resource-dust"),p,65,554,42,42);Txt("升级消耗 "+cost+" 灵尘   /   拥有 "+Rules.Save.coins,p,122,554,500,42,24,ink,TextAnchor.MiddleLeft).name="Appraisal Cost";
  var upgrade=SealButton(capped?"已达本地版本上限":Rules.Save.coins<cost?"灵尘不足":"升级鉴宝台",p,135,623,410,68,()=>{if(!Rules.UpgradeTree()){Home();TreeUpgrade();Notify("灵尘不足或已达上限");return;}Rules.Store();Home();TreeUpgrade();Notify("鉴宝台升级成功");},true);upgrade.name="Appraisal Upgrade";upgrade.interactable=Rules.CanUpgradeTree;
 }
 void ShowCodex(){
  if(busy)return;auto=false;
  if(ShowCodexPrefab())return;
  var p=OpenModal("遗宝图鉴 · 五品质三阶",760);
  for(int i=0;i<12;i++){
   int slot=i;float x=35+i%4*156,y=112+i/4*185;
   var shadow=Rect("Relic Rest "+i,p,x+19,y+102,106,22).gameObject.AddComponent<GuxuSoftVfx>();shadow.raycastTarget=false;shadow.color=new Color(.31f,.28f,.16f,.25f);
   Art(GuxuUiArt.Item(i),p,x+16,y+7,111,111);Img("",p,x+24,y+131,95,1,new Color(.54f,.44f,.22f,.45f));Txt(EquipmentTiers.Names[i],p,x+2,y+136,139,34,22,ink);
   Btn("",p,x,y,143,169,()=>ShowTierTable(slot),null,Color.clear).name="Codex "+slot;
  }
  Txt("每件遗宝均含五种品质、三个阶级",p,40,695,600,35,22,ink);
 }
 // prefab 版图鉴取代弹窗；宿主未就绪或 prefab 缺失时返回 false，回退旧弹窗。
 bool ShowCodexPrefab(){
  var page=OpenScreen<Gxdl.Modules.CodexView>(Gxdl.Modules.UiScreens.Codex);
  if(page==null)return false;
  CloseModal();
  // 旧图鉴的关闭只是收起弹窗，prefab 版按同一语义收页。
  if(page.Back)page.Back.onClick.RemoveAllListeners();
  if(page.Back)page.Back.onClick.AddListener(CloseScreen);
  page.BindCallbacks(ShowTierTable,ShowTierTable);
  page.ApplyEntries(CodexSnapshot());
  // 品质表默认收起：空行集让 15 行按钮全部隐藏，等同旧页面的未展开状态。
  page.ApplyTable("", "", null);
  return true;
 }
 // 12 个部位名与图标来自静态配置，与旧页面同源。
 Gxdl.Modules.CodexView.CodexSnapshot CodexSnapshot(){
  var entries=new Gxdl.Modules.CodexView.EntryCell[EquipmentTiers.Names.Length];
  for(int i=0;i<entries.Length;i++)entries[i]=new Gxdl.Modules.CodexView.EntryCell{Name=EquipmentTiers.Names[i],Enabled=true};
  return new Gxdl.Modules.CodexView.CodexSnapshot{Hint="每件遗宝均含五种品质、三个阶级",Entries=entries};
 }
 // 行数据沿用旧表：按当前部位与遗域等级套 EquipmentTiers 计算。
 Gxdl.Modules.CodexView.TierRow[] CodexTierRows(int slot){
  var rows=EquipmentTiers.All;var result=new Gxdl.Modules.CodexView.TierRow[rows.Length];
  for(int i=0;i<rows.Length;i++){
   var row=rows[i];var preview=new Gear{slot=slot,quality=row.quality,level=row.level,lootBand=Rules.Save.gear[slot].lootBand};EquipmentTiers.Apply(preview);
   result[i]=new Gxdl.Modules.CodexView.TierRow{Label=GuxuUiArt.TierLabel(row.quality,row.level),Attack=preview.attack.ToString(),Hp=preview.hp.ToString(),Defense=preview.defense.ToString()};
  }
  return result;
 }
 void ShowTierTable(int slot){
  var p=OpenModal(EquipmentTiers.Names[slot]+" · 品质数据",850);
  Txt("按当前部位 · 遗域等级 "+(Rules.Save.gear[slot].lootBand+1)+" 计算，含部位权重",p,45,88,590,24,18,ink);
  string[] headers={"品质 / 阶级","攻击力","生命值","防御力"};float[] xs={60,250,390,530};float[] widths={175,125,125,105};
  Img("",p,45,116,590,36,new Color(.13f,.29f,.27f,.13f));for(int c=0;c<4;c++)Txt(headers[c],p,xs[c],116,widths[c],36,23,ink,c==0?TextAnchor.MiddleLeft:TextAnchor.MiddleRight);
  int index=0;foreach(var row in EquipmentTiers.All){
   var preview=new Gear{slot=slot,quality=row.quality,level=row.level,lootBand=Rules.Save.gear[slot].lootBand};EquipmentTiers.Apply(preview);float y=164+index*34+index/3*12;
   if(index%3==0){Img("",p,46,y,5,102,GuxuUiArt.QualityColor(row.quality));if(index>0)Img("",p,57,y-6,577,1,new Color(.44f,.39f,.25f,.32f));}
   string[] values={GuxuUiArt.TierLabel(row.quality,row.level),preview.attack.ToString(),preview.hp.ToString(),preview.defense.ToString()};
   for(int c=0;c<4;c++)Txt(values[c],p,xs[c],y,widths[c],34,23,ink,c==0?TextAnchor.MiddleLeft:TextAnchor.MiddleRight).name="TierTable Value "+slot+" "+index+" "+c;index++;
  }
  SealButton("返回图鉴",p,180,757,320,60,ShowCodex);
 }
}
}
