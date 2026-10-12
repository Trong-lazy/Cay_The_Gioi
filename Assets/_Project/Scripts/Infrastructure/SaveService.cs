using System;
using System.IO;
using CayTheGioi.Domain;
using UnityEngine;

namespace CayTheGioi.Infrastructure
{
    /// <summary>
    /// Outcome of a save or load operation.
    /// </summary>
    public sealed class SaveResult
    {
        public readonly bool Success;
        public readonly string Reason;
        public readonly SaveData Data;

        public SaveResult(bool success, string reason = "", SaveData data = null)
        {
            Success = success;
            Reason = reason ?? "";
            Data = data;
        }

        public static SaveResult Ok(SaveData data = null) => new SaveResult(true, "", data);
        public static SaveResult Fail(string reason) => new SaveResult(false, reason);
    }

    // Same-directory temp/flush/validation followed by atomic replacement with backup.
    // Unsupported versions/configs and unconfirmed recovery are never overwritten.
    public enum SaveWritePhase { AfterFlush, BeforeReplace, AfterReplace }

    public sealed class SaveService
    {
        private readonly string mainPath;
        private readonly string backupPath;
        private readonly string tmpPath;
        private readonly PrototypeRules rules;
        private long checkpoint;
        private bool recoveryAccepted;
        public Action<SaveWritePhase> FaultInjection;
#if UNITY_EDITOR
        // Editor validation only: a normal player launch always uses persistentDataPath (QA args: see QaLaunchOptions).
        public static Func<long> ValidationClock;
        // SessionState survives Editor domain reload; only active with an isolated validation directory.
        public static long? ValidationUtcSeconds
        {
            get { long value; return long.TryParse(UnityEditor.SessionState.GetString("CayTheGioi.ValidationUtc", ""), out value) ? (long?)value : null; }
            set { UnityEditor.SessionState.SetString("CayTheGioi.ValidationUtc", value.HasValue ? value.Value.ToString(System.Globalization.CultureInfo.InvariantCulture) : ""); }
        }
        public static string ValidationDirectory
        {
            get { string value = UnityEditor.SessionState.GetString("CayTheGioi.ValidationDirectory", ""); return string.IsNullOrEmpty(value) ? null : value; }
            set { UnityEditor.SessionState.SetString("CayTheGioi.ValidationDirectory", value ?? ""); }
        }
#endif

        // Injectable clock for testing – default is real UTC.
        public Func<long> UtcNowSeconds = () => DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        public const string SaveFileName = "world_tree_save.json";
        public const string BackupFileName = "world_tree_save.backup.json";

        public SaveService(PrototypeRules rules, string overrideDirectory = null)
        {
            if (rules == null) throw new ArgumentNullException(nameof(rules));
            this.rules = rules;
#if UNITY_EDITOR
            if (ValidationClock != null) UtcNowSeconds = ValidationClock;
            else if (ValidationDirectory != null && ValidationUtcSeconds.HasValue) UtcNowSeconds = () => ValidationUtcSeconds ?? DateTimeOffset.UtcNow.ToUnixTimeSeconds();
#else
            // Player builds: only explicit QA launch arguments (T09) change clock/directory.
            if (QaLaunchOptions.QaUtcNow().HasValue) UtcNowSeconds = () => QaLaunchOptions.QaUtcNow() ?? DateTimeOffset.UtcNow.ToUnixTimeSeconds();
#endif
            string dir = overrideDirectory;
#if UNITY_EDITOR
            dir = dir ?? ValidationDirectory;
#else
            if (QaLaunchOptions.Active) dir = dir ?? QaLaunchOptions.SaveDirectory;
#endif
            dir = dir ?? Application.persistentDataPath;
            mainPath = Path.Combine(dir, SaveFileName);
            backupPath = Path.Combine(dir, BackupFileName);
            tmpPath = Path.Combine(dir, SaveFileName + ".tmp");
        }

        // Expose paths for testing / debug.
        public long CheckpointUtcSeconds => checkpoint;
        public string MainPath => mainPath;
        public string BackupPath => backupPath;

        // ── Save ─────────────────────────────────────────────────────

        /// <summary>
        /// Writes a consistent snapshot of <paramref name="state"/> to disk using the safe write protocol.
        /// On success returns Ok with the DTO that was written.
        /// On failure the original file is unchanged.
        /// </summary>
        public SaveResult Save(GameState state, string saveId, ref long revision, long? checkpointOverride = null)
        {
            if (state == null) return SaveResult.Fail("NULL_STATE");
            try
            {
                var loaded = Load();
                if (!loaded.result.Success && loaded.outcome != LoadOutcome.CorruptNoBackup)
                    return loaded.result;
                if (!loaded.result.Success || (loaded.outcome == LoadOutcome.LoadedFromBackup && !recoveryAccepted))
                    return SaveResult.Fail("RECOVERY_REQUIRED");
                long nextRevision = checked(revision + 1);
                var dto = SaveSerializer.Snapshot(state, rules.configVersion, saveId, nextRevision);
                dto.offlineCheckpointUtcSeconds = Math.Max(checkpoint, Math.Max(0, checkpointOverride ?? UtcNowSeconds()));
                dto.savedAtUtcSeconds = dto.offlineCheckpointUtcSeconds;
                var errors = SaveValidator.Validate(dto, rules);
                if (errors.Count > 0) return SaveResult.Fail(string.Join("; ", errors));
                string json = SaveCodec.Encode(dto);
                Directory.CreateDirectory(Path.GetDirectoryName(tmpPath));
                byte[] bytes = System.Text.Encoding.UTF8.GetBytes(json);
                using (var stream = new FileStream(tmpPath, FileMode.Create, FileAccess.Write, FileShare.None))
                { stream.Write(bytes, 0, bytes.Length); stream.Flush(true); }
                FaultInjection?.Invoke(SaveWritePhase.AfterFlush);
                var verify = SaveCodec.Decode(File.ReadAllText(tmpPath), rules);
                if (!verify.Success || File.ReadAllText(tmpPath) != json)
                { CleanTmp(); return SaveResult.Fail("VERIFY_FAILED"); }
                if (loaded.outcome == LoadOutcome.LoadedFromBackup && File.Exists(mainPath))
                    File.Copy(mainPath, mainPath + ".corrupt-" + Guid.NewGuid().ToString("N"));
                FaultInjection?.Invoke(SaveWritePhase.BeforeReplace);
                if (File.Exists(mainPath))
                    File.Replace(tmpPath, mainPath, loaded.outcome == LoadOutcome.Loaded ? backupPath : null);
                else File.Move(tmpPath, mainPath);
                // Replacement is the commit point. A later diagnostic failure cannot undo it.
                revision = nextRevision;
                checkpoint = dto.offlineCheckpointUtcSeconds;
                recoveryAccepted = false;
                try { FaultInjection?.Invoke(SaveWritePhase.AfterReplace); } catch { }
                return SaveResult.Ok(dto);
            }
            catch (Exception ex)
            {
                CleanTmp();
                return SaveResult.Fail("IO_ERROR: " + ex.Message);
            }
        }

        // ── Load ─────────────────────────────────────────────────────

        public enum LoadOutcome
        {
            NewGame,         // No save file found → start fresh.
            Loaded,          // Main file loaded and validated.
            LoadedFromBackup,// Main corrupt, backup used.
            CorruptNoBackup, // Both corrupt or absent after main was found.
            VersionTooNew,   // schemaVersion > supported → must not overwrite.
            IncompatibleConfig,
            IoError
        }

        /// <summary>
        /// Loads save data from disk.
        /// Returns the outcome and, when successful, the validated DTO ready for Restore().
        /// </summary>
        public (LoadOutcome outcome, SaveResult result) Load()
        {
            if (!File.Exists(mainPath) && !File.Exists(backupPath))
                return (LoadOutcome.NewGame, SaveResult.Ok());
            var main = File.Exists(mainPath) ? Read(mainPath) : SaveResult.Fail("MISSING_MAIN");
            if (main.Success)
            { checkpoint = main.Data.offlineCheckpointUtcSeconds; return (LoadOutcome.Loaded, main); }
            if (main.Reason.StartsWith("VERSION_TOO_NEW") || main.Reason.StartsWith("UNSUPPORTED_SCHEMA"))
                return (LoadOutcome.VersionTooNew, main);
            if (main.Reason.StartsWith("CONFIG_INCOMPATIBLE")) return (LoadOutcome.IncompatibleConfig, main);
            if (main.Reason.StartsWith("IO_ERROR")) return (LoadOutcome.IoError, main);
            var backup = File.Exists(backupPath) ? Read(backupPath) : SaveResult.Fail("MISSING_BACKUP");
            if (backup.Success)
            { checkpoint = backup.Data.offlineCheckpointUtcSeconds; return (LoadOutcome.LoadedFromBackup, backup); }
            if (backup.Reason.StartsWith("IO_ERROR")) return (LoadOutcome.IoError, backup);
            return (LoadOutcome.CorruptNoBackup, SaveResult.Fail("CORRUPT_NO_BACKUP: " + main.Reason + "; " + backup.Reason));
        }

        public void AcceptRecovery() { recoveryAccepted = true; }

        public SaveResult ArchiveForReset()
        {
            try
            {
                string suffix = ".reset-" + Guid.NewGuid().ToString("N");
                foreach (var path in new[] { mainPath, backupPath })
                    if (File.Exists(path)) File.Copy(path, path + suffix);
                foreach (var path in new[] { mainPath, backupPath, tmpPath })
                    if (File.Exists(path)) File.Delete(path);
                checkpoint = 0;
                return SaveResult.Ok();
            }
            catch (Exception ex) { return SaveResult.Fail("RESET_FAILED: " + ex.Message); }
        }

        private SaveResult Read(string path)
        {
            try { return SaveCodec.Decode(File.ReadAllText(path), rules); }
            catch (Exception ex) { return SaveResult.Fail("IO_ERROR: " + ex.Message); }
        }

        // ── Delete (tests / reset) ────────────────────────────────────

        public void DeleteSaveFiles()
        {
            try { if (File.Exists(mainPath)) File.Delete(mainPath); } catch { }
            try { if (File.Exists(backupPath)) File.Delete(backupPath); } catch { }
            try { if (File.Exists(tmpPath)) File.Delete(tmpPath); } catch { }
        }

        // ── Private helpers ───────────────────────────────────────────

        private void CleanTmp()
        {
            try { if (File.Exists(tmpPath)) File.Delete(tmpPath); } catch { }
        }
    }
}
