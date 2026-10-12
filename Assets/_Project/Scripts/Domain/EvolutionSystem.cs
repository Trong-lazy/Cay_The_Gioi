using System;

namespace CayTheGioi.Domain
{
    public readonly struct EvolutionQuote
    {
        public readonly string CurrentId, NextId, CurrentName, NextName, Reason;
        public readonly double WaterCost, PhotosynthesisCost, GrowthCost;
        public bool CanEvolve => Reason == "";
        public EvolutionQuote(StageDefinition current, StageDefinition next, string reason)
        {
            CurrentId = current?.stageId; NextId = next?.stageId;
            CurrentName = current?.displayName; NextName = next?.displayName;
            WaterCost = next?.evolutionWaterCost ?? 0; PhotosynthesisCost=next?.evolutionPhotosynthesisCost??0; GrowthCost = next?.evolutionGrowthCost ?? 0; Reason = reason;
        }
    }

    // Transition is transient command state, never saved as part of GameState.
    public sealed class EvolutionSystem
    {
        private readonly GameState state;
        private readonly PrototypeRules rules;
        private readonly ResourceSystem resources;
        private readonly Func<bool> busy;
        public bool IsTransitioning { get; private set; }
        public event Action<string> StageChanged;
        public event Action TransitionCompleted;
        public EvolutionSystem(GameState state, PrototypeRules rules, ResourceSystem resources, Func<bool> busy = null)
        { this.busy=busy; this.state = state; this.rules = rules; this.resources = resources; }
        public EvolutionQuote GetQuote()
        {
            var current = rules.FindStage(state.run.stageId);
            var next = rules.FindStage(current?.nextStageId);
            string reason = "";
            if (IsTransitioning || resources.SimulationBlocked || (busy != null && busy())) reason = "BUSY";
            else if (current == null || !ValidBalance(state.run.water) || !ValidBalance(state.run.growth) || !ValidBalance(state.run.photosynthesis) ||
                state.run.levels.root < 0 || state.run.levels.root > current.rootCap ||
                state.run.levels.leaves < 0 || state.run.levels.leaves > current.leavesCap ||
                state.run.levels.trunk < 0 || state.run.levels.trunk > current.trunkCap) reason = "INVALID_STATE";
            else if (state.run.life != "ALIVE") reason = "DEAD";
            else if (string.IsNullOrEmpty(current.nextStageId)) reason = "FINAL_STAGE";
            else if (next == null || !ValidBalance(next.evolutionWaterCost) || !ValidBalance(next.evolutionGrowthCost) || !ValidBalance(next.evolutionPhotosynthesisCost) ||
                next.rootCap < current.rootCap || next.leavesCap < current.leavesCap || next.trunkCap < current.trunkCap) reason = "INVALID_CONFIG";
            else if (state.run.water < next.evolutionWaterCost || state.run.growth < next.evolutionGrowthCost || state.run.photosynthesis < next.evolutionPhotosynthesisCost) reason = "INSUFFICIENT_RESOURCE";
            else if ((next.evolutionWaterCost > 0 && state.run.water - next.evolutionWaterCost == state.run.water) ||
                (next.evolutionPhotosynthesisCost>0 && state.run.photosynthesis-next.evolutionPhotosynthesisCost==state.run.photosynthesis) ||
                (next.evolutionGrowthCost > 0 && state.run.growth - next.evolutionGrowthCost == state.run.growth)) reason = "NUMERIC_PRECISION";
            return new EvolutionQuote(current, next, reason);
        }
        private static bool ValidBalance(double value) => ProductionSystem.Finite(value) && value >= 0;
        public CommandResult TryEvolve()
        {
            var quote = GetQuote();
            if (!quote.CanEvolve) return new CommandResult(false, quote.Reason);
            double water = state.run.water - quote.WaterCost, growth = state.run.growth - quote.GrowthCost;
            IsTransitioning = true;
            state.run.water = water; state.run.photosynthesis-=quote.PhotosynthesisCost; state.run.growth = growth; state.run.stageId = quote.NextId;
            // Preserve a later milestone from a previous run; persist IDs, never list indices.
            bool alreadyHigher = false;
            var milestone = rules.FindStage(quote.NextId);
            for (int i = 0; milestone != null && i < rules.stages.Length; i++)
            {
                if (milestone.stageId == state.permanent.highestStageEverId) { alreadyHigher = true; break; }
                milestone = rules.FindStage(milestone.nextStageId);
            }
            if (!alreadyHigher) state.permanent.highestStageEverId = quote.NextId;
            try { resources.NotifyChanged(); StageChanged?.Invoke(quote.NextId); }
            catch { CompleteTransition(); throw; }
            return new CommandResult(true);
        }
        public void CompleteTransition()
        {
            if (!IsTransitioning) return;
            IsTransitioning = false; TransitionCompleted?.Invoke();
        }
    }
}
