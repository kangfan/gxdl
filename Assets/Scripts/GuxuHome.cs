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
 var scene=Art(GuxuUiArt.Optional("screen-home")??Sprite("Game/Guxu/Final/background"),view,0,0,750,1334,false);GuxuParticleFx.Sort(scene.transform,-40);
 view.gameObject.AddComponent<GuxuFinalAtmosphere>();DrawRuinsMap();
 var inscription=Txt("古墟大陆",view,420,959,126,26,20,new Color(.35f,.39f,.32f));inscription.name="Platform Inscription";
 var inset=inscription.gameObject.AddComponent<Shadow>();inset.effectColor=new Color(.96f,.87f,.59f,.7f);inset.effectDistance=new Vector2(0,-1);
 var groundShadow=Rect("Seeker Ground Shadow",view,112,1092,166,36).gameObject.AddComponent<GuxuSoftVfx>();groundShadow.raycastTarget=false;groundShadow.color=new Color(.035f,.08f,.09f,.20f);GuxuParticleFx.Sort(groundShadow.transform,-31);
 seeker=Img("Game/Guxu/Final/Hero/hero",view,-25,513,400,527);seeker.preserveAspect=true;seeker.name="Seeker Character Layer";
 ancientStone=Img("Game/Guxu/Final/stone",view,425,505,190,210);ancientStone.preserveAspect=true;ancientStone.name="Sealed Stone Layer";
 var companion=Rect("Companion A Zhuan",view,515,390,145,125).gameObject.AddComponent<GuxuCompanionMotion>();companion.raycastTarget=false;
 unsealAnimation=view.gameObject.AddComponent<GuxuAnimation>();unsealAnimation.Bind(seeker,ancientStone,view);RefreshSealPresentation();
 GuxuParticleFx.Sort(seeker.transform,-20);GuxuParticleFx.Sort(ancientStone.transform,-20);GuxuParticleFx.Sort(companion.transform,-20);GuxuFootCloud.Attach(seeker);
 if(Rules.Save.pending!=null){var relic=Art(GuxuUiArt.Item(Rules.Save.pending.slot),view,437,500,150,150);relic.name="Pending Relic Display";}
 Header();DrawAccountEntry();

 FinalSideEntry("鉴宝台","home-entry-appraisal",GuxuUiArt.Icon.Codex,630,210,TreeUpgrade);
 FinalSideEntry("资源秘境","home-entry-dungeons",GuxuUiArt.Icon.Explore,630,350,ShowDungeons);
 FinalSideEntry("背包","home-entry-bag",GuxuUiArt.Icon.Inventory,630,490,ShowBag);

 unsealButton=HudAction("Unseal",view,320,751,300,88,Chop);
 var unsealArt=Art(GuxuUiArt.Optional("hud-unseal-button",true),unsealButton.transform,0,4,300,80,false);unsealArt.name="Unseal Gold Art";unsealArt.pixelsPerUnitMultiplier=.8f;unsealButton.targetGraphic=unsealArt;
 Txt("破  封",unsealButton.transform,25,4,250,80,36,new Color(.25f,.14f,.05f)).name="Unseal Label";
 Art(GuxuUiArt.Navigation(GuxuUiArt.Icon.SealToken),view,408,840,32,32);
 homeSealCost=HudText("消耗 ×1",view,449,837,168,38,24,cream);homeSealCost.name="Seal Token Cost";
 homeAutoButton=HudToggle(auto?"停止自动":"自动破封",auto?"停止自动":"自动破封",view,315,875,188,88,auto,ToggleAuto);
 homeRetentionButton=HudAction("保留规则",view,510,875,132,88,AutoSettings);HudText("保留规则 ›",homeRetentionButton.transform,0,0,132,88,24,cream,TextAnchor.MiddleCenter);
 RefreshSealPresentation();
 ShowDrop();
 if(Rules.Save.pending==null){
  bool complete=Rules.Save.stage>Stages.stages.Length;var mission=HudAction("GoQuestTarget",view,44,1034,662,146,Adventure);homeQuestButton=mission;
  HudText(complete?"主线 · 已通关":"主线 · "+CurrentStage.regionName,mission.transform,0,0,620,42,30,gold).name="Main Quest Title";
  string target=complete?"重访遗域":"前往 "+CurrentStage.regionName;
  var targetText=HudText(target+" ›",mission.transform,0,42,620,46,30,new Color(.48f,1,.85f));targetText.name="Main Quest Target";
  float lineWidth=Mathf.Min(620,targetText.preferredWidth);Img("",mission.transform,0,85,lineWidth,2,new Color(.48f,1,.85f)).name="Main Quest Underline";
  HudText(complete?"主线已通关 · 可继续挑战古塔与秘境":"目标："+CurrentStage.guardianName+" · 第 "+CurrentStage.id+" / "+Stages.stages.Length+" 关",mission.transform,0,96,640,40,25,cream).name="Main Quest Progress";
  mission.gameObject.SetActive(!busy&&!resolvingDrop);
 }
 string[] names={"洞府","修行","寻墟","图鉴","战备"};
 GuxuUiArt.Icon[] icons={GuxuUiArt.Icon.Sect,GuxuUiArt.Icon.Cultivation,GuxuUiArt.Icon.Ruins,GuxuUiArt.Icon.Codex,GuxuUiArt.Icon.Companion};
 Img("",view,0,1225,750,109,new Color(.055f,.15f,.20f,.95f));Img("",view,0,1225,750,2,new Color(.75f,.63f,.37f,.8f));
 for(int i=0;i<5;i++){int tab=i;var nav=Rect("Tab "+names[i],view,i*150,1225,150,109);if(i==2){var glow=Rect("Selected Tab Light",nav,15,-16,120,105).gameObject.AddComponent<GuxuSoftVfx>();glow.raycastTarget=false;glow.color=new Color(1,.76f,.28f,.25f);}Art(GuxuUiArt.Navigation(icons[i]),nav,34,-12,82,82);Txt(names[i],nav,5,65,140,36,25,gold);Btn("",nav,0,0,150,109,()=>Navigate(tab),null,Color.clear).name=names[i];}
 }
 void FinalSideEntry(string label,string key,GuxuUiArt.Icon icon,float x,float y,Action action){
  var root=Rect("Entry "+label,view,x,y,108,130);var sprite=GuxuUiArt.Optional(key);
  if(sprite)Art(sprite,root,0,0,108,130,false);else Art(GuxuUiArt.Navigation(icon),root,12,10,84,84);
  var text=Txt(label,root,2,99,104,30,22,cream);var outline=text.gameObject.AddComponent<Outline>();outline.effectColor=new Color(.04f,.13f,.18f,.95f);outline.effectDistance=new Vector2(1,-1);Btn("",root,0,0,108,130,action,null,Color.clear).name=label;
 }
 void RefreshSealPresentation(){
  bool ready=Rules.Save.pending==null&&!resolvingDrop;
  if(ancientStone)ancientStone.enabled=ready;
  if(unsealButton){unsealButton.interactable=ready&&Rules.Save.peaches>0;unsealButton.GetComponentInChildren<Text>().text=Rules.Save.peaches>0?"破  封":"破封符不足";}
  if(homeSealCost){homeSealCost.text=Rules.Save.peaches>0?"消耗 ×1":"消耗 ×1 · 不足";homeSealCost.color=Rules.Save.peaches>0?cream:new Color(1,.63f,.40f);}
  if(homeAutoButton)homeAutoButton.interactable=auto||!busy&&!resolvingDrop&&ready&&Rules.Save.peaches>0;
  if(homeRetentionButton)homeRetentionButton.interactable=!busy;
  if(homeQuestButton)homeQuestButton.gameObject.SetActive(ready&&!busy);
 }
 void RefreshHomeBalances(){if(ExpansionScreen!="home")return;if(homeTokens)homeTokens.text=Rules.Save.peaches.ToString();if(homePower)homePower.text="基础战力  "+Rules.Attributes.Power;if(coinText)coinText.text=Rules.Save.coins.ToString();if(homeAutoButton){homeAutoButton.name=auto?"停止自动":"自动破封";RefreshHudToggle(homeAutoButton,auto,homeAutoButton.name);}RefreshSealPresentation();}
 void Header(){
  var head=Rect("Player HUD",view,24,12,702,108);HudPortrait(head,0,3,84,"Home Player Portrait");HudAction("Rename Player",head,0,0,88,100,ShowRename);
  var identity=HudText("等级"+Rules.Save.level+"  "+PlayerNickname,head,98,0,255,32,27,cream);identity.name="Player Identity";identity.resizeTextForBestFit=true;identity.resizeTextMinSize=16;identity.supportRichText=false;
  homePower=HudText("基础战力  "+Rules.Attributes.Power,head,98,32,255,34,28,gold);homePower.name="Player Power";
  HudText(Rules.Realm.displayName,head,98,68,255,32,25,cream).name="Player Realm";
  Art(GuxuUiArt.Optional("hud-flourish"),head,258,93,80,20,false);
  HudAction("Cultivation Header",head,94,0,260,104,ShowCultivation);
  Art(GuxuUiArt.Navigation(GuxuUiArt.Icon.SealToken),head,365,20,37,37);homeTokens=HudText(Rules.Save.peaches.ToString(),head,405,14,120,45,27,cream);homeTokens.name="Seal Token Balance";
  spiritDustIcon=Art(GuxuUiArt.Navigation(GuxuUiArt.Icon.SpiritDust),head,535,20,37,37);spiritDustIcon.name="Spirit Dust Icon";coinText=HudText(Rules.Save.coins.ToString(),head,574,14,128,45,27,cream);coinText.name="Spirit Stone Balance";
  // Keep even ten-digit balances inside their own groups; identity never shares their rects.
  foreach(var amount in new[]{homeTokens,coinText}){amount.resizeTextForBestFit=true;amount.resizeTextMinSize=20;amount.resizeTextMaxSize=27;}
  Art(GuxuUiArt.Optional("hud-flourish"),head,432,58,72,18,false);Art(GuxuUiArt.Optional("hud-flourish"),head,612,58,72,18,false);
 }
 void ShowDrop(){
  if(lootPanel!=null)Destroy(lootPanel.gameObject);lootPanel=null;var g=Rules.Save.pending;if(g==null)return;
  lootPanel=HomePaperPanel("Relic Decision",15,983,720,224);GearCard(g,lootPanel,20,36,118,143);
  Txt(Rules.GearName(g)+" · "+GuxuUiArt.TierLabel(g.quality,g.level),lootPanel,158,25,350,38,27,ink,TextAnchor.MiddleLeft);
  int delta=Rules.AttributesWithGear(g).Power-Rules.Attributes.Power;Txt(delta==0?"基础战力不变":("基础战力 "+(delta>0?"↑ +":"↓ ")+delta),lootPanel,158,71,350,42,31,delta==0?ink:delta>0?new Color(.1f,.43f,.26f):new Color(.7f,.2f,.12f),TextAnchor.MiddleLeft);
  Txt("攻击力 +"+g.attack+"   生命值 +"+g.hp+"\n"+GameRules.GearAffix(Rules.Save.gear[g.slot])+" → "+GameRules.GearAffix(g),lootPanel,158,126,350,68,23,ink,TextAnchor.MiddleLeft).name="Drop Affix Comparison";
  SealButton("分解",lootPanel,535,35,160,65,()=>ResolveDrop(false)).name="Resolve Equipment";SealButton("装备",lootPanel,535,125,160,65,()=>ResolveDrop(true),true).name="Equip Relic";
 } int TableCost()=>Rules.TreeUpgradeCost;
 void ToggleAuto(){if(auto){auto=false;if(homeAutoButton){homeAutoButton.name="自动破封";RefreshHudToggle(homeAutoButton,false,"自动破封");homeAutoButton.interactable=!busy&&!resolvingDrop&&Rules.Save.pending==null&&Rules.Save.peaches>0;}return;}if(!CanPlay||busy||battle||modal!=null||resolvingDrop)return;if(Rules.Save.pending!=null){Notify("请先处理当前遗宝");return;}if(Rules.Save.peaches<=0){Notify("破封符不足");return;}auto=true;nextAuto=Time.time;Home();}
 public bool ShouldKeep(Gear g)=>g.quality>=Mathf.Clamp(Rules.Save.keepQuality,1,5)||Rules.AttributesWithGear(g).Power>Rules.Attributes.Power;
 void Navigate(int tab){if(busy)return;auto=false;if(tab==0)ShowFinalResidence();else if(tab==1)ShowGrowthHub();else if(tab==2)ShowWorldMap();else if(tab==3)ShowCodex();else ShowLoadout();}
 void ShowCultivation()=>ShowRealmPage();
 void ShowBag(){
  if(busy)return;auto=false;var p=OpenModal("背包 · 当前装备",740);
  var skin=p.Find("Modal Paper").GetComponent<Image>();skin.sprite=GuxuUiArt.Optional("panel-inventory",true)??PageArt("panel-dark");skin.type=Image.Type.Sliced;skin.pixelsPerUnitMultiplier=1;
  p.Find("Modal Header").GetComponent<Text>().color=cream;p.Find("Modal Separator").GetComponent<Image>().color=new Color(.56f,.61f,.58f,.5f);
  for(int i=0;i<12;i++){int slot=i;float x=42+i%4*154,y=112+i/4*174;GearCard(Rules.Save.gear[i],p,x,y,125,125,()=>ShowGear(slot));Txt(slots[i],p,x-4,y+128,133,34,24,cream);}
  Txt("点击装备查看属性 · 锻造等级随槽位保留",p,40,675,600,35,22,cream);
 }


 public void SkipCurrentUnseal(){if(unsealAnimation)unsealAnimation.SkipReveal();}
 IEnumerator ChopAnimation(){
  int version=screenVersion;var g=Rules.Save.pending;bool firstReward=PlayerPrefs.GetInt(FirstUnsealStoryKey,0)==0;
  Button skip=null;if(g.quality>=4){skip=SealButton("跳过演出",view,548,947,177,43,SkipCurrentUnseal);skip.name="Skip Unseal";}
  yield return unsealAnimation.PlayUnseal(GuxuUiArt.Item(0),GuxuUiArt.Item(g.slot),g.quality);
  if(version!=screenVersion)yield break;
  // Retain the existing one-time weapon policy independently of the retired tutorial cinematic.
  if(firstReward){PlayerPrefs.SetInt(FirstUnsealStoryKey,1);PlayerPrefs.Save();}
  if(skip)Destroy(skip.gameObject);busy=false;if(auto&&!ShouldKeep(g))ResolveDrop(false);else{auto=false;Home();}
 }

}
}















