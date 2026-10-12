using System;
using System.IO;
using CayTheGioi.Domain;
using CayTheGioi.Infrastructure;
using NUnit.Framework;
using UnityEngine;

namespace CayTheGioi.Tests
{
    public sealed class SaveSafetyTests
    {
        private string directory;
        private PrototypeRules rules;
        private SaveService service;
        private GameState state;
        private long revision;
        [SetUp] public void Setup()
        {
            directory = Path.Combine(Application.temporaryCachePath, "T04_Safety_" + Guid.NewGuid().ToString("N"));
            rules = new PrototypeRules(); service = new SaveService(rules, directory); state = GameStateFactory.CreateNew(rules);
            revision = 0;
        }
        [TearDown] public void Cleanup() { if (Directory.Exists(directory)) Directory.Delete(directory, true); }
        private void Save() { var result = service.Save(state, "safe-id", ref revision); Assert.That(result.Success, Is.True, result.Reason); }

        [TestCase(SaveWritePhase.AfterFlush)] [TestCase(SaveWritePhase.BeforeReplace)]
        public void FailureBeforeCommitRetainsMainAndRevision(SaveWritePhase phase)
        {
            Save(); string original = File.ReadAllText(service.MainPath); state.run.water = 33;
            service.FaultInjection = at => { if (at == phase) throw new IOException("Injected failure"); };
            var result = service.Save(state, "safe-id", ref revision);
            Assert.That(result.Success, Is.False); Assert.That(revision, Is.EqualTo(1));
            Assert.That(File.ReadAllText(service.MainPath), Is.EqualTo(original));
            Assert.That(File.Exists(service.MainPath + ".tmp"), Is.False);
        }
        [Test] public void FailureAfterReplacementAcknowledgesCommittedSnapshot()
        {
            Save(); state.run.water = 33;
            service.FaultInjection = at => { if (at == SaveWritePhase.AfterReplace) throw new IOException("After commit"); };
            Save(); Assert.That(revision, Is.EqualTo(2)); Assert.That(service.Load().result.Data.run.water, Is.EqualTo(33));
            Assert.That(SaveCodec.Decode(File.ReadAllText(service.BackupPath), rules).Success, Is.True);
        }
        [Test] public void NewerSchemaAndUnknownConfigCannotBeOverwritten()
        {
            Save(); string baseJson = File.ReadAllText(service.MainPath);
            string original = baseJson.Replace("\"schemaVersion\":1", "\"schemaVersion\":2"); File.WriteAllText(service.MainPath, original);
            Assert.That(service.Save(state, "safe-id", ref revision).Success, Is.False);
            Assert.That(File.ReadAllText(service.MainPath), Is.EqualTo(original));
            original = baseJson.Replace(rules.configVersion, "future-config"); File.WriteAllText(service.MainPath, original);
            Assert.That(service.Load().outcome, Is.EqualTo(SaveService.LoadOutcome.IncompatibleConfig));
            Assert.That(service.Save(state, "safe-id", ref revision).Success, Is.False);
            Assert.That(File.ReadAllText(service.MainPath), Is.EqualTo(original));
        }

        [Test] public void MissingOptionalFieldsDefaultButMissingCoreIsRejected()
        {
            string json = "{\"schemaVersion\":1,\"configVersion\":\"prototype-2\",\"saveId\":\"id\",\"revision\":1,\"savedAtUtcSeconds\":1,\"offlineCheckpointUtcSeconds\":1," +
                "\"run\":{\"runId\":\"run-id\",\"life\":\"ALIVE\",\"stageId\":\"seedling\",\"water\":10,\"growth\":0,\"hp\":100,\"levels\":{\"root\":0,\"leaves\":0,\"trunk\":0}}}";
            var result = SaveCodec.Decode(json, rules);
            Assert.That(result.Success, Is.True, result.Reason); Assert.That(result.Data.settings.musicEnabled, Is.True);
            Assert.That(result.Data.vitality.bankSeconds, Is.Zero); Assert.That(result.Data.run.weather.remainingOnlineSeconds, Is.EqualTo(120));
            Assert.That(SaveCodec.Decode(json.Replace("\"hp\":100,", ""), rules).Success, Is.False);
        }

        [Test] public void ClockRollbackDoesNotMoveCheckpointBackward()
        {
            service.UtcNowSeconds = () => 1000; Save(); service.UtcNowSeconds = () => 900; Save();
            Assert.That(service.Load().result.Data.offlineCheckpointUtcSeconds, Is.EqualTo(1000));
        }
        [Test] public void MissingMainStillOffersBackupAndRequiresAcceptance()
        {
            Save(); Save(); File.Delete(service.MainPath);
            Assert.That(service.Load().outcome, Is.EqualTo(SaveService.LoadOutcome.LoadedFromBackup));
            Assert.That(service.Save(state, "safe-id", ref revision).Success, Is.False);
            service.AcceptRecovery(); service.UtcNowSeconds = () => 2000000000; Save();
            Assert.That(service.Load().result.Data.offlineCheckpointUtcSeconds, Is.EqualTo(2000000000));
        }
        [Test] public void CorruptBothPreservedUntilExplicitArchivedReset()
        {
            Save(); Save(); File.WriteAllText(service.MainPath, "broken-main"); File.WriteAllText(service.BackupPath, "broken-backup");
            Assert.That(service.Save(state, "safe-id", ref revision).Success, Is.False);
            Assert.That(File.ReadAllText(service.MainPath), Is.EqualTo("broken-main"));
            Assert.That(service.ArchiveForReset().Success, Is.True);
            Assert.That(Directory.GetFiles(directory, "*.reset-*").Length, Is.EqualTo(2));
            Assert.That(service.Load().outcome, Is.EqualTo(SaveService.LoadOutcome.NewGame));
        }
        [Test] public void SnapshotAndRestoreHaveNoMutableEntryAliases()
        {
            state.run.runCounters.Add(new CounterEntry { counterId = "tap", value = 3 });
            var dto = SaveSerializer.Snapshot(state, rules.configVersion, "id", 1); state.run.runCounters[0].value = 99;
            Assert.That(dto.run.runCounters[0].value, Is.EqualTo(3));
            var restored = GameStateFactory.CreateNew(rules); SaveSerializer.Restore(dto, restored); dto.run.runCounters[0].value = 77;
            Assert.That(restored.run.runCounters[0].value, Is.EqualTo(3));
            Assert.That(restored.permanent.stagePoints, Is.Not.SameAs(dto.permanent.stagePoints));
        }
        [TestCase("life")] [TestCase("counter")] [TestCase("points")] [TestCase("weather")]
        public void InvalidRuntimeCannotReplaceGoodSave(string field)
        {
            Save(); string original = File.ReadAllText(service.MainPath);
            if (field == "life") state.run.hp = 0;
            if (field == "counter") { state.run.runCounters.Add(new CounterEntry { counterId = "a" }); state.run.runCounters.Add(new CounterEntry { counterId = "a" }); }
            if (field == "points") state.permanent.stagePoints[0].points = -1;
            if (field == "weather") state.run.weather.remainingOnlineSeconds = double.NaN;
            Assert.That(service.Save(state, "safe-id", ref revision).Success, Is.False);
            Assert.That(revision, Is.EqualTo(1)); Assert.That(File.ReadAllText(service.MainPath), Is.EqualTo(original));
        }
        [Test] public void OlderSchemaAndDuplicateJsonKeysAreRejected()
        {
            Save(); string json = File.ReadAllText(service.MainPath);
            Assert.That(SaveCodec.Decode(json.Replace("\"schemaVersion\":1", "\"schemaVersion\":0"), rules).Success, Is.False);
            Assert.That(SaveCodec.Decode(json.Replace("\"schemaVersion\":1", "\"schemaVersion\":1,\"schemaVersion\":1"), rules).Success, Is.False);
            Assert.That(SaveCodec.Decode(json.Replace("\""+rules.configVersion+"\"", "\"prototype-1\""), rules).Success, Is.True);
        }
    }
}
