import json
import os
import sys
import time

sys.stdout.reconfigure(encoding='utf-8')
from unity_mcp_client import UnityMCPClient

def main():
    c = UnityMCPClient()
    out_dir = os.path.join(os.path.dirname(os.path.dirname(os.path.abspath(__file__))), "Evidence", "T13_Bugfix_20261012", "Screenshots")
    os.makedirs(out_dir, exist_ok=True)

    print("=== CAPTURING GAME VIEWS WITH FULL HUD ===")

    # Ensure PlayMode
    play_state = c.call_tool('execute_code', {'action': 'execute', 'code': 'return UnityEngine.Application.isPlaying;'})
    if not play_state.get('data', {}).get('result'):
        print("Entering PlayMode...")
        c.call_tool('manage_editor', {'action': 'play'})
        time.sleep(3)

    capture_code = """
var b = UnityEngine.Object.FindAnyObjectByType<CayTheGioi.Bootstrap.GameBootstrapper>();
var tv = UnityEngine.Object.FindAnyObjectByType<CayTheGioi.Presentation.TreeView>();
var rig = UnityEngine.Object.FindAnyObjectByType<CayTheGioi.Presentation.OrbitCameraRig>();
var hud = UnityEngine.Object.FindAnyObjectByType<CayTheGioi.Presentation.FoundationHudView>();
var layout = UnityEngine.Object.FindAnyObjectByType<CayTheGioi.Presentation.ReferenceUiLayout>();

var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
var yawField = rig.GetType().GetField("yaw", flags);
var pitchField = rig.GetType().GetField("pitch", flags);
var applyPose = rig.GetType().GetMethod("ApplyPose", flags);

// Đảm bảo HUD hiển thị
hud.SendMessage("Refresh");
hud.SendMessage("LateUpdate");
if (layout != null) layout.SendMessage("LateUpdate");

string outDir = "Evidence/T13_Bugfix_20261012/Screenshots";
System.IO.Directory.CreateDirectory(outDir);

var captures = new System.Collections.Generic.List<string>();

// 1. Chụp 3 stage tại góc nhìn mặc định (Orbit 0 deg)
foreach (var stageId in new[] { "seedling", "young_tree", "large_tree" })
{
    b.State.run.stageId = stageId;
    var visual = b.Config.FindVisual(stageId);
    tv.ShowStage(visual);
    rig.SetStagePose(visual.cameraDistance, visual.cameraPitch, visual.cameraTargetHeight);
    yawField.SetValue(rig, 0f);
    pitchField.SetValue(rig, visual.cameraPitch);
    applyPose.Invoke(rig, null);

    b.Weather.ForceWeather("calm", 600);
    hud.SendMessage("Refresh");

    string filename = $"{outDir}/GameView_{stageId}_Calm_Orbit0.png";
    UnityEngine.ScreenCapture.CaptureScreenshot(filename);
    captures.Add(filename);
}

// 2. Chụp large_tree tại các góc Orbit khác nhau (0, 60, 120 - góc từng bị che, 180, 270 deg)
b.State.run.stageId = "large_tree";
var visLarge = b.Config.FindVisual("large_tree");
tv.ShowStage(visLarge);
rig.SetStagePose(visLarge.cameraDistance, visLarge.cameraPitch, visLarge.cameraTargetHeight);

foreach (var angle in new[] { 60f, 120f, 180f, 270f })
{
    yawField.SetValue(rig, angle);
    pitchField.SetValue(rig, visLarge.cameraPitch);
    applyPose.Invoke(rig, null);
    hud.SendMessage("Refresh");

    string filename = $"{outDir}/GameView_large_tree_Orbit_{angle}deg.png";
    UnityEngine.ScreenCapture.CaptureScreenshot(filename);
    captures.Add(filename);
}

// 3. Chụp 4 trạng thái thời tiết ở large_tree
foreach (var wId in new[] { "sunny", "rainy", "thunderstorm" })
{
    b.Weather.ForceWeather(wId, 600);
    b.GetComponent<CayTheGioi.Presentation.WeatherView>().SendMessage("Update");
    yawField.SetValue(rig, 0f);
    pitchField.SetValue(rig, visLarge.cameraPitch);
    applyPose.Invoke(rig, null);
    hud.SendMessage("Refresh");

    string filename = $"{outDir}/GameView_large_tree_Weather_{wId}.png";
    UnityEngine.ScreenCapture.CaptureScreenshot(filename);
    captures.Add(filename);
}

// Reset lại về seedling calm
b.State.run.stageId = "seedling";
var visSeed = b.Config.FindVisual("seedling");
tv.ShowStage(visSeed);
rig.SetStagePose(visSeed.cameraDistance, visSeed.cameraPitch, visSeed.cameraTargetHeight);
b.Weather.ForceWeather("calm", 600);
hud.SendMessage("Refresh");

return string.Join(" | ", captures);
"""
    res = c.call_tool('execute_code', {'action': 'execute', 'code': capture_code, 'safety_checks': False})
    print("Capture result:", res)

if __name__ == "__main__":
    main()
