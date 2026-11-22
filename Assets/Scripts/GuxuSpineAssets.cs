using System;
using Spine.Unity;
using UnityEngine;
namespace Gxdl {
public static class GuxuSpineAssets {
 static SkeletonDataAsset data;
 static Material material;
 public static Material Material {get{Ensure();return material;}}
 public static SkeletonDataAsset Data {get{Ensure();return data;}}
 static void Ensure(){
  if(data && data.GetSkeletonData(true)!=null)return;
  const string path="Game/Guxu/Final/Hero/";
  material=Resources.Load<Material>("Game/Guxu/SpineHero/SkeletonGraphic");
  var texture=Resources.Load<Texture2D>(path+"hero");
  var parts=Resources.Load<Texture2D>(path+"unseal-parts");
  var home=Resources.Load<Texture2D>(path+"home-hero");
  var armRepair=Resources.Load<Texture2D>(path+"home-arm-repair");
  var attackItems=Resources.Load<Texture2D>(path+"attack-items");
  var attackGrip=Resources.Load<Texture2D>(path+"attack-grip");
  var atlasText=Resources.Load<TextAsset>(path+"seeker-atlas");
  var json=Resources.Load<TextAsset>(path+"seeker-data");
  if(!material||!texture||!parts||!home||!armRepair||!attackItems||!attackGrip||!atlasText||!json)throw new InvalidOperationException("Missing Spine hero assets.");
  var atlas=SpineAtlasAsset.CreateRuntimeInstance(atlasText,new[]{texture,parts,home,armRepair,attackItems,attackGrip},material,true);
  new Spine.SkeletonJson(atlas.GetAtlas()).ReadSkeletonData(new System.IO.StringReader(json.text));
  data=SkeletonDataAsset.CreateRuntimeInstance(json,atlas,false,1);
  data.defaultMix=.06f;
  if(data.GetSkeletonData(false)==null)throw new InvalidOperationException("Could not load Spine hero skeleton.");
  var state=data.GetAnimationStateData();state.SetMix("attack","hit",.055f);state.SetMix("unseal","hit",.055f);
  state.SetMix("idle","attack_combo_01",.08f);state.SetMix("attack_combo_01","idle",.12f);state.SetMix("attack_combo_01","hit",.055f);
  state.SetMix("home_idle","home_cast",.08f);state.SetMix("home_cast","home_idle",.12f);state.SetMix("unseal_full","home_idle",0);state.SetMix("home_idle","unseal_full",0);state.SetMix("unseal","home_idle",.14f);state.SetMix("unseal_full","idle",.16f);state.SetMix("hit","idle",.16f);state.SetMix("attack","idle",.14f);state.SetMix("unseal","idle",.16f);
 }
}
}






