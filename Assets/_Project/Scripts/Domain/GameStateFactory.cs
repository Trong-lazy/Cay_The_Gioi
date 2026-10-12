using System;

namespace CayTheGioi.Domain
{
    public static class GameStateFactory
    {
        public static GameState CreateNew(PrototypeRules rules)
        {
            if (rules == null) throw new ArgumentNullException(nameof(rules));
            var errors = rules.Validate();
            if (errors.Count > 0) throw new ArgumentException(string.Join("\n", errors), nameof(rules));
            var state = new GameState();
            state.run.runId = Guid.NewGuid().ToString("N");
            state.run.stageId = rules.initialStageId;
            state.run.water = rules.initialWater;
            state.run.photosynthesis = rules.initialPhotosynthesis;
            state.run.growth = rules.initialGrowth;
            state.run.hp = rules.maxHp;
            state.run.weather.remainingOnlineSeconds = rules.initialCalmSeconds;
            state.permanent.highestStageEverId = rules.initialStageId;
            // Zero-valued wallets make stable IDs explicit without granting any point.
            foreach (var stage in rules.stages)
                state.permanent.stagePoints.Add(new StagePointEntry { stageId = stage.stageId, points = 0 });
            return state;
        }
    }
}
