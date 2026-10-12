using System.Collections.Generic;
using CayTheGioi.Domain;
using CayTheGioi.Presentation;
using NUnit.Framework;
using UnityEngine;

namespace CayTheGioi.Tests
{
    [TestFixture]
    public sealed class TutorialSettingsAudioTests
    {
        private GameState state;
        private PrototypeRules rules;
        private TutorialSystem tutorial;

        [SetUp]
        public void SetUp()
        {
            state = new GameState();
            state.run.runId = "test_run_1";
            state.run.life = "ALIVE";
            state.run.stageId = "seedling";
            state.run.water = 10;
            state.run.growth = 0;
            state.run.hp = 100;

            rules = new PrototypeRules();
            tutorial = new TutorialSystem(state, rules);
        }

        [Test]
        public void TutorialSystem_InitialState_CurrentStepIsVitality()
        {
            Assert.That(tutorial.GetCurrentStepId(), Is.EqualTo(TutorialSystem.StepVitality));
            Assert.That(tutorial.IsStepCompleted(TutorialSystem.StepVitality), Is.False);
        }

        [Test]
        public void TutorialSystem_VitalityEarned_CompletesStepVitality_AndAdvancesToUpgradeRoot()
        {
            tutorial.OnVitalityRewardEarned(20.0);

            Assert.That(tutorial.IsStepCompleted(TutorialSystem.StepVitality), Is.True);
            Assert.That(state.permanent.tutorialCompletedStepIds.Contains(TutorialSystem.StepVitality), Is.True);
            Assert.That(tutorial.GetCurrentStepId(), Is.EqualTo(TutorialSystem.StepUpgradeRoot));
        }

        [Test]
        public void TutorialSystem_UpgradeRootAndLeaves_AdvancesSteps()
        {
            // Bước 1: Vitality
            tutorial.OnVitalityRewardEarned(20.0);

            // Bước 2: Root
            tutorial.OnUpgradeCompleted("root");
            Assert.That(tutorial.IsStepCompleted(TutorialSystem.StepUpgradeRoot), Is.True);
            Assert.That(tutorial.GetCurrentStepId(), Is.EqualTo(TutorialSystem.StepUpgradeLeaves));

            // Bước 3: Leaves
            tutorial.OnUpgradeCompleted("leaves");
            Assert.That(tutorial.IsStepCompleted(TutorialSystem.StepUpgradeLeaves), Is.True);
            Assert.That(tutorial.GetCurrentStepId(),Is.EqualTo(TutorialSystem.StepUpgradeTrunk));
            tutorial.OnUpgradeCompleted("trunk");
            Assert.That(tutorial.GetCurrentStepId(), Is.EqualTo(TutorialSystem.StepEvolution));
        }

        [Test]
        public void TutorialSystem_EvolutionToYoungTree_AdvancesToTrunkWeather()
        {
            tutorial.OnVitalityRewardEarned(20.0);
            tutorial.OnUpgradeCompleted("root");
            tutorial.OnUpgradeCompleted("leaves");

            state.run.stageId = "young_tree";
            tutorial.OnStageChanged("young_tree");

            Assert.That(tutorial.IsStepCompleted(TutorialSystem.StepEvolution), Is.True);
            Assert.That(tutorial.GetCurrentStepId(), Is.EqualTo(TutorialSystem.StepTrunkWeather));
        }

        [Test]
        public void TutorialSystem_DialogueSteps_RequireConfirmation()
        {
            // Bỏ qua các bước trước
            state.permanent.tutorialCompletedStepIds.Add(TutorialSystem.StepVitality);
            state.permanent.tutorialCompletedStepIds.Add(TutorialSystem.StepUpgradeRoot);
            state.permanent.tutorialCompletedStepIds.Add(TutorialSystem.StepUpgradeLeaves);
            state.permanent.tutorialCompletedStepIds.Add(TutorialSystem.StepEvolution);
            state.run.stageId = "young_tree";

            Assert.That(tutorial.GetCurrentStepId(), Is.EqualTo(TutorialSystem.StepTrunkWeather));

            // Xác nhận bước TrunkWeather
            tutorial.ConfirmDialogueStep(TutorialSystem.StepTrunkWeather);
            Assert.That(tutorial.IsStepCompleted(TutorialSystem.StepTrunkWeather), Is.True);
            Assert.That(tutorial.GetCurrentStepId(), Is.EqualTo(TutorialSystem.StepRebirthIntro));

            // Xác nhận bước RebirthIntro
            tutorial.ConfirmDialogueStep(TutorialSystem.StepRebirthIntro);
            Assert.That(tutorial.IsStepCompleted(TutorialSystem.StepRebirthIntro), Is.True);
            Assert.That(tutorial.GetCurrentStepId(), Is.Null); // Hoàn thành tất cả các bước tutorial
        }

        [Test]
        public void TutorialSystem_DeadRun_ReturnsNullCurrentStep_ToAvoidSoftlock()
        {
            Assert.That(tutorial.GetCurrentStepId(), Is.EqualTo(TutorialSystem.StepVitality));

            state.run.life = "DEAD";
            Assert.That(tutorial.GetCurrentStepId(), Is.Null, "Khi cây chết, tutorial không được hiển thị để tránh softlock modal Rebirth");
        }

        [Test]
        public void TutorialSystem_Rebirth_PreservesCompletedSteps()
        {
            tutorial.OnVitalityRewardEarned(20.0);
            tutorial.OnUpgradeCompleted("root");

            Assert.That(state.permanent.tutorialCompletedStepIds, Contains.Item(TutorialSystem.StepVitality));
            Assert.That(state.permanent.tutorialCompletedStepIds, Contains.Item(TutorialSystem.StepUpgradeRoot));

            // Mô phỏng Rebirth: reset run state nhưng giữ permanent state
            state.run = new RunState();
            state.run.stageId = "seedling";
            state.run.life = "ALIVE";

            Assert.That(state.permanent.tutorialCompletedStepIds, Contains.Item(TutorialSystem.StepVitality));
            Assert.That(state.permanent.tutorialCompletedStepIds, Contains.Item(TutorialSystem.StepUpgradeRoot));
            Assert.That(tutorial.GetCurrentStepId(), Is.EqualTo(TutorialSystem.StepUpgradeLeaves), "Sau Rebirth, các bước đã làm không bị lặp lại");
        }

        [Test]
        public void TutorialSystem_SaveFailure_RollsBackStepCompletion()
        {
            bool shouldFailSave = true;
            var safeTutorial = new TutorialSystem(state, rules, () =>
            {
                if (shouldFailSave) return new CommandResult(false, "IO_DISK_FULL");
                return new CommandResult(true);
            });

            var result = safeTutorial.TryCompleteStep(TutorialSystem.StepVitality);
            Assert.That(result.Success, Is.False);
            Assert.That(result.Reason, Is.EqualTo("IO_DISK_FULL"));
            Assert.That(safeTutorial.IsStepCompleted(TutorialSystem.StepVitality), Is.False, "Khi save fail, step ID phải được rollback khỏi danh sách");
        }

        [Test]
        public void AudioController_RateLimiting_RejectsSpamTapSfx()
        {
            var go = new GameObject("AudioTest");
            try
            {
                var audioCtrl = go.AddComponent<AudioController>();
                audioCtrl.Bind(state);

                // Lần tap đầu tiên: thành công
                bool first = audioCtrl.PlayTap();
                Assert.That(first, Is.True, "Lần tap đầu tiên phải được phát âm thanh");

                // Lần tap ngay lập tức (dưới 80ms): bị chặn
                bool spam = audioCtrl.PlayTap();
                Assert.That(spam, Is.False, "Spam tap liên tiếp dưới 80ms phải bị từ chối để tránh rè âm thanh");
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void AudioController_MusicToggle_MutesAndUnmutesBgm()
        {
            var go = new GameObject("AudioTest");
            try
            {
                var audioCtrl = go.AddComponent<AudioController>();
                audioCtrl.Bind(state);

                Assert.That(audioCtrl.MusicEnabled, Is.True);

                audioCtrl.SetMusicEnabled(false);
                Assert.That(audioCtrl.MusicEnabled, Is.False);

                audioCtrl.SetMusicEnabled(true);
                Assert.That(audioCtrl.MusicEnabled, Is.True);
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void SettingsState_Serialization_PreservesValuesAcrossSaveLoad()
        {
            state.settings.musicEnabled = false;
            state.settings.orbitLocked = true;

            string json = JsonUtility.ToJson(state);
            var loaded = JsonUtility.FromJson<GameState>(json);

            Assert.That(loaded.settings.musicEnabled, Is.False);
            Assert.That(loaded.settings.orbitLocked, Is.True);
        }
    }
}
