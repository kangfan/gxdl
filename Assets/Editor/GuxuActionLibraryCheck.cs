using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Gxdl;
using Newtonsoft.Json;
using Spine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Object=UnityEngine.Object;

// Editor-only: preview never calls game actions or changes combat/reward configuration.
[InitializeOnLoad]
public static class GuxuActionLibraryCheck {
 const string Prefix="GXDL.ActionLibrary.", Root="Logs/action-library", Receipt=Root+"/check.txt";
 public static readonly string[] Actions={"library_idle","library_walk","library_run","library_cast_start","library_cast_channel","library_seal_burst","library_hit","library_knockdown"};
 public static readonly string[] ActionLabels={"待机","行走","跑步","起手施法","持续施法","破封大招","受击后退","倒地起身"};
 public static bool Active=>SessionState.GetBool(Prefix+"active",false);
 public static bool Recording=>Active&&SessionState.GetBool(Prefix+"record",false);
 public static GuxuActionLibraryRunner Runner {get;private set;}
 static bool starting;
 static readonly string[] SaveSuffixes={"",".bak",".tmp",".invalid",".before-equipment15"};
 static string[] PrefKeys=>new[]{GxdlGame.OpeningStoryKey,GxdlGame.FirstUnsealStoryKey};
 static GuxuActionLibraryCheck(){EditorApplication.update+=Monitor;EditorApplication.playModeStateChanged+=OnMode;}
 [MenuItem("Guxu/QA/Record Action Library")]
 public static void Record()=>Begin(true);
 [MenuItem("Guxu/Preview/Action Library")]
 public static void Preview(){GuxuActionLibraryWindow.Open();if(!Active)Begin(false);}
 public static bool IsLoop(string name)=>name=="library_idle"||name=="library_walk"||name=="library_run"||name=="library_cast_channel";
 static void Begin(bool record){
  if(Active)throw new InvalidOperationException("Finish the existing action preview first.");
  if(SessionState.GetBool(Prefix+"backup",false))Restore();
  if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Stop Play Mode before starting the action library.");
  for(int i=0;i<UnityEngine.SceneManagement.SceneManager.sceneCount;i++)if(UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty)throw new InvalidOperationException("Save scene changes before starting the action library.");
  Directory.CreateDirectory(Root);File.WriteAllText(Receipt,(record?"RUNNING QA ":"PREVIEW ")+DateTime.Now.ToString("O"));
  try{GuxuSpellVfx.ValidateContract();ValidateClips();}catch(Exception e){File.WriteAllText(Receipt,"FAIL "+e);throw;}
  Backup();SessionState.SetBool(Prefix+"active",true);SessionState.SetBool(Prefix+"record",record);SessionState.SetFloat(Prefix+"deadline",(float)EditorApplication.timeSinceStartup+600);
  try{EditorSceneManager.OpenScene("Assets/Scenes/GxdlMain.unity");GuxuAccountChecks.PreviewGameplay();}catch{Restore();throw;}
 }
 static void Backup(){
  SessionState.SetString(Prefix+"scenes",JsonConvert.SerializeObject(EditorSceneManager.GetSceneManagerSetup()));
  SessionState.SetString(Prefix+"savePath",GameRules.SavePath);
  foreach(string suffix in SaveSuffixes){string path=GameRules.SavePath+suffix;SessionState.SetBool(Prefix+"exists"+suffix,File.Exists(path));if(File.Exists(path))SessionState.SetString(Prefix+"bytes"+suffix,Convert.ToBase64String(File.ReadAllBytes(path)));}
  foreach(string key in PrefKeys){SessionState.SetBool(Prefix+"prefExists"+key,PlayerPrefs.HasKey(key));SessionState.SetInt(Prefix+"pref"+key,PlayerPrefs.GetInt(key));}
  SessionState.SetInt(Prefix+"capture",Time.captureFramerate);SessionState.SetFloat(Prefix+"timescale",Time.timeScale);SessionState.SetBool(Prefix+"backup",true);
  foreach(string key in PrefKeys)PlayerPrefs.SetInt(key,1);PlayerPrefs.Save();
 }
 static void Monitor(){
  if(!Active)return;
  if(!starting&&EditorApplication.isPlaying&&GxdlGame.Instance){
   starting=true;
   try{Runner=new GameObject("Action Library Preview Runner").AddComponent<GuxuActionLibraryRunner>();Runner.Initialize();if(Recording)Runner.StartCoroutine(Guard(Runner.RecordAll()));}
   catch(Exception e){Fail(e);}
  }
  if((Recording||!starting)&&EditorApplication.timeSinceStartup>SessionState.GetFloat(Prefix+"deadline",float.MaxValue))Fail(new TimeoutException("Action-library startup/recording timed out."));
 }
 static IEnumerator Guard(IEnumerator test){
  var stack=new Stack<IEnumerator>();stack.Push(test);bool failed=false;
  while(stack.Count>0){object value=null;bool moved=false;try{moved=stack.Peek().MoveNext();if(moved)value=stack.Peek().Current;}catch(Exception e){failed=true;Fail(e);}
   if(failed)break;if(!moved){(stack.Pop() as IDisposable)?.Dispose();continue;}
   if(value is IEnumerator nested){stack.Push(nested);continue;}yield return value;
  }
  while(stack.Count>0)(stack.Pop() as IDisposable)?.Dispose();if(!failed)Stop();
 }
 static void Fail(Exception e){File.WriteAllText(Receipt,"FAIL "+e);Debug.LogException(e);Stop();}
 public static void Stop(){if(Recording&&File.Exists(Receipt)&&File.ReadAllText(Receipt).StartsWith("RUNNING"))File.WriteAllText(Receipt,"CANCELLED before the complete recording passed.");if(Runner)Runner.StopPreview();if(EditorApplication.isPlayingOrWillChangePlaymode)EditorApplication.isPlaying=false;else Restore();}
 static void OnMode(PlayModeStateChange state){if(state==PlayModeStateChange.ExitingPlayMode&&Runner)Runner.StopPreview();if(state==PlayModeStateChange.EnteredEditMode&&SessionState.GetBool(Prefix+"backup",false))Restore();}
 static void Restore(){
  if(!SessionState.GetBool(Prefix+"backup",false))return;
  try{
   string save=SessionState.GetString(Prefix+"savePath",GameRules.SavePath);
   foreach(string suffix in SaveSuffixes){string path=save+suffix;if(SessionState.GetBool(Prefix+"exists"+suffix,false)){Directory.CreateDirectory(Path.GetDirectoryName(path));File.WriteAllBytes(path,Convert.FromBase64String(SessionState.GetString(Prefix+"bytes"+suffix,"")));}else if(File.Exists(path))File.Delete(path);}
   foreach(string key in PrefKeys){if(SessionState.GetBool(Prefix+"prefExists"+key,false))PlayerPrefs.SetInt(key,SessionState.GetInt(Prefix+"pref"+key,0));else PlayerPrefs.DeleteKey(key);}PlayerPrefs.Save();
   Time.captureFramerate=SessionState.GetInt(Prefix+"capture",0);Time.timeScale=SessionState.GetFloat(Prefix+"timescale",1);
   var scenes=JsonConvert.DeserializeObject<SceneSetup[]>(SessionState.GetString(Prefix+"scenes","[]"));if(scenes!=null&&scenes.Length>0)EditorSceneManager.RestoreSceneManagerSetup(scenes);
   File.AppendAllText(Receipt,"\nRESTORED original save bytes, save sidecars, story preferences and scene setup.");SessionState.SetBool(Prefix+"backup",false);
  }finally{SessionState.SetBool(Prefix+"active",false);starting=false;Runner=null;}
 }
 public static void Require(bool ok,string message){if(!ok)throw new InvalidOperationException("Action library: "+message);}
 public static void PrepareFrames(string folder){
  string full=Path.GetFullPath(folder),allowed=Path.GetFullPath(Root)+Path.DirectorySeparatorChar;Require(full.StartsWith(allowed,StringComparison.OrdinalIgnoreCase),"frame directory outside action-library output");Directory.CreateDirectory(full);
  foreach(string file in Directory.GetFiles(full,"????.png"))if(int.TryParse(Path.GetFileNameWithoutExtension(file),out _))File.Delete(file);
 }
 static bool Finite(float n)=>!float.IsNaN(n)&&!float.IsInfinity(n);
 static void ValidateClips(){
  var data=GuxuSpineAssets.Data.GetSkeletonData(false);var result=new List<object>();
  foreach(string name in Actions){var clip=data.FindAnimation(name);Require(clip!=null&&clip.Duration>0,"missing clip "+name);float greatestMotion=0,bodyMotion=0;
   for(int speed=1;speed<=3;speed++){
    var skeleton=new Skeleton(data);var state=new Spine.AnimationState(GuxuSpineAssets.Data.GetAnimationStateData());var entry=state.SetAnimation(0,name,IsLoop(name));entry.TimeScale=speed;state.Apply(skeleton);skeleton.UpdateWorldTransform(Skeleton.Physics.Update);
    var initial=skeleton.Bones.Select(b=>new Vector3(b.WorldX,b.WorldY,b.WorldRotationX)).ToArray();
    for(int f=0;f<48;f++){state.Update(clip.Duration/(48*speed));state.Apply(skeleton);skeleton.UpdateWorldTransform(Skeleton.Physics.Update);
     for(int b=0;b<skeleton.Bones.Count;b++){var bone=skeleton.Bones.Items[b];Require(Finite(bone.WorldX)&&Finite(bone.WorldY)&&Finite(bone.WorldRotationX),"finite bone "+name);float distance=Vector3.Distance(initial[b],new Vector3(bone.WorldX,bone.WorldY,bone.WorldRotationX));greatestMotion=Mathf.Max(greatestMotion,distance);if(bone.Data.Name=="pelvis"||bone.Data.Name=="chest"||bone.Data.Name.StartsWith("upper")||bone.Data.Name.StartsWith("fore")||bone.Data.Name.StartsWith("foot"))bodyMotion=Mathf.Max(bodyMotion,distance);}
     foreach(var slot in skeleton.Slots)if(slot.Attachment is MeshAttachment mesh){var points=new float[mesh.WorldVerticesLength];mesh.ComputeWorldVertices(slot,0,points.Length,points,0,2);Require(points.All(Finite),"finite mesh "+name);}
    }
    state.SetAnimation(0,"library_idle",true);state.Update(.12f);state.Apply(skeleton);Require(state.GetCurrent(0).Animation.Name=="library_idle","interrupt "+name);
   }
   Require(greatestMotion>.05f,"static skeleton "+name);if(name!="library_idle")Require(bodyMotion>.05f,"only hair/cloth moves in body action "+name);result.Add(new{clip=name,duration=clip.Duration,loop=IsLoop(name),maximumBoneMotion=greatestMotion,maximumBodyMotion=bodyMotion,speeds=new[]{1,2,3}});
  }
  Require(Mathf.Abs(data.FindAnimation("library_seal_burst").Duration-1.2f)<.002f,"reference composite duration must be 1.2 seconds");
  File.WriteAllText(Root+"/spine-contract.json",JsonConvert.SerializeObject(result,Formatting.Indented));
 }
}

public sealed class GuxuActionLibraryRunner:MonoBehaviour {
 public static readonly string[] EffectLabels={"E1 能量汇聚","E2 法阵生成","E3 锁链封印","E4 光柱冲天","E5 流云光带","E6 灵蝶","E7 碎石光屑","E8 冲击波"};
 const string Output="Logs/action-library";
 GuxuCharacterMotion actor;GuxuSpellVfx effects;RectTransform root,sealTarget;Text caption;GameObject previewCanvas;Camera captureCamera;RenderTexture target,previousTarget;Texture2D pixels;Sprite backdrop;
 string originalRules;bool stopped;GuxuTransparentLibraryCapture transparent;readonly List<object> manifest=new List<object>();
 public void Initialize(){
  var game=GxdlGame.Instance;originalRules=JsonConvert.SerializeObject(game.Rules.Save);game.StopAllCoroutines();game.enabled=false;
  foreach(var sound in game.GetComponents<AudioSource>())sound.Stop();
  foreach(var canvas in Object.FindObjectsOfType<Canvas>())if(canvas.isRootCanvas)canvas.gameObject.SetActive(false);
  captureCamera=Camera.main;GuxuActionLibraryCheck.Require(captureCamera,"main scene captureCamera");previousTarget=captureCamera.targetTexture;
  previewCanvas=new GameObject("Action Library Preview Canvas",typeof(Canvas),typeof(CanvasScaler));var c=previewCanvas.GetComponent<Canvas>();c.renderMode=RenderMode.ScreenSpaceCamera;c.worldCamera=captureCamera;c.planeDistance=9;c.sortingOrder=100;
  var scaler=previewCanvas.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(750,1334);scaler.matchWidthOrHeight=.5f;root=(RectTransform)previewCanvas.transform;
  var texture=Resources.Load<Texture2D>("Game/Guxu/Final/background");GuxuActionLibraryCheck.Require(texture,"approved background");backdrop=Sprite.Create(texture,new Rect(0,0,texture.width,texture.height),new Vector2(.5f,.5f),100);
  var image=Rect("Approved Landscape",0,0,750,1334).gameObject.AddComponent<Image>();image.sprite=backdrop;image.raycastTarget=false;
  var shade=Rect("Caption Panel",24,28,702,94).gameObject.AddComponent<Image>();shade.color=new Color(.025f,.075f,.11f,.85f);shade.raycastTarget=false;
  caption=Rect("Action Label",36,34,678,82).gameObject.AddComponent<Text>();caption.font=Resources.Load<Font>("Original/GameFont");caption.fontSize=28;caption.alignment=TextAnchor.MiddleCenter;caption.color=new Color(1,.89f,.62f);caption.raycastTarget=false;
  var hero=Rect("Library Hero",185,400,380,540);hero.gameObject.AddComponent<Image>().raycastTarget=false;actor=hero.gameObject.AddComponent<GuxuCharacterMotion>();actor.AttackTravel=0;actor.UseHomePose=false;
  InitializeEffects();PlayAction("library_idle",1,true);Canvas.ForceUpdateCanvases();
 }
 RectTransform Rect(string name,float x,float y,float w,float h){var obj=new GameObject(name,typeof(RectTransform));var rt=(RectTransform)obj.transform;rt.SetParent(root,false);rt.anchorMin=rt.anchorMax=new Vector2(0,1);rt.pivot=new Vector2(0,1);rt.anchoredPosition=new Vector2(x,-y);rt.sizeDelta=new Vector2(w,h);return rt;}
 public void PlayAction(string name,float speed,bool loop){CancelEffects();actor.Graphic.color=Color.white;actor.PlayLibraryAction(name,speed,loop);actor.Graphic.Update(0);actor.Graphic.UpdateMesh();caption.text=GuxuActionLibraryCheck.ActionLabels[Array.IndexOf(GuxuActionLibraryCheck.Actions,name)]+"  ×"+speed.ToString("0.##")+(loop?"  循环":"");}
 public void ResetPreview()=>PlayAction("library_idle",1,true);
 void InitializeEffects(){sealTarget=Rect("Seal Target Anchor",500,840,0,0);effects=root.gameObject.AddComponent<GuxuSpellVfx>();effects.Bind(root);}
 void CancelEffects(){if(effects)effects.Cancel();}
 public void PlayEffect(int index,float speed){
  ResetPreview();actor.Graphic.color=new Color(1,1,1,0);effects.PlayEffect("E"+(index+1),actor,sealTarget.position,speed,index==0||index==4);caption.text=EffectLabels[index]+"  ×"+speed.ToString("0.##");
 }
 public void PlayComposite(float speed){PlayAction("library_seal_burst",speed,false);effects.PlayBurst(actor,sealTarget.position,speed);caption.text="破封大招 · 动作与特效合成  ×"+speed.ToString("0.##");}
 void Advance(float delta){actor.Graphic.timeScale=1;actor.Graphic.Update(delta);actor.Graphic.timeScale=0;actor.Graphic.UpdateMesh();effects.ManualUpdate(delta);Canvas.ForceUpdateCanvases();}
 static float PoseFingerprint(GuxuCharacterMotion motion){float sum=0;int index=0;foreach(var bone in motion.Graphic.Skeleton.Bones){index++;sum+=(bone.WorldX*.31f+bone.WorldY*.17f+bone.WorldRotationX)*index;}return sum;}
 IEnumerator Frame(string folder,int frame){Canvas.ForceUpdateCanvases();CheckMeshes(root);Capture(folder,frame);yield return null;Advance(1/30f);}
 public IEnumerator RecordAll(){
  BeginCapture();actor.Graphic.timeScale=0;effects.enabled=false;yield return null;
  foreach(string name in GuxuActionLibraryCheck.Actions){
   float duration=GuxuSpineAssets.Data.GetSkeletonData(false).FindAnimation(name).Duration;bool loop=GuxuActionLibraryCheck.IsLoop(name);int count=Mathf.CeilToInt(duration*30)*(loop?3:1);string folder=Path.Combine(Output,"actions",name);GuxuActionLibraryCheck.PrepareFrames(folder);
   PlayAction(name,1,loop);float initial=PoseFingerprint(actor),movement=0;int before=actor.MeshBuildCount;
   for(int frame=0;frame<count;frame++){movement=Mathf.Max(movement,Mathf.Abs(PoseFingerprint(actor)-initial));yield return Frame(folder,frame);}
   GuxuActionLibraryCheck.Require(actor.MeshBuildCount>before&&movement>.01f,"actual animated Spine mesh "+name);
   manifest.Add(new{kind="action",name,duration,loop,frames=count,path=folder});
   actor.PlayLibraryAction(name,3,loop);Advance(Mathf.Min(.03f,duration/6));actor.PlayLibraryAction("library_hit",2,false);Advance(.03f);GuxuActionLibraryCheck.Require(actor.Graphic.AnimationState.GetCurrent(0).Animation.Name=="library_hit","live action interruption "+name);
   ResetPreview();Advance(.05f);
  }
  for(int i=0;i<8;i++){
   string key="E"+(i+1),folder=Path.Combine(Output,"effects",key);GuxuActionLibraryCheck.PrepareFrames(folder);PlayEffect(i,1);float duration=effects.Duration;bool loop=i==0||i==4;int count=Mathf.CeilToInt(duration*30)*(loop?3:1);bool visible=false;int maximumVertices=0;
   for(int frame=0;frame<count;frame++){Canvas.ForceUpdateCanvases();maximumVertices=Mathf.Max(maximumVertices,effects.CurrentVertexCount);visible|=effects.CurrentVertexCount>0;yield return Frame(folder,frame);}
   GuxuActionLibraryCheck.Require(visible,"visible rendered effect "+key);manifest.Add(new{kind="effect",name=key,duration,loop,frames=count,maximumVertices,path=folder});
   if(loop)effects.Cancel();else for(int f=0;f<12;f++){Advance(1/30f);yield return null;}
   GuxuActionLibraryCheck.Require(!effects.IsPlaying&&effects.ActiveVisualCount==0,"effect completion cleanup "+key);
   effects.PlayEffect(key,actor,sealTarget.position,3,false);bool spedVisible=false;
   for(int f=0;f<Mathf.CeilToInt(duration*10)+8;f++){Advance(1/30f);spedVisible|=effects.CurrentVertexCount>0;CheckMeshes(root);yield return null;}
   GuxuActionLibraryCheck.Require(spedVisible&&!effects.IsPlaying&&effects.ActiveVisualCount==0,"accelerated effect cleanup "+key);
   effects.PlayEffect(key,actor,sealTarget.position,1,true);Advance(.05f);effects.Cancel();yield return null;GuxuActionLibraryCheck.Require(!effects.IsPlaying&&effects.ActiveVisualCount==0,"effect interruption "+key);
  }
  string composite=Path.Combine(Output,"composite"),tail=Path.Combine(Output,"composite-tail");GuxuActionLibraryCheck.PrepareFrames(composite);GuxuActionLibraryCheck.PrepareFrames(tail);PlayComposite(1);bool combinedVisible=false;
  for(int frame=0;frame<36;frame++){combinedVisible|=effects.CurrentVertexCount>0;yield return Frame(composite,frame);}
  for(int frame=0;frame<6;frame++)yield return Frame(tail,frame);
  for(int f=0;f<6;f++){Advance(1/30f);yield return null;}
  GuxuActionLibraryCheck.Require(combinedVisible&&!effects.IsPlaying&&effects.ActiveVisualCount==0,"composite visible and tail cleaned");
  manifest.Add(new{kind="composite",name="library_seal_burst",duration=1.2f,frames=36,tailFrames=6,path=composite,tailPath=tail});
  PlayComposite(3);for(int f=0;f<22;f++){Advance(1/30f);CheckMeshes(root);yield return null;}GuxuActionLibraryCheck.Require(!effects.IsPlaying&&effects.ActiveVisualCount==0,"accelerated composite cleanup");
  PlayComposite(1);Advance(.3f);ResetPreview();yield return null;GuxuActionLibraryCheck.Require(!effects.IsPlaying&&effects.ActiveVisualCount==0,"composite cancellation");
  transparent=new GuxuTransparentLibraryCapture();yield return transparent.ExportAll(manifest);transparent.Dispose();transparent=null;
  GuxuActionLibraryCheck.Require(JsonConvert.SerializeObject(GxdlGame.Instance.Rules.Save)==originalRules,"preview must not change game data");WriteManifest();
  File.WriteAllText(Output+"/check.txt","PASS "+DateTime.Now.ToString("O")+"\n8 Spine clips: finite bones and meshes at 1x/2x/3x, visible live motion, accelerated interruption. 8 VFX: rendered finite vertices, 3x playback, completion and explicit cancellation cleanup. 1.2-second composite: 36 frames plus 0.2-second tail, acceleration and interruption. Separate RGBA action/effect cycles: fixed action pivot/baseline, full-lifetime bounds, transparent borders, visible alpha, no edge clipping. Game data unchanged. Original persisted state will be restored on leaving Play Mode.");
 }
 void BeginCapture(){Time.timeScale=1;Time.captureFramerate=30;previousTarget=captureCamera.targetTexture;target=new RenderTexture(750,1334,24);target.Create();captureCamera.targetTexture=target;pixels=new Texture2D(750,1334,TextureFormat.RGB24,false);}
 void Capture(string folder,int frame){Canvas.ForceUpdateCanvases();var before=RenderTexture.active;try{captureCamera.Render();RenderTexture.active=target;pixels.ReadPixels(new Rect(0,0,750,1334),0,0);pixels.Apply(false);File.WriteAllBytes(Path.Combine(folder,frame.ToString("D4")+".png"),pixels.EncodeToPNG());}finally{RenderTexture.active=before;}}
 static void CheckMeshes(Transform parent){foreach(var renderer in parent.GetComponentsInChildren<CanvasRenderer>()){var mesh=renderer.GetMesh();if(mesh==null)continue;foreach(var point in mesh.vertices)GuxuActionLibraryCheck.Require(!float.IsNaN(point.x)&&!float.IsNaN(point.y)&&!float.IsInfinity(point.x)&&!float.IsInfinity(point.y),"finite actual Canvas mesh "+renderer.name);}}
 void WriteManifest(){File.WriteAllText(Output+"/manifest.json",JsonConvert.SerializeObject(new{generatedAt=DateTime.Now.ToString("O"),width=750,height=1334,framesPerSecond=30,recordings=manifest},Formatting.Indented));}
 public void StopPreview(){if(stopped)return;stopped=true;StopAllCoroutines();CancelEffects();transparent?.Dispose();transparent=null;if(captureCamera)captureCamera.targetTexture=previousTarget;if(target){target.Release();Object.Destroy(target);}if(pixels)Object.Destroy(pixels);if(backdrop)Object.Destroy(backdrop);if(previewCanvas)Object.Destroy(previewCanvas);}
 void OnDestroy()=>StopPreview();
}

// An isolated world-space canvas avoids resizing the portrait preview between captures.
// Both source materials blend premultiplied alpha. Readback converts it to straight-alpha PNG.
sealed class GuxuTransparentLibraryCapture:IDisposable {
 const string Output="Logs/action-library/transparent";
 readonly GameObject sceneRoot;readonly RectTransform canvasRoot;readonly Camera exportCamera;readonly GuxuCharacterMotion motion;readonly GuxuSpellVfx effects;readonly Material effectMaterial;readonly int layer;
 RenderTexture target;Texture2D pixels;int width,height;bool disposed;
 public GuxuTransparentLibraryCapture(){
  int used=0;foreach(var t in Object.FindObjectsOfType<Transform>())used|=1<<t.gameObject.layer;
  layer=-1;for(int i=31;i>=8;i--)if((used&(1<<i))==0){layer=i;break;}GuxuActionLibraryCheck.Require(layer>=8,"an unused render layer for isolated export");
  sceneRoot=new GameObject("Transparent Action Library Export");sceneRoot.transform.position=new Vector3(10000,10000,0);
  var cameraObject=new GameObject("Transparent Export Camera",typeof(Camera));cameraObject.transform.SetParent(sceneRoot.transform,false);exportCamera=cameraObject.GetComponent<Camera>();exportCamera.enabled=false;exportCamera.orthographic=true;exportCamera.clearFlags=CameraClearFlags.SolidColor;exportCamera.backgroundColor=Color.clear;exportCamera.nearClipPlane=.1f;exportCamera.farClipPlane=100;exportCamera.cullingMask=1<<layer;exportCamera.allowHDR=false;exportCamera.allowMSAA=false;exportCamera.useOcclusionCulling=false;
  var canvasObject=new GameObject("Transparent Export Canvas",typeof(RectTransform),typeof(Canvas));canvasRoot=(RectTransform)canvasObject.transform;canvasRoot.SetParent(sceneRoot.transform,false);canvasRoot.sizeDelta=new Vector2(1024,1024);var canvas=canvasObject.GetComponent<Canvas>();canvas.renderMode=RenderMode.WorldSpace;canvas.worldCamera=exportCamera;
  var actorObject=new GameObject("Transparent Export Hero",typeof(RectTransform),typeof(Image));var rect=(RectTransform)actorObject.transform;rect.SetParent(canvasRoot,false);rect.anchorMin=rect.anchorMax=Vector2.one*.5f;rect.pivot=new Vector2(.5f,0);rect.sizeDelta=new Vector2(506.8f,506.8f);rect.anchoredPosition=Vector2.zero;actorObject.GetComponent<Image>().raycastTarget=false;motion=actorObject.AddComponent<GuxuCharacterMotion>();motion.AttackTravel=0;motion.UseHomePose=false;motion.Graphic.timeScale=0;
  effects=canvasObject.AddComponent<GuxuSpellVfx>();effects.Bind(canvasRoot);effects.enabled=false;
  // UI's ordinary straight-alpha blend squares destination alpha. This existing
  // PMA UI shader premultiplies procedural vertex colors and preserves true coverage.
  effectMaterial=new Material(GuxuSpineAssets.Material);effectMaterial.name="Transparent Export PMA VFX";effectMaterial.SetFloat("_CanvasGroupCompatible",1);effectMaterial.EnableKeyword("_CANVAS_GROUP_COMPATIBLE");
  SetSize(512,512);
 }
 void SetSize(int w,int h){
  width=w;height=h;if(target){exportCamera.targetTexture=null;target.Release();Object.Destroy(target);}if(pixels)Object.Destroy(pixels);
  target=new RenderTexture(w,h,24,RenderTextureFormat.ARGB32,RenderTextureReadWrite.Linear){antiAliasing=1};target.Create();exportCamera.targetTexture=target;exportCamera.aspect=(float)w/h;pixels=new Texture2D(w,h,TextureFormat.RGBA32,false,true);
 }
 void SetAction(string name,float time){
  var graphic=motion.Graphic;graphic.AnimationState.ClearTracks();graphic.Skeleton.SetToSetupPose();var clip=graphic.Skeleton.Data.FindAnimation(name);clip.Apply(graphic.Skeleton,0,time,false,null,1,MixBlend.Replace,MixDirection.In);graphic.Skeleton.UpdateWorldTransform(Skeleton.Physics.Update);graphic.UpdateMesh();Prepare();
 }
 void Prepare(){
  foreach(var child in canvasRoot.GetComponentsInChildren<Transform>(true))child.gameObject.layer=layer;
  foreach(var visual in canvasRoot.GetComponentsInChildren<GuxuSpellLayer>())if(visual.material!=effectMaterial)visual.material=effectMaterial;
  Canvas.ForceUpdateCanvases();
 }
 bool Accumulate(ref Bounds bounds,ref bool hasBounds){
  bool found=false;Prepare();foreach(var renderer in canvasRoot.GetComponentsInChildren<CanvasRenderer>()){
   var mesh=renderer.GetMesh();if(mesh==null||mesh.vertexCount==0)continue;
   foreach(var v in mesh.vertices){var point=canvasRoot.InverseTransformPoint(renderer.transform.TransformPoint(v));GuxuActionLibraryCheck.Require(!float.IsNaN(point.x)&&!float.IsNaN(point.y)&&!float.IsInfinity(point.x)&&!float.IsInfinity(point.y),"finite transparent-export vertex");if(!hasBounds){bounds=new Bounds(point,Vector3.zero);hasBounds=true;}else bounds.Encapsulate(point);found=true;}
  }return found;
 }
 void Fit(Bounds bounds){
  float aspect=(float)width/height,vertical=Mathf.Max(bounds.size.y/(1-32f/height),bounds.size.x/(aspect*(1-32f/width)));
  exportCamera.orthographicSize=Mathf.Max(1,vertical*.5f);exportCamera.transform.position=canvasRoot.TransformPoint(new Vector3(bounds.center.x,bounds.center.y,-10));exportCamera.transform.rotation=canvasRoot.rotation;
 }
 Vector2 Pivot(Vector3 world){var p=exportCamera.WorldToViewportPoint(world);return new Vector2(p.x,p.y);}
 int Capture(string folder,int frame){
  Prepare();var previous=RenderTexture.active;try{
   exportCamera.Render();RenderTexture.active=target;pixels.ReadPixels(new Rect(0,0,width,height),0,0);pixels.Apply(false);var colors=pixels.GetPixels();int visible=0,transparent=0;
   bool linear=QualitySettings.activeColorSpace==ColorSpace.Linear;
   for(int i=0;i<colors.Length;i++){
    Color value=colors[i];int x=i%width,y=i/width;
    GuxuActionLibraryCheck.Require((x>0&&y>0&&x<width-1&&y<height-1)||value.a<=1/255f,"transparent sequence clips a visible edge: "+folder+" frame "+frame);
    if(value.a<=1/255f){colors[i]=Color.clear;transparent++;continue;}
    visible++;value.r=Mathf.Clamp01(value.r/value.a);value.g=Mathf.Clamp01(value.g/value.a);value.b=Mathf.Clamp01(value.b/value.a);if(linear)value=value.gamma;colors[i]=value;
   }
   GuxuActionLibraryCheck.Require(transparent>0,"transparent capture has opaque background");pixels.SetPixels(colors);pixels.Apply(false);File.WriteAllBytes(Path.Combine(folder,frame.ToString("D4")+".png"),pixels.EncodeToPNG());return visible;
  }finally{RenderTexture.active=previous;}
 }
 public IEnumerator ExportAll(List<object> manifest){
  // One shared view fits every pose of every action, so feet/pivot/scale never swim.
  Bounds actionBounds=new Bounds();bool hasBounds=false;
  foreach(string name in GuxuActionLibraryCheck.Actions){float duration=motion.Graphic.Skeleton.Data.FindAnimation(name).Duration;int samples=Mathf.CeilToInt(duration*60);for(int f=0;f<=samples;f++){SetAction(name,Mathf.Min(duration,f/60f));Accumulate(ref actionBounds,ref hasBounds);}yield return null;}
  GuxuActionLibraryCheck.Require(hasBounds,"transparent action bounds");Fit(actionBounds);SetAction("library_idle",0);
  Vector2 pivot=Pivot(motion.Graphic.transform.TransformPoint(Vector3.zero));var foot=motion.Graphic.Skeleton.FindBone("footL");float baseline=Pivot(motion.Graphic.transform.TransformPoint(new Vector3(foot.WorldX,foot.WorldY,0)*motion.Graphic.MeshScale)).y*height;
  foreach(string name in GuxuActionLibraryCheck.Actions){
   float duration=motion.Graphic.Skeleton.Data.FindAnimation(name).Duration;int count=Mathf.CeilToInt(duration*30);string folder=Path.Combine(Output,"actions",name);GuxuActionLibraryCheck.PrepareFrames(folder);int visible=0;
   for(int frame=0;frame<count;frame++){SetAction(name,frame/30f);visible+=Capture(folder,frame);yield return null;}
   GuxuActionLibraryCheck.Require(visible>0,"empty transparent action "+name);manifest.Add(new{kind="transparent-action",name,width,height,frames=count,duration,cycles=1,framesPerSecond=30,alpha="straight RGBA",pivot=new[]{pivot.x,pivot.y},baselinePixels=baseline,pixelsPerWorldUnit=height/(exportCamera.orthographicSize*2),path=folder});
  }
  motion.gameObject.SetActive(false);
  for(int index=0;index<8;index++){
   string key="E"+(index+1),folder=Path.Combine(Output,"effects",key);GuxuActionLibraryCheck.PrepareFrames(folder);SetSize(512,index==3?1024:512);effects.PlayEffect(key,null,canvasRoot.position,1,false);float duration=effects.Duration;int samples=Mathf.CeilToInt(duration*60);Bounds bounds=new Bounds();bool present=false;
   for(int f=0;f<samples;f++){Accumulate(ref bounds,ref present);effects.ManualUpdate(1/60f);}effects.Cancel();GuxuActionLibraryCheck.Require(present,"transparent VFX bounds "+key);Fit(bounds);pivot=Pivot(canvasRoot.position);
   effects.PlayEffect(key,null,canvasRoot.position,1,false);int count=Mathf.CeilToInt(duration*30),visible=0;
   for(int frame=0;frame<count;frame++){visible+=Capture(folder,frame);effects.ManualUpdate(1/30f);yield return null;}
   effects.Cancel();GuxuActionLibraryCheck.Require(visible>0&&effects.ActiveVisualCount==0,"transparent VFX visible and cleaned "+key);manifest.Add(new{kind="transparent-effect",name=key,width,height,frames=count,duration,cycles=1,framesPerSecond=30,alpha="straight RGBA",pivot=new[]{pivot.x,pivot.y},pixelsPerWorldUnit=height/(exportCamera.orthographicSize*2),path=folder});
  }
 }
 public void Dispose(){if(disposed)return;disposed=true;if(effects)effects.Cancel();if(exportCamera)exportCamera.targetTexture=null;if(target){target.Release();Object.Destroy(target);}if(pixels)Object.Destroy(pixels);if(effectMaterial)Object.Destroy(effectMaterial);if(sceneRoot)Object.Destroy(sceneRoot);}
}

public sealed class GuxuActionLibraryWindow:EditorWindow {
 int selectedAction,selectedEffect;float speed=1;bool loop=true;
 public static void Open(){var w=GetWindow<GuxuActionLibraryWindow>("古墟 · 动作与特效");w.minSize=new Vector2(340,330);w.Show();}
 void OnInspectorUpdate()=>Repaint();
 void OnGUI(){
  EditorGUILayout.LabelField("8 个骨骼动作 · 8 个独立特效",EditorStyles.boldLabel);
  EditorGUILayout.HelpBox("播放在 Game 视图中显示。退出预览后恢复原存档与场景。",MessageType.Info);
  if(!GuxuActionLibraryCheck.Active){if(GUILayout.Button("进入运行预览"))GuxuActionLibraryCheck.Preview();if(GUILayout.Button("录制全部并检查"))GuxuActionLibraryCheck.Record();return;}
  if(GuxuActionLibraryCheck.Recording){EditorGUILayout.LabelField("正在录制，请等待检查完成。");if(GUILayout.Button("中止并恢复"))GuxuActionLibraryCheck.Stop();return;}
  var runner=GuxuActionLibraryCheck.Runner;
  using(new EditorGUI.DisabledScope(!runner)){
   int next=EditorGUILayout.Popup("动作",selectedAction,GuxuActionLibraryCheck.ActionLabels);if(next!=selectedAction){selectedAction=next;loop=GuxuActionLibraryCheck.IsLoop(GuxuActionLibraryCheck.Actions[next]);}
   speed=EditorGUILayout.Slider("播放速度",speed,.25f,3);loop=EditorGUILayout.Toggle("动作循环",loop);
   if(GUILayout.Button("播放所选动作"))runner.PlayAction(GuxuActionLibraryCheck.Actions[selectedAction],speed,loop);
   selectedEffect=EditorGUILayout.Popup("独立特效",selectedEffect,GuxuActionLibraryRunner.EffectLabels);
   if(GUILayout.Button("播放所选特效"))runner.PlayEffect(selectedEffect,speed);
   if(GUILayout.Button("播放 1.2 秒大招合成"))runner.PlayComposite(speed);
   if(GUILayout.Button("打断并回到待机"))runner.ResetPreview();
  }
  if(GUILayout.Button("结束预览并恢复"))GuxuActionLibraryCheck.Stop();
 }
}


