using UnityEngine;
namespace Gxdl {
// Port of the cached GoodsFallingEffect: rise, fall, two diminishing bounces.
public static class SpiritStoneMotion {
 public static float Duration(float rise,float drop)=> (2*rise+3.4f*drop)/300f;
 public static float EaseIn(float p)=>1-Mathf.Sqrt(1-Mathf.Clamp01(p)*Mathf.Clamp01(p));
 static float EaseOut(float p){p=Mathf.Clamp01(p)-1;return Mathf.Sqrt(1-p*p);}
 public static Vector2 Sample(float t,float spread,float rise,float drop){
  float total=Duration(rise,drop);float x=spread*Mathf.Clamp01(t/(total-.1f));
  float up=rise/300f,down=(rise+drop)/300f,bounce=.7f*drop/300f,small=.5f*drop/300f;
  float y;
  if(t<up)y=rise*EaseOut(t/up);
  else if((t-=up)<down)y=Mathf.Lerp(rise,-drop,EaseIn(t/down));
  else if((t-=down)<bounce)y=-drop+.5f*drop*EaseOut(t/bounce);
  else if((t-=bounce)<bounce)y=-.5f*drop-.5f*drop*EaseIn(t/bounce);
  else if((t-=bounce)<small)y=-drop+.2f*drop*EaseOut(t/small);
  else if((t-=small)<small)y=-.8f*drop-.2f*drop*EaseIn(t/small);
  else y=-drop;
  return new Vector2(x,y);
 }
}
}
