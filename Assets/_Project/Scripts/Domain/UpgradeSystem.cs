using System;

namespace CayTheGioi.Domain
{
    public readonly struct UpgradeQuote
    {
        public readonly string BranchId, CostResourceId, Reason;
        public readonly int Level, Cap;
        public readonly double Cost, CurrentOutput, NextOutput, CurrentWaterConsumption, NextWaterConsumption;
        public readonly bool IsUnlock;
        public readonly double UnlockWaterCost, UnlockPhotosynthesisCost, UnlockGrowthCost;
        public bool CanUpgrade => Reason == "";
        public UpgradeQuote(string id, int level, int cap, double cost, double output, double next, double consumption, double nextConsumption, string reason, string costResourceId = null, bool isUnlock=false, double unlockWater=0, double unlockPhoto=0, double unlockGrowth=0)
        { IsUnlock=isUnlock; UnlockWaterCost=unlockWater; UnlockPhotosynthesisCost=unlockPhoto; UnlockGrowthCost=unlockGrowth; BranchId = id; CostResourceId = costResourceId; Level = level; Cap = cap; Cost = cost; CurrentOutput = output; NextOutput = next; CurrentWaterConsumption = consumption; NextWaterConsumption = nextConsumption; Reason = reason; }
    }

    public sealed class UpgradeSystem
    {
        private readonly GameState state;
        private readonly PrototypeRules rules;
        private readonly ResourceSystem resources;
        private bool executing;
        private readonly Func<bool> busy;
        public event Action<string> Completed;
        public UpgradeSystem(GameState state, PrototypeRules rules, ResourceSystem resources, Func<bool> busy = null)
        { this.state = state; this.rules = rules; this.resources = resources; this.busy = busy; }

        public static bool TryCalculateCost(double baseCost, double factor, int level, out double cost)
        {
            cost = 0;
            if (level < 0 || !ProductionSystem.Finite(baseCost) || baseCost <= 0 || !ProductionSystem.Finite(factor) || factor <= 1) return false;
            double candidate = Math.Ceiling(baseCost * Math.Pow(factor, level));
            if (!ProductionSystem.Finite(candidate) || candidate <= 0) return false;
            cost = candidate; return true;
        }
        public UpgradeQuote GetQuote(string branchId)
        {
            var stage = rules.FindStage(state.run.stageId);
            int level, cap; double baseCost, outputPerLevel, consumptionPerLevel = 0;
            switch (branchId)
            {
                case "root": level = state.run.levels.root; cap = stage?.rootCap ?? 0; baseCost = rules.rootBaseCost; outputPerLevel = rules.rootWaterPerLevelSecond; break;
                case "leaves": level = state.run.levels.leaves; cap = stage?.leavesCap ?? 0; baseCost = rules.leavesBaseCost; outputPerLevel = rules.leavesPhotosynthesisPerLevelSecond; consumptionPerLevel = rules.leavesWaterPerLevelSecond; break;
                case "trunk": level = state.run.levels.trunk; cap = stage?.trunkCap ?? 0; baseCost = rules.trunkBaseCost; outputPerLevel = rules.trunkGrowthPerLevelSecond; consumptionPerLevel=rules.trunkPhotosynthesisPerLevelSecond; break;
                case "spirit_absorption": level=state.run.levels.spiritAbsorption; cap=stage?.spiritAbsorptionCap??0; baseCost=rules.spiritAbsorptionBaseCost; outputPerLevel=rules.spiritPerLevelSecond; break;
                default: return new UpgradeQuote(branchId, 0, 0, 0, 0, 0, 0, 0, "UNKNOWN_BRANCH");
            }
            var permanent = PermanentEffects.Evaluate(state.permanent, rules);
            if(branchId=="root")outputPerLevel *= permanent.Water;
            if(branchId=="leaves")outputPerLevel *= permanent.Photosynthesis;
            if(branchId=="trunk")outputPerLevel *= permanent.Growth;
            if(branchId=="spirit_absorption")outputPerLevel *= permanent.Spirit;
            double bonusProtection = 0;
            double multiplier = state.run.life == "ALIVE" && state.vitality.bankSeconds > 0 ? rules.vitalityOutputMultiplier : 1;
            int nextLevel = level >= 0 && level < cap ? level + 1 : level;
            double output = level * outputPerLevel * multiplier + bonusProtection, next = nextLevel * outputPerLevel * multiplier + bonusProtection;
            double consumption = level * consumptionPerLevel, nextConsumption = nextLevel * consumptionPerLevel;
            bool validCost = TryCalculateCost(baseCost, rules.upgradeCostFactor, level, out double cost);
            string resourceId = rules.UpgradeResourceId(branchId, level);
            double spendable=state.run.Balance(resourceId);
            bool isUnlock=branchId=="spirit_absorption"&&!state.run.spiritAbsorptionUnlocked;
            double unlockWater=isUnlock?rules.spiritUnlockWaterCost:0, unlockPhoto=isUnlock?rules.spiritUnlockPhotosynthesisCost:0, unlockGrowth=isUnlock?rules.spiritUnlockGrowthCost:0;
            bool unlockInvalid=isUnlock&&(!ProductionSystem.Finite(unlockWater)||unlockWater<=0||!ProductionSystem.Finite(unlockPhoto)||unlockPhoto<=0||!ProductionSystem.Finite(unlockGrowth)||unlockGrowth<=0);
            bool unlockShort=isUnlock&&(state.run.water<unlockWater||state.run.photosynthesis<unlockPhoto||state.run.growth<unlockGrowth);
            bool unlockPrecision=isUnlock&&(state.run.water-unlockWater==state.run.water||state.run.photosynthesis-unlockPhoto==state.run.photosynthesis||state.run.growth-unlockGrowth==state.run.growth);
            string reason = "";
            if (resources.SimulationBlocked || executing || (busy != null && busy())) reason = "BUSY";
            else if (stage == null || level < 0 || level > cap ||
                (state.run.spiritAbsorptionUnlocked && (state.run.levels.spiritAbsorption<1 || stage.spiritAbsorptionCap==0)) ||
                (!state.run.spiritAbsorptionUnlocked && (state.run.spirit!=0 || state.run.levels.spiritAbsorption!=0)) || !ProductionSystem.Finite(state.run.water) || state.run.water < 0 ||
                !ProductionSystem.Finite(state.run.growth) || state.run.growth < 0 ||
                !ProductionSystem.Finite(state.run.photosynthesis)||state.run.photosynthesis<0||!ProductionSystem.Finite(state.run.spirit)||state.run.spirit<0||
                !ProductionSystem.Finite(output) || !ProductionSystem.Finite(next) || output < 0 || next < 0 ||
                !ProductionSystem.Finite(consumption) || !ProductionSystem.Finite(nextConsumption) || consumption < 0)
                reason = "INVALID_STATE";
            else if (state.run.life != "ALIVE") reason = "DEAD";
            else if (cap == 0) reason = "LOCKED";
            else if (level == cap) reason = "CAP_REACHED";
            else if (!validCost) reason = "INVALID_COST";
            else if (!PrototypeRules.IsUpgradeResourceId(resourceId)) reason = "INVALID_COST_RESOURCE";
            else if (unlockInvalid) reason="INVALID_COST";
            else if (unlockShort) reason="INSUFFICIENT_RESOURCE";
            else if (unlockPrecision) reason="NUMERIC_PRECISION";
            else if (!isUnlock && spendable < cost) reason = "INSUFFICIENT_RESOURCE";
            // Do not grant a free level when a balance is too large to represent its deduction.
            else if (!isUnlock && spendable - cost == spendable) reason = "NUMERIC_PRECISION";
            return new UpgradeQuote(branchId, level, cap, cost, output, next, consumption, nextConsumption, reason, resourceId, isUnlock, unlockWater, unlockPhoto, unlockGrowth);
        }

        public double Protection => state.run.levels.trunk * rules.trunkProtectionPerLevel + PermanentEffects.Evaluate(state.permanent, rules).Protection;

        public CommandResult TryUpgrade(string branchId)
        {
            if (executing) return new CommandResult(false, "BUSY");
            // Always re-quote from live state; UI labels never authorize a transaction.
            var quote = GetQuote(branchId);
            if (!quote.CanUpgrade) return new CommandResult(false, quote.Reason);
            double balance=state.run.Balance(quote.CostResourceId)-(quote.IsUnlock?0:quote.Cost);
            int level = quote.Level + 1;
            if (!ProductionSystem.Finite(balance) || balance < 0) return new CommandResult(false, "INVALID_STATE");
            executing = true;
            try
            {
                if(quote.IsUnlock)
                {
                    state.run.water-=quote.UnlockWaterCost; state.run.photosynthesis-=quote.UnlockPhotosynthesisCost; state.run.growth-=quote.UnlockGrowthCost;
                    state.run.spiritAbsorptionUnlocked=true;
                }
                else state.run.SetBalance(quote.CostResourceId,balance);
                switch (branchId)
                {
                    case "root": state.run.levels.root = level; break;
                    case "leaves": state.run.levels.leaves = level; break;
                    case "trunk": state.run.levels.trunk = level; break;
                    case "spirit_absorption":state.run.levels.spiritAbsorption=level;break;
                }
                resources.NotifyChanged();
                Completed?.Invoke(branchId);
                return new CommandResult(true);
            }
            finally { executing = false; }
        }
    }
}
