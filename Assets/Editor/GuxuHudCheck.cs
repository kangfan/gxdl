using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Gxdl;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class GuxuHudCheck {
 const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
 static readonly List<string> notes=new List<string>();
 static readonly List<object> contacts=new List<object>();
 static bool running;static int assertions;
 [MenuItem("GXDL/QA/Hud Display Logic")]
 public static void Begin(){
  if(running||!Application.isPlaying||!GxdlGame.Instance.CanPlay)throw new Exception("Start offline preview with no active review.");
  GuxuUI23Review.BeginHud();notes.Clear();contacts.Clear();assertions=0;running=true;
  new GameObject("HUD Display Logic Runner").AddComponent<GuxuAccountCheckRunner>().StartCoroutine(Guard(Check()));
 }
 static void Require(bool ok,string message){assertions++;if(!ok)throw new Exception(message);notes.Add("PASS "+message);}
 static IEnumerator Action(string action,string name="",string scenario="",int height=1334)=>GuxuUI23Review.Run(new GuxuUI23Review.Command{action=action,name=name,scenario=scenario,height=height});
 static Text Text(string name)=>UnityEngine.Object.FindObjectsOfType<Text>().Single(t=>t.name==name);
 static int Hp(Text value)=>int.Parse(value.text.Split('/')[0].Trim());
 static int[] Totals(Text value)=>Regex.Matches(value.text,@"\d+").Cast<Match>().Select(m=>int.Parse(m.Value)).ToArray();
 static void Capture(string name){var image=ScreenCapture.CaptureScreenshotAsTexture();try{File.WriteAllBytes(GuxuUI23Review.ActiveOutput+"/"+name+".png",image.EncodeToPNG());}finally{UnityEngine.Object.Destroy(image);}}
 static IEnumerator Guard(IEnumerator routine){
  var stack=new Stack<IEnumerator>();stack.Push(routine);bool passed=false,restored=false;
  try{while(stack.Count>0){bool more;object value=null;try{more=stack.Peek().MoveNext();if(more)value=stack.Peek().Current;}catch(Exception e){notes.Add("FAIL "+e);Debug.LogException(e);yield break;}if(!more){(stack.Pop() as IDisposable)?.Dispose();continue;}if(value is IEnumerator child)stack.Push(child);else yield return value;}passed=true;}
  finally{
   while(stack.Count>0)(stack.Pop() as IDisposable)?.Dispose();
   File.WriteAllText(GuxuUI23Review.ActiveOutput+"/display-contacts.json",JsonConvert.SerializeObject(contacts,Formatting.Indented));
   try{GuxuUI23Review.End();restored=true;}catch(Exception e){notes.Add("RESTORE ERROR "+e);Debug.LogException(e);}
   running=false;string report=DateTime.UtcNow.ToString("O")+" "+(passed&&restored?"PASS":"FAIL")+" "+assertions+" assertions\nSession: "+GuxuUI23Review.ActiveOutput+"\nRestore verified: "+restored+"\n"+string.Join("\n",notes);
   Directory.CreateDirectory("Reference/HudRework20261111/technical");File.WriteAllText("Reference/HudRework20261111/technical/display-logic.txt",report);File.WriteAllText(GuxuUI23Review.ActiveOutput+"/display-logic.txt",report);
  }
 }
 static IEnumerator Check(){
  yield return Action("size",height:1334);yield return Action("scenario",scenario:"ordinary-unseal");var game=GxdlGame.Instance;
  // Isolate the stale-caption precondition without letting the timer start a cast before the pointer click.
  typeof(GxdlGame).GetField("auto",Private).SetValue(game,true);typeof(GxdlGame).GetField("nextAuto",Private).SetValue(game,Time.time+60);game.Home();
  Require(GameObject.Find("停止自动")!=null,"fixture shows enabled automatic state before retention settings");
  yield return Action("click","保留规则");
  Require(!(bool)typeof(GxdlGame).GetField("auto",Private).GetValue(game),"retention settings stops automatic logic");
  var autoButton=GameObject.Find("自动破封").GetComponent<Button>();Require(autoButton.GetComponentInChildren<Text>().text=="自动破封","underlying Home caption refreshes immediately while modal is open");
  yield return Action("click","Quality Choice 4");yield return Action("click","确认");
  Require(game.Rules.Save.keepQuality==4&&autoButton.GetComponentInChildren<Text>().text=="自动破封","retention choice persists with stopped Home state after close");
  Require(game.Rules.Save.peaches==120&&game.Rules.Save.cuts==0&&game.Rules.Save.pending==null,"opening and closing settings does not consume a seal or produce loot");
  foreach(int skill in new[]{0,2})yield return BattleStatistics(skill,skill==0?1:2);
 }
 static IEnumerator BattleStatistics(int skill,int speed){
  yield return Action("scenario",scenario:"finalboss");var game=GxdlGame.Instance;game.Rules.Save.activeSkill=skill;game.Rules.Save.autoSkill=skill==0;
  typeof(GxdlGame).GetField("fightSpeed",Private).SetValue(game,speed);game.StartBattle();var sim=game.ActiveBattle;
  var player=Text("Player Health Value");var enemy=Text("Enemy Health Value");var stats=Text("Battle Statistics");
  int previousPlayer=sim.Player.hp,previousEnemy=sim.Enemy.hp,expectedDone=0,expectedTaken=0,expectedHealing=0;bool aligned=true,sawForecast=false,queuedHeal=false,healed=false;var impactFrames=new HashSet<int>();string last="";
  double until=Time.realtimeSinceStartupAsDouble+45;
  while(game.ActiveBattle!=null&&Time.realtimeSinceStartupAsDouble<until){
   yield return new WaitForEndOfFrame();int p=Hp(player),e=Hp(enemy);var sums=Totals(stats);
   expectedDone+=Math.Max(0,previousEnemy-e);expectedTaken+=Math.Max(0,previousPlayer-p);expectedHealing+=Math.Max(0,p-previousPlayer);previousPlayer=p;previousEnemy=e;
   aligned&=sums.Length==3&&sums[0]==expectedDone&&sums[1]==expectedTaken&&sums[2]==expectedHealing;
   var combo=UnityEngine.Object.FindObjectOfType<GuxuCombatVfx>();int impact=combo?combo.ImpactCount:0;
   string signature=p+"/"+e+"/"+stats.text+"/"+sim.Round+"/"+sim.DamageDone+"/"+sim.DamageTaken+"/"+sim.HealingDone+"/"+impact;
   if(signature!=last){contacts.Add(new{atUtc=DateTime.UtcNow.ToString("O"),frame=Time.frameCount,skill,speed,round=sim.Round,playerHp=p,enemyHp=e,displayed=sums,expectedFromVisibleHp=new[]{expectedDone,expectedTaken,expectedHealing},precomputed=new[]{sim.DamageDone,sim.DamageTaken,sim.HealingDone},comboImpact=impact});last=signature;}
   if(!sawForecast&&(sim.DamageDone>sums[0]||sim.DamageTaken>sums[1])){sawForecast=true;Capture("stats-"+skill+"-before-contact");}
   if(skill==0&&combo&&combo.IsPlaying&&impact>0&&impactFrames.Add(impact))Capture("stats-0-combo-contact-"+impact);
   if(!healed&&sums[2]>0){healed=true;Capture("stats-"+skill+"-actual-heal");}
   // Queue the real manual healing rule once HP has actually fallen; this is a statistics test, not a pointer claim.
   if(skill==2&&!queuedHeal&&p<sim.Player.hp&&sim.CanQueueSkill)queuedHeal=sim.QueueSkill();
   yield return null;
  }
  Require(game.ActiveBattle==null&&sim.Completed,"real battle completed within bounded statistics check skill"+skill);
  Require(aligned,"each rendered HP change agrees with displayed damage/taken/healing totals skill"+skill);
  Require(sawForecast,"future precomputed totals remain hidden before their displayed contacts skill"+skill);
  var final=Totals(stats);Require(final.SequenceEqual(new[]{sim.DamageDone,sim.DamageTaken,sim.HealingDone}),"last displayed event totals equal actual settlement totals skill"+skill);
  if(skill==0)Require(impactFrames.Contains(1)&&impactFrames.Contains(2)&&impactFrames.Contains(3),"all three actual combo contacts sampled separately");
  else Require(queuedHeal&&healed&&final[2]>0,"real queued healing increments only when positive HP recovery is displayed");
  yield return new WaitForEndOfFrame();Capture("stats-"+skill+"-settlement");game.Home();yield return null;
 }
}
