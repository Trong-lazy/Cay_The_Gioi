using System.Collections;
using System.Collections.Generic;
using CayTheGioi.Bootstrap;
using CayTheGioi.Infrastructure;
using CayTheGioi.Presentation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using TouchPhase = UnityEngine.InputSystem.TouchPhase;

namespace CayTheGioi.Tests
{
    public sealed class ArtPipelineSceneTests
    {
        private GameBootstrapper b;
        private TreeView tree;
        private OrbitCameraRig rig;
        private Mouse mouse, priorMouse;
        private Touchscreen touch, priorTouch;

        [UnitySetUp]
        public IEnumerator Setup()
        {
            SaveTestScope.Begin();
            yield return SceneManager.LoadSceneAsync("MainScene");
            yield return null;
            b = Object.FindAnyObjectByType<GameBootstrapper>();
            Assert.That(b, Is.Not.Null);
            Assert.That(b.IsInitialized, Is.True);
            b.TickDriver.ManualTick = true;
            b.Input.SendMessage("OnApplicationFocus", true);
            tree = Object.FindAnyObjectByType<TreeView>();
            rig = Object.FindAnyObjectByType<OrbitCameraRig>();
            // These T10 tests compare a static mesh snapshot with gestures several frames later.
            // Keep that snapshot stable; live animation/collider/input are tested by WeatherSceneTests.
            tree.GetComponent<TreeAnimationController>().enabled = false;
            priorMouse = Mouse.current;
            priorTouch = Touchscreen.current;
            mouse = InputSystem.AddDevice<Mouse>();
            touch = InputSystem.AddDevice<Touchscreen>();
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (mouse != null && mouse.added) InputSystem.RemoveDevice(mouse);
            if (touch != null && touch.added) InputSystem.RemoveDevice(touch);
            if (priorMouse != null && priorMouse.added) priorMouse.MakeCurrent();
            if (priorTouch != null && priorTouch.added) priorTouch.MakeCurrent();
            SaveTestScope.End();
            yield return null;
        }

        private IEnumerator MouseTap(Vector2 screenPoint)
        {
            InputSystem.QueueStateEvent(mouse, new MouseState { position = screenPoint }.WithButton(MouseButton.Left));
            yield return null;
            yield return null;
            InputSystem.QueueStateEvent(mouse, new MouseState { position = screenPoint });
            yield return null;
            yield return null;
        }

        private IEnumerator TouchTap(int id, Vector2 screenPoint)
        {
            InputSystem.QueueStateEvent(touch, new TouchState { touchId = id, phase = TouchPhase.Began, position = screenPoint });
            yield return null;
            yield return null;
            InputSystem.QueueStateEvent(touch, new TouchState { touchId = id, phase = TouchPhase.Ended, position = screenPoint });
            yield return null;
            yield return null;
        }

        private struct MeshSampleData
        {
            public List<Vector3[]> AllTriangles;
            public Dictionary<string, List<Vector3[]>> PartTriangles;
            public float MinX, MinY, MaxX, MaxY;
        }

        private MeshSampleData BuildStageMeshSampleData()
        {
            Physics.SyncTransforms();
            var data = new MeshSampleData
            {
                AllTriangles = new List<Vector3[]>(),
                PartTriangles = new Dictionary<string, List<Vector3[]>>(),
                MinX = 99999f,
                MinY = 99999f,
                MaxX = -99999f,
                MaxY = -99999f
            };

            foreach (var mf in tree.ModelSlot.GetComponentsInChildren<MeshFilter>())
            {
                var mesh = mf.sharedMesh;
                if (mesh == null) continue;
                var vertices = mesh.vertices;
                var indices = mesh.triangles;
                for (int k = 0; k < vertices.Length; k++)
                {
                    vertices[k] = mf.transform.TransformPoint(vertices[k]);
                    var p = rig.SceneCamera.WorldToScreenPoint(vertices[k]);
                    if (p.z > 0f)
                    {
                        data.MinX = Mathf.Min(data.MinX, p.x);
                        data.MinY = Mathf.Min(data.MinY, p.y);
                        data.MaxX = Mathf.Max(data.MaxX, p.x);
                        data.MaxY = Mathf.Max(data.MaxY, p.y);
                    }
                }
                var partList = new List<Vector3[]>();
                for (int k = 0; k < indices.Length; k += 3)
                {
                    var tri = new[] { vertices[indices[k]], vertices[indices[k + 1]], vertices[indices[k + 2]] };
                    data.AllTriangles.Add(tri);
                    partList.Add(tri);
                }
                data.PartTriangles[mf.name] = partList;
            }
            return data;
        }

        private static bool RayIntersectsTriangles(Ray ray, List<Vector3[]> triangles, out float minDistance)
        {
            minDistance = float.PositiveInfinity;
            foreach (var tri in triangles)
            {
                var e1 = tri[1] - tri[0];
                var e2 = tri[2] - tri[0];
                var h = Vector3.Cross(ray.direction, e2);
                float det = Vector3.Dot(e1, h);
                if (Mathf.Abs(det) < 1e-9f) continue;
                float inv = 1f / det;
                var s = ray.origin - tri[0];
                float u = inv * Vector3.Dot(s, h);
                if (u < 0f || u > 1f) continue;
                var q = Vector3.Cross(s, e1);
                float v = inv * Vector3.Dot(ray.direction, q);
                if (v < 0f || u + v > 1f) continue;
                float d = inv * Vector3.Dot(e2, q);
                if (d > 0f && d < minDistance) minDistance = d;
            }
            return !float.IsPositiveInfinity(minDistance);
        }

        private void CollectVisibleAndNearSilhouetteEmptyPoints(
            MeshSampleData data,
            out List<Vector2> visibleSurfacePoints,
            out List<Vector2> nearSilhouetteEmptyPoints)
        {
            visibleSurfacePoints = new List<Vector2>();
            nearSilhouetteEmptyPoints = new List<Vector2>();

            // 1. Find at least 1 visible point on each distinct mesh region (Trunk_Branches, Foliage, Roots/SeedShell)
            foreach (var kv in data.PartTriangles)
            {
                var tris = kv.Value;
                int step = Mathf.Max(1, tris.Count / 40);
                for (int i = 0; i < tris.Count; i += step)
                {
                    var centroid = (tris[i][0] + tris[i][1] + tris[i][2]) / 3f;
                    var sp = rig.SceneCamera.WorldToScreenPoint(centroid);
                    if (sp.z <= 0f) continue;
                    var pt = new Vector2(sp.x, sp.y);
                    if (pt.x < 20f || pt.x > Screen.width - 20f || pt.y < 20f || pt.y > Screen.height - 20f) continue;
                    if (b.Input.IsOverUi(pt)) continue;

                    var ray = rig.SceneCamera.ScreenPointToRay(pt);
                    if (!RayIntersectsTriangles(ray, data.AllTriangles, out float meshDist)) continue;

                    var hits = Physics.RaycastAll(ray);
                    if (hits == null || hits.Length == 0) continue;
                    System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
                    bool validTreeHit = false;
                    for (int hIdx = 0; hIdx < hits.Length; hIdx++)
                    {
                        var hit = hits[hIdx];
                        if (hit.collider.GetComponentInParent<TreeView>() != tree)
                        {
                            if (Vector3.Dot(hit.normal, ray.direction) < 0f) break;
                            continue;
                        }
                        if (Vector3.Dot(hit.normal, ray.direction) < 0f)
                        {
                            validTreeHit = true;
                            break;
                        }
                    }
                    if (validTreeHit)
                    {
                        visibleSurfacePoints.Add(pt);
                        break;
                    }
                }
            }

            // Also scan the 25x25 grid around the model bounding box to pick outer canopy/branch visible points and near-silhouette empty points
            var gridVisible = new List<Vector2>();
            for (int ix = 0; ix <= 24; ix++)
            {
                for (int iy = 0; iy <= 24; iy++)
                {
                    var point = new Vector2(
                        Mathf.Lerp(data.MinX - 40f, data.MaxX + 40f, ix / 24f),
                        Mathf.Lerp(data.MinY - 40f, data.MaxY + 40f, iy / 24f));
                    if (point.x < 10f || point.x > Screen.width - 10f || point.y < 10f || point.y > Screen.height - 10f) continue;
                    if (b.Input.IsOverUi(point)) continue;
                    var ray = rig.SceneCamera.ScreenPointToRay(point);
                    if (RayIntersectsTriangles(ray, data.AllTriangles, out float meshDist))
                    {
                        bool groundCloser = false;
                        foreach (var hit in Physics.RaycastAll(ray, meshDist - 0.005f))
                        {
                            if (hit.collider.GetComponentInParent<TreeView>() != tree)
                            {
                                groundCloser = true;
                                break;
                            }
                        }
                        if (!groundCloser) gridVisible.Add(point);
                    }
                }
            }

            // Include extremes from gridVisible (e.g. leftmost/topmost canopy edge) so outer foliage is tested
            if (gridVisible.Count > 0)
            {
                visibleSurfacePoints.Add(gridVisible[0]);
                visibleSurfacePoints.Add(gridVisible[gridVisible.Count / 2]);
                visibleSurfacePoints.Add(gridVisible[gridVisible.Count - 1]);
            }

            // Collect near-silhouette empty points: points on the 25x25 grid that do NOT intersect any stage triangle
            // but lie close (12px..65px) to at least one visible surface point (where oversized sphere/capsule colliders used to false-positive)
            for (int ix = 0; ix <= 24; ix++)
            {
                for (int iy = 0; iy <= 24; iy++)
                {
                    var point = new Vector2(
                        Mathf.Lerp(data.MinX - 40f, data.MaxX + 40f, ix / 24f),
                        Mathf.Lerp(data.MinY - 40f, data.MaxY + 40f, iy / 24f));
                    if (point.x < 10f || point.x > Screen.width - 10f || point.y < 10f || point.y > Screen.height - 10f) continue;
                    if (b.Input.IsOverUi(point)) continue;
                    var ray = rig.SceneCamera.ScreenPointToRay(point);
                    if (RayIntersectsTriangles(ray, data.AllTriangles, out _)) continue;
                    // Empty mesh gaps inside the user-requested tap sphere are now valid targets.
                    if (tree.GetComponent<SphereCollider>().Raycast(ray, out _, float.PositiveInfinity)) continue;

                    float nearestVisible = float.PositiveInfinity;
                    foreach (var vp in gridVisible)
                    {
                        float d = Vector2.Distance(point, vp);
                        if (d < nearestVisible) nearestVisible = d;
                    }
                    if (nearestVisible >= 8f && nearestVisible <= 65f)
                    {
                        nearSilhouetteEmptyPoints.Add(point);
                    }
                }
            }
            if (nearSilhouetteEmptyPoints.Count < 3)
            {
                foreach (var fraction in new[] { new Vector2(.12f,.5f), new Vector2(.88f,.5f), new Vector2(.2f,.8f), new Vector2(.8f,.8f), new Vector2(.2f,.3f) })
                {
                    var point = new Vector2(Screen.width * fraction.x, Screen.height * fraction.y);
                    var ray = rig.SceneCamera.ScreenPointToRay(point);
                    if (!b.Input.IsOverUi(point) && !RayIntersectsTriangles(ray, data.AllTriangles, out _) &&
                        !tree.GetComponent<SphereCollider>().Raycast(ray, out _, float.PositiveInfinity))
                        nearSilhouetteEmptyPoints.Add(point);
                }
            }
        }

        [UnityTest]
        public IEnumerator AllStagePrefabsHaveCleanRootTransformTypedAnchorsAndUrpMaterials()
        {
            string[] stageIds = { "seedling", "young_tree", "large_tree" };
            float prevRadius = 0f;

            foreach (var stageId in stageIds)
            {
                var visual = b.Config.FindVisual(stageId);
                Assert.That(visual, Is.Not.Null, "Missing StageVisualDefinition for " + stageId);
                Assert.That(visual.prefab, Is.Not.Null, "Missing prefab for " + stageId);

                Assert.That(visual.prefab.transform.localPosition, Is.EqualTo(Vector3.zero));
                Assert.That(visual.prefab.transform.localRotation, Is.EqualTo(Quaternion.identity));
                Assert.That(visual.prefab.transform.localScale, Is.EqualTo(Vector3.one));

                tree.ShowStage(visual);
                rig.SetStagePose(visual.cameraDistance, visual.cameraPitch, visual.cameraTargetHeight);
                yield return null;

                Assert.That(tree.CurrentStageId, Is.EqualTo(stageId));
                Assert.That(tree.ModelSlot.childCount, Is.EqualTo(1));

                var stageModel = tree.ModelSlot.GetChild(0);
                Assert.That(stageModel.localPosition, Is.EqualTo(Vector3.zero));
                Assert.That(stageModel.localRotation, Is.EqualTo(Quaternion.identity));
                Assert.That(stageModel.localScale, Is.EqualTo(Vector3.one));

                var anchors = tree.CurrentStageAnchors;
                Assert.That(anchors, Is.Not.Null, "TreeStageAnchors component missing on " + stageId);
                Assert.That(anchors.RootAnchor, Is.Not.Null);
                Assert.That(anchors.TrunkAuraAnchor, Is.Not.Null);
                Assert.That(anchors.LeavesCenterAnchor, Is.Not.Null);
                Assert.That(anchors.LightningTargetAnchor, Is.Not.Null);

                Assert.That(anchors.TrunkAuraAnchor.position.y, Is.GreaterThan(anchors.RootAnchor.position.y));
                Assert.That(anchors.LeavesCenterAnchor.position.y, Is.GreaterThan(anchors.TrunkAuraAnchor.position.y));
                Assert.That(anchors.LightningTargetAnchor.position.y, Is.GreaterThan(anchors.LeavesCenterAnchor.position.y));

                Assert.That(tree.RootAnchor.position, Is.EqualTo(anchors.RootAnchor.position));
                Assert.That(tree.TrunkAnchor.position, Is.EqualTo(anchors.TrunkAuraAnchor.position));
                Assert.That(tree.LeavesAnchor.position, Is.EqualTo(anchors.LeavesCenterAnchor.position));
                Assert.That(tree.LightningTargetAnchor.position, Is.EqualTo(anchors.LightningTargetAnchor.position));

                var sphereCol = tree.GetComponent<SphereCollider>();
                Assert.That(sphereCol.radius, Is.GreaterThan(prevRadius));
                prevRadius = sphereCol.radius;
                Assert.That(sphereCol.enabled, Is.True, "All stages need a stable spherical tap volume.");
                Assert.That(sphereCol.isTrigger, Is.True, "Tap volume must not introduce solid geometry.");

                var meshColliders = stageModel.GetComponentsInChildren<MeshCollider>(true);
                Assert.That(meshColliders.Length, Is.GreaterThanOrEqualTo(3),
                    $"Stage '{stageId}' must use non-convex MeshColliders matching actual visible meshes.");
                foreach (var mc in meshColliders)
                {
                    Assert.That(mc.enabled, Is.True);
                    Assert.That(mc.convex, Is.False);
                    Assert.That(mc.sharedMesh, Is.Not.Null);
                }

                var renderers = stageModel.GetComponentsInChildren<MeshRenderer>();
                Assert.That(renderers.Length, Is.GreaterThanOrEqualTo(3));
                foreach (var r in renderers)
                {
                    var mats = r.sharedMaterials;
                    Assert.That(mats.Length, Is.GreaterThan(0));
                    foreach (var m in mats)
                    {
                        Assert.That(m, Is.Not.Null, $"Null material on {stageId}/{r.name}");
                        Assert.That(m.shader.name, Is.EqualTo("Universal Render Pipeline/Lit"),
                            $"Unexpected shader on {stageId}/{r.name}: {m.shader.name}");
                    }
                }

                if (stageId == "seedling")
                {
                    var seedShell = stageModel.Find("Model/SeedShell");
                    Assert.That(seedShell, Is.Not.Null, "SeedShell mesh missing on seedling prefab.");
                    var shellMats = seedShell.GetComponent<MeshRenderer>().sharedMaterials;
                    Assert.That(shellMats.Length, Is.EqualTo(2), "SeedShell must preserve both outer and inner material slots.");
                    Assert.That(shellMats[0].name, Does.Contain("M_SeedShell_Outer"));
                    Assert.That(shellMats[1].name, Does.Contain("M_SeedShell_Inner"));
                }
            }
        }

        [UnityTest]
        public IEnumerator RaycastTapHitsTrunkAndCanopyAndMissesEmptySpaceAcrossAllThreeStages()
        {
            string[] stageIds = { "seedling", "young_tree", "large_tree" };
            int touchId = 100;

            foreach (var stageId in stageIds)
            {
                var visual = b.Config.FindVisual(stageId);
                tree.ShowStage(visual);
                rig.SetStagePose(visual.cameraDistance, visual.cameraPitch, visual.cameraTargetHeight);
                b.State.vitality.tapProgress = 0;
                yield return null;

                var sampleData = BuildStageMeshSampleData();
                CollectVisibleAndNearSilhouetteEmptyPoints(
                    sampleData,
                    out var visiblePoints,
                    out var nearEmptyPoints);

                Assert.That(visiblePoints.Count, Is.GreaterThanOrEqualTo(3), $"Expected >=3 visible surface points for {stageId}");
                Assert.That(nearEmptyPoints.Count, Is.GreaterThanOrEqualTo(3), $"Expected >=3 near-silhouette empty points for {stageId}");

                int expectedTaps = 0;

                // Test 3 distinct visible surface points (trunk, foliage, roots/outer canopy) via Mouse and Touch
                for (int i = 0; i < 3; i++)
                {
                    var pt = visiblePoints[i];
                    yield return MouseTap(pt);
                    expectedTaps++;
                    Assert.That(b.State.vitality.tapProgress, Is.EqualTo(expectedTaps),
                        $"Mouse tap on visible surface point #{i} {pt} failed in stage '{stageId}'");

                    yield return TouchTap(touchId++, pt);
                    expectedTaps++;
                    Assert.That(b.State.vitality.tapProgress, Is.EqualTo(expectedTaps),
                        $"Touch tap on visible surface point #{i} {pt} failed in stage '{stageId}'");
                }

                if (stageId == "large_tree")
                {
                    // Test 3 near-silhouette empty points via Mouse and Touch (must NOT increment tapProgress)
                    int emptyIndicesStep = Mathf.Max(1, nearEmptyPoints.Count / 3);
                    for (int i = 0; i < 3; i++)
                    {
                        var emptyPt = nearEmptyPoints[Mathf.Min(nearEmptyPoints.Count - 1, i * emptyIndicesStep)];
                        yield return MouseTap(emptyPt);
                        Assert.That(b.State.vitality.tapProgress, Is.EqualTo(expectedTaps),
                            $"False positive Mouse tap on near-silhouette empty point #{i} {emptyPt} in stage '{stageId}'");

                        yield return TouchTap(touchId++, emptyPt);
                        Assert.That(b.State.vitality.tapProgress, Is.EqualTo(expectedTaps),
                            $"False positive Touch tap on near-silhouette empty point #{i} {emptyPt} in stage '{stageId}'");
                    }
                }
                else
                {
                    // For low-stage trees (seedling, young_tree), near-silhouette points inside the sphere are valid taps.
                    // Verify that far-outside background points beyond the sphere do NOT increment tapProgress.
                    var farPoints = new[]
                    {
                        new Vector2(Screen.width * 0.12f, Screen.height * 0.5f),
                        new Vector2(Screen.width * 0.88f, Screen.height * 0.5f),
                        new Vector2(Screen.width * 0.20f, Screen.height * 0.85f)
                    };
                    for (int i = 0; i < farPoints.Length; i++)
                    {
                        var farPt = farPoints[i];
                        if (b.Input.IsOverUi(farPt)) continue;
                        yield return MouseTap(farPt);
                        Assert.That(b.State.vitality.tapProgress, Is.EqualTo(expectedTaps),
                            $"False positive Mouse tap on far background point #{i} {farPt} in stage '{stageId}'");

                        yield return TouchTap(touchId++, farPt);
                        Assert.That(b.State.vitality.tapProgress, Is.EqualTo(expectedTaps),
                            $"False positive Touch tap on far background point #{i} {farPt} in stage '{stageId}'");
                    }
                }

                // Explicit regression check for large_tree at default pose from independent review
                if (stageId == "large_tree")
                {
                    var reviewEmptyPt = new Vector2(Screen.width * (611.49f / 1920f), Screen.height * (629.6163f / 1080f));
                    var reviewModelPt = new Vector2(Screen.width * (573.295166f / 1920f), Screen.height * (412.329346f / 1080f));

                    Assert.That(tree.GetComponent<SphereCollider>().Raycast(rig.SceneCamera.ScreenPointToRay(reviewEmptyPt), out _, 100), Is.True,
                        "Historical mesh gap must be inside the new intentional spherical tap volume");
                    yield return MouseTap(reviewEmptyPt);
                    expectedTaps++;
                    Assert.That(b.State.vitality.tapProgress, Is.EqualTo(expectedTaps),
                        "Mesh gap inside spherical volume must accept Mouse tap");
                    yield return TouchTap(touchId++, reviewEmptyPt);
                    expectedTaps++;
                    Assert.That(b.State.vitality.tapProgress, Is.EqualTo(expectedTaps),
                        "Mesh gap inside spherical volume must accept Touch tap");

                    yield return MouseTap(reviewModelPt);
                    expectedTaps++;
                    Assert.That(b.State.vitality.tapProgress, Is.EqualTo(expectedTaps),
                        "Review visible foliage point (573.2952, 412.3293) must increment tapProgress on Mouse");
                    yield return TouchTap(touchId++, reviewModelPt);
                    expectedTaps++;
                    Assert.That(b.State.vitality.tapProgress, Is.EqualTo(expectedTaps),
                        "Review visible foliage point (573.2952, 412.3293) must increment tapProgress on Touch");
                }
            }
        }

        [UnityTest]
        public IEnumerator RaycastAccurateAcrossYawAnglesElevatedPitchAspectRatiosAndFrontOccluder()
        {
            var visual = b.Config.FindVisual("large_tree");
            tree.ShowStage(visual);
            rig.SetStagePose(visual.cameraDistance, visual.cameraPitch, visual.cameraTargetHeight);
            b.State.vitality.tapProgress = 0;
            int expectedTaps = 0;
            int touchId = 300;
            yield return null;

            // 1. Test 4 yaw angles: 0°, 90°, 180°, 270° (Orbit delta 500px * 0.18 deg/px = 90°)
            for (int yawStep = 0; yawStep < 4; yawStep++)
            {
                if (yawStep > 0)
                {
                    rig.Orbit(new Vector2(500f, 0f));
                    yield return null;
                }

                var sampleData = BuildStageMeshSampleData();
                CollectVisibleAndNearSilhouetteEmptyPoints(sampleData, out var visiblePts, out var emptyPts);
                Assert.That(visiblePts.Count, Is.GreaterThanOrEqualTo(2), $"No visible points at yaw step {yawStep}");
                Assert.That(emptyPts.Count, Is.GreaterThanOrEqualTo(2), $"No near-empty points at yaw step {yawStep}");

                yield return MouseTap(visiblePts[0]);
                expectedTaps++;
                Assert.That(b.State.vitality.tapProgress, Is.EqualTo(expectedTaps), $"Mouse visible tap failed at yaw step {yawStep}");

                yield return TouchTap(touchId++, visiblePts[visiblePts.Count - 1]);
                expectedTaps++;
                Assert.That(b.State.vitality.tapProgress, Is.EqualTo(expectedTaps), $"Touch visible tap failed at yaw step {yawStep}");

                yield return MouseTap(emptyPts[0]);
                Assert.That(b.State.vitality.tapProgress, Is.EqualTo(expectedTaps), $"Mouse empty tap false positive at yaw step {yawStep}");

                yield return TouchTap(touchId++, emptyPts[emptyPts.Count / 2]);
                Assert.That(b.State.vitality.tapProgress, Is.EqualTo(expectedTaps), $"Touch empty tap false positive at yaw step {yawStep}");
            }

            // 2. Elevated pitch pose (35 degrees)
            rig.Orbit(new Vector2(500f, 0f)); // Back to 360° (0°)
            rig.SetView(visual.cameraDistance, 35f);
            yield return null;
            {
                var sampleData = BuildStageMeshSampleData();
                CollectVisibleAndNearSilhouetteEmptyPoints(sampleData, out var visiblePts, out var emptyPts);
                yield return MouseTap(visiblePts[0]);
                expectedTaps++;
                Assert.That(b.State.vitality.tapProgress, Is.EqualTo(expectedTaps), "Elevated pitch visible tap failed");
                yield return MouseTap(emptyPts[0]);
                Assert.That(b.State.vitality.tapProgress, Is.EqualTo(expectedTaps), "Elevated pitch empty tap false positive");
            }

            // 3. Wider mobile landscape viewport aspect (20:9 letterboxed camera rect)
            var origRect = rig.SceneCamera.rect;
            rig.SceneCamera.rect = new Rect(0f, 0.1f, 1f, 0.8f);
            rig.SetStagePose(visual.cameraDistance, visual.cameraPitch, visual.cameraTargetHeight);
            yield return null;
            {
                var sampleData = BuildStageMeshSampleData();
                CollectVisibleAndNearSilhouetteEmptyPoints(sampleData, out var visiblePts, out var emptyPts);
                yield return TouchTap(touchId++, visiblePts[0]);
                expectedTaps++;
                Assert.That(b.State.vitality.tapProgress, Is.EqualTo(expectedTaps), "Wide aspect Touch visible tap failed");
                yield return TouchTap(touchId++, emptyPts[0]);
                Assert.That(b.State.vitality.tapProgress, Is.EqualTo(expectedTaps), "Wide aspect Touch empty tap false positive");
            }
            rig.SceneCamera.rect = origRect;
            yield return null;

            // 4. Front occluder collider (non-TreeView collider in front of tree must block tap)
            {
                var sampleData = BuildStageMeshSampleData();
                CollectVisibleAndNearSilhouetteEmptyPoints(sampleData, out var visiblePts, out _);
                var targetScreenPt = visiblePts[0];
                var ray = rig.SceneCamera.ScreenPointToRay(targetScreenPt);
                Assert.That(Physics.Raycast(ray, out var treeHit), Is.True);
                Assert.That(treeHit.collider.GetComponentInParent<TreeView>(), Is.EqualTo(tree));

                var occluder = GameObject.CreatePrimitive(PrimitiveType.Cube);
                occluder.name = "FrontOccluder";
                occluder.transform.position = ray.GetPoint(treeHit.distance * 0.5f);
                occluder.transform.localScale = new Vector3(0.5f, 0.5f, 0.1f);
                occluder.transform.rotation = Quaternion.LookRotation(ray.direction);
                Physics.SyncTransforms();

                yield return MouseTap(targetScreenPt);
                Assert.That(b.State.vitality.tapProgress, Is.EqualTo(expectedTaps),
                    "Tap blocked by front occluder must not increment tapProgress on Mouse");
                yield return TouchTap(touchId++, targetScreenPt);
                Assert.That(b.State.vitality.tapProgress, Is.EqualTo(expectedTaps),
                    "Tap blocked by front occluder must not increment tapProgress on Touch");

                Object.DestroyImmediate(occluder);
                Physics.SyncTransforms();

                yield return MouseTap(targetScreenPt);
                expectedTaps++;
                Assert.That(b.State.vitality.tapProgress, Is.EqualTo(expectedTaps),
                    "Tap after removing front occluder must increment tapProgress");
            }
        }

        [UnityTest]
        public IEnumerator WeatherAndCosmeticAnchorsFollowStageTransitionsWithoutBlockingRaycast()
        {
            // Buy & equip glow_placeholder
            b.State.permanent.coin = 50;
            Assert.That(b.Cosmetics.TryBuy("glow_placeholder").Success, Is.True);
            Assert.That(b.Cosmetics.TryEquip("glow_placeholder").Success, Is.True);
            yield return null;

            var glowGo = GameObject.Find("GlowPlaceholder_Effect");
            Assert.That(glowGo, Is.Not.Null);
            Assert.That(glowGo.GetComponent<Collider>(), Is.Null);
            Assert.That(glowGo.transform.parent, Is.EqualTo(tree.TrunkAnchor));

            // Trigger lightning & burning on leaves
            b.Weather.TriggerStrike("leaves");
            yield return null;

            var burningGo = GameObject.Find("BurningIndicator");
            Assert.That(burningGo, Is.Not.Null);
            Assert.That(burningGo.activeInHierarchy, Is.True);
            Assert.That(burningGo.GetComponent<Collider>(), Is.Null);
            Assert.That(burningGo.transform.parent, Is.EqualTo(tree.LeavesAnchor));

            var boltGo = GameObject.Find("LightningBolt");
            Assert.That(boltGo, Is.Not.Null);
            var lr = boltGo.GetComponent<LineRenderer>();
            Assert.That(lr.GetPosition(3), Is.EqualTo(tree.LightningTargetAnchor.position));

            // Evolve seedling -> young_tree -> large_tree and verify anchors & raycast still work
            b.State.run.water = 5000;
            b.State.run.growth = 5000;b.State.run.photosynthesis=5000;
            Assert.That(b.Evolution.TryEvolve().Success, Is.True);
            yield return new WaitForSecondsRealtime(b.Config.EvolutionTransitionSeconds + 0.1f);
            Assert.That(tree.CurrentStageId, Is.EqualTo("young_tree"));
            Assert.That(glowGo.transform.position, Is.EqualTo(tree.CurrentStageAnchors.TrunkAuraAnchor.position));
            Assert.That(burningGo.transform.position, Is.EqualTo(tree.CurrentStageAnchors.LeavesCenterAnchor.position));

            b.State.vitality.tapProgress = 0;
            var youngSample = BuildStageMeshSampleData();
            CollectVisibleAndNearSilhouetteEmptyPoints(youngSample, out var youngVisible, out _);
            yield return MouseTap(youngVisible[0]);
            Assert.That(b.State.vitality.tapProgress, Is.EqualTo(1), "Aura or burning VFX must not block raycast tap on young_tree.");

            // Evolve young_tree -> large_tree
            b.State.run.water = 50000;
            b.State.run.growth = 50000;b.State.run.photosynthesis=50000;
            Assert.That(b.Evolution.TryEvolve().Success, Is.True);
            yield return new WaitForSecondsRealtime(b.Config.EvolutionTransitionSeconds + 0.1f);
            Assert.That(tree.CurrentStageId, Is.EqualTo("large_tree"));

            // Voluntary Rebirth -> resets back to seedling
            b.State.run.growth = 100000;b.State.run.photosynthesis=100000;
            Assert.That(b.Rebirth.TryRebirth(b.State.run.runId).Success, Is.True);
            tree.ShowStage(b.Config.FindVisual(b.State.run.stageId));
            yield return null;
            Assert.That(tree.CurrentStageId, Is.EqualTo("seedling"));

            // Forced Rebirth when dead -> resets back to seedling and saves cleanly
            b.State.run.life = "DEAD";
            b.State.run.hp = 0;
            Assert.That(b.Rebirth.TryRebirth(b.State.run.runId).Success, Is.True);
            Assert.That(b.State.run.life, Is.EqualTo("ALIVE"));
            Assert.That(b.SaveCtrl.ForceSave("TEST_SAVE").Success, Is.True);
        }

        [UnityTest]
        public IEnumerator Seedling_CulledBackfaceAndUndergroundGroundClicks_DoNotCountTaps()
        {
            tree.ShowStage(b.Config.FindVisual("seedling"));
            rig.SetStagePose(1.4f, 16f, 0.12f);
            yield return null;
            Physics.SyncTransforms();

            b.State.vitality.tapProgress = 0;
            Vector2 backfacePoint = new Vector2(913.731262f, 453.8399f);
            yield return MouseTap(backfacePoint);
            Assert.That(b.State.vitality.tapProgress, Is.EqualTo(0), "Clicking culled backface of SeedShell must not increment tapProgress.");

            Vector2 groundPoint = new Vector2(913.731262f, 407.544922f);
            yield return MouseTap(groundPoint);
            Assert.That(b.State.vitality.tapProgress, Is.EqualTo(0), "Clicking underground SeedShell occluded by Ground must not increment tapProgress.");
        }
    }
}
