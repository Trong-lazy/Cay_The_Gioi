using System;

namespace CayTheGioi.Domain
{
    public sealed class ResourceSystem
    {
        private readonly GameState state;
        private readonly PrototypeRules rules;
        private readonly VitalitySystem vitality;
        private readonly Func<bool> transactionBusy;
        public bool SimulationBlocked { get; set; }
        public event Action Changed;
        public void NotifyExternalCommit() => Changed?.Invoke();
        public ResourceSystem(GameState state, PrototypeRules rules, VitalitySystem vitality, Func<bool> transactionBusy = null)
        { this.transactionBusy = transactionBusy; this.state = state; this.rules = rules; this.vitality = vitality; }
        internal void NotifyChanged() => Changed?.Invoke();

        public CommandResult Tick(double seconds)
        {
            if (SimulationBlocked || (transactionBusy != null && transactionBusy())) return new CommandResult(false, "SUSPENDED");
            if (!ProductionSystem.Finite(seconds) || seconds < 0) return new CommandResult(false, "INVALID_TIME");
            if (state.run.life != "ALIVE") return new CommandResult(false, "DEAD");
            double bank = state.vitality.bankSeconds;
            if (!ProductionSystem.Finite(bank) || bank < 0 || bank > rules.vitalityMaxBankSeconds) return new CommandResult(false, "INVALID_STATE");
            if (seconds == 0) return new CommandResult(true);
            double boosted = Math.Min(bank, seconds);
            try
            {
                var permanent = PermanentEffects.Evaluate(state.permanent, rules);
                double weatherWater = state.run.weather != null && state.run.weather.weatherId == "rainy" ? rules.rainyWaterMultiplier : 1;
                double weatherGrowth = state.run.weather != null && state.run.weather.weatherId == "sunny" ? rules.sunnyGrowthMultiplier : 1;
                double burningLeaves = state.run.status != null && state.run.status.statusId == "burning" && state.run.status.impactRegionId == "leaves" ? rules.burningLeavesGrowthMultiplier : 1;

                // Candidate data prevents partial commit if the second segment overflows.
                var candidate = new RunState { life = state.run.life, water = state.run.water, photosynthesis=state.run.photosynthesis, growth = state.run.growth, spirit=state.run.spirit, spiritAbsorptionUnlocked=state.run.spiritAbsorptionUnlocked, levels = state.run.levels };
                var first = ProductionSystem.Evaluate(candidate, rules, boosted, permanent, rules.vitalityOutputMultiplier, weatherWater, weatherGrowth, burningLeaves);
                candidate.water = first.Water; candidate.photosynthesis=first.Photosynthesis; candidate.growth = first.Growth; candidate.spirit=first.Spirit;
                var last = ProductionSystem.Evaluate(candidate, rules, seconds - boosted, permanent, 1, weatherWater, weatherGrowth, burningLeaves);
                state.run.water = last.Water; state.run.photosynthesis=last.Photosynthesis; state.run.growth = last.Growth; state.run.spirit=last.Spirit;
                state.vitality.bankSeconds = bank - boosted;
            }
            catch (ArgumentException) { return new CommandResult(false, "INVALID_PRODUCTION"); }
            Changed?.Invoke();
            if (boosted > 0) vitality.NotifyChanged();
            return new CommandResult(true);
        }

        // Instantaneous sustainable rates at the current balance, not an offline reward estimate.
        public ProductionResult Rates()
        {
            if (state.run.life != "ALIVE") return new ProductionResult(state.run.water, state.run.growth, 0, 0, 0,state.run.photosynthesis,0,0,state.run.spirit);
            double multiplier = state.vitality.bankSeconds > 0 ? rules.vitalityOutputMultiplier : 1;
            var permanent = PermanentEffects.Evaluate(state.permanent, rules);
            double weatherWater = state.run.weather != null && state.run.weather.weatherId == "rainy" ? rules.rainyWaterMultiplier : 1;
            double weatherGrowth = state.run.weather != null && state.run.weather.weatherId == "sunny" ? rules.sunnyGrowthMultiplier : 1;
            double burningLeaves = state.run.status != null && state.run.status.statusId == "burning" && state.run.status.impactRegionId == "leaves" ? rules.burningLeavesGrowthMultiplier : 1;

            double supply = state.run.levels.root * rules.rootWaterPerLevelSecond * permanent.Water * weatherWater * multiplier;
            double demand = state.run.levels.leaves * rules.leavesWaterPerLevelSecond;
            double consumption = state.run.water > 0 ? demand : Math.Min(supply, demand);
            double fraction = demand > 0 ? consumption / demand : 1;
            double photo=state.run.levels.leaves*rules.leavesPhotosynthesisPerLevelSecond*permanent.Photosynthesis*weatherGrowth*burningLeaves*multiplier*fraction;
            double photoDemand=state.run.levels.trunk*rules.trunkPhotosynthesisPerLevelSecond;
            double photoConsumed=state.run.photosynthesis>0?photoDemand:Math.Min(photo,photoDemand);
            double growthFraction=photoDemand>0?photoConsumed/photoDemand:1;
            double growth=state.run.levels.trunk*rules.trunkGrowthPerLevelSecond*permanent.Growth*multiplier*growthFraction;
            double spirit=state.run.spiritAbsorptionUnlocked?state.run.levels.spiritAbsorption*rules.spiritPerLevelSecond*permanent.Spirit*multiplier:0;
            return new ProductionResult(state.run.water,state.run.growth,supply,consumption,growth,state.run.photosynthesis,photo,photoConsumed,state.run.spirit,spirit);
        }
    }
}
