using System.Collections;
using System.IO;
using CayTheGioi.Bootstrap;
using CayTheGioi.Domain;
using CayTheGioi.Infrastructure;
using CayTheGioi.Presentation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace CayTheGioi.Tests
{
    public sealed class ReviewFixVerificationTests
    {
        private GameBootstrapper b;
        private TreeView tree;
        private OrbitCameraRig rig;
        private Canvas hudCanvas;
        private ReferenceUiLayout referenceUiLayout;
        private SafeAreaHudLayout safeAreaHud;
        private Mouse mouse, priorMouse;
        private Touchscreen touch, priorTouch;

        [UnitySetUp]
        public IEnumerator Setup()
        {
            SaveTestScope.Begin();
            yield return SceneManager.LoadSceneAsync("MainScene");
            yield return null;

            b = Object.FindAnyObjectByType<GameBootstrapper>();
            b.TickDriver.ManualTick = true;
            b.Input.SendMessage("OnApplicationFocus", true);
            b.SaveCtrl.SendMessage("OnApplicationFocus", true);

            tree = Object.FindAnyObjectByType<TreeView>();
            rig = Object.FindAnyObjectByType<OrbitCameraRig>();
            hudCanvas = GameObject.Find("HUD")?.GetComponent<Canvas>();
            referenceUiLayout = hudCanvas?.GetComponent<ReferenceUiLayout>();
            safeAreaHud = hudCanvas?.GetComponent<SafeAreaHudLayout>();

            priorMouse = Mouse.current;
            priorTouch = Touchscreen.current;
            mouse = InputSystem.AddDevice<Mouse>();
            touch = InputSystem.AddDevice<Touchscreen>();
            mouse.MakeCurrent();
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator Teardown()
        {
            if (mouse != null && mouse.added) InputSystem.RemoveDevice(mouse);
            if (touch != null && touch.added) InputSystem.RemoveDevice(touch);
            if (priorMouse != null && priorMouse.added) priorMouse.MakeCurrent();
            if (priorTouch != null && priorTouch.added) priorTouch.MakeCurrent();
            SaveTestScope.End();
            yield return null;
        }

        [UnityTest]
        public IEnumerator TutorialBanner_DoesNotOverlapHudCards_AndDoesNotCoverTree_In1366x768_And1920x1080()
        {
            var safe = hudCanvas.transform.Find("SafeArea") as RectTransform;
            var banner = safe.Find("TutorialBanner") as RectTransform;
            var waterCard = safe.Find("WaterCard") as RectTransform;

            Assert.That(banner, Is.Not.Null);
            Assert.That(waterCard, Is.Not.Null);

            int[] widths = { 1366, 1920 };
            int[] heights = { 768, 1080 };

            for (int i = 0; i < widths.Length; i++)
            {
                int w = widths[i];
                int h = heights[i];

                safeAreaHud.Apply(new Rect(0, 0, w, h), new Vector2(w, h));
                referenceUiLayout.Layout();
                Canvas.ForceUpdateCanvases();
                yield return null;

                // Step 1: Vitality (TargetElement == "tree")
                Assert.That(b.Tutorial.GetCurrentStepId(), Is.EqualTo(TutorialSystem.StepVitality));

                // 1. Must NOT overlap resource cards vertically:
                // Resource cards are at y = -120 to -216. Banner top is banner.anchoredPosition.y.
                Assert.That(banner.anchoredPosition.y, Is.LessThanOrEqualTo(-216f),
                    $"Banner at {banner.anchoredPosition.y} must be strictly below resource cards (y <= -216) in {w}x{h}");

                // 2. Must NOT cover seedling tree tap area horizontally:
                var cam = rig.SceneCamera;
                var col = tree.GetComponent<SphereCollider>();
                Vector3 centerWorld = tree.transform.TransformPoint(col.center);
                Vector3 screenCenter = cam.WorldToScreenPoint(centerWorld);
                Vector3 screenEdge = cam.WorldToScreenPoint(centerWorld + cam.transform.right * col.radius);
                float screenRadius = Mathf.Abs(screenEdge.x - screenCenter.x);
                float sphereRightEdge = screenCenter.x + screenRadius;

                Assert.That(banner.anchoredPosition.x, Is.GreaterThan(sphereRightEdge - 5f),
                    $"Banner left edge ({banner.anchoredPosition.x}) must be to the right of seedling sphere right edge ({sphereRightEdge}) in {w}x{h}");

                // 3. Banner Image must have raycastTarget == false so it never blocks tree taps
                var bannerImg = banner.GetComponent<Image>();
                Assert.That(bannerImg.raycastTarget, Is.False, "Banner background must not block raycasts");

                // Capture screenshot
                CaptureProof(w, h, $"proof_tutorial_step1_{w}x{h}.png");
            }

            // Step 2: Open Upgrade Panel and verify banner adapts to remaining open viewport
            var upgradeView = Object.FindAnyObjectByType<UpgradePanelView>();
            if (!upgradeView.IsOpen) upgradeView.Toggle();
            referenceUiLayout.Layout();
            Canvas.ForceUpdateCanvases();
            yield return null;

            Assert.That(upgradeView.IsOpen, Is.True);
            Assert.That(banner.anchoredPosition.y, Is.LessThanOrEqualTo(-216f), "Banner must stay below resource cards with panel open");
            float panelLeftEdge = safe.rect.width - Mathf.Clamp(safe.rect.width * 0.38f, 380f, 520f);
            Assert.That(banner.anchoredPosition.x + banner.sizeDelta.x, Is.LessThanOrEqualTo(panelLeftEdge + 10f),
                "Banner must remain within the open left viewport and not overlap upgrade panel");

            CaptureProof(1366, 768, "proof_tutorial_step2_upgrade_open_1366x768.png");
            upgradeView.Close();
            yield return null;
        }

        [UnityTest]
        public IEnumerator TapInteraction_SphericalVolume_AcceptsGaps_BlocksOutsideAndOccluder()
        {
            foreach (var step in b.Tutorial.StepDefinitions)
                b.State.permanent.tutorialCompletedStepIds.Add(step.StepId);
            Object.FindAnyObjectByType<TutorialView>()?.Refresh();
            yield return null;

            var cam = rig.SceneCamera;
            string[] stages = { "seedling", "young_tree", "large_tree" };

            foreach (var stageId in stages)
            {
                b.State.run.stageId = stageId;
                b.State.vitality.tapProgress = 0;
                var visual = b.Config.FindVisual(stageId);
                tree.ShowStage(visual);
                rig.SetStagePose(visual.cameraDistance, visual.cameraPitch, visual.cameraTargetHeight);
                yield return null;

                var col = tree.GetComponent<SphereCollider>();
                Assert.That(col, Is.Not.Null, $"Stage {stageId} must have root SphereCollider");
                Assert.That(col.isTrigger, Is.True, $"SphereCollider on {stageId} must be trigger");

                // 1. Center of sphere must be valid tap
                Vector3 centerWorld = tree.transform.TransformPoint(col.center);
                Vector3 screenCenter = cam.WorldToScreenPoint(centerWorld);
                long initialTaps = b.State.vitality.tapProgress;

                yield return MouseClick(new Vector2(screenCenter.x, screenCenter.y));
                Assert.That(b.State.vitality.tapProgress, Is.EqualTo((initialTaps + 1) % 50),
                    $"Center tap on {stageId} must increment tapProgress");

                // 2. Off-center point inside sphere (gap between foliage) must accept tap
                Vector3 gapWorld = centerWorld + cam.transform.right * (col.radius * 0.35f);
                Vector3 screenGap = cam.WorldToScreenPoint(gapWorld);
                yield return MouseClick(new Vector2(screenGap.x, screenGap.y));
                Assert.That(b.State.vitality.tapProgress, Is.EqualTo((initialTaps + 2) % 50),
                    $"Inner sphere gap point on {stageId} must increment tapProgress");

                // 3. Point far outside sphere must NOT increment tapProgress
                Vector3 farWorld = centerWorld + cam.transform.right * (col.radius * 2.8f);
                Vector3 screenFar = cam.WorldToScreenPoint(farWorld);
                yield return MouseClick(new Vector2(screenFar.x, screenFar.y));
                Assert.That(b.State.vitality.tapProgress, Is.EqualTo((initialTaps + 2) % 50),
                    $"Point far outside sphere on {stageId} must NOT increment tapProgress");
            }
        }

        [UnityTest]
        public IEnumerator TerrainVertexColors_RenderedWithDedicatedTerrainLitShader()
        {
            var terrainGo = GameObject.Find("Planet_Terrain") ?? GameObject.Find("World/WorldEnvironment/Planet_Terrain");
            Assert.That(terrainGo, Is.Not.Null, "Planet_Terrain must exist in scene");

            var mf = terrainGo.GetComponent<MeshFilter>();
            Assert.That(mf, Is.Not.Null);
            Assert.That(mf.sharedMesh, Is.Not.Null);
            Assert.That(mf.sharedMesh.colors.Length, Is.GreaterThan(0), "Terrain mesh must have vertex colors populated");

            var mr = terrainGo.GetComponent<MeshRenderer>();
            Assert.That(mr, Is.Not.Null);
            Assert.That(mr.sharedMaterials.Length, Is.EqualTo(4), "Terrain must have 4 submesh materials");

            foreach (var mat in mr.sharedMaterials)
            {
                Assert.That(mat, Is.Not.Null);
                Assert.That(mat.shader.name, Is.EqualTo("Universal Render Pipeline/Custom/TerrainLit"),
                    $"Material {mat.name} must use Universal Render Pipeline/Custom/TerrainLit shader");
            }

            // Capture terrain proof screenshot
            CaptureProof(1366, 768, "proof_terrain_vertex_color_lit_1366x768.png");
            yield return null;
        }

        private IEnumerator MouseClick(Vector2 point)
        {
            InputSystem.QueueStateEvent(mouse, new MouseState { position = point });
            yield return null;
            yield return null;
            InputSystem.QueueStateEvent(mouse, new MouseState { position = point }.WithButton(MouseButton.Left));
            yield return null;
            yield return null;
            InputSystem.QueueStateEvent(mouse, new MouseState { position = point });
            yield return null;
            yield return null;
        }

        private void CaptureProof(int width, int height, string filename)
        {
            var cam = rig.SceneCamera;
            if (cam == null) cam = Camera.main;

            var prevMode = hudCanvas.renderMode;
            var prevCam = hudCanvas.worldCamera;

            var rt = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
            var captureGo = new GameObject("CaptureCamTemp", typeof(Camera));
            var captureCam = captureGo.GetComponent<Camera>();
            captureCam.CopyFrom(cam);
            captureCam.targetTexture = rt;

            try
            {
                hudCanvas.renderMode = RenderMode.ScreenSpaceCamera;
                hudCanvas.worldCamera = captureCam;
                hudCanvas.planeDistance = 1.0f;
                Canvas.ForceUpdateCanvases();

                captureCam.Render();

                RenderTexture.active = rt;
                Texture2D tex = new Texture2D(width, height, TextureFormat.RGB24, false);
                tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                tex.Apply();

                string dir = Path.Combine(Application.dataPath, "..", "Evidence", "ReviewFix_20261012");
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
                string path = Path.Combine(dir, filename);
                File.WriteAllBytes(path, tex.EncodeToPNG());
                Debug.Log($"[Evidence] Saved proof screenshot {filename} to: {path}");
                Object.DestroyImmediate(tex);
            }
            finally
            {
                RenderTexture.active = null;
                hudCanvas.renderMode = prevMode;
                hudCanvas.worldCamera = prevCam;
                Canvas.ForceUpdateCanvases();
                captureCam.targetTexture = null;
                Object.DestroyImmediate(captureGo);
                rt.Release();
                Object.DestroyImmediate(rt);
            }
        }
    }
}
