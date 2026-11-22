using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Gxdl;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class GuxuUIStyleCombatCheck {
 static readonly List<string> evidence=new List<string>();
 static bool running;
 static int assertions;
 [MenuItem("GXDL/QA/UIStyle Combat HUD")]
 public static void Begin(){
  if(running||!Application.isPlaying||!GxdlGame.Instance.CanPlay)throw new Exception("Use offline preview with no active review");
  evidence.Clear();assertions=0;GuxuUI23Review.BeginStyle();running=true;GxdlGame.Instance.StartCoroutine(Guard(Check()));
 }
 static void Require(bool passed,string message){assertions++;if(!passed)throw new Exception(message);evidence.Add("PASS "+message);}
 static IEnumerator Guard(IEnumerator routine){
  var stack=new Stack<IEnumerator>();stack.Push(routine);bool passed=false;
  try{
   while(stack.Count>0){bool more;object value=null;try{more=stack.Peek().MoveNext();if(more)value=stack.Peek().Current;}catch(Exception error){evidence.Add("FAIL "+error);Debug.LogException(error);yield break;}if(!more){(stack.Pop() as IDisposable)?.Dispose();continue;}if(value is IEnumerator inner)stack.Push(inner);else yield return value;}
   passed=true;
  }finally{
   while(stack.Count>0)(stack.Pop() as IDisposable)?.Dispose();GuxuUI23Review.End();running=false;
   string report=DateTime.UtcNow.ToString("O")+" "+(passed?"PASS":"FAIL")+" "+assertions+" assertions\nSession: "+GuxuUI23Review.ActiveOutput+"\n"+string.Join("\n",evidence)+"\nRESTORED via isolated review fixture; settlement visual tests pending separate implementation";
   Directory.CreateDirectory("Reference/UIStyleRework20261111/technical");File.WriteAllText("Reference/UIStyleRework20261111/technical/combat-hud-check.txt",report);File.WriteAllText(GuxuUI23Review.ActiveOutput+"/combat-hud-check.txt",report);
  }
 }
 static IEnumerator Action(string action,string name="",string scenario="",int height=1334)=>GuxuUI23Review.Run(new GuxuUI23Review.Command{action=action,name=name,scenario=scenario,height=height});
 static Button Button(string name)=>UnityEngine.Object.FindObjectsOfType<Button>().Single(b=>b.gameObject.activeInHierarchy&&b.name==name);
 static Text Text(string name)=>UnityEngine.Object.FindObjectsOfType<Text>().Single(t=>t.gameObject.activeInHierarchy&&t.name==name);
 static RectTransform Rect(string name)=>GameObject.Find(name).GetComponent<RectTransform>();
 static bool Fits(RectTransform rect){var corners=new Vector3[4];rect.GetWorldCorners(corners);var camera=rect.GetComponentInParent<Canvas>().worldCamera;return corners.All(c=>{var p=RectTransformUtility.WorldToScreenPoint(camera,c);return p.x>=-1&&p.y>=-1&&p.x<=Screen.width+1&&p.y<=Screen.height+1;});}
 static IEnumerator Check(){
  foreach(string key in new[]{"combat-enemy-frame","combat-player-frame","combat-skill-seal","combat-bar-frame","state-selected"})Require(GuxuUiArt.Optional(key)!=null,"production HUD asset imported: "+key);
  yield return Action("size",height:1334);yield return Action("scenario",scenario:"combat");
  var game=GxdlGame.Instance;var rules=game.Rules;rules.Save.activeSkill=1;rules.Save.autoSkill=false;
  typeof(GxdlGame).GetField("fightSpeed",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(game,1);
  var stage=GuxuStageConfig.Load().stages[99];int progress=rules.Save.stage;game.ShowBattlePrep(stage);yield return Action("click","Ruins Challenge");
  var battle=game.ActiveBattle;Require(battle!=null&&battle.Stage==stage,"real selected encounter started");
  Require(Text("Battle Enemy Name").text==stage.guardianName,"enemy name reads selected config");
  var boss=GuxuBossAnimations.Find(stage.enemyKey);Require(boss!=null&&GameObject.Find("Battle Enemy Portrait").GetComponent<Image>().sprite==boss.Frame(boss.idleFrame),"boss portrait uses the actual encounter atlas rather than the generic guardian");
  Require(Fits(Rect("Battle Enemy HUD"))&&Fits(Rect("Battle Player HUD"))&&Fits(Rect("Cast Skill")),"compact HUD fits standard viewport");
  Require(Rect("Battle Enemy HUD").rect.height<=150&&Rect("Battle Player HUD").rect.height<=120,"HUD leaves room for combatants");
  Require(GameObject.Find("Encounter ground")==null&&GameObject.Find("Battle Action Panel")==null,"obsolete full-width scene/action cards removed");
  Require(Button("Cast Skill").interactable&&!rules.Save.autoSkill,"manual skill initially usable");
  yield return Action("click","Cast Skill");Require(battle.SkillQueued||battle.SkillCooldown>0,"real manual shield skill queued or cast");
  float deadline=Time.realtimeSinceStartup+10;
  while(game.ActiveBattle!=null&&Time.realtimeSinceStartup<deadline&&(!GameObject.Find("Skill Cooldown Ring").GetComponent<Image>().enabled||Rect("Player Shield Bar").rect.width<=0))yield return null;
  Require(game.ActiveBattle==battle&&Rect("Player Shield Bar").rect.width>0,"real shield event updates separate blue shield bar");
  Require(Text("Battle Skill State").text.Contains("回合")&&!Button("Cast Skill").interactable,"cooldown uses turn count and disables manual reuse");
  Require(Text("Player Combat Status").text.Contains("护盾")&&Text("Enemy Combat Status").text.Contains("护盾"),"both shield values remain readable including zero");
  yield return Action("inspect");
  float skillWidth=Rect("Cast Skill").rect.width,lowerY=-Rect("Battle Lower HUD").anchoredPosition.y;
  yield return Action("size",height:1624);
  Require(game.ActiveBattle==battle&&rules.Save.stage==progress,"resize preserves running battle and progress");
  Require(Mathf.Abs(Rect("Battle Lower HUD").rect.width-750)<1&&Mathf.Abs(-Rect("Battle Lower HUD").anchoredPosition.y-lowerY-290)<1,"long viewport adds 290 to stage while lower HUD follows bottom");
  Require(Mathf.Abs(Rect("Cast Skill").rect.width-skillWidth)<.1f&&Fits(Rect("Cast Skill"))&&Fits(Rect("Auto Skill")),"long screen preserves skill and toggle sizes");
  Require(Fits(Rect("Enemy Health Value"))&&Fits(Rect("Player Health Value")),"numeric health values stay in viewport");
  yield return Action("click","Auto Skill");Require(battle.AutoSkill&&rules.Save.autoSkill&&!Button("Cast Skill").interactable&&Button("Auto Skill").GetComponentInChildren<Text>().text.Contains("开"),"auto mode refreshes save, logic, toggle and manual availability");
  yield return Action("click","Battle Speed");Require(Button("Battle Speed").GetComponentInChildren<Text>().text.Contains("×2"),"speed control refreshes immediately");
  yield return Action("click","退出挑战");yield return new WaitForSeconds(.6f);
  Require(game.ActiveBattle==null&&rules.Save.stage==progress,"exit cancels battle without late settlement");
  Require(game.ExpansionScreen=="chapter","exit returns to real encounter chapter");
  yield return Action("size",height:1334);Require(Fits(Rect("Chapter Challenge")),"return from flexible battle preserves chapter layout");
 }
}
