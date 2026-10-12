from unity_mcp_client import UnityMCPClient

c = UnityMCPClient()
code = """
var b = UnityEngine.Object.FindAnyObjectByType<CayTheGioi.Bootstrap.GameBootstrapper>();
var tv = UnityEngine.Object.FindAnyObjectByType<CayTheGioi.Presentation.TreeView>();
var anim = tv.GetComponent<CayTheGioi.Presentation.TreeAnimationController>();
var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;

var steps = new System.Collections.Generic.List<string>();

foreach (var id in new[] { "seedling", "young_tree", "large_tree" }) {
    steps.Add("Stage " + id);
    b.State.run.stageId = id;
    var visual = b.Config.FindVisual(id);
    if (visual == null) { steps.Add("Visual null"); break; }
    tv.ShowStage(visual);
    steps.Add("ShowStage OK");
    
    var rig = UnityEngine.Object.FindAnyObjectByType<CayTheGioi.Presentation.OrbitCameraRig>();
    rig.SetStagePose(visual.cameraDistance, visual.cameraPitch, visual.cameraTargetHeight);
    steps.Add("SetStagePose OK");
    
    b.Weather.ForceWeather("calm", 600);
    anim.SendMessage("Update");
    steps.Add("Update OK");
    
    var leaf = tv.CurrentStageAnchors.GetComponentsInChildren<UnityEngine.MeshFilter>().First(x => x.name == "Foliage");
    if (leaf == null) { steps.Add("Leaf null"); break; }
    
    var trunk = tv.CurrentStageAnchors.GetComponentsInChildren<UnityEngine.MeshFilter>().First(x => x.name == "Trunk_Branches");
    if (trunk == null) { steps.Add("Trunk null"); break; }
    
    var orig = (UnityEngine.Vector3[])anim.GetType().GetField("baseFoliageVertices", flags).GetValue(anim);
    if (orig == null) { steps.Add("orig is NULL!"); break; }
    steps.Add("orig len=" + orig.Length + ", leaf verts len=" + leaf.sharedMesh.vertices.Length);
}
return string.Join(" -> ", steps);
"""
res = c.call_tool('execute_code', {'action': 'execute', 'code': code})
print("Anim trace:", res)
