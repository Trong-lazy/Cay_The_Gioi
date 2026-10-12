import json
import urllib.request
import sys

MCP_URL = "http://127.0.0.1:8080/mcp"

class UnityMCPClient:
    def __init__(self):
        self.session_id = None
        self._init_session()

    def _init_session(self):
        req = urllib.request.Request(
            MCP_URL,
            data=json.dumps({
                "jsonrpc": "2.0",
                "id": 1,
                "method": "initialize",
                "params": {
                    "protocolVersion": "2024-11-05",
                    "capabilities": {},
                    "clientInfo": {"name": "agent_client", "version": "1.0"}
                }
            }).encode("utf-8"),
            headers={"Content-Type": "application/json", "Accept": "application/json, text/event-stream"}
        )
        res = urllib.request.urlopen(req)
        self.session_id = res.headers.get("mcp-session-id")

        # Send initialized notification
        req_notif = urllib.request.Request(
            MCP_URL,
            data=json.dumps({"jsonrpc": "2.0", "method": "notifications/initialized"}).encode("utf-8"),
            headers={"Content-Type": "application/json", "Accept": "application/json, text/event-stream", "mcp-session-id": self.session_id}
        )
        urllib.request.urlopen(req_notif)

    def call_tool(self, tool_name, arguments=None):
        if arguments is None:
            arguments = {}
        req = urllib.request.Request(
            MCP_URL,
            data=json.dumps({
                "jsonrpc": "2.0",
                "id": 2,
                "method": "tools/call",
                "params": {"name": tool_name, "arguments": arguments}
            }).encode("utf-8"),
            headers={"Content-Type": "application/json", "Accept": "application/json, text/event-stream", "mcp-session-id": self.session_id}
        )
        res = urllib.request.urlopen(req)
        body = res.read().decode("utf-8")
        for line in body.splitlines():
            if line.startswith("data: "):
                data = json.loads(line[6:])
                result = data.get("result", {})
                content = result.get("content", [])
                if content and "text" in content[0]:
                    try:
                        return json.loads(content[0]["text"])
                    except Exception:
                        return content[0]["text"]
                return result
        return body

if __name__ == "__main__":
    client = UnityMCPClient()
    tool = sys.argv[1] if len(sys.argv) > 1 else "read_console"
    args = json.loads(sys.argv[2]) if len(sys.argv) > 2 else {}
    res = client.call_tool(tool, args)
    print(json.dumps(res, indent=2, ensure_ascii=False) if isinstance(res, (dict, list)) else res)
