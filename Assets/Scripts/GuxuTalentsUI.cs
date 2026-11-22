using System;
using Gxdl.Modules;
using UnityEngine;
using UnityEngine.UI;

namespace Gxdl {
public partial class GxdlGame {
 int talentBranch;
 readonly int[] talentSelections={0,8,16};
 readonly string[] talentSymbols={"attack","critical","combo","critical","break","combo","healing","attack","health","defense","counter","defense","shield","counter","health","shield","health","regenerate","speed","healing","healing","health","regenerate","healing"};
 readonly Vector2[] talentPoints={new Vector2(150,62),new Vector2(470,107),new Vector2(452,241),new Vector2(150,272),new Vector2(150,400),new Vector2(465,426),new Vector2(468,558),new Vector2(150,582)};
 RectTransform talentBackdrop,talentMap,talentFooter;
 readonly RectTransform[] talentNodes=new RectTransform[8];
 GuxuTalentTrail talentTrail;

 void FitTalentPage(){
  Canvas.ForceUpdateCanvases();float scale=Mathf.Min(root.rect.width/750f,root.rect.height/1334f),height=root.rect.height/scale;
  view.sizeDelta=new Vector2(750,height);view.anchorMin=view.anchorMax=new Vector2(.5f,.5f);view.pivot=new Vector2(0,1);view.anchoredPosition=new Vector2(-375,height*.5f)*scale;view.localScale=Vector3.one*scale;
  if(talentBackdrop)talentBackdrop.sizeDelta=new Vector2(750,height);
  if(talentFooter)talentFooter.anchoredPosition=new Vector2(0,-(height-435));
  if(talentMap)talentMap.sizeDelta=new Vector2(750,height-665);
  if(talentTrail){
   var points=new Vector2[8];for(int i=0;i<8;i++){points[i]=talentPoints[i]+new Vector2(0,(height-1334)*i/7f);if(talentNodes[i])talentNodes[i].anchoredPosition=new Vector2(points[i].x-56,-points[i].y+56);}
   talentTrail.rectTransform.sizeDelta=new Vector2(750,height-665);talentTrail.Points=points;talentTrail.SetVerticesDirty();
  }
 }
 void DrawTalentTree(int branch){
  auto=false;battle=false;Clear();ExpansionScreen="talents";Rules.EnsureExpansion();talentBranch=Mathf.Clamp(branch,0,2);
  talentBackdrop=null;talentMap=null;talentFooter=null;talentTrail=null;for(int i=0;i<8;i++)talentNodes[i]=null;
  fitExpandedPage=FitTalentPage;FitTalentPage();
  talentBackdrop=Art(GuxuUiArt.Optional("talent-environment"),view,0,0,750,view.rect.height,false).rectTransform;
  SealButton("‹ 返回修行",view,16,24,174,52,ShowGrowthHub).name="Talent Back";
  Art(GuxuUiArt.SecondaryButton,view,232,16,486,68,false).name="Talent Title Backing";
  Txt("悟道天赋",view,235,18,480,64,44,gold);
  Art(GuxuItemIcons.Get("resource-insight"),view,237,91,48,48);
  Txt("悟道点  "+Rules.Save.insight,view,297,90,260,48,30,cream,TextAnchor.MiddleLeft).name="Talent Balance";
  string[] branches={"攻伐","御守","灵息"},symbols={"attack","shield","regenerate"};
  for(int i=0;i<3;i++){
   int next=i;var tab=SealButton("",view,27+i*240,160,216,52,()=>DrawTalentTree(next));tab.name="Talent Branch "+i;
   tab.image.sprite=i==talentBranch?GuxuUiArt.PrimaryButton:GuxuUiArt.SecondaryButton;
   Art(GuxuUiArt.Optional("effect-"+symbols[i]),tab.transform,12,-6,64,64);
   Txt(branches[i],tab.transform,78,1,126,50,28,i==talentBranch?ink:cream);
  }
  talentMap=Rect("Talent Constellation",view,0,230,750,view.rect.height-665);
  talentTrail=Rect("Talent Single Chain",talentMap,0,0,750,talentMap.rect.height).gameObject.AddComponent<GuxuTalentTrail>();talentTrail.raycastTarget=false;
  talentTrail.Lit=new bool[7];for(int i=0;i<7;i++)talentTrail.Lit[i]=Rules.GrowthUnlocked(1)&&Rules.Save.talents[talentBranch*8+i]>0;
  for(int i=0;i<8;i++)DrawTalentNode(i);
  talentFooter=Rect("Talent Selected Detail",view,0,view.rect.height-435,750,435);DrawTalentDetail();FitTalentPage();
 }
 void DrawTalentNode(int index){
  int node=talentBranch*8+index,rank=Rules.Save.talents[node];bool locked=!Rules.GrowthUnlocked(1)||(index>0&&Rules.Save.talents[node-1]==0),selected=talentSelections[talentBranch]==node;
  var button=Btn("",talentMap,0,0,270,126,()=>{talentSelections[talentBranch]=node;DrawTalentTree(talentBranch);},null,Color.clear);button.name="Talent "+node;button.transition=Selectable.Transition.None;talentNodes[index]=(RectTransform)button.transform;
  if(selected)Art(GuxuUiArt.Optional("state-selected"),button.transform,-15,-15,142,142).name="Talent Selected Ring";
  var baseArt=Art(GuxuUiArt.Optional("talent-node-base"),button.transform,0,0,112,112);baseArt.name="Talent Node Base "+node;
  var symbol=Art(GuxuUiArt.Optional("talent-"+node.ToString("00"))??GuxuUiArt.Optional("effect-"+talentSymbols[node]),button.transform,21,16,70,70);symbol.name="Talent Symbol "+node;
  if(locked){if(!chapterLockedMaterial)chapterLockedMaterial=new Material(Resources.Load<Shader>("Game/Guxu/ChapterStone"));baseArt.material=symbol.material=chapterLockedMaterial;baseArt.color=symbol.color=new Color(.66f,.76f,.80f);Art(GuxuUiArt.Optional("state-lock"),button.transform,73,64,28,35);}
  else if(rank>0)baseArt.color=new Color(.78f,1f,.92f);
  Txt(GameRules.TalentNames[node],button.transform,122,17,142,38,29,selected?gold:cream,TextAnchor.MiddleLeft).name="Talent Name "+node;
  Txt(rank+" / 3"+(rank==3?" · 满级":""),button.transform,122,54,144,31,23,rank==3?gold:cream,TextAnchor.MiddleLeft).name="Talent Rank "+node;
  for(int j=0;j<3;j++){var pip=Rect("Talent Rank Pip "+j,button.transform,30+j*20,99,12,12).gameObject.AddComponent<GuxuPortraitDisc>();pip.color=j<rank?new Color(1f,.85f,.45f):new Color(.24f,.37f,.42f);pip.raycastTarget=false;}
 }
 void DrawTalentDetail(){
  int node=talentSelections[talentBranch],rank=Rules.Save.talents[node];bool available=Rules.CanTrainTalent(node);
  var tray=PaperCard(talentFooter,"Talent Paper Detail",25,0,700,337);
  Art(GuxuUiArt.Optional("talent-"+node.ToString("00"))??GuxuUiArt.Optional("effect-"+talentSymbols[node]),tray,28,19,82,82);
  Txt(GameRules.TalentNames[node],tray,127,19,310,50,36,ink,TextAnchor.MiddleLeft);
  Txt(rank+" / 3级",tray,478,24,185,42,28,ink,TextAnchor.MiddleRight).name="Talent Selected Rank";
  Txt("每级 "+GameRules.TalentDetails[node],tray,127,75,535,43,29,ink,TextAnchor.MiddleLeft).name="Talent Effect";
  string reason=!Rules.GrowthUnlocked(1)?"通关主线第10关后开放天赋":rank>=3?"本节点已修满":node%8>0&&Rules.Save.talents[node-1]==0?"需先修习「"+GameRules.TalentNames[node-1]+"」1级":!available?"悟道点不足，可前往悟道秘境获取":"三系可同时修习 · 每节点最多3级";
  Txt(reason,tray,32,131,635,40,25,ink,TextAnchor.MiddleLeft).name="Talent Requirement";
  string cost=rank>=3?"已满级":"修习消耗 "+Rules.TalentCost(node)+" 悟道点";
  Txt(cost,tray,35,185,360,39,27,ink,TextAnchor.MiddleLeft).name="Talent Cost";
  Txt("可用 "+Rules.Save.insight,tray,396,185,267,39,26,ink,TextAnchor.MiddleRight);
  string label=rank>=3?"已满级":!Rules.GrowthUnlocked(1)?"尚未开放":node%8>0&&Rules.Save.talents[node-1]==0?"前置未修习":!available?"悟道点不足":"修习 · "+Rules.TalentCost(node)+" 点";
  var train=SealButton(label,tray,65,240,570,80,()=>GrowthAction(()=>Rules.TrainTalent(node),()=>DrawTalentTree(talentBranch),"悟道点不足或前置未满足"),true);train.name="Talent Train";train.interactable=available;
  SealButton("免费重置",talentFooter,72,354,270,52,()=>{var panel=OpenModal("重置天赋",360);Txt("返还全部三系已投入的悟道点。\n当前所有节点等级清零。",panel,35,105,610,100,27,ink);SealButton("确认重置",panel,180,245,320,64,()=>{var before=Rules.Attributes;var traits=Rules.CombatTraits;Rules.ResetTalents();SaveAndShow(()=>DrawTalentTree(talentBranch),"悟道点已全部返还。\n"+GameRules.GrowthChange(before,Rules.Attributes,traits,Rules.CombatTraits));},true).name="Talent Reset Confirm";}).name="Talent Reset";
  SealButton("获取悟道点",talentFooter,408,354,270,52,()=>ShowDungeon(2)).name="Talent Get Insight";
 }
}

// A single curved path between each pair of prerequisite nodes. Art and labels stay independent.
[RequireComponent(typeof(CanvasRenderer))]
public sealed class GuxuTalentTrail:MaskableGraphic {
 public Vector2[] Points;public bool[] Lit;
 protected override void OnPopulateMesh(VertexHelper mesh){
  mesh.Clear();if(Points==null||Points.Length!=8)return;
  for(int i=0;i<7;i++){
   Vector2 a=Points[i],b=Points[i+1];float dx=b.x-a.x;Vector2 start,end,c1,c2;
   if(Mathf.Abs(dx)<90){start=a+new Vector2(-42,36);end=b+new Vector2(-42,-36);c1=start+new Vector2(-4,18);c2=end-new Vector2(-4,18);}
   else{float sign=Mathf.Sign(dx);start=a+new Vector2(sign*40,43);end=b+new Vector2(-sign*50,34);c1=start+new Vector2(sign*72,34);c2=end-new Vector2(sign*65,-28);}
   var tint=Lit!=null&&Lit[i]?new Color(1f,.79f,.40f,.95f):new Color(.49f,.68f,.72f,.88f);Vector2 previous=start;
   for(int step=1;step<=24;step++){float t=step/24f,u=1-t;Vector2 next=u*u*u*start+3*u*u*t*c1+3*u*t*t*c2+t*t*t*end;AddLine(mesh,previous,next,3.8f,tint);previous=next;}
   Vector2 direction=(end-c2).normalized,normal=new Vector2(-direction.y,direction.x);int v=mesh.currentVertCount;AddPoint(mesh,end,tint);AddPoint(mesh,end-direction*14+normal*7,tint);AddPoint(mesh,end-direction*14-normal*7,tint);mesh.AddTriangle(v,v+1,v+2);
  }
 }
 void AddPoint(VertexHelper mesh,Vector2 point,Color tint){var rect=rectTransform.rect;mesh.AddVert(new Vector3(rect.xMin+point.x,rect.yMax-point.y),tint,Vector2.zero);}
 void AddLine(VertexHelper mesh,Vector2 a,Vector2 b,float width,Color tint){Vector2 n=(b-a).normalized;n=new Vector2(-n.y,n.x)*width*.5f;int v=mesh.currentVertCount;AddPoint(mesh,a+n,tint);AddPoint(mesh,b+n,tint);AddPoint(mesh,b-n,tint);AddPoint(mesh,a-n,tint);mesh.AddTriangle(v,v+1,v+2);mesh.AddTriangle(v,v+2,v+3);}
}
}
