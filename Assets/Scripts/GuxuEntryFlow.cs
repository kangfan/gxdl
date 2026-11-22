using System;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;

namespace Gxdl {
public enum GuxuEntryStage { Login, Loading, Opening, Ready, Failed }

// The loading artwork owns its presentation; the entry flow owns resource readiness.
public interface IGuxuEntryLoadingView {
 void SetProgress(float progress,string message);
 bool IsComplete {get;}
}

public partial class GxdlGame {
 public GuxuEntryStage EntryStage {get;private set;}=GuxuEntryStage.Login;
 public float EntryProgress {get;private set;}
 public string EntryMessage {get;private set;}="";
 Coroutine entryRoutine;
 IGuxuEntryLoadingView entryLoadingView;
 int entryGeneration,entryLastTick;
 string entryScope;

 // Implement this in a separate GxdlGame partial file owned by the loading UI.
 partial void CreateEntryLoadingView(RectTransform parent,ref IGuxuEntryLoadingView loadingView);
#if UNITY_EDITOR
 // Editor QA supplies a controlled view without enabling a runtime login bypass.
 Func<RectTransform,IGuxuEntryLoadingView> entryLoadingTestFactory;
#endif
 bool EntryAuthorized(string scope)=>accounts&&accounts.CanPlay&&accounts.SaveScope==scope;
 bool EntryValid(int generation)=>generation==entryGeneration&&isActiveAndEnabled&&EntryAuthorized(entryScope);

 void BeginGameEntry(string scope){
  if(!EntryAuthorized(scope)||EntryStage==GuxuEntryStage.Loading||EntryStage==GuxuEntryStage.Opening||EntryStage==GuxuEntryStage.Ready)return;
  CancelGameEntry();entryScope=scope;GameRules.SelectAccount(scope);
  accountGameOpen=false;Rules=null;auto=false;busy=true;EntryStage=GuxuEntryStage.Loading;
  entryLastTick=Time.frameCount;
  Clear();view.name="Entry Loading";
  try{
#if UNITY_EDITOR
   if(entryLoadingTestFactory!=null)entryLoadingView=entryLoadingTestFactory(view);
   else
#endif
   CreateEntryLoadingView(view,ref entryLoadingView);
   if(entryLoadingView==null)throw new InvalidOperationException("CreateEntryLoadingView has no loading UI implementation.");
   SetEntryProgress(0,"正在读取游戏配置……");
  }catch(Exception error){FailGameEntry(entryGeneration,error);return;}
  entryRoutine=StartCoroutine(RunGameEntry(entryGeneration));
 }
 void CancelGameEntry(){
  entryGeneration++;EntryStage=GuxuEntryStage.Login;
  if(entryRoutine!=null){StopCoroutine(entryRoutine);entryRoutine=null;}
  entryLoadingView=null;EntryProgress=0;EntryMessage="";
 }
 IEnumerator RunGameEntry(int generation){
  yield return null;
  using(var sequence=PrepareGameEntry(generation)){
   while(EntryValid(generation)){
    entryLastTick=Time.frameCount;
    bool more;object next;
    try{more=sequence.MoveNext();next=more?sequence.Current:null;}
    catch(Exception error){FailGameEntry(generation,error);yield break;}
    if(!more)break;
    yield return next;
   }
  }
  if(generation==entryGeneration)entryRoutine=null;
 }
 IEnumerator<object> PrepareGameEntry(int generation){
  var configRequest=Resources.LoadAsync<TextAsset>("Game/OriginalConfig");
  double deadline=Time.realtimeSinceStartupAsDouble+60;
  while(!configRequest.isDone){
   CheckEntryDeadline(deadline);SetEntryProgress(configRequest.progress*.15f,"正在读取游戏配置……");yield return null;
  }
  var configAsset=configRequest.asset as TextAsset;
  if(!configAsset)throw new InvalidOperationException("Missing Game/OriginalConfig.");
  var config=JsonConvert.DeserializeObject<OriginalConfig>(configAsset.text);
  if(config==null||config.realms==null||config.equipment==null||config.trees==null)throw new InvalidOperationException("Invalid game configuration.");
  SetEntryProgress(.15f,"正在读取角色进度……");yield return null;
  Rules=GuxuExperienceAccounts.LoadRules(config);Rules.EnsureExpansion();
#if UNITY_EDITOR
  bool playOpening=true;
#else
  bool playOpening=PlayerPrefs.GetInt(OpeningStoryKey,0)==0;
#endif
  var paths=new List<string>{
   "Game/Guxu/Stages","Game/Guxu/Final/background","Game/Guxu/Final/stone",
   "Game/Guxu/Final/UI/navigation-atlas","Game/Guxu/Final/UI/controls-atlas",
   "Game/Guxu/Final/Hero/hero","Game/Guxu/Final/Hero/unseal-parts","Game/Guxu/Final/Hero/home-hero",
   "Game/Guxu/Final/Hero/home-arm-repair","Game/Guxu/Final/Hero/attack-items","Game/Guxu/Final/Hero/attack-grip",
   "Game/Guxu/Final/Hero/seeker-atlas","Game/Guxu/Final/Hero/seeker-data","Game/Guxu/SpineHero/SkeletonGraphic"
  };
  if(playOpening)paths.AddRange(new[]{"Game/Guxu/Final/Story/opening-world","Game/Guxu/Final/Story/opening-seeker","Game/Guxu/Final/Story/first-seal"});
  SetEntryProgress(.25f,"正在加载场景与角色……");yield return null;
  for(int i=0;i<paths.Count;i++){
   var request=Resources.LoadAsync<UnityEngine.Object>(paths[i]);deadline=Time.realtimeSinceStartupAsDouble+60;
   while(!request.isDone){
    CheckEntryDeadline(deadline);SetEntryProgress(.25f+.65f*(i+request.progress)/paths.Count,"正在加载场景与角色……");yield return null;
   }
   if(!request.asset)throw new InvalidOperationException("Missing entry resource: "+paths[i]);
   SetEntryProgress(.25f+.65f*(i+1)/paths.Count,"正在加载场景与角色……");yield return null;
  }
  if(Stages==null||Stages.stages==null||Stages.stages.Length<200||!GuxuSpineAssets.Data)throw new InvalidOperationException("Scene initialization failed.");
  if(playOpening)GuxuStoryData.Validate();
  SetEntryProgress(.95f,"正在准备进入古墟……");yield return null;
  accountGameOpen=true;SetEntryProgress(1,"加载完成");
  deadline=Time.realtimeSinceStartupAsDouble+30;
  // The view can ease its bar to 100%; it cannot advance the flow before resources are ready.
  while(!entryLoadingView.IsComplete){CheckEntryDeadline(deadline);yield return null;}
  yield return null;
  if(!EntryValid(generation))yield break;
  if(playOpening){
   EntryStage=GuxuEntryStage.Opening;Clear();view.name="Entry Opening";
   bool completed=false;
   var story=PlayStory(false,()=>completed=true);
   try{while(story.MoveNext())yield return story.Current;}
   finally{(story as IDisposable)?.Dispose();}
   if(!EntryValid(generation))yield break;
   if(!completed)throw new InvalidOperationException("Opening interrupted before completion.");
  }
  EntryStage=GuxuEntryStage.Ready;busy=false;entryLoadingView=null;Home();
 }
 void SetEntryProgress(float progress,string message){
  EntryProgress=Mathf.Max(EntryProgress,Mathf.Clamp01(progress));EntryMessage=message;
  entryLoadingView.SetProgress(EntryProgress,message);
 }
 static void CheckEntryDeadline(double deadline){if(Time.realtimeSinceStartupAsDouble>deadline)throw new TimeoutException("Entry loading timed out.");}
 void MonitorGameEntry(){
  if((EntryStage!=GuxuEntryStage.Loading&&EntryStage!=GuxuEntryStage.Opening)||Time.frameCount-entryLastTick<=5||!EntryAuthorized(entryScope))return;
  if(entryRoutine!=null){StopCoroutine(entryRoutine);entryRoutine=null;}
  FailGameEntry(entryGeneration,new InvalidOperationException("Entry coroutine was interrupted."));
 }
 void FailGameEntry(int generation,Exception error){
  if(!EntryValid(generation))return;
  EntryStage=GuxuEntryStage.Failed;busy=false;entryLoadingView=null;
  Debug.LogWarning("[Entry] "+error.GetType().Name+": "+error.Message);
  Clear();view.name="Entry Failure";
  Img("",view,0,0,750,1334,new Color(.025f,.055f,.075f,1));
  Txt("暂时无法进入古墟",view,65,430,620,65,34,gold);
  Txt("资源加载未完成，请重试。",view,65,510,620,65,26,cream);
  Btn("重新加载",view,160,640,430,75,RetryGameEntry).name="Entry Retry";
  Btn("返回登录",view,160,750,430,75,()=>accounts.Logout()).name="Entry Back";
 }
 public void RetryGameEntry(){if(EntryStage==GuxuEntryStage.Failed&&EntryAuthorized(entryScope))BeginGameEntry(entryScope);}
}
}
