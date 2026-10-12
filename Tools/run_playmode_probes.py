import json
import os
import sys
import time

sys.path.append(os.path.dirname(os.path.abspath(__file__)))
from unity_mcp_client import UnityMCPClient

def main():
    client = UnityMCPClient()
    out_dir = os.path.join(os.path.dirname(os.path.dirname(os.path.abspath(__file__))), "Evidence", "T11_Fix_20261012")
    os.makedirs(out_dir, exist_ok=True)

    print("Checking active scene and PlayMode...")
    scene_info = client.call_tool("manage_scene", {"action": "get_active"})
    print("Scene:", scene_info)

    # 1. Probe Animation & Foliage Displacement & Raycast
    print("\n--- Running ANIMATION Probe ---")
    anim_code = """
var b = UnityEngine.Object.FindAnyObjectByType<CayTheGioi.Bootstrap.GameBootstrapper>();
var tv = UnityEngine.Object.FindAnyObjectByType<CayTheGioi.Presentation.TreeView>();
var anim = tv.GetComponent<CayTheGioi.Presentation.TreeAnimationController>();
var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
var rows = new System.Collections.Generic.List<object>();
foreach (var id in new[] { "seedling", "young_tree", "large_tree" }) {
    b.State.run.stageId = id;
    var visual = b.Config.FindVisual(id);
    tv.ShowStage(visual);
    UnityEngine.Object.FindAnyObjectByType<CayTheGioi.Presentation.OrbitCameraRig>().SetStagePose(visual.cameraDistance, visual.cameraPitch, visual.cameraTargetHeight);
    b.Weather.ForceWeather("calm", 600);
    anim.SendMessage("Update");
    var leaf = tv.CurrentStageAnchors.GetComponentsInChildren<UnityEngine.MeshFilter>().First(x => x.name == "Foliage");
    var trunk = tv.CurrentStageAnchors.GetComponentsInChildren<UnityEngine.MeshFilter>().First(x => x.name == "Trunk_Branches");
    var orig = (UnityEngine.Vector3[])anim.GetType().GetField("baseFoliageVertices", flags).GetValue(anim);
    var verts = leaf.sharedMesh.vertices;
    var tri = leaf.sharedMesh.triangles;
    var col = leaf.GetComponent<UnityEngine.MeshCollider>();
    float minMove = float.MaxValue, maxMove = 0;
    int miss = 0, offset = 0, checks = 0;
    float maxOffset = 0;
    for (int i = 0; i < verts.Length; i++) {
        float d = (verts[i] - orig[i]).magnitude;
        minMove = UnityEngine.Mathf.Min(minMove, d);
        maxMove = UnityEngine.Mathf.Max(maxMove, d);
    }
    for (int j = 0; j < tri.Length; j += 3 * System.Math.Max(1, tri.Length / 900)) {
        var a = leaf.transform.TransformPoint(verts[tri[j]]);
        var c = leaf.transform.TransformPoint(verts[tri[j + 1]]);
        var d = leaf.transform.TransformPoint(verts[tri[j + 2]]);
        var n = UnityEngine.Vector3.Cross(c - a, d - a).normalized;
        if (n.sqrMagnitude < 0.5f) continue;
        var center = (a + c + d) / 3f;
        UnityEngine.RaycastHit hit;
        checks++;
        if (!col.Raycast(new UnityEngine.Ray(center + n * 0.05f, -n), out hit, 0.1f)) miss++;
        else {
            float dist = UnityEngine.Vector3.Distance(hit.point, center);
            maxOffset = UnityEngine.Mathf.Max(maxOffset, dist);
            if (dist > 0.0001f) offset++;
        }
    }
    var baseTrunk = trunk.sharedMesh.vertices;
    b.Weather.ForceWeather("thunderstorm", 600);
    for (int k = 0; k < 50; k++) anim.SendMessage("Update");
    rows.Add(new {
        stage = id,
        vertices = verts.Length,
        minLeafDisplacement = minMove,
        maxLeafDisplacement = maxMove,
        colliderRays = checks,
        colliderMiss = miss,
        colliderOffset = offset,
        maxColliderOffset = maxOffset,
        trunkVerticesUnchanged = baseTrunk.SequenceEqual(trunk.sharedMesh.vertices),
        trunkRotation = trunk.transform.localEulerAngles.ToString(),
        leafRotation = leaf.transform.localEulerAngles.ToString()
    });
}
System.IO.File.WriteAllText("Evidence/T11_Fix_20261012/animation_collider_probe.json", Newtonsoft.Json.JsonConvert.SerializeObject(rows, Newtonsoft.Json.Formatting.Indented));
return rows;
"""
    res = client.call_tool("execute_code", {"action": "execute", "code": anim_code, "safety_checks": False})
    print("Animation probe result:", res)

    # 2. Probe Rain Particles
    print("\n--- Running RAIN Probe ---")
    rain_code = """
var b = UnityEngine.Object.FindAnyObjectByType<CayTheGioi.Bootstrap.GameBootstrapper>();
var tv = UnityEngine.Object.FindAnyObjectByType<CayTheGioi.Presentation.TreeView>();
var rig = UnityEngine.Object.FindAnyObjectByType<CayTheGioi.Presentation.OrbitCameraRig>();
var ps = UnityEngine.Object.FindObjectsByType<UnityEngine.ParticleSystem>().First(x => x.name == "RainParticleSystem");
var rows = new System.Collections.Generic.List<object>();
foreach (var id in new[] { "seedling", "young_tree", "large_tree" }) {
    b.State.run.stageId = id;
    var v = b.Config.FindVisual(id);
    tv.ShowStage(v);
    rig.SetStagePose(v.cameraDistance, v.cameraPitch, v.cameraTargetHeight);
    b.Weather.ForceWeather("thunderstorm", 600);
    b.GetComponent<CayTheGioi.Presentation.WeatherView>().SendMessage("Update");
    ps.Simulate(3f, true, true, true);
    var p = new UnityEngine.ParticleSystem.Particle[1200];
    int n = ps.GetParticles(p);
    int visible = 0, near = 0;
    float minY = float.MaxValue, maxY = float.MinValue;
    for (int i = 0; i < n; i++) {
        var vp = rig.SceneCamera.WorldToViewportPoint(p[i].position);
        if (vp.z > 0 && vp.x >= 0 && vp.x <= 1 && vp.y >= 0 && vp.y <= 1) visible++;
        if (p[i].position.y >= 0 && p[i].position.y <= tv.LightningTargetAnchor.position.y) near++;
        minY = UnityEngine.Mathf.Min(minY, p[i].position.y);
        maxY = UnityEngine.Mathf.Max(maxY, p[i].position.y);
    }
    rows.Add(new {
        stage = id,
        emitterY = ps.transform.position.y,
        shapeOffsetY = ps.shape.position.y,
        particles = n,
        visibleParticleCenters = visible,
        particlesAtTreeHeight = near,
        minY = minY,
        maxY = maxY,
        treeTop = tv.LightningTargetAnchor.position.y
    });
    ps.Play();
}
System.IO.File.WriteAllText("Evidence/T11_Fix_20261012/rain_probe.json", Newtonsoft.Json.JsonConvert.SerializeObject(rows, Newtonsoft.Json.Formatting.Indented));
return rows;
"""
    res = client.call_tool("execute_code", {"action": "execute", "code": rain_code, "safety_checks": False})
    print("Rain probe result:", res)

    # 3. Probe Audio
    print("\n--- Running AUDIO Probe ---")
    audio_code = """
var b = UnityEngine.Object.FindAnyObjectByType<CayTheGioi.Bootstrap.GameBootstrapper>();
var a = b.Audio;
var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
a.SendMessage("OnApplicationFocus", true);
a.SetMusicEnabled(false);
b.Weather.ForceWeather("calm", 600);
var rain = (UnityEngine.AudioSource)a.GetType().GetField("rainSource", flags).GetValue(a);
var wind = (UnityEngine.AudioSource)a.GetType().GetField("windSource", flags).GetValue(a);
rain.Stop();
wind.Stop();
b.Weather.ForceWeather("thunderstorm", 600);
a.SetMusicEnabled(true);
a.SendMessage("Update");
var result = new {
    rainPlaying = rain.isPlaying,
    windPlaying = wind.isPlaying,
    rainTarget = a.GetType().GetField("targetRainVolume", flags).GetValue(a),
    windTarget = a.GetType().GetField("targetWindVolume", flags).GetValue(a),
    musicEnabled = a.MusicEnabled
};
System.IO.File.WriteAllText("Evidence/T11_Fix_20261012/audio_resume_probe.json", Newtonsoft.Json.JsonConvert.SerializeObject(result, Newtonsoft.Json.Formatting.Indented));
b.Weather.TriggerStrike("leaves");
b.Weather.ForceWeather("calm", 600);
return result;
"""
    res = client.call_tool("execute_code", {"action": "execute", "code": audio_code, "safety_checks": False})
    print("Audio probe result:", res)

    # 4. Probe Light
    print("\n--- Running LIGHT Probe ---")
    light_code = """
var b = UnityEngine.Object.FindAnyObjectByType<CayTheGioi.Bootstrap.GameBootstrapper>();
var light = UnityEngine.Object.FindAnyObjectByType<UnityEngine.Light>();
var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
var view = b.GetComponent<CayTheGioi.Presentation.WeatherView>();
var r = new {
    weather = b.Weather.CurrentWeatherId,
    actualIntensity = light.intensity,
    expectedCalmIntensity = view.GetType().GetField("defaultLightIntensity", flags).GetValue(view),
    actualColor = light.color.ToString()
};
System.IO.File.WriteAllText("Evidence/T11_Fix_20261012/light_restore_probe.json", Newtonsoft.Json.JsonConvert.SerializeObject(r, Newtonsoft.Json.Formatting.Indented));
return r;
"""
    res = client.call_tool("execute_code", {"action": "execute", "code": light_code, "safety_checks": False})
    print("Light probe result:", res)

    # 5. Probe Sky Visibility
    print("\n--- Running SKY Probe ---")
    sky_code = """
var b = UnityEngine.Object.FindAnyObjectByType<CayTheGioi.Bootstrap.GameBootstrapper>();
var tv = UnityEngine.Object.FindAnyObjectByType<CayTheGioi.Presentation.TreeView>();
var rig = UnityEngine.Object.FindAnyObjectByType<CayTheGioi.Presentation.OrbitCameraRig>();
var rows = new System.Collections.Generic.List<object>();
foreach (var id in new[] { "seedling", "young_tree", "large_tree" }) {
    var vis = b.Config.FindVisual(id);
    rig.SetStagePose(vis.cameraDistance, vis.cameraPitch, vis.cameraTargetHeight);
    var planes = UnityEngine.GeometryUtility.CalculateFrustumPlanes(rig.SceneCamera);
    var rng = new System.Random(1337);
    int intersects = 0;
    for (int i = 0; i < 1000; i++) {
        rng.NextDouble();
        float sign = rng.NextDouble() < .5 ? -1 : 1;
        var p0 = new UnityEngine.Vector3(sign * (5 + (float)rng.NextDouble() * 8), 8.5f + (float)rng.NextDouble() * 4, -4 + (float)rng.NextDouble() * 8);
        var p3 = p0 + new UnityEngine.Vector3(sign * 3.5f, -3.5f, 1.5f);
        var box = new UnityEngine.Bounds(p0, UnityEngine.Vector3.zero);
        box.Encapsulate(p3);
        box.Expand(1.5f);
        if (UnityEngine.GeometryUtility.TestPlanesAABB(planes, box)) intersects++;
    }
    rows.Add(new {
        stage = id,
        conservativeSkyBoltBoundsIntersectingCamera = intersects,
        samples = 1000
    });
}
var v = b.Config.FindVisual("seedling");
b.State.run.stageId = "seedling";
tv.ShowStage(v);
rig.SetStagePose(v.cameraDistance, v.cameraPitch, v.cameraTargetHeight);
b.Weather.ForceWeather("rainy", 600);
System.IO.File.WriteAllText("Evidence/T11_Fix_20261012/sky_visibility_probe.json", Newtonsoft.Json.JsonConvert.SerializeObject(rows, Newtonsoft.Json.Formatting.Indented));
return rows;
"""
    res = client.call_tool("execute_code", {"action": "execute", "code": sky_code, "safety_checks": False})
    print("Sky probe result:", res)

if __name__ == "__main__":
    main()
