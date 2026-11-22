---
name: unity-tools
description: Codely CLI Unity Editor 工具集完整参考。当用户需要在 Unity Editor 中执行任何操作时必须使用此 skill，包括但不限于：控制编辑器状态（播放/停止/编译）、截图查看游戏画面、执行 C# 脚本、管理场景/游戏对象/资产/脚本/着色器/UI Toolkit、读取控制台日志、执行菜单项、管理 UPM 包、烘焙 NavMesh/光照、设置 Game View 分辨率。只要涉及与 Unity Editor 交互的操作都应该参考此 skill。触发关键词包括但不限于：Unity、预制体、prefab、场景、scene、游戏对象、GameObject、截图、screenshot、编译、compile、控制台、console、Play Mode、播放模式、资产、Asset、材质、Material、着色器、Shader、NavMesh、烘焙、Bake、UPM、包、package。
---

# Codely CLI Unity Editor 工具集

Codely CLI 通过 TCP Bridge 连接 Unity Editor，提供 17 个内置工具。所有工具都在 Unity Editor 进程内执行，能直接访问运行时对象。

## 外部工具入口（v2）

外部 AI/脚本环境没有 Codely CLI 内置的 `unity_editor`、`unity_screenshot` 等一等工具时，优先使用本 skill 自带的客户端：

```bash
python .agents/skills/unity-tools/scripts/unity_bridge_cli.py tools
python .agents/skills/unity-tools/scripts/unity_bridge_cli.py config
python .agents/skills/unity-tools/scripts/unity_bridge_cli.py state
python .agents/skills/unity-tools/scripts/unity_bridge_cli.py call unity_editor "{\"action\":\"get_current_state\"}"
```

多行 JSON 或 C# 不要塞进 shell 参数；用 stdin 或文件，避免 PowerShell/批处理转义破坏内容：

```bash
python .agents/skills/unity-tools/scripts/unity_bridge_cli.py json < request.json
python .agents/skills/unity-tools/scripts/unity_bridge_cli.py exec --stdin < script.cs
python .agents/skills/unity-tools/scripts/unity_bridge_cli.py exec-file C:\tmp\script.cs
```

脚本结构：
- `scripts/unity_bridge_client.py`：可复用 TCP 客户端，负责端口发现、握手、8 字节帧、重试和响应解包。
- `scripts/unity_bridge_cli_main.py`：命令行实现。
- `scripts/unity_bridge_cli.py`：兼容旧路径的入口。
- `references/tool-map.json`：机器可读的工具名到 Bridge `type` 映射。
- `references/external-bridge.md`：协议、同步文件清单和排障说明。

同步到其它项目时只拷贝本 skill 目录内上述文件；不要把 `.codely.packages` 当作 skill 的一部分同步。目标项目必须已经安装并运行 `cn.tuanjie.codely.bridge`。

## 连接机制（Bridge 架构）

### 底层原理

Unity Editor 侧安装了 `cn.tuanjie.codely.bridge` 包，包内含原生 TCP Server（Windows: `NativeTcpBridge.dll`，macOS: `libNativeTcpBridge.dylib`，Linux: `libNativeTcpBridge.so`）。该 Server 在 Unity Editor 启动时由 `[InitializeOnLoad]` 自动拉起，监听一个 OS 动态分配的本地端口。

### 端口发现

端口信息写入项目根目录的 `.com-unity-codely.json` 文件：

```json
{
  "unity_port": 62116,
  "stream_port": 56493,
  "created_date": "2026-07-01T08:04:49Z",
  "project_path": "D:/Project/.../Assets",
  "reloading": false,
  "reason": "ready",
  "seq": 1,
  "last_heartbeat": "2026-07-07T07:16:39Z"
}
```

### 通信协议

基于 TCP 的 JSON 帧协议，单帧最大 64MB。握手时 Bridge 发送欢迎消息：
```
WELCOME UNITY-TCP 1 FRAMING=1 SERVER_VERSION=2
```
之后每条消息格式为：8字节大端长度 + UTF-8 JSON payload。

### 心跳与编译

Bridge 每 0.5 秒写一次心跳。如果 `reloading: true`，说明 Unity 正在编译，命令会超时。编译后端口可能变更，需重新读取配置。

### 单客户端限制

Bridge 同一时刻只允许一个 TCP 客户端连接。如果 Codely CLI 正在运行并占用连接，外部工具的脚本会收到 `ConnectionResetError`（连接被强制关闭）。

**解决方案：**
- 外部 AI 工具调用 `unity_bridge_cli.py` 时，确保 Codely CLI 没有正在调用 Unity 工具
- 每次调用是独立连接（connect → send → recv → close），不保持长连接
- 如果频繁断开，检查是否有其他进程占用了该端口

## 调用方式（两种环境）

### 方式 A：Codely CLI 内部（有内置工具）

Codely CLI 会话中已注册了 17 个 Unity 工具（`unity_editor`、`unity_screenshot` 等），直接调用即可。

### 方式 B：外部 AI 工具（只有 shell 访问）

外部 AI 工具没有 Codely CLI 的内置工具，需要通过本 skill 附带的 Python 脚本 `scripts/unity_bridge_cli.py` 间接调用。所有操作通过 `run_shell_command` 执行。

#### 快速验证连接

```bash
python .agents/skills/unity-tools/scripts/unity_bridge_cli.py state
```

#### 简短别名（常用操作）

| 别名 | 说明 |
|------|------|
| `state` | 读取编辑器状态 |
| `play` | 进入播放模式 |
| `stop` | 退出播放模式 |
| `idle` | 等待空闲 |
| `compile` | 编译并检查错误 |
| `screenshot` | Game View 截图 |
| `scene_shot` | Scene View 截图 |
| `console` | 读控制台日志 |
| `console_err` | 只读错误 |
| `clear` | 清空控制台 |
| `hierarchy` | 场景层级 |
| `save_scene` | 保存场景 |

#### 执行 C# 代码

```bash
python .agents/skills/unity-tools/scripts/unity_bridge_cli.py exec "UnityEngine.Debug.Log(\"hello\");"
```

或从文件执行：
```bash
python .agents/skills/unity-tools/scripts/unity_bridge_cli.py exec_file /path/to/script.cs
```

#### 原始命令（覆盖所有工具）

```bash
python unity_bridge_cli.py manage_editor '{"action":"get_current_state"}'
python unity_bridge_cli.py manage_gameobject '{"action":"find","searchTerm":"Player"}'
python unity_bridge_cli.py manage_asset '{"action":"search","path":"Assets/","search_pattern":"*.mat"}'
python unity_bridge_cli.py manage_scene '{"action":"get_hierarchy"}'
python unity_bridge_cli.py manage_script '{"action":"read","name":"MyScript","path":"Assets/Scripts"}'
```

所有命令返回 JSON，`success` 字段表示成功与否。

#### 环境变量

如果项目根目录不在当前工作目录或其上级：
```bash
set CODELY_PROJECT_ROOT=D:\Project\footballmaster2-20221128
python unity_bridge_cli.py state
```

#### 常见工作流（外部 AI 用法）

**修改脚本并验证：**
```bash
python unity_bridge_cli.py compile
python unity_bridge_cli.py console_err
```

**截图验证界面：**
```bash
python unity_bridge_cli.py screenshot
# 读取返回的图片路径，用图片分析工具查看
```

**Play Mode 调试：**
```bash
python unity_bridge_cli.py save_scene
python unity_bridge_cli.py play
timeout 5
python unity_bridge_cli.py screenshot
python unity_bridge_cli.py console
python unity_bridge_cli.py stop
```

## 核心原则

1. **State → Action → Verify → Correct**：先读状态，再操作，再验证，有问题就修正
2. **Play Mode 写保护**：Play Mode 下避免写操作（除非明确需要调试 Play Mode 行为）
3. **脚本修改后必须编译验证**：改完 .cs 文件后必须 `compile_and_validate`，检查 console 有无报错
4. **Console 读新旧分离**：操作前记录 `sinceToken`，操作后用它读增量日志，避免混淆旧错误

## 工具速查表

| 工具 | 用途 | 关键参数 |
|------|------|----------|
| `unity_editor` | 编辑器状态、编译、标签/图层 | `action: get_current_state / play / stop / wait_for_compile` |
| `unity_workflow` | 高级组合工作流 | `action: init_session / compile_and_validate / checkpoint` |
| `unity_scene` | 场景生命周期 | `action: create / load / save / get_hierarchy` |
| `unity_gameobject` | 游戏对象与组件 | `action: create / modify / delete / find / list_children` |
| `unity_asset` | 磁盘资产管理 | `action: search / get_info / create / modify / move` |
| `unity_script` | C# 脚本 CRUD | `action: create / read / update / delete / validate` |
| `unity_shader` | 着色器管理 | `action: detect_render_pipeline / create / read` |
| `unity_console` | 控制台日志 | `action: get / clear` |
| `unity_screenshot` | 截图 | `action: capture_game_view / capture_scene_view` |
| `unity_menu` | 菜单项执行 | `menu_path: "File/Save"` |
| `unity_package` | UPM 包管理 | `action: install_package / remove_package / list_packages` |
| `unity_bake` | NavMesh/光照烘焙 | `action: bake_navmesh / bake_lighting` |
| `unity_ui_toolkit` | UI Toolkit 资产 | `action: ensure_panel_settings / create_uxml / create_uss` |
| `unity_gameview` | Game View 分辨率 | `action: get_resolution / set_resolution` |
| `execute_csharp_script` | 内联 C# 执行 | `script: "C# 代码"` 或 `script: "/path/to/file.cs"` |
| `execute_custom_tool` | 自定义工具 | `tool_name: "注册的工具名"` |
| `unity_refresh` | 重连 Bridge | 无参数，调用 `{}` |

---

## 1. 会话初始化

每次开始 Unity 相关工作前，先初始化会话：

```
unity_workflow → action: "init_session"
```

等价于：`get_current_state` → `clear console` → `wait_for_idle`。返回状态快照 + console sinceToken。

---

## 2. 编辑器状态控制（unity_editor）

### 读取状态
```
action: "get_current_state"
```
返回完整快照：playMode、isCompiling、场景路径、dirty 状态、SRP 类型等。

### Play Mode 控制
```
action: "play"     // 进入播放模式
action: "stop"     // 退出播放模式
action: "pause"    // 暂停
```
进入 Play Mode 前需确保场景已保存（`unity_scene → ensure_scene_saved`）。

### 编译控制
```
action: "start_compilation_pipeline"  // 返回 op_id + since_token
action: "wait_for_compile"            // 参数: op_id
action: "wait_for_idle"                // 等待编辑器空闲
```

推荐用 `unity_workflow → compile_and_validate` 一步完成编译+检查。

### 标签和图层
```
action: "ensure_tag"   // tagName: "Enemy"
action: "get_tags"
action: "ensure_layer" // layerName: "UI"
action: "get_layers"
```

---

## 3. 高级工作流（unity_workflow）

| action | 作用 |
|--------|------|
| `init_session` | 状态快照 + 清控制台 + 等待空闲 |
| `compile_and_validate` | 启动编译 + 等待完成 + 读新日志 |
| `checkpoint` | 保存场景 + 可选截图 |
| `install_package_and_validate` | 安装 UPM 包 + 等待 + 编译验证 |

---

## 4. 场景管理（unity_scene）

```
action: "create"     // name, path
action: "load"        // name 或 path 或 build_index
action: "save"
action: "ensure_scene_open"   // path: 完整路径
action: "ensure_scene_saved"
action: "get_hierarchy"        // 返回场景根对象列表
action: "get_active"
action: "get_build_settings"
```

注意：场景文件 Unity 引擎用 `.unity`，Tuanjie 引擎用 `.scene`。搜索时两种都要查。

---

## 5. 游戏对象管理（unity_gameobject）

### 创建
```
action: "create"
name: "MyObject"
primitiveType: "Cube"        // 可选: Cube/Sphere/Capsule/Cylinder/Plane/Quad
position: [0, 0, 0]
rotation: [0, 0, 0]
scale: [1, 1, 1]
parent: "ParentName"         // 可选
componentsToAdd: ["Rigidbody"] // 可选
saveAsPrefab: true            // 可选，保存为预制体
prefabPath: "Assets/Prefabs/My.prefab"
```

### 查找
```
action: "find"
searchTerm: "Player"
searchMethod: "by_name"      // by_name/by_id/by_path/by_tag/by_layer/by_component
findAll: true                  // 返回所有匹配
searchInChildren: true
searchInactive: true
```

### 修改
```
action: "modify"
target: "Player" 或 targetRef: {id: 12345}
name: "NewName"
position: [1, 2, 3]
componentsToAdd: ["BoxCollider"]
componentsToRemove: ["Rigidbody"]
```

### 组件属性
```
action: "set_component_property"
target: "Player"
componentName: "Rigidbody"
propertyName: "mass"
propertyValue: 10
```
批量设置：
```
action: "set_component_properties"
target: "Player"
componentProperties: {"Rigidbody": {"mass": 10, "useGravity": true}}
```

### 子节点遍历
```
action: "list_children"
target: "Parent"
depth: 3                      // 遍历深度
```

### 批量操作
```
action: "create_batch"   // 纯写入，确定性
action: "edit_batch"     // 查找后写入，0匹配→提前停止
```
使用 `captureAs` 捕获实例引用，后续操作用 `$alias` 引用。

---

## 6. 资产管理（unity_asset）

### 搜索
```
action: "search"
path: "Assets/Materials"      // 搜索范围
search_pattern: "*.mat"
filter_type: "Material"
```

### 创建
```
action: "create"
asset_type: "Material"         // Material/PhysicsMaterial/Folder/ScriptableObject
path: "Assets/Materials/My.mat"
properties: {color: [1,0,0,1], shader: "Standard"}
```

### 修改预制体
```
action: "modify"
path: "Assets/Prefabs/Player.prefab"
properties: {"MyComponent": {"someField": 123}}
```

### 移动/重命名/复制
```
action: "move"      // destination: 完整目标路径
action: "rename"   // destination: 完整目标路径
action: "duplicate" // destination: 完整目标路径
```

### Meta 文件
```
action: "ensure_has_meta"       // 确保 .meta 存在
action: "ensure_meta_integrity"  // 检查 meta 健康
```

---

## 7. C# 脚本管理（unity_script）

```
action: "create"
name: "MyScript"
path: "Assets/Scripts"
scriptType: "MonoBehaviour"   // MonoBehaviour/ScriptableObject/Editor/EditorWindow/Custom
namespace: "MyGame"
contents: "C# 代码"

action: "read"    // name, path
action: "update"  // 覆盖内容
action: "validate" // level: basic/standard/strict/comprehensive
action: "edit"     // 精确文本替换 (edits 数组)
```

脚本创建/修改后必须编译验证：
```
unity_workflow → action: "compile_and_validate"
```

---

## 8. 截图（unity_screenshot）

### Game View
```
action: "capture_game_view"
scale: 0.5                    // 分辨率缩放，默认0.5
as_gif: true                  // GIF 录制
frameCount: 30, fps: 25      // GIF 参数
```

### Scene View
```
action: "capture_scene_view"
view: "cardinal"             // cardinal/front/back/left/right/top/bottom/iso/all
render_mode: "shaded"        // shaded/wireframe/shadedwireframe
focus_object: {focusobject: "Root/Player"}  // 聚焦对象
```

### 相机截图
```
action: "capture_main_camera"
action: "capture_specific_camera"  // camera_name: "UICamera"
```

### 资产预览
```
action: "capture_asset"
assets: ["Assets/Prefabs/Hero.prefab"]
view: "cardinal"
```

---

## 9. 内联 C# 执行（execute_csharp_script）

在 Unity Editor 进程内编译执行 C# 代码，可访问 UnityEngine 和 UnityEditor 命名空间。

```
script: "UnityEngine.Debug.Log(\"Hello\");"
```

或执行文件：
```
script: "/absolute/path/to/script.cs"
```

### 常用模式

**遍历场景对象：**
```csharp
var gos = UnityEngine.Object.FindObjectsOfType<GameObject>();
foreach (var go in gos) {
    if (go.transform.parent == null)
        UnityEngine.Debug.Log("Root: " + go.name);
}
```

**修改预制体：**
```csharp
var root = UnityEditor.PrefabUtility.LoadPrefabContents("Assets/Prefab.prefab");
// 修改...
UnityEditor.PrefabUtility.SaveAsPrefabAsset(root, "Assets/Prefab.prefab");
UnityEditor.PrefabUtility.UnloadPrefabContents(root);
```

**检查组件属性：**
```csharp
var go = GameObject.Find("Player");
var rt = go.GetComponent<RectTransform>();
UnityEngine.Debug.Log($"pos={rt.anchoredPosition} size={rt.sizeDelta}");
```

### 注意事项
- Play Mode 下脚本仍然可以执行
- 修改场景后需 MarkSceneDirty + SaveScene
- 不能使用 Thread.Sleep 等阻塞操作
- 用 `System.Threading.Thread.Sleep(ms)` 做简单等待（在 Play Mode 下）

---

## 10. 控制台日志（unity_console）

### 清空
```
action: "clear"
scope: "all" 或 "errors_only"
```
返回 `sinceToken`，后续读取只看新日志。

### 读取
```
action: "get"
since_token: "上次的token"    // 增量读取
types: ["error", "warning", "log"]
count: 50
filterText: "关键词"
format: "plain" 或 "detailed" 或 "json"
includeStacktrace: true
```

---

## 11. Play Mode 内与游戏交互

在 Play Mode 下通过 `execute_csharp_script` 可以直接操作运行时对象：

```csharp
// 查找 UI 按钮
var btn = GameObject.Find("LoginBtn")?.GetComponent<UnityEngine.UI.Button>();
btn?.onClick.Invoke();  // 模拟点击

// 查找文本
var texts = UnityEngine.Object.FindObjectsOfType<UnityEngine.UI.Text>();
foreach (var t in texts) {
    if (t.text.Contains("巅峰"))
        UnityEngine.Debug.Log("Found: " + t.text);
}

// 等待
System.Threading.Thread.Sleep(3000);
```

### 典型导航流程
1. `unity_screenshot → capture_game_view` 看当前画面
2. `execute_csharp_script` 找到目标 UI 元素
3. `execute_csharp_script` 调用 `onClick.Invoke()` 或 `toggle.isOn = true`
4. `System.Threading.Thread.Sleep(ms)` 等待加载
5. `unity_screenshot → capture_game_view` 验证结果

---

## 12. 其他工具

### 着色器（unity_shader）
```
action: "detect_render_pipeline"  // 检测 SRP (builtin/urp/hdrp)
action: "ensure_material_shader_for_srp"  // 按 SRP 设置正确 shader
action: "create" / "read" / "update" / "delete"
```

### 烘焙（unity_bake）
```
action: "bake_navmesh"    // scope: active_scene/selection/whole_project
action: "bake_lighting"
action: "wait_for_bake"   // op_id, timeoutSeconds
action: "clear_navmesh" / "clear_baked_data"
```

### UPM 包（unity_package）
```
action: "install_package"  // id_or_url, version
action: "remove_package"
action: "list_packages"
action: "wait_for_upm"     // op_id
```

### UI Toolkit（unity_ui_toolkit）
```
action: "ensure_panel_settings_asset"
action: "create_uxml" / "create_uss"
action: "link_uss_to_uxml"
```

### Game View 分辨率（unity_gameview）
```
action: "get_resolution" / "set_resolution" / "list_resolutions"
```

### 菜单项（unity_menu）
```
action: "execute"
menu_path: "File/Save Project"
```
注意：不要用 `Assets/Reimport All` 等重量级操作。

### 重连 Bridge（unity_refresh）
当其他 Unity 工具因连接断开失败时调用：
```
unity_refresh → {}  // 无参数
```
然后重试原操作。

---

## 13. 常见工作流组合

### 修改脚本并验证
```
1. unity_script → update (修改 .cs)
2. unity_workflow → compile_and_validate
3. unity_console → get (since_token) (检查错误)
```

### 修改预制体并验证
```
1. execute_csharp_script → LoadPrefabContents, 修改, SaveAsPrefabAsset
2. unity_screenshot → capture_asset (预览)
```

### 换皮工作流
```
1. run_shell_command → 复制新素材覆盖旧文件
2. execute_csharp_script → AssetDatabase.Refresh()
3. unity_editor → play (进入 Play Mode)
4. execute_csharp_script → 导航到目标界面
5. unity_screenshot → capture_game_view (截图对比)
```

### Play Mode 调试
```
1. unity_scene → ensure_scene_saved
2. unity_editor → play
3. execute_csharp_script → 检查运行时状态
4. unity_screenshot → capture_game_view
5. unity_console → get (读取运行时日志)
6. unity_editor → stop
```

---

## 14. 注意事项

- **不要在 Play Mode 下做写操作**（预制体、资产修改等），除非明确需要调试
- **编译失败时不要继续操作**，先修完编译错误
- `execute_csharp_script` 的 `capture_logs` 参数可控制是否捕获日志
- 批量操作优先用 `create_batch` / `edit_batch`，减少工具调用次数
- 截图时用 `scale: 1` 获取全分辨率，默认 0.5 是为了节省 token
- 预制体修改后场景会被标记 dirty，需 `ensure_scene_saved` 保存
- 文件路径必须用绝对路径（`/` 开头），相对路径不支持

---

## 15. 错误排查与缺失安装环境

如果外部工具连接失败、目标项目缺少 Codely Bridge 安装环境、`.com-unity-codely.json` 不存在、TCP 握手异常、被 Codely CLI 占用连接位，或 `execute_csharp_script` 因 shell 引号/编码失败，先阅读 `references/troubleshooting.md`。

排查时优先使用只读命令：

```bash
python .agents/skills/unity-tools/scripts/unity_bridge_cli.py tools
python .agents/skills/unity-tools/scripts/unity_bridge_cli.py config
python .agents/skills/unity-tools/scripts/unity_bridge_cli.py state
```

不要在 Play Mode/domain reload 期间用 `refresh_unity`、`AssetDatabase.Refresh`、重新导入全项目来处理短暂断线。
