from unity_mcp_client import UnityMCPClient

c = UnityMCPClient()
code = """
var roots = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects();
var names = new System.Collections.Generic.List<string>();
foreach(var r in roots) names.Add(r.name);
return string.Join(", ", names);
"""
res = c.call_tool('execute_code', {'action': 'execute', 'code': code})
print("Active scene roots:", res)

code_play = """
return "isPlaying=" + UnityEngine.Application.isPlaying + ", activeScene=" + UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
"""
res_play = c.call_tool('execute_code', {'action': 'execute', 'code': code_play})
print("Play state:", res_play)
