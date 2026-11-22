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
using UnityEngine.UI;
public static class GuxuMotionVfxCheck {
 static readonly List<string> notes=new List<string>();static int assertions,peak;static bool running,cinematic;static string reportName="motion-technical",reportRoot="Reference/MotionVfx20261111/technical/";
 const BindingFlags Private=BindingFlags.NonPublic|BindingFlags.Instance;
 [MenuItem("GXDL/QA/Motion Technical")]
 public static void Begin()=>Start(Check(),"motion-technical");
 [MenuItem("GXDL/QA/Motion Contact HUD")]
 public static void BeginContact()=>Start(ContactCheck(),"motion-contact-hud");
 [MenuItem("GXDL/QA/Cinematic Technical")]
 public static void BeginCinematic()=>Start(Check(),"cinematic-technical",true);
 [MenuItem("GXDL/QA/Cinematic Contact HUD")]
 public static void BeginCinematicContact()=>Start(ContactCheck(),"cinematic-contact-hud",true);
 static void Start(IEnumerator check,string report,bool useCinematic=false){if(running||!Application.isPlaying||!GxdlGame.Instance.CanPlay)throw new Exception("Start offline preview first");if(useCinematic)GuxuUI23Review.BeginCinematic();else GuxuUI23Review.BeginMotion();cinematic=useCinematic;reportRoot=useCinematic?"Reference/MotionVfxCinematic20261111/technical/":"Reference/MotionVfx20261111/technical/";notes.Clear();assertions=peak=0;reportName=report;running=true;new GameObject("Motion Technical Runner").AddComponent<GuxuAccountCheckRunner>().StartCoroutine(Guard(check));}
 static void Require(bool ok,string message){assertions++;if(!ok)throw new Exception(message);notes.Add("PASS "+message);}
 static IEnumerator Guard(IEnumerator routine){float capture=Time.captureDeltaTime;var stack=new Stack<IEnumerator>();stack.Push(routine);bool pass=false,restored=false;try{while(stack.Count>0){bool more;object value=null;try{more=stack.Peek().MoveNext();if(more)value=stack.Peek().Current;}catch(Exception e){notes.Add("FAIL "+e);Debug.LogException(e);yield break;}if(!more){(stack.Pop() as IDisposable)?.Dispose();continue;}if(value is IEnumerator child)stack.Push(child);else yield return value;}pass=true;}finally{Time.captureDeltaTime=capture;while(stack.Count>0)(stack.Pop() as IDisposable)?.Dispose();try{GuxuUI23Review.End();restored=true;}catch(Exception e){notes.Add("RESTORE ERROR "+e);Debug.LogException(e);}running=false;string report=DateTime.UtcNow.ToString("O")+" "+(pass&&restored?"PASS":"FAIL")+" "+assertions+" assertions\nSession: "+GuxuUI23Review.ActiveOutput+"\nPeak actual particles sampled: "+peak+"\nRestore verified: "+restored+"\n"+string.Join("\n",notes);File.WriteAllText(GuxuUI23Review.ActiveOutput+"/"+reportName+".txt",report);File.WriteAllText(reportRoot+reportName+".txt",report);}}
 static IEnumerator Action(string action,string name="",string scenario="",int height=1334)=>GuxuUI23Review.Run(new GuxuUI23Review.Command{action=action,name=name,scenario=scenario,height=height});
 static IEnumerator Pointer(string name)=>(IEnumerator)typeof(GuxuIterationCheck).GetMethod("Click",BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,new object[]{name,0});
 static T Find<T>() where T:UnityEngine.Object=>UnityEngine.Object.FindObjectOfType<T>();
 static GuxuParticleFx[] AllParticles()=>UnityEngine.Object.FindObjectsOfType<GuxuParticleFx>(true);
 static void SamplePeak(){peak=Math.Max(peak,AllParticles().Where(p=>p.System).Sum(p=>p.System.particleCount));}
 static IEnumerator ContactCheck(){
  yield return Action("scenario",scenario:"finalboss");yield return Action("size",height:1334);var game=GxdlGame.Instance;game.StartBattle();yield return null;
  var state=Find<GuxuBattleParticles>();int initialShield=state.DisplayedEnemyShield;bool consistent=true,changed=false,sawContact=false;double end=Time.realtimeSinceStartupAsDouble+4;
  var left=GameObject.Find("Player Combat Status").GetComponent<Text>();var right=GameObject.Find("Enemy Combat Status").GetComponent<Text>();var bar=GameObject.Find("Enemy Shield Bar").GetComponent<RectTransform>();
  while(Time.realtimeSinceStartupAsDouble<end){yield return new WaitForEndOfFrame();consistent&=left.text.StartsWith("护盾 "+state.DisplayedPlayerShield)&&right.text.StartsWith("护盾 "+state.DisplayedEnemyShield);consistent&=Mathf.Abs(bar.sizeDelta.x-550f*state.DisplayedEnemyShield/game.ActiveBattle.Enemy.hp)<.01f;changed|=state.DisplayedEnemyShield!=initialShield;var combo=Find<GuxuCombatVfx>();sawContact|=combo&&combo.ImpactCount>0;SamplePeak();yield return null;}
  Require(initialShield>0&&changed,"real finalboss battle consumed shield at a visible event contact");Require(sawContact,"actual hero combo contacts occurred");Require(consistent,"every sampled contact keeps shield bar/native envelope count/status text on same displayed event snapshot");yield return Action("inspect");
  game.Home();yield return null;Require(AllParticles().All(p=>p.Persistent)&&AllParticles().Length==2,"exiting actual combat removes transient particles and shield envelopes");
 }
 static IEnumerator Check(){
  var game=GxdlGame.Instance;
  foreach(string key in new[]{"particle-cloud","particle-wisp"}){string path="Assets/Resources/Game/Guxu/Vfx/"+key+".png";var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(path);var importer=(TextureImporter)AssetImporter.GetAtPath(path);Require(texture&&texture.width<=256&&texture.height<=128,"bounded production texture "+key);Require(!importer.mipmapEnabled&&importer.alphaIsTransparency&&importer.wrapMode==TextureWrapMode.Clamp&&importer.GetDefaultPlatformTextureSettings().maxTextureSize==256,"particle alpha/clamp/256 importer "+key);}
  GuxuCombatVfx.ValidateContract();GuxuCharacterMotion.ValidateContract();Require(true,"existing Spine continuity/cloth/feet and exact combo damage contracts");
  if(cinematic){var data=JsonUtility.FromJson<GuxuCastFrames.Data>(Resources.Load<TextAsset>(GuxuCastFrames.ResourcePath).text);Require(data.frames.Length>=16&&data.releaseFrame>=0&&data.releaseFrame<data.frames.Length,"approved original-character pose archive intact");foreach(var f in data.frames){var texture=Resources.Load<Texture2D>(f.key);var importer=(TextureImporter)AssetImporter.GetAtPath("Assets/Resources/"+f.key+".png");Require(texture&&texture.width==512&&texture.height==768&&importer.GetDefaultPlatformTextureSettings().maxTextureSize==1024,"native512x768 action frame preserved outside particle256 rule: "+f.key);Require(new[]{f.palm,f.leftFoot,f.rightFoot}.All(a=>a.Length==2&&a.All(v=>v>=0&&v<=1)),"actual normalized frame anchors "+f.key);}}
  var anchors=JsonConvert.DeserializeObject<Dictionary<string,float[]>>(Resources.Load<TextAsset>("Game/Guxu/Vfx/FootAnchors").text);Require(anchors.Count==215&&anchors.All(p=>p.Value.Length==2&&p.Value.All(v=>v>=0&&v<=1)),"215 actual per-frame/static alpha foot anchors in bounds");
  foreach(var boss in GuxuBossAnimations.All){GuxuBossAnimations.Validate(boss);foreach(var attack in boss.attacks)Require(Enumerable.Range(0,attack.impacts.Length).Sum(i=>GuxuBossAnimations.DamagePart(997,i,attack.impacts.Length))==997,boss.key+" / "+attack.name+" exact precomputed amount partition");}
  foreach(int height in new[]{1334,1624}){
   yield return Action("scenario",scenario:"ordinary-unseal");yield return Action("size",height:height);yield return new WaitForSeconds(.15f);var clouds=UnityEngine.Object.FindObjectsOfType<GuxuFootCloud>();Require(clouds.Length==1&&Vector3.Distance(clouds[0].AnchorWorld,Find<GuxuCharacterMotion>().FeetWorld)<.002f,"Home cloud follows actual home foot bones at "+height);Require(AllParticles().Count(p=>p.Persistent)==2&&AllParticles().All(p=>p.System.GetComponent<ParticleSystemRenderer>().enabled),"two native rendered cloud layers at "+height);SamplePeak();yield return Action("inspect");
  }
  yield return Action("size",height:1334);
  if(cinematic){var source=Find<GuxuCharacterMotion>();source.PlayUnseal();yield return new WaitForSeconds(.2f);Require(source.AnimationSource=="whole-character-frames"&&!source.Graphic.gameObject.activeSelf,"cast owns visible frame renderer before interruption");source.PlayAttack();yield return null;Require(source.AnimationSource=="Spine"&&source.CurrentClip=="attack"&&source.Graphic.gameObject.activeSelf,"attack interruption replaces frames with active Spine");source.PlayUnseal();yield return new WaitForSeconds(.2f);source.PlayLibraryAction("library_idle",1,true);yield return null;Require(source.AnimationSource=="Spine"&&source.CurrentClip=="library_idle"&&source.Graphic.gameObject.activeSelf,"library interruption replaces frames with active Spine");source.ResetPose();}
  foreach(string mode in new[]{"ordinary-unseal","first-unseal"}){
   yield return Action("scenario",scenario:mode);var r=game.Rules;int tokens=r.Save.peaches;var animation=Find<GuxuAnimation>();yield return Pointer("Unseal");string pending=JsonConvert.SerializeObject(r.Save.pending);game.Chop();Require(r.Save.peaches==tokens-1&&pending==JsonConvert.SerializeObject(r.Save.pending),mode+" duplicate press does not double charge/drop");
   bool visible=true,sawFrames=false;var indices=new HashSet<int>();double deadline=Time.realtimeSinceStartupAsDouble+5;while(animation&&animation.RuneContactAt<0&&Time.realtimeSinceStartupAsDouble<deadline){visible&=animation.StoneVisible;var source=Find<GuxuCharacterMotion>();if(source.AnimationSource=="whole-character-frames"){sawFrames=true;indices.Add(source.CurrentFrame);}SamplePeak();yield return null;}
   if(cinematic)Require(sawFrames&&indices.Count>=8,"actual visible whole-character frames and frame metadata through "+mode);
   Require(animation&&visible&&animation.ReleaseAt>=0&&animation.RuneContactAt>animation.ReleaseAt,mode+" stone stays visible through real casting and flight");Require(Mathf.Abs(animation.RuneContactAt-animation.FractureAt)<=.0001f,mode+" same-frame rune contact/fracture timestamp");Require(UnityEngine.Object.FindObjectsOfType<GuxuStoneShard>().Length>0&&!animation.StoneVisible,mode+" actual fragments replace whole stone at impact");
   game.SkipCurrentUnseal();deadline=Time.realtimeSinceStartupAsDouble+5;while(animation&&animation.IsPlaying&&Time.realtimeSinceStartupAsDouble<deadline)yield return null;Require(!animation||!animation.IsPlaying,"ordinary recovery completes before unlock");Require(r.Save.peaches==tokens-1&&r.Save.pending!=null,mode+" reveal preserves one pending real drop");if(mode=="first-unseal")Require(PlayerPrefs.GetInt(GxdlGame.FirstUnsealStoryKey)==1,"ordinary completion records one-time first weapon policy");
   yield return Action("inspect");
  }
  foreach(float cancelAt in new[]{.12f,.82f,1.20f}){
   yield return Action("scenario",scenario:"ordinary-unseal");yield return Pointer("Unseal");yield return new WaitForSeconds(cancelAt);string pending=JsonConvert.SerializeObject(game.Rules.Save.pending);int tokens=game.Rules.Save.peaches;game.Home();yield return null;yield return null;Require(AllParticles().All(p=>p.Persistent)&&UnityEngine.Object.FindObjectsOfType<GuxuStoneShard>(true).Length==0,"page cancel cleans transient native particles/shards at "+cancelAt);Require(game.Rules.Save.peaches==tokens&&JsonConvert.SerializeObject(game.Rules.Save.pending)==pending,"cancel preserves already-paid pending drop at "+cancelAt);}
  yield return Action("scenario",scenario:"finalboss");game.StartBattle();game.StopAllCoroutines();yield return null;
  var actor=Find<GuxuCharacterMotion>();var fx=Find<GuxuCombatVfx>();var bossMotion=Find<GuxuBossMotion>();var foe=bossMotion.GetComponent<Image>();
  foreach(float speed in new[]{1f,2f}){
   var hits=new List<int>();var times=new List<float>();bool cloudTracks=true;float start=Time.time;actor.PlayAttackCombo(speed);fx.StartCoroutine(fx.PlayCombo(actor,foe,speed,i=>{hits.Add(i);times.Add((Time.time-start)*speed);}));double deadline=Time.realtimeSinceStartupAsDouble+5;
   while(fx.IsPlaying&&Time.realtimeSinceStartupAsDouble<deadline){SamplePeak();var cloud=actor.GetComponent<GuxuFootCloud>();yield return new WaitForEndOfFrame();cloudTracks&=Vector3.Distance(cloud.AnchorWorld,actor.FeetWorld)<.002f;yield return null;}
   Require(cloudTracks,"cloud tracks internal dash x"+speed);Require(hits.SequenceEqual(new[]{0,1,2}),"exact ordered combo contacts x"+speed);Require(times.Zip(new[]{.6f,1.6f,2.67f},(a,b)=>Mathf.Abs(a-b)<.13f).All(ok=>ok),"Spine/particle/contact clock x"+speed);Require(!fx.IsPlaying,"combo gameplay clock finishes before visual wake x"+speed);yield return new WaitForSeconds(1.3f/speed);Require(fx.ActiveVisualCount==0,"native impact wake naturally expires x"+speed);
  }
  var slowHits=new List<int>();var slowFrames=new List<int>();float previousDelta=Time.captureDeltaTime,previousMaximum=Time.maximumDeltaTime;try{Time.maximumDeltaTime=2.5f;Time.captureDeltaTime=2.4f;actor.PlayAttackCombo();fx.StartCoroutine(fx.PlayCombo(actor,foe,1,i=>{slowHits.Add(i);slowFrames.Add(Time.frameCount);}));for(int i=0;i<5&&fx.IsPlaying;i++)yield return null;}finally{Time.captureDeltaTime=previousDelta;Time.maximumDeltaTime=previousMaximum;}
  Require(slowHits.SequenceEqual(new[]{0,1,2})&&slowFrames[0]==slowFrames[1],"fixed-step synthetic slow frame crosses two markers once, then flushes third once (not realtime performance)");
  int cancelled=0;actor.PlayAttackCombo();fx.StartCoroutine(fx.PlayCombo(actor,foe,1,i=>cancelled++));yield return new WaitForSeconds(.8f);fx.Cancel();actor.ResetPose();yield return new WaitForSeconds(.5f);Require(cancelled==1&&!fx.IsPlaying&&fx.ActiveVisualCount==0,"combo cancellation prevents delayed contacts and clears owned particles");
  foreach(string scenario in new[]{"finalboss","motion-poison"}){
   yield return Action("scenario",scenario:scenario);game.StartBattle();game.StopAllCoroutines();yield return null;bossMotion=Find<GuxuBossMotion>();actor=Find<GuxuCharacterMotion>();var target=actor.GetComponent<Image>();var contacts=new List<int>();var definition=bossMotion.Definition;int selected=definition.attacks.Length-1;yield return bossMotion.PlayAttack(selected,target,(RectTransform)bossMotion.transform.parent.parent,2,i=>contacts.Add(i));Require(contacts.SequenceEqual(Enumerable.Range(0,definition.attacks[selected].impacts.Length)),scenario+" native heavy attack exact ordered markers");Require(!bossMotion.IsAttacking,"Boss returns idle "+scenario);SamplePeak();yield return Action("inspect");
  }
  var state=Find<GuxuBattleParticles>();state.SetShields(500,600);yield return new WaitForSeconds(.25f);Require(AllParticles().Any(p=>p.name.Contains("Shield")&&p.System.particleCount>0),"actual native shield particles emit while displayed shield is positive");state.SetShields(0,0);yield return null;Require(!AllParticles().Any(p=>p.name.Contains("Shield")),"displayed zero shield removes both native envelopes");state.SetShields(500,600);yield return new WaitForSeconds(.15f);
  typeof(GxdlGame).GetMethod("ShowBattleResult",Private).Invoke(game,new object[]{game.ActiveBattle,new GuxuReward(),false});yield return null;yield return new WaitForSeconds(.25f);Require(!state.enabled&&UnityEngine.Object.FindObjectsOfType<ParticleSystem>(true).All(p=>p.particleCount==0&&!p.isPlaying),"shielded result stops every active/inactive native emitter and particle");yield return Action("inspect");
  game.Home();yield return null;yield return new WaitForSeconds(.3f);Require(AllParticles().Length==2&&AllParticles().All(p=>p.Persistent),"reentry returns to two cloud systems without accumulated transient systems");SamplePeak();
 }
}

