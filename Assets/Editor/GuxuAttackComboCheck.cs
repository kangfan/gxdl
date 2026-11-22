using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Gxdl;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

[InitializeOnLoad]
public static class GuxuAttackComboCheck {
 const string Flag="GXDL.AttackComboQA",Receipt="Logs/attack-combo-check.txt";
 static bool started;static double deadline;
 static GuxuAttackComboCheck(){EditorApplication.update+=Monitor;}
 static string[] Keys=>new[]{GxdlGame.OpeningStoryKey,GxdlGame.FirstUnsealStoryKey};
 [MenuItem("Guxu/QA/Record Attack Combo")]
 public static void Run(){
  if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Stop Play Mode first.");
  for(int i=0;i<UnityEngine.SceneManagement.SceneManager.sceneCount;i++)if(UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty)throw new InvalidOperationException("Save scene changes first.");
  Directory.CreateDirectory("Logs/attack-combo/frames");File.WriteAllText(Receipt,"RUNNING "+DateTime.Now.ToString("O"));
  GuxuCombatVfx.ValidateContract();GuxuAcceptance.Rules();
  var clip=GuxuSpineAssets.Data.GetSkeletonData(false).FindAnimation("attack_combo_01");Require(clip!=null&&Mathf.Abs(clip.Duration-3)<.001f,"3-second Spine clip");
  foreach(var key in Keys){SessionState.SetInt(key,PlayerPrefs.GetInt(key,-1));PlayerPrefs.SetInt(key,1);}PlayerPrefs.Save();
  started=false;SessionState.SetBool(Flag,true);EditorSceneManager.OpenScene("Assets/Scenes/GxdlMain.unity");GuxuAccountChecks.PreviewGameplay();
 }
 static void Monitor(){if(!SessionState.GetBool(Flag,false))return;if(!started&&EditorApplication.isPlaying&&GxdlGame.Instance){started=true;deadline=EditorApplication.timeSinceStartup+180;new GameObject("Attack Combo QA").AddComponent<GuxuAttackComboRunner>().StartCoroutine(Guard(Check()));}if(started&&EditorApplication.timeSinceStartup>deadline){File.AppendAllText(Receipt,"\nFAIL timeout");Finish();}}
 static void Finish(){foreach(var key in Keys){int old=SessionState.GetInt(key,-1);if(old<0)PlayerPrefs.DeleteKey(key);else PlayerPrefs.SetInt(key,old);}PlayerPrefs.Save();SessionState.SetBool(Flag,false);started=false;EditorApplication.isPlaying=false;}
 static IEnumerator Guard(IEnumerator test){try{while(true){object next;try{if(!test.MoveNext())break;next=test.Current;}catch(Exception e){File.WriteAllText(Receipt,"FAIL "+e);Debug.LogException(e);break;}yield return next;}}finally{(test as IDisposable)?.Dispose();Finish();}}
 static void Require(bool ok,string why){if(!ok)throw new Exception("Attack combo: "+why);}
 static IEnumerator Check(){
  var game=GxdlGame.Instance;string save=Newtonsoft.Json.JsonConvert.SerializeObject(game.Rules.Save);int captureRate=Time.captureFramerate;
  var camera=Camera.main;var previous=camera.targetTexture;var rt=new RenderTexture(750,1334,24);camera.targetTexture=rt;
  try{
   game.Rules.Save.stage=1;game.Rules.Save.pending=null;game.Home();game.StartBattle();game.StopAllCoroutines();yield return null;
   var actor=UnityEngine.Object.FindObjectOfType<GuxuCharacterMotion>();var fx=UnityEngine.Object.FindObjectOfType<GuxuCombatVfx>();fx.Cancel();actor.ResetPose();
   var target=UnityEngine.Object.FindObjectsOfType<Image>().Single(i=>i.name=="Game/Guxu/Final/guardian");
   var actorRect=(RectTransform)actor.transform;var origin=actorRect.anchoredPosition;int meshes=actor.MeshBuildCount;
   Time.captureFramerate=30;
   for(int speed=1;speed<=3;speed++){
    var hits=new List<int>();var hitTimes=new List<float>();float start=Time.time;actor.PlayAttackCombo(speed);
    fx.StartCoroutine(fx.PlayCombo(actor,target,speed,i=>{hits.Add(i);hitTimes.Add((Time.time-start)*speed);}));
    bool sawSword=false,sawArc=false,sawRing=false,sawProjectile=false;int count=speed==1?102:Mathf.CeilToInt(3.4f/speed*30)+3;
    for(int frame=0;frame<count;frame++){
     yield return null;Canvas.ForceUpdateCanvases();
     sawSword|=actor.Graphic.Skeleton.FindSlot("attack_weapon").Attachment!=null;
     foreach(var stroke in UnityEngine.Object.FindObjectsOfType<GuxuCombatStroke>()){
      var mesh=stroke.canvasRenderer.GetMesh();if(stroke.color.a>.1f&&mesh.vertexCount>0){Require(mesh.vertices.All(v=>!float.IsNaN(v.x)&&!float.IsNaN(v.y)&&!float.IsInfinity(v.x)&&!float.IsInfinity(v.y)),"finite VFX mesh");sawArc|=stroke.name.Contains("1 Arc");sawRing|=stroke.name.Contains("2 Horizontal");sawProjectile|=stroke.Projectile;}
     }
     if(speed==1){Capture("frames/"+frame.ToString("D4"));if(frame==17||frame==45||frame==76||frame==82)Capture("key-"+frame);}
    }
    Require(hits.SequenceEqual(new[]{0,1,2}),"exactly three ordered contacts x"+speed);
    float[] expected={.6f,1.6f,2.67f};for(int i=0;i<3;i++)Require(Mathf.Abs(hitTimes[i]-expected[i])<.18f,"animation/VFX clock x"+speed+" contact "+i);
    Require(sawSword&&sawArc&&sawRing&&sawProjectile,"actual rendered weapon and all three effects x"+speed);
    Require(!fx.IsPlaying&&fx.ActiveVisualCount==0,"effects cleaned x"+speed);
    Require(actor.State==GuxuCharacterMotion.ActionState.Idle&&Mathf.Abs(actor.Graphic.rectTransform.anchoredPosition.x)<.5f,"idle and dash recovered x"+speed+" state="+actor.State+" offset="+actor.Graphic.rectTransform.anchoredPosition.x);
    Require(actorRect.anchoredPosition==origin&&actor.Graphic.Skeleton.FindSlot("attack_weapon").Attachment==null,"no actor drift or stale sword");
   }
   Require(actor.MeshBuildCount>meshes,"Spine mesh animated");
   int afterCancel=0;actor.PlayAttackCombo();fx.StartCoroutine(fx.PlayCombo(actor,target,1,i=>afterCancel++));yield return new WaitForSeconds(.8f);Require(afterCancel==1,"cancel probe first contact");fx.Cancel();actor.PlayHit();yield return new WaitForSeconds(.65f);Require(afterCancel==1&&!fx.IsPlaying&&fx.ActiveVisualCount==0&&actor.State==GuxuCharacterMotion.ActionState.Idle,"cancel and hit interruption");
   actor.PlayAttackCombo();fx.StartCoroutine(fx.PlayCombo(actor,target,1,i=>afterCancel++));yield return new WaitForSeconds(.15f);actor.gameObject.SetActive(false);yield return null;fx.Cancel();actor.gameObject.SetActive(true);Require(actor.State==GuxuCharacterMotion.ActionState.Idle,"disable reset");
   game.Home();yield return null;game.StartBattle();yield return new WaitForSeconds(.8f);Require(UnityEngine.Object.FindObjectOfType<GuxuCharacterMotion>().State==GuxuCharacterMotion.ActionState.Attack||UnityEngine.Object.FindObjectOfType<GuxuCombatVfx>()!=null,"battle flow integrated");game.Home();yield return null;
   Require(!UnityEngine.Object.FindObjectsOfType<GuxuCombatStroke>().Any()&&!UnityEngine.Object.FindObjectsOfType<GuxuCombatVfx>().Any(),"screen switch cleans effects");
   Require(game.Rules.Save.stage==1,"cancelled battle did not settle");
   File.WriteAllText(Receipt,"PASS "+DateTime.Now.ToString("O")+"\n3-second Spine combo; actual weapon, arc, ring, projectile meshes; 1x/2x/3x ordered contacts and timing; finite vertices; exact damage partition; cancellation; hit interrupt; disable reset; scene cleanup; seeded rules and single-settlement regression. Save restored after QA.");
  }finally{game.StopAllCoroutines();game.Rules.Save=Newtonsoft.Json.JsonConvert.DeserializeObject<SaveData>(save);game.Rules.Store();Time.captureFramerate=captureRate;camera.targetTexture=previous;rt.Release();UnityEngine.Object.Destroy(rt);}
 }
 static void Capture(string name){Canvas.ForceUpdateCanvases();var camera=Camera.main;var active=RenderTexture.active;var pixels=new Texture2D(750,1334,TextureFormat.RGB24,false);try{camera.Render();RenderTexture.active=camera.targetTexture;pixels.ReadPixels(new Rect(0,0,750,1334),0,0);pixels.Apply();File.WriteAllBytes("Logs/attack-combo/"+name+".png",pixels.EncodeToPNG());}finally{RenderTexture.active=active;UnityEngine.Object.Destroy(pixels);}}
}
public sealed class GuxuAttackComboRunner:MonoBehaviour{}
