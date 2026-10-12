from unity_mcp_client import UnityMCPClient

c = UnityMCPClient()
code = """
var b = UnityEngine.Object.FindAnyObjectByType<CayTheGioi.Bootstrap.GameBootstrapper>();
var tv = UnityEngine.Object.FindAnyObjectByType<CayTheGioi.Presentation.TreeView>();
var leaf = tv.CurrentStageAnchors.GetComponentsInChildren<UnityEngine.MeshFilter>().First(x=>x.name=="Foliage");
var comps = leaf.GetComponents<UnityEngine.Component>();
var names = new System.Collections.Generic.List<string>();
foreach(var comp in comps) names.Add(comp.GetType().Name);
return string.Join(", ", names);
"""
res = c.call_tool('execute_code', {'action': 'execute', 'code': code})
print("Foliage components:", res)
