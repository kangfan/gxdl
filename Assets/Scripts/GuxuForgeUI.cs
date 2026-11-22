using UnityEngine;
using UnityEngine.UI;

namespace Gxdl {
public partial class GxdlGame {
 RectTransform forgeBackdrop,forgeStage,forgeFooter;
 void FitForgePage(){
  Canvas.ForceUpdateCanvases();float scale=Mathf.Min(root.rect.width/750f,root.rect.height/1334f),height=root.rect.height/scale,extra=height-1334;
  view.sizeDelta=new Vector2(750,height);view.anchorMin=view.anchorMax=new Vector2(.5f,.5f);view.pivot=new Vector2(0,1);view.anchoredPosition=new Vector2(-375,height*.5f)*scale;view.localScale=Vector3.one*scale;
  if(forgeBackdrop){float imageHeight=height+150,width=750*imageHeight/1334;forgeBackdrop.sizeDelta=new Vector2(width,imageHeight);forgeBackdrop.anchoredPosition=new Vector2((750-width)*.5f,150);}
  if(forgeStage)forgeStage.anchoredPosition=new Vector2(0,-(385+extra*729/1334));
  if(forgeFooter)forgeFooter.anchoredPosition=new Vector2(0,-(height-520));
 }
 void DrawForge(int slot){
  auto=false;battle=false;Clear();ExpansionScreen="forge";Rules.EnsureExpansion();forgeBackdrop=null;forgeStage=null;forgeFooter=null;fitExpandedPage=FitForgePage;FitForgePage();
  forgeBackdrop=Art(GuxuUiArt.Optional("forge-environment"),view,0,0,750,1334,false).rectTransform;
  SealButton("‹ 返回修行",view,15,24,174,52,ShowGrowthHub).name="返回修行";
  Txt("槽位锻造",view,205,14,340,65,42,gold);
  Art(GuxuItemIcons.Get("resource-ore"),view,207,83,32,32);Txt("玄铁 "+Rules.Save.forgeOre,view,241,83,170,32,23,cream,TextAnchor.MiddleLeft).name="Forge Ore Balance";
  Art(GuxuItemIcons.Get("resource-dust"),view,420,83,32,32);Txt("灵尘 "+Rules.Save.coins,view,457,83,230,32,23,cream,TextAnchor.MiddleLeft).name="Forge Dust Balance";
  Txt("锻造绑定槽位 · 更换遗宝后继承",view,65,120,620,31,23,cream);
  int level=Rules.Save.forgeLevels[slot],cost=Rules.ForgeCost(slot),dust=Rules.ForgeDustCost(slot);bool capped=level>=Rules.ForgeLimit;
  for(int i=0;i<Rules.Save.gear.Length;i++){
   int choice=i;var gear=Rules.Save.gear[i];var hit=Btn("",view,18+i%6*122,160+i/6*110,108,104,()=>ShowForge(choice),null,Color.clear);hit.name="Forge Slot "+i;hit.transition=Selectable.Transition.None;
   var icon=ItemCell(GuxuUiArt.Item(i),hit.transform,12,0,83,gear.quality);icon.name="Forge Equipment Icon "+i;var frame=icon.transform.parent.Find("Item Quality Frame").GetComponent<Image>();
   Art(GuxuUiArt.SecondaryButton,hit.transform,0,77,108,28,false);
   Txt(slots[i]+" +"+Rules.Save.forgeLevels[i],hit.transform,0,76,108,30,21,i==slot?gold:cream).name="Forge Slot Level "+i;
   if(i==slot){var outline=frame.gameObject.AddComponent<Outline>();outline.effectColor=gold;outline.effectDistance=new Vector2(3,-3);Txt("◆",hit.transform,73,3,22,22,18,gold);}
  }
  forgeStage=Rect("Forge Workbench",view,0,385,750,405);
  Art(GuxuUiArt.Item(slot),forgeStage,285,95,180,180).name="Forge Selected Equipment";
  var label=Art(GuxuUiArt.SecondaryButton,forgeStage,35,77,192,58,false);Txt(slots[slot],label.transform,10,6,172,46,32,cream);
  Art(GuxuUiArt.SecondaryButton,forgeStage,35,145,192,42,false);Txt(GuxuUiArt.TierLabel(Rules.Save.gear[slot].quality,Rules.Save.gear[slot].level),forgeStage,43,145,176,42,24,cream).name="Forge Selected Tier";
  Art(GuxuUiArt.SecondaryButton,forgeStage,175,342,400,53,false);Txt(slots[slot]+" · 锻造 "+level+" / "+Rules.ForgeLimit,forgeStage,188,344,374,49,28,cream).name="Forge Selection";
  forgeFooter=Rect("Forge Footer",view,0,814,750,520);var tray=PaperCard(forgeFooter,"Forge Upgrade Summary",25,0,700,310);
  Txt("锻造加成"+(capped?" · 已达当前上限":" · 当前 → 下级"),tray,55,18,590,36,25,ink);
  var current=GameRules.ForgeBonus(level);var next=GameRules.ForgeBonus(Mathf.Min(level+1,Rules.ForgeLimit));
  string[] names={"攻击力","生命值","防御力"},icons={"effect-attack","effect-health","effect-defense"};int[] now={current.attack,current.hp,current.defense},after={next.attack,next.hp,next.defense};
  for(int i=0;i<3;i++){
   float x=44+i*210;Art(GuxuUiArt.Optional(icons[i]),tray,x+55,61,46,46);Txt(names[i],tray,x+105,63,84,42,28,ink,TextAnchor.MiddleLeft);
   Txt("+"+now[i],tray,x,121,89,44,29,ink).name="Forge Current "+i;Txt("→",tray,x+87,121,31,44,27,ink);Txt("+"+after[i],tray,x+119,121,91,44,31,new Color(.45f,.25f,.06f)).name="Forge Next "+i;
  }
  Img("",tray,50,184,600,1,new Color(.4f,.3f,.12f,.25f));
  Art(GuxuItemIcons.Get("resource-ore"),tray,48,205,35,35);Txt("玄铁：消耗 "+cost+" / 拥有 "+Rules.Save.forgeOre,tray,98,203,552,38,24,Rules.Save.forgeOre>=cost?ink:new Color(.65f,.15f,.07f),TextAnchor.MiddleLeft).name="Forge Ore Cost";
  Art(GuxuItemIcons.Get("resource-dust"),tray,48,249,35,35);Txt("灵尘：消耗 "+dust+" / 拥有 "+Rules.Save.coins,tray,98,247,552,38,24,Rules.Save.coins>=dust?ink:new Color(.65f,.15f,.07f),TextAnchor.MiddleLeft).name="Forge Dust Cost";
  bool unlocked=Rules.GrowthUnlocked(0);var upgrade=SealButton(!unlocked?"第5关后开放":capped?"已达当前上限":!Rules.CanForge(slot)?"材料不足":"锻造 +1",forgeFooter,65,344,402,88,()=>GrowthAction(()=>Rules.Forge(slot),()=>ShowForge(slot),"需通关第5关、材料充足且未达等级上限"),true);upgrade.name="Forge Upgrade";upgrade.interactable=Rules.CanForge(slot);upgrade.GetComponentInChildren<Text>().name="Forge Upgrade Label";
  SealButton("获取玄铁",forgeFooter,495,361,195,52,()=>ShowDungeon(0));
  Art(GuxuUiArt.SecondaryButton,forgeFooter,25,460,700,50,false);Txt("破封符 "+Rules.Save.peaches+"    灵尘 "+Rules.Save.coins+"    基础战力 "+Rules.Attributes.Power,forgeFooter,40,465,670,40,23,gold).name="Expansion Resource Summary";
  FitForgePage();
 }
}
}
