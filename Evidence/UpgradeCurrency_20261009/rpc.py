"""Evidence-producing client for the existing local Unity MCP server."""
import json
import sys
import urllib.request
from pathlib import Path

sys.stdout.reconfigure(encoding="utf-8")
session = None

def rpc(method, params, request_id):
    global session
    payload = {"jsonrpc": "2.0", "method": method, "params": params}
    if request_id is not None:
        payload["id"] = request_id
    headers = {"Content-Type": "application/json", "Accept": "application/json, text/event-stream"}
    if session:
        headers["Mcp-Session-Id"] = session
    request = urllib.request.Request("http://127.0.0.1:8080/mcp", json.dumps(payload).encode(), headers)
    with urllib.request.urlopen(request, timeout=60) as response:
        session = response.headers.get("Mcp-Session-Id", session)
        body = response.read().decode()
    if not body:
        return None
    if any(line.startswith("data: ") for line in body.splitlines()):
        messages = [json.loads(line[6:]) for line in body.splitlines() if line.startswith("data: ")]
        return next((item for item in messages if item.get("id") == request_id), messages[-1])
    return json.loads(body)

rpc("initialize", {"protocolVersion": "2024-11-05", "capabilities": {}, "clientInfo": {"name": "upgrade-currency-validation", "version": "1"}}, 1)
rpc("notifications/initialized", {}, None)
params = json.loads(Path(sys.argv[2][1:]).read_text(encoding="utf-8-sig")) if sys.argv[2].startswith("@") else json.loads(sys.argv[2])
result = rpc(sys.argv[1], params, 2)
encoded = json.dumps(result, ensure_ascii=False, indent=2)
if len(sys.argv) > 3:
    Path(sys.argv[3]).write_text(encoded, encoding="utf-8")
print(encoded)
