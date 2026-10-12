using System;
using System.IO;
using CayTheGioi.Domain;
using CayTheGioi.Infrastructure;
using NUnit.Framework;
using UnityEngine;

namespace CayTheGioi.Tests
{
    public sealed class AchievementTests
    {
        private PrototypeRules rules;
        private GameState state;
        private AchievementSystem achievements;
        private string testDirectory;
        private SaveService saveService;
        private long revision;

        [SetUp]
        public void Setup()
        {
            rules = new PrototypeRules();
            state = GameStateFactory.CreateNew(rules);
            testDirectory = Path.Combine(Application.temporaryCachePath, "T08A_Edit_" + Guid.NewGuid().ToString("N"));
            saveService = new SaveService(rules, testDirectory);
            revision = 0;
            saveService.Save(state, "t08a", ref revision);

            achievements = new AchievementSystem(state, rules, () =>
            {
                var r = saveService.Save(state, "t08a", ref revision);
                return new CommandResult(r.Success, r.Reason);
            });
        }

        [TearDown]
        public void Teardown()
        {
            if (Directory.Exists(testDirectory))
                Directory.Delete(testDirectory, true);
        }

        [Test]
        public void Rules_ContainsSixDefaultAchievements_WithValidRewardAndTarget()
        {
            Assert.That(rules.achievements, Is.Not.Null);
            Assert.That(rules.achievements.Length, Is.EqualTo(6));

            string[] expectedIds = { "first_root", "first_leaves", "evolve_young_tree", "evolve_large_tree", "first_rebirth", "first_vitality" };
            foreach (var id in expectedIds)
            {
                var def = rules.FindAchievement(id);
                Assert.That(def, Is.Not.Null, $"Missing achievement definition for {id}");
                Assert.That(def.targetProgress, Is.EqualTo(1));
                Assert.That(def.coinReward, Is.EqualTo(10));
                Assert.That(string.IsNullOrWhiteSpace(def.displayName), Is.False);
            }

            Assert.That(rules.Validate(), Is.Empty);
        }

        [Test]
        public void AchievementSystem_EnsureEntries_InitializesAllSixAchievementsWithZeroProgressAndUnclaimed()
        {
            var newState = GameStateFactory.CreateNew(rules);
            Assert.That(newState.permanent.coin, Is.EqualTo(0));
            var sys = new AchievementSystem(newState, rules);
            Assert.That(newState.permanent.achievements.Count, Is.EqualTo(6));

            foreach (var entry in newState.permanent.achievements)
            {
                Assert.That(entry.progress, Is.EqualTo(0));
                Assert.That(entry.claimed, Is.False);
            }
        }

        [Test]
        public void MilestoneDetection_UpgradeRootAndLeaves_UpdatesProgress()
        {
            var resources = new ResourceSystem(state, rules, new VitalitySystem(state, rules));
            var upgrades = new UpgradeSystem(state, rules, resources);
            upgrades.Completed += achievements.OnUpgradeCompleted;

            state.run.water = 100;
            var resRoot = upgrades.TryUpgrade("root");
            Assert.That(resRoot.Success, Is.True);
            Assert.That(achievements.GetStatus("first_root").CurrentProgress, Is.EqualTo(1));
            Assert.That(achievements.GetStatus("first_root").IsCompleted, Is.True);

            var resLeaves = upgrades.TryUpgrade("leaves");
            Assert.That(resLeaves.Success, Is.True);
            Assert.That(achievements.GetStatus("first_leaves").CurrentProgress, Is.EqualTo(1));
            Assert.That(achievements.GetStatus("first_leaves").IsCompleted, Is.True);
        }

        [Test]
        public void MilestoneDetection_Evolution_UpdatesYoungAndLargeTreeProgress()
        {
            var resources = new ResourceSystem(state, rules, new VitalitySystem(state, rules));
            var evolution = new EvolutionSystem(state, rules, resources);
            evolution.StageChanged += achievements.OnStageChanged;

            state.run.water = 500; state.run.photosynthesis=500;
            state.run.growth = 500;

            // Seedling -> Young Tree
            var evolveRes1 = evolution.TryEvolve();
            Assert.That(evolveRes1.Success, Is.True);
            Assert.That(achievements.GetStatus("evolve_young_tree").CurrentProgress, Is.EqualTo(1));
            Assert.That(achievements.GetStatus("evolve_young_tree").IsCompleted, Is.True);
            Assert.That(achievements.GetStatus("evolve_large_tree").CurrentProgress, Is.EqualTo(0));

            // Young Tree -> Large Tree
            evolution.CompleteTransition();
            var evolveRes2 = evolution.TryEvolve();
            Assert.That(evolveRes2.Success, Is.True);
            Assert.That(achievements.GetStatus("evolve_large_tree").CurrentProgress, Is.EqualTo(1));
            Assert.That(achievements.GetStatus("evolve_large_tree").IsCompleted, Is.True);
        }

        [Test]
        public void MilestoneDetection_Rebirth_UpdatesFirstRebirthProgress()
        {
            var rebirth = new RebirthSystem(state, rules, SaveSerializer.Clone, (c, id) =>
            {
                var r = saveService.Save(c, "t08a", ref revision);
                return new CommandResult(r.Success, r.Reason);
            });
            rebirth.Completed += achievements.OnRebirthCompleted;

            state.run.stageId = "young_tree";
            string runId = state.run.runId;
            var rebirthRes = rebirth.TryRebirth(runId);
            Assert.That(rebirthRes.Success, Is.True);

            Assert.That(achievements.GetStatus("first_rebirth").CurrentProgress, Is.EqualTo(1));
            Assert.That(achievements.GetStatus("first_rebirth").IsCompleted, Is.True);
        }

        [Test]
        public void MilestoneDetection_VitalityReward_RequiresRewardEarnedNotSingleTap()
        {
            var vitality = new VitalitySystem(state, rules);
            vitality.RewardEarned += achievements.OnVitalityRewardEarned;

            // 1 single tap does NOT complete achievement
            vitality.TryTap();
            Assert.That(achievements.GetStatus("first_vitality").CurrentProgress, Is.EqualTo(0));
            Assert.That(achievements.GetStatus("first_vitality").IsCompleted, Is.False);

            // Tap 48 more times (total 49)
            for (int i = 0; i < 48; i++)
                vitality.TryTap();
            Assert.That(achievements.GetStatus("first_vitality").CurrentProgress, Is.EqualTo(0));
            Assert.That(achievements.GetStatus("first_vitality").IsCompleted, Is.False);

            // 50th tap completes a bank reward (20s) and triggers RewardEarned
            vitality.TryTap();
            Assert.That(achievements.GetStatus("first_vitality").CurrentProgress, Is.EqualTo(1));
            Assert.That(achievements.GetStatus("first_vitality").IsCompleted, Is.True);
        }

        [Test]
        public void Claim_BeforeUnlocked_FailsAndGrantsNoCoin()
        {
            var res = achievements.TryClaim("first_root");
            Assert.That(res.Success, Is.False);
            Assert.That(res.Reason, Is.EqualTo("NOT_UNLOCKED"));
            Assert.That(state.permanent.coin, Is.EqualTo(0));
        }

        [Test]
        public void Claim_WhenUnlocked_Grants10Coin_AndIsIdempotent()
        {
            achievements.TryUpdateProgress("first_root", 1);
            Assert.That(achievements.GetStatus("first_root").CanClaim, Is.True);

            // First claim: success, +10 coin
            var res1 = achievements.TryClaim("first_root");
            Assert.That(res1.Success, Is.True);
            Assert.That(state.permanent.coin, Is.EqualTo(10));
            Assert.That(achievements.GetStatus("first_root").IsClaimed, Is.True);
            Assert.That(achievements.GetStatus("first_root").CanClaim, Is.False);

            // Duplicate claim / rapid click: fails, no coin added
            var res2 = achievements.TryClaim("first_root");
            Assert.That(res2.Success, Is.False);
            Assert.That(res2.Reason, Is.EqualTo("ALREADY_CLAIMED"));
            Assert.That(state.permanent.coin, Is.EqualTo(10));
        }

        [Test]
        public void Claim_RollbackOnSaveFailure_DoesNotDuplicateCoinOnRetry()
        {
            achievements.TryUpdateProgress("first_leaves", 1);

            // Configure failing save function
            bool failSave = true;
            var systemWithFailingSave = new AchievementSystem(state, rules, () =>
            {
                if (failSave) return new CommandResult(false, "DISK_FULL");
                var r = saveService.Save(state, "t08a", ref revision);
                return new CommandResult(r.Success, r.Reason);
            });

            // Attempt claim when save fails
            var resFail = systemWithFailingSave.TryClaim("first_leaves");
            Assert.That(resFail.Success, Is.False);
            Assert.That(resFail.Reason, Does.StartWith("SAVE_FAILED"));
            Assert.That(state.permanent.coin, Is.EqualTo(0)); // Coin rolled back
            Assert.That(systemWithFailingSave.GetStatus("first_leaves").IsClaimed, Is.False); // Claim flag rolled back

            // Retry after save issue resolved
            failSave = false;
            var resSuccess = systemWithFailingSave.TryClaim("first_leaves");
            Assert.That(resSuccess.Success, Is.True);
            Assert.That(state.permanent.coin, Is.EqualTo(10)); // Exactly +10, no duplicate
            Assert.That(systemWithFailingSave.GetStatus("first_leaves").IsClaimed, Is.True);
        }

        [Test]
        public void Rebirth_PreservesCoin_AndAchievementProgressAndClaimed()
        {
            achievements.TryUpdateProgress("first_root", 1);
            achievements.TryClaim("first_root");
            achievements.TryUpdateProgress("evolve_young_tree", 1); // Completed but not claimed

            Assert.That(state.permanent.coin, Is.EqualTo(10));

            var rebirth = new RebirthSystem(state, rules, SaveSerializer.Clone, (c, id) =>
            {
                var r = saveService.Save(c, "t08a", ref revision);
                return new CommandResult(r.Success, r.Reason);
            });

            state.run.stageId = "young_tree";
            string runId = state.run.runId;
            var res = rebirth.TryRebirth(runId);
            Assert.That(res.Success, Is.True);

            // Coin preserved
            Assert.That(state.permanent.coin, Is.EqualTo(10));

            // Achievement states preserved
            var rootStatus = achievements.GetStatus("first_root");
            Assert.That(rootStatus.IsClaimed, Is.True);
            Assert.That(rootStatus.CurrentProgress, Is.EqualTo(1));

            var youngStatus = achievements.GetStatus("evolve_young_tree");
            Assert.That(youngStatus.IsClaimed, Is.False);
            Assert.That(youngStatus.CurrentProgress, Is.EqualTo(1));
            Assert.That(youngStatus.CanClaim, Is.True);
        }

        [Test]
        public void SaveLoad_PreservesCoin_AndAchievementsIntegrity()
        {
            achievements.TryUpdateProgress("first_root", 1);
            achievements.TryClaim("first_root");
            achievements.TryUpdateProgress("first_leaves", 1);

            Assert.That(state.permanent.coin, Is.EqualTo(10));
            Assert.That(saveService.Save(state, "t08a", ref revision).Success, Is.True);

            // Load into a new GameState
            var loadRes = saveService.Load();
            Assert.That(loadRes.result.Success, Is.True);
            var loadedState = new GameState();
            SaveSerializer.Restore(loadRes.result.Data, loadedState);

            Assert.That(loadedState.permanent.coin, Is.EqualTo(10));
            var loadedAchievements = new AchievementSystem(loadedState, rules);

            var rootStatus = loadedAchievements.GetStatus("first_root");
            Assert.That(rootStatus.IsClaimed, Is.True);
            Assert.That(rootStatus.CurrentProgress, Is.EqualTo(1));

            var leavesStatus = loadedAchievements.GetStatus("first_leaves");
            Assert.That(leavesStatus.IsClaimed, Is.False);
            Assert.That(leavesStatus.CurrentProgress, Is.EqualTo(1));
            Assert.That(leavesStatus.CanClaim, Is.True);
        }

        [Test]
        public void Coin_NotBuffedByVitalityMultiplier_AndNotUsedForUpgradeCost()
        {
            // Vitality bank active
            state.vitality.bankSeconds = 100;
            achievements.TryUpdateProgress("first_root", 1);
            achievements.TryClaim("first_root");

            // Coin reward must be exactly 10, not 10 * 1.05 = 10.5
            Assert.That(state.permanent.coin, Is.EqualTo(10));

            // Upgrade uses water, does not consume coin
            state.run.water = 100;
            var resources = new ResourceSystem(state, rules, new VitalitySystem(state, rules));
            var upgrades = new UpgradeSystem(state, rules, resources);
            var upRes = upgrades.TryUpgrade("root");
            Assert.That(upRes.Success, Is.True);
            Assert.That(state.permanent.coin, Is.EqualTo(10)); // Coin completely untouched by upgrade
        }
    }
}
