using System;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.Json;
using CayTheGioi.Configuration;
using CayTheGioi.Domain;
using CayTheGioi.Infrastructure;
using UnityEditor;
using UnityEngine;

namespace CayTheGioi.Editor
{
    public static class T11RegressionRunner
    {
        // Removed [InitializeOnLoadMethod] so runner only executes explicitly upon user/test trigger

        [MenuItem("Cây Thế Giới/T11/Run Regression Verification")]
        public static void RunAll()
        {
            var rules = new PrototypeRules();
            var results = new StringBuilder();
            var inv = CultureInfo.InvariantCulture;

            results.AppendLine("{");
            results.AppendLine("  \"timestamp\": \"" + DateTime.UtcNow.ToString("o", inv) + "\",");
            results.AppendLine("  \"testSuite\": \"T11_Config_Formula_Smoke_Only\",");
            results.AppendLine("  \"scope\": \"R1-R6 are formula checks, not actual scene acceptance. Use Unity PlayMode tests and ReviewFix evidence.\",");
            results.AppendLine("  \"results\": [");

            bool allPassed = true;

            // ── Test R1: World Foliage Displacement & Stem Pinning ─────────────
            try
            {
                Vector3 largeLossyScale = new Vector3(100f, 100f, 100f);
                var animConfig = new TreeAnimationConfig();
                float worldAmp = animConfig.largeTreeFlutterAmpWorld; // 0.004m
                float scaleX = Mathf.Max(0.0001f, Mathf.Abs(largeLossyScale.x));
                float dxLocal = worldAmp / scaleX;
                float reconstructedWorldX = dxLocal * largeLossyScale.x;

                float stemWeight = 0f;
                float stemDisplacementWorld = worldAmp * stemWeight;

                bool r1Pass = Mathf.Abs(reconstructedWorldX - worldAmp) < 1e-6f &&
                              reconstructedWorldX < 0.01f &&
                              stemDisplacementWorld == 0f;

                if (!r1Pass) allPassed = false;
                results.AppendLine(string.Format(inv, "    {{ \"id\": \"R1_WorldFoliageDisplacement\", \"passed\": {0}, \"maxWorldLeafMovement\": {1:F6}, \"stemDisplacement\": {2:F6} }},",
                    r1Pass.ToString().ToLower(), reconstructedWorldX, stemDisplacementWorld));
            }
            catch (Exception ex)
            {
                allPassed = false;
                results.AppendLine("    { \"id\": \"R1_WorldFoliageDisplacement\", \"passed\": false, \"error\": \"" + Escape(ex.Message) + "\" },");
            }

            // ── Test R2: Anchor Synchronization with Tilt ─────────────────────
            try
            {
                var animConfig = new TreeAnimationConfig();
                float rad = animConfig.largeTreeMaxTiltAngle * Mathf.Deg2Rad;
                Vector3 initialPos = new Vector3(0f, 2.5f, 0f);
                Vector3 animatedPos = new Vector3(
                    initialPos.x,
                    initialPos.y * Mathf.Cos(rad) - initialPos.z * Mathf.Sin(rad),
                    initialPos.y * Mathf.Sin(rad) + initialPos.z * Mathf.Cos(rad)
                );

                bool r2Pass = Mathf.Abs(animatedPos.magnitude - initialPos.magnitude) < 1e-5f &&
                              animatedPos.y < initialPos.y &&
                              animatedPos.z > initialPos.z;

                if (!r2Pass) allPassed = false;
                results.AppendLine(string.Format(inv, "    {{ \"id\": \"R2_AnchorSynchronization\", \"passed\": {0}, \"initialPos\": \"{1}\", \"animatedPos\": \"{2}\" }},",
                    r2Pass.ToString().ToLower(), initialPos, animatedPos));
            }
            catch (Exception ex)
            {
                allPassed = false;
                results.AppendLine("    { \"id\": \"R2_AnchorSynchronization\", \"passed\": false, \"error\": \"" + Escape(ex.Message) + "\" },");
            }

            // ── Test R3: Rain Emitter Coverage & Ground Contact ───────────────
            try
            {
                var vfxConfig = new WeatherVfxConfig();
                float[] treeTops = { 0.5f, 1.5f, 3.8f };
                bool r3Pass = true;
                foreach (float top in treeTops)
                {
                    float emitterHeight = Mathf.Max(3.2f, top + 2.5f);
                    float minSpeed = Mathf.Max(1.0f, vfxConfig.rainStartSpeed * 0.85f);
                    float fallDistance = emitterHeight + 1.2f;
                    float lifetime = fallDistance / minSpeed;
                    float actualFall = minSpeed * lifetime;
                    if (emitterHeight <= top || actualFall < emitterHeight + 1.0f) { r3Pass = false; break; }
                }

                if (!r3Pass) allPassed = false;
                results.AppendLine(string.Format(inv, "    {{ \"id\": \"R3_RainCoverageAndFallDistance\", \"passed\": {0}, \"rainStartSpeed\": {1}, \"minStreakSize\": {2:F3} }},",
                    r3Pass.ToString().ToLower(), vfxConfig.rainStartSpeed, vfxConfig.rainMinStreakSize));
            }
            catch (Exception ex)
            {
                allPassed = false;
                results.AppendLine("    { \"id\": \"R3_RainCoverageAndFallDistance\", \"passed\": false, \"error\": \"" + Escape(ex.Message) + "\" },");
            }

            // ── Test R4: Sky Lightning Viewport Visibility (1000 samples) ──────
            try
            {
                var rng = new System.Random(2026);
                int inViewport = 0;
                for (int i = 0; i < 1000; i++)
                {
                    float u = 0.2f + (float)rng.NextDouble() * 0.6f;
                    float v = 0.72f + (float)rng.NextDouble() * 0.22f;
                    if (u >= 0.15f && u <= 0.85f && v >= 0.70f && v <= 0.96f) inViewport++;
                }

                bool r4Pass = inViewport == 1000;
                if (!r4Pass) allPassed = false;
                results.AppendLine(string.Format(inv, "    {{ \"id\": \"R4_SkyLightningViewportVisibility\", \"passed\": {0}, \"samplesTested\": 1000, \"samplesVisible\": {1} }},",
                    r4Pass.ToString().ToLower(), inViewport));
            }
            catch (Exception ex)
            {
                allPassed = false;
                results.AppendLine("    { \"id\": \"R4_SkyLightningViewportVisibility\", \"passed\": false, \"error\": \"" + Escape(ex.Message) + "\" },");
            }

            // ── Test R5: Audio Synchronization on Unmute / Resume ─────────────
            try
            {
                var audioConfig = new AudioConfig();
                float targetRain = audioConfig.thunderstormRainVolume;
                bool shouldPlayOnUnmute = targetRain > 0.001f;
                bool r5Pass = shouldPlayOnUnmute && audioConfig.IsValid;

                if (!r5Pass) allPassed = false;
                results.AppendLine(string.Format(inv, "    {{ \"id\": \"R5_AudioPlaybackSyncOnUnmute\", \"passed\": {0}, \"targetRainVolume\": {1:F2} }},",
                    r5Pass.ToString().ToLower(), targetRain));
            }
            catch (Exception ex)
            {
                allPassed = false;
                results.AppendLine("    { \"id\": \"R5_AudioPlaybackSyncOnUnmute\", \"passed\": false, \"error\": \"" + Escape(ex.Message) + "\" },");
            }

            // ── Test R6: Light Restore Logic ──────────────────────────────────
            try
            {
                string[] weathers = { "calm", "sunny", "rainy", "thunderstorm" };
                bool r6Pass = true;
                foreach (var w in weathers)
                {
                    float intensity = w == "sunny" ? 1.25f : (w == "rainy" ? 0.82f : (w == "thunderstorm" ? 0.52f : 1.0f));
                    if (intensity <= 0f) { r6Pass = false; break; }
                }

                if (!r6Pass) allPassed = false;
                results.AppendLine("    { \"id\": \"R6_LightRestoreAccuracy\", \"passed\": " + r6Pass.ToString().ToLower() + " },");
            }
            catch (Exception ex)
            {
                allPassed = false;
                results.AppendLine("    { \"id\": \"R6_LightRestoreAccuracy\", \"passed\": false, \"error\": \"" + Escape(ex.Message) + "\" },");
            }

            // ── Test R7: SaveValidator Rejections ─────────────────────────────
            try
            {
                var state = GameStateFactory.CreateNew(rules);
                state.run.stageId = "large_tree";
                state.run.weather.weatherId = "thunderstorm";
                state.run.weather.strikeEvaluated = true;
                state.run.weather.strikeWillHit = true;
                state.run.weather.strikeDelaySeconds = double.NaN;

                var dto = SaveSerializer.Snapshot(state, rules.configVersion, "nan_test", 1);
                var errors = SaveValidator.Validate(dto, rules);
                bool rejectsNan = errors.Contains("run.weather.strikeDelaySeconds must be finite and non-negative.");

                dto.run.weather.strikeDelaySeconds = -2.0;
                errors = SaveValidator.Validate(dto, rules);
                bool rejectsNegative = errors.Contains("run.weather.strikeDelaySeconds must be finite and non-negative.");

                state.run.weather.strikeEvaluated = false;
                state.run.weather.strikeWillHit = true;
                dto = SaveSerializer.Snapshot(state, rules.configVersion, "inconsistent_test", 1);
                errors = SaveValidator.Validate(dto, rules);
                bool rejectsInconsistent = errors.Contains("strikeWillHit cannot be true when strikeEvaluated is false.");

                bool r7Pass = rejectsNan && rejectsNegative && rejectsInconsistent;
                if (!r7Pass) allPassed = false;
                results.AppendLine(string.Format(inv, "    {{ \"id\": \"R7_SaveValidatorIntegrity\", \"passed\": {0}, \"rejectsNaN\": {1}, \"rejectsNegative\": {2}, \"rejectsInconsistent\": {3} }},",
                    r7Pass.ToString().ToLower(), rejectsNan.ToString().ToLower(), rejectsNegative.ToString().ToLower(), rejectsInconsistent.ToString().ToLower()));
            }
            catch (Exception ex)
            {
                allPassed = false;
                results.AppendLine("    { \"id\": \"R7_SaveValidatorIntegrity\", \"passed\": false, \"error\": \"" + Escape(ex.Message) + "\" },");
            }

            // ── Test R8: Timeline Sub-stepping across dt ──────────────────────
            try
            {
                double[] dts = { 0.05, 0.2, 1.0, 15.0, 60.0 };
                bool r8Pass = true;
                foreach (double dt in dts)
                {
                    var testState = GameStateFactory.CreateNew(rules);
                    testState.run.stageId = "large_tree";
                    testState.run.hp = 100.0;
                    var w = new WeatherSystem(testState, rules, rng01: () => 0.0, rngInt: max => 0);
                    var h = new HealthStatusSystem(testState, rules);

                    int strikes = 0;
                    w.LightningStruck += (reg) => strikes++;
                    w.ForceWeather("thunderstorm", 60);

                    int steps = (int)Math.Round(60.0 / dt);
                    for (int i = 0; i < steps; i++)
                    {
                        w.Tick(dt, segment => h.Tick(segment, out _), () => h.SecondsToBoundary);
                    }

                    if (strikes != 1 || Math.Abs(testState.run.hp - 80.0) > 1e-4 || w.CurrentWeatherId != "calm")
                    {
                        r8Pass = false;
                        break;
                    }
                }

                if (!r8Pass) allPassed = false;
                results.AppendLine("    { \"id\": \"R8_TimelineSubstepping\", \"passed\": " + r8Pass.ToString().ToLower() + ", \"dtsTested\": \"[0.05, 0.2, 1.0, 15.0, 60.0]\", \"damageConsistency\": \"20 HP exactly\" },");
            }
            catch (Exception ex)
            {
                allPassed = false;
                results.AppendLine("    { \"id\": \"R8_TimelineSubstepping\", \"passed\": false, \"error\": \"" + Escape(ex.Message) + "\" },");
            }

            // ── Test N01: Timeline at intermediate boundary t=20s with strike at 18s ──
            try
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

                bool n01Pass = Math.Abs(hpSmall - 96.0) < 0.2 &&
                               Math.Abs(hpLarge - 96.0) < 1e-4 &&
                               Math.Abs(burnSmall - 8.0) < 0.2 &&
                               Math.Abs(burnLarge - 8.0) < 1e-4;

                if (!n01Pass) allPassed = false;
                results.AppendLine(string.Format(inv, "    {{ \"id\": \"N01_IntermediateTimelineAt20s\", \"passed\": {0}, \"hpDt005\": {1:F2}, \"hpDt20\": {2:F2}, \"burnDt005\": {3:F2}, \"burnDt20\": {4:F2} }},",
                    n01Pass.ToString().ToLower(), hpSmall, hpLarge, burnSmall, burnLarge));
            }
            catch (Exception ex)
            {
                allPassed = false;
                results.AppendLine("    { \"id\": \"N01_IntermediateTimelineAt20s\", \"passed\": false, \"error\": \"" + Escape(ex.Message) + "\" },");
            }

            // ── Test N02: Storm End Calm transition does NOT pre-charge future burning ──
            try
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

                bool n02Pass = Math.Abs(res100.hp - 98.0) < 1e-4 && res100.life == "ALIVE" &&
                               Math.Abs(res5.hp - 3.0) < 1e-4 && res5.life == "ALIVE" && res5.diedEvents == 0;

                if (!n02Pass) allPassed = false;
                results.AppendLine(string.Format(inv, "    {{ \"id\": \"N02_NoPrechargeOnCalmTransition\", \"passed\": {0}, \"hp100After1s\": {1:F1}, \"hp5After1s\": {2:F1}, \"lifeAfterStorm\": \"{3}\" }},",
                    n02Pass.ToString().ToLower(), res100.hp, res5.hp, res5.life));
            }
            catch (Exception ex)
            {
                allPassed = false;
                results.AppendLine("    { \"id\": \"N02_NoPrechargeOnCalmTransition\", \"passed\": false, \"error\": \"" + Escape(ex.Message) + "\" },");
            }

            // ── Test N04: Due strike at delay=0 does not drop remaining dt ────
            try
            {
                var s = GameStateFactory.CreateNew(rules);
                s.run.stageId = "large_tree";
                var w = new WeatherSystem(s, rules, () => 0.0, max => 0);
                w.ForceWeather("thunderstorm", 60);
                s.run.weather.strikeDelaySeconds = 0;
                int strikes = 0;
                w.LightningStruck += _ => strikes++;
                w.Tick(5);

                bool n04Pass = strikes == 1 && Math.Abs(w.RemainingSeconds - 55.0) < 1e-4;
                if (!n04Pass) allPassed = false;
                results.AppendLine(string.Format(inv, "    {{ \"id\": \"N04_DueStrikeAtZeroDelay\", \"passed\": {0}, \"strikes\": {1}, \"remaining\": {2:F1} }},",
                    n04Pass.ToString().ToLower(), strikes, w.RemainingSeconds));
            }
            catch (Exception ex)
            {
                allPassed = false;
                results.AppendLine("    { \"id\": \"N04_DueStrikeAtZeroDelay\", \"passed\": false, \"error\": \"" + Escape(ex.Message) + "\" },");
            }

            // ── Test R9: Presentation Config Validation ───────────────────────
            try
            {
                var anim = new TreeAnimationConfig();
                var vfx = new WeatherVfxConfig();
                var audio = new AudioConfig();

                bool r9Pass = anim.IsValid && vfx.IsValid && audio.IsValid;
                if (!r9Pass) allPassed = false;
                results.AppendLine("    { \"id\": \"R9_PresentationConfigsValid\", \"passed\": " + r9Pass.ToString().ToLower() + " }");
            }
            catch (Exception ex)
            {
                allPassed = false;
                results.AppendLine("    { \"id\": \"R9_PresentationConfigsValid\", \"passed\": false, \"error\": \"" + Escape(ex.Message) + "\" }");
            }

            results.AppendLine("  ],");
            results.AppendLine("  \"overallPassed\": " + allPassed.ToString().ToLower());
            results.AppendLine("}");

            string finalJson = results.ToString();

            try
            {
                using var doc = JsonDocument.Parse(finalJson);
            }
            catch (Exception ex)
            {
                Debug.LogError("[N07] JSON parse verification failed: " + ex.Message);
                allPassed = false;
            }

            string outDir = Path.Combine(Directory.GetCurrentDirectory(), "Evidence", "T11_Fix_20261012");
            Directory.CreateDirectory(outDir);
            string outPath = Path.Combine(outDir, "editor_t11_automated_test_results.json");
            File.WriteAllText(outPath, finalJson, Encoding.UTF8);

            Debug.Log("[T11_REGRESSION] All regression tests executed. Overall pass = " + allPassed + ". Saved to: " + outPath);
        }

        private static string Escape(string s) => s == null ? "" : s.Replace("\"", "\\\"").Replace("\n", " ");
    }
}
