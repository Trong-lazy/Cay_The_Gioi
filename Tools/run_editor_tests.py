import json
from unity_mcp_client import UnityMCPClient

client = UnityMCPClient()

def run_csharp(code_str):
    res = client.call_tool("execute_code", {"code": code_str})
    return res

if __name__ == "__main__":
    code = """
Debug.Log("[MCP_BRIDGE] Live bridge test OK!");
"""
    print(run_csharp(code))
