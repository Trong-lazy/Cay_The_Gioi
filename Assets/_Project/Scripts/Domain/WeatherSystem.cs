using System;
using System.Collections.Generic;

namespace CayTheGioi.Domain
{
    public sealed class WeatherSystem
    {
        private readonly GameState state;
        private readonly PrototypeRules rules;
        private readonly Func<double> rng01;
        private readonly Func<int, int> rngInt;
        private readonly Random fallbackRandom = new Random();

        public event Action<string> WeatherChanged;
        public event Action<string> LightningStruck;
        public event Action StatusChanged;

        public string CurrentWeatherId => state.run.weather.weatherId;
        public double RemainingSeconds => state.run.weather.remainingOnlineSeconds;
        public int BadStreak => state.run.weather.badStreak;
        public double IncidentCooldown => state.run.weather.incidentCooldownOnlineSeconds;
        public bool StrikeEvaluated => state.run.weather.strikeEvaluated;
        public bool StrikeWillHit => state.run.weather.strikeWillHit;
        public double StrikeDelaySeconds => state.run.weather.strikeDelaySeconds;
        public bool StrikeExecuted => state.run.weather.strikeExecuted;

        public WeatherSystem(GameState state, PrototypeRules rules, Func<double> rng01 = null, Func<int, int> rngInt = null)
        {
            this.state = state ?? throw new ArgumentNullException(nameof(state));
            this.rules = rules ?? throw new ArgumentNullException(nameof(rules));
            this.rng01 = rng01;
            this.rngInt = rngInt;
            EnsureStrikeEvaluated();
        }

        private double NextDouble() => rng01 != null ? rng01() : fallbackRandom.NextDouble();
        private int NextInt(int maxExclusive) => rngInt != null ? rngInt(maxExclusive) : fallbackRandom.Next(maxExclusive);

        public void EnsureStrikeEvaluated()
        {
            var w = state.run.weather;
            if (w.weatherId == "thunderstorm" && !w.strikeEvaluated)
            {
                var stage = rules.FindStage(state.run.stageId);
                w.strikeEvaluated = true;
                w.strikeExecuted = false;
                bool eligible = stage != null && stage.harmfulWeatherUnlocked && stage.exposure > 0;
                if (eligible)
                {
                    w.strikeWillHit = NextDouble() < rules.thunderstormStrikeProbability;
                    if (w.strikeWillHit)
                    {
                        double fraction = rules.strikeDelayFraction > 0 ? rules.strikeDelayFraction : 0.3;
                        w.strikeDelaySeconds = Math.Max(1.0, w.remainingOnlineSeconds * fraction);
                    }
                    else
                    {
                        w.strikeDelaySeconds = 0;
                    }
                }
                else
                {
                    w.strikeWillHit = false;
                    w.strikeDelaySeconds = 0;
                }
            }
        }

        public void Tick(double seconds)
            => Tick(seconds, null, null);

        // Advance all online systems before dispatching events at the end of each segment.
        // A weather-only caller can omit the callbacks; online gameplay supplies both.
        public void Tick(double seconds, Action<double> advanceSystems, Func<double> nextSystemBoundary)
        {
            if (!ProductionSystem.Finite(seconds) || seconds <= 0) return;
            if (state.run.life != "ALIVE") return;

            while (seconds > 0 && state.run.life == "ALIVE")
            {
                var w = state.run.weather;
                if (w.remainingOnlineSeconds <= 1e-9)
                {
                    AdvanceWeather();
                    continue;
                }
                if (w.weatherId == "thunderstorm" && w.strikeWillHit && !w.strikeExecuted && w.strikeDelaySeconds <= 1e-9)
                {
                    TriggerStrike();
                    continue;
                }
                double nextEvent = w.remainingOnlineSeconds;
                if (w.weatherId == "thunderstorm" && w.strikeWillHit && !w.strikeExecuted)
                {
                    if (w.strikeDelaySeconds < nextEvent)
                    {
                        nextEvent = Math.Max(0.0, w.strikeDelaySeconds);
                    }
                }

                double step = Math.Min(seconds, nextEvent);
                if (nextSystemBoundary != null) step = Math.Min(step, nextSystemBoundary());
                if (!ProductionSystem.Finite(step) || step <= 0)
                    throw new InvalidOperationException("Online simulation must advance a positive finite segment.");

                if (w.incidentCooldownOnlineSeconds > 0)
                    w.incidentCooldownOnlineSeconds = Math.Max(0, w.incidentCooldownOnlineSeconds - step);

                w.remainingOnlineSeconds -= step;
                if (w.weatherId == "thunderstorm" && w.strikeWillHit && !w.strikeExecuted)
                {
                    w.strikeDelaySeconds -= step;
                }
                advanceSystems?.Invoke(step);
                seconds -= step;
                if (state.run.life != "ALIVE") break;
                if (w.weatherId == "thunderstorm" && w.strikeWillHit && !w.strikeExecuted && w.strikeDelaySeconds <= 1e-9)
                {
                    TriggerStrike();
                }
                if (w.remainingOnlineSeconds <= 1e-9) AdvanceWeather();
            }
        }

        public double CalculateStrikeProbability(double dt = 1.0, double trunkProtection = 0, double exposure = 1.0)
        {
            if (exposure <= 0 || dt <= 0) return 0;
            // 10% per thunderstorm spell when eligible; protection does not alter strike probability
            return rules.thunderstormStrikeProbability;
        }

        public void TriggerStrike(string forcedRegion = null)
        {
            string region = forcedRegion;
            if (string.IsNullOrEmpty(region))
            {
                string[] regions = { "root", "leaves", "trunk" };
                region = regions[NextInt(regions.Length)];
            }

            state.run.weather.incidentCooldownOnlineSeconds = rules.strikeCooldownOnlineSeconds;
            state.run.weather.strikeExecuted = true;
            state.run.status.statusId = "burning";
            state.run.status.remainingOnlineSeconds = rules.burningDurationSeconds;
            state.run.status.impactRegionId = region;

            LightningStruck?.Invoke(region);
            StatusChanged?.Invoke();
        }

        public void AdvanceWeather()
        {
            var w = state.run.weather;
            string previous = w.weatherId;
            var stage = rules.FindStage(state.run.stageId);

            string nextWeather;
            double nextDuration;

            if (previous == "thunderstorm")
            {
                // Rule: After bad weather, must transition to calm. Max 1 bad weather in a row.
                nextWeather = "calm";
                nextDuration = rules.initialCalmSeconds;
                w.badStreak = 0;
            }
            else
            {
                if (stage == null || !stage.weatherUnlocked)
                {
                    nextWeather = "calm";
                    nextDuration = rules.initialCalmSeconds;
                    w.badStreak = 0;
                }
                else
                {
                    var candidates = new List<string> { "calm", "sunny", "rainy" };
                    if (stage.harmfulWeatherUnlocked && w.badStreak == 0)
                    {
                        candidates.Add("thunderstorm");
                    }

                    // Rule: Cannot repeat the same non-calm weather twice in a row.
                    if (previous != "calm")
                    {
                        candidates.Remove(previous);
                    }

                    if (candidates.Count == 0)
                    {
                        nextWeather = "calm";
                    }
                    else
                    {
                        int index = NextInt(candidates.Count);
                        nextWeather = candidates[index];
                    }

                    if (nextWeather == "calm")
                    {
                        nextDuration = rules.initialCalmSeconds;
                        w.badStreak = 0;
                    }
                    else
                    {
                        double range = rules.maxWeatherDurationSeconds - rules.minWeatherDurationSeconds;
                        nextDuration = rules.minWeatherDurationSeconds + NextDouble() * range;
                        w.badStreak = (nextWeather == "thunderstorm") ? 1 : 0;
                    }
                }
            }

            ApplyNewWeather(nextWeather, nextDuration);
        }

        public void ForceWeather(string weatherId, double duration = 0)
        {
            if (weatherId != "calm" && weatherId != "sunny" && weatherId != "rainy" && weatherId != "thunderstorm")
                throw new ArgumentException("Unknown weatherId: " + weatherId);

            double d = duration > 0 ? duration : (weatherId == "calm" ? rules.initialCalmSeconds : rules.minWeatherDurationSeconds);
            ApplyNewWeather(weatherId, d);
        }

        private void ApplyNewWeather(string nextWeather, double nextDuration)
        {
            var w = state.run.weather;
            w.weatherId = nextWeather;
            w.remainingOnlineSeconds = nextDuration;
            w.badStreak = (nextWeather == "thunderstorm") ? 1 : 0;

            if (w.recentWeatherIds == null) w.recentWeatherIds = new List<string>();
            w.recentWeatherIds.Add(nextWeather);
            if (w.recentWeatherIds.Count > 10) w.recentWeatherIds.RemoveAt(0);

            if (nextWeather == "thunderstorm")
            {
                var stage = rules.FindStage(state.run.stageId);
                w.strikeEvaluated = true;
                w.strikeExecuted = false;
                bool eligible = stage != null && stage.harmfulWeatherUnlocked && stage.exposure > 0;
                if (eligible)
                {
                    w.strikeWillHit = NextDouble() < rules.thunderstormStrikeProbability;
                    if (w.strikeWillHit)
                    {
                        double fraction = rules.strikeDelayFraction > 0 ? rules.strikeDelayFraction : 0.3;
                        w.strikeDelaySeconds = nextDuration * fraction;
                    }
                    else
                    {
                        w.strikeDelaySeconds = 0;
                    }
                }
                else
                {
                    w.strikeWillHit = false;
                    w.strikeDelaySeconds = 0;
                }
            }
            else
            {
                w.strikeEvaluated = false;
                w.strikeWillHit = false;
                w.strikeDelaySeconds = 0;
                w.strikeExecuted = false;
            }

            // D06 Rule: Entering calm removes burning, but does not heal HP.
            if (nextWeather == "calm" && state.run.status.statusId == "burning")
            {
                ClearStatus();
            }

            WeatherChanged?.Invoke(nextWeather);
        }

        public void ClearStatus()
        {
            state.run.status.statusId = "none";
            state.run.status.remainingOnlineSeconds = 0;
            state.run.status.impactRegionId = "none";
            StatusChanged?.Invoke();
        }

        public void ResetForRebirth()
        {
            var w = state.run.weather;
            w.weatherId = "calm";
            w.remainingOnlineSeconds = rules.initialCalmSeconds;
            w.badStreak = 0;
            w.incidentCooldownOnlineSeconds = 0;
            w.strikeEvaluated = false;
            w.strikeWillHit = false;
            w.strikeDelaySeconds = 0;
            w.strikeExecuted = false;
            if (w.recentWeatherIds == null) w.recentWeatherIds = new List<string>();
            w.recentWeatherIds.Clear();
            w.recentWeatherIds.Add("calm");
            ClearStatus();
            WeatherChanged?.Invoke("calm");
        }

        public static string GetWeatherDisplayName(string weatherId)
        {
            switch (weatherId)
            {
                case "sunny": return "Nắng";
                case "rainy": return "Mưa";
                case "thunderstorm": return "Giông";
                case "calm":
                default:
                    return "Bình yên";
            }
        }
    }
}
