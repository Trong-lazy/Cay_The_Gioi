using System.Collections;
using System.IO;
using CayTheGioi.Bootstrap;
using CayTheGioi.Domain;
using CayTheGioi.Infrastructure;
using CayTheGioi.Presentation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace CayTheGioi.Tests
{
    public sealed class TutorialSceneTests
    {
        private GameBootstrapper b;
        private SettingsView settingsView;
        private TutorialView tutorialView;
        private ToastView toastView;
        private long now;

        [UnitySetUp]
        public IEnumerator Setup()
        {
            SaveTestScope.Begin();
            now = 2000000000;
            SaveService.ValidationClock = () => now;
            SceneManager.sceneLoaded += Freeze;

            yield return Reload();
            b = Object.FindAnyObjectByType<GameBootstrapper>();
            b.Input.SendMessage("OnApplicationFocus", true);
            b.SaveCtrl.SendMessage("OnApplicationFocus", true);

            settingsView = Object.FindAnyObjectByType<SettingsView>();
            tutorialView = Object.FindAnyObjectByType<TutorialView>();
            toastView = Object.FindAnyObjectByType<ToastView>();
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator Cleanup()
        {
            SceneManager.sceneLoaded -= Freeze;
            SaveTestScope.End();
            yield return null;
        }

        private void Freeze(Scene s, LoadSceneMode m)
        {
            var t = Object.FindAnyObjectByType<OnlineTickDriver>();
            if (t != null) t.ManualTick = true;
        }

        private IEnumerator Reload()
        {
            var op = SceneManager.LoadSceneAsync("MainScene", LoadSceneMode.Single);
            while (!op.isDone) yield return null;
            yield return null;
        }

        [UnityTest]
        public IEnumerator SettingsView_OpenAndClose_LocksAndUnlocksInput()
        {
            Assert.That(settingsView.IsVisible, Is.False);
            Assert.That(b.Input.GameplayAllowed, Is.True);

            // Mở Settings
            settingsView.Open();
            yield return null;

            Assert.That(settingsView.IsVisible, Is.True);
            Assert.That(b.Input.GameplayAllowed, Is.False, "Khi mở Settings, gameplay input phải bị khóa");

            // Đóng Settings
            settingsView.Close();
            yield return null;

            Assert.That(settingsView.IsVisible, Is.False);
            Assert.That(b.Input.GameplayAllowed, Is.True, "Khi đóng Settings, gameplay input phải được khôi phục");
        }

        [UnityTest]
        public IEnumerator SettingsView_Toggles_PersistAcrossSaveLoad()
        {
            // Bật toggle âm nhạc (đổi sang Tắt) và toggle orbit (đổi sang Khóa)
            settingsView.ToggleMusic();
            settingsView.ToggleOrbit();
            yield return null;

            Assert.That(b.State.settings.musicEnabled, Is.False);
            Assert.That(b.State.settings.orbitLocked, Is.True);

            // Force save
            now += 10;
            b.SaveCtrl.ForceSave("TEST_SETTINGS");
            yield return null;

            // Reload scene
            yield return Reload();
            b = Object.FindAnyObjectByType<GameBootstrapper>();
            yield return null;
            if (!b.IsInitialized)
            {
                b.RetryLoad();
                yield return null;
            }

            Assert.That(b != null && b.State != null && b.State.settings != null, Is.True, "GameBootstrapper State phải tồn tại sau reload");
            Assert.That(b.State.settings.musicEnabled, Is.False, "Cài đặt âm nhạc phải được khôi phục sau reload");
            Assert.That(b.State.settings.orbitLocked, Is.True, "Cài đặt khóa xoay camera phải được khôi phục sau reload");
        }

        [UnityTest]
        public IEnumerator UiPriority_EscapeKey_AndDeadModalCannotCancel()
        {
            var priority = b.UiPriority;
            Assert.That(priority, Is.Not.Null);

            // 1. Khi Settings mở, HandleEscape đóng Settings
            settingsView.Open();
            yield return null;
            Assert.That(settingsView.IsVisible, Is.True);

            priority.HandleEscape();
            yield return null;
            Assert.That(settingsView.IsVisible, Is.False);

            // 2. Khi cây DEAD, HandleEscape KHÔNG được thoát modal Rebirth
            b.State.run.life = "DEAD";
            var rebirthView = Object.FindAnyObjectByType<RebirthView>();
            rebirthView.SendMessage("LateUpdate");
            yield return null;

            Assert.That(rebirthView.IsVisible, Is.True);

            // Thử Escape khi DEAD
            priority.HandleEscape();
            yield return null;

            Assert.That(rebirthView.IsVisible, Is.True, "Khi cây DEAD, Escape không được phép đóng modal Forced Rebirth");
        }

        [UnityTest]
        public IEnumerator TutorialView_BannerShowsOnFreshRun_AndHidesWhenDead()
        {
            Assert.That(tutorialView.IsVisible, Is.True);
            Assert.That(tutorialView.ActiveStepId, Is.EqualTo(TutorialSystem.StepVitality));

            // Khi cây DEAD, banner tutorial tự động ẩn để tránh che modal Rebirth
            b.State.run.life = "DEAD";
            tutorialView.Refresh();
            yield return null;

            Assert.That(tutorialView.IsVisible, Is.False, "Khi cây DEAD, tutorial banner phải ẩn để nhường quyền cho Rebirth modal");
        }

        [UnityTest]
        public IEnumerator ToastView_Show_WorksAndDoesNotBlockRaycasts()
        {
            toastView.Show("Thông báo thử nghiệm T08C");
            yield return null;

            Assert.That(toastView.IsShowing, Is.True);
            Assert.That(toastView.CurrentMessage, Is.EqualTo("Thông báo thử nghiệm T08C"));

            var group = toastView.GetComponentInChildren<CanvasGroup>();
            Assert.That(group, Is.Not.Null);
            Assert.That(group.blocksRaycasts, Is.False, "Toast container tuyệt đối không chặn raycast");
        }
    }
}
