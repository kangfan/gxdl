using System;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

namespace Gxdl {

public partial class GxdlGame {
 bool socialBusy;
 string PlayerNickname=>string.IsNullOrWhiteSpace(Rules.Save.nickname)?"寻墟者":Rules.Save.nickname;
 InputField SocialInput(Transform parent,float x,float y,float width,string hint,int limit){
  var input=AccountInput("Social Input",parent,x,y,width,hint,limit,false);
  input.contentType=InputField.ContentType.Standard;input.keyboardType=TouchScreenKeyboardType.Default;input.characterLimit=limit;
  input.textComponent.supportRichText=false;return input;
 }
 void ShowRename(){
  if(!CanPlay||busy)return;var p=OpenModal("修改昵称",440);
  var input=SocialInput(p,50,125,580,"2至12个汉字、字母或数字",12);input.text=PlayerNickname;
  Txt("昵称保存在当前账号角色中。",p,50,215,580,50,24,ink);
  SealButton("保存昵称",p,180,315,320,70,async()=>{
   string name=(input.text??"").Normalize(NormalizationForm.FormKC).Trim();
   if(!System.Text.RegularExpressions.Regex.IsMatch(name,@"\A[\p{IsCJKUnifiedIdeographs}A-Za-z0-9]{2,12}\z")){Notify("昵称需为2至12个汉字、字母或数字。");return;}
   if(socialBusy)return;
   string scope=accounts?accounts.SaveScope:"";int version=screenVersion;
   if(accounts&&!string.IsNullOrEmpty(accounts.SocialToken)){
    var reply=await SocialRequest("rename",new{nickname=name});if(reply==null)return;
   }
   if(!CanPlay||version!=screenVersion||(accounts?accounts.SaveScope:"")!=scope)return;
   Rules.Save.nickname=name;Rules.Store();CloseModal();Home();Notify("昵称已更新。");
  },true);
 }
 async Task<JObject> SocialRequest(string action,object payload){
  if(socialBusy)return null;
  if(!accounts||string.IsNullOrEmpty(accounts.SocialToken)){Notify("请使用已接通账号服务的手机号账号登录社交功能。");return null;}
  string scope=accounts.SaveScope,url=accounts.SocialBaseUrl,token=accounts.SocialToken;int version=screenVersion;var origin=modal;
  if(!GuxuAccountSession.SocialAddressAllowed(url)){Notify("社交服务尚未配置。");return null;}
  socialBusy=true;
  try{
   using(var request=new UnityWebRequest(url.TrimEnd('/')+"/v1/social/"+action,"POST")){
    request.uploadHandler=new UploadHandlerRaw(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(payload)));request.downloadHandler=new DownloadHandlerBuffer();request.timeout=15;request.redirectLimit=0;
    request.SetRequestHeader("Content-Type","application/json");request.SetRequestHeader("Authorization","Bearer "+token);
    var operation=request.SendWebRequest();while(!operation.isDone)await Task.Yield();
    if(!this||!CanPlay||accounts.SaveScope!=scope||screenVersion!=version||modal!=origin)return null;
    JObject response=null;try{response=JObject.Parse(request.downloadHandler.text);}catch(JsonException){}
    if(request.result!=UnityWebRequest.Result.Success){Notify((string)response?["message"]??"社交服务连接失败，请稍后重试。");return null;}
    if(response==null){Notify("社交服务返回异常。");return null;}return response;
   }
  }catch(Exception){if(this&&CanPlay)Notify("社交服务连接失败，请稍后重试。");return null;}
  finally{socialBusy=false;}
 }
 async void ShowSocial(){
  if(!CanPlay||busy||socialBusy)return;
  int connectionVersion=screenVersion;var connectionOrigin=modal;
  if(accounts&&string.IsNullOrEmpty(accounts.SocialToken)){socialBusy=true;try{await accounts.EnsureSocialSession();}finally{socialBusy=false;}if(!this||!CanPlay||screenVersion!=connectionVersion||modal!=connectionOrigin)return;}
  if(!accounts||string.IsNullOrEmpty(accounts.SocialToken)){
   var unavailable=OpenModal("好友与聊天",460);
   Txt("好友与私聊使用真实账号服务。\n当前未连接社交服务，请连接服务后重试。",unavailable,45,125,590,130,27,ink);
   SealButton("修改昵称",unavailable,65,330,260,65,ShowRename);SealButton("重试连接",unavailable,355,330,260,65,ShowSocial);return;
  }
  int version=screenVersion;var reply=await SocialRequest("snapshot",new{});if(reply==null||!CanPlay||version!=screenVersion)return;
  var profile=reply["profile"] as JObject;if(profile!=null){Rules.Save.nickname=(string)profile["nickname"];Rules.Store();}
  DrawFriends(reply,0);
 }
 void DrawFriends(JObject data,int page){
  var p=OpenModal("好友与聊天",1070);var profile=data["profile"] as JObject;
  var nickname=Txt(profile==null?"请先设置昵称":(string)profile["nickname"],p,45,105,400,40,27,ink,TextAnchor.MiddleLeft);nickname.supportRichText=false;
  SealButton("改名",p,465,100,165,55,ShowRename);
  var search=SocialInput(p,45,170,410,"输入完整昵称",12);
  SealButton("搜索",p,465,175,165,62,async()=>{int version=screenVersion;var result=await SocialRequest("search",new{nickname=search.text});if(result==null||!CanPlay||version!=screenVersion)return;var players=result["players"] as JArray;if(players==null||players.Count==0){Notify("没有找到该昵称的玩家。");return;}var target=players[0];var box=OpenModal("搜索玩家",420);Txt((string)target["nickname"],box,45,125,590,80,30,ink).supportRichText=false;SealButton("申请好友",box,180,290,320,65,async()=>{if(await SocialRequest("request",new{target=(string)target["id"]})!=null)ShowSocial();},true);});
  var friends=data["friends"] as JArray??new JArray();int start=page*6;
  Txt("好友及申请 "+friends.Count+" / 100",p,45,260,590,40,24,ink,TextAnchor.MiddleLeft);
  for(int i=start;i<Math.Min(friends.Count,start+6);i++){
   var friend=friends[i];string id=(string)friend["id"];bool accepted=(int)friend["accepted"]==1,incoming=!accepted&&(string)friend["requester"]!=(string)profile?["id"];
   float y=320+(i-start)*95;Txt((string)friend["nickname"]+(accepted?"":incoming?" · 申请好友":" · 已申请"),p,45,y,295,62,24,ink,TextAnchor.MiddleLeft).supportRichText=false;
   if(accepted)SealButton("私聊",p,350,y,125,62,()=>ShowPrivateChat(id,(string)friend["nickname"]));
   else if(incoming)SealButton("接受",p,350,y,125,62,async()=>{if(await SocialRequest("accept",new{target=id})!=null)ShowSocial();});
   SealButton(accepted?"删除":"取消",p,490,y,140,62,()=>{var confirm=OpenModal("确认移除",390);Txt(accepted?"移除该好友后将无法继续私聊。":"取消或拒绝该好友申请。",confirm,45,115,590,100,27,ink);SealButton("确认",confirm,180,275,320,65,async()=>{if(await SocialRequest("remove",new{target=id})!=null)ShowSocial();});});
  }
  if(friends.Count==0)Txt("暂无好友，请搜索昵称发送申请。",p,45,350,590,80,27,ink);
  var prev=SealButton("上一页",p,45,945,170,62,()=>DrawFriends(data,page-1));prev.interactable=page>0;
  SealButton("刷新",p,255,945,170,62,ShowSocial);
  var next=SealButton("下一页",p,465,945,170,62,()=>DrawFriends(data,page+1));next.interactable=start+6<friends.Count;
 }
 async void ShowPrivateChat(string target,string nickname){
  if(!CanPlay||socialBusy)return;int version=screenVersion;var reply=await SocialRequest("messages",new{target});if(reply==null||!CanPlay||version!=screenVersion)return;
  var p=OpenModal("与 "+nickname+" 私聊",1080);
  var viewport=Rect("Chat Viewport",p,45,115,590,650);viewport.gameObject.AddComponent<RectMask2D>();var hit=viewport.gameObject.AddComponent<Image>();hit.color=Color.clear;hit.raycastTarget=true;
  var content=Rect("Chat Content",viewport,0,0,590,650);var messages=reply["messages"] as JArray??new JArray();var text=new StringBuilder();
  foreach(var message in messages)text.Append((string)message["sender"]==target?nickname:"我").Append("：").Append((string)message["body"]).Append("\n\n");
  var history=Txt(text.Length==0?"暂无消息。":text.ToString(),content,0,0,570,650,25,ink,TextAnchor.UpperLeft);history.supportRichText=false;
  float height=Mathf.Max(650,history.preferredHeight+20);history.rectTransform.sizeDelta=new Vector2(570,height);content.sizeDelta=new Vector2(590,height);
  var scroll=viewport.gameObject.AddComponent<ScrollRect>();scroll.viewport=viewport;scroll.content=content;scroll.horizontal=false;scroll.movementType=ScrollRect.MovementType.Clamped;scroll.verticalNormalizedPosition=0;
  var input=SocialInput(p,45,795,590,"输入消息，最多200字",200);
  SealButton("返回好友",p,45,940,170,65,ShowSocial);SealButton("刷新",p,255,940,170,65,()=>ShowPrivateChat(target,nickname));
  SealButton("发送",p,465,940,170,65,async()=>{if(await SocialRequest("send",new{target,message=input.text})!=null)ShowPrivateChat(target,nickname);},true);
 }
}
}

