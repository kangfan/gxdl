using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;
namespace Gxdl {
public static class GuxuVfxAnchors {
 static Dictionary<string,float[]> anchors;
 public static Vector2 ForSprite(Sprite sprite){
  if(anchors==null)anchors=JsonConvert.DeserializeObject<Dictionary<string,float[]>>(Resources.Load<TextAsset>("Game/Guxu/Vfx/FootAnchors").text);
  string key=sprite.name.Contains("/frame-")?sprite.name:sprite.texture.name+"/0";
  if(anchors.TryGetValue(key,out var p))return new Vector2(p[0],p[1]);
  return new Vector2(.5f,0);
 }
}
}
