using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CayTheGioi.Bootstrap;
using CayTheGioi.Configuration;
using CayTheGioi.Domain;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace CayTheGioi.Tests
{
    /// <summary>
    /// T09 measurement harness. Drives the real domain systems (same ones MainScene wires) with the deterministic
    /// QaAutoPlayer and writes Evidence/T09/progression_metrics.json. Numbers describe the CURRENT config
    /// (read from PrototypeConfig.asset); they are simulation output, not playtest results.
    /// </summary>
    public sealed class ProgressionMetricsTests
    {
        [Serializable] public sealed class Row
        {
            public string label; public string evolveMode; public double tapsPerSecond; public string points;
            public double firstRootSec, firstLeavesSec, seedlingMaxedSec, youngSec, youngMaxedSec, largeSec, largeMaxedSec;
            public int purchases; public long taps; public double bankAtLarge, finalWater, finalGrowth, longestGapSec; public string longestGapAt; public List<string> purchaseLog = new List<string>();
        }
        [Serializable] public sealed class Death
        {
            public int trunkLevel; public int runs; public double protection;
            public double medianHours, p10Hours, p90Hours, meanStrikes, fractionDeadWithin72h;
        }
        [Serializable] public sealed class Report
        {
            public string configVersion, note; public List<Row> progression = new List<Row>(); public List<Death> death = new List<Death>();
            public double offlineWaterAt24hBank; public string generatedBy = "ProgressionMetricsTests (EditMode simulation)";
        }
        private static Report report;

        private static PrototypeRules Rules()
        {
            var config = AssetDatabase.LoadAssetAtPath<PrototypeConfig>("Assets/_Project/Configs/PrototypeConfig.asset");
            Assert.That(config, Is.Not.Null);
            return JsonUtility.FromJson<PrototypeRules>(JsonUtility.ToJson(config.Rules));
        }

        private sealed class Sim
        {
            public readonly GameState S; public readonly PrototypeRules R; public readonly ResourceSystem Res; public readonly WeatherSystem W;
            public readonly HealthStatusSystem H; public readonly UpgradeSystem Up; public readonly EvolutionSystem Evo; public readonly VitalitySystem Vit; public readonly QaAutoPlayer P;
            public double T; public int Strikes; public readonly List<string> Log = new List<string>();
            public Sim(PrototypeRules rules, int seed, double tps, QaAutoPlayer.EvolveMode mode, GameState state = null)
            {
                R = rules; S = state ?? GameStateFactory.CreateNew(rules);
                var rng = new System.Random(seed);
                Vit = new VitalitySystem(S, R); Res = new ResourceSystem(S, R, Vit);
                W = new WeatherSystem(S, R, rng.NextDouble, rng.Next); H = new HealthStatusSystem(S, R);
                Evo = new EvolutionSystem(S, R, Res); Up = new UpgradeSystem(S, R, Res, () => Evo.IsTransitioning);
                W.LightningStruck += _ => Strikes++;
                Up.Completed += branch => Log.Add(Math.Round(T) + "s " + S.run.stageId + " " + branch + " -> L" + (branch == "root" ? S.run.levels.root : branch == "leaves" ? S.run.levels.leaves : S.run.levels.trunk));
                P = new QaAutoPlayer(S, R, Up, Evo, Vit) { Mode = mode, TapsPerSecond = tps };
            }
            public bool Step(double dt)
            {
                bool evolved = P.Act(dt);
                W.Tick(dt); H.Tick(dt, out double alive);
                if (alive > 0) Res.Tick(alive);
                T += dt; return evolved;
            }
        }

        private static Row Progress(string label, PrototypeRules rules, double tps, QaAutoPlayer.EvolveMode mode, long seedlingPts = 0, long youngPts = 0, long largePts = 0)
        {
            var state = GameStateFactory.CreateNew(rules);
            state.permanent.stagePoints.Find(p => p.stageId == "seedling").points = seedlingPts;
            state.permanent.stagePoints.Find(p => p.stageId == "young_tree").points = youngPts;
            state.permanent.stagePoints.Find(p => p.stageId == "large_tree").points = largePts;
            var sim = new Sim(rules, 7, tps, mode, state);
            var row = new Row { label = label, evolveMode = mode.ToString(), tapsPerSecond = tps, points = seedlingPts + "/" + youngPts + "/" + largePts, firstRootSec = -1, firstLeavesSec = -1, seedlingMaxedSec = -1, youngSec = -1, youngMaxedSec = -1, largeSec = -1, largeMaxedSec = -1 };
            const double limit = 24 * 3600;
            while (sim.T < limit)
            {
                sim.Step(1);
                var l = sim.S.run.levels; var st = rules.FindStage(sim.S.run.stageId);
                if (row.firstRootSec < 0 && l.root >= 1) row.firstRootSec = sim.T;
                if (row.firstLeavesSec < 0 && l.leaves >= 1) row.firstLeavesSec = sim.T;
                bool maxed = l.root >= st.rootCap && l.leaves >= st.leavesCap && l.trunk >= st.trunkCap;
                if (sim.S.run.stageId == "seedling" && maxed && row.seedlingMaxedSec < 0) row.seedlingMaxedSec = sim.T;
                if (sim.S.run.stageId == "young_tree") { if (row.youngSec < 0) row.youngSec = sim.T; if (maxed && row.youngMaxedSec < 0) row.youngMaxedSec = sim.T; }
                if (sim.S.run.stageId == "large_tree")
                {
                    if (row.largeSec < 0) { row.largeSec = sim.T; row.bankAtLarge = sim.S.vitality.bankSeconds; }
                    if (maxed) { row.largeMaxedSec = sim.T; break; }
                }
                if (sim.S.run.life != "ALIVE") break;
            }
            row.purchaseLog = sim.Log;
            double previousTime = 0; double longest = 0; string longestAt = "";
            foreach (string entry in sim.Log) { double at = double.Parse(entry.Substring(0, entry.IndexOf('s')), System.Globalization.CultureInfo.InvariantCulture); if (at - previousTime > longest) { longest = at - previousTime; longestAt = entry; } previousTime = at; }
            row.longestGapSec = longest; row.longestGapAt = longestAt;
            row.purchases = sim.P.Purchases; row.taps = sim.P.Taps; row.finalWater = sim.S.run.water; row.finalGrowth = sim.S.run.growth;
            return row;
        }

        private static double Percentile(List<double> sorted, double q) => sorted[Math.Min(sorted.Count - 1, (int)Math.Floor(q * (sorted.Count - 1)))];

        private static Death DeathStats(PrototypeRules rules, int trunk, int runs)
        {
            var hours = new List<double>(); long strikes = 0; int deadWithin = 0;
            const double limit = 72 * 3600; const double dt = 10;
            for (int i = 0; i < runs; i++)
            {
                var state = GameStateFactory.CreateNew(rules);
                state.run.stageId = "large_tree"; state.run.levels.root = 20; state.run.levels.leaves = 20; state.run.levels.trunk = trunk;
                var sim = new Sim(rules, 1000 + i * 7919 + trunk, 0, QaAutoPlayer.EvolveMode.AfterCaps, state);
                while (sim.T < limit && sim.S.run.life == "ALIVE") { sim.W.Tick(dt); sim.H.Tick(dt, out _); sim.T += dt; }
                if (sim.S.run.life == "DEAD") deadWithin++;
                hours.Add(sim.T / 3600.0); strikes += sim.Strikes;
            }
            hours.Sort();
            return new Death
            {
                trunkLevel = trunk, runs = runs, protection = trunk * rules.trunkProtectionPerLevel,
                medianHours = Percentile(hours, .5), p10Hours = Percentile(hours, .1), p90Hours = Percentile(hours, .9),
                meanStrikes = strikes / (double)runs, fractionDeadWithin72h = deadWithin / (double)runs
            };
        }

        [OneTimeSetUp] public void Start()
        {
            var rules = Rules();
            report = new Report { configVersion = rules.configVersion, note = "Simulation of the real domain systems with QaAutoPlayer; not playtest data. Online time only; 1s steps for progression, 10s steps for lightning." };
        }

        [Test] public void ProgressionToLargeTreeIsReachableForIdleAndActivePlayers()
        {
            var rules = Rules();
            var rows = new[]
            {
                Progress("idle, evolve as soon as affordable", rules, 0, QaAutoPlayer.EvolveMode.Asap),
                Progress("active 3 taps/s, evolve as soon as affordable", rules, 3, QaAutoPlayer.EvolveMode.Asap),
                Progress("idle, max caps before evolving", rules, 0, QaAutoPlayer.EvolveMode.AfterCaps),
                Progress("active 3 taps/s, max caps before evolving", rules, 3, QaAutoPlayer.EvolveMode.AfterCaps),
                Progress("idle, evolve ASAP, points 3/3/3 (after Rebirths)", rules, 0, QaAutoPlayer.EvolveMode.Asap, 3, 3, 3),
                Progress("idle, max caps first, points 3/3/3 (after Rebirths)", rules, 0, QaAutoPlayer.EvolveMode.AfterCaps, 3, 3, 3)
            };
            report.progression.AddRange(rows);
            foreach (var row in rows)
            {
                Debug.Log("[T09 metrics] " + JsonUtility.ToJson(row));
                Assert.That(row.firstRootSec, Is.GreaterThanOrEqualTo(0), row.label);
                Assert.That(row.firstLeavesSec, Is.GreaterThanOrEqualTo(row.firstRootSec), row.label);
                Assert.That(row.youngSec, Is.GreaterThan(0), "Never reached Cây Non: " + row.label);
                Assert.That(row.largeSec, Is.GreaterThan(row.youngSec), "Never reached Cây Lớn: " + row.label);
                Assert.That(row.largeSec, Is.LessThan(24 * 3600), row.label);
                Assert.That(double.IsFinite(row.finalWater) && double.IsFinite(row.finalGrowth), Is.True, row.label);
            }
            // Evolving earlier can never be slower than waiting for full caps.
            Assert.That(rows[0].largeSec, Is.LessThanOrEqualTo(rows[2].largeSec));
            // An adaptive buyer can spend the extra Growth on another upgrade and delay Evolution.
            // Compare the vitality benefit under identical purchase choices, rather than conflating policy with production.
            Assert.That(FixedPurchaseProgress(rules, true), Is.LessThanOrEqualTo(FixedPurchaseProgress(rules, false)),
                "With identical purchases, the vitality bonus must not delay Evolution.");
        }

        private static double FixedPurchaseProgress(PrototypeRules rules, bool active)
        {
            var state = GameStateFactory.CreateNew(rules);
            var vitality = new VitalitySystem(state, rules);
            var resources = new ResourceSystem(state, rules, vitality);
            var evolution = new EvolutionSystem(state, rules, resources);
            var upgrades = new UpgradeSystem(state, rules, resources);
            // Both runs buy Root L1/L2 and Leaves L1/L2, then reserve the same resources for both Evolutions.
            for (int seconds = 0; seconds < 86400; seconds++)
            {
                if (active) for (int tap = 0; tap < 3; tap++) vitality.TryTap();
                if (state.run.levels.root < 2) upgrades.TryUpgrade("root");
                if (state.run.levels.leaves < 2) upgrades.TryUpgrade("leaves");
                if(state.run.levels.trunk<2)upgrades.TryUpgrade("trunk");
                if (state.run.levels.root == 2 && state.run.levels.leaves == 2 && state.run.levels.trunk==2 && evolution.TryEvolve().Success)
                    evolution.CompleteTransition();
                if (state.run.stageId == "large_tree") return seconds;
                resources.Tick(1);
            }
            Assert.Fail("Fixed purchase route did not reach large_tree in 24 hours.");
            return double.PositiveInfinity;
        }

        [Test] public void ThunderstormDeathRisk_ProtectionLowersRisk_DeathIsRareButPossible()
        {
            var rules = Rules();
            var results = new[] { DeathStats(rules, 0, 200), DeathStats(rules, 5, 200), DeathStats(rules, 10, 200) };
            report.death.AddRange(results);
            foreach (var r in results) Debug.Log("[T09 metrics] " + JsonUtility.ToJson(r));
            // Under the confirmed rule, Protection reduces Burning damagePerSecond = base / (1 + Protection).
            // Higher trunk protection means each strike deals less damage, requiring more strikes to deplete maxHp.
            foreach (var r in results)
            {
                double dps = rules.CalculateBurningDamagePerSecond(r.protection);
                double expectedStrikes = rules.maxHp / (rules.burningDurationSeconds * dps);
                Assert.That(r.meanStrikes, Is.EqualTo(expectedStrikes).Within(1.0), "trunk " + r.trunkLevel);
            }
            Assert.That(results[1].medianHours, Is.GreaterThan(results[0].medianHours));
            Assert.That(results[2].medianHours, Is.GreaterThan(results[1].medianHours), "More Protection must delay death.");
            Assert.That(results[0].medianHours, Is.GreaterThan(0.5), "Death must not be a routine event within the first half hour at large tree.");
        }

        [Test] public void OfflineBankMatchesConfirmedRule24HoursAt50Percent()
        {
            var rules = Rules();
            var state = GameStateFactory.CreateNew(rules);
            state.run.levels.root = 4; state.run.levels.leaves = 2; state.run.stageId = "seedling"; state.run.water = 0;
            var reward = OfflineProduction.Evaluate(state, rules, 1000, 1000 + 3 * 86400);
            Assert.That(reward.ConsideredSeconds, Is.EqualTo(86400));
            // supply 4/s, demand 0.5/s => 3.5/s net over 43200 effective seconds; growth 2/s over 43200s.
            Assert.That(reward.WaterDelta, Is.EqualTo(3.5 * 43200).Within(1e-6));
            Assert.That(reward.PhotosynthesisDelta, Is.EqualTo(2 * 43200).Within(1e-6));
            report.offlineWaterAt24hBank = reward.WaterDelta;
        }

        [OneTimeTearDown] public void Write()
        {
            string dir = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Evidence",
                report.configVersion == "prototype-6" ? "UpgradeCurrency_20261009" : "T09"));
            Directory.CreateDirectory(dir);
            File.WriteAllText(Path.Combine(dir, "progression_metrics.json"), JsonUtility.ToJson(report, true));
        }
    }
}

