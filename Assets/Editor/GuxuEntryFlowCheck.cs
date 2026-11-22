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
using Object=UnityEngine.Object;

[InitializeOnLoad]
public static class GuxuEntryFlowCheck {
 const string Key="GXDL.EntryQA.",Receipt="Logs/accounts/entry-flow.txt";
 const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
 static bool running;
 static GuxuEntryFlowCheck(){EditorApplication.update+=Monitor;EditorApplication.playModeStateChanged+=Restore;}
 [MenuItem("GXDL/Accounts/Verify Entry Flow")]
 public static void Run(){
  if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Stop Play Mode first.");
  Directory.CreateDirectory("Logs/accounts");File.WriteAllText(Receipt,"RUNNING");
  for(int i=0;i<2;i++)SessionState.SetString(Key+"scope"+i,GuxuAccountConfig.Hash("entry-qa:"+Guid.NewGuid()));
  SessionState.SetBool(Key+"active",true);
  SessionState.SetFloat(Key+"deadline",(float)EditorApplication.timeSinceStartup+100);GuxuAccountChecks.Preview();
 }
 static void Monitor(){
  if(!SessionState.GetBool(Key+"active",false))return;
  if(EditorApplication.isPlaying&&GxdlGame.Instance&&!running){running=true;new GameObject("Entry Flow QA").AddComponent<GuxuEntryFlowCheckRunner>().StartCoroutine(Guard(Check()));}
  if(EditorApplication.timeSinceStartup>SessionState.GetFloat(Key+"deadline",0)){File.AppendAllText(Receipt,"\nFAIL timeout");EditorApplication.isPlaying=false;}
 }
 static void Restore(PlayModeStateChange state){
  if(state!=PlayModeStateChange.EnteredEditMode||!SessionState.GetBool(Key+"active",false))return;
  string previous=GameRules.AccountScope;
  try{for(int i=0;i<2;i++){
   GameRules.SelectAccount(SessionState.GetString(Key+"scope"+i,""));
   foreach(string suffix in new[]{"",".tmp",".bak",".before-equipment15",".before-expansion",".invalid"})if(File.Exists(GameRules.SavePath+suffix))File.Delete(GameRules.SavePath+suffix);
   PlayerPrefs.DeleteKey(GxdlGame.OpeningStoryKey);PlayerPrefs.DeleteKey(GxdlGame.FirstUnsealStoryKey);
  }}finally{GameRules.SelectAccount(previous);PlayerPrefs.Save();SessionState.SetBool(Key+"active",false);running=false;}
  if(File.ReadAllText(Receipt)=="RUNNING")File.WriteAllText(Receipt,"FAIL: check interrupted");
 }
 static IEnumerator Guard(IEnumerator routine){
  var stack=new Stack<IEnumerator>();stack.Push(routine);
  try{while(stack.Count>0){
   var current=stack.Peek();bool more;object next;
   try{more=current.MoveNext();next=more?current.Current:null;}
   catch(Exception error){File.WriteAllText(Receipt,"FAIL: "+error);Debug.LogException(error);yield break;}
   if(!more){(stack.Pop() as IDisposable)?.Dispose();continue;}
   if(next is IEnumerator nested){stack.Push(nested);continue;}
   yield return next;
  }}finally{while(stack.Count>0)(stack.Pop() as IDisposable)?.Dispose();EditorApplication.isPlaying=false;}
 }
 static void Require(bool value,string message){if(!value)throw new Exception("Entry flow: "+message);}
 static void Invoke(GxdlGame game,string method)=>typeof(GxdlGame).GetMethod(method,Private).Invoke(game,null);
 static GuxuAccountGate Gate(GxdlGame game)=>(GuxuAccountGate)typeof(GuxuAccountSession).GetField("gate",Private).GetValue(game.GetComponent<GuxuAccountSession>());
 static void Login(GxdlGame game,string scope){
  typeof(GuxuAccountSession).GetField("<SaveScope>k__BackingField",Private).SetValue(game.GetComponent<GuxuAccountSession>(),scope);
  Gate(game).Begin();Gate(game).Receive(500);Invoke(game,"RenderAccountState");
 }
 static void Restrict(GxdlGame game){Gate(game).Receive(1030);Invoke(game,"RenderAccountState");}
 static void Factory(GxdlGame game,Func<RectTransform,IGuxuEntryLoadingView> value)=>typeof(GxdlGame).GetField("entryLoadingTestFactory",Private).SetValue(game,value);
 static IEnumerator Until(Func<bool> predicate,string message,float seconds=8){
  float end=Time.realtimeSinceStartup+seconds;while(!predicate()&&Time.realtimeSinceStartup<end)yield return null;Require(predicate(),message);
 }
 static bool HasHome()=>GameObject.Find("Seeker Character Layer")!=null;
 static IEnumerator Check(){
  yield return null;
  var game=GxdlGame.Instance;string first=SessionState.GetString(Key+"scope0",""),second=SessionState.GetString(Key+"scope1","");
  Require(!game.CanPlay&&game.Rules==null,"login required before any game data");
  TestLoadingView loading=null;int creations=0;
  Func<RectTransform,IGuxuEntryLoadingView> create=parent=>{creations++;return loading=new TestLoadingView(parent);};Factory(game,create);
  Login(game,first);Invoke(game,"RenderAccountState");
  Require(game.EntryStage==GuxuEntryStage.Loading&&!game.CanPlay,"login starts loading, not Home");
  game.Home();game.Chop();game.StartBattle();game.ReplayOpening();Require(!HasHome()&&!game.IsStoryPlaying,"public entries cannot bypass loading");
  yield return Until(()=>game.EntryProgress==1,"resources reach 100 percent");
  Require(creations==1&&loading.Updates>5&&!HasHome()&&!game.CanPlay,"one monotonic load and no Home before visual completion");
  yield return null;Require(game.EntryStage==GuxuEntryStage.Loading,"waits for the loading view to finish");
  loading.Complete=true;
  yield return Until(()=>game.EntryStage==GuxuEntryStage.Opening&&game.IsStoryPlaying,"first account plays opening");
  Require(!HasHome()&&!game.CanPlay&&PlayerPrefs.GetInt(GxdlGame.OpeningStoryKey,0)==0,"no Home or seen flag during opening");
  int peaches=game.Rules.Save.peaches;game.Home();game.Chop();game.StartBattle();game.ReplayOpening();Require(!HasHome()&&game.Rules.Save.peaches==peaches,"gameplay blocked throughout opening");
  yield return Until(()=>game.EntryStage==GuxuEntryStage.Ready,"natural opening enters Home",35);
  Require(game.CanPlay&&HasHome()&&PlayerPrefs.GetInt(GxdlGame.OpeningStoryKey)==1,"completion unlocks Home and persists");
  Restrict(game);Require(!game.CanPlay&&game.Rules==null&&!HasHome(),"restriction immediately closes gameplay");
  Login(game,first);yield return Until(()=>game.EntryProgress==1,"returning account loads");loading.Complete=true;
  yield return Until(()=>game.IsStoryPlaying,"returning account also plays opening in Editor");
  Require(!HasHome()&&!game.CanPlay,"watched flag does not bypass current opening");
  UnityEngine.Object.Destroy(GameObject.Find("Final Art Story Overlay"));yield return Until(()=>game.EntryStage==GuxuEntryStage.Failed,"interrupted replay fails despite existing seen flag");
  Require(PlayerPrefs.GetInt(GxdlGame.OpeningStoryKey)==1&&!game.CanPlay,"replay interruption preserves old record without entering Home");
  game.RetryGameEntry();yield return Until(()=>game.EntryProgress==1,"replay retry loads");loading.Complete=true;
  yield return Until(()=>game.IsStoryPlaying,"replay retry starts opening");game.SkipStory();
  yield return Until(()=>game.EntryStage==GuxuEntryStage.Ready,"returning account enters Home after opening");
  Restrict(game);Login(game,second);yield return Until(()=>game.EntryProgress==1,"second account loads");loading.Complete=true;
  yield return Until(()=>game.IsStoryPlaying,"new account independently plays opening");
  Restrict(game);yield return null;Require(!game.IsStoryPlaying&&PlayerPrefs.GetInt("Guxu.FinalArt.OpeningSeen.v1"+second,0)==0,"interrupted opening not marked seen");
  Login(game,second);yield return Until(()=>game.EntryProgress==1,"interrupted account reloads");loading.Complete=true;
  yield return Until(()=>game.IsStoryPlaying,"interrupted opening plays again");game.StopAllCoroutines();
  yield return Until(()=>game.EntryStage==GuxuEntryStage.Failed,"stopped opening offers retry");
  Require(PlayerPrefs.GetInt(GxdlGame.OpeningStoryKey,0)==0,"stopped coroutine does not mark opening seen");
  game.RetryGameEntry();yield return Until(()=>game.EntryProgress==1,"stopped opening reloads");loading.Complete=true;
  yield return Until(()=>game.IsStoryPlaying,"opening retry restarts cinematic");game.SkipStory();
  yield return Until(()=>game.EntryStage==GuxuEntryStage.Ready,"skip enters Home");Require(PlayerPrefs.GetInt(GxdlGame.OpeningStoryKey)==1,"explicit skip counts as seen");
  Restrict(game);Login(game,first);yield return Until(()=>game.EntryProgress==1,"load before cancellation");var stale=loading;
  Restrict(game);stale.Complete=true;yield return null;yield return null;Require(game.EntryStage==GuxuEntryStage.Login&&!HasHome()&&!game.CanPlay,"late completion after restriction cannot enter");
  Login(game,first);yield return Until(()=>game.EntryProgress==1,"load before stopped routine");game.StopAllCoroutines();
  yield return Until(()=>game.EntryStage==GuxuEntryStage.Failed,"stopped loading offers retry");Restrict(game);
  Factory(game,parent=>null);Login(game,first);yield return Until(()=>game.EntryStage==GuxuEntryStage.Failed,"missing adapter fails closed");
  Require(!game.CanPlay&&GameObject.Find("Entry Retry"),"failure offers retry without gameplay");
  Factory(game,create);game.RetryGameEntry();yield return Until(()=>game.EntryProgress==1,"retry loads again");loading.Complete=true;
  yield return Until(()=>game.IsStoryPlaying,"retry still plays opening in Editor");game.SkipStory();
  yield return Until(()=>game.EntryStage==GuxuEntryStage.Ready,"retry succeeds");
  File.WriteAllText(Receipt,"PASS: login -> real resource loading -> visual 100% acknowledgement -> opening -> Home; duplicate callback prevention; no Home/gameplay during loading or opening; natural 30-second completion; Editor replays opening for returning accounts; existing seen flag cannot bypass interrupted replay; account isolation; interruption without seen flag; explicit skip; restriction and stale UI completion; stopped loading/opening recover through retry; missing view fails closed; retry. Editor-only account gate/view substitutes; no SDK authentication or SMS sent. "+DateTime.Now.ToString("O"));
  Debug.Log("[Entry QA] Flow checks passed.");
 }
 sealed class TestLoadingView:IGuxuEntryLoadingView {
  public bool Complete;public int Updates;float progress;
  public bool IsComplete=>Complete&&progress==1;
  readonly Text label;
  public TestLoadingView(RectTransform parent){
   var item=new GameObject("QA Loading Progress",typeof(RectTransform),typeof(Text));item.transform.SetParent(parent,false);label=item.GetComponent<Text>();label.font=Resources.Load<Font>("Original/GameFont");label.fontSize=28;
   var rect=(RectTransform)item.transform;rect.anchorMin=rect.anchorMax=Vector2.one*.5f;rect.sizeDelta=new Vector2(600,100);label.alignment=TextAnchor.MiddleCenter;
  }
  public void SetProgress(float value,string message){Require(value>=progress&&value<=1,"progress is monotonic and bounded");progress=value;Updates++;label.text="QA · "+message+" "+Mathf.RoundToInt(value*100)+"%";}
 }
}
public sealed class GuxuEntryFlowCheckRunner:MonoBehaviour {}
