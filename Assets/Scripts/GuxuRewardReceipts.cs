using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Gxdl {
[Serializable] public sealed class GuxuRewardReceipt {
 public string id,accountScope,source,label;
 public int sourceId;
 public GuxuReward basis;
 public bool doubled;
}
public partial class GameRules {
 public GuxuRewardReceipt LastRewardReceipt=>Save.rewardReceipts==null?null:Save.rewardReceipts.LastOrDefault();
 public static void ValidateReward(GuxuReward r){if(r==null||r.seals<0||r.dust<0||r.ore<0||r.insight<0||r.exp<0||r.stones<0||r.gems<0||r.gemIndex< -1||r.gemIndex>23||r.gems>0&&r.gemIndex<0)throw new InvalidDataException("Invalid reward");}
 static bool HasReward(GuxuReward r)=>r.seals>0||r.dust>0||r.ore>0||r.insight>0||r.exp>0||r.stones>0||r.gems>0;
 static GuxuReward CopyReward(GuxuReward r)=>new GuxuReward{seals=r.seals,dust=r.dust,ore=r.ore,insight=r.insight,exp=r.exp,stones=r.stones,gems=r.gems,gemIndex=r.gemIndex};
 static void ValidateRewardReceipts(SaveData save){
  if(save.rewardReceipts==null)return;var ids=new HashSet<string>();
  foreach(var receipt in save.rewardReceipts){if(receipt==null||string.IsNullOrEmpty(receipt.id)||!ids.Add(receipt.id)||receipt.accountScope==null||receipt.accountScope.Length!=0&&!System.Text.RegularExpressions.Regex.IsMatch(receipt.accountScope,@"\A[a-f0-9]{64}\z")||!new[]{"main","chapter","dungeon","tower"}.Contains(receipt.source))throw new InvalidDataException("Invalid reward receipt");ValidateReward(receipt.basis);}
 }
 public string GrantRecorded(GuxuReward reward,string source,int sourceId,string label){
  ValidateReward(reward);if(!HasReward(reward))return null;
  var receipt=new GuxuRewardReceipt{id=Guid.NewGuid().ToString("N"),accountScope=rewardAccountScope,source=source,sourceId=sourceId,label=label,basis=CopyReward(reward)};
  Grant(reward);if(Save.rewardReceipts==null)Save.rewardReceipts=new List<GuxuRewardReceipt>();Save.rewardReceipts.Add(receipt);
  int remove=Save.rewardReceipts.Count(x=>x.doubled)-64;if(remove>0)Save.rewardReceipts.RemoveAll(x=>x.doubled&&remove-->0);
  return receipt.id;
 }
 public GuxuRewardReceipt FindRewardReceipt(string id)=>Save.rewardReceipts?.FirstOrDefault(x=>x.id==id&&x.accountScope==rewardAccountScope&&AccountScope==rewardAccountScope);
 public bool DoubleReward(string id){var receipt=FindRewardReceipt(id);if(receipt==null||receipt.doubled||!HasReward(receipt.basis))return false;var preview=new GuxuRewardReceipt{basis=receipt.basis,doubled=true};ReceiptTotal(preview);Grant(receipt.basis);receipt.doubled=true;return true;}
 public GuxuReward ReceiptTotal(GuxuRewardReceipt receipt){var r=CopyReward(receipt.basis);if(receipt.doubled){r.seals=checked(r.seals*2);r.dust=checked(r.dust*2);r.ore=checked(r.ore*2);r.insight=checked(r.insight*2);r.exp=checked(r.exp*2);r.stones=checked(r.stones*2);r.gems=checked(r.gems*2);}return r;}
}
}
