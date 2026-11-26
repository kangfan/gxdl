using UnityEngine;
using UnityEngine.UI;

namespace Gxdl {
public partial class GxdlGame {
 int loadoutSelected;RectTransform loadoutBody,loadoutUpper,loadoutMode,loadoutActions;Image loadoutBackdrop;
 void FitLoadoutPage(){
  Canvas.ForceUpdateCanvases();float scale=Mathf.Min(root.rect.width/750f,root.rect.height/1334f),height=root.rect.height/scale,extra=height-1334;
  view.sizeDelta=new Vector2(750,height);view.anchorMin=view.anchorMax=new Vector2(.5f,.5f);view.pivot=new Vector2(0,1);view.anchoredPosition=new Vector2(-375,height*.5f)*scale;view.localScale=Vector3.one*scale;
  if(loadoutBackdrop){float width=750*height/1334;loadoutBackdrop.rectTransform.sizeDelta=new Vector2(width,height);loadoutBackdrop.rectTransform.anchoredPosition=new Vector2((750-width)*.5f,0);}
  if(loadoutBody)loadoutBody.sizeDelta=new Vector2(700,height-435);
  if(loadoutUpper)loadoutUpper.anchoredPosition=new Vector2(0,-(15+extra*.22f));
  if(loadoutMode)loadoutMode.anchoredPosition=new Vector2(0,-(588+extra*.5f));
  if(loadoutActions)loadoutActions.anchoredPosition=new Vector2(0,-(height-545));
 }
 public void ShowLoadout(){if(busy||!CanPlay)return;Rules.EnsureExpansion();loadoutSelected=Rules.Save.activeSkill;DrawLoadoutPage();}
 void DrawLoadoutPage(){
  if(DrawLoadoutPagePrefab())return;
  auto=false;battle=false;Clear();ExpansionScreen="loadout";loadoutBody=null;loadoutUpper=null;loadoutMode=null;loadoutActions=null;loadoutBackdrop=null;fitExpandedPage=FitLoadoutPage;FitLoadoutPage();
  loadoutBackdrop=Art(GuxuUiArt.Optional("screen-loadout"),view,0,0,750,1334,false);var heading=Rect("Loadout Heading Backing",view,120,0,510,175).gameObject.AddComponent<GuxuSoftVfx>();heading.raycastTarget=false;heading.color=new Color(.015f,.06f,.08f,.8f);
  SealButton("‹ 返回修行",view,16,24,174,52,ShowGrowthHub).name="Loadout Back";Art(GuxuUiArt.SecondaryButton,view,195,7,410,76,false);Txt("战前布置",view,210,15,365,65,43,gold);Txt("选择一个主动技能",view,100,104,550,39,27,cream);
  for(int i=0;i<4;i++){
   int skill=i;var hit=Btn("",view,15+i*184,190,168,194,()=>{loadoutSelected=skill;DrawLoadoutPage();},null,Color.clear);hit.name="Loadout Skill "+i;hit.transition=Selectable.Transition.None;
   Art(GuxuUiArt.Optional("combat-skill-seal"),hit.transform,14,18,140,140);
   Art(GuxuUiArt.BattlePrepSkill(i),hit.transform,39,43,90,90);
   var ring=Art(GuxuUiArt.Optional("state-selected"),hit.transform,8,12,152,152);ring.name="Loadout Selection Ring "+i;ring.gameObject.SetActive(i==loadoutSelected);
   Art(GuxuUiArt.SecondaryButton,hit.transform,0,147,168,47,false);Txt(GameRules.SkillNames[i],hit.transform,13,150,142,40,27,cream);
   if(i==Rules.Save.activeSkill){Art(GuxuUiArt.PrimaryButton,hit.transform,20,-10,128,39,false);Txt("已装备",hit.transform,26,-7,116,32,22,ink).name="Loadout Equipped Flag "+i;}
  }
  loadoutBody=Rect("Loadout Detail",view,25,410,700,view.rect.height-435);var paper=Art(GuxuUiArt.Optional("ui23-panel-modal",true),loadoutBody,0,0,700,view.rect.height-435,false);paper.pixelsPerUnitMultiplier=2;paper.rectTransform.anchorMin=Vector2.zero;paper.rectTransform.anchorMax=Vector2.one;paper.rectTransform.offsetMin=paper.rectTransform.offsetMax=Vector2.zero;
  loadoutUpper=Rect("Loadout Skill Display",loadoutBody,0,15,700,550);var contrast=Rect("Loadout Skill Contrast",loadoutUpper,260,39,180,180).gameObject.AddComponent<GuxuPortraitDisc>();contrast.raycastTarget=false;contrast.color=new Color(.06f,.20f,.21f);Art(GuxuUiArt.Optional("loadout-altar"),loadoutUpper,200,0,300,300).name="Loadout Altar";Art(GuxuUiArt.BattlePrepSkill(loadoutSelected),loadoutUpper,290,72,120,120).name="Loadout Selected Skill Icon";
  Art(GuxuUiArt.SecondaryButton,loadoutUpper,125,307,450,53,false);Txt(GameRules.SkillNames[loadoutSelected],loadoutUpper,145,308,410,51,36,cream).name="Loadout Selected Name";
  Txt(GameRules.SkillDetails[loadoutSelected],loadoutUpper,50,365,600,95,27,ink).name="Loadout Skill Description";
  bool equipped=Rules.Save.activeSkill==loadoutSelected;var equip=SealButton(equipped?"已装备":"装备此技能",loadoutUpper,170,467,360,72,()=>{Rules.Save.activeSkill=loadoutSelected;Rules.Store();DrawLoadoutPage();Notify("技能已装备");},!equipped);equip.name="Loadout Equip";equip.interactable=!equipped;
  loadoutMode=Rect("Loadout Skill Mode",loadoutBody,0,588,700,138);var mode=SealButton(Rules.Save.autoSkill?"自动释放 · 开":"自动释放 · 关",loadoutMode,37,0,278,57,()=>{Rules.Save.autoSkill=!Rules.Save.autoSkill;Rules.Store();DrawLoadoutPage();});mode.name="Loadout Auto";
  Txt(Rules.Save.autoSkill?"自动模式按当前技能规则\n择机释放。":"手动模式可保留技能\n等待关键回合释放",loadoutMode,337,-1,320,62,22,ink,TextAnchor.MiddleLeft).name="Loadout Auto Detail";
  Txt("被动效果来自装备、天赋与灵玉。",loadoutMode,45,91,610,35,24,ink);
  loadoutActions=Rect("Loadout Actions",loadoutBody,0,789,700,88);SealButton("返回修行",loadoutActions,35,0,240,78,ShowGrowthHub);SealButton("前往挑战",loadoutActions,301,0,365,78,ShowWorldMap,true).name="Loadout Challenge";FitLoadoutPage();
 }
 // prefab 首版。宿主未就绪或 prefab 缺失时返回 false，回退旧构建。
 bool DrawLoadoutPagePrefab(){
  // 先清旧页面，prefab 会直接挂在根节点；失败时旧构建从 Clear 的状态重新搭。
  auto=false;battle=false;Clear();ExpansionScreen="loadout";
  var page=OpenScreen<Gxdl.Modules.LoadoutView>(Gxdl.Modules.UiScreens.Loadout);
  if(page==null)return false;
  CloseModal();
  // 顶部返回与底部“返回修行”都必须回到成长总览，否则只能靠挑战离开本页。
  if(page.Back){page.Back.onClick.RemoveAllListeners();page.Back.onClick.AddListener(ShowGrowthHub);}
  if(page.Return){page.Return.onClick.RemoveAllListeners();page.Return.onClick.AddListener(ShowGrowthHub);}
  page.BindCallbacks(
   index=>{loadoutSelected=index;DrawLoadoutPage();},
   ()=>{Rules.Save.activeSkill=loadoutSelected;Rules.Store();DrawLoadoutPage();Notify("技能已装备");},
   ()=>{Rules.Save.autoSkill=!Rules.Save.autoSkill;Rules.Store();DrawLoadoutPage();},
   ShowWorldMap);
  page.Apply(LoadoutSnapshot());
  return true;
 }
 // 数值与旧页面同源：技能名/描述取静态表，选中与装备状态取存档。
 Gxdl.Modules.LoadoutView.LoadoutSnapshot LoadoutSnapshot(){
  var cells=new Gxdl.Modules.LoadoutView.SkillCell[GameRules.SkillNames.Length];
  for(int i=0;i<cells.Length;i++)cells[i]=new Gxdl.Modules.LoadoutView.SkillCell{Name=GameRules.SkillNames[i],Detail=GameRules.SkillDetails[i],Equipped=Rules.Save.activeSkill==i};
  bool equipped=Rules.Save.activeSkill==loadoutSelected;
  return new Gxdl.Modules.LoadoutView.LoadoutSnapshot{Skills=cells,SelectedIndex=loadoutSelected,
   EquipLabel=equipped?"已装备":"装备此技能",CanEquip=!equipped,
   AutoLabel=Rules.Save.autoSkill?"自动释放 · 开":"自动释放 · 关",
   AutoDetail=Rules.Save.autoSkill?"自动模式按当前技能规则\n择机释放。":"手动模式可保留技能\n等待关键回合释放"};
 }
}
}
