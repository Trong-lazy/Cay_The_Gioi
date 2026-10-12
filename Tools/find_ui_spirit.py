import sys
sys.stdout.reconfigure(encoding='utf-8')
from unity_mcp_client import UnityMCPClient

c = UnityMCPClient()
code = """
var texts = UnityEngine.Object.FindObjectsByType<UnityEngine.UI.Text>(UnityEngine.FindObjectsInactive.Include, UnityEngine.FindObjectsSortMode.None);
var list = new System.Collections.Generic.List<string>();
foreach(var t in texts)
{
    if (t.text.Contains("Cây Trưởng") || t.text.Contains("Linh") || t.gameObject.name.Contains("Spirit"))
    {
        list.Add(t.gameObject.name + " (" + t.transform.parent.name + "): '" + t.text + "', overflow=" + t.horizontalOverflow + ", size=" + t.fontSize);
    }
}
return string.Join(" | ", list);
"""
res = c.call_tool('execute_code', {'action': 'execute', 'code': code})
print("UI search result:", res)
