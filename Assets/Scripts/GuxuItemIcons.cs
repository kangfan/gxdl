using System.Collections.Generic;
using UnityEngine;

namespace Gxdl {
// Named sub-sprites preserve the supplied atlas pixels and alpha channel.
public static class GuxuItemIcons {
 public const string Path="Game/Guxu/Expansion/Icons/item-icons-20261110";
 static Dictionary<string,Sprite> sprites;
 public static Sprite Get(string key){
  if(sprites==null){sprites=new Dictionary<string,Sprite>();foreach(var sprite in Resources.LoadAll<Sprite>(Path))sprites.Add(sprite.name,sprite);}
  return sprites.TryGetValue(key,out var result)?result:null;
 }
 public static Sprite Gem(int encoded)=>encoded>=1&&encoded<=24?Get("gem-"+(encoded-1)/6+"-"+((encoded-1)%6+1)):null;
}
}
