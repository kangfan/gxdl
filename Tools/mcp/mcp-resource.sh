#!/usr/bin/env bash
# 用法: mcp-resource.sh <resource uri>
set -u
BASE="http://127.0.0.1:8080/mcp"
URI="${1:?resource uri required}"
HEADERS=$(mktemp); BODY=$(mktemp)
curl -s -D "$HEADERS" -o "$BODY" -m 10 -X POST "$BASE" \
  -H "Content-Type: application/json" -H "Accept: application/json, text/event-stream" \
  -d '{"jsonrpc":"2.0","id":1,"method":"initialize","params":{"protocolVersion":"2025-06-18","capabilities":{},"clientInfo":{"name":"gxdl-agent","version":"1.0"}}}'
SID=$(grep -i '^mcp-session-id:' "$HEADERS" | tr -d '\r' | awk '{print $2}')
if [ -z "$SID" ]; then echo '{"error":"no session id"}'; exit 1; fi
curl -s -o /dev/null -m 10 -X POST "$BASE" -H "Content-Type: application/json" \
  -H "Accept: application/json, text/event-stream" -H "mcp-session-id: $SID" \
  -d '{"jsonrpc":"2.0","method":"notifications/initialized"}'
curl -s -m 30 -X POST "$BASE" -H "Content-Type: application/json" \
  -H "Accept: application/json, text/event-stream" -H "mcp-session-id: $SID" \
  -d "{\"jsonrpc\":\"2.0\",\"id\":2,\"method\":\"resources/read\",\"params\":{\"uri\":\"$URI\"}}" |
  tr -d '\r' | grep '^data:' | sed 's/^data: //'
rm -f "$HEADERS" "$BODY"
