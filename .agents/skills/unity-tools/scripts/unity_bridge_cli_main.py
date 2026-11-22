#!/usr/bin/env python3
"""Command line entrypoint for the Codely Unity Bridge client."""

from __future__ import annotations

import argparse
import json
import sys
from pathlib import Path
from typing import Any, Dict, List, Optional

from unity_bridge_client import (
    ALIASES,
    PUBLIC_TO_RAW,
    UnityBridgeClient,
    UnityBridgeError,
    build_tools_payload,
    normalize_response,
    resolve_command,
)


SENSITIVE_SUFFIXES = (".env", ".key", ".pem")


def parse_json_arg(text: Optional[str]) -> Dict[str, Any]:
    if not text:
        return {}
    if text == "-":
        text = sys.stdin.read()
    value = json.loads(text)
    if not isinstance(value, dict):
        raise ValueError("Params JSON must be an object.")
    return value


def print_json(payload: Dict[str, Any], *, compact: bool = False) -> None:
    if compact:
        print(json.dumps(payload, ensure_ascii=False, separators=(",", ":")))
    else:
        print(json.dumps(payload, ensure_ascii=False, indent=2))


def reject_sensitive_path(path: Path) -> None:
    lowered = path.name.lower()
    if lowered in SENSITIVE_SUFFIXES or lowered.endswith(SENSITIVE_SUFFIXES):
        raise ValueError(f"Refusing to pass sensitive path to Unity: {path}")


def make_client(args: argparse.Namespace) -> UnityBridgeClient:
    return UnityBridgeClient(
        project_root=args.project_root,
        config_path=args.config,
        host=args.host,
        connect_timeout=args.connect_timeout,
        response_timeout=args.timeout,
        retries=args.retries,
    )


def emit_result(args: argparse.Namespace, result: Dict[str, Any]) -> int:
    print_json(result, compact=args.compact)
    if not result.get("success", False) and not args.no_fail:
        return 2
    return 0


def correct_game_view_orientation(result: Dict[str, Any]) -> Dict[str, Any]:
    """Correct the vertically inverted Game View capture produced by Unity OpenGL ES."""
    if not result.get("success", False):
        return result
    data = result.get("data")
    if not isinstance(data, dict) or not data.get("path"):
        return result

    image_path = Path(str(data["path"]))
    if not image_path.is_file():
        return result

    from PIL import Image

    with Image.open(image_path) as image:
        corrected = image.transpose(Image.Transpose.FLIP_TOP_BOTTOM)
        corrected.save(image_path)
    data["orientation_corrected"] = "flip_vertical"
    return result


def run_alias_or_raw(args: argparse.Namespace, argv: List[str]) -> int:
    command = argv[0]
    params = parse_json_arg(argv[1]) if len(argv) > 1 else {}
    client = make_client(args)

    if command in ALIASES:
        result = client.call_alias(command, params, include_raw=args.include_raw)
        if command == "screenshot":
            result = correct_game_view_orientation(result)
        return emit_result(args, result)

    if command in PUBLIC_TO_RAW or command in PUBLIC_TO_RAW.values():
        public_tool, raw_type = resolve_command(command)
        raw = client.call_raw(raw_type, params)
        result = raw if args.raw_output else normalize_response(
            raw,
            command_type=raw_type,
            public_tool=public_tool,
            include_raw=args.include_raw,
        )
        if public_tool == "unity_screenshot" and params.get("action") == "capture_game_view":
            result = correct_game_view_orientation(result)
        return emit_result(args, result)

    raise ValueError(f"Unknown command: {command}")


def command_call(args: argparse.Namespace) -> int:
    params = parse_json_arg(args.params)
    client = make_client(args)
    result = client.call(args.tool, params, include_raw=args.include_raw)
    if args.tool == "unity_screenshot" and params.get("action") == "capture_game_view":
        result = correct_game_view_orientation(result)
    return emit_result(args, result)


def command_raw(args: argparse.Namespace) -> int:
    params = parse_json_arg(args.params)
    client = make_client(args)
    raw = client.call_raw(args.type, params)
    result = raw if args.raw_output else normalize_response(
        raw,
        command_type=args.type,
        public_tool=args.type,
        include_raw=args.include_raw,
    )
    return emit_result(args, result)


def command_json(args: argparse.Namespace) -> int:
    payload = json.loads(sys.stdin.read())
    if not isinstance(payload, dict):
        raise ValueError("stdin JSON must be an object.")

    params = payload.get("params") or {}
    if not isinstance(params, dict):
        raise ValueError("'params' must be an object.")

    client = make_client(args)
    if payload.get("alias"):
        result = client.call_alias(str(payload["alias"]), params, include_raw=args.include_raw)
    elif payload.get("tool"):
        result = client.call(str(payload["tool"]), params, include_raw=args.include_raw)
    elif payload.get("type"):
        raw_type = str(payload["type"])
        raw = client.call_raw(raw_type, params)
        result = raw if args.raw_output else normalize_response(
            raw,
            command_type=raw_type,
            public_tool=raw_type,
            include_raw=args.include_raw,
        )
    else:
        raise ValueError("stdin JSON must contain 'alias', 'tool', or 'type'.")
    if (
        payload.get("alias") == "screenshot"
        or (
            payload.get("tool") == "unity_screenshot"
            and params.get("action") == "capture_game_view"
        )
    ):
        result = correct_game_view_orientation(result)
    return emit_result(args, result)


def command_exec(args: argparse.Namespace) -> int:
    if args.stdin:
        script = sys.stdin.read()
    elif args.script:
        script = args.script
    else:
        raise ValueError("Provide script text or use --stdin.")

    params: Dict[str, Any] = {
        "script": script,
        "capture_logs": not args.no_capture_logs,
    }
    if args.description:
        params["description"] = args.description

    client = make_client(args)
    result = client.call("execute_csharp_script", params, include_raw=args.include_raw)
    return emit_result(args, result)


def command_exec_file(args: argparse.Namespace) -> int:
    path = Path(args.path).expanduser()
    reject_sensitive_path(path)
    if path.suffix.lower() != ".cs":
        raise ValueError("exec-file only accepts .cs files.")
    if not path.exists():
        raise ValueError(f"Script file not found: {path}")

    params: Dict[str, Any] = {
        "script_path": str(path.resolve()),
        "capture_logs": not args.no_capture_logs,
    }
    if args.description:
        params["description"] = args.description

    client = make_client(args)
    result = client.call("execute_csharp_script", params, include_raw=args.include_raw)
    return emit_result(args, result)


def command_config(args: argparse.Namespace) -> int:
    client = make_client(args)
    return emit_result(args, {"success": True, "config": client.config().as_dict()})


def command_tools(args: argparse.Namespace) -> int:
    return emit_result(args, {"success": True, **build_tools_payload()})


def build_parser() -> argparse.ArgumentParser:
    parser = argparse.ArgumentParser(
        description="External client for Codely Unity Bridge.",
        formatter_class=argparse.ArgumentDefaultsHelpFormatter,
    )
    parser.add_argument("--project-root", help="Unity project root containing .com-unity-codely.json.")
    parser.add_argument("--config", help="Explicit path to .com-unity-codely.json.")
    parser.add_argument("--host", help="Override Unity TCP host.")
    parser.add_argument("--timeout", type=float, default=120.0, help="Response timeout in seconds.")
    parser.add_argument("--connect-timeout", type=float, default=2.0, help="Connect timeout in seconds.")
    parser.add_argument("--retries", type=int, default=1, help="Retry count for transient connection failures.")
    parser.add_argument("--include-raw", action="store_true", help="Include raw nested Bridge response.")
    parser.add_argument("--raw-output", action="store_true", help="Print raw Bridge response instead of normalized output.")
    parser.add_argument("--compact", action="store_true", help="Print compact JSON.")
    parser.add_argument("--no-fail", action="store_true", help="Do not return non-zero exit code for tool failure.")

    sub = parser.add_subparsers(dest="subcommand")

    call = sub.add_parser("call", help="Call a public tool name, e.g. unity_editor.")
    call.add_argument("tool")
    call.add_argument("params", nargs="?")
    call.set_defaults(func=command_call)

    raw = sub.add_parser("raw", help="Call a raw Bridge type, e.g. manage_editor.")
    raw.add_argument("type")
    raw.add_argument("params", nargs="?")
    raw.set_defaults(func=command_raw)

    stdin_json = sub.add_parser("json", help="Read {'tool'|'type'|'alias', 'params'} from stdin.")
    stdin_json.set_defaults(func=command_json)

    exec_cmd = sub.add_parser("exec", help="Execute inline C# in the Unity Editor process.")
    exec_cmd.add_argument("script", nargs="?")
    exec_cmd.add_argument("--stdin", action="store_true", help="Read C# script from stdin.")
    exec_cmd.add_argument("--description")
    exec_cmd.add_argument("--no-capture-logs", action="store_true")
    exec_cmd.set_defaults(func=command_exec)

    exec_file = sub.add_parser("exec-file", aliases=["exec_file"], help="Execute a .cs file by script_path.")
    exec_file.add_argument("path")
    exec_file.add_argument("--description")
    exec_file.add_argument("--no-capture-logs", action="store_true")
    exec_file.set_defaults(func=command_exec_file)

    config = sub.add_parser("config", help="Show discovered Bridge config.")
    config.set_defaults(func=command_config)

    tools = sub.add_parser("tools", help="Show tool mapping and aliases.")
    tools.set_defaults(func=command_tools)

    return parser


def split_legacy_argv(argv: List[str]) -> tuple[List[str], List[str]]:
    global_flags: List[str] = []
    command_parts: List[str] = []
    takes_value = {"--project-root", "--config", "--host", "--timeout", "--connect-timeout", "--retries"}

    i = 0
    while i < len(argv):
        item = argv[i]
        if item in takes_value:
            global_flags.append(item)
            if i + 1 >= len(argv):
                raise ValueError(f"Missing value for {item}")
            global_flags.append(argv[i + 1])
            i += 2
            continue
        if item in {"--include-raw", "--raw-output", "--compact", "--no-fail"}:
            global_flags.append(item)
            i += 1
            continue
        command_parts.append(item)
        i += 1
    return global_flags, command_parts


def main(argv: Optional[List[str]] = None) -> int:
    argv = list(sys.argv[1:] if argv is None else argv)
    parser = build_parser()

    known_subcommands = {"call", "raw", "json", "exec", "exec-file", "exec_file", "config", "tools"}
    global_flags, command_parts = split_legacy_argv(argv)
    if command_parts and command_parts[0] not in known_subcommands:
        args = parser.parse_args(global_flags)
        return run_alias_or_raw(args, command_parts)

    args = parser.parse_args(global_flags + command_parts)
    if not hasattr(args, "func"):
        parser.print_help()
        return 1
    return args.func(args)


if __name__ == "__main__":
    try:
        raise SystemExit(main())
    except (UnityBridgeError, OSError, ValueError, json.JSONDecodeError) as exc:
        print_json({"success": False, "error": str(exc)})
        raise SystemExit(2)
