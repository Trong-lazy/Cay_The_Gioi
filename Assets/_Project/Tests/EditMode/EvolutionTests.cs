using CayTheGioi.Domain;
using NUnit.Framework;

namespace CayTheGioi.Tests
{
    public sealed class EvolutionTests
    {
        private PrototypeRules rules;
        private GameState state;
        private ResourceSystem resources;
        private EvolutionSystem evolution;
        [SetUp] public void Setup()
        {
            rules=new PrototypeRules();state=GameStateFactory.CreateNew(rules);state.run.photosynthesis=1000;
            resources=new ResourceSystem(state,rules,new VitalitySystem(state,rules));evolution=new EvolutionSystem(state,rules,resources);
        }
        [Test] public void ExactCostCommitsBeforeEventsAndRetainsRunMetaAndVitality()
        {
            state.run.water=50;state.run.photosynthesis=25;state.run.growth=25;state.run.levels.root=5;state.run.levels.leaves=4;
            state.vitality.bankSeconds=20;state.vitality.tapProgress=49;state.permanent.coin=123;
            string runId=state.run.runId;int events=0;
            resources.Changed+=()=>{Assert.That(state.run.stageId,Is.EqualTo("young_tree"));Assert.That(state.run.water,Is.Zero);Assert.That(state.run.growth,Is.Zero);};
            evolution.StageChanged+=id=>{events++;Assert.That(id,Is.EqualTo("young_tree"));Assert.That(evolution.TryEvolve().Reason,Is.EqualTo("BUSY"));};
            Assert.That(evolution.GetQuote().WaterCost,Is.EqualTo(50));Assert.That(evolution.TryEvolve().Success,Is.True);
            Assert.That(events,Is.EqualTo(1));Assert.That(state.run.runId,Is.EqualTo(runId));Assert.That(state.run.levels.root,Is.EqualTo(5));Assert.That(state.run.levels.leaves,Is.EqualTo(4));
            Assert.That(state.vitality.bankSeconds,Is.EqualTo(20));Assert.That(state.vitality.tapProgress,Is.EqualTo(49));Assert.That(state.permanent.coin,Is.EqualTo(123));
            foreach(var point in state.permanent.stagePoints) Assert.That(point.points,Is.Zero);
        }
        [Test] public void AbundantResourcesNeverAutoEvolveAndTwoCommandsSpendOncePerTransition()
        {
            state.run.water=1000;state.run.growth=1000;resources.Tick(10);
            Assert.That(state.run.stageId,Is.EqualTo("seedling"));
            evolution.TryEvolve();for(int i=0;i<100;i++)Assert.That(evolution.TryEvolve().Reason,Is.EqualTo("BUSY"));
            Assert.That(state.run.water,Is.EqualTo(950));evolution.CompleteTransition();
            Assert.That(evolution.TryEvolve().Success,Is.True);Assert.That(state.run.water,Is.EqualTo(750));Assert.That(state.run.growth,Is.EqualTo(875));
            evolution.CompleteTransition();Assert.That(evolution.TryEvolve().Success,Is.True);evolution.CompleteTransition();Assert.That(evolution.TryEvolve().Reason,Is.EqualTo("FINAL_STAGE"));Assert.That(state.run.stageId,Is.EqualTo("mature_tree"));
        }
        [TestCase(49.999,25)] [TestCase(50,24.999)] [TestCase(double.NaN,25)] [TestCase(50,double.PositiveInfinity)]
        public void InvalidOrInsufficientBalancesDoNotMutateOrEmit(double water,double growth)
        {
            state.run.water=water;state.run.growth=growth;int events=0;resources.Changed+=()=>events++;evolution.StageChanged+=_=>events++;
            Assert.That(evolution.TryEvolve().Success,Is.False);Assert.That(state.run.stageId,Is.EqualTo("seedling"));
            Assert.That(state.run.water,Is.EqualTo(water));Assert.That(state.run.growth,Is.EqualTo(growth));Assert.That(events,Is.Zero);Assert.That(evolution.IsTransitioning,Is.False);
        }
        [Test] public void DeadInvalidStageAndShrinkingCapsAreRejected()
        {
            state.run.water=1000;state.run.growth=1000;state.run.life="DEAD";Assert.That(evolution.TryEvolve().Reason,Is.EqualTo("DEAD"));
            state.run.life="ALIVE";state.run.stageId="unknown";Assert.That(evolution.TryEvolve().Reason,Is.EqualTo("INVALID_STATE"));
            state.run.stageId="seedling";rules.FindStage("young_tree").rootCap=4;Assert.That(evolution.TryEvolve().Reason,Is.EqualTo("INVALID_CONFIG"));
        }
        [Test] public void UpgradeBlockedDuringEvolutionThenNewCapAndUnlockBecomeAvailable()
        {
            var upgrades=new UpgradeSystem(state,rules,resources,()=>evolution.IsTransitioning);
            state.run.water=1000;state.run.growth=1000;state.run.levels.root=5;
            Assert.That(upgrades.TryUpgrade("root").Reason,Is.EqualTo("CAP_REACHED"));Assert.That(upgrades.TryUpgrade("spirit_absorption").Reason,Is.EqualTo("LOCKED"));
            evolution.TryEvolve();Assert.That(upgrades.TryUpgrade("root").Reason,Is.EqualTo("BUSY"));evolution.CompleteTransition();
            Assert.That(upgrades.TryUpgrade("root").Success,Is.True);Assert.That(state.run.levels.root,Is.EqualTo(6));Assert.That(upgrades.TryUpgrade("trunk").Success,Is.True);
            Assert.That(rules.FindStage(state.run.stageId).rebirthUnlocked,Is.True);Assert.That(rules.FindStage(state.run.stageId).weatherUnlocked,Is.True);
        }
        [Test] public void NumericPrecisionInvalidCostAndLaterMilestoneAreHandled()
        {
            state.run.water=double.MaxValue;state.run.growth=100;Assert.That(evolution.TryEvolve().Reason,Is.EqualTo("NUMERIC_PRECISION"));
            state.run.water=100;rules.FindStage("young_tree").evolutionWaterCost=double.NaN;Assert.That(evolution.TryEvolve().Reason,Is.EqualTo("INVALID_CONFIG"));
            rules.FindStage("young_tree").evolutionWaterCost=50;state.permanent.highestStageEverId="large_tree";evolution.TryEvolve();Assert.That(state.permanent.highestStageEverId,Is.EqualTo("large_tree"));
        }
        [Test] public void SubscriberFailureReleasesTransientGuardWithoutRepeatingCommittedPayment()
        {
            state.run.water=50;state.run.photosynthesis=25;state.run.growth=25;evolution.StageChanged+=_=>throw new System.InvalidOperationException("view failed");
            Assert.Throws<System.InvalidOperationException>(()=>evolution.TryEvolve());Assert.That(evolution.IsTransitioning,Is.False);
            Assert.That(state.run.stageId,Is.EqualTo("young_tree"));Assert.That(state.run.water,Is.Zero);Assert.That(evolution.TryEvolve().Reason,Is.EqualTo("INSUFFICIENT_RESOURCE"));
        }

        [TestCase("water")] [TestCase("photosynthesis")] [TestCase("growth")]
        public void EvolutionRequiresAllThreeAndPaysBeforeNotification(string missing)
        {
            state.run.water=50;state.run.photosynthesis=25;state.run.growth=25;
            if(missing=="water")state.run.water=49.999;
            if(missing=="photosynthesis")state.run.photosynthesis=24.999;
            if(missing=="growth")state.run.growth=24.999;
            int events=0;
            resources.Changed+=()=>{events++;Assert.That(state.run.water+state.run.photosynthesis+state.run.growth,Is.Zero);};
            Assert.That(evolution.TryEvolve().Reason,Is.EqualTo("INSUFFICIENT_RESOURCE"));Assert.That(events,Is.Zero);
            state.run.water=50;state.run.photosynthesis=25;state.run.growth=25;
            Assert.That(evolution.TryEvolve().Success,Is.True);Assert.That(events,Is.EqualTo(1));
        }
    }
}
