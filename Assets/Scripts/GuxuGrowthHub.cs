using System;
using System.Linq;
using Gxdl.Modules;
using UnityEngine;
using UnityEngine.UI;

namespace Gxdl {
public partial class GxdlGame {
 RectTransform growthComposition;
 void FitGrowthHub(){
  Canvas.ForceUpdateCanvases();view.localScale=Vector3.one;view.anchorMin=Vector2.zero;view.anchorMax=Vector2.one;view.offsetMin=view.offsetMax=Vector2.zero;
  if(growthComposition)growthComposition.localScale=Vector3.one*Mathf.Min(root.rect.width/940f,root.rect.height/1672f);
 }
 // prefab 版成长总览。宿主未就绪或 prefab 缺失时返回 false，回退旧构建。
 bool DrawGrowthHubPrefab(){
  var bootstrap=Gxdl.Core.GameBootstrap.Instance;
  if(bootstrap==null||bootstrap.UiModules==null)return false;
  Clear();auto=false;battle=false;busy=false;ExpansionScreen="growth";Rules.EnsureExpansion();
  var hub=OpenScreen<GrowthHubView>(UiScreens.GrowthHub);
  if(hub==null)return false;
  if(hub.Back)hub.Back.onClick.AddListener(Home);
  if(hub.Dungeons)hub.Dungeons.onClick.AddListener(ShowDungeons);
  if(hub.Loadout)hub.Loadout.onClick.AddListener(ShowLoadout);
  // 顺序沿用旧页：锻造、境界、天赋、灵玉。
  hub.BindEntryCallbacks(i=>{switch(i){case 0:ShowForge();break;case 1:ShowRealmPage();break;case 2:ShowTalents();break;case 3:ShowGems();break;}});
  var entries=new GrowthHubView.EntryCell[4];
  entries[0]=new GrowthHubView.EntryCell{Title="槽位锻造",Detail="12部位槽位锻造 · 换装继承等级",Balance="玄铁 "+Rules.Save.forgeOre,Enabled=true};
  entries[1]=new GrowthHubView.EntryCell{Title="境界修炼",Detail="修为 "+Rules.Save.exp+" / "+Rules.ExpNeeded+" · 突破石 "+Rules.Save.breakthroughStones,Balance=Rules.Realm.displayName,Enabled=true};
  entries[2]=new GrowthHubView.EntryCell{Title="三系天赋",Detail="攻伐 / 御守 / 灵息 · 24节点",Balance="可用悟道点 "+Rules.Save.insight,Enabled=true};
  entries[3]=new GrowthHubView.EntryCell{Title="灵玉镶嵌",Detail="4类灵玉 · 6级合成 · 换装继承",Balance="库存 "+Rules.Save.gemInventory.Sum()+" 枚",Enabled=true};
  hub.Apply(new GrowthHubView.Snapshot{Entries=entries,Tokens="破封符 "+Rules.Save.peaches,Dust="灵尘 "+Rules.Save.coins,Power="基础战力 "+Rules.Attributes.Power});
  return true;
 }
 void DrawGrowthHub(){
  if(DrawGrowthHubPrefab())return;
  auto=false;battle=false;busy=false;Clear();ExpansionScreen="growth";Rules.EnsureExpansion();
  growthComposition=null;fitExpandedPage=FitGrowthHub;FitGrowthHub();
  var backing=Img("",view,0,0,root.rect.width,root.rect.height,new Color(.025f,.075f,.12f)).rectTransform;backing.anchorMin=Vector2.zero;backing.anchorMax=Vector2.one;backing.offsetMin=backing.offsetMax=Vector2.zero;
  var p=Rect("Growth Hub",view,0,0,940,1672);growthComposition=p;
  p.anchorMin=p.anchorMax=p.pivot=new Vector2(.5f,.5f);p.anchoredPosition=Vector2.zero;
  // Artwork, labels and hit areas share one fitted coordinate space on taller phones.
  p.localScale=Vector3.one*Mathf.Min(root.rect.width/940f,root.rect.height/1672f);
  Art(GuxuUiArt.Optional("growth-layout"),p,0,0,940,1672,false).name="Growth Hub Skin";
  var paleGold=new Color(1f,.89f,.62f);
  PrepText("古墟",p,104,54,81,46,35,cream);
  PrepHit("Growth Back",p,32,49,178,56,Home);
  PrepText("装备、境界、天赋、灵玉共同决定战斗表现",p,240,179,463,38,28,cream);
  string[] names={"槽位锻造","境界修炼","三系天赋","灵玉镶嵌"};
  string[] details={"12部位槽位锻造 · 换装继承等级","修为 "+Rules.Save.exp+" / "+Rules.ExpNeeded+" · 突破石 "+Rules.Save.breakthroughStones,"攻伐 / 御守 / 灵息 · 24节点","4类灵玉 · 6级合成 · 换装继承"};
  string[] balances={"玄铁 "+Rules.Save.forgeOre,Rules.Realm.displayName,"可用悟道点 "+Rules.Save.insight,"库存 "+Rules.Save.gemInventory.Sum()+" 枚"};
  string[] keys={"Forge","Realm","Talents","Gems"};
  float[] rows={323,565,804,1048};
  Action[] actions={()=>ShowForge(),ShowRealmPage,()=>ShowTalents(),()=>ShowGems()};
  for(int i=0;i<4;i++){
   float y=rows[i];int item=i;
   PrepText(names[i],p,153,y+24,235,65,46,paleGold,TextAnchor.MiddleLeft);
   var detail=PrepText(details[i],p,94,y+105,377,38,28,cream,TextAnchor.MiddleLeft);detail.name="Growth Detail "+keys[i];detail.resizeTextMinSize=17;
   var balance=PrepText(balances[i],p,144,y+145,319,43,30,cream,TextAnchor.MiddleLeft);balance.name="Growth Balance "+keys[i];balance.resizeTextMinSize=17;
   PrepText("修习",p,748,y+98,88,60,40,cream);
   PrepHit("Growth "+keys[i],p,707,y+96,192,62,()=>actions[item]());
  }
  PrepText("前往资源秘境",p,175,1367,210,62,36,new Color(.16f,.09f,.025f));
  PrepHit("Growth Dungeons",p,51,1355,392,87,ShowDungeons);
  PrepText("战前技能",p,641,1367,167,62,42,cream);
  PrepHit("Growth Loadout",p,508,1356,371,86,ShowLoadout);
  PrepText("破封符 "+Rules.Save.peaches,p,173,1561,151,43,29,paleGold,TextAnchor.MiddleLeft).name="Growth Tokens";
  PrepText("灵尘 "+Rules.Save.coins,p,416,1561,167,43,29,paleGold,TextAnchor.MiddleLeft).name="Growth Dust";
  PrepText("基础战力 "+Rules.Attributes.Power,p,681,1561,184,43,29,paleGold,TextAnchor.MiddleLeft).name="Growth Power";
 }
}
}
