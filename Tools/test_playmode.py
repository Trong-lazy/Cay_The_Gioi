from unity_mcp_client import UnityMCPClient
import time

c = UnityMCPClient()
code = """
UnityEditor.EditorApplication.isPlaying = true;
return UnityEditor.EditorApplication.isPlaying;
"""
res = c.call_tool('execute_code', {'action': 'execute', 'code': code})
print("Set isPlaying result:", res)

for i in range(5):
    time.sleep(1)
    check = c.call_tool('execute_code', {'action': 'execute', 'code': 'return "isPlaying=" + UnityEngine.Application.isPlaying + ", isCompiling=" + UnityEditor.EditorApplication.isCompiling + ", isPaused=" + UnityEditor.EditorApplication.isPaused;'})
    print(f"Check {i+1}:", check)
