using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Gxdl;
using Newtonsoft.Json;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Object=UnityEngine.Object;

[InitializeOnLoad]
public static class GuxuBossCheck {
 const string Prefix="GXDL.BossQA.",Root="Logs/boss-integration",Receipt=Root+"/check.txt";
 static readonly string[] Suffixes={"",".bak",".tmp",".invalid",".before-expansion",".before-equipment15"};
 static bool starting;static GuxuBossCheckRunner runner;
 static string[] Prefs=>new[]{GxdlGame.OpeningStoryKey,GxdlGame.FirstUnsealStoryKey};
 static GuxuBossCheck(){EditorApplication.update+=Monitor;EditorApplication.playModeStateChanged+=Mode;}
 [MenuItem("Guxu/QA/Record Boss Battles")]
 public static void Run(){
  Require(!EditorApplication.isPlayingOrWillChangePlaymode,"Stop Play Mode first");
  for(int i=0;i<UnityEngine.SceneManagement.SceneManager.sceneCount;i++)Require(!UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty,"Save scene edits before QA");
  Directory.CreateDirectory(Root);File.WriteAllText(Receipt,"RUNNING "+DateTime.Now.ToString("O"));
  GuxuAcceptance.Rules();var defs=GuxuBossAnimations.All.ToArray();Require(defs.Length==10&&defs.Sum(b=>b.attacks.Length)==40,"10 bosses / 40 clips");
  foreach(var b in defs){GuxuBossAnimations.Validate(b);for(int i=0;i<b.columns*b.rows;i++)Require(b.Frame(i)!=null,"all atlas cells load");}
  foreach(int total in new[]{0,1,2,17,int.MaxValue})foreach(int count in new[]{1,2,3,5})Require(Enumerable.Range(0,count).Sum(i=>(long)GuxuBossAnimations.DamagePart(total,i,count))==total,"damage partition exact");
  var config=GuxuStageConfig.Load();Require(config.stages.Count(s=>s.boss&&GuxuBossAnimations.Find(s.enemyKey)!=null)==20,"20 main bosses");Require(config.tower.Count(s=>s.boss&&GuxuBossAnimations.Find(s.enemyKey)!=null)==9,"nine tower projections");
  string[] selected={"ember-forge-boss","star-desert-boss","nether-court-boss","verdant-ruins-boss"};
  foreach(var s in config.dungeons)Require(s.boss&&s.enemyKey==selected[s.contentId]&&s.guardianName==GuxuBossAnimations.Find(s.enemyKey).name,"40 dungeon bindings");
  SessionState.SetString(Prefix+"scenes",JsonConvert.SerializeObject(EditorSceneManager.GetSceneManagerSetup()));SessionState.SetString(Prefix+"save",GameRules.SavePath);
  foreach(var suffix in Suffixes){string p=GameRules.SavePath+suffix;SessionState.SetBool(Prefix+"exists"+suffix,File.Exists(p));if(File.Exists(p))SessionState.SetString(Prefix+"bytes"+suffix,Convert.ToBase64String(File.ReadAllBytes(p)));}
  foreach(var key in Prefs){SessionState.SetInt(Prefix+key,PlayerPrefs.GetInt(key,-1));PlayerPrefs.SetInt(key,1);}PlayerPrefs.Save();
  SessionState.SetInt(Prefix+"capture",Time.captureFramerate);SessionState.SetBool(Prefix+"active",true);SessionState.SetFloat(Prefix+"deadline",(float)EditorApplication.timeSinceStartup+600);starting=false;
  EditorSceneManager.OpenScene("Assets/Scenes/GxdlMain.unity");GuxuAccountChecks.PreviewGameplay();
 }
 static void Monitor(){if(!SessionState.GetBool(Prefix+"active",false))return;
  if(!starting&&EditorApplication.isPlaying&&GxdlGame.Instance&&GxdlGame.Instance.Rules!=null){starting=true;runner=new GameObject("Boss QA Runner").AddComponent<GuxuBossCheckRunner>();runner.StartCoroutine(Guard(runner.Check()));}
  if(EditorApplication.timeSinceStartup>SessionState.GetFloat(Prefix+"deadline",float.MaxValue)){File.WriteAllText(Receipt,"FAIL timeout");EditorApplication.isPlaying=false;}
 }
 static IEnumerator Guard(IEnumerator test){var stack=new Stack<IEnumerator>();stack.Push(test);bool failed=false;
  while(stack.Count>0){object value=null;bool moved=false;try{moved=stack.Peek().MoveNext();if(moved)value=stack.Peek().Current;}catch(Exception e){failed=true;File.WriteAllText(Receipt,"FAIL "+e);Debug.LogException(e);}if(failed)break;if(!moved){(stack.Pop() as IDisposable)?.Dispose();continue;}if(value is IEnumerator nested){stack.Push(nested);continue;}yield return value;}
  while(stack.Count>0)(stack.Pop() as IDisposable)?.Dispose();EditorApplication.isPlaying=false;
 }
 static void Mode(PlayModeStateChange state){if(state!=PlayModeStateChange.EnteredEditMode||!SessionState.GetBool(Prefix+"active",false))return;
  string save=SessionState.GetString(Prefix+"save","");foreach(var suffix in Suffixes){string p=save+suffix;if(SessionState.GetBool(Prefix+"exists"+suffix,false))File.WriteAllBytes(p,Convert.FromBase64String(SessionState.GetString(Prefix+"bytes"+suffix,"")));else if(File.Exists(p))File.Delete(p);}
  foreach(var key in Prefs){int old=SessionState.GetInt(Prefix+key,-1);if(old<0)PlayerPrefs.DeleteKey(key);else PlayerPrefs.SetInt(key,old);}PlayerPrefs.Save();Time.captureFramerate=SessionState.GetInt(Prefix+"capture",0);
  var setup=JsonConvert.DeserializeObject<SceneSetup[]>(SessionState.GetString(Prefix+"scenes","[]"));if(setup.Length>0)EditorSceneManager.RestoreSceneManagerSetup(setup);
  File.AppendAllText(Receipt,"\nRESTORED original save bytes, sidecars, story preferences and scene setup.");SessionState.SetBool(Prefix+"active",false);starting=false;runner=null;
 }
 public static void Require(bool condition,string message){if(!condition)throw new InvalidOperationException("Boss QA: "+message);}
}

public sealed class GuxuBossCheckRunner:MonoBehaviour {
 const string Root="Logs/boss-integration";int frame;Camera renderCamera;RenderTexture render,previous;Texture2D pixels;readonly List<object> receipts=new List<object>();
 static void Require(bool condition,string message)=>GuxuBossCheck.Require(condition,message);
 static void Select(GxdlGame game,GuxuStage stage){game.StopAllCoroutines();game.Home();typeof(GxdlGame).GetField("selectedStage",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(game,stage);game.StartBattle();}
 void Capture(string folder,string still=null){Canvas.ForceUpdateCanvases();var active=RenderTexture.active;try{renderCamera.Render();RenderTexture.active=render;pixels.ReadPixels(new Rect(0,0,450,800),0,0);pixels.Apply();byte[] bytes=pixels.EncodeToPNG();if(folder!=null)File.WriteAllBytes(folder+"/"+(frame++).ToString("D4")+".png",bytes);if(still!=null)File.WriteAllBytes(Root+"/"+still+".png",bytes);}finally{RenderTexture.active=active;}}
 public IEnumerator Check(){
  var game=GxdlGame.Instance;string save=JsonConvert.SerializeObject(game.Rules.Save);renderCamera=Camera.main;previous=renderCamera.targetTexture;render=new RenderTexture(450,800,24);renderCamera.targetTexture=render;pixels=new Texture2D(450,800,TextureFormat.RGB24,false);Time.captureFramerate=15;
  try{
   game.Rules.Save.stage=201;game.Rules.Save.pending=null;game.Rules.EnsureExpansion();game.Rules.Save.dungeonClaims=new int[4];game.Rules.Save.dungeonClears=new[]{10,10,10,10};var config=GuxuStageConfig.Load();
   foreach(var definition in GuxuBossAnimations.All){
    Select(game,config.stages.First(s=>s.boss&&s.enemyKey==definition.key));game.StopAllCoroutines();yield return null;
    var boss=Object.FindObjectOfType<GuxuBossMotion>();var hero=Object.FindObjectOfType<GuxuCharacterMotion>();Require(boss&&hero&&boss.Definition.key==definition.key,"battle actor binding "+definition.key);
    var root=(RectTransform)boss.transform.parent;var target=hero.GetComponent<Image>();var fx=root.GetComponent<GuxuCombatVfx>();fx.Cancel();hero.ResetPose();var origin=boss.GetComponent<RectTransform>().anchoredPosition;
    string folder=Root+"/"+definition.key;Directory.CreateDirectory(folder);foreach(string p in Directory.GetFiles(folder,"*.png"))if(int.TryParse(Path.GetFileNameWithoutExtension(p),out _))File.Delete(p);frame=0;
    // Record the actual hero combo and boss ultimate on the game's encounter canvas.
    hero.PlayAttackCombo();var combo=fx.PlayCombo(hero,boss.GetComponent<Image>(),1,i=>boss.Hit());while(combo.MoveNext()){Capture(folder);yield return combo.Current;}fx.Cancel();hero.ResetPose();
    for(int index=0;index<definition.attacks.Length;index++)for(int speed=1;speed<=3;speed++){
     var clip=definition.attacks[index];bool recording=speed==1&&index==definition.attacks.Length-1,visible=false;int changes=boss.SpriteChanges;var hits=new List<int>();var clocks=new List<float>();float start=Time.time;int damage=0;
     var attack=boss.PlayAttack(index,target,root,speed,i=>{hits.Add(i);clocks.Add((Time.time-start)*speed);damage+=GuxuBossAnimations.DamagePart(17,i,clip.impacts.Length);hero.PlayHit(speed);});
     bool captured=false;while(attack.MoveNext()){
      Canvas.ForceUpdateCanvases();foreach(var effect in Object.FindObjectsOfType<GuxuBossVfx>()){var mesh=effect.canvasRenderer.GetMesh();visible|=mesh.vertexCount>0;Require(mesh.vertices.All(v=>!float.IsNaN(v.x)&&!float.IsNaN(v.y)&&!float.IsInfinity(v.x)&&!float.IsInfinity(v.y)),"finite effect vertices");}
      if(recording)Capture(folder);if(speed==1&&!captured&&hits.Count>0){Capture(null,definition.key+"-skill-"+index);captured=true;}yield return attack.Current;
     }
     yield return null;Require(hits.SequenceEqual(Enumerable.Range(0,clip.impacts.Length)),"one ordered callback per impact "+definition.key+"/"+clip.name+" x"+speed);
     for(int i=0;i<clocks.Count;i++)Require(Mathf.Abs(clocks[i]-clip.impacts[i])<=speed/15f+.025f,"impact timing "+clip.name);
     Require(damage==17&&boss.SpriteChanges>changes&&visible,"damage, animation and VFX "+clip.name);Require(!boss.IsAttacking&&boss.FrameIndex==definition.idleFrame,"returned to idle");Require(!Object.FindObjectsOfType<GuxuBossVfx>().Any(),"VFX cleaned");Require(Vector2.Distance(boss.GetComponent<RectTransform>().anchoredPosition,origin)<.01f,"no travel drift");
     receipts.Add(new{boss=definition.name,attack=clip.name,speed,contacts=hits.Count,damage,spriteChanges=boss.SpriteChanges-changes,visible});
    }
    File.WriteAllText(Root+"/progress.txt",definition.name+" PASS; "+receipts.Count+"/120 checks");
    int callbacks=0;var interrupted=boss.PlayAttack(0,target,root,1,i=>callbacks++);Require(interrupted.MoveNext(),"starts cancel probe");boss.Cancel();Require(!interrupted.MoveNext()&&callbacks==0,"cancel suppresses later damage");
    var disabled=boss.PlayAttack(0,target,root,1,i=>callbacks++);Require(disabled.MoveNext(),"starts disable probe");boss.gameObject.SetActive(false);Require(!disabled.MoveNext()&&callbacks==0,"disable suppresses damage");game.Home();yield return null;Require(!Object.FindObjectsOfType<GuxuBossVfx>().Any(),"screen switch cleanup");
   }
   foreach(var stage in config.dungeons.Where(s=>s.difficulty==1)){Select(game,stage);game.StopAllCoroutines();yield return null;Require(Object.FindObjectOfType<GuxuBossMotion>().Definition.key==stage.enemyKey,"dungeon actor binding");Capture(null,"dungeon-"+stage.contentId);game.Home();yield return null;}
   // Exercise the unmodified event producer through the real battle coroutine.
   foreach(var gear in game.Rules.Save.gear){gear.hp=100000;gear.attack=1;gear.defense=0;}
   foreach(var definition in GuxuBossAnimations.All){
    var live=JsonConvert.DeserializeObject<GuxuStage>(JsonConvert.SerializeObject(config.stages.First(s=>s.boss&&s.enemyKey==definition.key)));
    // Recording fixtures survive the first exchange; source encounter stats are never modified.
    live.enemyStats=new Stats{hp=10000,attack=1000,defense=0,speed=1};live.enemyTraits=new GuxuCombatTraits();Select(game,live);yield return null;
    string folder=Root+"/live-"+definition.key;Directory.CreateDirectory(folder);foreach(string p in Directory.GetFiles(folder,"*.png"))if(int.TryParse(Path.GetFileNameWithoutExtension(p),out _))File.Delete(p);frame=0;
    bool attacked=false,contact=false;float deadline=Time.time+22,contactAt=float.MaxValue;while(Time.time<deadline){var motion=Object.FindObjectOfType<GuxuBossMotion>();if(motion){attacked|=motion.IsAttacking;if(attacked&&motion.ImpactCount>0&&!contact){contact=true;contactAt=Time.time;Capture(null,"live-"+definition.key);}}Capture(folder);if(contact&&Time.time>contactAt+.6f)break;yield return null;}
    Require(attacked&&contact,"real event loop invoked boss clip and contact: "+definition.key);game.Home();yield return null;Require(game.Rules.Save.stage==201&&!Object.FindObjectsOfType<GuxuBossVfx>().Any(),"cancelled battle neither settles nor leaks effects");
   }
   File.WriteAllText(Root+"/runtime-checks.json",JsonConvert.SerializeObject(receipts,Formatting.Indented));File.WriteAllText(Root+"/check.txt","PASS "+DateTime.Now.ToString("O")+"\n10 atlases / 210 poses / 40 skills; 120 checks at 1x, 2x, 3x; impact timing; exact damage split; actual rendered VFX; finite meshes; idle recovery; cancel and disable; four dungeon battle bindings; all ten real battle event dispatches; no cancelled settlement.\n20 main bosses + 9 tower projections + 40 dungeon tiers; seeded rules regression passed.");
  }finally{game.StopAllCoroutines();game.Rules.Save=JsonConvert.DeserializeObject<SaveData>(save);renderCamera.targetTexture=previous;render.Release();Object.Destroy(render);Object.Destroy(pixels);}
 }
}
