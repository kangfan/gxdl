using UnityEngine;
using UnityEngine.UI;

namespace Gxdl {
public partial class GxdlGame {
 RectTransform OpenHandbook(string title,float height){
  var p=OpenModal(title,height);var surface=p.Find("Modal Paper").GetComponent<Image>();
  modal.Find("Full Canvas Modal Shade").GetComponent<Image>().color=new Color(.01f,.05f,.07f,.62f);
  surface.sprite=GuxuUiArt.Optional("handbook-panel",true);surface.type=Image.Type.Sliced;surface.pixelsPerUnitMultiplier=2;
  var header=p.Find("Modal Header").GetComponent<Text>();header.rectTransform.anchoredPosition=new Vector2(82,-28);header.rectTransform.sizeDelta=new Vector2(482,54);
  p.Find("Modal Separator").gameObject.SetActive(false);return p;
 }
 void HandbookStat(Transform p,string label,string value,float x,float y){
  var tag=Art(GuxuUiArt.Optional("handbook-stat-tag",true),p,x,y,265,112,false);tag.pixelsPerUnitMultiplier=1;
  Txt(label,p,x+20,y+24,225,30,23,ink);Txt(value,p,x+20,y+57,225,44,31,ink).name="Handbook "+label;
 }
 void ShowFinalResidence(){
  if(busy)return;auto=false;var p=OpenHandbook("洞府 · 修行手记",745);
  Art(GuxuUiArt.Optional("handbook-residence-insert"),p,64,116,210,210).name="Residence Insert";
  Txt("古墟大陆 · 遗石初鸣",p,295,135,310,90,31,ink,TextAnchor.MiddleLeft);
  Txt("一符破古封，\n一剑问前尘。",p,295,236,310,74,26,ink,TextAnchor.MiddleLeft);
  HandbookStat(p,"当前境界",Rules.Realm.displayName,65,358);HandbookStat(p,"基础战力",Rules.Attributes.Power.ToString(),350,358);
  SealButton("同行档案",p,65,625,260,68,ShowFinalCompanion);SealButton("好友与聊天",p,350,625,265,68,ShowSocial);
  SealButton("重温开场",p,65,532,260,68,()=>{CloseModal();ReplayOpening();});SealButton("整理行囊",p,350,532,265,68,ShowBag,true);
 }
 void ShowFinalCompanion(){
  if(busy)return;auto=false;var p=OpenHandbook("阿篆 · 同行档案",720);
  Art(GuxuUiArt.Optional("state-selected"),p,204,112,272,272).color=new Color(.60f,.52f,.30f,.42f);
  var bird=Rect("Companion Preview",p,215,119,250,235).gameObject.AddComponent<GuxuCompanionMotion>();bird.raycastTarget=false;
  Txt("阿篆 · 青铜机关鸟",p,65,370,550,43,32,ink);
  Art(GuxuUiArt.Optional("state-cleared"),p,273,421,31,31);
  Txt("同行中",p,312,418,130,36,24,new Color(.13f,.42f,.31f)).name="Companion Status";
  Txt("守候遗址千年的引路者\n随你探索沉剑遗庭，寻找师父留下的线索。",p,65,469,550,83,25,ink);
  
  SealButton("查看随身装备",p,165,624,350,68,ShowBag,true);
 }
 void AutoSettings(){
  if(busy)return;auto=false;RefreshHomeBalances();var p=OpenHandbook("自动破封 · 保留规则",590);int q=Mathf.Clamp(Rules.Save.keepQuality,1,5);
  Txt("当前保留品质",p,65,111,550,32,24,ink);
  Txt(quality[q]+" 及以上",p,65,148,550,49,35,ink).name="Auto Quality Threshold";
  for(int i=1;i<=5;i++){
   int selected=i;float x=65+(i-1)*111;
   var tag=Art(GuxuUiArt.Optional("handbook-stat-tag",true),p,x,223,106,100,false);tag.pixelsPerUnitMultiplier=2;
   Img("",tag.transform,18,18,70,6,GuxuUiArt.QualityColor(i));
   Txt(GuxuUiArt.QualityName(i)+"品质",tag.transform,3,31,100,35,24,ink);
   if(q==i){Art(GuxuUiArt.Optional("state-cleared"),tag.transform,16,68,24,24);Txt("已选",tag.transform,40,67,59,27,18,new Color(.13f,.42f,.31f));}
   var choice=Btn("",p,x,220,106,106,()=>{Rules.Save.keepQuality=selected;Rules.Store();AutoSettings();},null,Color.clear);choice.name="Quality Choice "+i;
  }
  Txt("达到保留品质，或战力提升时暂停，\n由你决定装备或分解。",p,65,350,550,72,25,ink,TextAnchor.MiddleLeft);
  Txt("其余遗宝自动分解，已有装备不会被自动替换。",p,65,426,550,55,23,ink,TextAnchor.MiddleLeft);
  SealButton("确认",p,190,506,300,64,CloseModal,true);
 }
}
}
