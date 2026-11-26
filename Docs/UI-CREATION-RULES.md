# 古墟大陆 · UI 制作与通用组件规范

> 适用：`Assets/Scripts/Modules`、`Assets/Editor`、`Assets/Resources/UI`。
> 参考来源：Football Master 2 的《Unity_UI预制体与通用组件跨项目规范》，按其"只采用规范、不照搬实现"的原则落地。
> 本文中标注 **【通用规范】** 的条目可直接执行；标注 **【本项目】** 的是古墟大陆已确定的写法，换组件时以本项目为准。

## 0. 规则边界

| 标记 | 含义 |
| --- | --- |
| 【通用规范】 | 从实践提炼的制作与验收规则，跨项目成立 |
| 【本项目】 | 古墟大陆的具体实现、目录、类名与工具 |
| 【差异说明】 | 参考工程有、本项目暂不采用或尚未实现的点 |

参考工程使用 HybridCLR、Hotfix View、`ComponentItemKey` 绑定、FancyScrollView 循环列表。
**本项目不引入这些**：无热更框架、无自动生成绑定文件、无第三方列表库。
因此参考文档中的类名（`MyText`、`MyButton`、`CommonFancyGridView` 等）**不得**写入本项目代码。

## 1. 项目接入信息

| 项目项 | 本项目取值 |
| --- | --- |
| UI 基础 | UGUI 1.0.0，无 TextMeshPro |
| 字体 | `Resources/Original/GameFont` |
| 设计分辨率 | 750 × 1334，竖屏 |
| Canvas | `ScreenSpaceCamera`，`ScaleWithScreenSize`，match 0.5 |
| prefab 根目录 | `Assets/Resources/UI/` |
| 路径常量 | `Assets/Scripts/Modules/UiScreens.cs` |
| 代码绑定 | `[SerializeField]` + 生成器末尾显式 `SerializedObject` 绑定 |
| 公共组件 | `UiModule`、`UiManager`、`UiList`、`UiRowView`、`UiTabGroup`、`UiRaycast`、`UiFactory` |
| 编辑器工具 | `UiPrefabBuilder`、`AnchorUtility`、`ScreenPrefabKit`、`UiViewBinder` |
| 验证 | Unity Bridge 编译 + `QaSelfTest` + Play 模式实测 |

## 2. 四件套与职责分层

每个界面固定四样，缺一样算没做完：

| 件 | 位置 | 职责 |
| --- | --- | --- |
| View | `Assets/Scripts/Modules/<Xxx>View.cs` | 继承 `UiModule`，持节点引用，暴露 `Apply(...)`，只做显示与转发 |
| 生成器 | `Assets/Editor/<Xxx>PrefabSource.cs` | 搭静态骨架，注册，显式绑定字段 |
| prefab | `Assets/Resources/UI/<Xxx>View.prefab` | 生成器产出，不手工拖拽 |
| 接线 | 旧 `ShowXxx()` 内 | `OpenScreen<T>()` 成功走 prefab，失败回退旧构建 |

【通用规范】职责边界：

| 层 | 职责 |
| --- | --- |
| 宿主 / `UiManager` | 实例化、缓存、层级、显隐 |
| View | 绑定事件、刷新显示、把操作转给业务层 |
| 业务系统（`Rules` / `GuxuStageRules`） | 数值、判定、发奖扣费 |
| ItemView（`UiRowView`） | 展示单条数据、报告单条点击、可安全复用 |
| prefab 生成器 | 结构，不承载业务 |

View 不做业务判断，不直接读写存档。

## 3. 页面制作前置：先写页面规格表

【通用规范】动手前先填这张表，再写代码：

| 区域 | 节点/组件 | 数据来源 | 状态 | 交互 |
| --- | --- | --- | --- | --- |
| 标题 | `Text` | 配置/存档 | 常驻 | 无 |
| 列表 | `UiList` + `UiRow` | `Rules.Save` | 0/1/多项、锁定、选中 | 点击行 |
| 操作区 | `Button` + 价格 | 规则计算 | 可操作、处理中、已完成、不可用 | 业务回调 |
| 时间区 | `Text` 倒计时 | 结束时间戳 | 进行中、已结束 | 到期刷新 |

状态必须**在 prefab 里有明确节点**（锁定遮罩、选中标记、空态、已领取），
由数据驱动切换，不能只在点击后改一次颜色。

## 4. 层级与命名

【通用规范】

- 用业务语义名：`Back`、`Chapter 3/Name`、`Reward Slot 2/Value`。
- 按真实布局拆容器（`Header` / `Content` / `Actions`），不要全平铺到根节点。
- 需要绑定的节点名保持稳定、可重复生成；重复项必须带编号后缀（`Stage 1`…`Stage 20`）。
- 同一视觉块里独立更新的文本拆成独立节点。

【本项目】

- 节点名用英文；中文只出现在节点上的 `Text` 内容里。
- 字段名与节点名尽量完全一致，`UiViewBinder` **精确匹配优先**；
  模糊匹配会连错相邻节点（曾把 `titleRect` 绑到 `Story Subtitle Shade`）。
- 数组字段按前缀收集并按层级排序，索引顺序必须与视觉顺序一致。
- 生成器末尾一律显式绑定，不依赖自动推断。

## 5. 重复项与运行时实例化

【通用规范】

- 出现 **3 个及以上**相同结构的视觉项，拆成独立 item prefab。
- 可滚动且数量变化的列表用循环列表（本项目为 `UiList`）；主 prefab 保存结构与模板引用。
- 模板节点**必须保持未激活**，运行时实例不得与模板同时显示。
- View/Controller **不通过 `new GameObject` 临时补界面结构**；缺节点就改 prefab 再重新绑定。

【本项目】固定数量的小组（10 张章节卡、20 个关卡节点、24 个天赋节点、12 个装备槽）
直接在 prefab 里全部建出并按索引绑定，不走池化；数量可变的长列表才用 `UiList`。

【差异说明】`HomeView` 目前在 `Apply` 里运行时创建遗宝决策面板与主线任务条。
这违反上面的规则，属于待偿还的技术债：后续应把这两块结构移入 prefab，
用显隐与文本填充代替运行时建节点。

## 6. 布局、点击区与遮罩

【通用规范】

- 根节点、背景、列表、按钮分别确定 anchor、pivot、size、offset，不依赖单一分辨率的绝对位置。
- 普通子页面复用宿主 Canvas；确有独立排序需求再加 Canvas。
- 纯装饰图与普通文本关闭 Raycast；点击热区可大于图标，但不能覆盖其他可交互区域。
- 列表必须有明确视口并裁剪，本项目统一用 `RectMask2D`。
- 九宫格图必须有正确 Sprite Border；拉伸图、九宫格图、Filled 图分别配置。
- 进度条按 prefab 结构实现：左锚点 + 左 pivot 的普通图改宽度；只有 `Filled` 图用 `fillAmount`。
  比例一律 `Clamp(0,1)`，目标值非法时有确定的显示结果。
- 安全区由 `SafeAreaFitter` 处理，不复制固定偏移量。

【本项目】透明点击区统一用 `UiRaycast`，不再用 alpha 0.001 的 `Image` 占位。

## 7. 锚点与整页适配

【本项目】两种方式，按界面二选一，不得混用：

1. **整页等比缩放**：根节点不挂 `UiRootLayout`，由 `FitPage` 缩放居中。
   适用于按美术图像素级对齐的界面（世界地图、章节地图）。
2. **高度自适应**：根节点挂 `UiRootLayout` + `SafeAreaFitter`，子节点锚点贴边。
   适用于内容随屏幕增高、上下留边由拉伸节点吸收的界面（列表类、成长类）。

长屏按固定 750×1334 内容居中，上下留边，不宣称"铺满适配"。

## 8. 状态与反馈

【通用规范】

- 可交互控件三态明确：可用、不可用（`interactable=false`）、选中。
- 不可用不能只靠颜色，同时给文案或锁标记。
- 耗时操作用 `busy` 门控；`interactable` 只表达显示状态，不能替代防重入。
- 请求开始置处理中，成功、失败、取消、异常四条路径都要恢复。

【本项目】锁定 / 已通关 / 当前 / 选中是四种独立状态，分别有节点与判定，不靠文案猜。

## 9. 循环列表与复用状态

使用 `UiList`（`Assets/Scripts/Modules/UiList.cs`）+ `UiRowView`。

```csharp
list.SetCount(items.Count, (row, i) =>
{
    var view = row.GetComponent<UiRowView>();
    view.Apply(items[i].Title, items[i].Subtitle, items[i].Value,
               items[i].Icon, locked: items[i].Locked, selected: i == current);
    view.BindClick(() => Select(i));
});
```

【通用规范】每次填充必须**完整恢复**，不能只改有变化的部分：

- 文本、图片、数值、进度、可交互状态一律重设。
- 锁定、选中、红点、空态、已领取遮罩同时复位。
- 点击回调先 `RemoveAllListeners()` 再绑，避免池化复用时叠加。
- 刷新数据默认**保留滚动位置**；只有进入页面或明确要求时才 `ScrollTo(0)`。
- 列表要覆盖 0 行、1 行、多行与快速滚动四种情况。

【本项目】`UiList` 只做池化不做虚拟化：行数规模在 30 以内。
超过这个量级再考虑虚拟化，不要提前引入。

## 10. 公共组件选型

| 场景 | 组件 | 要点 |
| --- | --- | --- |
| 垂直列表 | `UiList` | 模板必须未激活；`SetCount` 池化；`ScrollTo` 定位 |
| 标准行 | `UiRowView` | 图标/标题/副标题/数值/箭头/锁定/选中；行结构由各界面生成器内联建 |
| 页签 | `UiTabGroup` | 互斥选中、指示对象、禁用某页 |
| 透明点击区 | `UiRaycast` | 不产生可见网格，只接收射线 |
| 界面基类 | `UiModule` | `Track<T>` 自动释放订阅，`OnShow/OnHide/OnPause/OnResume` |
| 界面宿主 | `UiManager` | prefab 实例化、缓存、分层、暂停恢复 |
| 骨架 | `ScreenPrefabKit` | 背景 + 顶栏 + 内容区、批量生成、注册表 |
| 节点原语 | `UiPrefabBuilder` / `AnchorUtility` | 编辑器侧建节点 |
| 运行时原语 | `UiFactory` | 旧路径的 Rect/Txt/Btn/Img |

### 10.1 层级

`UiManager.Open(path, layer)` 的 `layer` 取 `UiLayer`：

| 层 | 用途 |
| --- | --- |
| `Background` | 常驻背景 |
| `Normal` | 普通页面，默认值 |
| `Modal` | 弹窗、确认框 |
| `Toast` | 提示、飘字，永远最上 |

- 每层是一个铺满的容器，层内靠 sibling 顺序排，不存在跨层抢序。
- 打开 `Modal` 时，其下 `Background` / `Normal` 的界面进入 `OnPause`；
  最后一个 `Modal` 关闭后自动 `OnResume`。`Toast` 不触发暂停。
- 弹窗页面的 View 必须实现 `OnPause/OnResume` 来停/启动画与倒计时；
  只在可见时有意义的内容（呼吸光、粒子、刷新计时）一律放在这两个方法里。
- 需要在弹窗之上再覆盖内容时用 `Toast` 层，不要往 `Modal` 层叠第二个弹窗。

【差异说明】参考工程的 `MyText`（语言 key）、`CommonPrice`、`RewardItemView`、`TimeLeft_View`、
`TopBarHelper`、红点系统在**本项目均不存在**，不要生成对这些类的引用。
本项目对应能力：文本直接用 `Text`（无多语言 key），奖励用 `RewardItems`，
价格与倒计时按各界面真实规则计算。

## 11. 回退与迁移期规则

```csharp
var view = OpenScreen<Gxdl.Modules.XxxView>(Gxdl.Modules.UiScreens.Xxx);
if (view == null) { /* 旧构建照常执行 */ }
```

- `OpenScreen` 返回 null 的三种情况：宿主未初始化、prefab 缺失、组件类型不符，均须安全回退。
- 迁移期旧代码一行不删，直到新路径验收通过。
- 同一界面不能同时渲染新旧两套页面。

## 12. 异步与生命周期

本项目界面数据是同步的（本地配置 + 存档），暂不涉及异步资源加载。
一旦引入异步（远端配置、大图），必须遵守：

1. 加载前记录页面/列表项身份或刷新版本。
2. `await` 后先检查页面、目标节点、结果是否仍然有效。
3. 通过检查再赋值；旧结果覆盖新状态视为缺陷。
4. 关闭/切页/复用旧项时使未完成的请求失效。
5. 列表项复用时，上一次的图片与倒计时必须失效。

## 13. 验收清单

### 静态

- [ ] 节点语义清楚、有分区，重复结构 ≤3 项未拆 item
- [ ] 静态结构在 prefab 内，代码未临时拼界面（已知例外：`HomeView` 两处）
- [ ] 动态值无写死演示数据，节点留空由 `Apply` 填
- [ ] 绑定字段无空引用（批量脚本核对）
- [ ] 纯装饰节点关闭 Raycast，点击热区不重叠
- [ ] 九宫格 Border 正确；进度条 Simple/Filled 未混用
- [ ] 状态节点（锁定/选中/已通关/空态）在 prefab 中存在

### 运行

- [ ] 打开、关闭、重复进入正常，无重复监听或重复窗口
- [ ] 按钮禁用/恢复、连点、失败与取消路径正确
- [ ] 列表覆盖 0/1/多项与快速滚动，复用后状态正确
- [ ] 操作后数值、按钮可用性、解锁、选中立即联动
- [ ] 失败不扣资源
- [ ] 三种分辨率目视：750×1334、1080×2400、2048×1536
- [ ] 与效果图逐项对照：层级、状态符号、可读性、操作焦点
- [ ] 关掉 prefab 分支能回退旧构建
