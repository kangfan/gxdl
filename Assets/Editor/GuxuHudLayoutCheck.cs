using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Gxdl;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public static class GuxuHudLayoutCheck {
 const BindingFlags Private=BindingFlags.NonPublic|BindingFlags.Instance;
 static readonly List<string> notes=new List<string>();static readonly List<object> geometry=new List<object>();static bool running;static int assertions;
 static GxdlGame Game=>GxdlGame.Instance;
 static Text Label(string name)=>UnityEngine.Object.FindObjectsOfType<Text>().Single(t=>t.name==name);
 static Button Button(string name)=>UnityEngine.Object.FindObjectsOfType<Button>().Single(b=>b.name==name);
 static IEnumerator Action(string action,string name="",string scenario="",int height=1334)=>GuxuUI23Review.Run(new GuxuUI23Review.Command{action=action,name=name,scenario=scenario,height=height});
 static void Require(bool ok,string message){assertions++;if(!ok)throw new Exception(message);notes.Add("PASS "+message);}
 [MenuItem("GXDL/QA/Hud Layout")]
 public static void Begin(){if(running||!Application.isPlaying||!Game.CanPlay)throw new Exception("Start offline preview, no active fixture.");GuxuUI23Review.BeginHud();running=true;assertions=0;notes.Clear();geometry.Clear();new GameObject("HUD Layout Runner").AddComponent<GuxuAccountCheckRunner>().StartCoroutine(Guard(Check()));}
 static IEnumerator Guard(IEnumerator routine){
  bool passed=false,restored=false;var stack=new Stack<IEnumerator>();stack.Push(routine);
  try{while(stack.Count>0){bool more;object current=null;try{more=stack.Peek().MoveNext();if(more)current=stack.Peek().Current;}catch(Exception e){notes.Add("FAIL "+e);Debug.LogException(e);yield break;}if(!more){(stack.Pop() as IDisposable)?.Dispose();continue;}if(current is IEnumerator child)stack.Push(child);else yield return current;}passed=true;}
  finally{while(stack.Count>0)(stack.Pop() as IDisposable)?.Dispose();File.WriteAllText(GuxuUI23Review.ActiveOutput+"/hud-geometry.json",JsonConvert.SerializeObject(geometry,Formatting.Indented));try{GuxuUI23Review.End();restored=true;}catch(Exception e){notes.Add("RESTORE ERROR "+e);Debug.LogException(e);}running=false;string report=DateTime.UtcNow.ToString("O")+" "+(passed&&restored?"PASS":"FAIL")+" "+assertions+" assertions\nSession: "+GuxuUI23Review.ActiveOutput+"\nRestore verified: "+restored+"\n"+string.Join("\n",notes);File.WriteAllText(GuxuUI23Review.ActiveOutput+"/layout.txt",report);File.WriteAllText("Reference/HudRework20261111/technical/layout.txt",report);}
 }
 static Vector2 ScreenPoint(RectTransform rect,Vector2 topLeft)=>RectTransformUtility.WorldToScreenPoint(rect.GetComponentInParent<Canvas>().worldCamera,rect.TransformPoint(new Vector2(rect.rect.xMin+topLeft.x,rect.rect.yMax-topLeft.y)));
 static GameObject Hit(RectTransform rect,Vector2 topLeft){var data=new PointerEventData(EventSystem.current){position=ScreenPoint(rect,topLeft)};var hits=new List<RaycastResult>();EventSystem.current.RaycastAll(data,hits);return hits.Count==0?null:ExecuteEvents.GetEventHandler<IPointerClickHandler>(hits[0].gameObject);}
 static IEnumerator Pointer(Button button,Vector2 topLeft){var rect=(RectTransform)button.transform;Require(button.interactable&&Hit(rect,topLeft)==button.gameObject,"pointer hits "+button.name+" at "+topLeft);var pointer=new PointerEventData(EventSystem.current){position=ScreenPoint(rect,topLeft),button=PointerEventData.InputButton.Left};ExecuteEvents.Execute(button.gameObject,pointer,ExecuteEvents.pointerDownHandler);yield return null;ExecuteEvents.Execute(button.gameObject,pointer,ExecuteEvents.pointerUpHandler);ExecuteEvents.Execute(button.gameObject,pointer,ExecuteEvents.pointerClickHandler);}
 static void FitText(Text text){
  Canvas.ForceUpdateCanvases();var settings=text.GetGenerationSettings(text.rectTransform.rect.size);var generator=new TextGenerator();generator.Populate(text.text,settings);var vertices=generator.verts;float min=vertices.Count>4?vertices.Take(vertices.Count-4).Min(v=>v.position.x):0,max=vertices.Count>4?vertices.Take(vertices.Count-4).Max(v=>v.position.x):0;
  geometry.Add(new{name=text.name,text=text.text,font=text.fontSize,bestFit=generator.fontSizeUsedForBestFit,width=text.rectTransform.rect.width,pixelBounds=new[]{min,max},rect=text.rectTransform.anchoredPosition.ToString()});
  Require(generator.characterCountVisible>=text.text.Length&&max-min<=text.rectTransform.rect.width*text.pixelsPerUnit+2,"all digits render within dedicated "+text.name+" rect");
 }
 static IEnumerator Check(){
  string[] keys={"hud-unseal-button","hud-health-frame","hud-skill-seal","hud-toggle-off","hud-toggle-on","hud-flourish"};int[] widths={256,256,128,128,128,128},heights={64,32,128,48,48,32};
  for(int i=0;i<keys.Length;i++){string path="Assets/Resources/Game/Guxu/Expansion/UI/"+keys[i]+".png";var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(path);var importer=(TextureImporter)AssetImporter.GetAtPath(path);Require(texture&&texture.width==widths[i]&&texture.height==heights[i]&&importer.maxTextureSize==256&&!importer.mipmapEnabled&&importer.wrapMode==TextureWrapMode.Clamp,"actual optimized texture/importer "+keys[i]);geometry.Add(new{asset=path,guid=AssetDatabase.AssetPathToGUID(path),width=texture.width,height=texture.height,max=importer.maxTextureSize,border=GuxuUiArt.Optional(keys[i],i<2).border.ToString()});}
  foreach(int height in new[]{1334,1624}){
   yield return Action("size",height:height);yield return Action("scenario",scenario:"ordinary-unseal");
   Require(Label("Seal Token Balance").text==Game.Rules.Save.peaches.ToString()&&Label("Spirit Stone Balance").text==Game.Rules.Save.coins.ToString(),"Home balances use actual save at "+height);
   Require(Label("Main Quest Title").text=="主线 · 已通关"&&Label("Main Quest Target").text=="重访遗域 ›","completed quest has truthful revisit target at "+height);
   Require(GameObject.Find("Unseal Controls")==null&&GameObject.Find("Main Quest")==null,"removed thick action and quest panels at "+height);
   foreach(string name in new[]{"自动破封","保留规则"}){var rect=(RectTransform)Button(name).transform;Require(rect.rect.height>=88&&rect.rect.width>=132,"secondary touch target >=44 display pixels: "+name+" at "+height);geometry.Add(new{name,height,logical=new[]{rect.rect.width,rect.rect.height},at375=new[]{rect.rect.width*.5f,rect.rect.height*.5f}});}
   foreach(var point in new[]{new Vector2(610,20),new Vector2(80,85),new Vector2(140,65)}){var quest=Button("GoQuestTarget");yield return Pointer(quest,point);yield return null;Require(Game.ExpansionScreen=="world","quest blank/underline/arrow uses real world-map action at "+height);yield return Action("click","World Back");}
   Game.Rules.Save.coins=int.MaxValue;Game.Rules.Save.peaches=int.MaxValue;Game.Home();yield return Action("inspect");FitText(Label("Seal Token Balance"));FitText(Label("Spirit Stone Balance"));
   yield return Action("scenario",scenario:"hud-no-seals");Require(!Button("Unseal").interactable&&!Button("自动破封").interactable&&Label("Seal Token Cost").text.Contains("不足"),"zero seals disable both spend paths with truthful cost at "+height);yield return Action("reject-click","Unseal");Require(Game.Rules.Save.peaches==0&&Game.Rules.Save.pending==null&&Game.Rules.Save.cuts==0,"disabled zero-seal attempt preserves save at "+height);
  }
  yield return Action("size",height:1334);yield return Action("scenario",scenario:"new");
  Require(Label("Main Quest Title").text.Contains(Game.CurrentStage.regionName)&&Label("Main Quest Target").text=="前往 "+Game.CurrentStage.regionName+" ›"&&Label("Main Quest Progress").text.Contains(Game.CurrentStage.guardianName),"unfinished quest binds real region guardian and stage");
  yield return Action("scenario",scenario:"ordinary-unseal");typeof(GxdlGame).GetField("auto",Private).SetValue(Game,true);typeof(GxdlGame).GetField("nextAuto",Private).SetValue(Game,Time.time+60);Game.Home();yield return Action("click","保留规则");Require(!(bool)typeof(GxdlGame).GetField("auto",Private).GetValue(Game)&&Button("自动破封").transform.Find("Toggle Track").GetComponent<Image>().sprite.name.EndsWith("hud-toggle-off"),"retention settings refreshes underlying real switch immediately");yield return Action("click","Quality Choice 4");yield return Action("click","确认");Require(Game.Rules.Save.keepQuality==4,"retention real choice persists");
  var oldQuest=Button("GoQuestTarget");int seals=Game.Rules.Save.peaches;yield return Pointer(Button("Unseal"),new Vector2(150,44));Require(Game.Rules.Save.pending!=null&&!oldQuest.gameObject.activeInHierarchy,"task disappears including raycast in same pending/cast transition");
  float until=Time.realtimeSinceStartup+15;while((bool)typeof(GxdlGame).GetField("busy",Private).GetValue(Game)&&Time.realtimeSinceStartup<until)yield return null;
  yield return Action("inspect");Require(Game.Rules.Save.peaches==seals-1&&Game.Rules.Save.pending!=null&&GameObject.Find("GoQuestTarget")==null,"pending decision replaces quest after exactly one real seal spend");
  foreach(string name in new[]{"Equip Relic","Resolve Equipment"}){var button=Button(name);Require(Hit((RectTransform)button.transform,((RectTransform)button.transform).rect.size*.5f)==button.gameObject,"pending action not blocked by old task: "+name);}
  yield return Action("click","Resolve Equipment");yield return new WaitForSeconds(3);yield return Action("inspect");Require(Game.Rules.Save.pending==null&&GameObject.Find("GoQuestTarget")!=null&&Label("Spirit Stone Balance").text==Game.Rules.Save.coins.ToString(),"resolve refreshes real currency and restores task after feedback");
  foreach(int height in new[]{1334,1624})yield return Combat(height);
 }
 static IEnumerator Combat(int height){
  yield return Action("size",height:height);yield return Action("scenario",scenario:"finalboss");Game.Rules.Save.activeSkill=1;Game.Rules.Save.autoSkill=false;typeof(GxdlGame).GetField("fightSpeed",Private).SetValue(Game,1);Game.StartBattle();var sim=Game.ActiveBattle;yield return new WaitForEndOfFrame();
  var lower=GameObject.Find("Battle Lower HUD").GetComponent<RectTransform>();Require(lower.rect.height==280&&Mathf.Abs(-lower.anchoredPosition.y+280-height)<2,"compact battle controls anchored to lower edge at "+height);
  FitText(Label("Player Health Value"));Require(Label("Player Health Value").rectTransform.anchoredPosition.x>=558+8,"HP digits leave clear space after gold end cap at "+height);
  Require(GameObject.Find("Player Shield Bar").GetComponent<RectTransform>().rect.width==0,"zero player shield has no false filled bar at "+height);
  foreach(string name in new[]{"退出挑战","Battle Speed","Cast Skill","Auto Skill"}){var button=Button(name);Require(Hit((RectTransform)button.transform,((RectTransform)button.transform).rect.size*.5f)==button.gameObject,"distinct battle pointer target "+name+" at "+height);}
  yield return Action("inspect");yield return Action("click","Battle Speed");Require(Button("Battle Speed").GetComponentInChildren<Text>().text.Contains("×"+(int)typeof(GxdlGame).GetField("fightSpeed",Private).GetValue(Game)),"speed caption refreshes at "+height);
  yield return Action("click","Cast Skill");Require(sim.SkillQueued||sim.SkillCooldown>0,"manual skill really queues or releases at "+height);
  yield return Action("click","Auto Skill");Require(sim.AutoSkill&&Game.Rules.Save.autoSkill&&Button("Auto Skill").transform.Find("Toggle Track").GetComponent<Image>().sprite.name.EndsWith("hud-toggle-on"),"automatic skill saves and shows real enabled state at "+height);
  float until=Time.realtimeSinceStartup+8;var shield=GameObject.Find("Player Shield Bar").GetComponent<RectTransform>();while(Game.ActiveBattle!=null&&shield.rect.width<=0&&Time.realtimeSinceStartup<until)yield return null;
  Require(shield.rect.width>0,"real queued shield becomes visible at "+height);int shownShield=int.Parse(Label("Player Combat Status").text.Split(' ')[1]);Require(Mathf.Abs(shield.rect.width-270f*shownShield/sim.Player.hp)<1,"displayed shield number and bar share same event at "+height);
  Require(!Button("Cast Skill").interactable&&Label("Battle Skill State").text.Contains("冷却"),"released skill shows actual cooldown and disallows second manual queue at "+height);
  yield return Action("inspect");yield return Action("click","退出挑战");Require(Game.ActiveBattle==null&&Game.ExpansionScreen=="chapter","exit returns actual chapter and ends battle at "+height);
  yield return Action("click","Chapter Back");yield return Action("click","World Back");Require(Game.ExpansionScreen=="home"&&Label("Seal Token Balance").text==Game.Rules.Save.peaches.ToString(),"Home reentry reflects same save at "+height);
 }
}
