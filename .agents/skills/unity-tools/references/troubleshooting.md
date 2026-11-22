# Codely Unity Bridge Troubleshooting

这份文档用于外部 AI/脚本环境调用 `unity-tools` 失败时排查安装环境和连接状态。优先从只读命令开始，不要通过刷新资源或重新编译来“修复”短暂断线。

## 快速判断链路

按顺序执行：

```bash
python .agents/skills/unity-tools/scripts/unity_bridge_cli.py tools
python .agents/skills/unity-tools/scripts/unity_bridge_cli.py config
python .agents/skills/unity-tools/scripts/unity_bridge_cli.py state
```

判断结果：

- `tools` 失败：本地 Python 路径、skill 文件或脚本同步不完整。
- `config` 失败：没有找到 `.com-unity-codely.json`，通常是 Unity Editor 未打开目标项目，或 Codely Bridge 包未安装/未启动。
- `config` 成功但 `state` 失败：端口存在但 TCP 连接失败，重点查 Bridge 进程、单客户端占用、domain reload、协议版本。
- `state` 成功但业务脚本失败：连接没问题，继续看 Unity Console、C# 脚本编译错误或运行时对象路径。

## 文件和安装环境

目标项目必须同时满足：

- Unity Editor 已打开目标工程。
- 项目安装了 `cn.tuanjie.codely.bridge` 包。
- 项目根目录能看到 `.com-unity-codely.json`。
- `.com-unity-codely.json` 内有有效的 `unity_port`。
- `last_heartbeat` 持续更新，`reloading` 不是 `true`。

如果 `.com-unity-codely.json` 缺失：

1. 确认 Unity 打开的是当前项目，而不是另一个同名目录或分支工作树。
2. 检查 `Packages/manifest.json` 是否引用 Codely Bridge 包。
3. 检查 `.codely.packages/cn.tuanjie.codely.bridge@*/Plugins/Win/NativeTcpBridge.dll` 是否存在。
4. 在 Unity Console 搜索 `NativeTcpBridge`、`UnityTcpBridge`、`Codely` 相关错误。
5. 不要在 Play Mode/domain reload 中调用 `AssetDatabase.Refresh` 或重新导入全项目。

## 常见错误

### Cannot find .com-unity-codely.json

含义：CLI 找不到 Bridge 端口配置。

处理：

- 从 Unity 项目根目录执行命令，或设置 `CODELY_PROJECT_ROOT`。
- 确认 Unity Editor 已完成启动和脚本编译。
- 确认 Codely Bridge 包安装在当前项目。

### Invalid unity_port

含义：配置文件存在，但端口字段缺失或无效。

处理：

- 等待 Unity Editor 恢复到 ready。
- 重新执行 `config`，观察 `last_heartbeat` 是否更新。
- 如果端口长期无效，重启 Unity Editor。

### Connection refused

含义：端口没有监听，或 Bridge 原生服务未启动。

处理：

- 看 `.com-unity-codely.json` 的 `last_heartbeat` 是否新鲜。
- 检查 Unity Console 是否有 Native DLL 加载失败。
- 检查是否打开了错误的 Unity 实例。

### Connection reset / closed before response

含义：Bridge 连接被关闭，常见原因是同一时刻已有 Codely CLI 或另一个外部客户端占用连接。

处理：

- 避免并发调用 Unity 工具。
- 每次调用保持 connect -> send -> recv -> close 的短连接模式。
- 等待 1-2 秒重试只读命令 `state`。

### Unexpected Unity TCP handshake

含义：连到的不是当前版本的 Codely Unity Bridge，或协议版本不匹配。

处理：

- 当前协议应包含 `WELCOME UNITY-TCP` 和 `FRAMING=1`。
- 当前帧格式是 8 字节大端长度，不是 4 字节。
- 重新读取 `.com-unity-codely.json`，确认没有连到旧端口。

### Timeout

含义：命令已发送但 Unity 没在超时时间内返回。

处理：

- 如果 Unity 正在编译、进入/退出 Play Mode、domain reload，等待后重试 `state`。
- `manage_screenshot` 是协程命令，截图或 GIF 可能比普通状态查询慢。
- 大型 `execute_csharp_script` 不要阻塞主线程，不要长时间 `Thread.Sleep`。

### C# script execution failed

含义：Bridge 可用，但传入的 C# 脚本编译或运行失败。

处理：

- 优先用 `exec --stdin` 或 `exec-file`，避免 shell 引号转义破坏代码。
- `exec-file` 只传 `.cs` 路径给 Unity，由 Unity 侧读取文件并做编码识别。
- 查看返回的 `message` 和捕获日志；必要时再读 Console。

## 安全边界

- 不要把 `.env`、`.key`、`.pem` 等敏感文件作为 `exec-file` 输入。
- 不要用这套工具执行删除工程目录、清空磁盘、批量破坏资源的命令。
- 不要在 Play Mode 稳定运行时用刷新/重导入动作处理连接问题。

## 只读恢复流程

当不确定 Unity 当前是否稳定时，只执行：

```bash
python .agents/skills/unity-tools/scripts/unity_bridge_cli.py config
python .agents/skills/unity-tools/scripts/unity_bridge_cli.py state
python .agents/skills/unity-tools/scripts/unity_bridge_cli.py console_err
```

这些命令用于观察状态，不会触发资源刷新或重新编译。
