using System.Collections;
using System.IO;
using CayTheGioi.Bootstrap;
using CayTheGioi.Domain;
using CayTheGioi.Infrastructure;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace CayTheGioi.Tests
{
    /// <summary>
    /// PlayMode integration tests for T04 Save System.
    /// SetUp deletes saves so each test begins from NewGame state.
    /// Note: Debug.Log (Info) messages do not fail tests; only unhandled LogError/LogWarning do.
    /// </summary>
    public sealed class SaveSceneTests
    {
        private SaveService tempSvc;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            SaveTestScope.Begin();
            SceneManager.sceneLoaded += FreezeTicks;
            tempSvc = new SaveService(new PrototypeRules());
            tempSvc.DeleteSaveFiles();
            yield return SceneManager.LoadSceneAsync("MainScene");
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            SceneManager.sceneLoaded -= FreezeTicks;
            SaveTestScope.End();
            yield return null;
        }

        private void FreezeTicks(Scene scene, LoadSceneMode mode)
        {
            var driver = Object.FindAnyObjectByType<CayTheGioi.Presentation.OnlineTickDriver>();
            if (driver != null) driver.ManualTick = true;
        }

        // ── Test: bootstrapper initializes SaveController ─────────────

        [UnityTest]
        public IEnumerator SaveControllerIsBoundAfterInit()
        {
            var b = Object.FindAnyObjectByType<GameBootstrapper>();
            Assert.That(b, Is.Not.Null);
            Assert.That(b.IsInitialized, Is.True);
            Assert.That(b.SaveCtrl, Is.Not.Null);
            Assert.That(b.SaveSvc, Is.Not.Null);
            Assert.That(b.LastLoadOutcome, Is.EqualTo(SaveService.LoadOutcome.NewGame));
            yield return null;
        }

        // ── Test: save creates file at persistentDataPath ─────────────

        [UnityTest]
        public IEnumerator ForceSave_CreatesFileOnDisk()
        {
            var b = Object.FindAnyObjectByType<GameBootstrapper>();
            Assert.That(b.SaveSvc, Is.Not.Null);

            var result = b.SaveCtrl.ForceSave("TEST");
            Assert.That(result.Success, Is.True, result.Reason);
            Assert.That(File.Exists(b.SaveSvc.MainPath), Is.True, "Save file not created at: " + b.SaveSvc.MainPath);
            yield return null;
        }

        // ── Test: state persists across scene reload ──────────────────

        [UnityTest]
        public IEnumerator SaveAndReload_RestoresWaterAndLevels()
        {
            var b = Object.FindAnyObjectByType<GameBootstrapper>();

            // Mutate state in a realistic way
            b.State.run.water = 77.5;
            b.State.run.levels.root = 2;
            b.State.vitality.bankSeconds = 250;
            b.State.settings.orbitLocked = true;

            var saveResult = b.SaveCtrl.ForceSave("TEST_ROUNDTRIP");
            Assert.That(saveResult.Success, Is.True, saveResult.Reason);

            // Reload scene (simulates re-launch; SetUp won't run again for inline LoadSceneAsync)
            yield return SceneManager.LoadSceneAsync("MainScene");
            yield return null;

            var b2 = Object.FindAnyObjectByType<GameBootstrapper>();
            Assert.That(b2, Is.Not.Null);
            Assert.That(b2.LastLoadOutcome, Is.EqualTo(SaveService.LoadOutcome.Loaded));
            Assert.That(b2.State.run.water, Is.EqualTo(77.5).Within(1e-6));
            Assert.That(b2.State.run.levels.root, Is.EqualTo(2));
            Assert.That(b2.State.vitality.bankSeconds, Is.EqualTo(250).Within(1e-6));
            Assert.That(b2.State.settings.orbitLocked, Is.True);
        }

        // ── Test: no auto Evolution on load, stage preserved ─────────

        [UnityTest]
        public IEnumerator Reload_DoesNotAutoEvolve_AndPreservesStage()
        {
            var b = Object.FindAnyObjectByType<GameBootstrapper>();
            Assert.That(b.State.run.stageId, Is.EqualTo("seedling"));
            b.SaveCtrl.ForceSave("SEEDLING");

            yield return SceneManager.LoadSceneAsync("MainScene");
            yield return null;

            var b2 = Object.FindAnyObjectByType<GameBootstrapper>();
            Assert.That(b2.State.run.stageId, Is.EqualTo("seedling"));
            Assert.That(b2.Evolution.IsTransitioning, Is.False);
        }

        // ── Test: corrupt main uses backup ────────────────────────────

        [UnityTest]
        public IEnumerator CorruptMain_LoadsFromBackup()
        {
            var b = Object.FindAnyObjectByType<GameBootstrapper>();
            b.State.run.water = 88;

            // Save twice so backup (= first save content) exists
            b.SaveCtrl.ForceSave("T1");
            b.SaveCtrl.ForceSave("T2");
            string mainPath = b.SaveSvc.MainPath;
            string backupPath = b.SaveSvc.BackupPath;
            Assert.That(File.Exists(mainPath), Is.True);
            Assert.That(File.Exists(backupPath), Is.True);

            // Corrupt main AFTER saving backup
            File.WriteAllText(mainPath, "{bad json -- corrupted for test}");

            yield return SceneManager.LoadSceneAsync("MainScene");
            yield return null;

            var b2 = Object.FindAnyObjectByType<GameBootstrapper>();
            Assert.That(b2.LastLoadOutcome, Is.EqualTo(SaveService.LoadOutcome.LoadedFromBackup));
            Assert.That(b2.IsInitialized, Is.False);
            Assert.That(Object.FindAnyObjectByType<CayTheGioi.Presentation.SaveStatusView>().LoadPanelVisible, Is.True);
            Assert.That(b2.RetryLoad(true), Is.True);
            b2.TickDriver.ManualTick = true;
            Assert.That(b2.SaveSvc.Load().result.Data.offlineCheckpointUtcSeconds, Is.GreaterThan(0));
            // Water should be restored from backup (first save content)
            Assert.That(b2.State.run.water, Is.EqualTo(88).Within(1e-6));
        }

        // ── Test: bank and tapProgress preserved ─────────────────────

        [UnityTest]
        public IEnumerator Reload_PreservesBankAndTapProgress()
        {
            var b = Object.FindAnyObjectByType<GameBootstrapper>();
            b.State.vitality.bankSeconds = 1800;
            b.State.vitality.tapProgress = 30;

            b.SaveCtrl.ForceSave("VITALITY_TEST");

            yield return SceneManager.LoadSceneAsync("MainScene");
            yield return null;

            var b2 = Object.FindAnyObjectByType<GameBootstrapper>();
            Assert.That(b2.State.vitality.bankSeconds, Is.EqualTo(1800).Within(1e-6));
            Assert.That(b2.State.vitality.tapProgress, Is.EqualTo(30));
        }

        // ── Test: T01–T03 regression ──────────────────────────────────

        [UnityTest]
        public IEnumerator Regression_TickAndVitalityWorkAfterSaveInit()
        {
            var b = Object.FindAnyObjectByType<GameBootstrapper>();
            Assert.That(b.Resources, Is.Not.Null);
            Assert.That(b.Vitality, Is.Not.Null);

            b.State.run.levels.root = 1;
            var tickResult = b.Resources.Tick(1.0);
            Assert.That(tickResult.Success, Is.True, tickResult.Reason);

            var tapResult = b.Vitality.TryTap(inputAllowed: true);
            Assert.That(tapResult.Success, Is.True, tapResult.Reason);

            yield return null;
        }
        [UnityTest]
        public IEnumerator LifecycleSavesOnceFreezesBankAndZeroElapsedResumeDoesNotCredit()
        {
            var b = Object.FindAnyObjectByType<GameBootstrapper>();
            b.State.run.levels.root = 2; b.State.vitality.bankSeconds = 300;
            b.SaveCtrl.SendMessage("OnApplicationFocus", true);
            b.SaveCtrl.SendMessage("OnApplicationPause", false);
            b.Input.SendMessage("OnApplicationFocus", true); b.TickDriver.SendMessage("OnApplicationFocus", true);
            long before = b.SaveCtrl.Revision;
            b.TickDriver.SendMessage("OnApplicationPause", true); b.Input.SendMessage("OnApplicationPause", true);
            b.SaveCtrl.SendMessage("OnApplicationPause", true);
            b.TickDriver.SendMessage("OnApplicationFocus", false); b.Input.SendMessage("OnApplicationFocus", false);
            b.SaveCtrl.SendMessage("OnApplicationFocus", false);
            Assert.That(b.SaveCtrl.Revision, Is.EqualTo(before + 1));
            b.TickDriver.ManualTick = false; double water = b.State.run.water;
            b.SaveCtrl.Pulse(120); yield return null; yield return null;
            Assert.That(b.State.run.water, Is.EqualTo(water)); Assert.That(b.State.vitality.bankSeconds, Is.EqualTo(300));
            Assert.That(b.SaveCtrl.Revision, Is.EqualTo(before + 1));
            b.TickDriver.ManualTick = true;
            b.SaveCtrl.SendMessage("OnApplicationPause", false); b.SaveCtrl.SendMessage("OnApplicationFocus", true);
            b.TickDriver.SendMessage("OnApplicationPause", false); b.TickDriver.SendMessage("OnApplicationFocus", true);
            b.Input.SendMessage("OnApplicationPause", false); b.Input.SendMessage("OnApplicationFocus", true);
            Assert.That(b.State.run.water, Is.EqualTo(water)); Assert.That(b.State.vitality.bankSeconds, Is.EqualTo(300));
            b.SaveCtrl.Pulse((float)b.Config.Rules.autosaveIntervalSeconds);
            Assert.That(b.SaveCtrl.Revision, Is.EqualTo(before + 3));
        }

        [UnityTest]
        public IEnumerator UnsupportedSaveBlocksGameplayAndAllAutomaticWrites()
        {
            var b = Object.FindAnyObjectByType<GameBootstrapper>(); b.SaveCtrl.ForceSave();
            string path = b.SaveSvc.MainPath;
            string json = File.ReadAllText(path).Replace("\"schemaVersion\":1", "\"schemaVersion\":99");
            File.WriteAllText(path, json);
            yield return SceneManager.LoadSceneAsync("MainScene"); yield return null;
            b = Object.FindAnyObjectByType<GameBootstrapper>();
            Assert.That(b.IsInitialized, Is.False); Assert.That(b.Resources, Is.Null); Assert.That(b.SaveCtrl, Is.Null);
            Assert.That(b.RetryLoad(), Is.False);
            Assert.That(File.ReadAllText(path), Is.EqualTo(json));
            Assert.That(Object.FindAnyObjectByType<CayTheGioi.Presentation.SaveStatusView>().LoadPanelVisible, Is.True);
        }

        [UnityTest]
        public IEnumerator SaveFailureShowsRetryAndMouseAndTouchCommitOnce()
        {
            var b = Object.FindAnyObjectByType<GameBootstrapper>();
            var view = Object.FindAnyObjectByType<CayTheGioi.Presentation.SaveStatusView>();
            b.SaveCtrl.SendMessage("OnApplicationFocus", true); b.Input.SendMessage("OnApplicationFocus", true);
            b.SaveCtrl.ForceSave(); string original = File.ReadAllText(b.SaveSvc.MainPath);
            b.SaveSvc.FaultInjection = at => { if (at == SaveWritePhase.BeforeReplace) throw new IOException("Injected UI save failure"); };
            Assert.That(b.SaveCtrl.ForceSave().Success, Is.False);
            Assert.That(view.StatusText, Does.Contain("Chưa lưu")); Assert.That(view.RetrySaveButton.gameObject.activeSelf, Is.True);
            Assert.That(File.ReadAllText(b.SaveSvc.MainPath), Is.EqualTo(original));
            var priorMouse = UnityEngine.InputSystem.Mouse.current;
            var priorTouch = UnityEngine.InputSystem.Touchscreen.current;
            var mouse = UnityEngine.InputSystem.InputSystem.AddDevice<UnityEngine.InputSystem.Mouse>();
            var touch = UnityEngine.InputSystem.InputSystem.AddDevice<UnityEngine.InputSystem.Touchscreen>();
            try
            {
                Canvas.ForceUpdateCanvases();
                var p = (Vector2)view.RetrySaveButton.transform.TransformPoint(((RectTransform)view.RetrySaveButton.transform).rect.center);
                int taps = b.State.vitality.tapProgress; long revision = b.SaveCtrl.Revision;
                b.SaveSvc.FaultInjection = null;
                UnityEngine.InputSystem.InputSystem.QueueStateEvent(mouse, new UnityEngine.InputSystem.LowLevel.MouseState { position = p });
                yield return null; yield return null;
                UnityEngine.InputSystem.InputSystem.QueueStateEvent(mouse, new UnityEngine.InputSystem.LowLevel.MouseState { position = p }.WithButton(UnityEngine.InputSystem.LowLevel.MouseButton.Left));
                yield return null; yield return null;
                UnityEngine.InputSystem.InputSystem.QueueStateEvent(mouse, new UnityEngine.InputSystem.LowLevel.MouseState { position = p });
                yield return null; yield return null;
                Assert.That(b.SaveCtrl.Revision, Is.EqualTo(revision + 1)); Assert.That(b.SaveCtrl.HasDirtySave, Is.False);
                b.SaveSvc.FaultInjection = at => { if (at == SaveWritePhase.AfterFlush) throw new IOException("Touch retry"); };
                b.SaveCtrl.ForceSave(); b.SaveSvc.FaultInjection = null; revision = b.SaveCtrl.Revision;
                UnityEngine.InputSystem.InputSystem.QueueStateEvent(touch, new UnityEngine.InputSystem.LowLevel.TouchState { touchId = 1, phase = UnityEngine.InputSystem.TouchPhase.Began, position = p });
                yield return null; yield return null;
                UnityEngine.InputSystem.InputSystem.QueueStateEvent(touch, new UnityEngine.InputSystem.LowLevel.TouchState { touchId = 1, phase = UnityEngine.InputSystem.TouchPhase.Ended, position = p });
                yield return null; yield return null;
                Assert.That(b.SaveCtrl.Revision, Is.EqualTo(revision + 1)); Assert.That(b.State.vitality.tapProgress, Is.EqualTo(taps));
            }
            finally
            {
                UnityEngine.InputSystem.InputSystem.RemoveDevice(mouse); UnityEngine.InputSystem.InputSystem.RemoveDevice(touch);
                if (priorMouse != null && priorMouse.added) priorMouse.MakeCurrent();
                if (priorTouch != null && priorTouch.added) priorTouch.MakeCurrent();
            }
        }

        [UnityTest]
        public IEnumerator FinalStageDeadMetaAndSettingsRestoreWithoutProduction()
        {
            var b = Object.FindAnyObjectByType<GameBootstrapper>();
            b.State.run.stageId = "large_tree"; b.State.run.levels.root = 20; b.State.run.levels.leaves = 20; b.State.run.levels.trunk = 10;
            b.State.run.life = "DEAD"; b.State.run.hp = 0; b.State.permanent.highestStageEverId = "large_tree";
            b.State.run.runCounters.Add(new CounterEntry { counterId = "run-test", value = 7 });
            b.State.permanent.coin = 14; b.State.permanent.stagePoints[1].points = 2;
            b.State.permanent.achievements.Add(new AchievementEntry { achievementId = "test-achievement", progress = 3, claimed = true });
            b.State.permanent.ownedCosmeticIds.Add("test-cosmetic"); b.State.permanent.equippedCosmeticId = "test-cosmetic";
            b.State.permanent.tutorialCompletedStepIds.Add("test-step"); b.State.settings.musicEnabled = false; b.State.settings.orbitLocked = true;
            b.State.vitality.bankSeconds = 333; b.State.vitality.tapProgress = 23;
            Assert.That(b.SaveCtrl.ForceSave().Success, Is.True);
            yield return SceneManager.LoadSceneAsync("MainScene"); yield return null;
            b = Object.FindAnyObjectByType<GameBootstrapper>();
            Assert.That(b.State.run.stageId, Is.EqualTo("large_tree")); Assert.That(b.State.run.life, Is.EqualTo("DEAD"));
            Assert.That(Object.FindAnyObjectByType<CayTheGioi.Presentation.TreeView>().CurrentStageId, Is.EqualTo("large_tree"));
            Assert.That(b.State.run.runCounters[0].value, Is.EqualTo(7)); Assert.That(b.State.permanent.coin, Is.EqualTo(14));
            Assert.That(b.State.permanent.stagePoints[1].points, Is.EqualTo(2));
            Assert.That(b.State.permanent.achievements.Find(a => a.achievementId == "test-achievement")?.claimed, Is.True);
            Assert.That(b.State.permanent.equippedCosmeticId, Is.EqualTo("test-cosmetic")); Assert.That(b.State.permanent.tutorialCompletedStepIds[0], Is.EqualTo("test-step"));
            Assert.That(b.State.settings.musicEnabled, Is.False); Assert.That(b.State.settings.orbitLocked, Is.True);
            Assert.That(b.State.vitality.bankSeconds, Is.EqualTo(333)); Assert.That(b.State.vitality.tapProgress, Is.EqualTo(23));
            Assert.That(b.Resources.Tick(10).Success, Is.False);
        }

        [UnityTest]
        public IEnumerator EvolutionCommitsStageToDiskDuringTransition()
        {
            var b = Object.FindAnyObjectByType<GameBootstrapper>(); b.State.run.water = 500; b.State.run.growth = 200;b.State.run.photosynthesis=200;
            Assert.That(b.Evolution.TryEvolve().Success, Is.True);
            var loaded = b.SaveSvc.Load(); Assert.That(loaded.result.Success, Is.True);
            Assert.That(loaded.result.Data.run.stageId, Is.EqualTo("young_tree"));
            Assert.That(loaded.result.Data.run.water, Is.EqualTo(b.State.run.water));
            yield return SceneManager.LoadSceneAsync("MainScene"); yield return null;
            b = Object.FindAnyObjectByType<GameBootstrapper>(); Assert.That(b.State.run.stageId, Is.EqualTo("young_tree"));
            Assert.That(b.Evolution.IsTransitioning, Is.False);
        }
    }
}
