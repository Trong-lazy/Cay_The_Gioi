"""RPC client for the installed local Unity MCP. Optional third argument saves evidence."""
import json
import sys
import urllib.request
from pathlib import Path
sys.stdout.reconfigure(encoding="utf-8")
session = None
def rpc(method, params, request_id=1):
    global session
    payload = {"jsonrpc": "2.0", "method": method, "params": params}
    if request_id is not None: payload["id"] = request_id
    headers = {"Content-Type": "application/json", "Accept": "application/json, text/event-stream"}
    if session: headers["Mcp-Session-Id"] = session
    req = urllib.request.Request("http://127.0.0.1:8080/mcp", json.dumps(payload).encode(), headers)
    with urllib.request.urlopen(req, timeout=60) as response:
        session = response.headers.get("Mcp-Session-Id", session)
        body = response.read().decode()
    if not body: return None
    if body.startswith("event:") or body.startswith("data:"):
        body = next(line[6:] for line in body.splitlines() if line.startswith("data: "))
    return json.loads(body)
rpc("initialize", {"protocolVersion": "2024-11-05", "capabilities": {}, "clientInfo": {"name": "cay-t01-validation", "version": "1"}})
rpc("notifications/initialized", {}, None)
argument = sys.argv[2] if len(sys.argv) > 2 else "{}"
if argument.startswith("@"): argument = Path(argument[1:]).read_text(encoding="utf-8-sig")
result = rpc(sys.argv[1], json.loads(argument), 2)
encoded = json.dumps(result, ensure_ascii=False, indent=2)
if len(sys.argv) > 3:
    path = Path(sys.argv[3]); path.parent.mkdir(parents=True, exist_ok=True); path.write_text(encoded, encoding="utf-8")
print(encoded)
