using UnityEngine;
using Gxdl.Core;
using Gxdl.Modules;

namespace Gxdl {
// prefab 界面与旧构建之间的切换点。
// 旧方法一律「先试 prefab，失败回退旧构建」，迁移期间两条路径必须都能跑。
public partial class GxdlGame {
 // 当前挂着的 prefab 页面。同一时刻只允许一张，切换时统一回收。
 Gxdl.Modules.UiModule activeModule;
 RectTransform activeModulePage;

 /// <summary>开启一张 prefab 页面。宿主不可用或 prefab 缺失时返回 null，调用方回退旧构建。</summary>
 T OpenScreen<T>(string prefabPath) where T:Gxdl.Modules.UiModule{
  var bootstrap=GameBootstrap.Instance;
  if(bootstrap==null||bootstrap.UiModules==null)return null;
  var module=bootstrap.UiModules.Open(prefabPath);
  if(module==null)return null;
  var view=module as T;
  if(view==null){bootstrap.UiModules.Close(module.ModuleId);return null;}
  CloseScreen();
  activeModule=module;activeModulePage=(RectTransform)view.transform;
  // 与旧页面同层：挂在旧 view 的父节点下，尺寸与缩放沿用 FitPage。
  activeModulePage.SetParent(root,false);
  FitPage(activeModulePage);
  return view;
 }

 /// <summary>收起当前 prefab 页面。旧页面切换（Clear/Home）前必须先调用。</summary>
 void CloseScreen(){
  if(activeModule!=null){activeModule.OnHide();activeModule.gameObject.SetActive(false);activeModule=null;}
  activeModulePage=null;
 }
}
}
