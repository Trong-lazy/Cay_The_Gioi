from unity_mcp_client import UnityMCPClient
import json

c = UnityMCPClient()

probes = {
    "animation": """
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
    
    int step = System.Math.Max(3, (tri.Length / 900) * 3);
    for (int j = 0; j < tri.Length; j += step) {
        var a = leaf.transform.TransformPoint(verts[tri[j]]);
        var c2 = leaf.transform.TransformPoint(verts[tri[j + 1]]);
        var d2 = leaf.transform.TransformPoint(verts[tri[j + 2]]);
        var n = UnityEngine.Vector3.Cross(c2 - a, d2 - a).normalized;
        if (n.sqrMagnitude < 0.5f) continue;
        var center = (a + c2 + d2) / 3f;
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
""",
    "rain": """
var b = UnityEngine.Object.FindAnyObjectByType<CayTheGioi.Bootstrap.GameBootstrapper>();
var tv = UnityEngine.Object.FindAnyObjectByType<CayTheGioi.Presentation.TreeView>();
var rig = UnityEngine.Object.FindAnyObjectByType<CayTheGioi.Presentation.OrbitCameraRig>();
var pss = UnityEngine.Object.FindObjectsByType<UnityEngine.ParticleSystem>(UnityEngine.FindObjectsInactive.Include, UnityEngine.FindObjectsSortMode.None);
UnityEngine.ParticleSystem ps = null;
foreach(var p in pss) { if (p.name == "RainParticleSystem") { ps = p; break; } }
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
""",
    "audio": """
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
""",
    "light": """
var b = UnityEngine.Object.FindAnyObjectByType<CayTheGioi.Bootstrap.GameBootstrapper>();
var lights = UnityEngine.Object.FindObjectsByType<UnityEngine.Light>(UnityEngine.FindObjectsInactive.Include, UnityEngine.FindObjectsSortMode.None);
UnityEngine.Light dirLight = null;
foreach(var l in lights) { if (l.type == UnityEngine.LightType.Directional) { dirLight = l; break; } }
var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
var view = b.GetComponent<CayTheGioi.Presentation.WeatherView>();
var r = new {
    weather = b.Weather.CurrentWeatherId,
    actualIntensity = dirLight.intensity,
    expectedCalmIntensity = view.GetType().GetField("defaultLightIntensity", flags).GetValue(view),
    actualColor = dirLight.color.ToString()
};
System.IO.File.WriteAllText("Evidence/T11_Fix_20261012/light_restore_probe.json", Newtonsoft.Json.JsonConvert.SerializeObject(r, Newtonsoft.Json.Formatting.Indented));
return r;
""",
    "sky": """
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
}

for name, code in probes.items():
    print(f"\n================ Running Probe: {name} ================")
    res = c.call_tool('execute_code', {'action': 'execute', 'code': code, 'safety_checks': False})
    print(f"Result for {name}:", res)
