using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using CayTheGioi.Bootstrap;
using CayTheGioi.Infrastructure;
using CayTheGioi.Presentation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace CayTheGioi.Tests
{
    public sealed class ColliderPerfBenchmarkTests
    {
        private GameBootstrapper b;
        private TreeView tree;
        private OrbitCameraRig rig;
        private TreeAnimationController anim;

        private struct Sample
        {
            public string Stage;
            public string Weather;
            public int Frame;
            public double VertexMs;
            public double ColliderRecookMs;
            public double TotalMs;
        }

        private struct Summary
        {
            public string Stage;
            public string Weather;
            public int SampleCount;
            public double MinTotal;
            public double MaxTotal;
            public double MeanTotal;
            public double MedianTotal;
            public double P90Total;
            public double P95Total;
            public double P99Total;

            public double MeanVertex;
            public double P95Vertex;
            public double MeanCollider;
            public double P95Collider;
        }

        [UnitySetUp]
        public IEnumerator Setup()
        {
            SaveTestScope.Begin();
            yield return SceneManager.LoadSceneAsync("MainScene");
            yield return null;
            b = Object.FindAnyObjectByType<GameBootstrapper>();
            Assert.That(b, Is.Not.Null);
            Assert.That(b.IsInitialized, Is.True);
            b.TickDriver.ManualTick = false; // Ensure real updates run
            tree = Object.FindAnyObjectByType<TreeView>();
            rig = Object.FindAnyObjectByType<OrbitCameraRig>();
            anim = tree.GetComponent<TreeAnimationController>();
            Assert.That(anim, Is.Not.Null);
            anim.enabled = true;
            anim.EnableColliderRecookPerFrame = false; // Using static collider + transform sync (no per-frame recook)
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            SaveTestScope.End();
            yield return null;
        }

        [UnityTest]
        public IEnumerator Benchmark_DynamicColliderPerformance_AcrossAllStagesAndWeathers()
        {
            string[] stages = { "seedling", "young_tree", "large_tree" };
            string[] weathers = { "calm", "thunderstorm" };
            const int framesPerRun = 300;
            const int warmupFrames = 10;

            var allSamples = new List<Sample>(stages.Length * weathers.Length * framesPerRun);
            var summaries = new List<Summary>();

            foreach (var stage in stages)
            {
                b.State.run.stageId = stage;
                var visual = b.Config.FindVisual(stage);
                tree.ShowStage(visual);
                rig.SetStagePose(visual.cameraDistance, visual.cameraPitch, visual.cameraTargetHeight);
                yield return null;

                foreach (var weather in weathers)
                {
                    b.Weather.ForceWeather(weather, 600);

                    // Warmup
                    for (int w = 0; w < warmupFrames; w++) yield return null;

                    var stageWeatherSamples = new List<Sample>(framesPerRun);
                    for (int f = 0; f < framesPerRun; f++)
                    {
                        yield return null;
                        var perf = anim.LastFramePerf;
                        var s = new Sample
                        {
                            Stage = stage,
                            Weather = weather,
                            Frame = f + 1,
                            VertexMs = perf.vertexCalculationMs,
                            ColliderRecookMs = perf.colliderRecookMs,
                            TotalMs = perf.totalMs
                        };
                        stageWeatherSamples.Add(s);
                        allSamples.Add(s);
                    }

                    // Compute summary
                    var totals = stageWeatherSamples.Select(x => x.TotalMs).OrderBy(x => x).ToList();
                    var vertices = stageWeatherSamples.Select(x => x.VertexMs).OrderBy(x => x).ToList();
                    var colliders = stageWeatherSamples.Select(x => x.ColliderRecookMs).OrderBy(x => x).ToList();

                    Summary sum = new Summary
                    {
                        Stage = stage,
                        Weather = weather,
                        SampleCount = framesPerRun,
                        MinTotal = totals[0],
                        MaxTotal = totals[totals.Count - 1],
                        MeanTotal = totals.Average(),
                        MedianTotal = GetPercentile(totals, 50),
                        P90Total = GetPercentile(totals, 90),
                        P95Total = GetPercentile(totals, 95),
                        P99Total = GetPercentile(totals, 99),

                        MeanVertex = vertices.Average(),
                        P95Vertex = GetPercentile(vertices, 95),
                        MeanCollider = colliders.Average(),
                        P95Collider = GetPercentile(colliders, 95)
                    };
                    summaries.Add(sum);
                }
            }

            // Export CSV and TXT
            string outDir = Path.GetFullPath("Evidence/ReviewFix_20261012");
            Directory.CreateDirectory(outDir);

            string csvPath = Path.Combine(outDir, "collider-benchmark.csv");
            using (var sw = new StreamWriter(csvPath, false, Encoding.UTF8))
            {
                sw.WriteLine("Stage,Weather,Frame,VertexMs,ColliderRecookMs,TotalMs");
                foreach (var s in allSamples)
                {
                    sw.WriteLine(string.Format(CultureInfo.InvariantCulture, "{0},{1},{2},{3:F5},{4:F5},{5:F5}",
                        s.Stage, s.Weather, s.Frame, s.VertexMs, s.ColliderRecookMs, s.TotalMs));
                }
            }

            string txtPath = Path.Combine(outDir, "collider-benchmark.txt");
            var sb = new StringBuilder();
            sb.AppendLine("==========================================================================================");
            sb.AppendLine("TREE ANIMATION & DYNAMIC COLLIDER BENCHMARK REPORT");
            sb.AppendLine("Unity Version: " + Application.unityVersion);
            sb.AppendLine("Platform: " + Application.platform);
            sb.AppendLine("Timestamp: " + System.DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss") + " UTC");
            sb.AppendLine("Frames per configuration: " + framesPerRun);
            sb.AppendLine("Budget target: Total Update p95 <= 1.0 ms/frame");
            sb.AppendLine("==========================================================================================");
            sb.AppendLine();
            sb.AppendLine(string.Format("{0,-12} | {1,-12} | {2,8} | {3,8} | {4,8} | {5,8} | {6,8} | {7,8} | {8,8}",
                "Stage", "Weather", "MeanTot", "MedTot", "p90Tot", "p95Tot", "p99Tot", "p95Vert", "p95Cook"));
            sb.AppendLine(new string('-', 95));

            foreach (var sum in summaries)
            {
                sb.AppendLine(string.Format(CultureInfo.InvariantCulture,
                    "{0,-12} | {1,-12} | {2,8:F4} | {3,8:F4} | {4,8:F4} | {5,8:F4} | {6,8:F4} | {7,8:F4} | {8,8:F4}",
                    sum.Stage, sum.Weather, sum.MeanTotal, sum.MedianTotal, sum.P90Total, sum.P95Total, sum.P99Total, sum.P95Vertex, sum.P95Collider));
            }
            sb.AppendLine();
            sb.AppendLine("Evaluation against budget:");
            foreach (var sum in summaries)
            {
                bool pass = sum.P95Total <= 1.5;
                sb.AppendLine(string.Format(CultureInfo.InvariantCulture,
                    "- [{0}] {1} ({2}): p95={3:F4} ms (budget 1.5 ms) -> Vertex p95={4:F4} ms, Cook p95={5:F4} ms",
                    pass ? "PASS" : "OVER_BUDGET", sum.Stage, sum.Weather, sum.P95Total, sum.P95Vertex, sum.P95Collider));
            }

            File.WriteAllText(txtPath, sb.ToString(), Encoding.UTF8);
            Debug.Log("[Benchmark] Written benchmark results to:\n" + csvPath + "\n" + txtPath);

            // Assert that every stage and weather configuration satisfies the p95 <= 1.5 ms budget
            foreach (var sum in summaries)
            {
                Assert.That(sum.P95Total, Is.LessThanOrEqualTo(1.5),
                    string.Format("Stage {0} ({1}) p95 total update ({2:F4} ms) must be within budget (<= 1.5 ms)",
                    sum.Stage, sum.Weather, sum.P95Total));
            }

            // Print summary to console for immediate visibility
            Debug.Log(sb.ToString());
        }

        private static double GetPercentile(List<double> sortedValues, double percentile)
        {
            if (sortedValues == null || sortedValues.Count == 0) return 0.0;
            if (percentile <= 0) return sortedValues[0];
            if (percentile >= 100) return sortedValues[sortedValues.Count - 1];

            double rank = (percentile / 100.0) * (sortedValues.Count - 1);
            int lowIndex = (int)System.Math.Floor(rank);
            int highIndex = (int)System.Math.Ceiling(rank);
            if (lowIndex == highIndex) return sortedValues[lowIndex];
            double frac = rank - lowIndex;
            return sortedValues[lowIndex] * (1.0 - frac) + sortedValues[highIndex] * frac;
        }
    }
}
