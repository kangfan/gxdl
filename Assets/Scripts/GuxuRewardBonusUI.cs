using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace Gxdl {
public partial class GxdlGame {
 bool RewardMatches(GuxuRewardReceipt r,string source)=>r.accountScope==GameRules.AccountScope&&(r.source==source||source=="main"&&r.source=="chapter");
 string RewardHistoryLabel(string source){int count=Rules.Save.rewardReceipts?.Count(r=>RewardMatches(r,source)&&!r.doubled)??0;return "▷ 奖励加倍"+(count>0?" "+count:"");}
 void RewardHistoryButton(Transform parent,string source,float x,float y,float width){
  var b=SealButton(RewardHistoryLabel(source),parent,x,y,width,48,()=>ShowRewardHistory(source,0));b.name="Reward History "+source;
 }
 void RefreshRewardHistoryButtons(){
  const string prefix="Reward History ";
  foreach(var button in view.GetComponentsInChildren<Button>())if(button.name.StartsWith(prefix))button.GetComponentInChildren<Text>().text=RewardHistoryLabel(button.name.Substring(prefix.Length));
 }
 void ShowRewardHistory(string source,int page){
  var rows=Rules.Save.rewardReceipts?.Where(r=>RewardMatches(r,source)).Reverse().OrderBy(r=>r.doubled).ToArray()??new GuxuRewardReceipt[0];int pages=Math.Max(1,(rows.Length+4)/5);page=Mathf.Clamp(page,0,pages-1);
  var p=OpenModal("奖励记录 · "+(page+1)+" / "+pages,720);
  Txt("基础奖励已到账 · 双倍入口当前点击即领",p,35,106,610,52,25,ink).name="Reward History Hint";
  if(rows.Length==0)Txt("暂无可加倍的奖励记录",p,45,245,590,80,28,ink);
  foreach(var r in rows.Skip(page*5).Take(5).Select((value,index)=>new{value,index})){
   string id=r.value.id;var b=SealButton(r.value.label+"\n"+(r.value.doubled?"已领取双倍奖励":"▷ 额外领取一份"),p,35,178+r.index*83,610,72,()=>ShowRewardReceipt(id,()=>ShowRewardHistory(source,page)));b.name="Reward Receipt "+r.index;
   var text=b.GetComponentInChildren<Text>();text.fontSize=24;text.resizeTextForBestFit=true;text.resizeTextMinSize=20;text.resizeTextMaxSize=24;
  }
  var previous=SealButton("‹ 上页",p,40,627,180,56,()=>ShowRewardHistory(source,page-1));previous.name="Reward Previous";previous.interactable=page>0;
  var next=SealButton("下页 ›",p,460,627,180,56,()=>ShowRewardHistory(source,page+1));next.name="Reward Next";next.interactable=page<pages-1;
 }
 void ShowRewardReceipt(string id,Action back=null){
  var receipt=Rules.FindRewardReceipt(id);if(receipt==null){Notify("奖励记录已失效或账号已变化");return;}
  var p=OpenModal("奖励明细",620);
  Txt(receipt.label,p,40,105,600,58,30,ink).name="Reward Receipt Source";
  Txt("基础已到账\n"+receipt.basis,p,45,180,590,132,26,ink).name="Reward Receipt Base";
  Txt(receipt.doubled?"额外已领取\n"+receipt.basis:"点击后额外获得\n"+receipt.basis,p,45,320,590,132,26,ink).name="Reward Receipt Extra";
  var claim=SealButton(receipt.doubled?"已领取双倍奖励":"▷ 双倍奖励 · 点击即领",p,65,472,550,72,()=>ClaimRewardBonus(id,()=>ShowRewardReceipt(id,back)),true);claim.name="Reward Double";claim.interactable=!receipt.doubled;
  if(back!=null){var close=p.Find("Close Modal").GetComponent<Button>();close.onClick.RemoveAllListeners();close.onClick.AddListener(()=>{CloseModal();back();});}
 }
 void ClaimRewardBonus(string id,Action refresh){
  if(!CanPlay)return;
  try{if(!Rules.DoubleReward(id)){refresh();Notify("该奖励已领取或已失效");return;}Rules.Store();RefreshHomeBalances();RefreshRewardHistoryButtons();refresh();Notify("已领取双倍奖励");}
  catch(OverflowException){Notify("资源数量已达上限，本次未领取");}
 }
}
}
