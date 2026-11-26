using System;
using UnityEngine;
using UnityEngine.UI;

namespace Gxdl {
public partial class GxdlGame {
 static readonly string[] DungeonPortalKeys={"dungeon-portal-forge","dungeon-portal-gems","dungeon-portal-insight","dungeon-portal-spirit"};
 Image dungeonCourtyard;RectTransform dungeonLinks;readonly RectTransform[] dungeonGates=new RectTransform[4];
 void FitDungeonCourtyard(){
  Canvas.ForceUpdateCanvases();float scale=Mathf.Min(root.rect.width/750f,root.rect.height/1334f),height=root.rect.height/scale,extra=height-1334;
  view.sizeDelta=new Vector2(750,height);view.anchorMin=view.anchorMax=new Vector2(.5f,.5f);view.pivot=new Vector2(0,1);view.anchoredPosition=new Vector2(-375,height*.5f)*scale;view.localScale=Vector3.one*scale;
  if(dungeonCourtyard&&dungeonCourtyard.sprite){var source=dungeonCourtyard.sprite.rect;float cover=Mathf.Max(750/source.width,height/source.height);var size=new Vector2(source.width,source.height)*cover;dungeonCourtyard.rectTransform.sizeDelta=size;dungeonCourtyard.rectTransform.anchoredPosition=new Vector2((750-size.x)*.5f,0);}
  for(int i=0;i<4;i++)if(dungeonGates[i])dungeonGates[i].anchoredPosition=new Vector2(25+i%2*370,-(180+i/2*(420+extra*.5f)));
  if(dungeonLinks)dungeonLinks.anchoredPosition=new Vector2(0,-(height-244));
 }
 // prefab 版秘境总览。宿主未就绪或 prefab 缺失时返回 false，回退旧构建。
 bool ShowDungeonsPrefab(){
  if(Gxdl.Core.GameBootstrap.Instance==null||Gxdl.Core.GameBootstrap.Instance.UiModules==null)return false;
  CloseScreen(); // 先收起残留的 prefab 页，避免两页同时在屏。
  var page=OpenScreen<Gxdl.Modules.DungeonsView>(Gxdl.Modules.UiScreens.Dungeons);if(page==null)return false;
  var gates=new Gxdl.Modules.DungeonsView.GateState[4];
  for(int k=0;k<4;k++){
   bool unlocked=Rules.HighestClear>=GuxuStageRules.DungeonUnlocks[k];
   gates[k]=new Gxdl.Modules.DungeonsView.GateState{Name=GuxuStageRules.DungeonNames[k],
    Progress=unlocked?"已通关 "+Rules.Save.dungeonClears[k]+" / 10":"主线第"+GuxuStageRules.DungeonUnlocks[k]+"关后开放",
    Attempts=unlocked?"今日剩余 "+(3-Rules.Save.dungeonClaims[k])+" 次":"尚未解锁",
    Unlocked=unlocked,AttemptsExhausted=unlocked&&Rules.Save.dungeonClaims[k]>=3};
  }
  if(page.Back){page.Back.onClick.RemoveAllListeners();page.Back.onClick.AddListener(Home);}
  page.BindGateCallbacks(k=>ShowDungeon(k));page.BindTower(ShowTower);page.BindGrowth(ShowGrowthHub);page.Apply(gates);
  return true;
 }
 public void ShowDungeons(){
  if(busy||!CanPlay)return;Rules.RefreshDaily(DateTime.UtcNow);auto=false;battle=false;Clear();ExpansionScreen="dungeons";Rules.EnsureExpansion();
  if(ShowDungeonsPrefab())return;
  dungeonCourtyard=null;dungeonLinks=null;for(int i=0;i<4;i++)dungeonGates[i]=null;fitExpandedPage=FitDungeonCourtyard;FitDungeonCourtyard();
  dungeonCourtyard=Art(GuxuUiArt.Optional("screen-dungeons"),view,0,0,750,1334,false);
  var headingShade=Rect("Dungeon Heading Backing",view,100,0,550,185).gameObject.AddComponent<GuxuSoftVfx>();headingShade.raycastTarget=false;headingShade.color=new Color(.01f,.05f,.07f,.96f);
  SealButton("‹ 古墟",view,18,25,124,52,Home).name="Dungeon Courtyard Back";
  Txt("资源秘境",view,180,16,390,65,44,gold);
  Txt("每类每日3次收益 · 首通后可速通",view,80,88,590,37,26,cream);
  Txt("北京时间零点刷新",view,200,132,350,30,21,cream);
  string[] keys={"resource-ore","gem-0-3","resource-insight","resource-exp"},labels={"玄铁","灵玉","悟道点","修为 / 突破石"};
  for(int k=0;k<4;k++){
   int kind=k;bool unlocked=Rules.HighestClear>=GuxuStageRules.DungeonUnlocks[k];var gate=Rect("Dungeon Gate "+k,view,25+k%2*370,180+k/2*420,330,410);dungeonGates[k]=gate;
   var portal=Art(GuxuUiArt.Optional(DungeonPortalKeys[k]),gate,0,0,330,330);portal.name="Dungeon Portal "+k;if(!unlocked)portal.color=new Color(.61f,.67f,.68f);
   Art(GuxuUiArt.SecondaryButton,gate,-4,18,338,51,false);Txt(GuxuStageRules.DungeonNames[k],gate,10,23,310,43,30,gold);
   Art(GuxuUiArt.SecondaryButton,gate,38,78,254,39,false);
   if(k==3){Art(GuxuItemIcons.Get("resource-exp"),gate,47,80,33,33);Art(GuxuItemIcons.Get("resource-stone"),gate,78,80,33,33);Txt(labels[k],gate,111,79,170,36,22,cream);}
   else{Art(GuxuItemIcons.Get(keys[k]),gate,58,78,39,39);Txt(labels[k],gate,104,79,174,36,25,cream);}
   if(!unlocked)Art(GuxuUiArt.Optional("state-lock"),gate,142,166,48,60);
   Frame("Dungeon Gate State "+k,gate,32,266,266,80,panelDark);
   Txt(unlocked?"已通关 "+Rules.Save.dungeonClears[k]+" / 10":"主线第"+GuxuStageRules.DungeonUnlocks[k]+"关后开放",gate,41,271,248,33,23,cream).name="Dungeon Progress "+k;
   Txt(unlocked?"今日剩余 "+(3-Rules.Save.dungeonClaims[k])+" 次":"尚未解锁",gate,41,307,248,32,23,unlocked&&Rules.Save.dungeonClaims[k]>=3?new Color(1,.65f,.42f):cream).name="Dungeon Attempts "+k;
   var enter=SealButton(unlocked?"进入 ›":"未解锁",gate,60,359,210,56,()=>ShowDungeon(kind),true);enter.name="Dungeon "+k;enter.interactable=unlocked;
  }
  dungeonLinks=Rect("Dungeon Courtyard Links",view,0,1090,750,244);
  SealButton("通天古塔 · 百层试炼",dungeonLinks,65,0,620,76,ShowTower,true);
  SealButton("修行与养成",dungeonLinks,220,119,310,58,ShowGrowthHub);FitDungeonCourtyard();
 }
}
}
