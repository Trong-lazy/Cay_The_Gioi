from unity_mcp_client import UnityMCPClient

c = UnityMCPClient()
code = """
var b = UnityEngine.Object.FindAnyObjectByType<CayTheGioi.Bootstrap.GameBootstrapper>();
if (b == null) return "GameBootstrapper is NULL";
var tv = UnityEngine.Object.FindAnyObjectByType<CayTheGioi.Presentation.TreeView>();
if (tv == null) return "TreeView is NULL";
var rig = UnityEngine.Object.FindAnyObjectByType<CayTheGioi.Presentation.OrbitCameraRig>();
if (rig == null) return "OrbitCameraRig is NULL";
var audio = b.Audio;
if (audio == null) return "Audio is NULL";
var weather = b.Weather;
if (weather == null) return "Weather is NULL";
return "ALL COMPONENTS READY! Stage=" + b.State.run.stageId + ", Weather=" + weather.CurrentWeatherId;
"""
res = c.call_tool('execute_code', {'action': 'execute', 'code': code})
print("Component check:", res)
