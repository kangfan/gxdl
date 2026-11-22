using System;
using UnityEngine;
using UnityEngine.UI;

namespace Gxdl {
public partial class GxdlGame {
 string DefeatAdvice(GuxuBattle simulation){
  if(Rules.Save.level<Rules.Config.realms.Length&&Rules.Save.exp>=Rules.ExpNeeded&&Rules.Save.breakthroughStones>=Rules.BreakthroughCost)return "已可突破境界：前往修行突破，提升属性后再挑战。";
  if(!Rules.GrowthUnlocked(0))return Rules.Save.peaches>0?"继续破封换上更强遗宝，积累修为后前往修行突破。":"重访已通关主线获取破封符，换装并积累修为突破。";
  if(!Rules.GrowthUnlocked(1)){
   for(int slot=0;slot<12;slot++)if(Rules.CanForge(slot))return "可先在修行中锻造装备，提升攻击与生存；继续破封积累修为。";
   return Rules.Save.dungeonClaims[0]<3?"挑战玄铁副本获取玄铁，再锻造装备；也可破封积累修为。":"继续破封换装并积累修为；玄铁副本收益次数每日恢复。";
  }
  if(simulation.Round>=simulation.Stage.maxRounds)return "回合耗尽：尝试御剑诀或碎甲剑，并锻造装备提高攻击。";
  if(simulation.Stage.enemyTraits.healPercent>0)return "敌人恢复较强：尝试御剑诀集中输出，并锻造装备提高攻击。";
  if(Rules.CanTrainTalent(8))return "可先学习坚壁天赋增加生命；尝试玄甲盾抵挡伤害。";
  if(Rules.GrowthUnlocked(2))for(int i=6;i<12;i++)if(Rules.Save.gemInventory[i]>0)return "可镶嵌碧生灵玉增加生命；尝试玄甲盾抵挡伤害。";
  return "生存不足：尝试玄甲盾，或在锻造中提升生命与防御。";
 }
 void ShowBattleResult(GuxuBattle simulation,GuxuReward reward,bool paid){
  if(combatParticles)combatParticles.enabled=false;
  var stage=simulation.Stage;
  var receipt=Rules.FindRewardReceipt(simulation.RewardReceiptId);if(receipt!=null)reward=Rules.ReceiptTotal(receipt);
  if(ShowBattleResultPrefab(simulation,reward,paid,receipt))return;
  foreach(Transform child in view)if(child!=combatBackdrop.transform&&child.GetComponent<GuxuEncounterBackdrop>()==null)child.gameObject.SetActive(false);
  bool finished=simulation.Won&&(stage.mode==0&&stage.id==200||stage.mode==2&&stage.difficulty==100);
  string completeLabel=stage.mode==0?"主线已通关":"百层已通关";
  string[] rewards=paid?reward.ToString().Split(new[]{"  "},StringSplitOptions.RemoveEmptyEntries):new string[0];
  float contentTop=finished?308:270;
  float contentBottom=contentTop+(simulation.Won?(rewards.Length>0?46+Mathf.CeilToInt(rewards.Length/3f)*184:112):182);
  string unlock=simulation.Won&&paid&&stage.mode==0?UnlockMessage(stage.id):"";
  float statsTop=contentBottom+18+(unlock.Length>0?54:0),actionsTop=statsTop+94,height=actionsTop+(finished?102:174)+(receipt!=null?86:0);
  var p=OpenModal(simulation.Won?"挑战成功":"挑战未果",height);
  var close=p.Find("Close Modal").GetComponent<Button>();close.onClick.RemoveAllListeners();close.onClick.AddListener(()=>ReturnToEncounter(stage));
  modal.Find("Full Canvas Modal Shade").GetComponent<Image>().color=new Color(.01f,.05f,.07f,.68f);
  var paper=p.Find("Modal Paper").GetComponent<Image>();paper.rectTransform.anchoredPosition=new Vector2(0,-226);paper.rectTransform.sizeDelta=new Vector2(680,height-226);
  p.Find("Modal Header").gameObject.SetActive(false);p.Find("Modal Separator").gameObject.SetActive(false);
  var emblem=Art(GuxuUiArt.Optional(simulation.Won?"settlement-victory-emblem":"settlement-defeat-emblem"),p,50,0,580,220);emblem.name="Settlement Emblem";emblem.rectTransform.pivot=Vector2.one*.5f;emblem.rectTransform.anchoredPosition=new Vector2(340,-110);
  var result=Txt(simulation.Won?"破 阵 凯 旋":"重 整 行 装",p,45,simulation.Won?115:8,590,70,52,simulation.Won?gold:cream);result.name="Settlement Result";
  var outline=result.gameObject.AddComponent<Outline>();outline.effectColor=new Color(.09f,.13f,.12f,.95f);outline.effectDistance=new Vector2(1,-2);
  Art(GuxuUiArt.SecondaryButton,p,50,192,580,54,false);
  Txt(stage.name,p,72,199,536,40,29,cream).name="Settlement Stage";
  if(finished){var complete=SealButton(completeLabel,p,165,251,350,46,()=>{},true);complete.name=completeLabel;var colors=complete.colors;colors.disabledColor=Color.white;complete.colors=colors;complete.interactable=false;}
  if(simulation.Won){
   Txt(rewards.Length>0?(receipt!=null&&receipt.doubled?"双 倍 奖 励 · 合 计":"基 础 奖 励 · 已 到 账"):"挑战完成",p,55,contentTop,570,39,30,ink).name="Settlement Reward Heading";
   if(rewards.Length>0)SettlementRewards(reward,rewards,p,contentTop+46);
   else Txt("本次无可领取奖励",p,55,contentTop+49,570,48,26,ink).name="Settlement No Reward";
  }else{
   string advice=DefeatAdvice(simulation);
   Art(GuxuUiArt.Optional("effect-shield")??GuxuUiArt.BattlePrepSkill(1),p,65,contentTop+30,85,85);
   Txt("整备后再战",p,175,contentTop,440,42,31,ink,TextAnchor.MiddleLeft);
   Txt(advice,p,175,contentTop+52,440,111,27,ink,TextAnchor.MiddleLeft).name="Settlement Advice";
  }
  if(unlock.Length>0)Txt(unlock,p,60,contentBottom,560,54,23,new Color(.15f,.48f,.35f)).name="Settlement Unlock";
  Img("",p,50,statsTop,580,1,new Color(.44f,.39f,.25f,.35f));
  string[] labels={"回合","输出","承伤","治疗"};int[] values={simulation.Round,simulation.DamageDone,simulation.DamageTaken,simulation.HealingDone};
  for(int i=0;i<4;i++){float x=45+i*147.5f;Txt(labels[i],p,x,statsTop+9,147.5f,29,23,ink);Txt(values[i].ToString(),p,x,statsTop+39,147.5f,36,29,ink).name="Settlement Stat "+i;if(i>0)Img("",p,x,statsTop+17,1,51,new Color(.44f,.39f,.25f,.22f));}
  GuxuStage next=stage.mode==0?Stages.ForProgress(Rules.Save.stage):stage.mode==2?Stages.tower[Math.Min(99,Rules.Save.towerFloor-1)]:Stages.Dungeon(stage.contentId,Math.Min(10,stage.difficulty+1));
  Action returnToEncounter=()=>ReturnToEncounter(stage);
  Action growth=()=>{CloseModal();ShowGrowthHub();};
  if(receipt!=null){string id=receipt.id;var bonus=SealButton(receipt.doubled?"已领取双倍奖励":"▷ 双倍奖励 · 点击即领",p,65,actionsTop,550,68,()=>ClaimRewardBonus(id,()=>ShowBattleResult(simulation,reward,paid)),true);bonus.name="Reward Double";bonus.interactable=!receipt.doubled;actionsTop+=86;}
  if(finished){var back=SealButton(stage.mode==0?"返回章节":"返回古塔",p,45,actionsTop,285,68,returnToEncounter,true);back.name="返回";SealButton("调整养成",p,350,actionsTop,285,68,growth);}
  else{
   string nextLabel=simulation.Won?"继续挑战":"再次挑战";
   var challenge=SealButton(nextLabel,p,45,actionsTop,285,68,()=>ShowBattlePrep(simulation.Won?next:stage),true);challenge.interactable=!simulation.Won||GuxuStageRules.CanEnter(next,Rules.Save);
   SealButton("调整养成",p,350,actionsTop,285,68,growth);
   var back=Btn("返回",p,200,actionsTop+88,280,52,returnToEncounter,null,Color.clear,25);back.name="返回";back.GetComponentInChildren<Text>().color=ink;
  }
 }
 // prefab 版结算。宿主未就绪或 prefab 缺失时返回 false，回退旧弹窗。
 bool ShowBattleResultPrefab(GuxuBattle simulation,GuxuReward reward,bool paid,GuxuRewardReceipt receipt){
  var stage=simulation.Stage;
  var page=OpenScreen<Gxdl.Modules.SettlementView>(Gxdl.Modules.UiScreens.Settlement);
  if(page==null)return false;
  CloseModal();
  bool finished=simulation.Won&&(stage.mode==0&&stage.id==200||stage.mode==2&&stage.difficulty==100);
  GuxuStage next=stage.mode==0?Stages.ForProgress(Rules.Save.stage):stage.mode==2?Stages.tower[Math.Min(99,Rules.Save.towerFloor-1)]:Stages.Dungeon(stage.contentId,Math.Min(10,stage.difficulty+1));
  // prefab 会被缓存复用，重进结算前清掉上一轮监听，避免叠绑。
  if(page.Close)page.Close.onClick.RemoveAllListeners();
  if(page.Back)page.Back.onClick.RemoveAllListeners();
  if(page.Growth)page.Growth.onClick.RemoveAllListeners();
  if(page.Challenge)page.Challenge.onClick.RemoveAllListeners();
  if(page.Bonus)page.Bonus.onClick.RemoveAllListeners();
  if(page.Close)page.Close.onClick.AddListener(()=>{CloseScreen();ReturnToEncounter(stage);});
  if(page.Back)page.Back.onClick.AddListener(()=>{CloseScreen();ReturnToEncounter(stage);});
  if(page.Growth)page.Growth.onClick.AddListener(()=>{CloseScreen();CloseModal();ShowGrowthHub();});
  if(page.Challenge)page.Challenge.onClick.AddListener(()=>{CloseScreen();ShowBattlePrep(simulation.Won?next:stage);});
  if(page.Bonus){
   bool hasReceipt=receipt!=null;
   page.Bonus.gameObject.SetActive(hasReceipt);
   if(hasReceipt){
    string id=receipt.id;
    page.Bonus.interactable=!receipt.doubled;
    var bonusLabel=page.Bonus.GetComponentInChildren<Text>();
    if(bonusLabel)bonusLabel.text=receipt.doubled?"已领取双倍奖励":"▷ 双倍奖励 · 点击即领";
    page.Bonus.onClick.AddListener(()=>ClaimRewardBonus(id,()=>ShowBattleResult(simulation,reward,paid)));
   }
  }
  string[] labels=paid?reward.ToString().Split(new[]{"  "},StringSplitOptions.RemoveEmptyEntries):new string[0];
  var cells=SettlementCells(reward,labels);
  var model=SettlementModel(simulation,labels,cells,finished,receipt);
  page.Apply(model);
  page.ApplyRewards(cells);
  page.SetStats(simulation.Round,simulation.DamageDone,simulation.DamageTaken,simulation.HealingDone);
  // 判定与旧页一致：下关是否可进决定按钮可用，继续/再次挑战按胜败切换。
  page.SetChallenge(!simulation.Won||GuxuStageRules.CanEnter(next,Rules.Save),simulation.Won?"继续挑战":"再次挑战");
  return true;
 }
 Gxdl.Modules.SettlementView.Model SettlementModel(GuxuBattle simulation,string[] labels,Gxdl.Modules.SettlementView.RewardCell[] cells,bool finished,GuxuRewardReceipt receipt){
  var stage=simulation.Stage;
  bool paid=labels.Length>0;
  string unlock=simulation.Won&&paid&&stage.mode==0?UnlockMessage(stage.id):"";
  // 有回执时合计已含双倍；标题文案与旧页同源。
  string heading=labels.Length==0?"挑战完成":receipt!=null&&receipt.doubled?"双 倍 奖 励 · 合 计":"基 础 奖 励 · 已 到 账";
  return new Gxdl.Modules.SettlementView.Model{Won=simulation.Won,Finished=finished,
   CompleteLabel=stage.mode==0?"主线已通关":"百层已通关",StageName=stage.name,
   RewardHeading=heading,Rewards=cells,
   DefeatAdvice=simulation.Won?"":DefeatAdvice(simulation),Unlock=unlock,
   Round=simulation.Round,DamageDone=simulation.DamageDone,DamageTaken=simulation.DamageTaken,HealingDone=simulation.HealingDone,
   HasReceipt=receipt!=null,ReceiptDoubled=receipt!=null&&receipt.doubled,ChallengeLabel=simulation.Won?"继续挑战":"再次挑战"};
 }
 // 结算奖励拆分与旧版一致：「破封符 +5」按最后空格拆成名称与数量，图标编号同一套。
 Gxdl.Modules.SettlementView.RewardCell[] SettlementCells(GuxuReward reward,string[] labels){
  var cells=new System.Collections.Generic.List<Gxdl.Modules.SettlementView.RewardCell>();
  int[] counts={reward.seals,reward.dust,reward.ore,reward.insight,reward.exp,reward.stones,reward.gems},icons={0,1,3,4,2,5,-1};
  for(int i=0;i<counts.Length;i++){
   if(counts[i]<=0||cells.Count>=labels.Length)continue;
   string label=labels[cells.Count];int split=label.LastIndexOf(' ');
   cells.Add(new Gxdl.Modules.SettlementView.RewardCell{Name=split>=0?label.Substring(0,split):label,Value=split>=0?label.Substring(split+1):"",IconKind=icons[i],GemIndex=icons[i]<0?reward.gemIndex:-1});
  }
  return cells.ToArray();
 }
 void SettlementRewards(GuxuReward reward,string[] labels,Transform parent,float y){
  int[] counts={reward.seals,reward.dust,reward.ore,reward.insight,reward.exp,reward.stones,reward.gems},icons={0,1,3,4,2,5,-1};int item=0;
  int columns=Math.Min(3,labels.Length);float width=590f/columns;
  for(int i=0;i<counts.Length;i++){
   if(counts[i]<=0)continue;float x=45+item%columns*width,top=y+item/columns*184;
   ItemCell(icons[i]<0?GuxuItemIcons.Gem(reward.gemIndex+1):GuxuUiArt.BattlePrepReward(icons[i]),parent,x+(width-112)*.5f,top,112).name="Settlement Reward Icon "+item;
   int split=labels[item].LastIndexOf(' ');string name=split>=0?labels[item].Substring(0,split):labels[item],value=split>=0?labels[item].Substring(split+1):"";
   Txt(name,parent,x+3,top+113,width-6,29,24,ink).name="Settlement Reward Name "+item;
   float countLeft=x+(width-112)*.5f+12;Img("",parent,countLeft,top+5,94,28,new Color(.02f,.04f,.05f,.82f));var count=Txt(value,parent,countLeft+2,top+4,90,30,23,cream,TextAnchor.MiddleRight);count.resizeTextForBestFit=true;count.resizeTextMinSize=14;count.resizeTextMaxSize=23;count.name="Settlement Reward Value "+item;item++;
  }
 }
}
}
