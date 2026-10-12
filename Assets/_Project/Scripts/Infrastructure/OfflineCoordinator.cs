using System;
using CayTheGioi.Domain;

namespace CayTheGioi.Infrastructure
{
    // Evaluates against a frozen source and writes a candidate before exposing its resources.
    public sealed class OfflineCoordinator
    {
        private readonly PrototypeRules rules;
        private readonly SaveService service;
        public OfflineCoordinator(PrototypeRules rules, SaveService service) { this.rules = rules; this.service = service; }
        public SaveResult TryApply(GameState source, string saveId, ref long revision, long checkpoint, out OfflineReward reward)
        {
            reward = null;
            try
            {
                long now = service.UtcNowSeconds();
                var evaluated = OfflineProduction.Evaluate(source, rules, checkpoint, now);
                var candidate = GameStateFactory.CreateNew(rules);
                SaveSerializer.Restore(SaveSerializer.Snapshot(source, rules.configVersion, saveId, revision), candidate);
                candidate.run.water = evaluated.Water; candidate.run.photosynthesis=evaluated.Photosynthesis; candidate.run.growth = evaluated.Growth; candidate.run.spirit=evaluated.Spirit;
                var result = service.Save(candidate, saveId, ref revision, evaluated.CheckpointUtcSeconds);
                if (!result.Success) return SaveResult.Fail("OFFLINE_SAVE_FAILED: " + result.Reason);
                // Keep the shared GameState and its subobjects stable for domain/view consumers.
                source.run.water = candidate.run.water; source.run.photosynthesis=candidate.run.photosynthesis; source.run.growth = candidate.run.growth; source.run.spirit=candidate.run.spirit;
                reward = evaluated;
                return result;
            }
            catch (Exception ex) { return SaveResult.Fail("OFFLINE_FAILED: " + ex.Message); }
        }
    }
}
