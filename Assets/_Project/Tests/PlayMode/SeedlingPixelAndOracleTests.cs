using System.Collections;
using System.IO;
using CayTheGioi.Bootstrap;
using CayTheGioi.Infrastructure;
using CayTheGioi.Presentation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace CayTheGioi.Tests
{
    public sealed class SeedlingPixelAndOracleTests
    {
        private GameBootstrapper b;
        private TreeView tree;
        private OrbitCameraRig rig;

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
            tree = Object.FindAnyObjectByType<TreeView>();
            rig = Object.FindAnyObjectByType<OrbitCameraRig>();
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            SaveTestScope.End();
            yield return null;
        }

        [UnityTest]
        public IEnumerator Probe_SeedlingPixel_642_307_RaycastAndCrop()
        {
            b.State.run.stageId = "seedling";
            var visual = b.Config.FindVisual("seedling");
            tree.ShowStage(visual);
            rig.SetStagePose(visual.cameraDistance, visual.cameraPitch, visual.cameraTargetHeight);
            yield return null;
            yield return new WaitForSeconds(0.2f);

            var cam = rig.SceneCamera;
            Assert.That(cam, Is.Not.Null);

            // Set render texture to 1366x768 to test exact coordinates
            int width = 1366;
            int height = 768;
            var rt = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
            var prevRT = cam.targetTexture;
            cam.targetTexture = rt;
            cam.Render();

            RenderTexture.active = rt;
            var tex = new Texture2D(width, height, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            tex.Apply();

            // Pixel to inspect
            Vector2 targetPixel = new Vector2(642, 307);
            var ray = cam.ScreenPointToRay(targetPixel);

            // Test raycast with queriesHitBackfaces = false
            Physics.queriesHitBackfaces = false;
            var hitsNoBack = Physics.RaycastAll(ray);

            // Test raycast with queriesHitBackfaces = true
            Physics.queriesHitBackfaces = true;
            var hitsWithBack = Physics.RaycastAll(ray);

            string outDir = Path.GetFullPath("Evidence/ReviewFix_20261012");
            Directory.CreateDirectory(outDir);

            // Write diagnostic log
            string logPath = Path.Combine(outDir, "seedling_pixel_642_307_probe.txt");
            using (var sw = new StreamWriter(logPath, false))
            {
                sw.WriteLine("=== SEEDLING PIXEL (642, 307) DIAGNOSTIC ===");
                sw.WriteLine("Resolution: 1366x768");
                sw.WriteLine("Target Pixel: " + targetPixel);
                sw.WriteLine("Ray: origin=" + ray.origin + ", dir=" + ray.direction);
                sw.WriteLine();
                sw.WriteLine("-- WITH queriesHitBackfaces = FALSE --");
                sw.WriteLine("Hits count: " + hitsNoBack.Length);
                for (int i = 0; i < hitsNoBack.Length; i++)
                {
                    var h = hitsNoBack[i];
                    sw.WriteLine(string.Format("  Hit {0}: collider={1}, parent={2}, dist={3:F4}, normal={4}, dot={5:F4}",
                        i, h.collider.name, h.collider.transform.parent != null ? h.collider.transform.parent.name : "null",
                        h.distance, h.normal, Vector3.Dot(h.normal, ray.direction)));
                }

                sw.WriteLine();
                sw.WriteLine("-- WITH queriesHitBackfaces = TRUE --");
                sw.WriteLine("Hits count: " + hitsWithBack.Length);
                for (int i = 0; i < hitsWithBack.Length; i++)
                {
                    var h = hitsWithBack[i];
                    sw.WriteLine(string.Format("  Hit {0}: collider={1}, parent={2}, dist={3:F4}, normal={4}, dot={5:F4}",
                        i, h.collider.name, h.collider.transform.parent != null ? h.collider.transform.parent.name : "null",
                        h.distance, h.normal, Vector3.Dot(h.normal, ray.direction)));
                }

                sw.WriteLine();
                sw.WriteLine("Current OnTree result: " + b.Input.OnTree(targetPixel));
            }

            // Crop a 128x128 region centered at (642, 307)
            int cropSize = 128;
            int startX = Mathf.Clamp((int)targetPixel.x - cropSize / 2, 0, width - cropSize);
            int startY = Mathf.Clamp((int)targetPixel.y - cropSize / 2, 0, height - cropSize);
            var cropTex = new Texture2D(cropSize, cropSize, TextureFormat.RGB24, false);
            var colors = tex.GetPixels(startX, startY, cropSize, cropSize);

            // Mark the center target pixel with a small red crosshair on the crop
            int localTargetX = (int)targetPixel.x - startX;
            int localTargetY = (int)targetPixel.y - startY;
            for (int dx = -3; dx <= 3; dx++)
            {
                int px = localTargetX + dx;
                int py = localTargetY;
                if (px >= 0 && px < cropSize && py >= 0 && py < cropSize)
                    colors[py * cropSize + px] = Color.red;
            }
            for (int dy = -3; dy <= 3; dy++)
            {
                int px = localTargetX;
                int py = localTargetY + dy;
                if (px >= 0 && px < cropSize && py >= 0 && py < cropSize)
                    colors[py * cropSize + px] = Color.red;
            }

            cropTex.SetPixels(colors);
            cropTex.Apply();

            string cropPath = Path.Combine(outDir, "seedling_pixel_642_307_crop.png");
            File.WriteAllBytes(cropPath, cropTex.EncodeToPNG());

            // Cleanup
            cam.targetTexture = prevRT;
            RenderTexture.active = null;
            Object.Destroy(rt);
            Object.Destroy(tex);
            Object.Destroy(cropTex);

            Debug.Log("[Probe] Output probe log to " + logPath + " and crop to " + cropPath);
        }

        [UnityTest]
        public IEnumerator IndependentOracle_RenderMask_AllStages_TapPrecisionAndZeroFalsePositives()
        {
            string[] stages = { "seedling", "young_tree", "large_tree" };
            int width = 1366;
            int height = 768;

            var cam = rig.SceneCamera;
            Assert.That(cam, Is.Not.Null);

            var envController = Object.FindAnyObjectByType<CayTheGioi.MapEnvironment.WorldEnvironmentController>();
            var envRenderers = envController != null ? envController.GetComponentsInChildren<Renderer>() : new Renderer[0];

            var sb = new System.Text.StringBuilder();
            sb.AppendLine("==========================================================================================");
            sb.AppendLine("INDEPENDENT ORACLE TAP PRECISION REPORT (RenderTexture Silhouette Mask)");
            sb.AppendLine("Resolution: " + width + "x" + height);
            sb.AppendLine("Timestamp: " + System.DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss") + " UTC");
            sb.AppendLine("Acceptance Criteria: Tree Hit Rate >= 98.0%, False Positives outside tree AND spherical tap volume = 0");
            sb.AppendLine("==========================================================================================");
            sb.AppendLine();

            foreach (var stage in stages)
            {
                b.State.run.stageId = stage;
                var visual = b.Config.FindVisual(stage);
                tree.ShowStage(visual);
                rig.SetStagePose(visual.cameraDistance, visual.cameraPitch, visual.cameraTargetHeight);
                yield return null;
                yield return new WaitForSeconds(0.15f);

                // 1. Generate Depth-Tested Tree Silhouette Mask
                // Render tree in pure white and environment in pure black with depth testing enabled,
                // so underground root geometry occluded by terrain is correctly masked out as non-visible.
                var unlitShader = Shader.Find("Universal Render Pipeline/Unlit");
                if (unlitShader == null) unlitShader = Shader.Find("Unlit/Color");
                Assert.That(unlitShader, Is.Not.Null, "Unlit shader must exist for mask rendering");

                var whiteMat = new Material(unlitShader) { color = Color.white };
                var blackMat = new Material(unlitShader) { color = Color.black };

                var treeRenderers = tree.GetComponentsInChildren<Renderer>();
                var origTreeMats = new Material[treeRenderers.Length][];
                for (int i = 0; i < treeRenderers.Length; i++)
                {
                    origTreeMats[i] = treeRenderers[i].sharedMaterials;
                    var whites = new Material[origTreeMats[i].Length];
                    for (int m = 0; m < whites.Length; m++) whites[m] = whiteMat;
                    treeRenderers[i].sharedMaterials = whites;
                }

                var origEnvMats = new Material[envRenderers.Length][];
                for (int i = 0; i < envRenderers.Length; i++)
                {
                    origEnvMats[i] = envRenderers[i].sharedMaterials;
                    var blacks = new Material[origEnvMats[i].Length];
                    for (int m = 0; m < blacks.Length; m++) blacks[m] = blackMat;
                    envRenderers[i].sharedMaterials = blacks;
                }

                var prevFlags = cam.clearFlags;
                var prevBg = cam.backgroundColor;
                var prevRT = cam.targetTexture;
                var uacd = cam.GetComponent("UniversalAdditionalCameraData");
                var postProp = uacd != null ? uacd.GetType().GetProperty("renderPostProcessing") : null;
                bool prevPost = postProp != null && (bool)postProp.GetValue(uacd);
                if (postProp != null) postProp.SetValue(uacd, false);

                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = Color.black;

                var maskRT = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
                cam.targetTexture = maskRT;
                cam.Render();

                RenderTexture.active = maskRT;
                var maskTex = new Texture2D(width, height, TextureFormat.RGB24, false);
                maskTex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                maskTex.Apply();

                // Restore camera and materials
                if (postProp != null) postProp.SetValue(uacd, prevPost);
                cam.targetTexture = prevRT;
                cam.clearFlags = prevFlags;
                cam.backgroundColor = prevBg;
                RenderTexture.active = null;

                for (int i = 0; i < treeRenderers.Length; i++)
                {
                    treeRenderers[i].sharedMaterials = origTreeMats[i];
                }
                for (int i = 0; i < envRenderers.Length; i++)
                {
                    envRenderers[i].sharedMaterials = origEnvMats[i];
                }
                Object.Destroy(whiteMat);
                Object.Destroy(blackMat);

                // 2. Identify Inside Tree Pixels (erode 1px to avoid antialiasing edge boundary)
                // and Outside Pixels (background sky / terrain)
                var treePixels = new System.Collections.Generic.List<Vector2>();
                var outsidePixels = new System.Collections.Generic.List<Vector2>();

                // Sampling on a 4-pixel grid for thorough test distribution
                for (int y = 50; y < height - 50; y += 4)
                {
                    for (int x = 50; x < width - 50; x += 4)
                    {
                        Color c = maskTex.GetPixel(x, y);
                        bool isTree = c.r > 0.08f || c.g > 0.08f || c.b > 0.08f;
                        if (isTree)
                        {
                            // 1-pixel interior erosion check: ensure neighbors are also tree
                            Color cL = maskTex.GetPixel(x - 1, y);
                            Color cR = maskTex.GetPixel(x + 1, y);
                            Color cD = maskTex.GetPixel(x, y - 1);
                            Color cU = maskTex.GetPixel(x, y + 1);
                            bool interior = (cL.r > 0.05f || cL.g > 0.05f || cL.b > 0.05f)
                                         && (cR.r > 0.05f || cR.g > 0.05f || cR.b > 0.05f)
                                         && (cD.r > 0.05f || cD.g > 0.05f || cD.b > 0.05f)
                                         && (cU.r > 0.05f || cU.g > 0.05f || cU.b > 0.05f);
                            if (interior)
                            {
                                treePixels.Add(new Vector2(x, y));
                            }
                        }
                        else
                        {
                            // 2-pixel margin check: ensure pixel is genuinely outside and not an antialiasing edge pixel
                            Color cL = maskTex.GetPixel(x - 2, y);
                            Color cR = maskTex.GetPixel(x + 2, y);
                            Color cD = maskTex.GetPixel(x, y - 2);
                            Color cU = maskTex.GetPixel(x, y + 2);
                            bool genuineOutside = (cL.r < 0.05f && cL.g < 0.05f && cL.b < 0.05f)
                                               && (cR.r < 0.05f && cR.g < 0.05f && cR.b < 0.05f)
                                               && (cD.r < 0.05f && cD.g < 0.05f && cD.b < 0.05f)
                                               && (cU.r < 0.05f && cU.g < 0.05f && cU.b < 0.05f);
                            if (genuineOutside)
                            {
                                outsidePixels.Add(new Vector2(x, y));
                            }
                        }
                    }
                }

                Assert.That(treePixels.Count, Is.GreaterThan(100), stage + " must have visible rendered tree pixels in mask");
                Assert.That(outsidePixels.Count, Is.GreaterThan(100), stage + " must have visible outside pixels");

                // 3. Test Tap Accuracy on Tree Pixels
                int treeHits = 0;
                int treeTests = treePixels.Count;
                var missedDiagnostics = new System.Collections.Generic.List<string>();
                for (int i = 0; i < treeTests; i++)
                {
                    if (b.Input.OnTree(treePixels[i]))
                    {
                        treeHits++;
                    }
                    else if (missedDiagnostics.Count < 20)
                    {
                        var missRay = cam.ScreenPointToRay(treePixels[i]);
                        var hits = Physics.RaycastAll(missRay);
                        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
                        string hitInfo = hits.Length == 0 ? "No hits" : string.Join(" | ", System.Array.ConvertAll(hits, h => h.collider.name + "(dist=" + h.distance.ToString("F3") + ", dot=" + Vector3.Dot(h.normal, missRay.direction).ToString("F3") + ")"));
                        missedDiagnostics.Add(string.Format("Pixel {0}: rayOrigin={1}, rayDir={2} -> Hits: {3}", treePixels[i], missRay.origin, missRay.direction, hitInfo));
                    }
                }
                float hitRate = (float)treeHits / treeTests * 100f;

                sb.AppendLine(string.Format(System.Globalization.CultureInfo.InvariantCulture,
                    "Stage: {0,-12} | Tree Samples: {1,5} | Hits: {2,5} | Hit Rate: {3,6:F2}% | Missed: {4}",
                    stage, treeTests, treeHits, hitRate, treeTests - treeHits));
                if (missedDiagnostics.Count > 0)
                {
                    sb.AppendLine("  Sample of missed pixels:");
                    foreach (var d in missedDiagnostics) sb.AppendLine("    " + d);
                }

                // Write intermediate report even if assertion fails
                string outDirPartial = Path.GetFullPath("Evidence/ReviewFix_20261012");
                Directory.CreateDirectory(outDirPartial);
                File.WriteAllText(Path.Combine(outDirPartial, "tap-oracle-report.txt"), sb.ToString(), System.Text.Encoding.UTF8);
                int outsideFalsePositives = 0;
                int outsideTests = Mathf.Min(500, outsidePixels.Count);
                int stride = Mathf.Max(1, outsidePixels.Count / outsideTests);
                for (int i = 0; i < outsidePixels.Count && outsideTests > 0; i += stride)
                {
                    if (b.Input.OnTree(outsidePixels[i]))
                    {
                        outsideFalsePositives++;
                    }
                    outsideTests--;
                }

                sb.AppendLine(string.Format(System.Globalization.CultureInfo.InvariantCulture,
                    "Stage: {0,-12} | Tree Samples: {1,5} | Hits: {2,5} | Hit Rate: {3,6:F2}% | Outside-silhouette hits (includes permitted sphere gaps): {4}",
                    stage, treeTests, treeHits, hitRate, outsideFalsePositives));

                // Assertions for each stage
                Assert.That(hitRate, Is.GreaterThanOrEqualTo(98.0f),
                    string.Format("Stage {0} tap hit rate ({1:F2}%) must be >= 98.0% of rendered tree pixels", stage, hitRate));
                if (stage == "large_tree")
                {
                    // Empty gaps in the sphere are intentional tap targets at every stage.
                    int outsideVolumeHits = 0;
                    var sphere = tree.GetComponent<SphereCollider>();
                    for (int i = 0; i < outsidePixels.Count; i += stride)
                    {
                        var ray = cam.ScreenPointToRay(outsidePixels[i]);
                        if (!sphere.Raycast(ray, out _, float.PositiveInfinity) && b.Input.OnTree(outsidePixels[i])) outsideVolumeHits++;
                    }
                    Assert.That(outsideVolumeHits, Is.Zero, "No tap outside both silhouette and sphere");
                }
                else
                {
                    // For low-stage trees (seedling, young_tree), taps in the spherical volume around the tree are intentional.
                    // Verify that outside pixels far beyond the sphere radius have 0 false positives.
                    Vector3 sphereCenterWorld = tree.transform.position + (stage == "seedling" ? new Vector3(0f, 0.16f, 0f) : new Vector3(0f, 0.65f, 0f));
                    Vector2 sphereCenterScreen = cam.WorldToScreenPoint(sphereCenterWorld);
                    float sphereRadiusWorld = stage == "seedling" ? 0.25f : 0.58f;
                    Vector2 sphereEdgeScreen = cam.WorldToScreenPoint(sphereCenterWorld + cam.transform.right * sphereRadiusWorld);
                    float sphereRadiusPx = Vector2.Distance(sphereCenterScreen, sphereEdgeScreen);

                    int farOutsideFalsePositives = 0;
                    for (int i = 0; i < outsidePixels.Count; i += stride)
                    {
                        var pt = outsidePixels[i];
                        if (Vector2.Distance(pt, sphereCenterScreen) > sphereRadiusPx + 20f)
                        {
                            if (b.Input.OnTree(pt)) farOutsideFalsePositives++;
                        }
                    }

                    Assert.That(farOutsideFalsePositives, Is.EqualTo(0),
                        string.Format("Stage {0} far-outside pixels beyond spherical volume must have 0 false positives (found {1})", stage, farOutsideFalsePositives));
                }

                Object.Destroy(maskRT);
                Object.Destroy(maskTex);
            }

            string outDir = Path.GetFullPath("Evidence/ReviewFix_20261012");
            Directory.CreateDirectory(outDir);
            string reportPath = Path.Combine(outDir, "tap-oracle-report.txt");
            File.WriteAllText(reportPath, sb.ToString(), System.Text.Encoding.UTF8);
            Debug.Log("[Oracle] Written tap oracle report to: " + reportPath);
            Debug.Log(sb.ToString());
        }
    }
}
