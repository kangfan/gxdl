using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Gxdl;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public static class GuxuItemIconCheck {
 const string Output="Reference/IconRedesign125-20261116-r01/technical/runtime";
 static int assertions;
 static bool running;
 static readonly BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
 static void Require(bool value,string message){assertions++;if(!value)throw new Exception("Item icon QA: "+message);}
 [MenuItem("GXDL/Art/Verify Item Icons")]
 public static void Verify(){
  assertions=0;Directory.CreateDirectory(Output);
  AssetDatabase.ImportAsset("Assets/Resources/"+GuxuItemIcons.Path+".png",ImportAssetOptions.ForceUpdate|ImportAssetOptions.ForceSynchronousImport);
  var icons=Resources.LoadAll<Sprite>(GuxuItemIcons.Path);Require(icons.Length==90,"90 imported sub-sprites including compatibility aliases");
  Require(icons.Select(s=>s.name).Distinct().Count()==90,"unique names");
  foreach(var s in icons){Require(s.texture.width==903&&s.texture.height==903,"unscaled packed texture");Require(s.rect.width==125&&s.rect.height==125&&s.rect.xMin>=0&&s.rect.yMin>=0&&s.rect.xMax<=903&&s.rect.yMax<=903,"125-square bounded region "+s.name);}
  var expected=new List<string>();expected.AddRange(new[]{"seal","dust","ore","insight","exp","stone","chest"}.Select(k=>"resource-"+k));expected.AddRange(Enumerable.Range(0,12).Select(i=>"equipment-"+i.ToString("00")));expected.AddRange(Enumerable.Range(0,24).Select(i=>"gem-"+i/6+"-"+(i%6+1)));
  foreach(var key in expected)Require(icons.Any(s=>s.name==key),"named icon "+key);
  Require(icons.Count(s=>s.name.StartsWith("frame-"))==41,"41 supplied frames");
  foreach(string key in new[]{"quality-1","quality-2","quality-3","quality-4","quality-5","slot-background"})Require(icons.Any(s=>s.name==key),"shared cell "+key);
  File.WriteAllText(Output+"/import-check.txt","PASS "+assertions+" checks: 43 named icons + 41 compatibility frames + 6 shared cells; 903-square texture; all sprites125-square.");
  Debug.Log("[ITEM ICONS] Import verified: "+assertions+" checks.");
 }
 [MenuItem("GXDL/QA/Item Icons")]
 public static void Run(){
  if(running||!Application.isPlaying||!GxdlGame.Instance||!GxdlGame.Instance.CanPlay)throw new InvalidOperationException("Enter the game's editor preview first.");
  assertions=0;running=true;Directory.CreateDirectory(Output);File.WriteAllText(Output+"/runtime-check.txt","RUNNING");
  GxdlGame.Instance.StartCoroutine(Guard(Check(GxdlGame.Instance)));
 }
 static IEnumerator Guard(IEnumerator routine){
  var stack=new Stack<IEnumerator>();stack.Push(routine);
  try{while(stack.Count>0){bool more;object value=null;try{more=stack.Peek().MoveNext();if(more)value=stack.Peek().Current;}catch(Exception e){File.WriteAllText(Output+"/runtime-check.txt","FAIL: "+e);Debug.LogException(e);yield break;}if(!more){(stack.Pop() as IDisposable)?.Dispose();continue;}if(value is IEnumerator nested)stack.Push(nested);else yield return value;}}
  finally{while(stack.Count>0)(stack.Pop() as IDisposable)?.Dispose();running=false;}
 }
 static void SetSize(int w,int h)=>typeof(GuxuBattlePreparationCheck).GetMethod("SetSize",BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,new object[]{w,h});
 static Image Icon(string name)=>UnityEngine.Object.FindObjectsOfType<Image>().Single(x=>x.name==name&&x.gameObject.activeInHierarchy);
 static void Click(string name,int index=0){
  var button=UnityEngine.Object.FindObjectsOfType<Button>().Where(b=>b.name==name&&b.gameObject.activeInHierarchy).OrderByDescending(b=>b.transform.position.y).ThenBy(b=>b.transform.position.x).ElementAt(index);
  Require(button.interactable,"enabled "+name);Canvas.ForceUpdateCanvases();var rect=(RectTransform)button.transform;
  var pointer=new PointerEventData(EventSystem.current){position=RectTransformUtility.WorldToScreenPoint(button.GetComponentInParent<Canvas>().worldCamera,rect.TransformPoint(rect.rect.center)),button=PointerEventData.InputButton.Left};
  var hits=new List<RaycastResult>();EventSystem.current.RaycastAll(pointer,hits);Require(hits.Count>0&&ExecuteEvents.GetEventHandler<IPointerClickHandler>(hits[0].gameObject)==button.gameObject,"unblocked "+name+" at "+pointer.position+"; hits: "+string.Join(",",hits.Select(v=>v.gameObject.name)));ExecuteEvents.Execute(button.gameObject,pointer,ExecuteEvents.pointerClickHandler);
 }
 static IEnumerator Capture(string name){yield return new WaitForEndOfFrame();var image=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes(Output+"/"+name+".png",image.EncodeToPNG());UnityEngine.Object.Destroy(image);}
 static RectTransform Rect(Transform parent,string name,float x,float y,float w,float h){var go=new GameObject(name,typeof(RectTransform));var r=(RectTransform)go.transform;r.SetParent(parent,false);r.anchorMin=r.anchorMax=r.pivot=new Vector2(0,1);r.anchoredPosition=new Vector2(x,-y);r.sizeDelta=new Vector2(w,h);return r;}
 static IEnumerator Gallery(bool frames=false){
  var canvas=new GameObject("Item Icon Gallery",typeof(Canvas),typeof(CanvasScaler));var c=canvas.GetComponent<Canvas>();c.renderMode=RenderMode.ScreenSpaceOverlay;c.sortingOrder=500;
  var scaler=canvas.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(750,1334);
  try{
   Rect(canvas.transform,"Background",0,0,750,1334).gameObject.AddComponent<Image>().color=new Color(.025f,.055f,.09f);
   var font=UnityEngine.Object.FindObjectsOfType<Text>().First().font;
   var title=Rect(canvas.transform,"Title",20,18,710,56).gameObject.AddComponent<Text>();title.font=font;title.fontSize=28;title.text=frames?"背景空框验收  41个独立切片":"图标资源验收  43枚主体图标";title.color=Color.white;title.alignment=TextAnchor.MiddleCenter;
   var keys=new List<string>();var names=new List<string>();keys.AddRange(new[]{"seal","dust","ore","insight","exp","stone","chest"}.Select(k=>"resource-"+k));names.AddRange(new[]{"破封符","灵尘","玄铁","悟道点","修为","突破石","章节宝匣"});
   for(int i=0;i<12;i++){keys.Add("equipment-"+i.ToString("00"));names.Add(EquipmentTiers.Names[i]);}
   for(int i=0;i<24;i++){keys.Add("gem-"+i/6+"-"+(i%6+1));names.Add(GameRules.GemLabel(i+1));}
   if(frames){
    keys.Clear();names.Clear();
    var resources=new[]{"seal","dust","ore","insight","exp","stone","chest"};var labels=new[]{"破封符","灵尘","玄铁","悟道点","修为","突破石","章节宝匣"};
    for(int i=0;i<7;i++){keys.Add("frame-resource-"+resources[i]);names.Add(labels[i]+"框");}
    for(int i=0;i<10;i++){keys.Add("frame-equipment-"+i.ToString("00"));names.Add("装备通用框 "+(i+1));}
    for(int i=0;i<24;i++){keys.Add("frame-gem-"+i/6+"-"+(i%6+1));names.Add(new[]{"赤曜","碧生","玄罡","紫电"}[i/6]+" "+(i%6+1)+"级框");}
   }
   for(int i=0;i<keys.Count;i++){
    float x=15+i%6*121,y=85+i/6*151;
    var bg=Rect(canvas.transform,"Cell "+i,x,y,112,112).gameObject.AddComponent<Image>();bg.color=i%2==0?new Color(.10f,.16f,.22f):new Color(.78f,.82f,.84f);
    var icon=Rect(canvas.transform,keys[i],x+3,y+3,106,106).gameObject.AddComponent<Image>();icon.sprite=GuxuItemIcons.Get(keys[i]);icon.preserveAspect=true;icon.rectTransform.pivot=new Vector2(.5f,.5f);icon.rectTransform.anchoredPosition=new Vector2(x+56,-y-56);
    var label=Rect(canvas.transform,"Label "+i,x-2,y+114,116,30).gameObject.AddComponent<Text>();label.font=font;label.fontSize=15;label.text=names[i];label.color=Color.white;label.alignment=TextAnchor.UpperCenter;
   }
   yield return Capture(frames?"00-frame-overview":"00-icon-overview");
  }finally{canvas.SetActive(false);UnityEngine.Object.Destroy(canvas);}
 }
 static IEnumerator Check(GxdlGame g){
  var original=g.Rules;var property=typeof(GxdlGame).GetProperty("Rules");string savePath=GameRules.SavePath;byte[] bytes=File.Exists(savePath)?File.ReadAllBytes(savePath):null;
  int opening=PlayerPrefs.GetInt(GxdlGame.OpeningStoryKey,-1),first=PlayerPrefs.GetInt(GxdlGame.FirstUnsealStoryKey,-1);
  var window=EditorWindow.GetWindow(typeof(Editor).Assembly.GetType("UnityEditor.GameView"));var sizeProperty=window.GetType().GetProperty("selectedSizeIndex",BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic);int size=(int)sizeProperty.GetValue(window);
  try{
   PlayerPrefs.SetInt(GxdlGame.OpeningStoryKey,1);PlayerPrefs.SetInt(GxdlGame.FirstUnsealStoryKey,1);g.SkipStory();float deadline=Time.realtimeSinceStartup+4;while(g.IsStoryPlaying&&Time.realtimeSinceStartup<deadline)yield return null;Require(!g.IsStoryPlaying,"story finished");
   var r=new GameRules(original.Config,new SaveData{stage=201,level=40,equipmentSchema=1,coins=50000,forgeOre=3000,insight=1000,breakthroughStones=50});r.EnsureExpansion();for(int i=0;i<24;i++)r.Save.gemInventory[i]=9;
   typeof(GameRules).GetField("storagePath",Private).SetValue(r,Path.GetFullPath(Output+"/fixture-save.json"));property.SetValue(g,r);
   SetSize(750,1334);yield return null;yield return null;g.Home();yield return null;
   Require(GuxuUiArt.Navigation(GuxuUiArt.Icon.SealToken).name=="resource-seal","wallet seal");Require(GuxuUiArt.Navigation(GuxuUiArt.Icon.SpiritDust).name=="resource-dust","wallet dust");
   Require(GuxuItemIcons.Gem(0)==null&&GuxuItemIcons.Gem(25)==null,"empty and invalid gems");
   yield return Gallery();yield return Gallery(true);yield return Capture("01-home");
   g.ShowForge();yield return new WaitForEndOfFrame();for(int i=0;i<12;i++)Require(Icon("Forge Equipment Icon "+i).sprite.name=="equipment-"+i.ToString("00"),"forge slot "+i);yield return Capture("02-forge");Click("Forge Slot 11");yield return new WaitForEndOfFrame();Click("Forge Upgrade");Require(r.Save.forgeLevels[11]==1,"forge action preserved");
   for(int color=0;color<4;color++){
    g.ShowGems(0,0,color);yield return new WaitForEndOfFrame();
    for(int lv=0;lv<6;lv++)Require(Icon("Gem Inventory Icon "+(color*6+lv)).sprite==GuxuItemIcons.Gem(color*6+lv+1),"gem level "+color+"/"+lv);
    yield return Capture("03-gems-"+color);Click("镶嵌",0);yield return new WaitForEndOfFrame();Require(r.Save.gemSlots[0]==color*6+1,"socket action "+color);Click("3合1",0);yield return new WaitForEndOfFrame();Require(r.Save.gemInventory[color*6+1]==10,"combine action "+color);Click("卸下当前孔");yield return new WaitForEndOfFrame();Require(r.Save.gemSlots[0]==0,"remove action "+color);
   }
   var catalog=GuxuStageConfig.Load();
   for(int tier=1;tier<=10;tier++){
    for(int kind=0;kind<4;kind++)r.Save.dungeonClears[kind]=10;
    g.ShowBattlePrep(catalog.Dungeon(1,tier));yield return new WaitForEndOfFrame();var reward=GuxuStageRules.Reward(catalog.Dungeon(1,tier),false);
    Require(Icon("Prep Reward Icon 1").sprite==GuxuItemIcons.Gem(reward.gemIndex+1),"actual jade reward tier "+tier);if(tier==8)yield return Capture("04-jade-reward");g.CloseModal();
   }
   g.ShowChapter(0);yield return new WaitForEndOfFrame();Require(Icon("Chapter Chest Icon").sprite.name=="resource-chest","chest sprite");yield return Capture("05-chapter-chest");Click("领取章节宝匣");Require(r.Save.chapterChests[0],"chest action preserved");
   SetSize(750,1624);yield return null;yield return null;g.ShowGems(11,1,3);yield return Capture("06-tall-gems");Click("镶嵌",5);Require(r.Save.gemSlots[23]==24,"tall phone socket target");g.ShowForge(11);yield return Capture("07-tall-forge");Click("Forge Slot 0");
   Require(bytes==null?!File.Exists(savePath):bytes.SequenceEqual(File.ReadAllBytes(savePath)),"original save unchanged");
   File.WriteAllText(Output+"/runtime-check.txt","PASS "+assertions+" checks: wallet icons; all 12 forge equipment sprites; all 24 gem variants; all 10 jade reward mappings; socket/combine/remove; forge/chest actions; 750x1334 and 750x1624 input; original player save unchanged.");
  }finally{
   g.CloseModal();property.SetValue(g,original);if(opening<0)PlayerPrefs.DeleteKey(GxdlGame.OpeningStoryKey);else PlayerPrefs.SetInt(GxdlGame.OpeningStoryKey,opening);if(first<0)PlayerPrefs.DeleteKey(GxdlGame.FirstUnsealStoryKey);else PlayerPrefs.SetInt(GxdlGame.FirstUnsealStoryKey,first);sizeProperty.SetValue(window,size);g.Home();
  }
 }
}
