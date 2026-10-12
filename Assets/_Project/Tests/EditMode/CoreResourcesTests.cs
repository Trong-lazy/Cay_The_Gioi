using System;
using CayTheGioi.Domain;
using CayTheGioi.Presentation;
using NUnit.Framework;
using UnityEngine;

namespace CayTheGioi.Tests
{
    public sealed class CoreResourcesTests
    {
        private PrototypeRules rules;
        private GameState state;
        private ResourceSystem resources;
        private VitalitySystem vitality;
        [SetUp]
        public void Setup()
        {
            rules = new PrototypeRules(); state = GameStateFactory.CreateNew(rules); state.run.water = 0; state.run.photosynthesis=0; state.run.growth=0;
            vitality = new VitalitySystem(state, rules); resources = new ResourceSystem(state, rules, vitality);
        }
        [TestCase(1)] [TestCase(10)] [TestCase(1000)]
        public void RootProducesTenRegardlessOfPartition(int steps)
        {
            state.run.levels.root = 1;
            for (int i = 0; i < steps; i++) Assert.That(resources.Tick(10.0 / steps).Success, Is.True);
            Assert.That(state.run.water, Is.EqualTo(10).Within(1e-9));
        }
        [TestCase(1)] [TestCase(10)] [TestCase(1000)]
        public void LeavesStopAtAvailableWater(int steps)
        {
            state.run.water = 1; state.run.levels.leaves = 1;
            for (int i = 0; i < steps; i++) resources.Tick(10.0 / steps);
            Assert.That(state.run.water, Is.EqualTo(0).Within(1e-9));
            Assert.That(state.run.photosynthesis, Is.EqualTo(4).Within(1e-9));
        }
        [Test]
        public void SimultaneousSupplyAndShortageAgreeAcrossIrregularPartitions()
        {
            state.run.water = 0.37; state.run.levels.root = 1; state.run.levels.leaves = 5; state.vitality.bankSeconds = 3.7;
            var expected = new GameState { run = new RunState { water = 0.37, levels = new LevelState { root = 1, leaves = 5 } }, vitality = new VitalityState { bankSeconds = 3.7 } };
            var v = new VitalitySystem(expected, rules); var r = new ResourceSystem(expected, rules, v); r.Tick(10);
            var random = new System.Random(42); double remaining = 10;
            while (remaining > 0) { double dt = Math.Min(remaining, random.NextDouble() * 0.7 + 0.01); resources.Tick(dt); remaining -= dt; }
            Assert.That(state.run.water, Is.EqualTo(expected.run.water).Within(1e-9));
            Assert.That(state.run.photosynthesis, Is.EqualTo(expected.run.photosynthesis).Within(1e-9));
        }
        [Test]
        public void FiftyAndHundredTapsBankTimeWithoutStackingOrWaterYield()
        {
            for (int i = 0; i < 49; i++) vitality.TryTap();
            Assert.That(state.vitality.bankSeconds, Is.Zero); Assert.That(state.vitality.tapProgress, Is.EqualTo(49));
            vitality.TryTap(); Assert.That(state.vitality.tapProgress, Is.Zero); Assert.That(state.vitality.bankSeconds, Is.EqualTo(20));
            for (int i = 0; i < 50; i++) vitality.TryTap();
            Assert.That(state.vitality.bankSeconds, Is.EqualTo(40));
            state.run.levels.root = 1; resources.Tick(1);
            Assert.That(state.run.water, Is.EqualTo(1.05).Within(1e-12));
        }
        [Test]
        public void RewardClampsAtCapEvenWhenAlreadyFull()
        {
            state.vitality.bankSeconds = 3590; state.vitality.tapProgress = 49;
            Assert.That(vitality.TryTap().Reason, Is.EqualTo("BANK_FULL")); Assert.That(state.vitality.bankSeconds, Is.EqualTo(3600));
            for (int i = 0; i < 50; i++) vitality.TryTap();
            Assert.That(state.vitality.bankSeconds, Is.EqualTo(3600)); Assert.That(state.vitality.tapProgress, Is.Zero);
        }
        [Test]
        public void CrossingBankBoundaryOnlyBuffsFirstFiveSeconds()
        {
            state.run.levels.root = 1; state.vitality.bankSeconds = 5; resources.Tick(10);
            Assert.That(state.run.water, Is.EqualTo(10.25).Within(1e-12)); Assert.That(state.vitality.bankSeconds, Is.Zero);
        }
        [Test]
        public void BuffDoesNotIncreaseConsumptionOrUnrelatedState()
        {
            state.run.levels.leaves = 1; state.run.levels.trunk = 3; state.run.water = 10; state.vitality.bankSeconds = 20;
            state.permanent.coin = 7; state.permanent.stagePoints[0].points = 2; resources.Tick(10);
            Assert.That(state.run.water, Is.EqualTo(7.5)); Assert.That(state.run.photosynthesis, Is.EqualTo(3)); Assert.That(state.run.growth, Is.EqualTo(31.5).Within(1e-9));
            Assert.That(state.run.hp, Is.EqualTo(100)); Assert.That(state.run.levels.trunk, Is.EqualTo(3));
            Assert.That(state.permanent.coin, Is.EqualTo(7)); Assert.That(state.permanent.stagePoints[0].points, Is.EqualTo(2));
        }
        [Test]
        public void DeadAndBlockedCommandsPreserveStateAndEmitNoChanges()
        {
            int events = 0; resources.Changed += () => events++; vitality.Changed += () => events++;
            Assert.That(vitality.TryTap(false).Reason, Is.EqualTo("INPUT_BLOCKED"));
            state.run.life = "DEAD"; state.vitality.bankSeconds = 5; state.run.levels.root = 1;
            Assert.That(resources.Tick(10).Reason, Is.EqualTo("DEAD")); Assert.That(vitality.TryTap().Reason, Is.EqualTo("DEAD"));
            Assert.That(state.vitality.bankSeconds, Is.EqualTo(5)); Assert.That(state.run.water, Is.Zero); Assert.That(events, Is.Zero);
        }
        [TestCase(double.NaN)] [TestCase(double.PositiveInfinity)] [TestCase(-1)]
        public void InvalidTimeCannotMutate(double dt)
        { Assert.That(resources.Tick(dt).Success, Is.False); Assert.That(state.run.water, Is.Zero); }
        [Test]
        public void OverflowInSecondSegmentRollsBackResourcesAndBank()
        {
            state.run.levels.root = 5; state.vitality.bankSeconds = 5;
            Assert.That(resources.Tick(double.MaxValue).Success, Is.False);
            Assert.That(state.run.water, Is.Zero); Assert.That(state.vitality.bankSeconds, Is.EqualTo(5));
        }
        [Test]
        public void EvaluateIsPureAndEventsSeeCommittedSnapshot()
        {
            state.run.levels.root = 1;
            var result = ProductionSystem.Evaluate(state.run, rules, 10);
            Assert.That(result.Water, Is.EqualTo(10)); Assert.That(state.run.water, Is.Zero);
            state.vitality.bankSeconds = 5;
            resources.Changed += () => { Assert.That(state.run.water, Is.EqualTo(10.25)); Assert.That(state.vitality.bankSeconds, Is.Zero); };
            resources.Tick(10);
        }
        [Test]
        public void GestureRejectsHoldDragCancelWrongPointerAndUi()
        {
            var g = new PointerGesture();
            g.Begin(1, Vector2.zero, 0, 10, false); Assert.That(g.Release(1, Vector2.zero, 0.1, 0.4, false), Is.True);
            g.Begin(1, Vector2.zero, 0, 10, false); Assert.That(g.Release(1, Vector2.zero, 1, 0.4, false), Is.False);
            g.Begin(1, Vector2.zero, 0, 10, false); g.Move(1, new Vector2(11, 0)); Assert.That(g.Release(1, Vector2.zero, 0.1, 0.4, false), Is.False);
            g.Begin(1, Vector2.zero, 0, 10, false); g.Cancel(); Assert.That(g.Release(1, Vector2.zero, 0.1, 0.4, false), Is.False);
            g.Begin(1, Vector2.zero, 0, 10, false); Assert.That(g.Release(2, Vector2.zero, 0.1, 0.4, false), Is.False);
            g.Begin(1, Vector2.zero, 0, 10, true); Assert.That(g.Release(1, Vector2.zero, 0.1, 0.4, false), Is.False);
            g.Begin(1, Vector2.zero, 0, 10, false); Assert.That(g.Release(1, Vector2.zero, 0.1, 0.4, true), Is.False);
        }
        [Test]
        public void DragThresholdUsesDpiWithBoundedScreenFallback()
        {
            var input = new GameplayInputConfig();
            Assert.That(input.DragPixels(0, 1000, 2000), Is.EqualTo(12));
            Assert.That(input.DragPixels(254, 1000, 2000), Is.EqualTo(20).Within(1e-6));
            Assert.That(input.DragPixels(float.PositiveInfinity, 1000, 2000), Is.EqualTo(12));
        }

        [TestCase(1)] [TestCase(100)] [TestCase(10000)]
        public void EntireChainAgreesAcrossWaterAndPhotoDepletion(int steps)
        {
            state.run.water=0.37;state.run.photosynthesis=0.91;state.run.growth=0;state.run.levels.root=1;state.run.levels.leaves=5;state.run.levels.trunk=20;
            var expected=ProductionSystem.Evaluate(state.run,rules,100);
            for(int i=0;i<steps;i++)Assert.That(resources.Tick(100.0/steps).Success,Is.True);
            Assert.That(state.run.water,Is.EqualTo(expected.Water).Within(1e-8));
            Assert.That(state.run.photosynthesis,Is.EqualTo(expected.Photosynthesis).Within(1e-8));
            Assert.That(state.run.growth,Is.EqualTo(expected.Growth).Within(1e-8));
            Assert.That(state.run.growth,Is.LessThan(2000));
        }
        [Test]
        public void LockedSpiritNeverProducesAndUnlockedSpiritUsesOnlineBuffOnly()
        {
            state.run.levels.spiritAbsorption=1;resources.Tick(10);Assert.That(state.run.spirit,Is.Zero);
            state.run.spiritAbsorptionUnlocked=true;state.run.stageId="mature_tree";state.vitality.bankSeconds=5;
            resources.Tick(10);Assert.That(state.run.spirit,Is.EqualTo(10.25));
            var offline=OfflineProduction.Evaluate(state,rules,1000,1020);Assert.That(offline.SpiritDelta,Is.EqualTo(10));
            state.run.life="DEAD";resources.Tick(10);Assert.That(state.run.spirit,Is.EqualTo(10.25));
        }
    }
}
