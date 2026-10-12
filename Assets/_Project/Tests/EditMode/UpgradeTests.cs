using CayTheGioi.Domain;
using CayTheGioi.Presentation;
using NUnit.Framework;

namespace CayTheGioi.Tests
{
    public sealed class UpgradeTests
    {
        private PrototypeRules rules;
        private GameState state;
        private UpgradeSystem upgrades;
        private ResourceSystem resources;
        [SetUp]
        public void Setup()
        {
            rules = new PrototypeRules(); state = GameStateFactory.CreateNew(rules);
            var vitality = new VitalitySystem(state, rules); resources = new ResourceSystem(state, rules, vitality);
            upgrades = new UpgradeSystem(state, rules, resources);
        }
        [Test]
        public void ExactFiveWaterUpgradesOnceAndEventsObserveCommittedState()
        {
            state.run.water = 5; int resourceEvents = 0, upgradeEvents = 0;
            resources.Changed += () => { resourceEvents++; Assert.That(state.run.water, Is.Zero); Assert.That(state.run.levels.root, Is.EqualTo(1)); };
            upgrades.Completed += id => { upgradeEvents++; Assert.That(id, Is.EqualTo("root")); Assert.That(state.run.levels.root, Is.EqualTo(1)); };
            Assert.That(upgrades.TryUpgrade("root").Success, Is.True);
            Assert.That(resourceEvents, Is.EqualTo(1)); Assert.That(upgradeEvents, Is.EqualTo(1));
            Assert.That(state.run.growth, Is.EqualTo(rules.initialGrowth));
        }
        [Test]
        public void InsufficientRoundedDisplayAndStaleQuoteCannotAuthorizePurchase()
        {
            state.run.water = 5; Assert.That(upgrades.GetQuote("root").CanUpgrade, Is.True);
            state.run.water = 4.999; Assert.That(UpgradePanelView.Format(state.run.water), Is.EqualTo("5"));
            int events = 0; resources.Changed += () => events++; upgrades.Completed += _ => events++;
            Assert.That(upgrades.TryUpgrade("root").Reason, Is.EqualTo("INSUFFICIENT_RESOURCE"));
            Assert.That(state.run.water, Is.EqualTo(4.999)); Assert.That(state.run.levels.root, Is.Zero); Assert.That(events, Is.Zero);
        }
        [TestCase(0, 5)] [TestCase(1, 8)] [TestCase(2, 13)] [TestCase(3, 21)] [TestCase(4, 33)]
        public void CostUsesConfiguredExponentialAndSingleCeiling(int level, double expected)
        {
            state.run.levels.root = level;
            Assert.That(upgrades.GetQuote("root").Cost, Is.EqualTo(expected));
            Assert.That(UpgradeSystem.TryCalculateCost(10, 1.6, level, out double cost), Is.True);
            Assert.That(cost, Is.EqualTo(System.Math.Ceiling(10 * System.Math.Pow(1.6, level))));
        }
        [Test]
        public void CustomCostAndKDisplayStillSpendExactFullValue()
        {
            rules.rootBaseCost = 1234; state.run.water = 2000;
            Assert.That(UpgradePanelView.Format(upgrades.GetQuote("root").Cost), Is.EqualTo("1.23K"));
            upgrades.TryUpgrade("root"); Assert.That(state.run.water, Is.EqualTo(766));
        }
        [Test]
        public void LockedCapDeadAndUnknownHaveReasonsWithoutMutationOrEvents()
        {
            int events = 0; resources.Changed += () => events++; upgrades.Completed += _ => events++;
            double balance = state.run.water;
            Assert.That(upgrades.TryUpgrade("spirit_absorption").Reason, Is.EqualTo("LOCKED"));
            Assert.That(upgrades.TryUpgrade("other").Reason, Is.EqualTo("UNKNOWN_BRANCH"));
            state.run.levels.root = 5; Assert.That(upgrades.TryUpgrade("root").Reason, Is.EqualTo("CAP_REACHED"));
            state.run.levels.root = 0; state.run.life = "DEAD"; Assert.That(upgrades.TryUpgrade("root").Reason, Is.EqualTo("DEAD"));
            Assert.That(state.run.water, Is.EqualTo(balance)); Assert.That(state.run.levels.root, Is.Zero); Assert.That(state.run.levels.trunk, Is.Zero); Assert.That(events, Is.Zero);
        }
        [Test]
        public void StageCapUnlocksTrunkAndVitalityNeverChangesProtection()
        {
            state.run.stageId = "young_tree"; state.run.water = 100; state.run.growth = 100; state.vitality.bankSeconds = 20;
            Assert.That(upgrades.GetQuote("trunk").Cost, Is.EqualTo(10)); Assert.That(upgrades.GetQuote("trunk").NextOutput, Is.EqualTo(1.05));
            upgrades.TryUpgrade("trunk"); Assert.That(upgrades.Protection, Is.EqualTo(0.05));
            Assert.That(state.run.hp, Is.EqualTo(100)); Assert.That(state.vitality.bankSeconds, Is.EqualTo(20));
            state.vitality.bankSeconds = 0; Assert.That(upgrades.Protection, Is.EqualTo(0.05));
            state.run.levels.trunk = 5; Assert.That(upgrades.TryUpgrade("trunk").Reason, Is.EqualTo("CAP_REACHED"));
        }
        [Test]
        public void UpgradeUpdatesProductionWithOneVitalityMultiplier()
        {
            state.run.water = 100; state.vitality.bankSeconds = 20;
            upgrades.TryUpgrade("root"); upgrades.TryUpgrade("leaves");
            Assert.That(resources.Rates().WaterProduced, Is.EqualTo(1.05));
            Assert.That(resources.Rates().WaterConsumed, Is.EqualTo(0.25));
            Assert.That(upgrades.GetQuote("leaves").NextOutput, Is.EqualTo(2.1));
            resources.Tick(10);
            Assert.That(state.run.water, Is.EqualTo(103)); Assert.That(state.run.photosynthesis, Is.EqualTo(10.5)); Assert.That(state.run.growth, Is.EqualTo(10)); Assert.That(state.vitality.bankSeconds, Is.EqualTo(10));
        }
        [Test]
        public void RepeatedCommandsNeverOverdrawAndReentrantSubscriberCannotUpgrade()
        {
            state.run.water = 20; CommandResult nested = default; int count = 0;
            upgrades.Completed += _ => { count++; nested = upgrades.TryUpgrade("root"); };
            for (int i = 0; i < 100; i++) upgrades.TryUpgrade("root");
            Assert.That(state.run.levels.root, Is.EqualTo(2)); Assert.That(state.run.water, Is.EqualTo(7)); Assert.That(count, Is.EqualTo(2)); Assert.That(nested.Reason, Is.EqualTo("BUSY"));
        }
        [Test]
        public void AllBranchesReachTheirOwnCapWithAbundantResources()
        {
            state.run.stageId = "large_tree"; state.run.water = 1e8; state.run.photosynthesis=1e8; state.run.growth = 1e8;
            for (int i = 0; i < 30; i++) { upgrades.TryUpgrade("root"); upgrades.TryUpgrade("leaves"); upgrades.TryUpgrade("trunk"); }
            Assert.That(state.run.levels.root, Is.EqualTo(20)); Assert.That(state.run.levels.leaves, Is.EqualTo(20)); Assert.That(state.run.levels.trunk, Is.EqualTo(10));
            Assert.That(state.run.water, Is.GreaterThan(0));
        }
        [TestCase(double.NaN)] [TestCase(double.PositiveInfinity)] [TestCase(-1)]
        public void InvalidBalanceCannotMutate(double water)
        {
            state.run.water = water; Assert.That(upgrades.TryUpgrade("root").Reason, Is.EqualTo("INVALID_STATE")); Assert.That(state.run.levels.root, Is.Zero);
        }
        [Test]
        public void InvalidStageLevelsCostOverflowAndUnrepresentableSpendAreRejected()
        {
            state.run.stageId = "unknown"; Assert.That(upgrades.TryUpgrade("root").Reason, Is.EqualTo("INVALID_STATE"));
            state.run.stageId = "seedling"; state.run.levels.root = -1; Assert.That(upgrades.TryUpgrade("root").Reason, Is.EqualTo("INVALID_STATE"));
            state.run.levels.root = 6; Assert.That(upgrades.TryUpgrade("root").Reason, Is.EqualTo("INVALID_STATE"));
            Assert.That(UpgradeSystem.TryCalculateCost(10, 1.6, 10000, out _), Is.False);
            rules.rootBaseCost = double.PositiveInfinity; state.run.levels.root = 0; Assert.That(upgrades.TryUpgrade("root").Reason, Is.EqualTo("INVALID_COST"));
            rules.rootBaseCost = 5; state.run.water = double.MaxValue; Assert.That(upgrades.TryUpgrade("root").Reason, Is.EqualTo("NUMERIC_PRECISION")); Assert.That(state.run.levels.root, Is.Zero);
        }

        [TestCase("root", 0, "water", 5)]
        [TestCase("root", 1, "water", 8)]
        [TestCase("leaves", 0, "photosynthesis", 5)]
        [TestCase("leaves", 1, "photosynthesis", 8)]
        [TestCase("trunk", 0, "growth", 10)]
        public void EachUpgradeDeductsOnlyItsQuotedCurrency(string branch, int level, string currency, double cost)
        {
            state.run.stageId = "young_tree"; state.run.water = 100; state.run.photosynthesis=100; state.run.growth = 100;
            if (branch == "root") state.run.levels.root = level;
            if (branch == "leaves") state.run.levels.leaves = level;
            var quote = upgrades.GetQuote(branch);
            Assert.That(quote.CostResourceId, Is.EqualTo(currency)); Assert.That(quote.Cost, Is.EqualTo(cost));
            int events = 0;
            resources.Changed += () =>
            {
                events++;
                Assert.That(state.run.water, Is.EqualTo(currency == "water" ? 100 - cost : 100));
                Assert.That(state.run.growth, Is.EqualTo(currency == "growth" ? 100 - cost : 100));
                Assert.That(state.run.photosynthesis,Is.EqualTo(currency=="photosynthesis"?100-cost:100));
                Assert.That(upgrades.GetQuote(branch).Level, Is.EqualTo(level + 1));
            };
            Assert.That(upgrades.TryUpgrade(branch).Success, Is.True); Assert.That(events, Is.EqualTo(1));
        }

        [TestCase("leaves", 1)] [TestCase("trunk", 0)]
        public void AbundantWaterCannotPayAGrowthCost(string branch, int level)
        {
            state.run.stageId = "young_tree"; state.run.water = 10000; state.run.photosynthesis=0;state.run.growth = 0;
            state.run.levels.leaves = level;
            int events = 0; resources.Changed += () => events++; upgrades.Completed += _ => events++;
            Assert.That(upgrades.TryUpgrade(branch).Reason, Is.EqualTo("INSUFFICIENT_RESOURCE"));
            Assert.That(state.run.water, Is.EqualTo(10000)); Assert.That(state.run.growth, Is.Zero);
            Assert.That(state.run.levels.leaves, Is.EqualTo(level)); Assert.That(state.run.levels.trunk, Is.Zero); Assert.That(events, Is.Zero);
        }

        [Test]
        public void FreshRunFundsAllDefaultBranchesAndStartsTheWholeChain()
        {
            Assert.That(upgrades.TryUpgrade("root").Success,Is.True);
            Assert.That(upgrades.TryUpgrade("leaves").Success,Is.True);
            Assert.That(upgrades.TryUpgrade("trunk").Success,Is.True);
            Assert.That(state.run.water,Is.EqualTo(5));
            Assert.That(state.run.photosynthesis,Is.Zero);Assert.That(state.run.growth,Is.Zero);
            resources.Tick(8);
            Assert.That(state.run.water,Is.EqualTo(11));Assert.That(state.run.photosynthesis,Is.EqualTo(6));Assert.That(state.run.growth,Is.EqualTo(8));
            Assert.That(upgrades.GetQuote("leaves").CostResourceId,Is.EqualTo("photosynthesis"));
            Assert.That(upgrades.TryUpgrade("leaves").Reason,Is.EqualTo("INSUFFICIENT_RESOURCE"));
            resources.Tick(3);Assert.That(upgrades.TryUpgrade("leaves").Success,Is.True);
        }

        [Test]
        public void GrowthPurchaseUsesFullPrecisionAndRejectsFreeOrReentrantLevels()
        {
            state.run.levels.leaves = 1; state.run.water = 100;
            state.run.photosynthesis = 7.999;
            Assert.That(upgrades.TryUpgrade("leaves").Reason, Is.EqualTo("INSUFFICIENT_RESOURCE"));
            state.run.photosynthesis = double.MaxValue;
            Assert.That(upgrades.TryUpgrade("leaves").Reason, Is.EqualTo("NUMERIC_PRECISION"));
            state.run.photosynthesis = 8; CommandResult nested = default;
            resources.Changed += () => nested = upgrades.TryUpgrade("leaves");
            Assert.That(upgrades.TryUpgrade("leaves").Success, Is.True);
            Assert.That(nested.Reason, Is.EqualTo("BUSY")); Assert.That(state.run.photosynthesis, Is.Zero); Assert.That(state.run.water, Is.EqualTo(100));
        }

        [Test]
        public void ConfiguredCurrencyOverridesAreUsedAndInvalidBootstrapIsRejected()
        {
            rules.leavesCostResourceId = "water"; state.run.levels.leaves = 1; state.run.water = 8;
            Assert.That(upgrades.GetQuote("leaves").CostResourceId, Is.EqualTo("water"));
            Assert.That(upgrades.TryUpgrade("leaves").Success, Is.True);
            rules.trunkCostResourceId = "coin"; state.run.stageId = "young_tree";
            Assert.That(upgrades.TryUpgrade("trunk").Reason, Is.EqualTo("INVALID_COST_RESOURCE"));
            Assert.That(rules.Validate(), Is.Not.Empty);
            rules.trunkCostResourceId = "growth"; rules.leavesCostResourceId = "growth";
            Assert.That(rules.Validate(), Has.Some.Contains("Root/Leaves/Trunk"));
        }

        [TestCase("water")] [TestCase("photosynthesis")] [TestCase("growth")]
        public void SpiritUnlockRequiresEveryWalletAndCommitsOnce(string missing)
        {
            state.run.stageId="mature_tree";state.run.water=1000;state.run.photosynthesis=1000;state.run.growth=1000;
            if(missing=="water")state.run.water=999;
            if(missing=="photosynthesis")state.run.photosynthesis=999;
            if(missing=="growth")state.run.growth=999;
            int events=0;CommandResult nested=default;
            resources.Changed+=()=>{events++;Assert.That(state.run.spiritAbsorptionUnlocked,Is.True);Assert.That(state.run.levels.spiritAbsorption,Is.EqualTo(1));Assert.That(state.run.water+state.run.photosynthesis+state.run.growth,Is.Zero);nested=upgrades.TryUpgrade("spirit_absorption");};
            Assert.That(upgrades.TryUpgrade("spirit_absorption").Reason,Is.EqualTo("INSUFFICIENT_RESOURCE"));
            Assert.That(events,Is.Zero);Assert.That(state.run.spiritAbsorptionUnlocked,Is.False);
            state.run.water=1000;state.run.photosynthesis=1000;state.run.growth=1000;
            Assert.That(upgrades.TryUpgrade("spirit_absorption").Success,Is.True);Assert.That(events,Is.EqualTo(1));Assert.That(nested.Reason,Is.EqualTo("BUSY"));
            Assert.That(upgrades.GetQuote("spirit_absorption").CostResourceId,Is.EqualTo("spirit"));
            Assert.That(upgrades.TryUpgrade("spirit_absorption").Reason,Is.EqualTo("INSUFFICIENT_RESOURCE"));
        }
        [Test]
        public void SpiritIsLockedBeforeMatureAndRejectsUnrepresentableUnlock()
        {
            state.run.stageId="large_tree";state.run.water=10000;state.run.photosynthesis=10000;state.run.growth=10000;
            Assert.That(upgrades.TryUpgrade("spirit_absorption").Reason,Is.EqualTo("LOCKED"));
            state.run.stageId="mature_tree";state.run.photosynthesis=double.MaxValue;
            Assert.That(upgrades.TryUpgrade("spirit_absorption").Reason,Is.EqualTo("NUMERIC_PRECISION"));
            Assert.That(state.run.spiritAbsorptionUnlocked,Is.False);Assert.That(state.run.levels.spiritAbsorption,Is.Zero);
        }
    }
}
