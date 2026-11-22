using System.Collections;
using UnityEngine;
using UnityEngine.UI;
namespace Gxdl {
// Each effect owns its lifetime, so changing screens also cancels its animation.
public class UiFeedback : MonoBehaviour {
 public static void Float(Graphic target,float distance=70,float duration=.85f){target.gameObject.AddComponent<UiFeedback>().StartCoroutine(Drift(target,distance,duration));}
 static IEnumerator Drift(Graphic target,float distance,float duration){var r=target.rectTransform;var origin=r.anchoredPosition;var color=target.color;for(float t=0;t<duration;t+=Time.deltaTime){float p=t/duration;r.anchoredPosition=origin+Vector2.up*(distance*(1-Mathf.Pow(1-p,2)));target.color=new Color(color.r,color.g,color.b,color.a*(1-Mathf.SmoothStep(0,1,Mathf.Clamp01((p-.35f)/.65f))));r.localScale=Vector3.one*(1+.15f*Mathf.Sin(p*Mathf.PI));yield return null;}Destroy(target.gameObject);}
 public static void Pop(RectTransform target){target.gameObject.AddComponent<UiFeedback>().StartCoroutine(Entrance(target));}
 static IEnumerator Entrance(RectTransform target){var origin=target.anchoredPosition;var group=target.gameObject.AddComponent<CanvasGroup>();for(float t=0;t<.18f;t+=Time.unscaledDeltaTime){float p=1-Mathf.Pow(1-t/.18f,3);group.alpha=p;target.anchoredPosition=origin+Vector2.down*(22*(1-p));yield return null;}group.alpha=1;target.anchoredPosition=origin;}
 public static void Hit(Graphic target,float direction,float duration){target.gameObject.AddComponent<UiFeedback>().StartCoroutine(Impact(target,direction,duration));}
 static IEnumerator Impact(Graphic target,float direction,float duration){var r=target.rectTransform;var origin=r.anchoredPosition;var color=target.color;for(float t=0;t<duration;t+=Time.deltaTime){float p=t/duration;r.anchoredPosition=origin+Vector2.right*(direction*12*Mathf.Sin(p*Mathf.PI));target.color=Color.Lerp(new Color(1,.5f,.4f),color,Mathf.Clamp01(p*3));yield return null;}r.anchoredPosition=origin;target.color=color;}
 public static void Bar(RectTransform target,float width,float duration){var effect=target.GetComponent<UiFeedback>()??target.gameObject.AddComponent<UiFeedback>();effect.StopAllCoroutines();effect.StartCoroutine(Fill(target,width,duration));}
 static IEnumerator Fill(RectTransform target,float width,float duration){float initial=target.sizeDelta.x;for(float t=0;t<duration;t+=Time.deltaTime){target.sizeDelta=new Vector2(Mathf.Lerp(initial,width,t/duration),target.sizeDelta.y);yield return null;}target.sizeDelta=new Vector2(width,target.sizeDelta.y);}
 void OnDisable(){StopAllCoroutines();}
}
}
