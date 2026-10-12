using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using CayTheGioi.Bootstrap;
using CayTheGioi.Infrastructure;
using CayTheGioi.Presentation;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace CayTheGioi.Tests
{
    public sealed class VisualPolishBenchmarkTests
    {
        private GameBootstrapper b;
        private TreeView tree;
        private OrbitCameraRig rig;
        private Canvas hudCanvas;
        private SafeAreaHudLayout safeAreaHud;
        private ReferenceUiLayout referenceUiLayout;

        [UnitySetUp]
        public IEnumerator Setup()
        {
            SaveTestScope.Begin();
            yield return SceneManager.LoadSceneAsync("MainScene");
            yield return null;

            b = Object.FindAnyObjectByType<GameBootstrapper>();
            Assert.That(b, Is.Not.Null);
            b.TickDriver.ManualTick = true;

            tree = Object.FindAnyObjectByType<TreeView>();
            rig = Object.FindAnyObjectByType<OrbitCameraRig>();
            hudCanvas = GameObject.Find("HUD")?.GetComponent<Canvas>();
            safeAreaHud = hudCanvas?.GetComponent<SafeAreaHudLayout>();
            referenceUiLayout = hudCanvas?.GetComponent<ReferenceUiLayout>();

            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            SaveTestScope.End();
            yield return null;
        }

        [UnityTest]
        [Ignore("Baseline already recorded")]
        public IEnumerator T14_Run_Baseline_Benchmark_And_Screenshots()
        {
            yield return RunBenchmarkAndCapture("Before");
        }

        [UnityTest]
        public IEnumerator T14_Run_PostPolish_Benchmark_And_Screenshots()
        {
            yield return RunBenchmarkAndCapture("After");
        }

        private IEnumerator RunBenchmarkAndCapture(string phase)
        {
            string baseDir = Path.Combine(Application.dataPath, "..", "Evidence", "T14_Visual_Polish", phase);
            string dir1366 = Path.Combine(baseDir, "1366x768");
            string dir1920 = Path.Combine(baseDir, "1920x1080");
            Directory.CreateDirectory(dir1366);
            Directory.CreateDirectory(dir1920);

            string[] stages = { "seedling", "young_tree", "large_tree" };
            string[] weathers = { "calm", "sunny", "rainy", "thunderstorm" };
            float[] yaws = { 0f, 90f, 180f, 270f };

            var csvSb = new StringBuilder();
            csvSb.AppendLine("Phase,Stage,Weather,Yaw,Resolution,FPS,FrameTimeMs,Batches,DrawCalls,SetPass,Triangles");

            var txtSb = new StringBuilder();
            txtSb.AppendLine($"================================================================================");
            txtSb.AppendLine($"T14 VISUAL POLISH PERFORMANCE BENCHMARK REPORT — PHASE: {phase.ToUpper()}");
            txtSb.AppendLine($"Scope: Unity Editor PlayMode (Real Frame Loop, Not Standalone Build)");
            txtSb.AppendLine($"Machine: {SystemInfo.processorType} | {SystemInfo.graphicsDeviceName} ({SystemInfo.graphicsMemorySize}MB)");
            txtSb.AppendLine($"Date: {System.DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
            txtSb.AppendLine($"================================================================================");
            txtSb.AppendLine(string.Format("{0,-12} {1,-14} {2,-6} {3,-8} {4,-12} {5,-9} {6,-9} {7,-9} {8,-10}",
                "Stage", "Weather", "Yaw", "FPS", "FrameTime(ms)", "Batches", "DrawCalls", "SetPass", "Tris"));
            txtSb.AppendLine(new string('-', 92));

            string evidenceDir = Path.Combine(Application.dataPath, "..", "Evidence", "T14_Visual_Polish");
            string csvFile = Path.Combine(evidenceDir, $"{phase.ToLower()}-performance.csv");
            string txtFile = Path.Combine(evidenceDir, $"{phase.ToLower()}-performance.txt");

            // Setup camera and canvas
            var cam = rig.SceneCamera;
            if (cam == null) cam = Camera.main;

            foreach (string stage in stages)
            {
                b.State.run.stageId = stage;
                var visual = b.Config.FindVisual(stage);
                tree.ShowStage(visual);
                rig.SetStagePose(visual.cameraDistance, visual.cameraPitch, visual.cameraTargetHeight);
                yield return null;

                foreach (string weather in weathers)
                {
                    b.Weather.ForceWeather(weather, 60.0);
                    yield return null;

                    foreach (float yaw in yaws)
                    {
                        rig.SetYaw(yaw);
                        yield return null;

                        // Warm up 2 gameplay frames
                        yield return null;
                        yield return null;

                        // Benchmark real gameplay frames (across actual frame loop)
                        int sampleFrames = 10;
                        var sw = Stopwatch.StartNew();
                        for (int f = 0; f < sampleFrames; f++)
                        {
                            yield return null;
                        }
                        sw.Stop();

                        double totalMs = sw.Elapsed.TotalMilliseconds;
                        double frameTimeMs = totalMs / sampleFrames;
                        double fps = 1000.0 / Mathf.Max(0.001f, (float)frameTimeMs);

                        int batches = UnityStats.dynamicBatches + UnityStats.staticBatches + UnityStats.instancedBatches;
                        int drawCalls = UnityStats.drawCalls;
                        int setPass = UnityStats.setPassCalls;
                        int triangles = UnityStats.triangles;

                        csvSb.AppendLine($"{phase},{stage},{weather},{yaw},1920x1080,{fps:F1},{frameTimeMs:F3},{batches},{drawCalls},{setPass},{triangles}");
                        txtSb.AppendLine(string.Format("{0,-12} {1,-14} {2,-6:F0} {3,-8:F1} {4,-12:F3} {5,-9} {6,-9} {7,-9} {8,-10}",
                            stage, weather, yaw, fps, frameTimeMs, batches, drawCalls, setPass, triangles));

                        File.WriteAllText(csvFile, csvSb.ToString(), Encoding.UTF8);
                        File.WriteAllText(txtFile, txtSb.ToString(), Encoding.UTF8);

                        // Capture screenshot at 1920x1080
                        string shot1920Name = $"{stage}_{weather}_yaw{yaw:F0}.png";
                        CaptureViewport(cam, 1920, 1080, Path.Combine(dir1920, shot1920Name));

                        // Capture screenshot at 1366x768
                        string shot1366Name = $"{stage}_{weather}_yaw{yaw:F0}.png";
                        CaptureViewport(cam, 1366, 768, Path.Combine(dir1366, shot1366Name));

                        yield return null;
                    }
                }
            }

            // Capture UI screenshots (Upgrade Panel and Shop)
            var upgradeView = Object.FindAnyObjectByType<UpgradePanelView>();
            if (upgradeView != null)
            {
                if (!upgradeView.IsOpen) upgradeView.Toggle();
                if (safeAreaHud != null) safeAreaHud.Apply(new Rect(0, 0, 1920, 1080), new Vector2(1920, 1080));
                if (referenceUiLayout != null) referenceUiLayout.Layout();
                Canvas.ForceUpdateCanvases();
                yield return null;
                CaptureViewport(cam, 1920, 1080, Path.Combine(dir1920, "ui_upgrade.png"));
                upgradeView.Close();
                yield return null;
            }

            b.Shop.Open();
            if (referenceUiLayout != null) referenceUiLayout.Layout();
            Canvas.ForceUpdateCanvases();
            yield return null;
            CaptureViewport(cam, 1920, 1080, Path.Combine(dir1920, "ui_shop.png"));
            b.Shop.Close();
            yield return null;

            File.WriteAllText(csvFile, csvSb.ToString(), Encoding.UTF8);
            File.WriteAllText(txtFile, txtSb.ToString(), Encoding.UTF8);

            UnityEngine.Debug.Log($"[T14] Saved {phase} benchmark report to: {csvFile} and {txtFile}");
            yield return null;
        }

        private void CaptureViewport(Camera cam, int width, int height, string fullPath)
        {
            var prevMode = hudCanvas != null ? hudCanvas.renderMode : RenderMode.ScreenSpaceOverlay;
            var prevCam = hudCanvas != null ? hudCanvas.worldCamera : null;

            var rt = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
            var captureGo = new GameObject("CaptureCamTemp", typeof(Camera));
            var captureCam = captureGo.GetComponent<Camera>();
            captureCam.CopyFrom(cam);
            captureCam.targetTexture = rt;

            try
            {
                if (hudCanvas != null)
                {
                    hudCanvas.renderMode = RenderMode.ScreenSpaceCamera;
                    hudCanvas.worldCamera = captureCam;
                    hudCanvas.planeDistance = 1.0f;
                    Canvas.ForceUpdateCanvases();
                }

                captureCam.Render();

                RenderTexture.active = rt;
                Texture2D tex = new Texture2D(width, height, TextureFormat.RGB24, false);
                tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                tex.Apply();

                File.WriteAllBytes(fullPath, tex.EncodeToPNG());
                Object.DestroyImmediate(tex);
            }
            finally
            {
                RenderTexture.active = null;
                if (hudCanvas != null)
                {
                    hudCanvas.renderMode = prevMode;
                    hudCanvas.worldCamera = prevCam;
                    Canvas.ForceUpdateCanvases();
                }
                captureCam.targetTexture = null;
                Object.DestroyImmediate(captureGo);
                rt.Release();
                Object.DestroyImmediate(rt);
            }
        }
    }
}
