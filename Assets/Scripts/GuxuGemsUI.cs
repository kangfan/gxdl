using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace Gxdl {
public partial class GxdlGame {
 int gemSlot,gemSocket,gemColor;
 readonly int[] gemLevels=new int[4];
 RectTransform gemBackdrop,gemStage,gemTypes,gemInventory,gemFooter;
 readonly RectTransform[] gemStocks=new RectTransform[6];
 void FitGemPage(){
  Canvas.ForceUpdateCanvases();float scale=Mathf.Min(root.rect.width/750f,root.rect.height/1334f),height=root.rect.height/scale,extra=height-1334;
  view.sizeDelta=new Vector2(750,height);view.anchorMin=view.anchorMax=new Vector2(.5f,.5f);view.pivot=new Vector2(0,1);view.anchoredPosition=new Vector2(-375,height*.5f)*scale;view.localScale=Vector3.one*scale;
  if(gemBackdrop){float imageHeight=height+100,width=750*imageHeight/1334;gemBackdrop.sizeDelta=new Vector2(width,imageHeight);gemBackdrop.anchoredPosition=new Vector2((750-width)*.5f,100);}
  if(gemStage)gemStage.anchoredPosition=new Vector2(0,-(290+extra*560/1334));
  if(gemTypes)gemTypes.anchoredPosition=new Vector2(0,-(640+extra*560/1334));
  if(gemInventory){float y=720+extra*.82f;gemInventory.anchoredPosition=new Vector2(25,-y);gemInventory.sizeDelta=new Vector2(700,200);for(int i=0;i<6;i++)if(gemStocks[i])gemStocks[i].anchoredPosition=new Vector2(16+i*112,-18);}
  if(gemFooter)gemFooter.anchoredPosition=new Vector2(0,-(height-399));
 }
 void DrawGemWorkshop(int slot,int socket,int color){
  auto=false;battle=false;Clear();ExpansionScreen="gems";Rules.EnsureExpansion();gemSlot=Mathf.Clamp(slot,0,11);gemSocket=Mathf.Clamp(socket,0,1);gemColor=Mathf.Clamp(color,0,3);
  gemBackdrop=null;gemStage=null;gemTypes=null;gemInventory=null;gemFooter=null;for(int i=0;i<6;i++)gemStocks[i]=null;fitExpandedPage=FitGemPage;FitGemPage();
  gemBackdrop=Art(GuxuUiArt.Optional("gem-workbench"),view,0,0,750,1334,false).rectTransform;
  SealButton("‹ 返回修行",view,14,24,173,52,ShowGrowthHub).name="Gem Back";
  Txt("灵玉镶嵌",view,203,17,340,63,42,gold);
  Txt("库存 "+Rules.Save.gemInventory.Sum()+" 枚",view,540,29,196,44,24,cream).name="Gem Total Inventory";
  Txt("20 / 40关开启镶孔 · 免费卸下",view,140,84,490,32,23,cream);
  for(int i=0;i<12;i++){
   int choice=i;float x=18+i%6*122,y=124+i/6*81;var hit=Btn("",view,x,y,108,79,()=>DrawGemWorkshop(choice,gemSocket,gemColor),null,Color.clear);hit.name="Gem Slot "+i;hit.transition=Selectable.Transition.None;
   if(i==gemSlot)Art(GuxuUiArt.Optional("state-selected"),hit.transform,20,-6,68,68);
   Art(GuxuUiArt.Item(i),hit.transform,25,0,58,56);Art(GuxuUiArt.SecondaryButton,hit.transform,0,53,108,27,false);Txt(slots[i],hit.transform,0,52,108,28,21,i==gemSlot?gold:cream);
  }
  gemStage=Rect("Gem Workbench",view,0,290,750,340);
  Art(GuxuUiArt.Item(gemSlot),gemStage,285,75,180,180).name="Gem Selected Equipment";
  Art(GuxuUiArt.SecondaryButton,gemStage,275,270,200,40,false);Txt(slots[gemSlot]+" · "+GuxuUiArt.QualityName(Rules.Save.gear[gemSlot].quality),gemStage,275,270,200,40,25,cream);
  for(int hole=0;hole<2;hole++){
   int choice=hole,encoded=Rules.Save.gemSlots[gemSlot*2+hole];float x=hole==0?105:475;bool locked=!Rules.GrowthUnlocked(2)||hole>=Rules.SocketCount;
   var hit=Btn("",gemStage,x,90,170,170,()=>DrawGemWorkshop(gemSlot,choice,gemColor),null,Color.clear);hit.name="Gem Socket "+hole;hit.transition=Selectable.Transition.None;
   Art(GuxuUiArt.Optional("gem-socket-base"),hit.transform,0,0,170,170);
   if(hole==gemSocket)Art(GuxuUiArt.Optional("state-selected"),hit.transform,-10,-10,190,190).name="Gem Selected Socket";
   if(locked){Art(GuxuUiArt.Optional("state-lock"),hit.transform,61,56,48,60);Txt(hole==0?"20关开放":"40关开放",hit.transform,-5,173,180,32,23,cream);}
   else if(encoded>0){Art(GuxuItemIcons.Gem(encoded),hit.transform,41,41,88,88);Txt(GameRules.GemLabel(encoded),hit.transform,-19,173,208,32,23,cream);}
   else Txt("空孔",hit.transform,25,66,120,40,25,cream);
   Art(GuxuUiArt.SecondaryButton,gemStage,x+11,58,148,33,false);Txt("镶孔 "+(hole+1),gemStage,x+11,58,148,33,24,cream);
  }
  int current=Rules.Save.gemSlots[gemSlot*2+gemSocket];
  var effectShade=Rect("Gem Socket Effect Backing",gemStage,199,302,362,51).gameObject.AddComponent<GuxuSoftVfx>();effectShade.raycastTarget=false;effectShade.color=new Color(.015f,.08f,.10f,.96f);
  Txt(current>0?Rules.GemEffect(current):"选择灵玉后镶入目标孔",gemStage,215,312,330,30,23,cream).name="Gem Socket Effect";
  var remove=SealButton("卸下",gemStage,585,300,121,42,()=>GrowthAction(()=>Rules.UnsocketGem(gemSlot,gemSocket),()=>DrawGemWorkshop(gemSlot,gemSocket,gemColor),"当前孔为空"));remove.name="Gem Remove";remove.interactable=current>0;
  gemTypes=Rect("Gem Types",view,0,640,750,65);
  for(int type=0;type<4;type++){
   int choice=type;var hit=Btn("",gemTypes,15+type*184,0,174,59,()=>DrawGemWorkshop(gemSlot,gemSocket,choice),null,Color.clear);hit.name="Gem Type "+type;
   Art(type==gemColor?GuxuUiArt.PrimaryButton:GuxuUiArt.SecondaryButton,hit.transform,0,0,174,59,false);Art(GuxuItemIcons.Gem(type*6+1),hit.transform,14,13,32,32);Txt(GameRules.GemNames[type],hit.transform,47,3,109,53,19,type==gemColor?ink:cream);
  }
  gemInventory=Rect("Gem Inventory",view,25,720,700,200);var stockSkin=Art(PageArt("panel-dark"),gemInventory,0,0,700,200,false).rectTransform;stockSkin.anchorMin=Vector2.zero;stockSkin.anchorMax=Vector2.one;stockSkin.offsetMin=stockSkin.offsetMax=Vector2.zero;
  for(int level=0;level<6;level++){
   int choice=level,index=gemColor*6+level,count=Rules.Save.gemInventory[index];var hit=Btn("",gemInventory,0,0,108,164,()=>{gemLevels[gemColor]=choice;DrawGemWorkshop(gemSlot,gemSocket,gemColor);},null,Color.clear);hit.name="Gem Inventory "+index;gemStocks[level]=(RectTransform)hit.transform;
   var icon=ItemCell(GuxuItemIcons.Gem(index+1),hit.transform,3,0,102);icon.name="Gem Inventory Icon "+index;icon.color=count>0?Color.white:new Color(.48f,.57f,.59f);
   Img("",hit.transform,34,4,66,27,new Color(.02f,.04f,.05f,.82f));Txt(count.ToString(),hit.transform,35,3,64,29,21,cream,TextAnchor.MiddleRight).name="Gem Stock "+index;
   Txt((level+1)+"级",hit.transform,0,105,108,29,23,cream);if(level==5)Txt("最高级",hit.transform,0,134,108,24,18,cream);
   if(level==gemLevels[gemColor]){var outline=icon.transform.parent.Find("Item Quality Frame").gameObject.AddComponent<Outline>();outline.effectColor=gold;outline.effectDistance=new Vector2(2,-2);}
  }
  gemFooter=Rect("Gem Selection Detail",view,0,935,750,399);DrawGemDetail();FitGemPage();
 }
 void DrawGemDetail(){
  int index=gemColor*6+gemLevels[gemColor],count=Rules.Save.gemInventory[index];var tray=PaperCard(gemFooter,"Gem Paper Detail",25,0,700,310);
  Art(GuxuItemIcons.Gem(index+1),tray,30,22,95,95);Txt(GameRules.GemLabel(index+1),tray,143,18,330,40,30,ink,TextAnchor.MiddleLeft);
  Txt(Rules.GemEffect(index+1),tray,143,62,330,37,28,ink,TextAnchor.MiddleLeft).name="Gem Selected Effect";Txt("拥有 "+count,tray,143,102,330,31,24,ink,TextAnchor.MiddleLeft).name="Gem Selected Stock";
  Txt("目标\n"+slots[gemSlot]+" · 镶孔"+(gemSocket+1),tray,472,25,203,86,26,ink);
  bool can=Rules.CanSocketGem(gemSlot,gemSocket,index);string caption=!Rules.GrowthUnlocked(2)?"主线20关后开放":gemSocket>=Rules.SocketCount?"主线40关开启孔2":count==0?"当前灵玉库存不足":"镶嵌到孔"+(gemSocket+1);
  var equip=SealButton(caption,tray,105,143,490,80,()=>GrowthAction(()=>Rules.SocketGem(gemSlot,gemSocket,index),()=>DrawGemWorkshop(gemSlot,gemSocket,gemColor),"当前孔未开放或库存不足"),true);equip.name="Gem Equip";equip.interactable=can;
  bool top=index%6==5;var combine=SealButton(top?"最高级":"3合1",tray,40,242,170,49,()=>GrowthAction(()=>Rules.CombineGem(index),()=>DrawGemWorkshop(gemSlot,gemSocket,gemColor),"需要3枚同级未镶嵌灵玉"));combine.name="Gem Combine";combine.interactable=Rules.CanCombineGem(index);
  if(top)Txt("已达灵玉最高等级",tray,245,246,400,40,25,ink,TextAnchor.MiddleLeft);
  else{Art(GuxuItemIcons.Gem(index+1),tray,233,239,52,52);Txt((index%6+1)+"级×3  →",tray,287,247,180,34,25,ink);Art(GuxuItemIcons.Gem(index+2),tray,466,239,52,52);Txt((index%6+2)+"级×1",tray,522,247,142,34,25,ink);}
  SealButton("获取灵玉",gemFooter,75,334,270,52,()=>ShowDungeon(1)).name="Gem Get More";SealButton("返回修行",gemFooter,405,334,270,52,ShowGrowthHub).name="Gem Return";
 }
}
}
