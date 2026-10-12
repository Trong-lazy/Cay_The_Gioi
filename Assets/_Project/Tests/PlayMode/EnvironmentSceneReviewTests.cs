using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using CayTheGioi.Bootstrap;
using CayTheGioi.Infrastructure;
using CayTheGioi.MapEnvironment;
using CayTheGioi.Presentation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace CayTheGioi.Tests
{
    public sealed class EnvironmentSceneReviewTests
    {
        private sealed class Prop
        {
            public MeshFilter Filter;
            public MeshRenderer Renderer;
            public Vector3[] Vertices;
            public int[] Triangles;
        }

        [UnitySetUp]
        public IEnumerator Setup()
        {
            SaveTestScope.Begin();
            SceneManager.sceneLoaded += Freeze;
            yield return SceneManager.LoadSceneAsync("MainScene");
            yield return null;
        }
        private void Freeze(Scene scene, LoadSceneMode mode)
        { Object.FindAnyObjectByType<OnlineTickDriver>().ManualTick = true; }
        [UnityTearDown]
        public IEnumerator TearDown()
        { SceneManager.sceneLoaded -= Freeze; SaveTestScope.End(); yield return null; }

        [UnityTest]
        public IEnumerator RealMap_OrbitOcclusionHudAndUpgradeEvidence()
        {
            var b = Object.FindAnyObjectByType<GameBootstrapper>();
            foreach (var step in b.Tutorial.StepDefinitions)
                if (!b.State.permanent.tutorialCompletedStepIds.Contains(step.StepId)) b.State.permanent.tutorialCompletedStepIds.Add(step.StepId);
            Object.FindAnyObjectByType<TutorialView>().Refresh();
            var environment = Object.FindAnyObjectByType<WorldEnvironmentController>();
            Assert.That(environment, Is.Not.Null);
            var tree = Object.FindAnyObjectByType<TreeView>();
            var rig = Object.FindAnyObjectByType<OrbitCameraRig>();
            rig.OrbitLocked = false;
            var props = environment.GetComponentsInChildren<MeshFilter>()
                .Where(filter => filter.name != "Planet_Terrain" && filter.name != "Lake_Surface" && filter.name != "Waterfall")
                .Select(filter => new Prop { Filter = filter, Renderer = filter.GetComponent<MeshRenderer>(), Vertices = filter.sharedMesh.vertices, Triangles = filter.sharedMesh.triangles }).ToArray();
            string directory = Path.GetFullPath("Evidence/ReviewFix_20261012/EnvironmentScreenshots/" + Screen.width + "x" + Screen.height);
            Directory.CreateDirectory(directory);
            var measurements = new StringBuilder();
            int totalBlocked = 0;
            foreach (string stage in new[] { "seedling", "young_tree", "large_tree" })
            {
                b.State.run.stageId = stage;
                var visual = b.Config.FindVisual(stage);
                tree.ShowStage(visual);
                Object.FindAnyObjectByType<FoundationHudView>().Bind(b.State, stage == "seedling" ? "Mầm" : stage == "young_tree" ? "Cây Non" : "Cây Lớn", 50);
                foreach (float pitch in new[] { visual.cameraPitch, 10f, 70f })
                {
                    rig.SetStagePose(visual.cameraDistance, pitch, visual.cameraTargetHeight);
                    for (int angle = 0; angle < 12; angle++)
                    {
                        yield return null;
                        yield return new WaitForEndOfFrame();
                        Physics.SyncTransforms();
                        int sampled = 0, blocked = 0;
                        var colliders = tree.CurrentStageAnchors.GetComponentsInChildren<MeshCollider>();
                        foreach (var filter in tree.CurrentStageAnchors.GetComponentsInChildren<MeshFilter>())
                        {
                            var vertices = filter.sharedMesh.vertices;
                            var triangles = filter.sharedMesh.triangles;
                            int stride = Mathf.Max(3, triangles.Length / 24 / 3 * 3);
                            for (int triangle = 0; triangle + 2 < triangles.Length; triangle += stride)
                            {
                                Vector3 point = filter.transform.TransformPoint((vertices[triangles[triangle]] + vertices[triangles[triangle+1]] + vertices[triangles[triangle+2]]) / 3);
                                Vector3 viewport = rig.SceneCamera.WorldToViewportPoint(point);
                                if (viewport.z <= 0 || viewport.x < .04f || viewport.x > .96f || viewport.y < .04f || viewport.y > .96f) continue;
                                var ray = rig.SceneCamera.ViewportPointToRay(viewport);
                                float distance = float.PositiveInfinity;
                                foreach (var collider in colliders)
                                    if (collider.Raycast(ray, out var hit, 50)) distance = Mathf.Min(distance, hit.distance);
                                if (float.IsPositiveInfinity(distance)) continue;
                                sampled++;
                                foreach (var prop in props)
                                    if (Occludes(prop, ray, distance))
                                    {
                                        blocked++;
                                        measurements.AppendLine(stage + " pitch=" + pitch + " yaw=" + angle * 30 + " occluder=" + prop.Filter.name + " position=" + prop.Filter.transform.position);
                                        break;
                                    }
                            }
                        }
                        measurements.AppendLine(stage + " pitch=" + pitch + " yaw=" + angle * 30 + " sampled=" + sampled + " blocked=" + blocked);
                        totalBlocked += blocked;
                        if (pitch == visual.cameraPitch && angle % 3 == 0)
                        {
                            ScreenCapture.CaptureScreenshot(Path.Combine(directory, stage + "_yaw" + angle * 30 + ".png"));
                            yield return null;
                        }
                        rig.Orbit(new Vector2(30 / .18f, 0));
                    }
                }
            }
            File.WriteAllText(Path.Combine(directory, "orbit-metrics.txt"), measurements.ToString());
            Assert.That(totalBlocked, Is.Zero, "Environment props hide sampled tree surfaces; see orbit-metrics.txt");
            var upgrade = Object.FindAnyObjectByType<UpgradePanelView>();
            rig.SetStagePose(b.Config.FindVisual("large_tree").cameraDistance, b.Config.FindVisual("large_tree").cameraPitch, b.Config.FindVisual("large_tree").cameraTargetHeight);
            Object.FindAnyObjectByType<EvolutionPanelView>().Refresh();
            upgrade.OpenButton.onClick.Invoke();
            yield return null;
            Canvas.ForceUpdateCanvases();
            yield return new WaitForEndOfFrame();
            ScreenCapture.CaptureScreenshot(Path.Combine(directory, "upgrade-prices.png"));
            yield return null;
            foreach (string branch in new[] { "root", "leaves", "trunk" })
            {
                var label = upgrade.FindRow(branch).button.GetComponentInChildren<Text>();
                Assert.That(label.text, Does.Not.Contain("+1"));
                Assert.That(label.text, Does.Contain(branch == "root" ? "Water" : branch == "leaves" ? "Quang hợp" : "Growth"));
                // preferredHeight uses the requested font size, not the generated best-fit size.
                // Check actual glyph geometry after the Canvas has rendered instead.
                var glyphs = label.cachedTextGenerator.verts;
                var rect = ((RectTransform)label.transform).rect;
                for (int i = 0; i < glyphs.Count - 4; i++)
                {
                    var position = glyphs[i].position / label.pixelsPerUnit;
                    Assert.That(position.y, Is.InRange(rect.yMin-2, rect.yMax+2), branch + " price glyph outside button");
                }
            }
            yield return new WaitForEndOfFrame();
            ScreenCapture.CaptureScreenshot(Path.Combine(directory, "upgrade-prices.png"));
            yield return null;
        }

        private static bool Occludes(Prop prop, Ray ray, float distance)
        {
            if (!prop.Renderer.bounds.IntersectRay(ray, out float entry) || entry > distance) return false;
            Vector3 origin = prop.Filter.transform.InverseTransformPoint(ray.origin);
            Vector3 direction = prop.Filter.transform.InverseTransformVector(ray.direction);
            for (int i = 0; i < prop.Triangles.Length; i += 3)
            {
                Vector3 a = prop.Vertices[prop.Triangles[i]], b = prop.Vertices[prop.Triangles[i+1]], c = prop.Vertices[prop.Triangles[i+2]];
                Vector3 ab = b-a, ac = c-a, cross = Vector3.Cross(direction, ac);
                float determinant = Vector3.Dot(ab, cross);
                if (Mathf.Abs(determinant) < 1e-8f) continue;
                float inverse = 1 / determinant;
                Vector3 offset = origin-a;
                float u = Vector3.Dot(offset, cross) * inverse;
                if (u < 0 || u > 1) continue;
                Vector3 q = Vector3.Cross(offset, ab);
                float v = Vector3.Dot(direction, q) * inverse;
                if (v < 0 || u+v > 1) continue;
                float hit = Vector3.Dot(ac, q) * inverse;
                if (hit > 0 && hit < distance-.002f) return true;
            }
            return false;
        }
    }
}
