using System.Collections;
using UnityEngine;
using UnityEngine.UI;
namespace Gxdl {
// Presentation consumes the displayed event snapshots, never NextTurn's already advanced totals.
public sealed class GuxuBattleParticles:MonoBehaviour {
 Image player,enemy;GuxuParticleFx playerShield,enemyShield;float nextShield;
 public int DisplayedPlayerShield {get;private set;}
 public int DisplayedEnemyShield {get;private set;}
 public float Speed=1;
 public void Bind(Image hero,Image foe,int heroShield,int foeShield){player=hero;enemy=foe;SetShields(heroShield,foeShield);}
 public void SetShields(int hero,int foe){if(!isActiveAndEnabled)return;DisplayedPlayerShield=hero;DisplayedEnemyShield=foe;Shield(ref playerShield,hero,"Player");Shield(ref enemyShield,foe,"Enemy");}
 void Shield(ref GuxuParticleFx fx,int amount,string side){if(amount>0&&!fx)fx=GuxuParticleFx.Create(transform,"Particle "+side+" Shield",false,-7,true,Speed);if(amount<=0&&fx){fx.gameObject.SetActive(false);Destroy(fx.gameObject);fx=null;}}
 void Update(){if(Time.time<nextShield)return;nextShield=Time.time+.075f/Mathf.Max(.1f,Speed);Shell(playerShield,player);Shell(enemyShield,enemy);}
 void Shell(GuxuParticleFx fx,Image body){if(!fx||!body||!body.gameObject.activeInHierarchy)return;fx.Speed=Speed;float t=Time.time*2;var motion=body.GetComponent<GuxuCharacterMotion>();var foot=transform.InverseTransformPoint(motion?motion.FeetWorld:GuxuFootCloud.Foot(body));float h=body.rectTransform.rect.height*.6f;for(int i=0;i<3;i++){float a=t+i*Mathf.PI*2/3;fx.Emit(foot+new Vector3(Mathf.Cos(a)*75,h*.5f+Mathf.Sin(a)*h*.5f,0),Vector3.zero,new Vector2(13,26),new Color(.22f,.82f,1,.65f),.32f,a*Mathf.Rad2Deg);}}
 void OnDisable(){StopAllCoroutines();if(playerShield){playerShield.gameObject.SetActive(false);Destroy(playerShield.gameObject);}if(enemyShield){enemyShield.gameObject.SetActive(false);Destroy(enemyShield.gameObject);}playerShield=enemyShield=null;}
 public void Swing(GuxuCharacterMotion actor){if(isActiveAndEnabled)StartCoroutine(SwingTrail(actor,Speed));}
 IEnumerator SwingTrail(GuxuCharacterMotion actor,float speed){
  GuxuParticleFx fx=null;float next=0;
  for(float t=0;t<.55f;t+=Time.deltaTime*speed){if(!actor||!actor.isActiveAndEnabled||!isActiveAndEnabled)yield break;
   if(t>=.14f&&t>=next){if(!fx)fx=GuxuParticleFx.Create(transform,"Particle Normal Sword Sweep",false,-8,false,speed);float a=Mathf.Lerp(-125,90,(t-.14f)/.41f)*Mathf.Deg2Rad;var center=transform.InverseTransformPoint(actor.WeaponWorld);for(int i=0;i<3;i++){float b=a-i*.12f;fx.Emit(center+new Vector3(Mathf.Cos(b)*145,Mathf.Sin(b)*140,0),new Vector3(-Mathf.Sin(b),Mathf.Cos(b),0)*90,new Vector2(22,65),new Color(.35f,.94f,1,.9f),.38f,b*Mathf.Rad2Deg);}next=t+.025f;}yield return null;}
 }
 public void Event(GuxuBattleEvent e,float strength=1){
  bool restoration=e.kind==GuxuBattleEventKind.Heal||e.kind==GuxuBattleEventKind.Shield;Image target=restoration?(e.playerActor?player:enemy):(e.playerActor?enemy:player);if(!target)return;
  var color=e.kind==GuxuBattleEventKind.Poison||e.kind==GuxuBattleEventKind.Break?new Color(.72f,.30f,1,.88f):e.kind==GuxuBattleEventKind.Heal?new Color(.3f,1,.62f,.9f):e.kind==GuxuBattleEventKind.Shield?new Color(.3f,.85f,1,.9f):new Color(1,.83f,.40f,.95f);
  var fx=restoration||e.kind==GuxuBattleEventKind.Poison?GuxuParticleFx.Burst(transform,GuxuParticleFx.Body(target),color,18,strength,Speed):GuxuParticleFx.Impact(transform,GuxuParticleFx.Body(target),color,Mathf.RoundToInt(20*strength),strength,Speed);
  if(e.kind==GuxuBattleEventKind.Heal){fx.System.Clear();for(int i=0;i<20;i++)fx.Emit(new Vector3((i*37%120)-60,-70+i%3*25,0),Vector3.up*(100+i%5*12),new Vector2(10,25),color,.6f+i%4*.1f);}
  SetShields(e.playerShield,e.enemyShield);
 }
 public void Warning(Image body,Color color){var p=GuxuParticleFx.Create(transform,"Particle Boss Telegraph",false,-9,false,Speed);p.transform.position=GuxuFootCloud.Foot(body);for(int i=0;i<32;i++){float a=i*Mathf.PI*2/32;p.Emit(new Vector3(Mathf.Cos(a)*175,Mathf.Sin(a)*50,0),Vector3.up*35,new Vector2(22,65),color,.9f,a*Mathf.Rad2Deg);}}
}
}
