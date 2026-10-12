import os
import sys
import time

sys.stdout.reconfigure(encoding='utf-8')
from unity_mcp_client import UnityMCPClient

c = UnityMCPClient()
out_dir = "Evidence/T13_Bugfix_20261012/Screenshots"
os.makedirs(out_dir, exist_ok=True)

shots = [
    # (stage, angle, weather, filename)
    ("seedling", 0, "calm", "GameView_seedling_Calm_Orbit0.png"),
    ("young_tree", 0, "calm", "GameView_young_tree_Calm_Orbit0.png"),
    ("large_tree", 0, "calm", "GameView_large_tree_Calm_Orbit0.png"),
    ("large_tree", 60, "calm", "GameView_large_tree_Orbit_60deg.png"),
    ("large_tree", 120, "calm", "GameView_large_tree_Orbit_120deg.png"),
    ("large_tree", 180, "calm", "GameView_large_tree_Orbit_180deg.png"),
    ("large_tree", 270, "calm", "GameView_large_tree_Orbit_270deg.png"),
    ("large_tree", 0, "sunny", "GameView_large_tree_Weather_sunny.png"),
    ("large_tree", 0, "rainy", "GameView_large_tree_Weather_rainy.png"),
    ("large_tree", 0, "thunderstorm", "GameView_large_tree_Weather_thunderstorm.png"),
]

for stage, angle, weather, filename in shots:
    code = f"""
var b = UnityEngine.Object.FindAnyObjectByType<CayTheGioi.Bootstrap.GameBootstrapper>();
var tv = UnityEngine.Object.FindAnyObjectByType<CayTheGioi.Presentation.TreeView>();
var rig = UnityEngine.Object.FindAnyObjectByType<CayTheGioi.Presentation.OrbitCameraRig>();
var hud = UnityEngine.Object.FindAnyObjectByType<CayTheGioi.Presentation.FoundationHudView>();
var layout = UnityEngine.Object.FindAnyObjectByType<CayTheGioi.Presentation.ReferenceUiLayout>();

var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
rig.GetType().GetField("yaw", flags).SetValue(rig, {angle}f);
var visual = b.Config.FindVisual("{stage}");
b.State.run.stageId = "{stage}";
tv.ShowStage(visual);
rig.SetStagePose(visual.cameraDistance, visual.cameraPitch, visual.cameraTargetHeight);
rig.GetType().GetField("pitch", flags).SetValue(rig, visual.cameraPitch);
rig.GetType().GetMethod("ApplyPose", flags).Invoke(rig, null);

b.Weather.ForceWeather("{weather}", 600);
b.GetComponent<CayTheGioi.Presentation.WeatherView>().SendMessage("Update");

hud.SendMessage("Refresh");
hud.SendMessage("LateUpdate");
if (layout != null) layout.SendMessage("LateUpdate");

string path = "{out_dir}/{filename}";
UnityEngine.ScreenCapture.CaptureScreenshot(path);
return "Captured: " + path;
"""
    res = c.call_tool('execute_code', {'action': 'execute', 'code': code, 'safety_checks': False})
    print(f"[{filename}] Result:", res.get("data", {}).get("result"))
    time.sleep(0.4)

# Reset back to seedling calm
reset_code = """
var b = UnityEngine.Object.FindAnyObjectByType<CayTheGioi.Bootstrap.GameBootstrapper>();
var tv = UnityEngine.Object.FindAnyObjectByType<CayTheGioi.Presentation.TreeView>();
var rig = UnityEngine.Object.FindAnyObjectByType<CayTheGioi.Presentation.OrbitCameraRig>();
var hud = UnityEngine.Object.FindAnyObjectByType<CayTheGioi.Presentation.FoundationHudView>();
var vis = b.Config.FindVisual("seedling");
b.State.run.stageId = "seedling";
tv.ShowStage(vis);
rig.SetStagePose(vis.cameraDistance, vis.cameraPitch, vis.cameraTargetHeight);
b.Weather.ForceWeather("calm", 600);
hud.SendMessage("Refresh");
return "RESET_OK";
"""
c.call_tool('execute_code', {'action': 'execute', 'code': reset_code, 'safety_checks': False})
print("Screenshots capturing completed successfully!")
