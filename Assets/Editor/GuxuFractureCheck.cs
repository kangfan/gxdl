using System;
using System.Collections;
using System.IO;
using System.Linq;
using Gxdl;
using UnityEditor;
using UnityEngine;

public static class GuxuFractureCheck {
 [MenuItem("GXDL/QA/Fracture Capture")]
 public static void Run(){
  if(!Application.isPlaying)throw new InvalidOperationException("Enter Play mode first.");
  GxdlGame.Instance.StartCoroutine(Check());
 }
 static IEnumerator Check(){
  var game=GxdlGame.Instance;
  string save=Newtonsoft.Json.JsonConvert.SerializeObject(game.Rules.Save);
  int captureRate=Time.captureFramerate;
  GuxuAnimation animation=null;
  string folder="Logs/fracture-capture";
  Directory.CreateDirectory(folder);
  File.WriteAllText("Logs/guxu-fracture-check.txt","RUNNING "+DateTime.Now.ToString("O"));
  try{
   game.Home();yield return null;
   animation=UnityEngine.Object.FindObjectOfType<GuxuAnimation>();
   Time.captureFramerate=30;
   animation.StartCoroutine(animation.PlayUnseal(GuxuUiArt.Item(0),GuxuUiArt.Item(0),5));
   bool rendered=false;
   for(int frame=0;frame<84;frame++){
    yield return new WaitForEndOfFrame();
    if(animation.Phase=="fracture")rendered|=UnityEngine.Object.FindObjectsOfType<GuxuStoneShard>().Any(x=>x.MeshBuildCount>0);
    ScreenCapture.CaptureScreenshot(folder+"/frame-"+frame.ToString("D3")+".png");
    yield return null;
   }
   Require(rendered,"fracture mesh was rendered");
   Require(!animation.IsPlaying&&animation.TemporaryCount==0,"normal completion cleanup");
   animation.StartCoroutine(animation.PlayUnseal(GuxuUiArt.Item(0),GuxuUiArt.Item(0),1));
   yield return new WaitForSeconds(.65f);
   Require(animation.IsPlaying&&animation.TemporaryCount>0,"mid-fracture cancellation fixture");
   animation.Cancel();yield return null;
   Require(!animation.IsPlaying&&animation.TemporaryCount==0,"cancel cleanup");
   animation.StartCoroutine(animation.PlayUnseal(GuxuUiArt.Item(0),GuxuUiArt.Item(0),5));
   animation.SkipReveal();yield return new WaitForSeconds(GuxuAnimation.RareDuration+.2f);
   Require(!animation.IsPlaying&&animation.TemporaryCount==0,"skip cleanup");
   Require(save==Newtonsoft.Json.JsonConvert.SerializeObject(game.Rules.Save),"presentation cannot mutate save");
   File.WriteAllText("Logs/guxu-fracture-check.txt","PASS: rendered fracture, normal/cancel/skip cleanup, save unchanged, 84 captured frames. "+DateTime.Now.ToString("O"));
  }finally{Time.captureFramerate=captureRate;if(animation)animation.Cancel();game.Home();}
 }
 static void Require(bool condition,string message){if(!condition)throw new Exception("Fracture QA: "+message);}
}
