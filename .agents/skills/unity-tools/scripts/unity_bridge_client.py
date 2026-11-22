#!/usr/bin/env python3
"""Reusable TCP client for the Codely Unity Bridge.

The bridge protocol is the same one used by Codely's own JS client:
connect -> read welcome line -> send 8-byte big-endian length + UTF-8 JSON
payload -> receive 8-byte big-endian length + UTF-8 JSON response -> close.
"""

from __future__ import annotations

import json
import os
import socket
import struct
import time
from dataclasses import dataclass
from datetime import datetime, timezone
from pathlib import Path
from typing import Any, Dict, Iterable, Optional, Tuple


DEFAULT_HOST = "127.0.0.1"
MAX_FRAME_BYTES = 64 * 1024 * 1024
HANDSHAKE_LIMIT = 512
CONFIG_NAME = ".com-unity-codely.json"

PUBLIC_TO_RAW: Dict[str, str] = {
    "unity_editor": "manage_editor",
    "unity_workflow": "manage_workflow",
    "unity_scene": "manage_scene",
    "unity_gameobject": "manage_gameobject",
    "unity_asset": "manage_asset",
    "unity_script": "manage_script",
    "unity_shader": "manage_shader",
    "unity_console": "read_console",
    "unity_screenshot": "manage_screenshot",
    "unity_menu": "execute_menu_item",
    "unity_package": "manage_package",
    "unity_bake": "manage_bake",
    "unity_ui_toolkit": "manage_ui_toolkit",
    "unity_gameview": "manage_gameview",
    "unity_window_bridge": "manage_window_bridge",
    "execute_csharp_script": "execute_csharp_script",
    "execute_custom_tool": "execute_custom_tool",
}

ALIASES: Dict[str, Tuple[str, Dict[str, Any]]] = {
    "state": ("unity_editor", {"action": "get_current_state"}),
    "play": ("unity_editor", {"action": "play"}),
    "pause": ("unity_editor", {"action": "pause"}),
    "stop": ("unity_editor", {"action": "stop"}),
    "idle": ("unity_editor", {"action": "wait_for_idle", "timeoutSeconds": 60}),
    "init": ("unity_workflow", {"action": "init_session"}),
    "compile": ("unity_workflow", {"action": "compile_and_validate"}),
    "checkpoint": ("unity_workflow", {"action": "checkpoint"}),
    "screenshot": ("unity_screenshot", {"action": "capture_game_view"}),
    "scene_shot": ("unity_screenshot", {"action": "capture_scene_view"}),
    "console": ("unity_console", {"action": "get", "count": 50}),
    "console_err": ("unity_console", {"action": "get", "types": ["error"], "count": 20}),
    "clear": ("unity_console", {"action": "clear"}),
    "hierarchy": ("unity_scene", {"action": "get_hierarchy"}),
    "save_scene": ("unity_scene", {"action": "ensure_scene_saved"}),
    "windows": ("unity_window_bridge", {"action": "list_windows"}),
    "gameview_resolution": ("unity_gameview", {"action": "get_resolution"}),
}


class UnityBridgeError(RuntimeError):
    """Base error for bridge connection and protocol failures."""


class UnityBridgeConfigError(UnityBridgeError):
    """Raised when a bridge config cannot be found or parsed."""


class UnityBridgeProtocolError(UnityBridgeError):
    """Raised when TCP protocol framing is invalid."""


@dataclass(frozen=True)
class BridgeConfig:
    path: Path
    host: str
    port: int
    stream_port: int
    project_path: str
    reloading: bool
    reason: str
    seq: int
    last_heartbeat: str

    @property
    def heartbeat_age_seconds(self) -> Optional[float]:
        if not self.last_heartbeat:
            return None
        value = self.last_heartbeat.replace("Z", "+00:00")
        try:
            stamp = datetime.fromisoformat(value)
        except ValueError:
            return None
        if stamp.tzinfo is None:
            stamp = stamp.replace(tzinfo=timezone.utc)
        return (datetime.now(timezone.utc) - stamp.astimezone(timezone.utc)).total_seconds()

    def as_dict(self) -> Dict[str, Any]:
        return {
            "config_path": str(self.path),
            "host": self.host,
            "port": self.port,
            "stream_port": self.stream_port,
            "project_path": self.project_path,
            "reloading": self.reloading,
            "reason": self.reason,
            "seq": self.seq,
            "last_heartbeat": self.last_heartbeat,
            "heartbeat_age_seconds": self.heartbeat_age_seconds,
        }


def _candidate_roots(start: Path) -> Iterable[Path]:
    cur = start.resolve()
    if cur.is_file():
        cur = cur.parent
    for parent in [cur, *cur.parents]:
        yield parent


def find_config_path(
    *,
    project_root: Optional[str] = None,
    config_path: Optional[str] = None,
    start_dir: Optional[Path] = None,
) -> Path:
    if config_path:
        path = Path(config_path).expanduser()
        if path.exists():
            return path.resolve()
        raise UnityBridgeConfigError(f"Bridge config not found: {path}")

    explicit_root = project_root or os.environ.get("CODELY_PROJECT_ROOT")
    if explicit_root:
        path = Path(explicit_root).expanduser() / CONFIG_NAME
        if path.exists():
            return path.resolve()
        raise UnityBridgeConfigError(f"Bridge config not found under CODELY_PROJECT_ROOT: {path}")

    start = start_dir or Path.cwd()
    for root in _candidate_roots(start):
        path = root / CONFIG_NAME
        if path.exists():
            return path.resolve()

    legacy = Path.home() / ".unity-tcp" / "unity-tcp-port.json"
    if legacy.exists():
        return legacy.resolve()

    raise UnityBridgeConfigError(
        f"Cannot find {CONFIG_NAME}. Run from the Unity project or set CODELY_PROJECT_ROOT."
    )


def load_config(
    *,
    project_root: Optional[str] = None,
    config_path: Optional[str] = None,
    host_override: Optional[str] = None,
) -> BridgeConfig:
    path = find_config_path(project_root=project_root, config_path=config_path)
    with path.open("r", encoding="utf-8") as handle:
        payload = json.load(handle)

    port = payload.get("unity_port") or payload.get("port")
    if not isinstance(port, int) or port <= 0 or port > 65535:
        raise UnityBridgeConfigError(f"Invalid unity_port in {path}: {port!r}")

    stream_port = payload.get("stream_port") or 0
    if not isinstance(stream_port, int):
        stream_port = 0

    host = host_override or payload.get("unity_host") or DEFAULT_HOST
    return BridgeConfig(
        path=path,
        host=str(host),
        port=port,
        stream_port=stream_port,
        project_path=str(payload.get("project_path") or ""),
        reloading=bool(payload.get("reloading")),
        reason=str(payload.get("reason") or ""),
        seq=int(payload.get("seq") or 0),
        last_heartbeat=str(payload.get("last_heartbeat") or ""),
    )


def resolve_command(command: str) -> Tuple[str, str]:
    if command in PUBLIC_TO_RAW:
        return command, PUBLIC_TO_RAW[command]
    if command in PUBLIC_TO_RAW.values():
        return command, command
    raise UnityBridgeConfigError(f"Unknown Unity tool or raw command type: {command}")


def recv_exactly(sock: socket.socket, size: int) -> bytes:
    chunks = []
    remaining = size
    while remaining > 0:
        chunk = sock.recv(min(remaining, 65536))
        if not chunk:
            raise UnityBridgeProtocolError(
                f"Connection closed while reading frame; missing {remaining} bytes."
            )
        chunks.append(chunk)
        remaining -= len(chunk)
    return b"".join(chunks)


def read_welcome(sock: socket.socket) -> str:
    chunks = []
    total = 0
    while total < HANDSHAKE_LIMIT:
        chunk = sock.recv(1)
        if not chunk:
            raise UnityBridgeProtocolError("Unity TCP connection closed during handshake.")
        chunks.append(chunk)
        total += len(chunk)
        if chunk == b"\n":
            break
    welcome = b"".join(chunks).decode("ascii", errors="replace").strip()
    if "WELCOME UNITY-TCP" not in welcome or "FRAMING=1" not in welcome:
        raise UnityBridgeProtocolError(f"Unexpected Unity TCP handshake: {welcome!r}")
    return welcome


def send_frame(sock: socket.socket, payload: Dict[str, Any]) -> None:
    data = json.dumps(payload, ensure_ascii=False, separators=(",", ":")).encode("utf-8")
    if len(data) > MAX_FRAME_BYTES:
        raise UnityBridgeProtocolError(f"Frame is too large: {len(data)} bytes.")
    sock.sendall(struct.pack(">Q", len(data)))
    sock.sendall(data)


def read_frame(sock: socket.socket) -> Dict[str, Any]:
    header = recv_exactly(sock, 8)
    size = struct.unpack(">Q", header)[0]
    if size == 0 or size > MAX_FRAME_BYTES:
        raise UnityBridgeProtocolError(f"Invalid response frame length: {size}")
    data = recv_exactly(sock, size)
    try:
        return json.loads(data.decode("utf-8"))
    except json.JSONDecodeError as exc:
        raise UnityBridgeProtocolError(f"Invalid JSON response: {exc}") from exc


def send_raw_command(
    config: BridgeConfig,
    command_type: str,
    params: Optional[Dict[str, Any]] = None,
    *,
    connect_timeout: float = 2.0,
    response_timeout: float = 120.0,
) -> Dict[str, Any]:
    params = params or {}
    with socket.create_connection((config.host, config.port), timeout=connect_timeout) as sock:
        sock.settimeout(response_timeout)
        sock.setsockopt(socket.IPPROTO_TCP, socket.TCP_NODELAY, 1)
        read_welcome(sock)
        send_frame(sock, {"type": command_type, "params": params})
        return read_frame(sock)


def normalize_response(
    raw: Dict[str, Any],
    *,
    command_type: Optional[str] = None,
    public_tool: Optional[str] = None,
    include_raw: bool = False,
) -> Dict[str, Any]:
    outer_success = raw.get("success") is not False
    inner = raw.get("data") if isinstance(raw.get("data"), dict) else None

    if inner and ("success" in inner or "message" in inner or "error" in inner):
        success = bool(inner.get("success", outer_success))
        message = inner.get("message") or inner.get("error") or raw.get("message")

        # ExecuteCSharpScript currently reports Roslyn failures as success with
        # a failure message. Normalize that for automation callers.
        if command_type == "execute_csharp_script" and isinstance(message, str):
            if message.lower().startswith("c# script execution failed"):
                success = False

        normalized: Dict[str, Any] = {
            "success": success,
            "message": message,
            "tool": public_tool or command_type,
            "type": command_type,
            "outer_success": outer_success,
            "tool_success": inner.get("success"),
        }
        for key in (
            "data",
            "state",
            "state_delta",
            "code",
            "error",
            "_mcp_status",
            "op_id",
            "poll_interval",
        ):
            if key in inner:
                normalized[key] = inner[key]
    else:
        normalized = {
            "success": outer_success,
            "message": raw.get("message") or raw.get("error"),
            "tool": public_tool or command_type,
            "type": command_type,
        }
        for key in ("data", "state", "state_delta", "code", "error"):
            if key in raw:
                normalized[key] = raw[key]

    if include_raw:
        normalized["raw"] = raw
    return normalized


class UnityBridgeClient:
    def __init__(
        self,
        *,
        project_root: Optional[str] = None,
        config_path: Optional[str] = None,
        host: Optional[str] = None,
        connect_timeout: float = 2.0,
        response_timeout: float = 120.0,
        retries: int = 1,
        retry_delay: float = 0.35,
    ) -> None:
        self.project_root = project_root
        self.config_path = config_path
        self.host = host
        self.connect_timeout = connect_timeout
        self.response_timeout = response_timeout
        self.retries = max(0, retries)
        self.retry_delay = retry_delay

    def config(self) -> BridgeConfig:
        return load_config(
            project_root=self.project_root,
            config_path=self.config_path,
            host_override=self.host,
        )

    def call_raw(self, command_type: str, params: Optional[Dict[str, Any]] = None) -> Dict[str, Any]:
        last_error: Optional[BaseException] = None
        for attempt in range(self.retries + 1):
            cfg = self.config()
            try:
                return send_raw_command(
                    cfg,
                    command_type,
                    params,
                    connect_timeout=self.connect_timeout,
                    response_timeout=self.response_timeout,
                )
            except (OSError, socket.timeout, TimeoutError, UnityBridgeProtocolError) as exc:
                last_error = exc
                if attempt >= self.retries:
                    break
                time.sleep(self.retry_delay * (attempt + 1))
        raise UnityBridgeError(str(last_error)) from last_error

    def call(
        self,
        tool_or_type: str,
        params: Optional[Dict[str, Any]] = None,
        *,
        include_raw: bool = False,
    ) -> Dict[str, Any]:
        public_tool, command_type = resolve_command(tool_or_type)
        raw = self.call_raw(command_type, params)
        return normalize_response(
            raw,
            command_type=command_type,
            public_tool=public_tool,
            include_raw=include_raw,
        )

    def call_alias(
        self,
        alias: str,
        extra_params: Optional[Dict[str, Any]] = None,
        *,
        include_raw: bool = False,
    ) -> Dict[str, Any]:
        if alias not in ALIASES:
            raise UnityBridgeConfigError(f"Unknown alias: {alias}")
        tool, params = ALIASES[alias]
        merged = dict(params)
        if extra_params:
            merged.update(extra_params)
        return self.call(tool, merged, include_raw=include_raw)


def build_tools_payload() -> Dict[str, Any]:
    return {
        "protocol": {
            "handshake": "WELCOME UNITY-TCP 1 FRAMING=1 SERVER_VERSION=2",
            "frame": "8-byte big-endian unsigned length + UTF-8 JSON payload",
            "request": {"type": "raw command type", "params": "object"},
            "max_frame_bytes": MAX_FRAME_BYTES,
        },
        "public_to_raw": PUBLIC_TO_RAW,
        "aliases": ALIASES,
    }
