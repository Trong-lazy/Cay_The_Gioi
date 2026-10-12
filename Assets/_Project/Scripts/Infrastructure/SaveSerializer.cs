using System;
using System.Collections.Generic;
using CayTheGioi.Domain;

namespace CayTheGioi.Infrastructure
{
    /// <summary>
    /// Snapshot mapping between live GameState and SaveData DTO.
    /// Pure data conversion – no file I/O, no Unity references.
    /// </summary>
    public static class SaveSerializer
    {
        public const int CurrentSchemaVersion = 1;
        public static GameState Clone(GameState source)
        {
            var copy=new GameState();
            Restore(Snapshot(source,"clone","clone",0),copy);
            return copy;
        }

        // ── Snapshot ──────────────────────────────────────────────────

        /// <summary>Creates a snapshot DTO from the live state. Deep-copies all nested data.</summary>
        public static SaveData Snapshot(GameState state, string configVersion, string saveId, long revision)
        {
            if (state == null) throw new ArgumentNullException(nameof(state));
            if (string.IsNullOrWhiteSpace(configVersion)) throw new ArgumentException("configVersion required.", nameof(configVersion));
            if (string.IsNullOrWhiteSpace(saveId)) throw new ArgumentException("saveId required.", nameof(saveId));

            var dto = new SaveData
            {
                schemaVersion = CurrentSchemaVersion,
                configVersion = configVersion,
                saveId = saveId,
                revision = revision,
                savedAtUtcSeconds = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                offlineCheckpointUtcSeconds = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            };

            // RunState deep-copy
            dto.run = CopyRun(state.run);

            // PermanentState deep-copy
            dto.permanent = CopyPermanent(state.permanent);

            // VitalityState
            dto.vitality = new VitalityState
            {
                tapProgress = state.vitality.tapProgress,
                bankSeconds = state.vitality.bankSeconds
            };

            // SettingsState
            dto.settings = new SettingsState
            {
                musicEnabled = state.settings.musicEnabled,
                orbitLocked = state.settings.orbitLocked
            };

            return dto;
        }

        /// <summary>Updates the checkpoint timestamp on an existing DTO without touching revision.</summary>
        public static void StampCheckpoint(SaveData dto, long utcSeconds)
        {
            if (dto == null) throw new ArgumentNullException(nameof(dto));
            dto.offlineCheckpointUtcSeconds = utcSeconds;
        }

        // ── Restore ───────────────────────────────────────────────────

        /// <summary>
        /// Applies a validated DTO back into <paramref name="state"/>.
        /// Call <see cref="SaveValidator.Validate"/> before calling this.
        /// </summary>
        public static void Restore(SaveData dto, GameState state)
        {
            if (dto == null) throw new ArgumentNullException(nameof(dto));
            if (state == null) throw new ArgumentNullException(nameof(state));

            state.run = CopyRun(dto.run);
            state.permanent = CopyPermanent(dto.permanent);
            state.vitality = new VitalityState { tapProgress = dto.vitality.tapProgress, bankSeconds = dto.vitality.bankSeconds };
            state.settings = new SettingsState { musicEnabled = dto.settings.musicEnabled, orbitLocked = dto.settings.orbitLocked };
        }

        // ── Deep copy helpers ─────────────────────────────────────────

        private static RunState CopyRun(RunState src)
        {
            if (src == null) return new RunState();
            return new RunState
            {
                runId = src.runId,
                life = src.life,
                stageId = src.stageId,
                water = src.water,
                photosynthesis = src.photosynthesis,
                growth = src.growth,
                spirit = src.spirit,
                spiritAbsorptionUnlocked = src.spiritAbsorptionUnlocked,
                hp = src.hp,
                levels = src.levels == null ? new LevelState() : new LevelState { root = src.levels.root, leaves = src.levels.leaves, trunk = src.levels.trunk, spiritAbsorption = src.levels.spiritAbsorption },
                weather = CopyWeather(src.weather),
                status = CopyStatus(src.status),
                runCounters = src.runCounters == null ? new List<CounterEntry>() : CopyCounters(src.runCounters)
            };
        }

        private static List<CounterEntry> CopyCounters(List<CounterEntry> source)
        {
            var copy = new List<CounterEntry>();
            foreach (var entry in source) copy.Add(new CounterEntry { counterId = entry.counterId, value = entry.value });
            return copy;
        }

        private static WeatherState CopyWeather(WeatherState src)
        {
            if (src == null) return new WeatherState();
            return new WeatherState
            {
                weatherId = src.weatherId,
                remainingOnlineSeconds = src.remainingOnlineSeconds,
                badStreak = src.badStreak,
                recentWeatherIds = src.recentWeatherIds == null ? new List<string>() : new List<string>(src.recentWeatherIds),
                incidentCooldownOnlineSeconds = src.incidentCooldownOnlineSeconds,
                strikeEvaluated = src.strikeEvaluated,
                strikeWillHit = src.strikeWillHit,
                strikeDelaySeconds = src.strikeDelaySeconds,
                strikeExecuted = src.strikeExecuted
            };
        }

        private static StatusState CopyStatus(StatusState src)
        {
            if (src == null) return new StatusState();
            return new StatusState
            {
                statusId = src.statusId,
                remainingOnlineSeconds = src.remainingOnlineSeconds,
                impactRegionId = src.impactRegionId
            };
        }

        private static PermanentState CopyPermanent(PermanentState src)
        {
            if (src == null) return new PermanentState();
            var dst = new PermanentState
            {
                coin = src.coin,
                equippedCosmeticId = src.equippedCosmeticId,
                rebirthCount = src.rebirthCount,
                highestStageEverId = src.highestStageEverId,
                stagePoints = new List<StagePointEntry>(),
                achievements = new List<AchievementEntry>(),
                ownedCosmeticIds = new List<string>(),
                tutorialCompletedStepIds = new List<string>(),
                permanentCounters = new List<CounterEntry>()
            };
            if (src.stagePoints != null) foreach (var e in src.stagePoints) dst.stagePoints.Add(new StagePointEntry { stageId = e.stageId, points = e.points });
            if (src.achievements != null) foreach (var e in src.achievements) dst.achievements.Add(new AchievementEntry { achievementId = e.achievementId, progress = e.progress, claimed = e.claimed });
            if (src.ownedCosmeticIds != null) dst.ownedCosmeticIds.AddRange(src.ownedCosmeticIds);
            if (src.tutorialCompletedStepIds != null) dst.tutorialCompletedStepIds.AddRange(src.tutorialCompletedStepIds);
            if (src.permanentCounters != null) foreach (var e in src.permanentCounters) dst.permanentCounters.Add(new CounterEntry { counterId = e.counterId, value = e.value });
            return dst;
        }
    }
}
