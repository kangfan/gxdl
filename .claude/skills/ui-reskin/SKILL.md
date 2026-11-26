---
name: ui-reskin
description: 古墟大陆系统界面换皮标准流程。按效果图切图换皮某个系统界面时使用（如洞府、商城、图鉴等），覆盖切片修补、导入、四件套、验证全流程。
---

# 界面换皮流程（已验证）

按 Art/<系统>/ 效果图为某系统界面换皮。本轮样板：洞府（commit 参考 RESIDENCE-RESKIN-20261125，见 Design/ITERATIONS.md）。

## 0. 前置确认

1. 读 `Design/ITERATIONS.md` 最新轮次与 `Docs/UI-CREATION-RULES.md`。
2. 向用户确认两点再动手：呈现方式（整页自带背景 vs 透出主界面弹窗）；相邻旧弹窗是否同轮换皮。
3. 效果图含烘焙文字 → **全部修补擦除，文字一律 Unity 实时渲染**（UI 底图不烘焙文字）。
4. 用户只给成品图时：开发自己切图，不走 `Tools/ResourcePipeline/pipeline.ps1`（其 manifest 强制 Image 2.6 生图来源）；来源与 SHA 记入 ITERATIONS。

## 1. 切图（Python PIL）

- **用户给官方透明图集时优先用图集**（背包轮 BAG-ATLAS-20261125 起为准），不要自己从效果图截图切（带实心背景，只能整块带出再修补，易残留色差）。**图集切片必须按像素对象选取，不是纯矩形裁剪**（洞府轮 RESIDENCE-ATLAS-20261125 用户指正）：网格定坐标取粗框（外扩 ≥4px），在框内做 alpha 连通域分析——以框中心为种子提取主对象连通体（含柔光晕，柔光 alpha 低但范围大，框必须放宽，否则主对象触界会被误删）、保留框内完整小件、清零触到框边界的邻居入侵组件，最后裁到保留像素的紧贴包围盒。等价于 PS 对象选取。样例脚本 `Tools/ResidenceReskin/slice-atlas.py`（`extract_object`）。面板内已烘焙的分隔线/饰带直接保留（省节点），只擦动态文字；分段标题饰线（中央菱形+长线）左右对切两片、右片 localScale.x=-1 翻转对称，中间实时文字；关闭按钮/角标用原位覆盖；显示尺寸按对象切片原比例适配布局，不硬拉到效果图比例。
- 环境：`"$LOCALAPPDATA/Python/bin/python.exe"`（PIL 13；WindowsApps python 是坏链，bash 不认中文路径，脚本一律写文件执行，禁 heredoc）。
- 目录：`ResourceCache/<系统>-<日期>/raw|processed/`（raw 存效果图 ASCII 别名副本），脚本放 `Tools/<系统>Reskin/`。
- 先给效果图加坐标网格输出 `_grid-*.png` 目视定坐标；关键区域再 2x 放大加刻度复核（按钮文字起点、铭牌边缘极易估错，洞府轮返工三次均因坐标）。**多张原图分辨率可能不同**（如背包轮 1440 与 1125），每张分别折算，禁止沿用比例——详情切片曾整体偏移 100px。
- 生成器里 panel-local 坐标与 screen 坐标不可混用：一律先算 screen（source×比例）再减面板原点，只减一次；背包轮槽位行/饰线/提示被二次减原点，截图对照才发现。
- 功能性关闭按钮与角标可用"原位覆盖"贴法：切片保留烘焙原样，live Sprite 略大于烘焙图放同一位置盖住，免去异形抠图与修补（背包轮关闭 X、阶数角标均此法）。装饰性 X 若对应功能不允许关闭则必须擦除。
- 切片规格：生成尺寸→按用途 maxTextureSize（整页 bg 2048、大面板 1024、按钮 256—512、小件 128—256），显示尺寸按 750×1334 布局换算（×0.731）。
- 文字修补 `inpaint`：逐列上下取 3—4 行均值渐变 + 高斯模糊；**擦除区上下各留 ≥8px 落在按钮/铭牌内部**，蹭到金边线会把内部洗白。大面积内部填充用整行均值渐变（逐列采样会产生竖条纹）。
- 动态元素必须从底图擦掉（示例：遗宝图标、按钮整体切出）。效果图里示意性的关闭按钮若对应功能不允许关闭，也要擦除并记录偏差。
- 文字颜色从擦除前采样：暗字取最暗 2% 均值，浅字（青底）取最亮 2% 均值；输出 #RRGGBB 写入 View/生成器常量。
- 产出 `_review.png` 蒙太奇逐张目检后再导入。

## 2. 导入

- 复制到 `Assets/Resources/Game/Guxu/Expansion/UI/`（`GuxuUiArt.Optional(key)` 即按此路径解析）。
- **导入器必须加分支**（`GuxuFinalArtImporter.OnPreprocessTexture`）：
  ```csharp
  if(lowerAsset.Contains("/<前缀>-")){
   t.textureType=TextureImporterType.Sprite;t.spriteImportMode=SpriteImportMode.Single;t.spritePixelsPerUnit=100;
  }
  ```
  外层 Expansion 分支默认 Default 类型，不加此分支 `LoadAssetAtPath<Sprite>` 返回 null、prefab 存空引用（洞府轮踩坑）。
- `refresh_unity` → 触发 `Tools/qa/reimport-textures.trigger` → 报告 `Logs/qa/reimport-textures.txt` 核对覆盖数。

## 3. 四件套（按 UI-CREATION-RULES）

1. **View** `Assets/Scripts/Modules/<Xxx>View.cs`：继承 UiModule；只有动态值进 Apply（境界/战力等取自 Rules，禁演示常量）；静态文案在 prefab；BindActions 统一挂回调（先 RemoveAllListeners）；IsComplete 门控回退。
2. **生成器** `Assets/Editor/<Xxx>PrefabSource.cs`：`[InitializeOnLoad]` + `ScreenPrefabKit.Register`；皮肤 `AssetDatabase.LoadAssetAtPath<Sprite>(SkinPath + key + ".png")`，注意 SkinPath 目录——根目录与 HomeReskin/ 子目录都要能命中（洞府轮 SkinPath 固定子目录导致 relic 切片全 null）；`builder.Node/TextNode` 的 parentPath 是**全路径**（"Panel/BtnX"）。
3. **prefab**：`UI/<Xxx>View.prefab`，路径常量在 UiScreens.cs；把生成加入 `GxdlPrefabRefresh`（Tools/qa/generate.trigger）。
4. **接线**：旧 `ShowXxx()` 开头 `if(ShowXxxPrefab())return;`；OpenScreen 失败/IsComplete 不满足回退旧构建；跳转其他 prefab 页直接调其 ShowXxx（OpenScreen 自带先关）；跳旧弹窗页需先 CloseScreen；`if(busy)return` 保留。
   - **需要透出主界面的弹窗页不走 OpenScreen**（它先关当前页）：直接 `UiModules.Open(path)` 叠开（主页 prefab 保持可见），再手动 `activeModulePage.SetParent(root,false);FitPage(...);SetSizeWithCurrentAnchors(750/1334)` 并赋 activeModule/activeModulePage（参考 ShowBagPrefab）。上一页之上的二级弹窗做成主 prefab 的**子面板**（OpenScreen 无法叠页），开合用缩放动画。
   - 生成器 `builder.Node/TextNode` 的 parentPath 是**全路径**（"BagPanel/Slot 0"，两处易漏：子节点创建与 Bind 的 Find）。
   - prefab 里 Button/Text 数组绑定后要抽查序列化值非 `{fileID: 0}`——Find 路径错时 Bind 静默全空，IsComplete 若只查 Length 会漏检。

## 4. 验证（Unity MCP，端口 8080，工具用 Tools/mcp/mcp-call.sh）

1. `refresh_unity` → read_console 无 `error CS`。
2. `generate.trigger` → `Logs/qa/generate.txt` 无"异常"。**编辑器在 Play 模式时 Tick 跳过且触发器不会被消费**——若等不到报告，先 stop Play，触发器会在退出 Play 后自动执行。
3. `GxdlAutoFlow`（Assets/Scripts/QA/）加用例：Step 开页断言 ActiveId、Click 关键按钮、状态断言（如分解后 pending==null）。注意演出动画窗口期（如灵尘飞行 ~2s 置 busy）与缩放动画（~0.1s）——waitSeconds 后再断言导航结果。method 组引用的宿主方法要 public。
4. play + `Tools/qa/flowcheck.run` → `Logs/qa/flow-check.txt` 全 PASS → `Logs/qa/shots/` 截图与效果图逐项对照（层级/文字颜色/状态/可读性），stop Play。
5. 全程用隔离验证：Chop 造 pending 前备份存档由 runner 自动处理；不打包不真机时在 ITERATIONS 标注 technical-verified。

## 5. 记录

Design/ITERATIONS.md 追加 `## <系统>-RESKIN-<日期>`：来源与切图方式、贴图清单与导入规则、四件套清单、与效果图偏差（逐条写原因）、验证证据位置。
