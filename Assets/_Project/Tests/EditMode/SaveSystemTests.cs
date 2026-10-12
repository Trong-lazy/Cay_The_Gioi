using System;
using System.IO;
using CayTheGioi.Domain;
using CayTheGioi.Infrastructure;
using NUnit.Framework;
using UnityEngine;

namespace CayTheGioi.Tests
{
    /// <summary>
    /// EditMode tests for T04: SaveSerializer, SaveValidator and SaveService.
    /// No MonoBehaviour; uses a temp directory as the save directory.
    /// </summary>
    public sealed class SaveSystemTests
    {
        private PrototypeRules rules;
        private string tempDir;

        [SetUp]
        public void SetUp()
        {
            SaveService.ValidationClock = null;
            rules = new PrototypeRules();
            tempDir = Path.Combine(Application.temporaryCachePath, "SaveTests_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempDir);
        }

        [TearDown]
        public void TearDown()
        {
            SaveService.ValidationClock = null;
            if (Directory.Exists(tempDir)) Directory.Delete(tempDir, recursive: true);
        }

        // ── Serializer ────────────────────────────────────────────────

        [Test]
        public void SnapshotAndCloneKeepPhotosynthesisAndSpiritIndependent()
        {
            var state = GameStateFactory.CreateNew(rules);
            state.run.water = 17;
            state.run.photosynthesis = 23;
            state.run.growth = 31;
            state.run.spirit = 7;
            state.run.spiritAbsorptionUnlocked = true;
            state.run.levels.spiritAbsorption = 2;
            var copy = SaveSerializer.Clone(state);
            state.run.photosynthesis = 99;
            state.run.spirit = 88;
            state.run.spiritAbsorptionUnlocked = false;
            state.run.levels.spiritAbsorption = 0;
            Assert.That(copy.run.water, Is.EqualTo(17));
            Assert.That(copy.run.photosynthesis, Is.EqualTo(23));
            Assert.That(copy.run.growth, Is.EqualTo(31));
            Assert.That(copy.run.spirit, Is.EqualTo(7));
            Assert.That(copy.run.spiritAbsorptionUnlocked, Is.True);
            Assert.That(copy.run.levels.spiritAbsorption, Is.EqualTo(2));
        }

        [TestCase("prototype-1")] [TestCase("prototype-2")] [TestCase("prototype-3")] [TestCase("prototype-4")]
        public void OldConfigLoadsIntoCurrencyConfigWithoutResettingResourcesOrLevels(string oldVersion)
        {
            var state = GameStateFactory.CreateNew(rules);
            state.run.water = 42.5; state.run.growth = 19.25; state.run.levels.root = 2; state.run.levels.leaves = 1;
            state.permanent.coin = 30; state.vitality.bankSeconds = 20;
            var dto = SaveSerializer.Snapshot(state, oldVersion, "currency-migration", 3);
            var decoded = SaveCodec.Decode(SaveCodec.Encode(dto), rules);
            Assert.That(decoded.Success, Is.True, decoded.Reason);
            var restored = GameStateFactory.CreateNew(rules); SaveSerializer.Restore(decoded.Data, restored);
            Assert.That(restored.run.water, Is.EqualTo(42.5)); Assert.That(restored.run.growth, Is.EqualTo(19.25));
            Assert.That(restored.run.levels.root, Is.EqualTo(2)); Assert.That(restored.run.levels.leaves, Is.EqualTo(1));
            Assert.That(restored.permanent.coin, Is.EqualTo(30)); Assert.That(restored.vitality.bankSeconds, Is.EqualTo(20));
            Assert.That(SaveSerializer.Snapshot(restored, rules.configVersion, "currency-migration", 4).configVersion, Is.EqualTo("prototype-6"));
        }

        [Test]
        public void Snapshot_ReturnsDto_WithExpectedSchemaAndFields()
        {
            var state = GameStateFactory.CreateNew(rules);
            state.vitality.tapProgress = 12;
            state.vitality.bankSeconds = 500;
            state.settings.orbitLocked = true;

            var dto = SaveSerializer.Snapshot(state, "prototype-2", "save-001", 1);

            Assert.That(dto.schemaVersion, Is.EqualTo(1));
            Assert.That(dto.configVersion, Is.EqualTo("prototype-2"));
            Assert.That(dto.saveId, Is.EqualTo("save-001"));
            Assert.That(dto.revision, Is.EqualTo(1));
            Assert.That(dto.vitality.tapProgress, Is.EqualTo(12));
            Assert.That(dto.vitality.bankSeconds, Is.EqualTo(500));
            Assert.That(dto.settings.orbitLocked, Is.True);
            Assert.That(dto.run, Is.Not.Null);
            Assert.That(dto.permanent, Is.Not.Null);
        }

        [Test]
        public void Restore_Roundtrip_PreservesAllFields()
        {
            var state = GameStateFactory.CreateNew(rules);
            state.run.water = 42.5;
            state.run.growth = 10.0;
            state.run.levels.root = 3;
            state.run.levels.leaves = 2;
            state.vitality.bankSeconds = 720;
            state.vitality.tapProgress = 25;
            state.settings.musicEnabled = false;
            state.settings.orbitLocked = true;

            var dto = SaveSerializer.Snapshot(state, "prototype-2", "abc", 5);
            // JSON roundtrip (as would happen on disk)
            string json = JsonUtility.ToJson(dto);
            var parsed = JsonUtility.FromJson<SaveData>(json);

            var restored = GameStateFactory.CreateNew(rules);
            SaveSerializer.Restore(parsed, restored);

            Assert.That(restored.run.water, Is.EqualTo(42.5).Within(1e-9));
            Assert.That(restored.run.growth, Is.EqualTo(10.0).Within(1e-9));
            Assert.That(restored.run.levels.root, Is.EqualTo(3));
            Assert.That(restored.run.levels.leaves, Is.EqualTo(2));
            Assert.That(restored.vitality.bankSeconds, Is.EqualTo(720).Within(1e-9));
            Assert.That(restored.vitality.tapProgress, Is.EqualTo(25));
            Assert.That(restored.settings.musicEnabled, Is.False);
            Assert.That(restored.settings.orbitLocked, Is.True);
        }

        [Test]
        public void Snapshot_DeepCopiesNestedState_NoSharedReference()
        {
            var state = GameStateFactory.CreateNew(rules);
            var dto = SaveSerializer.Snapshot(state, "prototype-2", "x", 1);

            // Mutate original after snapshot – should not affect DTO
            state.run.water = 9999;
            state.vitality.bankSeconds = 9999;

            Assert.That(dto.run.water, Is.Not.EqualTo(9999));
            Assert.That(dto.vitality.bankSeconds, Is.Not.EqualTo(9999));
        }

        // ── Validator ─────────────────────────────────────────────────

        [Test]
        public void Validate_ValidNewGameDto_ReturnsEmpty()
        {
            var state = GameStateFactory.CreateNew(rules);
            var dto = SaveSerializer.Snapshot(state, "prototype-2", "id1", 1);
            var errors = SaveValidator.Validate(dto, rules);
            Assert.That(errors, Is.Empty, string.Join("; ", errors));
        }

        [Test]
        public void Validate_NullDto_ReturnsError()
        {
            var errors = SaveValidator.Validate(null, rules);
            Assert.That(errors, Is.Not.Empty);
        }

        [Test]
        public void Validate_UnknownStageId_ReturnsError()
        {
            var state = GameStateFactory.CreateNew(rules);
            var dto = SaveSerializer.Snapshot(state, "prototype-2", "id1", 1);
            dto.run.stageId = "unknown_stage";
            var errors = SaveValidator.Validate(dto, rules);
            Assert.That(errors, Is.Not.Empty);
        }

        [Test]
        public void Validate_NegativeWater_ReturnsError()
        {
            var state = GameStateFactory.CreateNew(rules);
            var dto = SaveSerializer.Snapshot(state, "prototype-2", "id1", 1);
            dto.run.water = -1;
            var errors = SaveValidator.Validate(dto, rules);
            Assert.That(errors, Is.Not.Empty);
        }

        [Test]
        public void Validate_NanGrowth_ReturnsError()
        {
            var state = GameStateFactory.CreateNew(rules);
            var dto = SaveSerializer.Snapshot(state, "prototype-2", "id1", 1);
            dto.run.growth = double.NaN;
            var errors = SaveValidator.Validate(dto, rules);
            Assert.That(errors, Is.Not.Empty);
        }

        [Test]
        public void Validate_LevelExceedsCap_ReturnsError()
        {
            var state = GameStateFactory.CreateNew(rules);
            var dto = SaveSerializer.Snapshot(state, "prototype-2", "id1", 1);
            dto.run.levels.root = 999; // far exceeds seedling cap of 5
            var errors = SaveValidator.Validate(dto, rules);
            Assert.That(errors, Is.Not.Empty);
        }

        [Test]
        public void Validate_EquippedCosmeticNotOwned_ReturnsError()
        {
            var state = GameStateFactory.CreateNew(rules);
            var dto = SaveSerializer.Snapshot(state, "prototype-2", "id1", 1);
            dto.permanent.equippedCosmeticId = "glow_placeholder"; // not in ownedCosmeticIds
            var errors = SaveValidator.Validate(dto, rules);
            Assert.That(errors, Is.Not.Empty);
        }

        [Test]
        public void Validate_TapProgressAtMax_ReturnsError()
        {
            var state = GameStateFactory.CreateNew(rules);
            var dto = SaveSerializer.Snapshot(state, "prototype-2", "id1", 1);
            dto.vitality.tapProgress = rules.tapsPerReward; // must be in [0, tapsPerReward-1]
            var errors = SaveValidator.Validate(dto, rules);
            Assert.That(errors, Is.Not.Empty);
        }

        [Test]
        public void Validate_BankSecondsOverCap_ReturnsError()
        {
            var state = GameStateFactory.CreateNew(rules);
            var dto = SaveSerializer.Snapshot(state, "prototype-2", "id1", 1);
            dto.vitality.bankSeconds = rules.vitalityMaxBankSeconds + 1;
            var errors = SaveValidator.Validate(dto, rules);
            Assert.That(errors, Is.Not.Empty);
        }

        [Test]
        public void Validate_SchemaVersionTooNew_ReturnsError()
        {
            var state = GameStateFactory.CreateNew(rules);
            var dto = SaveSerializer.Snapshot(state, "prototype-2", "id1", 1);
            dto.schemaVersion = 9999;
            var errors = SaveValidator.Validate(dto, rules);
            Assert.That(errors, Is.Not.Empty);
        }

        // ── SaveService ───────────────────────────────────────────────

        [Test]
        public void SaveService_NewGame_WhenNoFileExists()
        {
            var svc = new SaveService(rules, tempDir);
            var (outcome, result) = svc.Load();
            Assert.That(outcome, Is.EqualTo(SaveService.LoadOutcome.NewGame));
            Assert.That(result.Success, Is.True);
        }

        [Test]
        public void SaveService_Roundtrip_SaveAndLoad()
        {
            var svc = new SaveService(rules, tempDir);
            var state = GameStateFactory.CreateNew(rules);
            state.run.water = 99;
            state.vitality.bankSeconds = 300;

            string saveId = "test-save";
            long revision = 0;
            var saveResult = svc.Save(state, saveId, ref revision);
            Assert.That(saveResult.Success, Is.True, saveResult.Reason);
            Assert.That(revision, Is.EqualTo(1));

            var (outcome, loadResult) = svc.Load();
            Assert.That(outcome, Is.EqualTo(SaveService.LoadOutcome.Loaded));
            Assert.That(loadResult.Success, Is.True);
            Assert.That(loadResult.Data.run.water, Is.EqualTo(99).Within(1e-9));
            Assert.That(loadResult.Data.vitality.bankSeconds, Is.EqualTo(300).Within(1e-9));
        }

        [Test]
        public void SaveService_RevisionIncrementsEachSave()
        {
            var svc = new SaveService(rules, tempDir);
            var state = GameStateFactory.CreateNew(rules);
            string id = "x"; long rev = 0;
            svc.Save(state, id, ref rev);
            Assert.That(rev, Is.EqualTo(1));
            svc.Save(state, id, ref rev);
            Assert.That(rev, Is.EqualTo(2));
        }

        [Test]
        public void SaveService_CorruptMain_LoadsFromBackup()
        {
            var svc = new SaveService(rules, tempDir);
            var state = GameStateFactory.CreateNew(rules);
            state.run.water = 55;
            string id = "x"; long rev = 0;

            // Save once to create both main and backup files
            svc.Save(state, id, ref rev); // creates main
            svc.Save(state, id, ref rev); // creates backup (from previous main) + new main

            // Corrupt the main file
            File.WriteAllText(svc.MainPath, "{invalid}");

            var (outcome, result) = svc.Load();
            Assert.That(outcome, Is.EqualTo(SaveService.LoadOutcome.LoadedFromBackup));
            Assert.That(result.Success, Is.True);
        }

        [Test]
        public void SaveService_CorruptBothFiles_ReturnsCorruptNoBackup()
        {
            var svc = new SaveService(rules, tempDir);
            var state = GameStateFactory.CreateNew(rules);
            string id = "x"; long rev = 0;
            svc.Save(state, id, ref rev);
            svc.Save(state, id, ref rev);

            File.WriteAllText(svc.MainPath, "bad json");
            File.WriteAllText(svc.BackupPath, "also bad");

            var (outcome, result) = svc.Load();
            Assert.That(outcome, Is.EqualTo(SaveService.LoadOutcome.CorruptNoBackup));
            Assert.That(result.Success, Is.False);
        }

        [Test]
        public void SaveService_VersionTooNew_DoesNotAllowLoad()
        {
            var svc = new SaveService(rules, tempDir);
            var state = GameStateFactory.CreateNew(rules);
            string id = "x"; long rev = 0;
            svc.Save(state, id, ref rev);

            // Manually patch schemaVersion to a future version
            string json = File.ReadAllText(svc.MainPath);
            json = json.Replace("\"schemaVersion\":1", "\"schemaVersion\":9999");
            File.WriteAllText(svc.MainPath, json);

            var (outcome, _) = svc.Load();
            Assert.That(outcome, Is.EqualTo(SaveService.LoadOutcome.VersionTooNew));
        }

        [Test]
        public void SaveService_PreviewAndCoroutineNotInSave_NoUnityReferences()
        {
            // Validate that SaveData is a plain serializable type (no MB, no GO, no Transform)
            var state = GameStateFactory.CreateNew(rules);
            var dto = SaveSerializer.Snapshot(state, "prototype-2", "id", 1);
            string json = JsonUtility.ToJson(dto, prettyPrint: false);

            // Should not contain any Unity reference types
            Assert.That(json, Does.Not.Contain("instanceID"));
            Assert.That(json, Does.Not.Contain("GameObject"));
            Assert.That(json, Does.Not.Contain("Transform"));
            Assert.That(json, Does.Not.Contain("Coroutine"));
        }

        [Test]
        public void SaveService_MissingFieldsGetDefaults_AfterRestore()
        {
            // Simulate a partial save (missing vitality and settings – as if future schema dropped fields)
            var minimalJson = "{\"schemaVersion\":1,\"configVersion\":\"prototype-2\",\"saveId\":\"min-1\",\"revision\":1," +
                "\"savedAtUtcSeconds\":0,\"offlineCheckpointUtcSeconds\":0," +
                "\"run\":{\"runId\":\"abc\",\"life\":\"ALIVE\",\"stageId\":\"seedling\",\"water\":10,\"growth\":0,\"hp\":100," +
                "\"levels\":{\"root\":0,\"leaves\":0,\"trunk\":0}," +
                "\"weather\":{\"weatherId\":\"calm\",\"remainingOnlineSeconds\":120,\"badStreak\":0,\"recentWeatherIds\":[],\"incidentCooldownOnlineSeconds\":0}," +
                "\"status\":{\"statusId\":\"none\",\"remainingOnlineSeconds\":0,\"impactRegionId\":\"none\"},\"runCounters\":[]}," +
                "\"permanent\":{\"stagePoints\":[{\"stageId\":\"seedling\",\"points\":0},{\"stageId\":\"young_tree\",\"points\":0},{\"stageId\":\"large_tree\",\"points\":0}]," +
                "\"coin\":0,\"achievements\":[],\"ownedCosmeticIds\":[],\"equippedCosmeticId\":\"none\"," +
                "\"tutorialCompletedStepIds\":[],\"rebirthCount\":0,\"highestStageEverId\":\"seedling\",\"permanentCounters\":[]}," +
                "\"vitality\":{\"tapProgress\":0,\"bankSeconds\":0}," +
                "\"settings\":{\"musicEnabled\":true,\"orbitLocked\":false}}";

            var dto = JsonUtility.FromJson<SaveData>(minimalJson);
            Assert.That(dto, Is.Not.Null);
            var errors = SaveValidator.Validate(dto, rules);
            Assert.That(errors, Is.Empty, string.Join("; ", errors));

            var restored = GameStateFactory.CreateNew(rules);
            SaveSerializer.Restore(dto, restored);
            Assert.That(restored.settings.musicEnabled, Is.True);
            Assert.That(restored.vitality.tapProgress, Is.EqualTo(0));
        }

        [Test]
        public void SaveService_MutipleQuickSaves_DoNotDoubleSave()
        {
            var svc = new SaveService(rules, tempDir);
            long clock = 1000;
            svc.UtcNowSeconds = () => ++clock;
            var state = GameStateFactory.CreateNew(rules);
            string id = "x"; long rev = 0;

            for (int i = 0; i < 5; i++) svc.Save(state, id, ref rev);

            Assert.That(rev, Is.EqualTo(5));
            var (_, loadResult) = svc.Load();
            Assert.That(loadResult.Data.revision, Is.EqualTo(5));
        }

        [Test]
        public void SaveService_DeleteFiles_ClearsAll()
        {
            var svc = new SaveService(rules, tempDir);
            var state = GameStateFactory.CreateNew(rules);
            string id = "x"; long rev = 0;
            svc.Save(state, id, ref rev);
            svc.Save(state, id, ref rev); // creates backup too

            Assert.That(File.Exists(svc.MainPath), Is.True);
            svc.DeleteSaveFiles();
            Assert.That(File.Exists(svc.MainPath), Is.False);
            Assert.That(File.Exists(svc.BackupPath), Is.False);
        }

        [Test]
        public void LegacyBootstrapGrantIsNotRepeatedAfterConfigSixSave()
        {
            var state=GameStateFactory.CreateNew(rules);state.run.growth=0;state.run.photosynthesis=0;
            var dto=SaveSerializer.Snapshot(state,"prototype-4","legacy-grant",1);
            string json=SaveCodec.Encode(dto);
            foreach(string field in new[]{"photosynthesis","spirit","spiritAbsorptionUnlocked","spiritAbsorption"})
                json=System.Text.RegularExpressions.Regex.Replace(json,",\""+field+"\":(?:false|0(?:\\.0)?)","");
            var old=SaveCodec.Decode(json,rules);Assert.That(old.Success,Is.True,old.Reason);
            Assert.That(old.Data.run.photosynthesis,Is.EqualTo(5));Assert.That(old.Data.run.growth,Is.EqualTo(10));
            old.Data.configVersion=rules.configVersion;old.Data.run.photosynthesis=0;old.Data.run.growth=0;
            var current=SaveCodec.Decode(SaveCodec.Encode(old.Data),rules);Assert.That(current.Success,Is.True,current.Reason);
            Assert.That(current.Data.run.photosynthesis,Is.Zero);Assert.That(current.Data.run.growth,Is.Zero);
            json=System.Text.RegularExpressions.Regex.Replace(SaveCodec.Encode(current.Data),",\"photosynthesis\":0(?:\\.0)?","");
            Assert.That(SaveCodec.Decode(json,rules).Success,Is.False);
        }
        [Test]
        public void MatureSpiritRoundtripValidatesStageAndUnlock()
        {
            var state=GameStateFactory.CreateNew(rules);state.run.stageId="mature_tree";state.run.spiritAbsorptionUnlocked=true;state.run.levels.spiritAbsorption=2;state.run.spirit=7;
            var dto=SaveSerializer.Snapshot(state,rules.configVersion,"spirit-save",1);
            var loaded=SaveCodec.Decode(SaveCodec.Encode(dto),rules);Assert.That(loaded.Success,Is.True,loaded.Reason);
            Assert.That(loaded.Data.run.spirit,Is.EqualTo(7));Assert.That(loaded.Data.run.levels.spiritAbsorption,Is.EqualTo(2));
            dto.run.stageId="large_tree";Assert.That(SaveValidator.Validate(dto,rules),Is.Not.Empty);
            dto.run.stageId="mature_tree";dto.run.spiritAbsorptionUnlocked=false;Assert.That(SaveValidator.Validate(dto,rules),Is.Not.Empty);
        }
    }
}
