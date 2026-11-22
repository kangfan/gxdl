using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace Gxdl {
public partial class GxdlGame {
 public string ExpansionScreen {get;private set;}="home";
 readonly Color panelDark=new Color(.055f,.12f,.16f,.96f);
 void UiIcon(string key,Transform parent,float x,float y,float size){var sprite=GuxuUiArt.Optional(key);if(sprite)Art(sprite,parent,x,y,size,size);}
 void ExpansionPage(string title,string subtitle,string key){
  auto=false;battle=false;busy=false;Clear();ExpansionScreen=key;Rules.EnsureExpansion();
  var background=GuxuUiArt.Optional("screen-"+key);if(background)Art(background,view,0,0,750,1334,false);else{Img("Game/Guxu/Final/background",view,0,0,750,1334);Img("",view,0,0,750,1334,new Color(.015f,.045f,.065f,.88f));}
  if(RedesignedPage)Img("",view,0,0,750,180,new Color(.01f,.05f,.07f,(key=="talents"||key=="dungeons"||key=="tower"||key=="loadout") ? .68f : .25f));
  if(RedesignedPage)SealButton("‹ 古墟",view,22,28,120,58,Home);else Btn("‹ 古墟",view,22,28,120,58,Home,null,panelDark,25);Txt(title,view,150,27,440,62,35,gold);
  Txt(subtitle,view,25,102,700,65,23,cream);Img("",view,25,176,700,2,new Color(.6f,.5f,.3f,.6f));
  if(RedesignedPage)Frame("Page Resource Bar",view,30,1256,690,64,panelDark);
  Txt("破封符 "+Rules.Save.peaches+"    灵尘 "+Rules.Save.coins+"    基础战力 "+Rules.Attributes.Power,view,25,1269,700,42,23,gold).name="Expansion Resource Summary";
 }
 void Card(float x,float y,float w,float h){Frame("Expansion Card",view,x,y,w,h,panelDark);}
 void SaveAndShow(Action show,string message){Rules.Store();show();if(!string.IsNullOrEmpty(message))Notify(message);}
 void GrowthAction(Func<bool> action,Action show,string error){var before=Rules.Attributes;var traits=Rules.CombatTraits;if(action())SaveAndShow(show,GameRules.GrowthChange(before,Rules.Attributes,traits,Rules.CombatTraits));else {show();Notify(error); }}
 public void ShowWorldMap(){
  if(busy||!CanPlay)return;
  selectedStage=null;auto=false;battle=false;Clear();ExpansionScreen="world";Rules.EnsureExpansion();
  if(ShowWorldMapPrefab())return;
  // The skin contains scenery and empty frames only; all progress and labels are live UI.
  Art(GuxuUiArt.Optional("world-layout"),view,0,0,750,1334,false);
  var heading=new Color(1f,.87f,.56f);var muted=new Color(.64f,.69f,.72f);
  WorldMapHotspot("World Back",18,21,136,46,Home);
  Txt("古墟",view,65,22,76,43,27,cream);
  Txt("主线 "+Rules.HighestClear+" / 200 关",view,115,149,194,36,20,cream,TextAnchor.MiddleLeft).name="World Progress";
  Txt("每章20关",view,350,149,125,36,20,cream,TextAnchor.MiddleLeft);
  Img("",view,485,145,40,40,new Color(.04f,.16f,.19f));Art(GuxuUiArt.Optional("feedback-neutral-boss")??GuxuUiArt.Optional("effect-shield"),view,488,147,34,34);
  Txt("精英与首领交替",view,530,149,166,36,18,cream,TextAnchor.MiddleLeft);
  float[] rows={257,420,582,742,901};
  for(int i=0;i<10;i++){
   int chapter=i;float x=i%2==0?27:385,y=rows[i/2];bool unlocked=IsRuinsRegionUnlocked(i);
   if(!unlocked)Img("",view,x+3,y+3,334,140,new Color(.015f,.03f,.045f,.65f)).name="Chapter Locked "+(i+1);
   var color=unlocked?heading:muted;
   Txt("第\n"+(i+1)+"\n章",view,x+12,y+14,32,80,18,color);
   Img("",view,x+52,y+22,1,67,new Color(color.r,color.g,color.b,.55f));
   Txt(Stages.regions[i].name,view,x+72,y+11,258,39,26,color,TextAnchor.MiddleLeft);
   Txt((i*20+1)+"—"+((i+1)*20)+"关",view,x+74,y+48,222,25,19,unlocked?cream:muted,TextAnchor.MiddleLeft);
   string progress=unlocked?"已通关 "+Mathf.Clamp(Rules.HighestClear-i*20,0,20)+" / 20":"通过第"+(i*20)+"关解锁";
   Txt(progress,view,x+74,y+73,230,24,17,unlocked?cream:muted,TextAnchor.MiddleLeft).name="Chapter Progress "+(i+1);
   Txt(unlocked?"进入遗域  ››":"尚未解锁",view,x+59,y+101,162,35,20,unlocked?cream:muted);
   var button=WorldMapHotspot("Chapter "+(i+1),x,y,340,148,()=>OpenRuinsRegion(chapter));button.interactable=unlocked;
  }
  Art(GuxuUiArt.SecondaryButton,view,71,1089,289,69,false);
  WorldMapHotspot("资源秘境",71,1089,289,69,ShowDungeons);
  Txt("资源秘境",view,178,1092,153,59,29,cream);
  Art(GuxuUiArt.SecondaryButton,view,391,1089,288,69,false);
  WorldMapHotspot("通天古塔",391,1089,288,69,ShowTower);
  Txt("通天古塔",view,493,1092,157,59,29,cream);
  var tokens=Txt("破封符 "+Rules.Save.peaches,view,178,1234,104,34,17,cream,TextAnchor.MiddleLeft);
  var dust=Txt("灵尘 "+Rules.Save.coins,view,348,1234,114,34,17,cream,TextAnchor.MiddleLeft);
  var power=Txt("基础战力 "+Rules.Attributes.Power,view,524,1234,111,34,17,cream,TextAnchor.MiddleLeft);
  tokens.name="World Tokens";dust.name="World Dust";power.name="World Power";
  foreach(var label in new[]{tokens,dust,power}){label.resizeTextForBestFit=true;label.resizeTextMinSize=11;label.resizeTextMaxSize=17;label.horizontalOverflow=HorizontalWrapMode.Overflow;}
 }
 Button WorldMapHotspot(string name,float x,float y,float w,float h,Action action){
  var b=Btn("",view,x,y,w,h,action,null,Color.white);b.name=name;
  var colors=b.colors;colors.normalColor=Color.clear;colors.selectedColor=Color.clear;colors.disabledColor=Color.clear;
  colors.highlightedColor=new Color(1,1,1,.07f);colors.pressedColor=new Color(0,0,0,.22f);colors.fadeDuration=.08f;b.colors=colors;
  b.targetGraphic.CrossFadeColor(Color.clear,0,true,true);
  return b;
 }
 public void ShowChapter(int regionIndex){
  if(busy||!CanPlay)return;if(!IsRuinsRegionUnlocked(regionIndex)){Notify("通过前一章后解锁");return;}
  DrawChapterMap(regionIndex);
 }
 // prefab 版世界地图。宿主未就绪或 prefab 缺失时返回 false，回退旧构建。
 bool ShowWorldMapPrefab(){
  var bootstrap=Gxdl.Core.GameBootstrap.Instance;
  if(bootstrap==null||bootstrap.UiModules==null)return false;
  var module=bootstrap.UiModules.Open(Gxdl.Modules.UiScreens.WorldMap);
  var map=module as Gxdl.Modules.WorldMapView;
  if(map==null)return false;
  var page=(RectTransform)map.transform;
  page.SetParent(view.parent,false);view.gameObject.SetActive(false);
  FitPage(page);
  if(map.Back)map.Back.onClick.AddListener(Home);
  if(map.Dungeons)map.Dungeons.onClick.AddListener(ShowDungeons);
  if(map.Tower)map.Tower.onClick.AddListener(ShowTower);
  map.BindChapterCallbacks(index=>ShowChapter(index));
  map.Apply(MapSnapshot(map));
  return true;
 }
 Gxdl.Modules.WorldMapView.MapSnapshot MapSnapshot(Gxdl.Modules.WorldMapView map){
  var regions=Stages.regions;int count=Mathf.Min(10,regions.Length);
  var cells=new Gxdl.Modules.WorldMapView.ChapterCell[count];
  for(int i=0;i<count;i++){
   bool unlocked=IsRuinsRegionUnlocked(i);
   cells[i]=new Gxdl.Modules.WorldMapView.ChapterCell{Index="第\n"+(i+1)+"\n章",Name=regions[i].name,Range=(i*20+1)+"—"+((i+1)*20)+"关",
    Progress=unlocked?"已通关 "+Mathf.Clamp(Rules.HighestClear-i*20,0,20)+" / 20":"通过第"+(i*20)+"关解锁",
    Entry=unlocked?"进入遗域  ››":"尚未解锁",Unlocked=unlocked};
  }
  return new Gxdl.Modules.WorldMapView.MapSnapshot{ProgressLabel="主线 "+Rules.HighestClear+" / 200 关",Chapters=cells,
   Tokens="破封符 "+Rules.Save.peaches,Dust="灵尘 "+Rules.Save.coins,Power="基础战力 "+Rules.Attributes.Power};
 }

 public void ShowBattlePrep(GuxuStage stage){
  if(busy||!CanPlay)return;Rules.RefreshDaily(DateTime.UtcNow);if(!GuxuStageRules.CanEnter(stage,Rules.Save)){Notify("当前进度尚未解锁");return;}selectedStage=stage;auto=false;
  DrawBattlePreparation(stage);
 }
 public void ShowGrowthHub(){
  if(busy||!CanPlay)return;DrawGrowthHub();
 }
 public void ShowForge(int slot=0){
  if(busy||!CanPlay)return;DrawForge(Mathf.Clamp(slot,0,11));
 }

 public void ShowTalents(int branch=-1){
  if(busy||!CanPlay)return;DrawTalentTree(branch<0?talentBranch:Mathf.Clamp(branch,0,2));
 }
 public void ShowGems(int slot=-1,int socket=-1,int color=-1){
  if(busy||!CanPlay)return;DrawGemWorkshop(slot<0?gemSlot:slot,socket<0?gemSocket:socket,color<0?gemColor:color);
 }

}
}










