using System.Collections;
using CayTheGioi.Bootstrap;
using CayTheGioi.Domain;
using CayTheGioi.Infrastructure;
using CayTheGioi.Presentation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace CayTheGioi.Tests
{
    public sealed class WeatherSceneTests
    {
        private GameBootstrapper b;

        [UnitySetUp]
        public IEnumerator Setup()
        {
            SaveTestScope.Begin();
            SceneManager.sceneLoaded += Freeze;
            yield return SceneManager.LoadSceneAsync("MainScene");
            yield return null;
            b = Object.FindAnyObjectByType<GameBootstrapper>();
            Assert.That(b, Is.Not.Null);
            Assert.That(b.IsInitialized, Is.True);
            b.TickDriver.ManualTick = true;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            SceneManager.sceneLoaded -= Freeze;
            SaveTestScope.End();
            yield return null;
        }

        private void Freeze(Scene s, LoadSceneMode m)
        {
            var t = Object.FindAnyObjectByType<OnlineTickDriver>();
            if (t != null) t.ManualTick = true;
        }

        [UnityTest]
        public IEnumerator InitialWeatherIsCalmAndHpIsMax()
        {
            Assert.That(b.Weather, Is.Not.Null);
            Assert.That(b.Health, Is.Not.Null);
            Assert.That(b.Weather.CurrentWeatherId, Is.EqualTo("calm"));
            Assert.That(b.State.run.hp, Is.EqualTo(100));
            Assert.That(b.Health.IsBurning, Is.False);
            Assert.That(b.State.run.status.statusId, Is.EqualTo("none"));
            yield return null;
        }

        [UnityTest]
        public IEnumerator OnlineDriver_ProducesUntilDeathAtStrikeBoundary()
        {
            b.State.run.stageId = "large_tree";
            b.State.run.hp = 1; b.State.run.levels.root = 1;
            b.Weather.ForceWeather("thunderstorm", 60);
            b.State.run.weather.strikeWillHit = true;
            b.State.run.weather.strikeExecuted = false;
            b.State.run.weather.strikeDelaySeconds = 18;
            double initialWater = b.State.run.water;
            int died = 0; b.Health.RunDied += () => died++;
            b.TickDriver.TickManual(20);
            Assert.That(b.State.run.life, Is.EqualTo("DEAD"));
            Assert.That(b.State.run.water, Is.EqualTo(initialWater + 18.5).Within(1e-6));
            Assert.That(b.Weather.RemainingSeconds, Is.EqualTo(41.5).Within(1e-6));
            Assert.That(died, Is.EqualTo(1));
            yield return null;
        }

        [UnityTest]
        public IEnumerator AnimatedTreeReceivesMouseTapsAcrossAllStagesAndCalmStorm()
        {
            var tree = Object.FindAnyObjectByType<TreeView>();
            var rig = Object.FindAnyObjectByType<OrbitCameraRig>();
            Assert.That(tree.GetComponent<TreeAnimationController>().enabled, Is.True);
            foreach (var step in b.Tutorial.StepDefinitions)
                if (!b.State.permanent.tutorialCompletedStepIds.Contains(step.StepId)) b.State.permanent.tutorialCompletedStepIds.Add(step.StepId);
            Object.FindAnyObjectByType<TutorialView>().Refresh();
            b.Input.SendMessage("OnApplicationFocus", true);
            var previousMouse = Mouse.current;
            var mouse = InputSystem.AddDevice<Mouse>();
            try
            {
                foreach (string stage in new[] { "seedling", "young_tree", "large_tree" })
                {
                    b.State.run.stageId = stage;
                    var visual = b.Config.FindVisual(stage);
                    tree.ShowStage(visual);
                    rig.SetStagePose(visual.cameraDistance, visual.cameraPitch, visual.cameraTargetHeight);
                    foreach (string weather in new[] { "calm", "thunderstorm" })
                    {
                        b.Weather.ForceWeather(weather, 600);
                        yield return new WaitForSeconds(.3f);
                        Vector2 point = default; bool found = false;
                        for (int x = 35; x <= 65 && !found; x += 1)
                            for (int y = 25; y <= 75 && !found; y += 1)
                            {
                                var candidate = new Vector2(Screen.width * x / 100f, Screen.height * y / 100f);
                                if (b.Input.IsOverUi(candidate)) continue;
                                bool interior = true;
                                foreach (var offset in new[] { Vector2.zero, new Vector2(-6,0), new Vector2(6,0), new Vector2(0,-6), new Vector2(0,6) })
                                {
                                    if (!b.Input.OnTree(candidate + offset)) { interior = false; break; }
                                }
                                if (interior) { point = candidate; found = true; }
                            }
                        Assert.That(found, Is.True, stage + " " + weather + " needs a visible tap point with margin for live motion");
                        int before = b.State.vitality.tapProgress;
                        InputSystem.QueueStateEvent(mouse, new MouseState { position = point });
                        yield return null; yield return null;
                        InputSystem.QueueStateEvent(mouse, new MouseState { position = point }.WithButton(MouseButton.Left));
                        yield return null; yield return null;
                        InputSystem.QueueStateEvent(mouse, new MouseState { position = point });
                        yield return null; yield return null;
                        Assert.That(b.State.vitality.tapProgress, Is.EqualTo(before + 1), stage + " " + weather + " live animation must receive one mouse tap");
                    }
                }
            }
            finally
            {
                InputSystem.RemoveDevice(mouse);
                if (previousMouse != null && previousMouse.added) previousMouse.MakeCurrent();
            }
        }

        [UnityTest]
        public IEnumerator Review_ActualSkyBoltAndAudioLifecycle()
        {
            var tree = Object.FindAnyObjectByType<TreeView>();
            var rig = Object.FindAnyObjectByType<OrbitCameraRig>();
            var view = Object.FindAnyObjectByType<WeatherView>();
            var sky = GameObject.Find("AmbientSkyLightningBolt").GetComponent<LineRenderer>();
            var rain = b.Audio.transform.Find("Rain_Source").GetComponent<AudioSource>();
            var wind = b.Audio.transform.Find("Wind_Source").GetComponent<AudioSource>();
            var lightField = typeof(WeatherView).GetField("sceneLight", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            var light = (Light)lightField.GetValue(view);
            Assert.That(light.type, Is.EqualTo(LightType.Directional));
            float oldMin = b.Config.WeatherVfx.skyLightningMinInterval, oldMax = b.Config.WeatherVfx.skyLightningMaxInterval;
            try
            {
                b.Config.WeatherVfx.skyLightningMinInterval = b.Config.WeatherVfx.skyLightningMaxInterval = .5f;
                foreach (string stage in new[] { "seedling", "young_tree", "large_tree" })
                {
                    var visual = b.Config.FindVisual(stage);
                    tree.ShowStage(visual);
                    rig.SetStagePose(visual.cameraDistance, visual.cameraPitch, visual.cameraTargetHeight);
                    b.Weather.ForceWeather("thunderstorm", 600);
                    double hp = b.State.run.hp;
                    float deadline = Time.realtimeSinceStartup + 2;
                    while (!sky.enabled && Time.realtimeSinceStartup < deadline) yield return null;
                    Assert.That(sky.enabled, Is.True, stage + " must show a real sky bolt");
                    for (int i = 0; i < sky.positionCount; i++)
                    {
                        var viewport = rig.SceneCamera.WorldToViewportPoint(sky.GetPosition(i));
                        Assert.That(viewport.z, Is.GreaterThan(0));
                        Assert.That(viewport.x, Is.InRange(.1f, .9f));
                        Assert.That(viewport.y, Is.InRange(.55f, .99f));
                    }
                    Assert.That(b.State.run.hp, Is.EqualTo(hp), "Decorative lightning must not damage the tree");
                    b.Weather.ForceWeather("rainy", 600);
                    yield return new WaitForSeconds(.3f);
                    view.GetBaseWeatherLight("rainy", out var color, out var intensity);
                    Assert.That(sky.enabled, Is.False);
                    Assert.That(light.intensity, Is.EqualTo(intensity).Within(.001f));
                    Assert.That(light.color, Is.EqualTo(color));
                }
                b.Audio.SendMessage("OnApplicationFocus", true);
                b.Audio.SendMessage("OnApplicationPause", false);
                b.Audio.SetMusicEnabled(true);
                yield return null;
                Assert.That(rain.isPlaying && wind.isPlaying, Is.True);
                b.Audio.SetMusicEnabled(false);
                Assert.That(rain.isPlaying || wind.isPlaying, Is.False);
                b.Audio.SetMusicEnabled(true);
                yield return null;
                Assert.That(rain.isPlaying && wind.isPlaying, Is.True);
                b.Audio.SendMessage("OnApplicationPause", true);
                b.Audio.SendMessage("OnApplicationFocus", true);
                Assert.That(b.Audio.IsSuspended, Is.True, "Focus must not override an outstanding pause");
                Assert.That(rain.isPlaying || wind.isPlaying, Is.False);
                b.Audio.SendMessage("OnApplicationPause", false);
                yield return null;
                Assert.That(rain.isPlaying && wind.isPlaying, Is.True);
                b.Health.Kill();
                Assert.That(rain.isPlaying || wind.isPlaying, Is.False);
            }
            finally
            {
                b.Config.WeatherVfx.skyLightningMinInterval = oldMin;
                b.Config.WeatherVfx.skyLightningMaxInterval = oldMax;
            }
        }

        [UnityTest]
        public IEnumerator Review_RealModelsWeatherAndGameViewEvidence()
        {
            foreach (var step in b.Tutorial.StepDefinitions)
                if (!b.State.permanent.tutorialCompletedStepIds.Contains(step.StepId)) b.State.permanent.tutorialCompletedStepIds.Add(step.StepId);
            Object.FindAnyObjectByType<TutorialView>().Refresh();
            var tree = Object.FindAnyObjectByType<TreeView>();
            var rig = Object.FindAnyObjectByType<OrbitCameraRig>();
            string directory = System.IO.Path.GetFullPath("Evidence/ReviewFix_20261012/Screenshots");
            System.IO.Directory.CreateDirectory(directory);
            var metrics = new System.Text.StringBuilder();
            foreach (string stage in new[] { "seedling", "young_tree", "large_tree" })
            {
                b.State.run.stageId = stage;
                var visual = b.Config.FindVisual(stage);
                tree.ShowStage(visual);
                rig.SetStagePose(visual.cameraDistance, visual.cameraPitch, visual.cameraTargetHeight);
                Object.FindAnyObjectByType<FoundationHudView>().Bind(b.State, stage, 50);
                var parts = tree.CurrentStageAnchors.GetComponentsInChildren<Transform>();
                var trunkPart = System.Array.Find(parts, part => part.name == "Trunk_Branches");
                var foliagePart = System.Array.Find(parts, part => part.name == "Foliage");
                var rootsPart = System.Array.Find(parts, part => part.name == "Roots");
                Quaternion trunkRest = trunkPart.localRotation, foliageRest = foliagePart.localRotation, rootsRest = rootsPart.localRotation;
                foreach (string weather in new[] { "calm", "sunny", "rainy", "thunderstorm" })
                {
                    b.Weather.ForceWeather(weather, 600);
                    yield return new WaitForSeconds(0.4f);
                    Canvas.ForceUpdateCanvases();
                    yield return new WaitForEndOfFrame();
                    ScreenCapture.CaptureScreenshot(System.IO.Path.Combine(directory, stage + "_" + weather + ".png"));
                    yield return null;
                    var anchor = tree.LightningTargetAnchor;
                    Assert.That(anchor, Is.Not.Null);
                    var foliage = System.Array.Find(tree.CurrentStageAnchors.GetComponentsInChildren<MeshFilter>(), mf => mf.name == "Foliage");
                    Assert.That(foliage, Is.Not.Null);
                    var collider = foliage.GetComponent<MeshCollider>();
                    var mesh = foliage.sharedMesh; var vertices = mesh.vertices; var indices = mesh.triangles;
                    var anim = tree.GetComponent<TreeAnimationController>();
                    var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
                    var weights = (float[])typeof(TreeAnimationController).GetField("foliageWeights", flags).GetValue(anim);
                    var rest = (Vector3[])typeof(TreeAnimationController).GetField("baseFoliageVertices", flags).GetValue(anim);
                    int pinned = 0; float movement = 0;
                    for (int i = 0; i < vertices.Length; i++)
                    {
                        float delta = foliage.transform.TransformVector(vertices[i] - rest[i]).magnitude;
                        movement = Mathf.Max(movement, delta);
                        if (weights[i] <= .0001f) { pinned++; Assert.That(delta, Is.LessThan(.000001f), "Pinned leaf vertex moved"); }
                    }
                    Assert.That(pinned, Is.GreaterThan(0));
                    Assert.That(movement, Is.GreaterThan(0));
                    float amplitude = stage == "seedling" ? b.Config.TreeAnimation.seedlingFlutterAmpWorld : stage == "young_tree" ? b.Config.TreeAnimation.youngTreeFlutterAmpWorld : b.Config.TreeAnimation.largeTreeFlutterAmpWorld;
                    if (weather == "thunderstorm") amplitude *= b.Config.TreeAnimation.thunderstormAmpMultiplier;
                    Assert.That(movement, Is.LessThanOrEqualTo(amplitude * 1.46f));
                    Assert.That(Quaternion.Angle(trunkPart.localRotation * Quaternion.Inverse(trunkRest), foliagePart.localRotation * Quaternion.Inverse(foliageRest)), Is.LessThan(.05f), "Trunk and foliage must tilt together");
                    Assert.That(Quaternion.Angle(rootsPart.localRotation, rootsRest), Is.LessThan(.001f));
                    metrics.AppendLine(stage + " " + weather + " pinnedVertices=" + pinned + " maxFlutterMetres=" + movement.ToString("R", System.Globalization.CultureInfo.InvariantCulture));
                    var rays = new System.Collections.Generic.List<Ray>();
                    for (int triangle = 0; triangle + 2 < indices.Length; triangle += System.Math.Max(3, indices.Length / 96 / 3 * 3))
                    {
                        Vector3 a = foliage.transform.TransformPoint(vertices[indices[triangle]]);
                        Vector3 c = foliage.transform.TransformPoint(vertices[indices[triangle + 1]]);
                        Vector3 d = foliage.transform.TransformPoint(vertices[indices[triangle + 2]]);
                        Vector3 normal = Vector3.Cross(c - a, d - a).normalized;
                        if (normal.sqrMagnitude > 0.5f) rays.Add(new Ray((a + c + d) / 3 + normal * .02f, -normal));
                    }
                    var distances = new float[rays.Count];
                    for (int i = 0; i < rays.Count; i++) distances[i] = collider.Raycast(rays[i], out var hit, .04f) ? hit.distance : -1;
                    collider.sharedMesh = null; collider.sharedMesh = mesh;
                    int mismatches = 0;
                    for (int i = 0; i < rays.Count; i++)
                    {
                        float expected = collider.Raycast(rays[i], out var hit, .04f) ? hit.distance : -1;
                        if (Mathf.Abs(expected - distances[i]) > .0001f) mismatches++;
                    }
                    if (anim.EnableColliderRecookPerFrame)
                    {
                        Assert.That(mismatches, Is.Zero, stage + " " + weather + " collider differs from actual animated mesh");
                    }
                    metrics.AppendLine(stage + " " + weather + " camera=" + rig.Distance + " anchor=" + anchor.position);
                }
            }
            System.IO.File.WriteAllText(System.IO.Path.Combine(directory, "model-weather-metrics.txt"), metrics.ToString());
        }

        [UnityTest]
        public IEnumerator WeatherMultipliersApplyToLiveRates()
        {
            b.State.run.water = 100;
            b.State.run.levels.root = 2;
            b.State.run.levels.leaves = 2;

            b.Weather.ForceWeather("calm");
            var calmRates = b.Resources.Rates();

            b.Weather.ForceWeather("sunny", 300);
            var sunnyRates = b.Resources.Rates();
            Assert.That(sunnyRates.PhotosynthesisProduced, Is.EqualTo(calmRates.PhotosynthesisProduced * 1.2).Within(1e-6));

            b.Weather.ForceWeather("rainy", 300);
            var rainyRates = b.Resources.Rates();
            Assert.That(rainyRates.WaterProduced, Is.EqualTo(calmRates.WaterProduced * 1.2).Within(1e-6));

            b.Weather.ForceWeather("calm");
            var backToCalm = b.Resources.Rates();
            Assert.That(backToCalm.PhotosynthesisProduced, Is.EqualTo(calmRates.PhotosynthesisProduced).Within(1e-6));
            Assert.That(backToCalm.WaterProduced, Is.EqualTo(calmRates.WaterProduced).Within(1e-6));
            yield return null;
        }

        [UnityTest]
        public IEnumerator StrikeOnLeavesStartsBurningReducesGrowthAndDamagesHp()
        {
            b.State.run.water = 100;
            b.State.run.levels.root = 2;
            b.State.run.levels.leaves = 2;
            b.Weather.ForceWeather("calm");
            var normalRates = b.Resources.Rates();

            b.Weather.TriggerStrike("leaves");
            Assert.That(b.Health.IsBurning, Is.True);
            Assert.That(b.State.run.status.impactRegionId, Is.EqualTo("leaves"));

            var burningRates = b.Resources.Rates();
            Assert.That(burningRates.PhotosynthesisProduced, Is.EqualTo(normalRates.PhotosynthesisProduced * 0.7).Within(1e-6));

            // Tick 2 seconds manual -> HP should drop from 100 to 96 (2 HP/s)
            b.TickDriver.TickManual(2.0);
            Assert.That(b.State.run.hp, Is.EqualTo(96).Within(1e-6));
            yield return null;
        }

        [UnityTest]
        public IEnumerator TreeDeathOpensForcedRebirthModalAndStopsProduction()
        {
            b.State.run.water = 50;
            b.State.run.levels.root = 2;
            b.State.run.levels.leaves = 2;

            var rebirthView = Object.FindAnyObjectByType<RebirthView>();
            Assert.That(rebirthView, Is.Not.Null);

            // Kill tree
            b.Health.Kill();
            Assert.That(b.State.run.life, Is.EqualTo("DEAD"));
            Assert.That(b.State.run.hp, Is.EqualTo(0));

            yield return null; // allow LateUpdate on RebirthView

            Assert.That(rebirthView.IsVisible, Is.True, "Forced Rebirth modal must be visible when tree dies.");
            Assert.That(rebirthView.CancelButton.gameObject.activeSelf, Is.False, "Cancel button must be hidden for Forced Rebirth.");
            Assert.That(rebirthView.ConfirmButton.interactable, Is.True);

            // Production must be stopped
            double waterBefore = b.State.run.water;
            b.TickDriver.TickManual(5.0);
            Assert.That(b.State.run.water, Is.EqualTo(waterBefore), "Dead tree must not produce any resources.");
        }

        [UnityTest]
        public IEnumerator ForcedRebirthRestoresHpAndClearsBurningAndResetsCalm()
        {
            b.State.run.stageId = "young_tree";
            b.Weather.TriggerStrike("leaves");
            b.Health.Kill();

            yield return null;
            var rebirthView = Object.FindAnyObjectByType<RebirthView>();
            Assert.That(rebirthView.IsVisible, Is.True);

            rebirthView.Confirm();
            yield return null;

            Assert.That(b.State.run.life, Is.EqualTo("ALIVE"));
            Assert.That(b.State.run.hp, Is.EqualTo(100));
            Assert.That(b.State.run.stageId, Is.EqualTo("seedling"));
            Assert.That(b.Weather.CurrentWeatherId, Is.EqualTo("calm"));
            Assert.That(b.Health.IsBurning, Is.False);
            Assert.That(b.State.run.status.statusId, Is.EqualTo("none"));
            Assert.That(rebirthView.IsVisible, Is.False);
        }

        [UnityTest]
        public IEnumerator SaveAndReloadPreservesWeatherStatusAndHp()
        {
            b.Weather.ForceWeather("rainy", 250);
            b.Health.SetHp(72.5);
            b.State.run.status.statusId = "burning";
            b.State.run.status.remainingOnlineSeconds = 6;
            b.State.run.status.impactRegionId = "trunk";

            var saveResult = b.SaveCtrl.ForceSave("TEST_WEATHER_SAVE");
            Assert.That(saveResult.Success, Is.True, saveResult.Reason);

            yield return SceneManager.LoadSceneAsync("MainScene");
            yield return null;

            var b2 = Object.FindAnyObjectByType<GameBootstrapper>();
            Assert.That(b2, Is.Not.Null);
            Assert.That(b2.LastLoadOutcome, Is.EqualTo(SaveService.LoadOutcome.Loaded));
            Assert.That(b2.Weather.CurrentWeatherId, Is.EqualTo("rainy"));
            Assert.That(b2.State.run.hp, Is.EqualTo(72.5).Within(1e-6));
            Assert.That(b2.State.run.status.statusId, Is.EqualTo("burning"));
            Assert.That(b2.State.run.status.remainingOnlineSeconds, Is.EqualTo(6).Within(1e-6));
            Assert.That(b2.State.run.status.impactRegionId, Is.EqualTo("trunk"));
        }
    }
}
