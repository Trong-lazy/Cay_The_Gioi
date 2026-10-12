using System;
using CayTheGioi.Domain;

namespace CayTheGioi.Bootstrap
{
    /// <summary>
    /// Deterministic "reasonable player" used only for T09 measurement and build QA. It issues the same
    /// commands as the UI (TryTap, TryUpgrade, TryEvolve); it never edits state directly.
    /// </summary>
    public sealed class QaAutoPlayer
    {
        public enum EvolveMode { Asap, AfterCaps }
        private readonly GameState state;
        private readonly PrototypeRules rules;
        private readonly UpgradeSystem upgrades;
        private readonly EvolutionSystem evolution;
        private readonly VitalitySystem vitality;
        private double tapCarry;
        public EvolveMode Mode = EvolveMode.Asap;
        public double TapsPerSecond;
        public bool AutoCompleteTransition = true;
        public int Purchases, Taps, Evolutions;
        public string LastEvolvedStage;

        public QaAutoPlayer(GameState state, PrototypeRules rules, UpgradeSystem upgrades, EvolutionSystem evolution, VitalitySystem vitality)
        { this.state = state; this.rules = rules; this.upgrades = upgrades; this.evolution = evolution; this.vitality = vitality; }

        /// <summary>One decision step covering dt seconds of play. Returns true when it evolved the tree.</summary>
        public bool Act(double dt)
        {
            if (state.run.life != "ALIVE") return false;
            tapCarry += TapsPerSecond * dt;
            while (tapCarry >= 1) { tapCarry -= 1; if (vitality.TryTap(true).Success) Taps++; }

            var stage = rules.FindStage(state.run.stageId);
            if (stage == null) return false;
            bool atCaps = state.run.levels.root >= stage.rootCap && state.run.levels.leaves >= stage.leavesCap && state.run.levels.trunk >= stage.trunkCap;
            var quote = evolution.GetQuote();
            if (quote.CanEvolve && (Mode == EvolveMode.Asap || atCaps))
            {
                if (evolution.TryEvolve().Success)
                {
                    Evolutions++; LastEvolvedStage = quote.NextId;
                    if (AutoCompleteTransition) evolution.CompleteTransition();
                    return true;
                }
            }
            var next = rules.FindStage(stage.nextStageId);
            // While saving for Evolution (growth already enough) stop spending Water on upgrades.
            bool saving = Mode == EvolveMode.Asap && next != null && state.run.growth >= next.evolutionGrowthCost && state.run.photosynthesis >= next.evolutionPhotosynthesisCost && state.run.levels.root>0 && state.run.levels.leaves>0 && state.run.levels.trunk>0;
            if (saving) return false;
            for (int guard = 0; guard < 64; guard++)
            {
                string branch = NextBranch(stage);
                if (branch == null || !upgrades.TryUpgrade(branch).Success) break;
                Purchases++;
            }
            return false;
        }

        private string NextBranch(StageDefinition stage)
        {
            var l = state.run.levels;
            string best = null; int bestLevel = int.MaxValue;
            void Consider(string id, int level, int cap)
            { if (cap > 0 && level < cap && level < bestLevel) { best = id; bestLevel = level; } }
            Consider("root", l.root, stage.rootCap);
            Consider("leaves", l.leaves, stage.leavesCap);
            Consider("trunk", l.trunk, stage.trunkCap);
            return best;
        }
    }
}
