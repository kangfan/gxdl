using UnityEngine;
namespace Gxdl {
// Independent environmental light; no baked character or monolith in the scene art.
public sealed class GuxuFinalAtmosphere:MonoBehaviour {
 GuxuSoftVfx[] lights=new GuxuSoftVfx[12];Vector2[] positions=new Vector2[12];
 void Start(){for(int i=0;i<lights.Length;i++){var go=new GameObject("Ambient Wisp "+i,typeof(RectTransform));var r=go.GetComponent<RectTransform>();r.SetParent(transform,false);r.anchorMin=r.anchorMax=r.pivot=new Vector2(0,1);r.sizeDelta=i<4?new Vector2(160,50):new Vector2(5,5);positions[i]=new Vector2(90+i*139%610,-(185+i*73%610));r.anchoredPosition=positions[i];go.transform.SetSiblingIndex(1);lights[i]=go.AddComponent<GuxuSoftVfx>();lights[i].raycastTarget=false;}}
 void Update(){for(int i=0;i<lights.Length;i++){if(!lights[i])continue;float t=Time.unscaledTime;lights[i].rectTransform.anchoredPosition=positions[i]+new Vector2(Mathf.Sin(t*.18f+i)*20,Mathf.Cos(t*.25f+i)*8);lights[i].color=new Color(.75f,.94f,1,(i<4?.06f:.5f)*(.65f+.35f*Mathf.Sin(t+i)));}}
}
}
