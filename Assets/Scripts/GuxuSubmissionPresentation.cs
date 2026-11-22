using System.Collections;
using System.Text.RegularExpressions;
using UnityEngine;

namespace Gxdl {
public static class GuxuDisplayText {
 public static string Normalize(string value){
  if(string.IsNullOrEmpty(value))return value;
  string[] old={"暴击","吸血","扫荡","毒瘴幽谷","九幽冥殿","锁魂使","冰刃幽灵","冰灯幽灵","万蛊蛛母","蛊木残魂","蛊术残魂","毒棘蛛","毒囊蛛","幽冥判官","幽灯卫","裂隙灵体","太虚残神","血刃"};
  string[] now={"会心","吸收","速通","青岚幽谷","九曜古殿","玄锁卫","冰刃守卫","冰灯守卫","千丝蛛后","古木守卫","古木守卫","荆棘蛛","荆棘蛛","古殿执事","曜灯卫","裂隙守卫","太虚守尊","灵刃"};
  for(int i=0;i<old.Length;i++)value=value.Replace(old[i],now[i]);
  string[] chapterOld={"精英·浮空剑灵","精英·花冠灵妖","精英·机关弩卫","精英·冰灯守卫","精英·逐电枪卫","精英·古木守卫","精英·星核浮灵","精英·裂隙守卫","精英·火羽妖","精英·玄锁卫","浮空剑灵","高瘦剑傀","花冠灵妖","枯木士卒","机关弩卫","重盾甲卫","霜刃侍卫","古木守卫","星核浮灵","砂刃游傀","太虚禁卫","火羽妖","熔岩兵","雷翼傀","曜灯卫"};
  string[] chapterNow={"精英·双剑巡守","精英·花冠巨木卫","精英·持戟盾卫","精英·霜甲灯卫","精英·双环雷卫","精英·千丝木蛛","精英·星砂巨卫","精英·星盘守卫","精英·重锤熔甲卫","精英·九曜锁卫","机关剑卫","机关剑卫","藤木守卫","藤木守卫","玄甲步卫","玄甲步卫","冰灯守卫","荆棘蛛","砂岩行者","砂岩行者","裂隙守卫","铜炉傀儡","铜炉傀儡","逐电枪卫","玄锁卫"};
  for(int i=0;i<chapterOld.Length;i++)value=value.Replace(chapterOld[i],chapterNow[i]);
  return value;
 }
}
public partial class GxdlGame {
 const string HealthyAdvice="抵制不良游戏，拒绝盗版游戏。\n注意自我保护，谨防受骗上当。\n适度游戏益脑，沉迷游戏伤身。\n合理安排时间，享受健康生活。";
 void InitializeAccounts(){StartCoroutine(ShowHealthNotice());}
 IEnumerator ShowHealthNotice(){
  Clear();ExpansionScreen="health-notice";
  var backing=Img("",view,0,0,750,1334,Color.black).rectTransform;
  backing.anchorMin=backing.anchorMax=backing.pivot=Vector2.one*.5f;backing.anchoredPosition=Vector2.zero;
  float scale=Mathf.Min(root.rect.width/750f,root.rect.height/1334f);backing.sizeDelta=root.rect.size/scale;
  Txt("健康游戏忠告",view,60,370,630,70,42,Color.white);
  Txt(HealthyAdvice,view,45,500,660,310,32,Color.white);
  yield return new WaitForSecondsRealtime(3f);
  InitializeAccountSession();
 }
 void DrawLoginNotices(){
  Txt("健康游戏忠告",view,65,1035,620,40,25,cream);
  Txt(HealthyAdvice,view,65,1080,620,140,22,cream);
  int age=GuxuAccountConfig.Load().ageRating;
  if(age==8||age==12||age==16){
   SealButton(age+"+ 适龄提示",view,540,225,175,52,()=>{var p=OpenModal("适龄提示",410);Txt("本游戏适合"+age+"周岁及以上用户。\n请合理安排游戏时间，未成年人应在监护人指导下使用。",p,45,115,590,180,28,ink);});
  }
 }
}
}
