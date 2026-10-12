using System;
using System.IO;
using CayTheGioi.Domain;
using CayTheGioi.Infrastructure;
using NUnit.Framework;
using UnityEngine;

namespace CayTheGioi.Tests
{
    public sealed class OfflineTests
    {
        private PrototypeRules rules;
        private GameState state;
        private string directory;
        private SaveService service;
        private OfflineCoordinator offline;
        private long revision, now;
        [SetUp] public void Setup()
        {
            rules = new PrototypeRules(); state = GameStateFactory.CreateNew(rules);
            state.run.levels.root = 1; state.run.water = 0; state.run.photosynthesis=0;state.run.growth=0;
            state.vitality.bankSeconds = 600; state.vitality.tapProgress = 37;
            directory = Path.Combine(Application.temporaryCachePath, "T05_Edit_" + Guid.NewGuid().ToString("N"));
            now = 1000; revision = 0; service = new SaveService(rules, directory); service.UtcNowSeconds = () => now;
            offline = new OfflineCoordinator(rules, service);
        }
        [TearDown] public void Cleanup() { if (Directory.Exists(directory)) Directory.Delete(directory, true); }
        private void Save() { var saved = service.Save(state, "test-id", ref revision); Assert.That(saved.Success, Is.True, saved.Reason); }
        [TestCase(3600, 1800)] [TestCase(86400, 43200)] [TestCase(108000, 43200)]
        public void RootCapHalfRateAndVitalityFreeze(long seconds, double water)
        {
            var reward = OfflineProduction.Evaluate(state, rules, 1000, 1000 + seconds);
            Assert.That(reward.Water, Is.EqualTo(water)); Assert.That(reward.ConsideredSeconds, Is.EqualTo(Math.Min(seconds, 86400)));
            Assert.That(reward.Capped, Is.EqualTo(seconds > 86400)); Assert.That(reward.CheckpointUtcSeconds, Is.EqualTo(1000 + seconds));
            Assert.That(state.run.water, Is.Zero); Assert.That(state.vitality.bankSeconds, Is.EqualTo(600)); Assert.That(state.vitality.tapProgress, Is.EqualTo(37));
        }
        [TestCase(0, 3600, 0, 0)] [TestCase(10, 3600, 0, 40)] [TestCase(1000, 100, 987.5, 50)]
        public void LeavesOnlyConsumeAvailableWaterOnce(double initialWater, long seconds, double finalWater, double growth)
        {
            state.run.levels.root = 0; state.run.levels.leaves = 1; state.run.water = initialWater;
            var reward = OfflineProduction.Evaluate(state, rules, 1000, 1000 + seconds);
            Assert.That(reward.Water, Is.EqualTo(finalWater).Within(1e-9)); Assert.That(reward.Photosynthesis, Is.EqualTo(growth).Within(1e-9));
        }
        [Test] public void SimultaneousSupplyDemandUsesEffectiveDurationOnce()
        {
            state.run.levels.leaves = 1;
            var reward = OfflineProduction.Evaluate(state, rules, 1000, 4600);
            Assert.That(reward.Water, Is.EqualTo(1350)); Assert.That(reward.Photosynthesis, Is.EqualTo(1800));
            state.run.levels.leaves = 5;
            reward = OfflineProduction.Evaluate(state, rules, 1000, 4600);
            Assert.That(reward.Water, Is.Zero); Assert.That(reward.Photosynthesis, Is.EqualTo(7200));
        }
        [TestCase(1000)] [TestCase(999)] [TestCase(-1)]
        public void ZeroAndNegativeClockDoNotRewardOrRewind(long utc)
        {
            var reward = OfflineProduction.Evaluate(state, rules, 1000, utc);
            Assert.That(reward.WaterDelta, Is.Zero); Assert.That(reward.GrowthDelta, Is.Zero); Assert.That(reward.CheckpointUtcSeconds, Is.EqualTo(1000));
        }
        [Test] public void DeadWeatherStatusPermanentAndSettingsRemainUntouched()
        {
            state.run.life = "DEAD"; state.run.hp = 0; state.run.weather.remainingOnlineSeconds = 73;
            state.run.status.statusId = "burning"; state.run.status.remainingOnlineSeconds = 8; state.run.status.impactRegionId = "leaves";
            state.permanent.coin = 31; state.permanent.stagePoints[0].points = 4;
            state.settings.musicEnabled = false;
            Save(); now += 108000;
            Assert.That(offline.TryApply(state, "test-id", ref revision, 1000, out var reward).Success, Is.True);
            Assert.That(reward.Dead, Is.True); Assert.That(state.run.water, Is.Zero); Assert.That(state.run.growth, Is.Zero);
            Assert.That(state.run.hp, Is.Zero); Assert.That(state.run.stageId, Is.EqualTo("seedling"));
            Assert.That(state.run.weather.remainingOnlineSeconds, Is.EqualTo(73)); Assert.That(state.run.status.remainingOnlineSeconds, Is.EqualTo(8));
            Assert.That(state.permanent.coin, Is.EqualTo(31)); Assert.That(state.permanent.stagePoints[0].points, Is.EqualTo(4)); Assert.That(state.settings.musicEnabled, Is.False);
            Assert.That(state.vitality.bankSeconds, Is.EqualTo(600)); Assert.That(state.vitality.tapProgress, Is.EqualTo(37));
        }
        [Test] public void NeutralProductionKeepsConfiguredBranchRatesAndFreezeMetadata()
        {
            rules.rootWaterPerLevelSecond = 2; rules.leavesPhotosynthesisPerLevelSecond = 3; state.run.levels.leaves = 1;
            state.run.weather.remainingOnlineSeconds = 77; state.run.status.statusId = "burning"; state.run.status.remainingOnlineSeconds = 8;
            var reward = OfflineProduction.Evaluate(state, rules, 1000, 4600);
            Assert.That(reward.Water, Is.EqualTo(3150)); Assert.That(reward.Photosynthesis, Is.EqualTo(5400));
            Assert.That(state.run.hp, Is.EqualTo(100)); Assert.That(state.run.weather.remainingOnlineSeconds, Is.EqualTo(77)); Assert.That(state.run.status.remainingOnlineSeconds, Is.EqualTo(8));
        }
        [TestCase(SaveWritePhase.AfterFlush)] [TestCase(SaveWritePhase.BeforeReplace)]
        public void FailureKeepsSourceAndRetryFromOriginalCreditsOnce(SaveWritePhase phase)
        {
            Save(); string original = File.ReadAllText(service.MainPath); now = 4600;
            service.FaultInjection = at => { if (at == phase) throw new IOException("Offline fault"); };
            Assert.That(offline.TryApply(state, "test-id", ref revision, 1000, out var reward).Success, Is.False);
            Assert.That(reward, Is.Null); Assert.That(state.run.water, Is.Zero); Assert.That(revision, Is.EqualTo(1));
            Assert.That(File.ReadAllText(service.MainPath), Is.EqualTo(original));
            service.FaultInjection = null;
            Assert.That(offline.TryApply(state, "test-id", ref revision, 1000, out reward).Success, Is.True);
            Assert.That(state.run.water, Is.EqualTo(1800));
            Assert.That(offline.TryApply(state, "test-id", ref revision, service.CheckpointUtcSeconds, out reward).Success, Is.True);
            Assert.That(reward.WaterDelta, Is.Zero); Assert.That(state.run.water, Is.EqualTo(1800));
        }
        [Test] public void CrashBeforeCommitReloadsOldSnapshotAndAfterCommitDoesNotReplay()
        {
            Save(); now = 4600;
            service.FaultInjection = at => { if (at == SaveWritePhase.BeforeReplace) throw new IOException("Crash before replace"); };
            offline.TryApply(state, "test-id", ref revision, 1000, out _);
            var reopened = new SaveService(rules, directory); reopened.UtcNowSeconds = () => now;
            var dto = reopened.Load().result.Data; var fresh = GameStateFactory.CreateNew(rules); SaveSerializer.Restore(dto, fresh);
            long freshRevision = dto.revision;
            reopened.FaultInjection = at => { if (at == SaveWritePhase.AfterReplace) throw new IOException("Crash after replace"); };
            var coordinator = new OfflineCoordinator(rules, reopened);
            Assert.That(coordinator.TryApply(fresh, dto.saveId, ref freshRevision, dto.offlineCheckpointUtcSeconds, out _).Success, Is.True);
            dto = reopened.Load().result.Data; Assert.That(dto.run.water, Is.EqualTo(1800));
            SaveSerializer.Restore(dto, fresh); freshRevision = dto.revision;
            Assert.That(coordinator.TryApply(fresh, dto.saveId, ref freshRevision, dto.offlineCheckpointUtcSeconds, out var repeated).Success, Is.True);
            Assert.That(repeated.WaterDelta, Is.Zero); Assert.That(fresh.run.water, Is.EqualTo(1800));
        }
        [Test] public void InvalidOrOverflowCandidateCannotChangeGoodFile()
        {
            Save(); string original = File.ReadAllText(service.MainPath); rules.rootWaterPerLevelSecond = double.MaxValue; now = 4600;
            Assert.That(offline.TryApply(state, "test-id", ref revision, 1000, out _).Success, Is.False);
            Assert.That(state.run.water, Is.Zero); Assert.That(File.ReadAllText(service.MainPath), Is.EqualTo(original));
        }
    }
}
