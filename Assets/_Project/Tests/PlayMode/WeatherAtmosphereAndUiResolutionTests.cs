using System.Collections;
using System.IO;
using CayTheGioi.Bootstrap;
using CayTheGioi.Infrastructure;
using CayTheGioi.Presentation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace CayTheGioi.Tests
{
    public sealed class WeatherAtmosphereAndUiResolutionTests
    {
        private GameBootstrapper b;
        private WeatherView weatherView;
        private OrbitCameraRig rig;
        private Canvas hudCanvas;
        private SafeAreaHudLayout safeAreaHud;
        private ReferenceUiLayout referenceUiLayout;
        private FoundationHudView foundationHud;

        [UnitySetUp]
        public IEnumerator Setup()
        {
            SaveTestScope.Begin();
            yield return SceneManager.LoadSceneAsync("MainScene");
            yield return null;

            b = Object.FindAnyObjectByType<GameBootstrapper>();
            Assert.That(b, Is.Not.Null);
            b.TickDriver.ManualTick = true;

            weatherView = Object.FindAnyObjectByType<WeatherView>();
            rig = Object.FindAnyObjectByType<OrbitCameraRig>();
            hudCanvas = GameObject.Find("HUD")?.GetComponent<Canvas>();
            safeAreaHud = hudCanvas?.GetComponent<SafeAreaHudLayout>();
            referenceUiLayout = hudCanvas?.GetComponent<ReferenceUiLayout>();
            foundationHud = hudCanvas?.GetComponent<FoundationHudView>();

            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            SaveTestScope.End();
            yield return null;
        }

        [UnityTest]
        public IEnumerator WeatherAtmosphere_TransitionsCorrectly_InRainAndThunderstorm()
        {
            Assert.That(weatherView, Is.Not.Null);
            var cam = rig.SceneCamera;
            Assert.That(cam, Is.Not.Null);

            // 1. Switch to Thunderstorm
            b.Weather.ForceWeather("thunderstorm", 60.0);
            yield return null;

            Assert.That(RenderSettings.fog, Is.True, "Fog should be enabled during thunderstorm");
            Assert.That(RenderSettings.fogDensity, Is.GreaterThan(0.005f), "Fog density should be active in thunderstorm");
            Assert.That(RenderSettings.ambientLight.r, Is.LessThan(0.20f), "Ambient light red should be dimmed in thunderstorm");
            Assert.That(RenderSettings.ambientLight.g, Is.LessThan(0.20f), "Ambient light green should be dimmed in thunderstorm");
            Assert.That(cam.backgroundColor.r, Is.LessThan(0.18f), "Camera background should be darkened in thunderstorm");

            // Capture thunderstorm screenshot at 1920x1080
            CaptureCompositeScreenshot(1920, 1080, "thunderstorm_1920x1080.png");

            // 2. Switch back to Calm
            b.Weather.ForceWeather("calm", 60.0);
            yield return null;

            Assert.That(RenderSettings.fogDensity, Is.LessThan(0.005f), "Fog density should be restored to calm levels");
            Assert.That(RenderSettings.ambientLight.r, Is.GreaterThan(0.20f), "Ambient light should be restored in calm weather");
        }

        [UnityTest]
        public IEnumerator UiResolutions_RenderAndValidate_1366x768_1920x1080_2400x1080()
        {
            Assert.That(hudCanvas, Is.Not.Null);
            Assert.That(foundationHud, Is.Not.Null);

            var resolutions = new[]
            {
                new { Width = 1366, Height = 768, Name = "ui_1366x768.png" },
                new { Width = 1920, Height = 1080, Name = "ui_1920x1080.png" },
                new { Width = 2400, Height = 1080, Name = "ui_2400x1080.png" }
            };

            foreach (var res in resolutions)
            {
                // Apply responsive layout for target resolution
                if (safeAreaHud != null)
                {
                    safeAreaHud.Apply(new Rect(0, 0, res.Width, res.Height), new Vector2(res.Width, res.Height));
                }
                if (referenceUiLayout != null)
                {
                    referenceUiLayout.Layout();
                }
                if (foundationHud != null)
                {
                    foundationHud.LayoutCards();
                }
                Canvas.ForceUpdateCanvases();
                yield return null;

                // Validate Vietnamese stage name
                var stageText = foundationHud.StageText;
                Assert.That(stageText, Is.Not.Null);
                Assert.That(stageText.text, Is.EqualTo("Mầm"), "Default seedling stage should display 'Mầm'");

                // Validate Orbit Button in HUD
                var evComponent = hudCanvas.GetComponent<EvolutionPanelView>();
                Assert.That(evComponent, Is.Not.Null);
                Assert.That(evComponent.OrbitButton, Is.Not.Null, "Orbit button must exist");
                Assert.That(evComponent.OrbitButton.gameObject.activeInHierarchy, Is.True, "Orbit button must be active");
                Assert.That(evComponent.OrbitButton.transform.parent.name, Is.EqualTo("SafeArea"), "Orbit button must be relocated to HUD SafeArea");

                // Validate Spiritual Energy card "Chưa mở" text
                var seReason = hudCanvas.transform.Find("SafeArea/ResourceBackdrop/spiritualEnergy/ReasonText")?.GetComponent<Text>();
                if (seReason != null && seReason.gameObject.activeInHierarchy)
                {
                    Assert.That(seReason.text, Does.Contain("Chưa mở"), "Locked spiritual energy card must display 'Chưa mở'");
                    Assert.That(seReason.horizontalOverflow, Is.EqualTo(HorizontalWrapMode.Overflow), "Horizontal overflow should prevent clipping");
                }

                // Capture screenshot
                CaptureCompositeScreenshot(res.Width, res.Height, res.Name);
            }
        }

        private void CaptureCompositeScreenshot(int width, int height, string filename)
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
                // Render 3D scene + UI through isolated camera
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
                Debug.Log($"[Evidence] Saved screenshot {filename} ({width}x{height}) to: {path}");
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
