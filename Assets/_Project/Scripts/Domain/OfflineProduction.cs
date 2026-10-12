using System;

namespace CayTheGioi.Domain
{
    public sealed class OfflineReward
    {
        public readonly double ElapsedSeconds, ConsideredSeconds, Water, Photosynthesis, Growth, Spirit, WaterDelta, PhotosynthesisDelta, GrowthDelta, SpiritDelta, MaxSeconds, RateFactor;
        public readonly long CheckpointUtcSeconds;
        public readonly bool Dead, Capped;
        public OfflineReward(double elapsed, double considered, long checkpoint, ProductionResult result, RunState source, bool capped, PrototypeRules rules)
        {
            MaxSeconds = rules.offlineMaxSeconds; RateFactor = rules.offlineRateFactor;
            ElapsedSeconds = elapsed; ConsideredSeconds = considered; CheckpointUtcSeconds = checkpoint;
            Water = result.Water; Photosynthesis=result.Photosynthesis; Growth = result.Growth; Spirit=result.Spirit;
            WaterDelta = Water - source.water; PhotosynthesisDelta=Photosynthesis-source.photosynthesis; GrowthDelta = Growth - source.growth; SpiritDelta=Spirit-source.spirit;
            Dead = source.life == "DEAD"; Capped = capped;
        }
    }

    // Pure neutral economy: effective duration applies 50% once to supply, demand and output.
    // Never ticks vitality, weather, health, input, Evolution or permanent transactions.
    public static class OfflineProduction
    {
        public static OfflineReward Evaluate(GameState state, PrototypeRules rules, long checkpoint, long now)
        {
            if (state == null || state.run == null || rules == null || checkpoint < 0 ||
                !ProductionSystem.Finite(rules.offlineMaxSeconds) || rules.offlineMaxSeconds < 0 ||
                !ProductionSystem.Finite(rules.offlineRateFactor) || rules.offlineRateFactor < 0 || rules.offlineRateFactor > 1)
                throw new ArgumentException("Invalid offline source or config.");
            now = Math.Max(0, now);
            double elapsed = now > checkpoint ? (double)now - checkpoint : 0;
            double considered = Math.Min(elapsed, rules.offlineMaxSeconds);
            var production = ProductionSystem.Evaluate(state.run, rules, considered * rules.offlineRateFactor, PermanentEffects.Evaluate(state.permanent, rules));
            return new OfflineReward(elapsed, considered, Math.Max(checkpoint, now), production, state.run, elapsed > rules.offlineMaxSeconds, rules);
        }
    }
}
