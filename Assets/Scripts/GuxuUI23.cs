using UnityEngine;
using UnityEngine.UI;

namespace Gxdl {
public partial class GxdlGame {
 bool RedesignedPage=>ExpansionScreen!="world"&&ExpansionScreen!="chapter"&&ExpansionScreen!="growth";
 Sprite PageArt(string key,bool sliced=true)=>(RedesignedPage?GuxuUiArt.Optional("ui23-"+(key=="panel-decision"?"panel-modal":key=="panel-combat"?"panel-dark":key),sliced):null)??GuxuUiArt.Optional(key,sliced);
 RectTransform PaperCard(Transform parent,string name,float x,float y,float w,float h){
  var r=Rect(name,parent,x,y,w,h);var image=Art(GuxuUiArt.Optional("ui23-panel-modal",true)??GuxuUiArt.DecisionPanel,r,0,0,w,h,false);image.pixelsPerUnitMultiplier=3*image.sprite.rect.width/1024f;return r;
 }
 void StatCell(Transform parent,string label,string value,float x,float y,float w=280,Color? color=null){
  Img("",parent,x,y,w,76,new Color(.12f,.26f,.27f,.08f));
  Txt(label,parent,x+15,y+8,w-30,25,20,color??ink,TextAnchor.MiddleLeft);
  Txt(value,parent,x+15,y+34,w-30,35,29,color??ink,TextAnchor.MiddleLeft);
 }
 void PageSection(string value,float y){Txt(value,view,32,y,686,35,23,gold,TextAnchor.MiddleLeft);}
 void GemVisual(Transform parent,int encoded,float x,float y,float size){
  if(encoded>0)Art(GuxuItemIcons.Gem(encoded),parent,x,y,size,size);
  else {Img("",parent,x,y,size,size,new Color(.16f,.36f,.37f,.4f));Txt("◇",parent,x,y,size,size,38,gold);}
 }
}
}


