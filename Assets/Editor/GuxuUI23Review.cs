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
using UnityEngine.EventSystems;
[InitializeOnLoad] public static class GuxuUI23Review {
 static string Output="Reference/UIRedesign20261110/review";
 public static string ActiveOutput=>Output;
 const BindingFlags Private=BindingFlags.NonPublic|BindingFlags.Instance;
 static IDisposable fixture;static bool busy,loginReview;static int sequence;
 [Serializable] internal sealed class Command {public string action="inspect",name="",scenario="growth",text="";public int index,height=1334,fps=15;public float delta,seconds=12;}
 sealed class LoginFixture:IDisposable {
  readonly GuxuAccountSession account=UnityEngine.Object.FindObjectOfType<GuxuAccountSession>();
  readonly bool had=PlayerPrefs.HasKey("GXDL.Privacy.Accepted");readonly string original=PlayerPrefs.GetString("GXDL.Privacy.Accepted","");readonly bool consent;
  readonly EditorWindow window;readonly PropertyInfo sizeProperty;readonly int size;
  public LoginFixture(){consent=account.Consented;window=EditorWindow.GetWindow(typeof(Editor).Assembly.GetType("UnityEditor.GameView"));sizeProperty=window.GetType().GetProperty("selectedSizeIndex",BindingFlags.Public|Private);size=(int)sizeProperty.GetValue(window);}
  public void Dispose(){account.SetConsent(consent);if(had)PlayerPrefs.SetString("GXDL.Privacy.Accepted",original);else PlayerPrefs.DeleteKey("GXDL.Privacy.Accepted");PlayerPrefs.Save();sizeProperty.SetValue(window,size);typeof(GxdlGame).GetMethod("RenderAccountState",Private).Invoke(GxdlGame.Instance,null);}
 }
 static GuxuUI23Review(){
  EditorApplication.playModeStateChanged+=state=>{if(state==PlayModeStateChange.ExitingPlayMode&&fixture!=null)End();};
  AssemblyReloadEvents.beforeAssemblyReload+=()=>{if(fixture==null)return;File.WriteAllText(Output+"/reload-trigger.txt",DateTime.UtcNow.ToString("O"));try{End();}catch(Exception e){Debug.LogException(e);}};
 }
 sealed class PrefSnapshot {public string key;public bool exists;public int value;}
 sealed class RecoverySnapshot {
  public string at,savePath,sha256,accountScope,storagePath;public bool saveExisted;public int gameViewIndex;public PrefSnapshot[] prefs;public SaveData player;
 }
 static RecoverySnapshot RecoveryState(){
  var game=GxdlGame.Instance;var window=EditorWindow.GetWindow(typeof(Editor).Assembly.GetType("UnityEditor.GameView"));string path=GameRules.SavePath;
  string hash=null;if(File.Exists(path))using(var sha=System.Security.Cryptography.SHA256.Create())hash=BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(path))).Replace("-","").ToLowerInvariant();
  return new RecoverySnapshot{at=DateTime.UtcNow.ToString("O"),savePath=path,saveExisted=File.Exists(path),sha256=hash,accountScope=GameRules.AccountScope,storagePath=(string)typeof(GameRules).GetField("storagePath",Private).GetValue(game.Rules),gameViewIndex=(int)window.GetType().GetProperty("selectedSizeIndex",BindingFlags.Public|Private).GetValue(window),prefs=new[]{GxdlGame.OpeningStoryKey,GxdlGame.FirstUnsealStoryKey}.Select(key=>new PrefSnapshot{key=key,exists=PlayerPrefs.HasKey(key),value=PlayerPrefs.GetInt(key,0)}).ToArray(),player=game.Rules.Save};
 }
 static void SaveRecoveryBefore(){
  var state=RecoveryState();if(state.saveExisted)File.Copy(state.savePath,Output+"/original-player-save.bin",true);
  File.WriteAllText(Output+"/recovery-before.json",JsonConvert.SerializeObject(state,Formatting.Indented));
 }
 static void VerifyRecoveryAfter(){
  var before=JsonConvert.DeserializeObject<RecoverySnapshot>(File.ReadAllText(Output+"/recovery-before.json"));var after=RecoveryState();
  File.WriteAllText(Output+"/recovery-after.json",JsonConvert.SerializeObject(after,Formatting.Indented));
  bool prefs=before.prefs.Length==after.prefs.Length&&before.prefs.Zip(after.prefs,(a,b)=>a.key==b.key&&a.exists==b.exists&&(!a.exists||a.value==b.value)).All(equal=>equal);
  if(before.savePath!=after.savePath||before.saveExisted!=after.saveExisted||before.sha256!=after.sha256||before.accountScope!=after.accountScope||before.storagePath!=after.storagePath||before.gameViewIndex!=after.gameViewIndex||!prefs||JsonConvert.SerializeObject(before.player)!=JsonConvert.SerializeObject(after.player))throw new Exception("Review recovery checkpoint mismatch; inspect recovery-before/after.json. Original backup is retained.");
 }
 [MenuItem("GXDL/QA/UI23 Review Begin")]
 public static void Begin()=>BeginAt("Reference/UIRedesign20261110/review","growth");
 [MenuItem("GXDL/QA/UIStyle Review Begin")]
 public static void BeginStyle(){BeginAt("Reference/UIStyleRework20261111/sessions/"+DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fff"),"chapter5");File.WriteAllText("Reference/UIStyleRework20261111/review-session.txt",Path.GetFullPath(Output));}
 [MenuItem("GXDL/QA/Motion Review Begin")]
 public static void BeginMotion(){BeginAt("Reference/MotionVfx20261111/sessions/"+DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fff"),"growth");File.WriteAllText("Reference/MotionVfx20261111/review-session.txt",Path.GetFullPath(Output));}
 [MenuItem("GXDL/QA/Cinematic Review Begin")]
 public static void BeginCinematic(){BeginAt("Reference/MotionVfxCinematic20261111/sessions/"+DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fff"),"growth");File.WriteAllText("Reference/MotionVfxCinematic20261111/review-session.txt",Path.GetFullPath(Output));}
 [MenuItem("GXDL/QA/Hud Review Begin")]
 public static void BeginHud(){BeginAt("Reference/HudRework20261111/sessions/"+DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fff"),"ordinary-unseal");File.WriteAllText("Reference/HudRework20261111/review-session.txt",Path.GetFullPath(Output));}
 [MenuItem("GXDL/QA/Growth Review Begin")]
 public static void BeginGrowth(){BeginAt("Reference/GrowthBalance20261111/sessions/"+DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fff"),"growth-new");File.WriteAllText("Reference/GrowthBalance20261111/review-session.txt",Path.GetFullPath(Output));}
 [MenuItem("GXDL/QA/Submission Feedback Review Begin")]
 public static void BeginSubmissionFeedback(){BeginAt("Reference/SubmissionFeedback20261117/sessions/"+DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fff"),"growth");File.WriteAllText("Reference/SubmissionFeedback20261117/review-session.txt",Path.GetFullPath(Output));}
 static void BeginAt(string output,string scenario){
  if(fixture!=null)throw new Exception("Review already owns an isolated fixture.");
  if(!Application.isPlaying||!GxdlGame.Instance||!GxdlGame.Instance.CanPlay)throw new Exception("Start Preview Gameplay (Offline) before Review Begin.");
  Output=output;Directory.CreateDirectory(Output);SaveRecoveryBefore();loginReview=false;fixture=(IDisposable)Activator.CreateInstance(typeof(GuxuIterationCheck).GetNestedType("Fixture",BindingFlags.NonPublic),true);
  typeof(GameRules).GetField("storagePath",Private).SetValue(GxdlGame.Instance.Rules,Path.GetFullPath(Output+"/player-fixture.json"));sequence=0;Scenario(scenario);
  File.WriteAllText(Output+"/status.txt","ACTIVE isolated review fixture; use Review End to restore. "+DateTime.UtcNow.ToString("O"));
 }
 [MenuItem("GXDL/QA/UI23 Review Login Begin")]
 public static void BeginLogin(){if(fixture!=null||!Application.isPlaying||!GxdlGame.Instance||GxdlGame.Instance.EntryStage!=GuxuEntryStage.Login)throw new Exception("Use normal Login Play with no active review.");Directory.CreateDirectory(Output);fixture=new LoginFixture();loginReview=true;sequence=0;File.WriteAllText(Output+"/status.txt","ACTIVE login review; local invalid-input checks only; use Review End to restore consent/GameView. "+DateTime.UtcNow.ToString("O"));}
 [MenuItem("GXDL/QA/Submission Feedback Login Review Begin")]
 public static void BeginSubmissionFeedbackLogin(){
  if(fixture!=null||!GuxuPhoneTestLogin.Enabled)throw new Exception("Requires explicitly enabled internal test login and no active review; back up test accounts before logging in.");
  Output="Reference/SubmissionFeedback20261117/login-sessions/"+DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fff");BeginLogin();File.WriteAllText("Reference/SubmissionFeedback20261117/review-session.txt",Path.GetFullPath(Output));
 }
 static void Scenario(string scenario){
  var g=GxdlGame.Instance;var r=g.Rules;bool fresh=scenario=="new"||scenario=="first-unseal"||scenario=="defeat";
  if(scenario=="new"||scenario=="first-unseal")PlayerPrefs.DeleteKey(GxdlGame.FirstUnsealStoryKey);
  if(scenario=="ordinary-unseal"||scenario=="hud-no-seals")PlayerPrefs.SetInt(GxdlGame.FirstUnsealStoryKey,1);
  Array.Clear((int[])typeof(GxdlGame).GetField("chapterSelections",Private).GetValue(g),0,10);
  if(scenario=="growth-new"||scenario=="growth-mid"||scenario=="growth-late"||scenario=="growth-double"||scenario=="growth-failure"||scenario=="growth-resume"||scenario=="growth-reward"||scenario=="growth-hint"){
   string source=scenario=="growth-new"?null:scenario=="growth-resume"?"Reference/GrowthBalance20261111/actual-newbie-before-stop-bug.json":scenario=="growth-reward"?"Reference/GrowthBalance20261111/actual-newbie-post6.json":scenario=="growth-hint"?"Reference/GrowthBalance20261111/actual-newbie-failure5.json":"Reference/GrowthBalance20261111/simulation/"+scenario+".json";
   var save=source==null?new SaveData{equipmentSchema=1}:JsonConvert.DeserializeObject<SaveData>(File.ReadAllText(source));
   GameRules.ValidateSave(save);r.Save=new GameRules(r.Config,save,42).Save;r.EnsureExpansion();typeof(GameRules).GetField("random",Private).SetValue(r,new System.Random(42));
   if(source==null)PlayerPrefs.DeleteKey(GxdlGame.FirstUnsealStoryKey);else PlayerPrefs.SetInt(GxdlGame.FirstUnsealStoryKey,1);
   File.WriteAllText(Output+"/scenario-source.json",JsonConvert.SerializeObject(new{scenario,source=source??"Default SaveData; seed 42; no injected resources",kind=source==null?"new-player":source.Contains("/actual-")?"actual-review-player":"sim-derived",at=DateTime.UtcNow.ToString("O")},Formatting.Indented));g.Home();return;
  }
  if(scenario=="finalboss"||scenario=="motion-poison"){typeof(GuxuIterationCheck).GetMethod("CombatBuild",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{fixture,scenario=="motion-poison"?140:200,0});g.Home();return;}
  r.Save=new GameRules(r.Config,new SaveData{equipmentSchema=1,stage=scenario=="new"||scenario=="first-unseal"?1:201,level=fresh?1:60,coins=50000,peaches=120,forgeOre=3000,insight=1000,breakthroughStones=100},42).Save;r.EnsureExpansion();
  if(!fresh){r.Save.exp=r.ExpNeeded;for(int i=0;i<24;i++)r.Save.gemInventory[i]=6;}
  if(scenario=="hud-no-seals")r.Save.peaches=0;
  if(scenario=="talentconcept"){r.TrainTalent(0);((int[])typeof(GxdlGame).GetField("talentSelections",Private).GetValue(g))[0]=1;g.ShowTalents(0);return;}
  if(scenario=="gemconcept"){r.SocketGem(0,0,0);r.SocketGem(0,1,7);((int[])typeof(GxdlGame).GetField("gemLevels",Private).GetValue(g))[1]=1;g.ShowGems(0,1,1);return;}
  if(scenario=="combat")foreach(var gear in r.Save.gear){gear.quality=5;gear.level=3;gear.lootBand=9;EquipmentTiers.Apply(gear);}
  if(scenario.StartsWith("chapter")){
   int stage=scenario=="chapter5"?5:scenario=="chapter10"?10:scenario=="chapter15"?15:scenario=="chapter20"?20:scenario=="chapterclear"?21:scenario=="chapterold"?41:201;
   r.Save=new GameRules(r.Config,new SaveData{equipmentSchema=1,stage=stage,level=30,coins=50000,peaches=120},42).Save;r.EnsureExpansion();g.ShowChapter(scenario=="chapterend"?9:0);return;
  }
  g.Home();
 }
 [MenuItem("GXDL/QA/UI23 Review Command")]
 [MenuItem("GXDL/QA/UIStyle Review Command")]
 [MenuItem("GXDL/QA/Motion Review Command")]
 [MenuItem("GXDL/QA/Cinematic Review Command")]
 [MenuItem("GXDL/QA/Hud Review Command")]
 [MenuItem("GXDL/QA/Growth Review Command")]
 public static void Execute(){
  if(fixture==null||busy)throw new Exception("Begin isolated review and wait for prior command.");
  var command=JsonConvert.DeserializeObject<Command>(File.ReadAllText(Output+"/command.json"));busy=true;
  new GameObject("UI23 Review Runner").AddComponent<GuxuAccountCheckRunner>().StartCoroutine(Guard(command));
 }
 static IEnumerator Guard(Command command){
  var routine=Run(command);bool pass=false;
  try{while(true){bool moved;object current=null;try{moved=routine.MoveNext();if(moved)current=routine.Current;}catch(Exception e){File.WriteAllText(Output+"/command-error.txt",e.ToString());Debug.LogException(e);yield break;}if(!moved){pass=true;break;}yield return current;}}
  finally{(routine as IDisposable)?.Dispose();busy=false;File.WriteAllText(Output+"/command-status.txt",(pass?"PASS":"FAIL")+" "+command.action+" "+DateTime.UtcNow.ToString("O"));}
 }
 internal static IEnumerator Run(Command command){
  var g=GxdlGame.Instance;var r=g.Rules;
  bool recording=command.action=="capture-click"||command.action=="record";
  if(recording&&(loginReview||command.seconds<=0||command.seconds>15||(command.fps!=15&&command.fps!=30)))throw new Exception("Gameplay capture requires0<seconds<=15 and fps15 or30.");
  string captureBefore=recording?JsonConvert.SerializeObject(r.Save):null;
  if(command.action=="size"){if(command.height!=1334&&command.height!=1624)throw new Exception("Supported heights: 1334/1624");typeof(GuxuBattlePreparationCheck).GetMethod("SetSize",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{750,command.height});yield return null;}
  else if(command.action=="scenario"){if(loginReview||!new[]{"growth-new","growth-mid","growth-late","growth-double","growth-failure","growth-resume","growth-reward","growth-hint","growth","new","first-unseal","ordinary-unseal","hud-no-seals","combat","defeat","finalboss","motion-poison","chapter5","chapter10","chapter15","chapter20","chapterclear","chapterold","chapterend","talentconcept","gemconcept"}.Contains(command.scenario))throw new Exception("Unknown or unavailable review scenario");Scenario(command.scenario);yield return null;}
  else if(command.action=="input"){
   bool allowed=loginReview?(new[]{"Account Phone","Account Code"}.Contains(command.name)||GuxuPhoneTestLogin.Enabled&&new[]{"Experience Username","Experience Password"}.Contains(command.name)):command.name=="Social Input";
   if(!allowed)throw new Exception("Input action is limited to named login or social fields");UnityEngine.Object.FindObjectsOfType<InputField>().Single(f=>f.name==command.name).text=command.text;yield return null;
  }
  else if(command.action=="scroll"||command.action=="drag"){
   if(loginReview)throw new Exception("Scroll review is gameplay only");yield return GuxuMotionReviewCapture.FrameEnd;var scroll=UnityEngine.Object.FindObjectsOfType<ScrollRect>().Single(s=>s.gameObject.activeInHierarchy&&(command.name==""||s.name==command.name));
   var camera=scroll.GetComponentInParent<Canvas>().worldCamera;var start=RectTransformUtility.WorldToScreenPoint(camera,scroll.viewport.TransformPoint(scroll.viewport.rect.center));var pointer=new PointerEventData(EventSystem.current){position=start,pressPosition=start,button=PointerEventData.InputButton.Left};var hits=new List<RaycastResult>();EventSystem.current.RaycastAll(pointer,hits);
   if(hits.Count==0||ExecuteEvents.GetEventHandler<IScrollHandler>(hits[0].gameObject)!=scroll.gameObject)throw new Exception("Scroll viewport is blocked");
   pointer.pointerPressRaycast=hits[0];pointer.pointerCurrentRaycast=hits[0];pointer.pointerDrag=scroll.gameObject;
   if(command.action=="scroll"){pointer.scrollDelta=new Vector2(0,command.delta);ExecuteEvents.Execute(scroll.gameObject,pointer,ExecuteEvents.scrollHandler);}
   else{ExecuteEvents.Execute(scroll.gameObject,pointer,ExecuteEvents.initializePotentialDrag);ExecuteEvents.Execute(scroll.gameObject,pointer,ExecuteEvents.beginDragHandler);pointer.position=start+new Vector2(0,command.delta);pointer.delta=new Vector2(0,command.delta);ExecuteEvents.Execute(scroll.gameObject,pointer,ExecuteEvents.dragHandler);yield return null;ExecuteEvents.Execute(scroll.gameObject,pointer,ExecuteEvents.endDragHandler);scroll.StopMovement();}
   yield return null;
  }
  else if(command.action=="click"||command.action=="reject-click"||command.action=="capture-click"){
   yield return GuxuMotionReviewCapture.FrameEnd;var buttons=Buttons();var button=buttons.Where(b=>b.name==command.name).ElementAt(command.index);if(!button.interactable&&command.action!="reject-click")throw new Exception("Button is disabled: "+command.name);
   if(loginReview&&(command.name=="Account Tap Login"||(command.name=="Experience Login"&&!GuxuPhoneTestLogin.Enabled)||((command.name=="Account Send Code"||command.name=="Account Phone Login")&&GuxuAccountConfig.Phone(UnityEngine.Object.FindObjectsOfType<InputField>().Single(f=>f.name=="Account Phone").text.Trim()))))throw new Exception("Login review allows local invalid inputs or explicitly enabled internal experience accounts; no production authentication/SMS request");
   Canvas.ForceUpdateCanvases();var rect=(RectTransform)button.transform;var pointer=new PointerEventData(EventSystem.current){position=RectTransformUtility.WorldToScreenPoint(button.GetComponentInParent<Canvas>().worldCamera,rect.TransformPoint(rect.rect.center)),button=PointerEventData.InputButton.Left};var hits=new List<RaycastResult>();EventSystem.current.RaycastAll(pointer,hits);
   bool clickable=button.interactable&&pointer.position.x>=0&&pointer.position.y>=0&&pointer.position.x<=Screen.width&&pointer.position.y<=Screen.height&&hits.Count>0&&ExecuteEvents.GetEventHandler<IPointerClickHandler>(hits[0].gameObject)==button.gameObject;
   if(command.action=="reject-click"){if(clickable)throw new Exception("Expected click to be blocked: "+command.name);}
   else{if(!clickable)throw new Exception("Button center is blocked: "+command.name);ExecuteEvents.Execute(button.gameObject,pointer,ExecuteEvents.pointerDownHandler);yield return null;ExecuteEvents.Execute(button.gameObject,pointer,ExecuteEvents.pointerUpHandler);ExecuteEvents.Execute(button.gameObject,pointer,ExecuteEvents.pointerClickHandler);yield return null;}
  }else if(command.action!="inspect"&&command.action!="record")throw new Exception("Unknown action");
  if(recording){var capture=GuxuMotionReviewCapture.Record(Output+"/"+(sequence+1).ToString("000")+"-"+command.action+"-frames",command.seconds,command.fps,captureBefore);try{while(capture.MoveNext())yield return capture.Current;}finally{(capture as IDisposable)?.Dispose();}}
  yield return new WaitForSeconds(.35f);yield return GuxuMotionReviewCapture.FrameEnd;string prefix=(++sequence).ToString("000")+"-"+command.action;
  var image=GuxuMotionReviewCapture.Capture();File.WriteAllBytes(Output+"/"+prefix+".png",image.EncodeToPNG());UnityEngine.Object.Destroy(image);
  var rows=Buttons().Select(b=>new{name=b.name,b.interactable,rect=new[]{((RectTransform)b.transform).rect.width,((RectTransform)b.transform).rect.height},text=string.Join(" | ",b.GetComponentsInChildren<Text>().Select(t=>t.text))}).ToArray();
  if(command.action=="input"&&command.name=="Experience Password")command.text="[REDACTED]";
  var state=new{at=DateTime.UtcNow.ToString("O"),command,screen=g.ExpansionScreen,viewport=new[]{Screen.width,Screen.height},screenshot=Path.GetFullPath(Output+"/"+prefix+".png"),buttons=rows,scrolls=UnityEngine.Object.FindObjectsOfType<ScrollRect>().Select(s=>new{s.name,offset=s.content.anchoredPosition.y,viewport=s.viewport.rect.height,content=s.content.rect.height}).ToArray(),texts=UnityEngine.Object.FindObjectsOfType<Text>().Where(t=>t.gameObject.activeInHierarchy).Select(t=>new{name=t.name,value=t.text}).ToArray(),player=r?.Save,attributes=r?.Attributes};
  string json=JsonConvert.SerializeObject(state,Formatting.Indented);File.WriteAllText(Output+"/"+prefix+".json",json);File.WriteAllText(Output+"/latest.json",json);
 }
 static Button[] Buttons()=>UnityEngine.Object.FindObjectsOfType<Button>().Where(b=>b.gameObject.activeInHierarchy).OrderByDescending(b=>b.transform.position.y).ThenBy(b=>b.transform.position.x).ToArray();
 [MenuItem("GXDL/QA/UI23 Review End")]
 [MenuItem("GXDL/QA/UIStyle Review End")]
 [MenuItem("GXDL/QA/Motion Review End")]
 [MenuItem("GXDL/QA/Cinematic Review End")]
 [MenuItem("GXDL/QA/Hud Review End")]
 [MenuItem("GXDL/QA/Growth Review End")]
 public static void End(){
  if(fixture==null)return;
  try{fixture.Dispose();if(!loginReview)VerifyRecoveryAfter();fixture=null;busy=false;File.WriteAllText(Output+"/status.txt",(loginReview?"RESTORED login consent/PlayerPrefs/GameView.":"RESTORED original Rules/save/PlayerPrefs/GameView; fixture Dispose verifies original save bytes.")+" "+DateTime.UtcNow.ToString("O"));loginReview=false;}
  catch(Exception e){File.WriteAllText(Output+"/restore-error.txt",e.ToString());File.WriteAllText(Output+"/status.txt","RESTORE FAILED; recovery checkpoint retained. "+DateTime.UtcNow.ToString("O"));throw;}
 }
}

