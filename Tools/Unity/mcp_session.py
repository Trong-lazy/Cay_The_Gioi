"""Persistent session runner for Unity MCP over HTTP."""
import json
import sys
import time
import urllib.request
from pathlib import Path

BASE_URL = "http://127.0.0.1:8080/mcp"

class UnityMcpSession:
    def __init__(self):
        self.session_id = None
        self.request_id = 1
        self.initialize()

    def rpc(self, method, params, notify=False):
        payload = {"jsonrpc": "2.0", "method": method, "params": params}
        if not notify:
            payload["id"] = self.request_id
            self.request_id += 1
        headers = {
            "Content-Type": "application/json",
            "Accept": "application/json, text/event-stream",
        }
        if self.session_id:
            headers["Mcp-Session-Id"] = self.session_id
        req = urllib.request.Request(BASE_URL, json.dumps(payload).encode(), headers)
        with urllib.request.urlopen(req, timeout=120) as resp:
            self.session_id = resp.headers.get("Mcp-Session-Id", self.session_id)
            body = resp.read().decode("utf-8")
        if not body:
            return None
        if body.startswith("event:") or body.startswith("data:"):
            for line in body.splitlines():
                if line.startswith("data: "):
                    body = line[6:]
                    break
        try:
            return json.loads(body)
        except Exception:
            return body

    def initialize(self):
        self.rpc("initialize", {
            "protocolVersion": "2024-11-05",
            "capabilities": {},
            "clientInfo": {"name": "persistent-session", "version": "1"}
        })
        self.rpc("notifications/initialized", {}, notify=True)

    def activate_groups(self, groups):
        for g in groups:
            self.rpc("tools/call", {"name": "manage_tools", "arguments": {"action": "activate", "group": g}})

    def call_tool(self, name, arguments):
        return self.rpc("tools/call", {"name": name, "arguments": arguments})

    def read_resource(self, uri):
        return self.rpc("resources/read", {"uri": uri})

if __name__ == "__main__":
    session = UnityMcpSession()
    session.activate_groups(["core", "testing", "scripting_ext"])
    cmd = sys.argv[1] if len(sys.argv) > 1 else "state"
    if cmd == "state":
        res = session.read_resource("mcpforunity://editor/state")
        print(json.dumps(res, indent=2, ensure_ascii=False))
    elif cmd == "run_tests":
        mode = sys.argv[2] if len(sys.argv) > 2 else "EditMode"
        job = session.call_tool("run_tests", {"mode": mode, "init_timeout": 120000})
        print(json.dumps(job, indent=2, ensure_ascii=False))
        # Poll for completion if requested
        if len(sys.argv) > 3 and sys.argv[3] == "--wait":
            try:
                res_data = json.loads(job["result"]["content"][0]["text"])
                job_id = res_data["data"]["job_id"]
                for _ in range(60):
                    time.sleep(1)
                    poll = session.call_tool("get_test_job", {"job_id": job_id})
                    poll_data = json.loads(poll["result"]["content"][0]["text"])
                    status = poll_data["data"]["status"]
                    if status != "running":
                        print("Test job finished with status:", status)
                        print(json.dumps(poll_data, indent=2, ensure_ascii=False))
                        break
            except Exception as e:
                print("Error polling test job:", e)
    elif cmd == "get_test_job":
        job_id = sys.argv[2]
        res = session.call_tool("get_test_job", {"job_id": job_id})
        print(json.dumps(res, indent=2, ensure_ascii=False))
    elif cmd == "code":
        code = sys.argv[2]
        res = session.call_tool("execute_code", {"action": "execute", "code": code})
        print(json.dumps(res, indent=2, ensure_ascii=False))
