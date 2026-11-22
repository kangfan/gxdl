using UnityEngine;
using UnityEngine.UI;

namespace Gxdl {
public partial class GxdlGame {
 void DrawRuinsMap(){
  Vector2[] positions={new Vector2(100,165),new Vector2(300,210),new Vector2(485,235)};
  Vector2[] entrances={new Vector2(70,48),new Vector2(65,20),new Vector2(65,65)};
  Color[] light={new Color(.36f,.85f,1f,.12f),new Color(.46f,1f,.75f,.12f),new Color(1f,.83f,.41f,.12f)};
  for(int i=0;i<positions.Length;i++){
   int region=Mathf.Clamp((Rules.Save.stage-1)/20,0,7)+i;bool unlocked=IsRuinsRegionUnlocked(region);var first=Stages.stages[region*20];
   var node=Rect("Ruins Region "+i,view,positions[i].x,positions[i].y,220,178);
   var touch=node.gameObject.AddComponent<Image>();touch.color=Color.clear;
   var button=node.gameObject.AddComponent<Button>();button.targetGraphic=touch;button.interactable=unlocked;button.transition=Selectable.Transition.None;
   button.onClick.AddListener(()=>OpenRuinsRegion(region));
   var mountain=Art(MapMountainSprite(i,positions[i]),node,0,0,220,178,false);mountain.name="Dungeon Mountain";
   var halo=Rect("Mountain Spirit Light",node,entrances[i].x,entrances[i].y,120,108).gameObject.AddComponent<GuxuSoftVfx>();halo.color=light[i];halo.raycastTarget=false;
   var label=Rect("Ruins Transient Label",node,20,110,180,48);
   var group=label.gameObject.AddComponent<CanvasGroup>();group.alpha=0;group.blocksRaycasts=false;group.interactable=false;
   var title=Txt(first.regionName,label,0,0,180,25,19,unlocked?new Color(1f,.98f,.85f):new Color(.88f,.92f,.94f));title.fontStyle=FontStyle.Bold;
   var edge=title.gameObject.AddComponent<Outline>();edge.effectColor=new Color(.08f,.20f,.25f,.95f);edge.effectDistance=new Vector2(1,-1);
   int room=Mathf.Clamp(Rules.Save.stage-first.id+1,1,20);
   string state=!unlocked?"未解锁":Rules.Save.stage>first.id+19?"已开放 · 可重访":"可挑战 · 第"+room+"关";
   var caption=Txt(state,label,0,25,180,22,15,unlocked?new Color(.74f,.96f,.90f):new Color(.82f,.87f,.9f));
   var shadow=caption.gameObject.AddComponent<Outline>();shadow.effectColor=new Color(.07f,.18f,.24f,.95f);shadow.effectDistance=new Vector2(1,-1);
   node.gameObject.AddComponent<GuxuMapBeacon>().Bind(halo,unlocked,i);
   if(!unlocked)halo.gameObject.SetActive(false);
   label.gameObject.AddComponent<GuxuRuinsTransientLabel>().Bind(group,unlocked);
  }
 }
 Sprite MapMountainSprite(int index,Vector2 position){
  string key="Ruins Mountain "+index;if(sprites.TryGetValue(key,out var sprite))return sprite;
  // Matching crops keep independent mountain layers seamless with the continuous landscape.
  var texture=(GuxuUiArt.Optional("screen-home")??Sprite("Game/Guxu/Final/background")).texture;
  var region=new Rect(position.x/750*texture.width,(1334-position.y-178)/1334*texture.height,220f/750*texture.width,178f/1334*texture.height);
  sprite=UnityEngine.Sprite.Create(texture,region,new Vector2(.5f,.5f),100,0,SpriteMeshType.FullRect);sprites[key]=sprite;return sprite;
 }
 void HighlightRuinsMap(){
  foreach(var beacon in view.GetComponentsInChildren<GuxuMapBeacon>())beacon.Highlight();
  foreach(var label in view.GetComponentsInChildren<GuxuRuinsTransientLabel>())label.Show();
 }
}

// The landscape stays unobstructed during play; names only accompany an explicit exploration hint.
public sealed class GuxuRuinsTransientLabel:MonoBehaviour {
 CanvasGroup group;bool unlocked;float showUntil;
 public void Bind(CanvasGroup canvas,bool open){group=canvas;unlocked=open;group.alpha=0;}
 public void Show(){if(unlocked&&group){showUntil=Time.unscaledTime+4.2f;group.alpha=1;}}
 void Update(){if(group&&group.alpha>0)group.alpha=Mathf.Clamp01((showUntil-Time.unscaledTime)/.5f);}
}
}
