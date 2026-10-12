using System;
using System.Collections.Generic;
using CayTheGioi.Domain;
using CayTheGioi.Infrastructure;
using NUnit.Framework;
using UnityEngine;

namespace CayTheGioi.Tests
{
    public sealed class WeatherTests
    {
        private PrototypeRules rules;
        private GameState state;

        [SetUp]
        public void Setup()
        {
            rules = new PrototypeRules();
            state = GameStateFactory.CreateNew(rules);
        }

        // ── 1. Duration and Unlock Rules ──────────────────────────────

        [Test]
        public void Seedling_OnlyHasCalmWeather()
        {
            state.run.stageId = "seedling";
            var weather = new WeatherSystem(state, rules);

            // Advance weather 10 times, must always remain calm on seedling
            for (int i = 0; i < 10; i++)
            {
                weather.AdvanceWeather();
                Assert.That(weather.CurrentWeatherId, Is.EqualTo("calm"));
                Assert.That(weather.RemainingSeconds, Is.EqualTo(rules.initialCalmSeconds));
            }
        }

        [Test]
        public void YoungTree_UnlocksSunnyAndRainy_NeverThunderstorm()
        {
            state.run.stageId = "young_tree";
            var chosen = new HashSet<string>();

            // Mock RNG to cycle through possibilities
            int rngCall = 0;
            var weather = new WeatherSystem(state, rules,
                rng01: () => 0.5,
                rngInt: max => (rngCall++) % max);

            for (int i = 0; i < 30; i++)
            {
                weather.AdvanceWeather();
                chosen.Add(weather.CurrentWeatherId);
                Assert.That(weather.CurrentWeatherId, Is.Not.EqualTo("thunderstorm"),
                    "Young tree must never receive thunderstorm weather.");
                if (weather.CurrentWeatherId != "calm")
                {
                    Assert.That(weather.RemainingSeconds, Is.InRange(rules.minWeatherDurationSeconds, rules.maxWeatherDurationSeconds));
                }
            }

            Assert.That(chosen, Does.Contain("sunny"));
            Assert.That(chosen, Does.Contain("rainy"));
            Assert.That(chosen, Does.Contain("calm"));
        }

        [Test]
        public void LargeTree_UnlocksThunderstorm()
        {
            state.run.stageId = "large_tree";
            var chosen = new HashSet<string>();

            int rngCall = 0;
            var weather = new WeatherSystem(state, rules,
                rng01: () => 0.5,
                rngInt: max => (rngCall++) % max);

            for (int i = 0; i < 40; i++)
            {
                weather.AdvanceWeather();
                chosen.Add(weather.CurrentWeatherId);
            }

            Assert.That(chosen, Does.Contain("thunderstorm"));
            Assert.That(chosen, Does.Contain("sunny"));
            Assert.That(chosen, Does.Contain("rainy"));
            Assert.That(chosen, Does.Contain("calm"));
        }

        // ── 2. Sequencing Rules (Bad streak & Anti-repeat) ───────────

        [Test]
        public void AfterThunderstorm_MustAlwaysTransitionToCalm()
        {
            state.run.stageId = "large_tree";
            var weather = new WeatherSystem(state, rules,
                rng01: () => 0.99,
                rngInt: max => max - 1);

            weather.ForceWeather("thunderstorm", 10);
            Assert.That(weather.CurrentWeatherId, Is.EqualTo("thunderstorm"));

            // Advance weather after thunderstorm
            weather.AdvanceWeather();
            Assert.That(weather.CurrentWeatherId, Is.EqualTo("calm"),
                "After bad weather (thunderstorm), weather must always transition to calm.");
            Assert.That(weather.RemainingSeconds, Is.EqualTo(rules.initialCalmSeconds));
            Assert.That(weather.BadStreak, Is.EqualTo(0));
        }

        [Test]
        public void NonCalmWeather_NeverRepeatsTwiceInARow()
        {
            state.run.stageId = "young_tree";
            string prior = "calm";

            // Predictable RNG
            int step = 0;
            var weather = new WeatherSystem(state, rules,
                rng01: () => 0.25,
                rngInt: max => (step++) % max);

            for (int i = 0; i < 30; i++)
            {
                weather.AdvanceWeather();
                string current = weather.CurrentWeatherId;
                if (prior != "calm" && current != "calm")
                {
                    Assert.That(current, Is.Not.EqualTo(prior),
                        $"Weather '{current}' must not repeat immediately after '{prior}'.");
                }
                prior = current;
            }
        }

        // ── 3. Production Multipliers ─────────────────────────────────

        [Test]
        public void SunnyWeather_BuffsLeavesGrowthBy20Percent()
        {
            state.run.water = 100;
            state.run.levels.root = 2;
            state.run.levels.leaves = 2;

            // Neutral / Calm production
            state.run.weather.weatherId = "calm";
            var res = new ResourceSystem(state, rules, new VitalitySystem(state, rules));
            var neutralRates = res.Rates();

            // Sunny production
            state.run.weather.weatherId = "sunny";
            var sunnyRates = res.Rates();

            Assert.That(sunnyRates.PhotosynthesisProduced, Is.EqualTo(neutralRates.PhotosynthesisProduced * 1.2).Within(1e-9));
            Assert.That(sunnyRates.WaterProduced, Is.EqualTo(neutralRates.WaterProduced).Within(1e-9),
                "Sunny should not buff water production.");
        }

        [Test]
        public void RainyWeather_BuffsRootWaterBy20Percent()
        {
            state.run.water = 100;
            state.run.levels.root = 2;
            state.run.levels.leaves = 2;

            state.run.weather.weatherId = "calm";
            var res = new ResourceSystem(state, rules, new VitalitySystem(state, rules));
            var neutralRates = res.Rates();

            state.run.weather.weatherId = "rainy";
            var rainyRates = res.Rates();

            Assert.That(rainyRates.WaterProduced, Is.EqualTo(neutralRates.WaterProduced * 1.2).Within(1e-9));
            Assert.That(rainyRates.PhotosynthesisProduced, Is.EqualTo(neutralRates.PhotosynthesisProduced).Within(1e-9),
                "Rainy should not directly buff growth rate beyond water availability.");
        }

        [Test]
        public void VitalityAndWeatherBuffs_MultiplyIndependentlyWithoutDoubleCounting()
        {
            state.run.water = 100;
            state.run.levels.root = 2;
            state.run.levels.leaves = 2;
            state.vitality.bankSeconds = 3600; // +5% buff

            state.run.weather.weatherId = "sunny";
            var res = new ResourceSystem(state, rules, new VitalitySystem(state, rules));
            var rates = res.Rates();

            // Base growth = 2 * 1 * 1.05 (vitality) * 1.2 (sunny) = 2.52
            double expectedGrowth = 2.0 * rules.leavesPhotosynthesisPerLevelSecond * rules.vitalityOutputMultiplier * rules.sunnyGrowthMultiplier;
            Assert.That(rates.PhotosynthesisProduced, Is.EqualTo(expectedGrowth).Within(1e-9));
        }

        // ── 4. Exposure, Protection and Strike Probability ────────────

        [Test]
        public void EarlyStages_ExposureZero_StrikeProbabilityIsZero()
        {
            state.run.stageId = "seedling";
            var weather = new WeatherSystem(state, rules);
            double probSeedling = weather.CalculateStrikeProbability(100.0, 0, 0);
            Assert.That(probSeedling, Is.EqualTo(0));

            state.run.stageId = "young_tree";
            double probYoung = weather.CalculateStrikeProbability(100.0, 0, 0);
            Assert.That(probYoung, Is.EqualTo(0));
        }

        [Test]
        public void Thunderstorm_Rolls10PercentStrikeChance_OncePerSpell()
        {
            state.run.stageId = "large_tree";

            // Case 1: Roll < 0.10 (e.g. 0.05) -> Strike scheduled
            var weatherHit = new WeatherSystem(state, rules, rng01: () => 0.05);
            weatherHit.ForceWeather("thunderstorm", 100.0);

            Assert.That(weatherHit.StrikeEvaluated, Is.True);
            Assert.That(weatherHit.StrikeWillHit, Is.True);
            Assert.That(weatherHit.StrikeExecuted, Is.False);
            Assert.That(weatherHit.StrikeDelaySeconds, Is.EqualTo(100.0 * rules.strikeDelayFraction).Within(1e-6));

            int strikes = 0;
            weatherHit.LightningStruck += _ => strikes++;

            // Tick halfway through delay -> not yet struck
            weatherHit.Tick(15.0);
            Assert.That(strikes, Is.EqualTo(0));
            Assert.That(weatherHit.StrikeExecuted, Is.False);

            // Tick past delay (delay was 30s) -> strikes exactly once
            weatherHit.Tick(20.0);
            Assert.That(strikes, Is.EqualTo(1));
            Assert.That(weatherHit.StrikeExecuted, Is.True);
            Assert.That(state.run.status.statusId, Is.EqualTo("burning"));

            // Tick further in same thunderstorm -> never strikes a second time
            weatherHit.Tick(40.0);
            Assert.That(strikes, Is.EqualTo(1), "Thunderstorm must never strike more than once per spell.");
        }

        [Test]
        public void Thunderstorm_MissesWhenRollAbove10Percent()
        {
            state.run.stageId = "large_tree";

            // Roll >= 0.10 (e.g. 0.15) -> No strike
            var weatherMiss = new WeatherSystem(state, rules, rng01: () => 0.15);
            weatherMiss.ForceWeather("thunderstorm", 100.0);

            Assert.That(weatherMiss.StrikeEvaluated, Is.True);
            Assert.That(weatherMiss.StrikeWillHit, Is.False);
            Assert.That(weatherMiss.StrikeExecuted, Is.False);

            int strikes = 0;
            weatherMiss.LightningStruck += _ => strikes++;

            // Tick through entire thunderstorm
            weatherMiss.Tick(100.0);
            Assert.That(strikes, Is.EqualTo(0));
            Assert.That(state.run.status.statusId, Is.Not.EqualTo("burning"));
        }

        [Test]
        public void SeedlingAndYoungTree_NeverGetHit_EvenIfThunderstormForced()
        {
            // Seedling protected
            state.run.stageId = "seedling";
            var weatherSeedling = new WeatherSystem(state, rules, rng01: () => 0.0);
            weatherSeedling.ForceWeather("thunderstorm", 100.0);
            Assert.That(weatherSeedling.StrikeWillHit, Is.False);

            // Young Tree protected
            state.run.stageId = "young_tree";
            var weatherYoung = new WeatherSystem(state, rules, rng01: () => 0.0);
            weatherYoung.ForceWeather("thunderstorm", 100.0);
            Assert.That(weatherYoung.StrikeWillHit, Is.False);
        }

        [Test]
        public void Protection_DoesNotAlterStrikeProbability_Stays10Percent()
        {
            state.run.stageId = "large_tree";
            var weather = new WeatherSystem(state, rules);

            // Protection = 0 vs Protection = 5.0 -> probability remains identical (0.10)
            double prob0 = weather.CalculateStrikeProbability(1.0, trunkProtection: 0, exposure: 1.0);
            double probHigh = weather.CalculateStrikeProbability(1.0, trunkProtection: 5.0, exposure: 1.0);

            Assert.That(prob0, Is.EqualTo(0.10).Within(1e-9));
            Assert.That(probHigh, Is.EqualTo(0.10).Within(1e-9));
        }

        // ── 5. Strike, Cooldown, and Burning ─────────────────────────

        [Test]
        public void LightningStrike_SetsCooldownAndStartsBurning()
        {
            state.run.stageId = "large_tree";
            var weather = new WeatherSystem(state, rules);

            bool strikeFired = false;
            string struckRegion = null;
            weather.LightningStruck += r => { strikeFired = true; struckRegion = r; };

            weather.TriggerStrike("leaves");

            Assert.That(strikeFired, Is.True);
            Assert.That(struckRegion, Is.EqualTo("leaves"));
            Assert.That(state.run.weather.incidentCooldownOnlineSeconds, Is.EqualTo(rules.strikeCooldownOnlineSeconds));
            Assert.That(state.run.weather.strikeExecuted, Is.True);
            Assert.That(state.run.status.statusId, Is.EqualTo("burning"));
            Assert.That(state.run.status.remainingOnlineSeconds, Is.EqualTo(rules.burningDurationSeconds));
            Assert.That(state.run.status.impactRegionId, Is.EqualTo("leaves"));
        }

        [Test]
        public void Protection_ReducesBurningDamage_FormulaCheck()
        {
            state.run.hp = 100;
            state.run.status.statusId = "burning";
            state.run.status.remainingOnlineSeconds = 10;
            state.run.status.impactRegionId = "trunk";

            // 0 Protection: damage = 2 HP/s -> in 5s deals 10 HP
            var health = new HealthStatusSystem(state, rules);
            health.Tick(5.0, out _);
            Assert.That(state.run.hp, Is.EqualTo(90).Within(1e-9));

            // Reset and add 20 trunk levels (20 * 0.05 = 1.0 Protection)
            state.run.hp = 100;
            state.run.status.remainingOnlineSeconds = 10;
            state.run.levels.trunk = 20; // Protection = 1.0
            // Formula: 2 / (1 + 1.0) = 1.0 HP/s -> in 5s deals 5 HP
            health.Tick(5.0, out _);
            Assert.That(state.run.hp, Is.EqualTo(95).Within(1e-9));
        }

        // ── 6. Burning Damage and Debuff ──────────────────────────────

        [Test]
        public void Burning_Deals2HpPerSecond_AndExpiresAfterDuration()
        {
            state.run.hp = 100;
            state.run.status.statusId = "burning";
            state.run.status.remainingOnlineSeconds = 10;
            state.run.status.impactRegionId = "root";

            var health = new HealthStatusSystem(state, rules);

            // Tick 3 seconds
            health.Tick(3.0, out double aliveSeconds);
            Assert.That(aliveSeconds, Is.EqualTo(3.0));
            Assert.That(state.run.hp, Is.EqualTo(94).Within(1e-9)); // 100 - 3 * 2 = 94
            Assert.That(state.run.status.remainingOnlineSeconds, Is.EqualTo(7).Within(1e-9));
            Assert.That(health.IsBurning, Is.True);

            // Tick remaining 7 seconds
            health.Tick(7.0, out aliveSeconds);
            Assert.That(aliveSeconds, Is.EqualTo(7.0));
            Assert.That(state.run.hp, Is.EqualTo(80).Within(1e-9)); // 94 - 7 * 2 = 80
            Assert.That(state.run.status.statusId, Is.EqualTo("none"));
            Assert.That(health.IsBurning, Is.False);
        }

        [Test]
        public void BurningLeaves_ReducesGrowthBy30Percent_OtherRegionsDoNot()
        {
            state.run.water = 100;
            state.run.levels.root = 2;
            state.run.levels.leaves = 2;
            state.run.weather.weatherId = "calm";

            var res = new ResourceSystem(state, rules, new VitalitySystem(state, rules));
            var normalRates = res.Rates();

            // Burning Leaves
            state.run.status.statusId = "burning";
            state.run.status.remainingOnlineSeconds = 10;
            state.run.status.impactRegionId = "leaves";

            var leavesBurnRates = res.Rates();
            Assert.That(leavesBurnRates.PhotosynthesisProduced, Is.EqualTo(normalRates.PhotosynthesisProduced * 0.7).Within(1e-9));

            // Burning Root
            state.run.status.impactRegionId = "root";
            var rootBurnRates = res.Rates();
            Assert.That(rootBurnRates.PhotosynthesisProduced, Is.EqualTo(normalRates.PhotosynthesisProduced).Within(1e-9));
        }

        // ── 7. Death Transition (HP = 0) and Single RunDied Event ─────

        [Test]
        public void HpReachingZero_TriggersDead_AndFiresRunDiedOnce()
        {
            state.run.hp = 4;
            state.run.status.statusId = "burning";
            state.run.status.remainingOnlineSeconds = 10;

            var health = new HealthStatusSystem(state, rules);
            int diedEvents = 0;
            health.RunDied += () => diedEvents++;

            // Tick 5 seconds: burning deals 2 HP/s. Tree dies after 2 seconds!
            health.Tick(5.0, out double aliveSeconds);

            Assert.That(state.run.hp, Is.EqualTo(0));
            Assert.That(state.run.life, Is.EqualTo("DEAD"));
            Assert.That(aliveSeconds, Is.EqualTo(2.0).Within(1e-9));
            Assert.That(diedEvents, Is.EqualTo(1));

            // Subsequent tick should NOT fire RunDied again and aliveSeconds must be 0
            health.Tick(5.0, out double subsequentAlive);
            Assert.That(diedEvents, Is.EqualTo(1));
            Assert.That(subsequentAlive, Is.EqualTo(0));
        }

        [Test]
        public void DeadTree_StopsProductionEntirely()
        {
            state.run.hp = 0;
            state.run.life = "DEAD";
            state.run.water = 50;
            state.run.levels.root = 5;
            state.run.levels.leaves = 5;

            var res = new ResourceSystem(state, rules, new VitalitySystem(state, rules));
            var tickResult = res.Tick(10.0);

            Assert.That(tickResult.Success, Is.False);
            Assert.That(tickResult.Reason, Is.EqualTo("DEAD"));
            Assert.That(state.run.water, Is.EqualTo(50));
            Assert.That(state.run.growth, Is.EqualTo(rules.initialGrowth));
        }

        // ── 8. Calm Removes Burning Without Auto-Heal ─────────────────

        [Test]
        public void TransitionToCalm_ClearsBurning_WithoutHealingHp()
        {
            state.run.hp = 70;
            state.run.status.statusId = "burning";
            state.run.status.remainingOnlineSeconds = 8;
            state.run.status.impactRegionId = "trunk";

            var weather = new WeatherSystem(state, rules);
            weather.ForceWeather("calm");

            Assert.That(state.run.status.statusId, Is.EqualTo("none"));
            Assert.That(state.run.status.remainingOnlineSeconds, Is.EqualTo(0));
            Assert.That(state.run.hp, Is.EqualTo(70), "HP must not auto-heal upon entering calm.");
        }

        // ── 9. Rebirth Clears Burning, Resets Weather and Restores HP ──

        [Test]
        public void ResetForRebirth_RestoresHpAndCalmStatus()
        {
            state.run.hp = 20;
            state.run.status.statusId = "burning";
            state.run.status.remainingOnlineSeconds = 5;
            state.run.weather.weatherId = "thunderstorm";

            var weather = new WeatherSystem(state, rules);
            var health = new HealthStatusSystem(state, rules);

            weather.ResetForRebirth();
            health.ResetForRebirth();

            Assert.That(state.run.hp, Is.EqualTo(100));
            Assert.That(state.run.weather.weatherId, Is.EqualTo("calm"));
            Assert.That(state.run.weather.remainingOnlineSeconds, Is.EqualTo(rules.initialCalmSeconds));
            Assert.That(state.run.status.statusId, Is.EqualTo("none"));
            Assert.That(state.run.status.remainingOnlineSeconds, Is.EqualTo(0));
        }

        // ── 10. Save and Load Validation ──────────────────────────────

        [Test]
        public void SaveData_ValidatesAllWeatherIdsAndBurningStatus()
        {
            string[] weathers = { "calm", "sunny", "rainy", "thunderstorm" };
            foreach (var w in weathers)
            {
                state.run.weather.weatherId = w;
                state.run.weather.remainingOnlineSeconds = 200;
                state.run.status.statusId = "burning";
                state.run.status.remainingOnlineSeconds = 8;
                state.run.status.impactRegionId = "leaves";
                state.run.hp = 85;

                var dto = SaveSerializer.Snapshot(state, rules.configVersion, "test_save", 1);
                var errors = SaveValidator.Validate(dto, rules);
                Assert.That(errors, Is.Empty, $"Save data validation failed for weather '{w}': {string.Join("; ", errors)}");

                string json = SaveCodec.Encode(dto);
                var decodeResult = SaveCodec.Decode(json, rules);
                Assert.That(decodeResult.Success, Is.True, decodeResult.Reason);
                Assert.That(decodeResult.Data.run.weather.weatherId, Is.EqualTo(w));
                Assert.That(decodeResult.Data.run.status.statusId, Is.EqualTo("burning"));
                Assert.That(decodeResult.Data.run.status.impactRegionId, Is.EqualTo("leaves"));
                Assert.That(decodeResult.Data.run.hp, Is.EqualTo(85).Within(1e-9));
            }
        }

        [Test]
        public void SaveData_PreservesStrikeStateAndDoesNotRerollOnLoad()
        {
            state.run.stageId = "large_tree";
            state.run.weather.weatherId = "thunderstorm";
            state.run.weather.remainingOnlineSeconds = 120;
            state.run.weather.strikeEvaluated = true;
            state.run.weather.strikeWillHit = true;
            state.run.weather.strikeDelaySeconds = 28.5;
            state.run.weather.strikeExecuted = false;

            var dto = SaveSerializer.Snapshot(state, rules.configVersion, "strike_save", 1);
            string json = SaveCodec.Encode(dto);
            var decoded = SaveCodec.Decode(json, rules);

            Assert.That(decoded.Success, Is.True);
            var restoredState = new GameState { run = decoded.Data.run, permanent = decoded.Data.permanent, vitality = decoded.Data.vitality, settings = decoded.Data.settings };

            var restoredWeather = new WeatherSystem(restoredState, rules);
            Assert.That(restoredWeather.StrikeEvaluated, Is.True);
            Assert.That(restoredWeather.StrikeWillHit, Is.True);
            Assert.That(restoredWeather.StrikeDelaySeconds, Is.EqualTo(28.5).Within(1e-6));
            Assert.That(restoredWeather.StrikeExecuted, Is.False);
        }

        [Test]
        public void OfflineCatchup_NeverAppliesLightningDamageOrKillsTree()
        {
            state.run.stageId = "large_tree";
            state.run.hp = 80;
            state.run.levels.root = 10;
            state.run.levels.leaves = 10;
            state.run.weather.weatherId = "thunderstorm";
            state.run.weather.strikeEvaluated = true;
            state.run.weather.strikeWillHit = true;
            state.run.weather.strikeDelaySeconds = 10;

            // Offline evaluate 12 hours
            var reward = OfflineProduction.Evaluate(state, rules, 1000, 1000 + 12 * 3600);
            Assert.That(state.run.hp, Is.EqualTo(80), "Offline catchup must never reduce HP from weather/lightning.");
            Assert.That(state.run.life, Is.EqualTo("ALIVE"), "Tree must never die in offline catchup.");
            Assert.That(reward.Dead, Is.False);
        }

        // ── R7 & R8 Regression Tests ─────────────────────────────────

        [Test]
        public void SaveValidator_RejectsNaNOrNegativeStrikeDelay()
        {
            state.run.stageId = "large_tree";
            state.run.weather.weatherId = "thunderstorm";
            state.run.weather.strikeEvaluated = true;
            state.run.weather.strikeWillHit = true;
            state.run.weather.strikeDelaySeconds = double.NaN;

            var dto = SaveSerializer.Snapshot(state, rules.configVersion, "nan_test", 1);
            var errors = SaveValidator.Validate(dto, rules);
            Assert.That(errors, Does.Contain("run.weather.strikeDelaySeconds must be finite and non-negative."));

            dto.run.weather.strikeDelaySeconds = -5.0;
            errors = SaveValidator.Validate(dto, rules);
            Assert.That(errors, Does.Contain("run.weather.strikeDelaySeconds must be finite and non-negative."));
        }

        [Test]
        public void SaveValidator_RejectsInconsistentStrikeFlags()
        {
            state.run.stageId = "large_tree";
            state.run.weather.weatherId = "thunderstorm";
            state.run.weather.strikeEvaluated = false;
            state.run.weather.strikeWillHit = true; // Inconsistent!

            var dto = SaveSerializer.Snapshot(state, rules.configVersion, "inconsistent_test", 1);
            var errors = SaveValidator.Validate(dto, rules);
            Assert.That(errors, Does.Contain("strikeWillHit cannot be true when strikeEvaluated is false."));
        }

        [TestCase(0.1)]
        [TestCase(1.0)]
        [TestCase(10.0)]
        [TestCase(60.0)]
        public void TimelineSubstepping_ProducesConsistentStrikeAndDamageAcrossAllDt(double dt)
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
            Assert.That(testState.run.hp, Is.EqualTo(80.0).Within(1e-5), $"dt={dt} must take exactly 20 damage");
            Assert.That(w.CurrentWeatherId, Is.EqualTo("calm"));
        }
    }
}
