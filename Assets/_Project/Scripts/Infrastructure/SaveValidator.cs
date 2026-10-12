using System;
using System.Collections.Generic;
using CayTheGioi.Domain;

namespace CayTheGioi.Infrastructure
{
    /// <summary>
    /// Validates a SaveData DTO against confirmed rules and the supplied PrototypeRules.
    /// Returns a list of human-readable error strings; empty list means valid.
    /// </summary>
    public static class SaveValidator
    {
        private static readonly HashSet<string> ValidLifeValues = new HashSet<string>(StringComparer.Ordinal) { "ALIVE", "DEAD" };
        private static readonly HashSet<string> ValidStatusIds = new HashSet<string>(StringComparer.Ordinal) { "none", "burning" };

        public static List<string> Validate(SaveData dto, PrototypeRules rules)
        {
            var errors = new List<string>();
            if (dto == null) { errors.Add("DTO is null."); return errors; }
            if (rules == null) { errors.Add("Rules are null."); return errors; }

            // Envelope
            if (dto.schemaVersion < 1) errors.Add($"schemaVersion {dto.schemaVersion} is not supported.");
            if (dto.schemaVersion > SaveSerializer.CurrentSchemaVersion) errors.Add($"Save schemaVersion {dto.schemaVersion} is newer than supported {SaveSerializer.CurrentSchemaVersion}. Will not overwrite.");
            if (string.IsNullOrWhiteSpace(dto.saveId)) errors.Add("saveId is missing.");
            if (dto.revision < 0) errors.Add("revision must be non-negative.");

            if (errors.Count > 0) return errors; // stop early – envelope invalid

            if (string.IsNullOrWhiteSpace(dto.configVersion)) errors.Add("configVersion is missing.");
            if (dto.savedAtUtcSeconds < 0 || dto.offlineCheckpointUtcSeconds < 0) errors.Add("Invalid timestamp.");
            if (dto.settings == null) errors.Add("settings is null.");
            // Run
            var r = dto.run;
            if (r == null) { errors.Add("run is null."); return errors; }
            if (string.IsNullOrWhiteSpace(r.runId)) errors.Add("run.runId is missing.");
            if (!ValidLifeValues.Contains(r.life ?? "")) errors.Add($"run.life '{r.life}' is not valid (ALIVE|DEAD).");

            // Validate stageId against known stages
            bool knownStage = false;
            if (rules.stages != null)
                foreach (var s in rules.stages)
                    if (s != null && s.stageId == r.stageId) { knownStage = true; break; }
            if (!knownStage) errors.Add($"run.stageId '{r.stageId}' not found in config.");

            if (!Finite(r.water) || r.water < 0) errors.Add("run.water must be finite and non-negative.");
            if (!Finite(r.photosynthesis) || r.photosynthesis < 0) errors.Add("run.photosynthesis must be finite and non-negative.");
            if (!Finite(r.growth) || r.growth < 0) errors.Add("run.growth must be finite and non-negative.");
            if (!Finite(r.spirit) || r.spirit < 0) errors.Add("run.spirit must be finite and non-negative.");
            if (r.levels != null && (r.levels.spiritAbsorption < 0 || (r.spiritAbsorptionUnlocked && r.levels.spiritAbsorption==0) || (!r.spiritAbsorptionUnlocked && (r.levels.spiritAbsorption != 0 || r.spirit != 0))))
                errors.Add("Spirit absorption state is inconsistent.");
            if (!Finite(r.hp) || r.hp < 0) errors.Add("run.hp must be finite and non-negative.");
            if (r.hp > rules.maxHp + 1e-6) errors.Add($"run.hp {r.hp} exceeds maxHp {rules.maxHp}.");

            if ((r.life == "ALIVE" && r.hp <= 0) || (r.life == "DEAD" && r.hp != 0)) errors.Add("HP/life inconsistent.");
            if (r.weather == null) errors.Add("weather is null.");
            else
            {
                var validWeather = new HashSet<string> { "calm", "sunny", "rainy", "thunderstorm" };
                if (!validWeather.Contains(r.weather.weatherId ?? "")) errors.Add("Unknown weather ID.");
                if (!Finite(r.weather.remainingOnlineSeconds) || r.weather.remainingOnlineSeconds < 0 || !Finite(r.weather.incidentCooldownOnlineSeconds) || r.weather.incidentCooldownOnlineSeconds < 0 || r.weather.badStreak < 0) errors.Add("Invalid weather values.");
                if (!Finite(r.weather.strikeDelaySeconds) || r.weather.strikeDelaySeconds < 0) errors.Add("run.weather.strikeDelaySeconds must be finite and non-negative.");
                if (!r.weather.strikeEvaluated)
                {
                    if (r.weather.strikeWillHit) errors.Add("strikeWillHit cannot be true when strikeEvaluated is false.");
                    if (r.weather.strikeExecuted) errors.Add("strikeExecuted cannot be true when strikeEvaluated is false.");
                }
                if (r.weather.strikeExecuted && !r.weather.strikeWillHit)
                {
                    errors.Add("strikeExecuted cannot be true when strikeWillHit is false.");
                }
                if (r.weather.recentWeatherIds == null) errors.Add("Missing weather history.");
                else foreach (var id in r.weather.recentWeatherIds)
                    if (!validWeather.Contains(id ?? "")) errors.Add("Unknown weather history ID.");
            }
            if (r.status == null) errors.Add("status is null.");
            if (r.status != null && !new HashSet<string> { "none", "root", "leaves", "trunk" }.Contains(r.status.impactRegionId ?? "")) errors.Add("Unknown impact region.");
            ValidateEntries(r.runCounters, e => e.counterId, e => e.value >= 0, errors);
            // Level caps per stage
            if (r.levels != null)
            {
                var stage = rules.FindStage(r.stageId);
                if (stage != null)
                {
                    if (r.levels.root < 0 || r.levels.root > stage.rootCap) errors.Add($"run.levels.root {r.levels.root} out of range [0,{stage.rootCap}].");
                    if (r.levels.leaves < 0 || r.levels.leaves > stage.leavesCap) errors.Add($"run.levels.leaves {r.levels.leaves} out of range [0,{stage.leavesCap}].");
                    if (r.levels.trunk < 0 || r.levels.trunk > stage.trunkCap) errors.Add($"run.levels.trunk {r.levels.trunk} out of range [0,{stage.trunkCap}].");
                    if (r.levels.spiritAbsorption > stage.spiritAbsorptionCap || (r.spiritAbsorptionUnlocked && stage.spiritAbsorptionCap == 0))
                        errors.Add("Spirit absorption is unavailable at the saved stage.");
                }
            }
            else errors.Add("run.levels is null.");

            // Status
            if (r.status != null)
            {
                if (!ValidStatusIds.Contains(r.status.statusId ?? "")) errors.Add($"run.status.statusId '{r.status.statusId}' unknown.");
                if (!Finite(r.status.remainingOnlineSeconds) || r.status.remainingOnlineSeconds < 0) errors.Add("run.status.remainingOnlineSeconds invalid.");
            }

            // Permanent
            var p = dto.permanent;
            if (p == null) { errors.Add("permanent is null."); }
            else
            {
                if (p.coin < 0) errors.Add("permanent.coin must be non-negative.");
                if (p.rebirthCount < 0) errors.Add("permanent.rebirthCount must be non-negative.");
                // highestStageEverId must be a known stage
                bool knownHighest = false;
                if (rules.stages != null)
                    foreach (var s in rules.stages)
                        if (s != null && s.stageId == p.highestStageEverId) { knownHighest = true; break; }
                if (!knownHighest) errors.Add($"permanent.highestStageEverId '{p.highestStageEverId}' not found in config.");

                ValidateEntries(p.stagePoints, e => e.stageId, e => rules.FindStage(e.stageId) != null && e.points >= 0, errors);
                ValidateEntries(p.achievements, e => e.achievementId, e => e.progress >= 0, errors);
                ValidateEntries(p.permanentCounters, e => e.counterId, e => e.value >= 0, errors);
                ValidateEntries(p.ownedCosmeticIds, e => e, e => true, errors);
                ValidateEntries(p.tutorialCompletedStepIds, e => e, e => true, errors);
                if (string.IsNullOrWhiteSpace(p.equippedCosmeticId)) errors.Add("Missing equipped ID.");
                // Cosmetic equipped must be owned or "none"
                if ((p.equippedCosmeticId ?? "") != "none" && (p.ownedCosmeticIds == null || !p.ownedCosmeticIds.Contains(p.equippedCosmeticId)))
                    errors.Add($"permanent.equippedCosmeticId '{p.equippedCosmeticId}' is not in ownedCosmeticIds.");
            }

            // Vitality
            var v = dto.vitality;
            if (v == null) { errors.Add("vitality is null."); }
            else
            {
                if (v.tapProgress < 0 || v.tapProgress >= rules.tapsPerReward) errors.Add($"vitality.tapProgress {v.tapProgress} out of [0,{rules.tapsPerReward - 1}].");
                if (!Finite(v.bankSeconds) || v.bankSeconds < 0 || v.bankSeconds > rules.vitalityMaxBankSeconds) errors.Add($"vitality.bankSeconds {v.bankSeconds} out of [0,{rules.vitalityMaxBankSeconds}].");
            }

            return errors;
        }

        private static void ValidateEntries<T>(List<T> entries, Func<T, string> id, Func<T, bool> valid, List<string> errors) where T : class
        {
            if (entries == null) { errors.Add("Missing list."); return; }
            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (var entry in entries)
            {
                if (entry == null) { errors.Add("Null list entry."); continue; }
                if (string.IsNullOrWhiteSpace(id(entry)) || !seen.Add(id(entry)) || !valid(entry)) errors.Add("Invalid/duplicate entry ID or value.");
            }
        }

        private static bool Finite(double v) => !double.IsNaN(v) && !double.IsInfinity(v);
    }
}
