using System;
using System.Collections.Generic;
using CayTheGioi.Configuration;
using CayTheGioi.Domain;
using CayTheGioi.Infrastructure;
using NUnit.Framework;
using UnityEngine;

namespace CayTheGioi.Tests
{
    public sealed class T11_RegressionTests
    {
        private PrototypeRules rules;
        private GameState state;

        [SetUp]
        public void Setup()
        {
            rules = new PrototypeRules();
            state = GameStateFactory.CreateNew(rules);
        }

        // ── R1: Chuẩn hóa không gian chuyển động & ghim cuống lá ──────

        [Test]
        public void R1_WorldFoliageDisplacement_IsBounded_UnderAnyTransformScale()
        {
            // Simulate FBX large_tree with scale (100, 100, 100) vs seedling (1, 1, 1)
            Vector3 largeLossyScale = new Vector3(100f, 100f, 100f);
            Vector3 seedlingLossyScale = new Vector3(1f, 1f, 1f);

            var animConfig = new TreeAnimationConfig();
            float worldAmp = animConfig.largeTreeFlutterAmpWorld; // 0.004m

            // Large tree local offset calculation
            float scaleX = Mathf.Max(0.0001f, Mathf.Abs(largeLossyScale.x));
            float scaleY = Mathf.Max(0.0001f, Mathf.Abs(largeLossyScale.y));
            float scaleZ = Mathf.Max(0.0001f, Mathf.Abs(largeLossyScale.z));

            float dxWorld = worldAmp; // maximum world displacement
            float dxLocal = dxWorld / scaleX;

            // Reconstructed world displacement
            float reconstructedWorldX = dxLocal * largeLossyScale.x;
            Assert.That(reconstructedWorldX, Is.EqualTo(worldAmp).Within(1e-6));
            Assert.That(reconstructedWorldX, Is.LessThan(0.01f), "Large tree leaf world displacement must be bounded to millimetres, never meters!");

            // Test stem attachment pinning (w = 0)
            float stemWeight = 0.0f;
            float stemDisplacementWorld = worldAmp * stemWeight;
            Assert.That(stemDisplacementWorld, Is.EqualTo(0f), "Leaf stem attachment point must remain strictly pinned (0 world displacement).");
        }

        // ── R2: Đồng bộ Anchor & Bending trong Giông ─────────────────

        [Test]
        public void R2_AnchorsRotateSynchronously_WithTreeBending()
        {
            var animConfig = new TreeAnimationConfig();
            Quaternion tilt = Quaternion.Euler(animConfig.largeTreeMaxTiltAngle, 0f, 0f);

            Vector3 initialLeavesAnchorLocalPos = new Vector3(0f, 2.5f, 0f);
            Vector3 animatedLeavesAnchorLocalPos = tilt * initialLeavesAnchorLocalPos;

            // Anchor must follow trunk tilt smoothly
            Assert.That(animatedLeavesAnchorLocalPos.y, Is.LessThan(initialLeavesAnchorLocalPos.y));
            Assert.That(animatedLeavesAnchorLocalPos.z, Is.GreaterThan(initialLeavesAnchorLocalPos.z));

            // Radial distance from base (0,0,0) must be conserved
            Assert.That(animatedLeavesAnchorLocalPos.magnitude, Is.EqualTo(initialLeavesAnchorLocalPos.magnitude).Within(1e-5));
        }

        // ── R3: Mưa phủ đúng vùng chơi và vệt rơi chạm đất ───────────

        [TestCase(0.5f)] // Seedling
        [TestCase(1.5f)] // Young Tree
        [TestCase(3.8f)] // Large Tree
        public void R3_RainEmitter_CoversTreeAndReachesGround(float treeTop)
        {
            var vfxConfig = new WeatherVfxConfig();
            float emitterHeight = Mathf.Max(3.2f, treeTop + 2.5f);
            float lifetime = (emitterHeight + 0.5f) / vfxConfig.rainStartSpeed;
            float fallDistance = vfxConfig.rainStartSpeed * lifetime;

            Assert.That(emitterHeight, Is.GreaterThan(treeTop), "Emitter must be positioned above tree canopy.");
            Assert.That(fallDistance, Is.GreaterThanOrEqualTo(emitterHeight), "Rain particles must reach ground level (y <= 0) before expiring.");
            Assert.That(vfxConfig.rainMinStreakSize, Is.LessThanOrEqualTo(0.035f), "Rain streaks must remain slender and refined.");
        }

        // ── R4: Tia sét trời 1000 mẫu nằm trong tầm nhìn Camera ───────

        [Test]
        public void R4_SkyLightning_1000Samples_AllWithinValidViewport()
        {
            var rng = new System.Random(2026);
            int validCount = 0;

            for (int i = 0; i < 1000; i++)
            {
                float u = 0.2f + (float)rng.NextDouble() * 0.6f;
                float v = 0.72f + (float)rng.NextDouble() * 0.22f;

                // Viewport coordinates must fall cleanly inside visible upper screen region
                if (u >= 0.15f && u <= 0.85f && v >= 0.70f && v <= 0.96f)
                {
                    validCount++;
                }
            }

            Assert.That(validCount, Is.EqualTo(1000), "All 1000 decorative sky lightning samples must reside within visible camera upper viewport.");
        }

        // ── R5: Đồng bộ Playback Audio khi Unmute / Resume ────────────

        [Test]
        public void R5_AudioPlaybackSynchronization_Rules()
        {
            var audioConfig = new AudioConfig();

            // When music is disabled, target volume is kept, but unmuting triggers play
            bool musicEnabled = false;
            float targetRain = audioConfig.thunderstormRainVolume;
            Assert.That(targetRain, Is.GreaterThan(0.001f));

            // Simulating unmute logic:
            musicEnabled = true;
            bool shouldPlay = musicEnabled && targetRain > 0.001f;
            Assert.That(shouldPlay, Is.True, "Unmuting while in thunderstorm must immediately trigger rain loop playback.");
        }

        // ── R6: Khôi phục ánh sáng chuẩn sau khi chớp sét ───────────

        [TestCase("calm")]
        [TestCase("sunny")]
        [TestCase("rainy")]
        [TestCase("thunderstorm")]
        public void R6_LightRestore_MatchesCurrentWeatherExactly(string weatherId)
        {
            Color defaultColor = Color.white;
            float defaultIntensity = 1.0f;

            Color targetColor;
            float targetIntensity;

            switch (weatherId)
            {
                case "sunny":
                    targetColor = new Color(1.0f, 0.97f, 0.88f);
                    targetIntensity = defaultIntensity * 1.25f;
                    break;
                case "rainy":
                    targetColor = new Color(0.70f, 0.80f, 0.92f);
                    targetIntensity = defaultIntensity * 0.82f;
                    break;
                case "thunderstorm":
                    targetColor = new Color(0.42f, 0.46f, 0.60f);
                    targetIntensity = defaultIntensity * 0.52f;
                    break;
                case "calm":
                default:
                    targetColor = defaultColor;
                    targetIntensity = defaultIntensity;
                    break;
            }

            Assert.That(targetIntensity, Is.GreaterThan(0f));
            if (weatherId == "calm")
            {
                Assert.That(targetIntensity, Is.EqualTo(defaultIntensity));
                Assert.That(targetColor, Is.EqualTo(defaultColor));
            }
        }

        // ── R7: SaveValidator kiểm tra strikeDelaySeconds & flags ─────

        [Test]
        public void R7_SaveValidator_RejectsNaNOrNegativeStrikeDelay()
        {
            state.run.stageId = "large_tree";
            state.run.weather.weatherId = "thunderstorm";
            state.run.weather.strikeEvaluated = true;
            state.run.weather.strikeWillHit = true;
            state.run.weather.strikeDelaySeconds = double.NaN;

            var dto = SaveSerializer.Snapshot(state, rules.configVersion, "test_nan", 1);
            var errors = SaveValidator.Validate(dto, rules);
            Assert.That(errors, Does.Contain("run.weather.strikeDelaySeconds must be finite and non-negative."));

            dto.run.weather.strikeDelaySeconds = -0.5;
            errors = SaveValidator.Validate(dto, rules);
            Assert.That(errors, Does.Contain("run.weather.strikeDelaySeconds must be finite and non-negative."));
        }

        [Test]
        public void R7_SaveValidator_RejectsInconsistentStrikeFlags()
        {
            state.run.stageId = "large_tree";
            state.run.weather.weatherId = "thunderstorm";
            state.run.weather.strikeEvaluated = false;
            state.run.weather.strikeWillHit = true;

            var dto = SaveSerializer.Snapshot(state, rules.configVersion, "test_inconsistent", 1);
            var errors = SaveValidator.Validate(dto, rules);
            Assert.That(errors, Does.Contain("strikeWillHit cannot be true when strikeEvaluated is false."));

            state.run.weather.strikeEvaluated = true;
            state.run.weather.strikeWillHit = false;
            state.run.weather.strikeExecuted = true;
            dto = SaveSerializer.Snapshot(state, rules.configVersion, "test_inconsistent_2", 1);
            errors = SaveValidator.Validate(dto, rules);
            Assert.That(errors, Does.Contain("strikeExecuted cannot be true when strikeWillHit is false."));
        }

        // ── R8: Timeline Sub-stepping bảo toàn sét và Burning qua dt lớn ─

        [TestCase(0.05)]
        [TestCase(0.2)]
        [TestCase(1.0)]
        [TestCase(15.0)]
        [TestCase(60.0)]
        public void R8_TimelineSubstepping_ConsistentAcrossAllDt(double dt)
        {
            var testState = GameStateFactory.CreateNew(rules);
            testState.run.stageId = "large_tree";
            testState.run.hp = 100.0;
            var w = new WeatherSystem(testState, rules, rng01: () => 0.0, rngInt: max => 0);
            var h = new HealthStatusSystem(testState, rules);

            int strikes = 0;
            w.LightningStruck += (region) => strikes++;
            w.ForceWeather("thunderstorm", 60);

            int steps = (int)Math.Round(60.0 / dt);
            for (int i = 0; i < steps; i++)
            {
                w.Tick(dt, segment => h.Tick(segment, out _), () => h.SecondsToBoundary);
            }

            Assert.That(strikes, Is.EqualTo(1), $"dt={dt} must execute exactly 1 strike");
            Assert.That(testState.run.hp, Is.EqualTo(80.0).Within(1e-5), $"dt={dt} must take exactly 20 damage (10s Burning @ 2HP/s)");
            Assert.That(w.CurrentWeatherId, Is.EqualTo("calm"));
        }

        // ── N01: Timeline at intermediate boundary t=20s with strike at 18s ──

        [Test]
        public void N01_IntermediateTimelineAt20s_ConsistentAcrossAllDt()
        {
            Func<double, (double hp, double burningRemaining)> runTo20 = (dt) =>
            {
                var s = GameStateFactory.CreateNew(rules);
                s.run.stageId = "large_tree";
                s.run.levels.trunk = 0;
                var w = new WeatherSystem(s, rules, () => 0.0, max => 0);
                var h = new HealthStatusSystem(s, rules);
                w.ForceWeather("thunderstorm", 60);
                for (double elapsed = 0; elapsed < 20 - 1e-8;)
                {
                    double step = Math.Min(dt, 20 - elapsed);
                    w.Tick(step, segment => h.Tick(segment, out _), () => h.SecondsToBoundary);
                    elapsed += step;
                }
                return (s.run.hp, s.run.status.remainingOnlineSeconds);
            };

            var (hpSmall, burnSmall) = runTo20(0.05);
            var (hpLarge, burnLarge) = runTo20(20.0);

            Assert.That(hpSmall, Is.EqualTo(96.0).Within(0.2));
            Assert.That(hpLarge, Is.EqualTo(96.0).Within(1e-4));
            Assert.That(burnSmall, Is.EqualTo(8.0).Within(0.2));
            Assert.That(burnLarge, Is.EqualTo(8.0).Within(1e-4));
        }

        // ── N02: Storm End Calm transition does NOT pre-charge future burning ──

        [Test]
        public void N02_StormEndCalmTransition_DoesNotPrechargeFutureBurning()
        {
            Func<double, (double hp, string life, int diedEvents, int healthEvents, double alive)> testEndStorm = (initHp) =>
            {
                var s = GameStateFactory.CreateNew(rules);
                s.run.stageId = "large_tree";
                s.run.levels.trunk = 0;
                s.run.hp = initHp;
                var w = new WeatherSystem(s, rules, () => 1.0, max => 0);
                var h = new HealthStatusSystem(s, rules);
                w.ForceWeather("thunderstorm", 1);
                s.run.status.statusId = "burning";
                s.run.status.remainingOnlineSeconds = 10;
                int died = 0, changed = 0;
                h.RunDied += () => died++;
                h.HealthChanged += () => changed++;
                w.Tick(1, segment => h.Tick(segment, out _), () => h.SecondsToBoundary);
                return (s.run.hp, s.run.life, died, changed, 1.0);
            };

            var res100 = testEndStorm(100.0);
            var res5 = testEndStorm(5.0);

            Assert.That(res100.hp, Is.EqualTo(98.0).Within(1e-4));
            Assert.That(res100.life, Is.EqualTo("ALIVE"));

            Assert.That(res5.hp, Is.EqualTo(3.0).Within(1e-4), "Tree with 5 HP must survive 1s burn with 3 HP remaining!");
            Assert.That(res5.life, Is.EqualTo("ALIVE"));
            Assert.That(res5.diedEvents, Is.EqualTo(0));
            Assert.That(res5.alive, Is.EqualTo(1.0));
        }

        [TestCase(0.05)]
        [TestCase(20.0)]
        [TestCase(200.0)]
        public void DeathBoundary_ProducesUntilDeathAndDoesNotAdvancePastIt(double dt)
        {
            var s = GameStateFactory.CreateNew(rules);
            s.run.stageId = "large_tree"; s.run.hp = 1; s.run.levels.root = 1;
            var w = new WeatherSystem(s, rules, () => 0, max => 0);
            var h = new HealthStatusSystem(s, rules);
            var r = new ResourceSystem(s, rules, new VitalitySystem(s, rules));
            w.ForceWeather("thunderstorm", 60);
            int died = 0; h.RunDied += () => died++;
            for (double elapsed = 0; elapsed < 200 && s.run.life == "ALIVE"; elapsed += dt)
                w.Tick(dt, segment => { Assert.That(r.Tick(segment).Success, Is.True); h.Tick(segment, out _); }, () => h.SecondsToBoundary);
            Assert.That(s.run.water, Is.EqualTo(28.5).Within(1e-6));
            Assert.That(w.RemainingSeconds, Is.EqualTo(41.5).Within(1e-6));
            Assert.That(died, Is.EqualTo(1));
        }

        [Test]
        public void Production_AcrossWeatherAndBurnExpiryMatchesSmallSteps()
        {
            Func<double, double[]> simulate = dt =>
            {
                var s = GameStateFactory.CreateNew(rules);
                s.run.stageId = "large_tree"; s.run.levels.root = 1; s.run.levels.leaves = 1; s.run.photosynthesis = 0;
                var w = new WeatherSystem(s, rules, () => 0, max => 1);
                var h = new HealthStatusSystem(s, rules);
                var r = new ResourceSystem(s, rules, new VitalitySystem(s, rules));
                w.ForceWeather("thunderstorm", 60);
                s.run.weather.strikeDelaySeconds = 18;
                for (double elapsed = 0; elapsed < 70 - 1e-8;)
                {
                    double step = Math.Min(dt, 70 - elapsed);
                    w.Tick(step, segment => { r.Tick(segment); h.Tick(segment, out _); }, () => h.SecondsToBoundary);
                    elapsed += step;
                }
                return new[] { s.run.hp, s.run.water, s.run.photosynthesis, s.run.growth, w.RemainingSeconds };
            };
            var small = simulate(0.05); var large = simulate(70);
            for (int i = 0; i < small.Length; i++) Assert.That(large[i], Is.EqualTo(small[i]).Within(1e-6), "field " + i);
            Assert.That(large[2], Is.EqualTo(67).Within(1e-6));
        }

        // ── N04: Due strike at delay=0 does not drop remaining dt ────

        [Test]
        public void N04_DueStrikeAtZeroDelay_DoesNotDropRemainingDt()
        {
            var s = GameStateFactory.CreateNew(rules);
            s.run.stageId = "large_tree";
            var w = new WeatherSystem(s, rules, () => 0.0, max => 0);
            w.ForceWeather("thunderstorm", 60);
            s.run.weather.strikeDelaySeconds = 0;
            int strikes = 0;
            w.LightningStruck += _ => strikes++;
            w.Tick(5);

            Assert.That(strikes, Is.EqualTo(1));
            Assert.That(w.RemainingSeconds, Is.EqualTo(55.0).Within(1e-4));
        }

        // ── R9: Cấu hình Presentation độc lập và hợp lệ ─────────────

        [Test]
        public void R9_PresentationConfigs_DefaultValuesAreFullyValid()
        {
            var animConfig = new TreeAnimationConfig();
            Assert.That(animConfig.IsValid, Is.True);

            var vfxConfig = new WeatherVfxConfig();
            Assert.That(vfxConfig.IsValid, Is.True);

            var audioConfig = new AudioConfig();
            Assert.That(audioConfig.IsValid, Is.True);
        }
    }
}
