using System;
using System.Collections.Generic;

namespace CayTheGioi.Domain
{
    public readonly struct AchievementStatus
    {
        public readonly string AchievementId;
        public readonly string DisplayName;
        public readonly string Description;
        public readonly long CurrentProgress;
        public readonly long TargetProgress;
        public readonly long CoinReward;
        public readonly bool IsCompleted;
        public readonly bool IsClaimed;
        public readonly bool CanClaim;

        public AchievementStatus(string id, string name, string desc, long current, long target, long reward, bool claimed)
        {
            AchievementId = id;
            DisplayName = name;
            Description = desc;
            CurrentProgress = current;
            TargetProgress = target;
            CoinReward = reward;
            IsCompleted = current >= target;
            IsClaimed = claimed;
            CanClaim = !claimed && current >= target;
        }
    }

    public sealed class AchievementSystem
    {
        private readonly GameState state;
        private readonly PrototypeRules rules;
        private readonly Func<CommandResult> saveFunc;
        private bool isClaiming;

        public event Action<string, long> ProgressUpdated;
        public event Action<string, long> Claimed;
        public event Action StateChanged;

        public AchievementSystem(GameState state, PrototypeRules rules, Func<CommandResult> saveFunc = null)
        {
            this.state = state ?? throw new ArgumentNullException(nameof(state));
            this.rules = rules ?? throw new ArgumentNullException(nameof(rules));
            this.saveFunc = saveFunc;
            EnsureEntries();
        }

        public void EnsureEntries()
        {
            if (state.permanent == null) return;
            if (state.permanent.achievements == null)
                state.permanent.achievements = new List<AchievementEntry>();

            if (rules.achievements == null) return;
            foreach (var def in rules.achievements)
            {
                if (def == null || string.IsNullOrWhiteSpace(def.achievementId)) continue;
                var existing = state.permanent.achievements.Find(a => a.achievementId == def.achievementId);
                if (existing == null)
                {
                    state.permanent.achievements.Add(new AchievementEntry
                    {
                        achievementId = def.achievementId,
                        progress = 0,
                        claimed = false
                    });
                }
            }
        }

        public void SyncFromCurrentState()
        {
            EnsureEntries();

            if (state.run != null && state.run.levels != null)
            {
                if (state.run.levels.root >= 1)
                    TryUpdateProgress("first_root", 1);
                if (state.run.levels.leaves >= 1)
                    TryUpdateProgress("first_leaves", 1);
            }

            string highest = state.permanent?.highestStageEverId ?? "";
            string current = state.run?.stageId ?? "";
            if (highest == "young_tree" || highest == "large_tree" || current == "young_tree" || current == "large_tree")
                TryUpdateProgress("evolve_young_tree", 1);
            if (highest == "large_tree" || current == "large_tree")
                TryUpdateProgress("evolve_large_tree", 1);

            if (state.permanent != null && state.permanent.rebirthCount >= 1)
                TryUpdateProgress("first_rebirth", 1);
        }

        public void OnUpgradeCompleted(string branchId)
        {
            if (state.run == null || state.run.levels == null) return;
            if (branchId == "root" && state.run.levels.root >= 1)
                TryUpdateProgress("first_root", 1);
            else if (branchId == "leaves" && state.run.levels.leaves >= 1)
                TryUpdateProgress("first_leaves", 1);
        }

        public void OnStageChanged(string stageId)
        {
            if (stageId == "young_tree")
                TryUpdateProgress("evolve_young_tree", 1);
            else if (stageId == "large_tree")
            {
                TryUpdateProgress("evolve_young_tree", 1);
                TryUpdateProgress("evolve_large_tree", 1);
            }
        }

        public void OnRebirthCompleted()
        {
            if (state.permanent != null && state.permanent.rebirthCount >= 1)
                TryUpdateProgress("first_rebirth", 1);
        }

        public void OnVitalityRewardEarned()
        {
            TryUpdateProgress("first_vitality", 1);
        }

        public bool TryUpdateProgress(string achievementId, long progress)
        {
            if (string.IsNullOrWhiteSpace(achievementId)) return false;
            EnsureEntries();

            var entry = state.permanent.achievements.Find(a => a.achievementId == achievementId);
            if (entry == null) return false;

            if (progress > entry.progress)
            {
                entry.progress = progress;
                ProgressUpdated?.Invoke(achievementId, entry.progress);
                StateChanged?.Invoke();
                return true;
            }
            return false;
        }

        public CommandResult TryClaim(string achievementId)
        {
            if (isClaiming) return new CommandResult(false, "BUSY");
            if (string.IsNullOrWhiteSpace(achievementId)) return new CommandResult(false, "INVALID_ID");

            EnsureEntries();

            var def = rules.FindAchievement(achievementId);
            if (def == null) return new CommandResult(false, "UNKNOWN_ACHIEVEMENT");

            var entry = state.permanent.achievements.Find(a => a.achievementId == achievementId);
            if (entry == null) return new CommandResult(false, "ENTRY_NOT_FOUND");

            if (entry.claimed) return new CommandResult(false, "ALREADY_CLAIMED");
            if (entry.progress < def.targetProgress) return new CommandResult(false, "NOT_UNLOCKED");

            isClaiming = true;
            try
            {
                entry.claimed = true;
                state.permanent.coin = checked(state.permanent.coin + def.coinReward);

                if (saveFunc != null)
                {
                    var saveResult = saveFunc();
                    if (!saveResult.Success)
                    {
                        // Rollback on failure so retry won't double-award coin
                        entry.claimed = false;
                        state.permanent.coin -= def.coinReward;
                        return new CommandResult(false, "SAVE_FAILED: " + saveResult.Reason);
                    }
                }

                Claimed?.Invoke(achievementId, def.coinReward);
                StateChanged?.Invoke();
                return new CommandResult(true);
            }
            catch (OverflowException)
            {
                entry.claimed = false;
                return new CommandResult(false, "COIN_OVERFLOW");
            }
            finally
            {
                isClaiming = false;
            }
        }

        public AchievementStatus GetStatus(string achievementId)
        {
            var def = rules.FindAchievement(achievementId);
            if (def == null) return default;

            EnsureEntries();
            var entry = state.permanent.achievements.Find(a => a.achievementId == achievementId);
            long progress = entry?.progress ?? 0;
            bool claimed = entry?.claimed ?? false;

            return new AchievementStatus(def.achievementId, def.displayName, def.description, progress, def.targetProgress, def.coinReward, claimed);
        }

        public List<AchievementStatus> GetAllStatuses()
        {
            var list = new List<AchievementStatus>();
            if (rules.achievements == null) return list;

            EnsureEntries();
            foreach (var def in rules.achievements)
            {
                if (def == null) continue;
                var entry = state.permanent.achievements.Find(a => a.achievementId == def.achievementId);
                long progress = entry?.progress ?? 0;
                bool claimed = entry?.claimed ?? false;
                list.Add(new AchievementStatus(def.achievementId, def.displayName, def.description, progress, def.targetProgress, def.coinReward, claimed));
            }
            return list;
        }
    }
}
