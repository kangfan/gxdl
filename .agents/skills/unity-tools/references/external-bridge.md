# External Codely Unity Bridge Client

This reference describes how external agents should call the Unity Editor tools
without relying on Codely CLI's in-process tool registration.

## Source Of Truth

- Unity side TCP router:
  `.codely.packages/cn.tuanjie.codely.bridge@1.0.66/Editor/Bridge/UnityTcpBridge.cs`
- Native queue wrapper:
  `.codely.packages/cn.tuanjie.codely.bridge@1.0.66/Editor/Bridge/Native/NativeUnityTcpBridgeHost.cs`
- Codely external JS client example:
  `.codely-cli/extensions/codely-unity-lsp-server/dist/setup/unityTcpProjectGeneration.js`
- Machine-readable sync map:
  `references/tool-map.json`

## Protocol

1. Read `.com-unity-codely.json` from the Unity project root.
2. Connect to `unity_port` on `127.0.0.1` unless the config has `unity_host`.
3. Read the welcome line. It must contain `WELCOME UNITY-TCP` and `FRAMING=1`.
4. Send one frame: 8-byte big-endian unsigned payload length, followed by UTF-8 JSON.
5. Request JSON shape: `{"type":"manage_editor","params":{"action":"get_current_state"}}`.
6. Read one response frame using the same 8-byte length header.
7. Close the socket after each call.

The bridge currently allows only one TCP client at a time. External tooling should
avoid parallel calls and should retry short connection-reset windows.

## CLI Entry Points

Preferred commands:

```bash
python .agents/skills/unity-tools/scripts/unity_bridge_cli.py tools
python .agents/skills/unity-tools/scripts/unity_bridge_cli.py config
python .agents/skills/unity-tools/scripts/unity_bridge_cli.py call unity_editor "{\"action\":\"get_current_state\"}"
python .agents/skills/unity-tools/scripts/unity_bridge_cli.py raw manage_editor "{\"action\":\"get_current_state\"}"
```

For multi-line JSON, avoid shell quoting and use stdin:

```bash
python .agents/skills/unity-tools/scripts/unity_bridge_cli.py json < request.json
```

`request.json`:

```json
{
  "tool": "unity_editor",
  "params": {
    "action": "get_current_state"
  }
}
```

For C# scripts, prefer stdin or script files:

```bash
python .agents/skills/unity-tools/scripts/unity_bridge_cli.py exec --stdin < script.cs
python .agents/skills/unity-tools/scripts/unity_bridge_cli.py exec-file C:\tmp\script.cs
```

`exec-file` passes `script_path` to Unity, so Unity uses its own smart file
encoding detection. The CLI refuses non-`.cs` files and sensitive suffixes such
as `.env`, `.key`, and `.pem`.

## Response Shape

UnityTcpBridge wraps every tool result once:

```json
{
  "success": true,
  "message": "Command executed successfully",
  "data": {
    "success": true,
    "message": "Retrieved full Unity state snapshot.",
    "state": {}
  }
}
```

The CLI prints a normalized response by default:

```json
{
  "success": true,
  "message": "Retrieved full Unity state snapshot.",
  "tool": "unity_editor",
  "type": "manage_editor",
  "state": {}
}
```

Use `--raw-output` to inspect the original Bridge response, or `--include-raw`
to include it inside the normalized response.

## Sync Package

To copy this skill to another project, sync only these files:

- `SKILL.md`
- `references/external-bridge.md`
- `references/troubleshooting.md`
- `references/tool-map.json`
- `scripts/unity_bridge_client.py`
- `scripts/unity_bridge_cli_main.py`
- `scripts/unity_bridge_cli.py`

Do not copy `.codely.packages` as part of this skill. The target Unity project
must already have the Codely Bridge package installed and running in the Editor.

## Tool Mapping

Public names mirror Codely CLI tool names. TCP `type` values are the Unity-side
router names.

| Public name | Raw Bridge type |
| --- | --- |
| `unity_editor` | `manage_editor` |
| `unity_workflow` | `manage_workflow` |
| `unity_scene` | `manage_scene` |
| `unity_gameobject` | `manage_gameobject` |
| `unity_asset` | `manage_asset` |
| `unity_script` | `manage_script` |
| `unity_shader` | `manage_shader` |
| `unity_console` | `read_console` |
| `unity_screenshot` | `manage_screenshot` |
| `unity_menu` | `execute_menu_item` |
| `unity_package` | `manage_package` |
| `unity_bake` | `manage_bake` |
| `unity_ui_toolkit` | `manage_ui_toolkit` |
| `unity_gameview` | `manage_gameview` |
| `unity_window_bridge` | `manage_window_bridge` |
| `execute_csharp_script` | `execute_csharp_script` |
| `execute_custom_tool` | `execute_custom_tool` |
