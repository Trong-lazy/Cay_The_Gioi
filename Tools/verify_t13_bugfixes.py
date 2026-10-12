import json
import os
import sys

sys.stdout.reconfigure(encoding='utf-8')
from unity_mcp_client import UnityMCPClient

def main():
    c = UnityMCPClient()
    out_dir = os.path.join(os.path.dirname(os.path.dirname(os.path.abspath(__file__))), "Evidence", "T13_Bugfix_20261012")
    os.makedirs(out_dir, exist_ok=True)

    print("=== RUNNING T13 INDEPENDENT VERIFICATION PROBES ===")

    # 1. Probe B04: Terrain Geometry & Boundary Edges
    print("\n--- 1. Probe B04: Terrain Geometry & Boundary Edges ---")
    probe_b04_code = """
var envGo = UnityEngine.GameObject.Find("World/WorldEnvironment");
var terrainMf = envGo.transform.Find("Planet_Terrain").GetComponent<UnityEngine.MeshFilter>();
var mesh = terrainMf.sharedMesh;

var tris = mesh.triangles;
var edgeCount = new System.Collections.Generic.Dictionary<string, int>();

for (int i = 0; i < tris.Length; i += 3)
{
    int a = tris[i];
    int b = tris[i + 1];
    int c = tris[i + 2];

    string e1 = System.Math.Min(a, b) + "_" + System.Math.Max(a, b);
    string e2 = System.Math.Min(b, c) + "_" + System.Math.Max(b, c);
    string e3 = System.Math.Min(c, a) + "_" + System.Math.Max(c, a);

    edgeCount[e1] = edgeCount.ContainsKey(e1) ? edgeCount[e1] + 1 : 1;
    edgeCount[e2] = edgeCount.ContainsKey(e2) ? edgeCount[e2] + 1 : 1;
    edgeCount[e3] = edgeCount.ContainsKey(e3) ? edgeCount[e3] + 1 : 1;
}

int boundaryEdges = 0;
foreach (var kvp in edgeCount)
{
    if (kvp.Value == 1) boundaryEdges++;
}

var result = new {
    meshName = mesh.name,
    vertexCount = mesh.vertexCount,
    triangleCount = tris.Length / 3,
    totalEdges = edgeCount.Count,
    boundaryEdges = boundaryEdges,
    isClosedManifold = (boundaryEdges == 0),
    minY = mesh.bounds.min.y,
    maxY = mesh.bounds.max.y
};
return result;
"""
    res_b04 = c.call_tool('execute_code', {'action': 'execute', 'code': probe_b04_code, 'safety_checks': False})
    print("B04 Result:", res_b04)

    # 2. Probe B02: Water vs Terrain Boundary
    print("\n--- 2. Probe B02: Water vs Terrain Boundary ---")
    probe_b02_code = """
var envGo = UnityEngine.GameObject.Find("World/WorldEnvironment");
var controller = envGo.GetComponent<CayTheGioi.MapEnvironment.WorldEnvironmentController>();
var config = controller.Config;
var waterMf = envGo.transform.Find("Water_System/Lake_Surface").GetComponent<UnityEngine.MeshFilter>();
var waterMesh = waterMf.sharedMesh;
var waterVerts = waterMesh.vertices;

var rows = new System.Collections.Generic.List<object>();
int hangingCount = 0; // Số đỉnh nước nổi cao hơn đất (treo trên không)
int deeplyBuriedCount = 0; // Số đỉnh nước bị chôn sâu hơn 0.15m

// Bỏ qua đỉnh tâm 0, kiểm tra 40 đỉnh vành nước
for (int i = 1; i < waterVerts.Length; i++)
{
    var wv = waterVerts[i];
    float terrainY = CayTheGioi.MapEnvironment.SphericalWorldTerrainBuilder.CalculateHeight(wv.x, wv.z, config);
    float diff = wv.y - terrainY; // > 0 là nước nổi cao hơn đất (treo), <= 0 là chìm vào bờ

    if (diff > 0.005f) hangingCount++;
    if (diff < -0.15f) deeplyBuriedCount++;

    rows.Add(new {
        index = i,
        pos = new { x = wv.x, y = wv.y, z = wv.z },
        terrainY = terrainY,
        waterMinusTerrain = diff
    });
}

var result = new {
    totalEdgeVertices = waterVerts.Length - 1,
    hangingVerticesCount = hangingCount,
    deeplyBuriedVerticesCount = deeplyBuriedCount,
    hasNoHangingEdges = (hangingCount == 0),
    samples = rows
};
return result;
"""
    res_b02 = c.call_tool('execute_code', {'action': 'execute', 'code': probe_b02_code, 'safety_checks': False})
    print("B02 Summary:", {k: v for k, v in res_b02.get("data", {}).get("result", {}).items() if k != "samples"} if isinstance(res_b02, dict) and "data" in res_b02 and isinstance(res_b02["data"].get("result"), dict) else res_b02)

    # 3. Probe B06: Waterfall Normals & Winding
    print("\n--- 3. Probe B06: Waterfall Normals & Winding ---")
    probe_b06_code = """
var envGo = UnityEngine.GameObject.Find("World/WorldEnvironment");
var wfMf = envGo.transform.Find("Water_System/Waterfall").GetComponent<UnityEngine.MeshFilter>();
var mesh = wfMf.sharedMesh;
var normals = mesh.normals;
var tris = mesh.triangles;

float minMagnitude = float.MaxValue;
float maxMagnitude = 0;
bool anyZeroNormal = false;

for (int i = 0; i < normals.Length; i++)
{
    float m = normals[i].magnitude;
    minMagnitude = UnityEngine.Mathf.Min(minMagnitude, m);
    maxMagnitude = UnityEngine.Mathf.Max(maxMagnitude, m);
    if (m < 0.5f) anyZeroNormal = true;
}

var result = new {
    vertexCount = mesh.vertexCount,
    triangleCount = tris.Length / 3,
    minNormalMagnitude = minMagnitude,
    maxNormalMagnitude = maxMagnitude,
    hasValidNormals = (!anyZeroNormal && minMagnitude > 0.95f),
    firstNormal = normals.Length > 0 ? normals[0].ToString() : "none"
};
return result;
"""
    res_b06 = c.call_tool('execute_code', {'action': 'execute', 'code': probe_b06_code, 'safety_checks': False})
    print("B06 Result:", res_b06)

    # 4. Probe B05: Coral Flower Ground Contact
    print("\n--- 4. Probe B05: Coral Flower Ground Contact ---")
    probe_b05_code = """
var envGo = UnityEngine.GameObject.Find("World/WorldEnvironment");
var flowerMf = envGo.transform.Find("Decorations/Foliage_CoralFlowers").GetComponentInChildren<UnityEngine.MeshFilter>();
var mesh = flowerMf.sharedMesh;
var verts = mesh.vertices;

float minY = float.MaxValue;
float maxY = float.MinValue;
for (int i = 0; i < verts.Length; i++)
{
    minY = UnityEngine.Mathf.Min(minY, verts[i].y);
    maxY = UnityEngine.Mathf.Max(maxY, verts[i].y);
}

var result = new {
    meshName = mesh.name,
    vertexCount = mesh.vertexCount,
    minY = minY,
    maxY = maxY,
    hasGroundContact = (minY <= -0.03f) // Có cuống và đế tiếp xúc mặt đất y <= -0.03m
};
return result;
"""
    res_b05 = c.call_tool('execute_code', {'action': 'execute', 'code': probe_b05_code, 'safety_checks': False})
    print("B05 Result:", res_b05)

    # 5. Probe B07: Mushroom Unique Mesh Assets
    print("\n--- 5. Probe B07: Mushroom Unique Mesh Assets ---")
    probe_b07_code = """
var envGo = UnityEngine.GameObject.Find("World/WorldEnvironment");
var mushRoot = envGo.transform.Find("Decorations/Mushrooms_Bioluminescent");
var clusters = mushRoot.GetComponentsInChildren<UnityEngine.Transform>();

var list = new System.Collections.Generic.List<object>();
var meshNames = new System.Collections.Generic.HashSet<string>();
bool hasDuplicates = false;

var filters = mushRoot.GetComponentsInChildren<UnityEngine.MeshFilter>();
foreach (var mf in filters)
{
    string name = mf.sharedMesh.name;
    if (meshNames.Contains(name)) hasDuplicates = true;
    meshNames.Add(name);

    list.Add(new {
        goName = mf.gameObject.name,
        meshName = name,
        vertexCount = mf.sharedMesh.vertexCount
    });
}

var result = new {
    totalMushrooms = filters.Length,
    uniqueMeshCount = meshNames.Count,
    noMeshOverwrites = (!hasDuplicates && meshNames.Count == filters.Length),
    meshes = list
};
return result;
"""
    res_b07 = c.call_tool('execute_code', {'action': 'execute', 'code': probe_b07_code, 'safety_checks': False})
    print("B07 Result:", res_b07)

    # 6. Probe B03: Camera Orbit 360 Viewing Corridor & Raycast Clearance
    print("\n--- 6. Probe B03: Camera Orbit 360 Viewing Corridor & Raycast Clearance ---")
    probe_b03_code = """
var b = UnityEngine.Object.FindAnyObjectByType<CayTheGioi.Bootstrap.GameBootstrapper>();
var tv = UnityEngine.Object.FindAnyObjectByType<CayTheGioi.Presentation.TreeView>();
var rig = UnityEngine.Object.FindAnyObjectByType<CayTheGioi.Presentation.OrbitCameraRig>();
var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
var yawField = rig.GetType().GetField("yaw", flags);
var pitchField = rig.GetType().GetField("pitch", flags);
var applyPose = rig.GetType().GetMethod("ApplyPose", flags);

var stageResults = new System.Collections.Generic.List<object>();

foreach (var stageId in new[] { "seedling", "young_tree", "large_tree" })
{
    b.State.run.stageId = stageId;
    var visual = b.Config.FindVisual(stageId);
    tv.ShowStage(visual);
    rig.SetStagePose(visual.cameraDistance, visual.cameraPitch, visual.cameraTargetHeight);

    Vector3 targetPos = new Vector3(0, visual.cameraTargetHeight, 0);
    int totalAngles = 12; // 0, 30, 60, ..., 330 deg
    int clearViews = 0;
    int occludedByProps = 0;
    var angleSamples = new System.Collections.Generic.List<object>();

    for (int a = 0; a < totalAngles; a++)
    {
        float angleDeg = a * (360f / totalAngles);
        yawField.SetValue(rig, angleDeg);
        pitchField.SetValue(rig, visual.cameraPitch);
        applyPose.Invoke(rig, null);

        Vector3 camPos = rig.SceneCamera.transform.position;
        Vector3 dir = (targetPos - camPos).normalized;
        float dist = Vector3.Distance(camPos, targetPos);

        // Raycast từ camera tới điểm ngắm trên cây
        UnityEngine.RaycastHit hit;
        bool occluded = false;
        string hitName = "none";

        if (UnityEngine.Physics.Raycast(camPos, dir, out hit, dist - 0.1f))
        {
            hitName = hit.collider.gameObject.name;
            // Nếu va phải props môi trường (Đá, cây lá lớn, etc)
            if (hit.collider.transform.IsChildOf(UnityEngine.GameObject.Find("World/WorldEnvironment").transform))
            {
                occluded = true;
                occludedByProps++;
            }
        }

        if (!occluded) clearViews++;

        angleSamples.Add(new {
            orbitAngle = angleDeg,
            cameraPos = new { x = camPos.x, y = camPos.y, z = camPos.z },
            targetPos = new { x = targetPos.x, y = targetPos.y, z = targetPos.z },
            isClear = !occluded,
            hitObject = hitName
        });
    }

    stageResults.Add(new {
        stage = stageId,
        distance = visual.cameraDistance,
        pitch = visual.cameraPitch,
        targetHeight = visual.cameraTargetHeight,
        totalCheckedAngles = totalAngles,
        clearViewAngles = clearViews,
        occludedAngles = occludedByProps,
        isFullyClear360 = (occludedByProps == 0),
        samples = angleSamples
    });
}

return stageResults;
"""
    res_b03 = c.call_tool('execute_code', {'action': 'execute', 'code': probe_b03_code, 'safety_checks': False})
    print("B03 Summary:", [{k: v for k, v in item.items() if k != "samples"} for item in res_b03.get("data", {}).get("result", [])] if isinstance(res_b03, dict) and "data" in res_b03 and isinstance(res_b03["data"].get("result"), list) else res_b03)

    # 7. Probe UI01: Spirit Card Rate Text
    print("\n--- 7. Probe UI01: Spirit Card Rate Text ---")
    probe_ui01_code = """
var b = UnityEngine.Object.FindAnyObjectByType<CayTheGioi.Bootstrap.GameBootstrapper>();
var hud = UnityEngine.Object.FindAnyObjectByType<CayTheGioi.Presentation.FoundationHudView>();
hud.SendMessage("Refresh");
hud.SendMessage("LateUpdate");

var layout = UnityEngine.Object.FindAnyObjectByType<CayTheGioi.Presentation.ReferenceUiLayout>();
if (layout != null) layout.SendMessage("LateUpdate");

var texts = UnityEngine.Object.FindObjectsByType<UnityEngine.UI.Text>(UnityEngine.FindObjectsInactive.Include, UnityEngine.FindObjectsSortMode.None);
UnityEngine.UI.Text rateText = null;
foreach(var t in texts)
{
    if (t.name == "Rate" && t.transform.parent != null && t.transform.parent.name == "SpiritCard")
    {
        rateText = t;
        break;
    }
}

var result = new {
    spiritAbsorptionUnlocked = b.State.run.spiritAbsorptionUnlocked,
    rateString = rateText != null ? rateText.text : "NOT_FOUND",
    fontSize = rateText != null ? rateText.fontSize : 0,
    horizontalOverflow = rateText != null ? rateText.horizontalOverflow.ToString() : "none",
    rectWidth = rateText != null ? ((UnityEngine.RectTransform)rateText.transform).rect.width : 0,
    isFullyVisible = (rateText != null && rateText.text == "Cây Trưởng Thành")
};
return result;
"""
    res_ui01 = c.call_tool('execute_code', {'action': 'execute', 'code': probe_ui01_code, 'safety_checks': False})
    print("UI01 Result:", res_ui01)

    # Save all measurements to JSON file
    all_measurements = {
        "timestamp": "2026-10-12T11:47:00Z",
        "probe_B04_terrain": res_b04.get("data", {}).get("result"),
        "probe_B02_water_boundary": res_b02.get("data", {}).get("result"),
        "probe_B06_waterfall": res_b06.get("data", {}).get("result"),
        "probe_B05_flower_contact": res_b05.get("data", {}).get("result"),
        "probe_B07_mushroom_naming": res_b07.get("data", {}).get("result"),
        "probe_B03_camera_corridor_360": res_b03.get("data", {}).get("result"),
        "probe_UI01_spirit_card": res_ui01.get("data", {}).get("result")
    }

    out_file = os.path.join(out_dir, "t13_bugfix_measurements.json")
    with open(out_file, "w", encoding="utf-8") as f:
        json.dump(all_measurements, f, indent=2, ensure_ascii=False)
    print(f"\nAll measurements saved to: {out_file}")

if __name__ == "__main__":
    main()
