using System;
using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
namespace Gxdl {
public partial class GxdlGame {
 Image ancientStone,seeker,spiritDustIcon;GuxuAnimation unsealAnimation;
 RectTransform lootPanel;
 bool resolvingDrop;
 Button unsealButton,homeAutoButton,homeRetentionButton,homeQuestButton;Text homeTokens,homePower,homeSealCost;
 readonly Color jade=new Color(.10f,.19f,.25f,1),bronze=new Color(.48f,.48f,.39f),paper=new Color(.93f,.93f,.86f);
 RectTransform Frame(string name,Transform parent,float x,float y,float w,float h,Color fill){var r=Rect(name,parent,x,y,w,h);var replacement=PageArt(name=="Status Toast"?"panel-toast":name.StartsWith("Battle")?"panel-combat":"panel-dark");if(replacement){var skin=Art(replacement,r,0,0,w,h,false);if(RedesignedPage)skin.pixelsPerUnitMultiplier=2*replacement.rect.width/512f;return r;}Img("",r,0,0,w,h,bronze);Img("",r,2,2,w-4,h-4,fill);Img("",r,7,7,w-14,1,new Color(.80f,.83f,.76f,.25f));Img("",r,7,h-8,w-14,1,new Color(.80f,.83f,.76f,.25f));return r;}
 Image Art(Sprite sprite,Transform p,float x,float y,float w,float h,bool preserve=true){var im=Img("",p,x,y,w,h);im.sprite=sprite;im.preserveAspect=preserve;if(preserve&&sprite!=null&&sprite.texture.name=="item-icons-20261110"){im.rectTransform.pivot=new Vector2(.5f,.5f);im.rectTransform.anchoredPosition=new Vector2(x+w*.5f,-y-h*.5f);}if(sprite!=null&&sprite.border.sqrMagnitude>0){im.type=Image.Type.Sliced;im.preserveAspect=false;}return im;}
 Button SealButton(string text,Transform p,float x,float y,float w,float h,Action action,bool primary=false){if(RedesignedPage&&h>=56){float expanded=Mathf.Max(80,h);y-=(expanded-h)*.5f;h=expanded;}var sprite=PageArt(!primary&&(h<56||w<220)?"button-flat":primary?"button-primary":"button-secondary")??(primary?GuxuUiArt.PrimaryButton:GuxuUiArt.SecondaryButton);var im=Art(sprite,p,x,y,w,h,false);if(RedesignedPage&&sprite)im.pixelsPerUnitMultiplier=sprite.rect.height/h;im.name=text;im.raycastTarget=true;var b=im.gameObject.AddComponent<Button>();b.targetGraphic=im;if(RedesignedPage){var colors=b.colors;colors.disabledColor=new Color(.55f,.60f,.58f,1f);b.colors=colors;}b.onClick.AddListener(()=>action());Txt(text,im.transform,8,4,w-16,h-8,Mathf.Min(primary?32:24,Mathf.FloorToInt(h*.48f)),primary?new Color(.25f,.14f,.05f):cream);return b;}
 RectTransform HomePaperPanel(string name,float x,float y,float width,float height){
  var panel=Rect(name,view,x,y,width,height);var surface=Art(PageArt("panel-decision")??GuxuUiArt.ModalPanel,panel,0,0,width,height,false);
  // Small cards need scaled corner borders so their paper center spans the whole text area.
  surface.pixelsPerUnitMultiplier=8f*surface.sprite.rect.width/1024f;return panel;
 }
 public void Home(){
 if(!CanPlay)return;
 battle=false;busy=resolvingDrop;Clear();hero=null;enemy=null;ActiveBattle=null;ExpansionScreen="home";selectedStage=null;Rules.EnsureExpansion();
 if(ShowHomePrefab())return;
 var scene=Art(GuxuUiArt.Optional("home-bg")??GuxuUiArt.Optional("screen-home")??Sprite("Game/Guxu/Final/background"),view,0,0,750,1334,false);GuxuParticleFx.Sort(scene.transform,-40);
 view.gameObject.AddComponent<GuxuFinalAtmosphere>();DrawRuinsMap();
 var groundShadow=Rect("Seeker Ground Shadow",view,112,955,166,36).gameObject.AddComponent<GuxuSoftVfx>();groundShadow.raycastTarget=false;groundShadow.color=new Color(.035f,.08f,.09f,.20f);GuxuParticleFx.Sort(groundShadow.transform,-31);
 seeker=Img("Game/Guxu/Final/Hero/hero",view,-25,440,400,527);seeker.preserveAspect=true;seeker.name="Seeker Character Layer";
 ancientStone=Img("",view,430,455,200,220);ancientStone.preserveAspect=true;ancientStone.name="Sealed Stone Layer";
 var companion=Rect("Companion A Zhuan",view,515,300,145,125).gameObject.AddComponent<GuxuCompanionMotion>();companion.raycastTarget=false;
 unsealAnimation=view.gameObject.AddComponent<GuxuAnimation>();unsealAnimation.Bind(seeker,ancientStone,view);RefreshSealPresentation();
 GuxuParticleFx.Sort(seeker.transform,-20);GuxuParticleFx.Sort(ancientStone.transform,-20);GuxuParticleFx.Sort(companion.transform,-20);GuxuFootCloud.Attach(seeker);
 if(Rules.Save.pending!=null){var relic=Art(GuxuUiArt.Item(Rules.Save.pending.slot),view,450,470,150,150);relic.name="Pending Relic Display";}
 Header();

 if(accounts&&CanPlay)FinalSideEntry("账号","",GuxuUiArt.Icon.Codex,630,84,ShowAccountPanel);
 FinalSideEntry("商城","home-entry-shop",GuxuUiArt.Icon.Sect,630,220,ShowShop);
 FinalSideEntry("鉴宝台","home-entry-appraisal",GuxuUiArt.Icon.Codex,630,356,TreeUpgrade);
 FinalSideEntry("资源秘境","home-entry-dungeons",GuxuUiArt.Icon.Explore,630,492,ShowDungeons);
 FinalSideEntry("背包","home-entry-bag",GuxuUiArt.Icon.Inventory,630,628,ShowBag);

 unsealButton=HudAction("Unseal",view,195,790,360,92,Chop);
 var unsealArt=Art(GuxuUiArt.Optional("hud-unseal-button",true),unsealButton.transform,0,6,360,80,false);unsealArt.name="Unseal Gold Art";unsealArt.pixelsPerUnitMultiplier=.8f;unsealButton.targetGraphic=unsealArt;
 Txt("破  封",unsealButton.transform,55,6,250,80,40,new Color(.25f,.14f,.05f)).name="Unseal Label";
 Art(GuxuUiArt.Navigation(GuxuUiArt.Icon.SealToken),view,318,895,30,30);
 homeSealCost=HudText("消耗 ×1",view,356,892,160,36,24,cream);homeSealCost.name="Seal Token Cost";
 homeAutoButton=HudToggle(auto?"停止自动":"自动破封",auto?"停止自动":"自动破封",view,205,930,225,72,auto,ToggleAuto);
 homeRetentionButton=HudAction("保留规则",view,445,930,150,72,AutoSettings);HudText("保留规则 ›",homeRetentionButton.transform,0,0,150,72,24,cream,TextAnchor.MiddleCenter);
 RefreshSealPresentation();
 ShowDrop();
 if(Rules.Save.pending==null){
  bool complete=Rules.Save.stage>Stages.stages.Length;var mission=HudAction("GoQuestTarget",view,40,1020,662,146,Adventure);homeQuestButton=mission;
  HomeText(complete?"主线 · 已通关":"主线 · "+CurrentStage.regionName,mission.transform,0,0,620,42,30,new Color(.52f,.36f,.08f)).name="Main Quest Title";
  string target=complete?"重访遗域":"前往 "+CurrentStage.regionName;
  var targetText=HomeText(target+" ›",mission.transform,0,42,620,46,30,new Color(.05f,.36f,.30f));targetText.name="Main Quest Target";
  HomeText(complete?"主线已通关 · 可继续挑战古塔与秘境":"目标："+CurrentStage.guardianName+" · 第 "+CurrentStage.id+" / "+Stages.stages.Length+" 关",mission.transform,0,96,640,40,25,ink).name="Main Quest Progress";
  mission.gameObject.SetActive(!busy&&!resolvingDrop);
 }
 string[] names={"洞府","修行","寻墟","图鉴","战备"};
 GuxuUiArt.Icon[] icons={GuxuUiArt.Icon.Sect,GuxuUiArt.Icon.Cultivation,GuxuUiArt.Icon.Ruins,GuxuUiArt.Icon.Codex,GuxuUiArt.Icon.Companion};
 var navSkin=Art(GuxuUiArt.Optional("ui23-panel-dark",true),view,-12,1218,774,121,false);navSkin.name="Navigation Panel";if(navSkin.sprite)navSkin.pixelsPerUnitMultiplier=1.4f;
 for(int i=0;i<5;i++){int tab=i;var nav=Rect("Tab "+names[i],view,i*150,1225,150,109);if(i==2){var glow=Rect("Selected Tab Light",nav,15,-16,120,105).gameObject.AddComponent<GuxuSoftVfx>();glow.raycastTarget=false;glow.color=new Color(1,.76f,.28f,.25f);}Art(GuxuUiArt.Navigation(icons[i]),nav,34,-12,82,82);Txt(names[i],nav,5,65,140,36,25,gold);Btn("",nav,0,0,150,109,()=>Navigate(tab),null,Color.clear).name=names[i];}
 }
 // prefab 版主界面。宿主未就绪或 prefab 缺失时返回 false，回退旧构建。
 bool ShowHomePrefab(){
  if(Gxdl.Core.GameBootstrap.Instance==null||Gxdl.Core.GameBootstrap.Instance.UiModules==null)return false;
  var page=OpenScreen<Gxdl.Modules.HomeView>(Gxdl.Modules.UiScreens.Home);
  if(page==null)return false;
  if(!page.IsComplete){CloseScreen();return false;}
  // 旧的刷新方法仍以这些字段为入口，这里指向 prefab 节点，避免两套数据口径。
  unsealButton=page.Unseal;homeAutoButton=page.Auto;homeRetentionButton=page.Retention;homeQuestButton=page.Quest;
  homeTokens=page.SealTokenText;homePower=page.PowerText;coinText=page.SpiritStoneText;homeSealCost=page.SealCostText;
  seeker=page.Seeker;ancientStone=page.Stone;spiritDustIcon=page.SpiritStoneIcon;
  page.BindNavigation(Navigate);
  page.BindActions(ShowRename,ShowRealmPage,ShowAccountPanel,ShowShop,TreeUpgrade,ShowDungeons,ShowBag,Chop,ToggleAuto,AutoSettings,Adventure,
   ()=>ResolveDrop(false),()=>ResolveDrop(true));
  unsealAnimation=page.GetComponent<GuxuAnimation>();if(!unsealAnimation)unsealAnimation=page.gameObject.AddComponent<GuxuAnimation>();
  unsealAnimation.Bind(seeker,ancientStone,page.transform as RectTransform);
  GuxuFootCloud.Attach(seeker);
  page.gameObject.AddComponent<GuxuFinalAtmosphere>();
  GuxuParticleFx.Sort(page.Seeker.transform,-20);GuxuParticleFx.Sort(page.Stone.transform,-20);GuxuParticleFx.Sort(page.Companion,-20);
  page.Apply(Gxdl.Modules.HomeView.HomeSnapshot.Capture(Rules,PlayerNickname,auto,busy,accounts!=null));
  return true;
 }
 void FinalSideEntry(string label,string key,GuxuUiArt.Icon icon,float x,float y,Action action){
  var root=Rect("Entry "+label,view,x,y,108,130);var sprite=GuxuUiArt.Optional(key);
  if(sprite)Art(sprite,root,0,0,108,130,false);else Art(GuxuUiArt.Navigation(icon),root,12,10,84,84);
  var text=Txt(label,root,2,99,104,30,22,cream);var outline=text.gameObject.AddComponent<Outline>();outline.effectColor=new Color(.04f,.13f,.18f,.95f);outline.effectDistance=new Vector2(1,-1);Btn("",root,0,0,108,130,action,null,Color.clear).name=label;
 }
 void RefreshSealPresentation(){
  bool ready=Rules.Save.pending==null&&!resolvingDrop;
  if(ancientStone)ancientStone.enabled=ready&&ancientStone.sprite;
  if(activeModule is Gxdl.Modules.HomeView home){
   home.ApplySeal(Gxdl.Modules.HomeView.HomeSnapshot.CaptureSeal(Rules,auto,busy||resolvingDrop));
   if(homeQuestButton)homeQuestButton.gameObject.SetActive(ready&&!busy);
   return;
  }
  if(unsealButton){unsealButton.interactable=ready&&Rules.Save.peaches>0;unsealButton.GetComponentInChildren<Text>().text=Rules.Save.peaches>0?"破  封":"破封符不足";}
  if(homeSealCost){homeSealCost.text=Rules.Save.peaches>0?"消耗 ×1":"消耗 ×1 · 不足";homeSealCost.color=Rules.Save.peaches>0?cream:new Color(1,.63f,.40f);}
  if(homeAutoButton)homeAutoButton.interactable=auto||!busy&&!resolvingDrop&&ready&&Rules.Save.peaches>0;
  if(homeRetentionButton)homeRetentionButton.interactable=!busy;
  if(homeQuestButton)homeQuestButton.gameObject.SetActive(ready&&!busy);
 }
 void RefreshHomeBalances(){if(ExpansionScreen!="home")return;if(homeTokens)homeTokens.text=Rules.Save.peaches.ToString();if(homePower)homePower.text="基础战力  "+Rules.Attributes.Power;if(coinText)coinText.text=Rules.Save.coins.ToString();if(homeAutoButton){homeAutoButton.name=auto?"停止自动":"自动破封";if(!(activeModule is Gxdl.Modules.HomeView))RefreshHudToggle(homeAutoButton,auto,homeAutoButton.name);}RefreshSealPresentation();}
 void Header(){
  var head=Rect("Player HUD",view,24,12,702,108);HudPortrait(head,0,3,84,"Home Player Portrait");HudAction("Rename Player",head,0,0,88,100,ShowRename);
  var identity=HomeText("等级"+Rules.Save.level+"  "+PlayerNickname,head,98,0,255,32,27,ink);identity.name="Player Identity";identity.resizeTextForBestFit=true;identity.resizeTextMinSize=16;identity.supportRichText=false;
  homePower=HomeText("基础战力  "+Rules.Attributes.Power,head,98,32,255,34,28,new Color(.45f,.31f,.10f));homePower.name="Player Power";
  HomeText(Rules.Realm.displayName,head,98,68,255,32,25,ink).name="Player Realm";
  Art(GuxuUiArt.Optional("hud-flourish"),head,258,93,80,20,false);
  HudAction("Cultivation Header",head,94,0,260,104,ShowCultivation);
  Art(GuxuUiArt.Navigation(GuxuUiArt.Icon.SealToken),head,400,20,37,37);homeTokens=HomeText(Rules.Save.peaches.ToString(),head,440,14,100,45,27,ink);homeTokens.name="Seal Token Balance";
  spiritDustIcon=Art(GuxuUiArt.Navigation(GuxuUiArt.Icon.SpiritDust),head,545,20,37,37);spiritDustIcon.name="Spirit Dust Icon";coinText=HomeText(Rules.Save.coins.ToString(),head,585,14,110,45,27,ink);coinText.name="Spirit Stone Balance";
  // Keep even ten-digit balances inside their own groups; identity never shares their rects.
  foreach(var amount in new[]{homeTokens,coinText}){amount.resizeTextForBestFit=true;amount.resizeTextMinSize=20;amount.resizeTextMaxSize=27;}
  Art(GuxuUiArt.Optional("hud-flourish"),head,432,58,72,18,false);Art(GuxuUiArt.Optional("hud-flourish"),head,612,58,72,18,false);
 }
 // Bright sky background: dark ink text with a light halo instead of the HUD's dark outline.
 Text HomeText(string value,Transform parent,float x,float y,float width,float height,int size,Color color){
  var text=Txt(value,parent,x,y,width,height,size,color,TextAnchor.MiddleLeft);var halo=text.gameObject.AddComponent<Shadow>();halo.effectColor=new Color(1f,.99f,.94f,.9f);halo.effectDistance=new Vector2(1,-1);return text;
 }
 void ShowDrop(){
  if(lootPanel!=null)Destroy(lootPanel.gameObject);lootPanel=null;var g=Rules.Save.pending;if(g==null)return;
  // 金纹横幅弹窗（Art/洞府 弹窗效果图）。prefab 版结构一致，仅旧构建走这里。
  lootPanel=Rect("Relic Decision",view,25,900,700,316);
  var surface=Art(GuxuUiArt.Optional("relic-modal-panel"),lootPanel,0,0,700,316,false);surface.name="Modal Paper";
  if(surface.sprite==null)surface.color=new Color(.94f,.92f,.84f,.98f);
  surface.raycastTarget=true;
  GearCard(g,lootPanel,60,63,139,139);
  Txt(Rules.GearName(g)+" · "+GuxuUiArt.TierLabel(g.quality,g.level),lootPanel,217,48,291,40,26,new Color(.20f,.20f,.20f),TextAnchor.MiddleLeft);
  int delta=Rules.AttributesWithGear(g).Power-Rules.Attributes.Power;Txt(delta==0?"基础战力不变":("基础战力 "+(delta>0?"↑ +":"↓ ")+delta),lootPanel,217,92,264,47,30,delta==0?new Color(.36f,.32f,.28f):delta>0?new Color(.40f,.25f,.09f):new Color(.7f,.2f,.12f),TextAnchor.MiddleLeft);
  Txt("攻击力 +"+g.attack+"   生命值 +"+g.hp+"\n"+GameRules.GearAffix(Rules.Save.gear[g.slot])+" → "+GameRules.GearAffix(g),lootPanel,228,163,253,68,20,new Color(.36f,.32f,.28f),TextAnchor.MiddleLeft).name="Drop Affix Comparison";
  var dismantle=SealButton("分解",lootPanel,512,100,178,60,()=>ResolveDrop(false));dismantle.name="Resolve Equipment";SkinSealButton(dismantle,"relic-btn-teal",new Color(.98f,.96f,.89f));
  var equip=SealButton("装备",lootPanel,512,178,178,64,()=>ResolveDrop(true));equip.name="Equip Relic";SkinSealButton(equip,"relic-btn-gold",new Color(.26f,.17f,.06f));
 }
 // 弹窗按钮换皮：保留 SealButton 的回调结构，只替换底图与文字颜色。
 void SkinSealButton(Button button,string key,Color textColor){
  var image=button.image;if(image){var sprite=GuxuUiArt.Optional(key);if(sprite){image.sprite=sprite;image.type=Image.Type.Simple;image.pixelsPerUnitMultiplier=1f;}}
  var label=button.GetComponentInChildren<Text>();if(label)label.color=textColor;
 } int TableCost()=>Rules.TreeUpgradeCost;
 void ToggleAuto(){if(auto){auto=false;if(homeAutoButton){homeAutoButton.name="自动破封";if(activeModule is Gxdl.Modules.HomeView)RefreshSealPresentation();else{RefreshHudToggle(homeAutoButton,false,"自动破封");homeAutoButton.interactable=!busy&&!resolvingDrop&&Rules.Save.pending==null&&Rules.Save.peaches>0;}}return;}if(!CanPlay||busy||battle||modal!=null||resolvingDrop)return;if(Rules.Save.pending!=null){Notify("请先处理当前遗宝");return;}if(Rules.Save.peaches<=0){Notify("破封符不足");return;}auto=true;nextAuto=Time.time;Home();}
 public bool ShouldKeep(Gear g)=>g.quality>=Mathf.Clamp(Rules.Save.keepQuality,1,5)||Rules.AttributesWithGear(g).Power>Rules.Attributes.Power;
 void Navigate(int tab){if(busy)return;auto=false;if(tab==0)ShowFinalResidence();else if(tab==1)ShowGrowthHub();else if(tab==2)ShowWorldMap();else if(tab==3)ShowCodex();else ShowLoadout();}
 void ShowCultivation()=>ShowRealmPage();
 public void ShowBag(){
  if(busy)return;auto=false;
  if(ShowBagPrefab())return;
  var p=OpenModal("背包 · 当前装备",740);
  var skin=p.Find("Modal Paper").GetComponent<Image>();skin.sprite=GuxuUiArt.Optional("panel-inventory",true)??PageArt("panel-dark");skin.type=Image.Type.Sliced;skin.pixelsPerUnitMultiplier=1;
  p.Find("Modal Header").GetComponent<Text>().color=cream;p.Find("Modal Separator").GetComponent<Image>().color=new Color(.56f,.61f,.58f,.5f);
  for(int i=0;i<12;i++){int slot=i;float x=42+i%4*154,y=112+i/4*174;GearCard(Rules.Save.gear[i],p,x,y,125,125,()=>ShowGear(slot));Txt(slots[i],p,x-4,y+128,133,34,24,cream);}
  Txt("点击装备查看属性 · 锻造等级随槽位保留",p,40,675,600,35,22,cream);
 }
 // prefab 版背包。宿主未就绪或 prefab 缺失时返回 false，回退旧弹窗构建。
 // 背包要透出主界面，不走 OpenScreen（它先关当前页）；主页 prefab 保留在背后。
 bool ShowBagPrefab(){
  var bootstrap=Gxdl.Core.GameBootstrap.Instance;
  if(bootstrap==null||bootstrap.UiModules==null)return false;
  var module=bootstrap.UiModules.Open(Gxdl.Modules.UiScreens.Bag);
  var page=module as Gxdl.Modules.BagView;
  if(page==null){if(module!=null)bootstrap.UiModules.Close(module.ModuleId);return false;}
  if(!page.IsComplete){bootstrap.UiModules.Close(page.ModuleId);return false;}
  activeModule=module;activeModulePage=(RectTransform)page.transform;
  activeModulePage.SetParent(root,false);
  FitPage(activeModulePage);
  activeModulePage.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal,750f);
  activeModulePage.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical,1334f);
  var badges=new string[12];
  for(int i=0;i<12;i++)badges[i]=Rules.Save.gear[i].level+"阶";
  page.Apply(badges);
  // 详情按钮接真实系统：锻造与灵玉镶嵌仍是旧弹窗，从背包页之上打开。
  page.BindActions(()=>{page.CloseAnimated(()=>{CloseScreen();Home();});},
   slot=>ShowBagDetail(page,slot),
   slot=>{CloseBagDetail(page);ShowForge(slot);},
   slot=>{CloseBagDetail(page);ShowGems(slot);},
   ()=>page.HideDetail());
  return true;
 }
 void ShowBagDetail(Gxdl.Modules.BagView page,int slot){
  if(busy)return;var g=Rules.Save.gear[slot];int affix=Mathf.Clamp(g.affix,0,6);
  page.ShowDetail(new Gxdl.Modules.BagView.GearDetail{
   Slot=slot,
   Badge=g.level+"阶",
   Name=Rules.GearName(g),
   Subtitle=slots[slot]+" · "+g.level+"阶 · "+quality[Mathf.Clamp(g.quality,1,5)],
   Stats=new[]{"+"+g.attack,"+"+g.hp,"+"+g.defense,"+"+g.speed},
   Affix="战斗词条："+GameRules.AffixNames[affix],
   Desc="槽位锻造 +"+Rules.Save.forgeLevels[slot]+" · 点击锻造或镶嵌可继续强化该槽位",
  });
 }
 void CloseBagDetail(Gxdl.Modules.BagView page)=>page.HideDetail();


 public void SkipCurrentUnseal(){if(unsealAnimation)unsealAnimation.SkipReveal();}
 IEnumerator ChopAnimation(){
  int version=screenVersion;var g=Rules.Save.pending;bool firstReward=PlayerPrefs.GetInt(FirstUnsealStoryKey,0)==0;
  Button skip=null;if(g.quality>=4){skip=SealButton("跳过演出",view,548,activeModule is Gxdl.Modules.HomeView?748:947,177,43,SkipCurrentUnseal);skip.name="Skip Unseal";}
  yield return unsealAnimation.PlayUnseal(GuxuUiArt.Item(0),GuxuUiArt.Item(g.slot),g.quality);
  if(version!=screenVersion)yield break;
  // Retain the existing one-time weapon policy independently of the retired tutorial cinematic.
  if(firstReward){PlayerPrefs.SetInt(FirstUnsealStoryKey,1);PlayerPrefs.Save();}
  if(skip)Destroy(skip.gameObject);busy=false;if(auto&&!ShouldKeep(g))ResolveDrop(false);else{auto=false;Home();}
 }

}
}















