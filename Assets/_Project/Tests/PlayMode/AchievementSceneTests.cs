using System.Collections;
using System.IO;
using CayTheGioi.Bootstrap;
using CayTheGioi.Domain;
using CayTheGioi.Infrastructure;
using CayTheGioi.Presentation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace CayTheGioi.Tests
{
    public sealed class AchievementSceneTests
    {
        private GameBootstrapper b;
        private AchievementView view;
        private Mouse mouse, priorMouse;
        private Touchscreen touch, priorTouch;
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

            view = Object.FindAnyObjectByType<AchievementView>();
            priorMouse = Mouse.current;
            priorTouch = Touchscreen.current;
            mouse = InputSystem.AddDevice<Mouse>();
            touch = InputSystem.AddDevice<Touchscreen>();
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator Cleanup()
        {
            SceneManager.sceneLoaded -= Freeze;
            if (mouse != null && mouse.added) InputSystem.RemoveDevice(mouse);
            if (touch != null && touch.added) InputSystem.RemoveDevice(touch);
            if (priorMouse != null && priorMouse.added) priorMouse.MakeCurrent();
            if (priorTouch != null && priorTouch.added) priorTouch.MakeCurrent();
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
            yield return SceneManager.LoadSceneAsync("MainScene");
            yield return null;
            b = Object.FindAnyObjectByType<GameBootstrapper>();
            view = Object.FindAnyObjectByType<AchievementView>();
        }

        private Vector2 Center(Component c)
        {
            Canvas.ForceUpdateCanvases();
            var r = (RectTransform)c.transform;
            return RectTransformUtility.WorldToScreenPoint(null, r.TransformPoint(r.rect.center));
        }

        private IEnumerator MouseFrame(Vector2 p, bool down = false)
        {
            var s = new MouseState { position = p };
            if (down) s = s.WithButton(MouseButton.Left);
            InputSystem.QueueStateEvent(mouse, s);
            yield return null;
            yield return null;
        }

        private IEnumerator Click(Component c)
        {
            var p = Center(c);
            yield return MouseFrame(p);
            yield return MouseFrame(p, true);
            yield return MouseFrame(p);
        }

        [UnityTest]
        public IEnumerator AchievementView_OpensAndCloses_LocksAndUnlocksInput()
        {
            Assert.That(view, Is.Not.Null);
            Assert.That(view.IsVisible, Is.False);
            Assert.That(b.Input.GameplayAllowed, Is.True);

            // Open panel
            yield return Click(view.OpenButton);
            Assert.That(view.IsVisible, Is.True);
            Assert.That(b.Input.GameplayAllowed, Is.False);

            // Close panel
            view.Close();
            yield return null;
            Assert.That(view.IsVisible, Is.False);
            Assert.That(b.Input.GameplayAllowed, Is.True);
        }

        [UnityTest]
        public IEnumerator MilestoneInScene_UpgradeAndClaimCoin_SavesAndPersistsAcrossReload()
        {
            Assert.That(b.State.permanent.coin, Is.EqualTo(0));

            // Upgrade root to level 1
            b.State.run.water = 100;
            var upRes = b.Upgrades.TryUpgrade("root");
            Assert.That(upRes.Success, Is.True);

            // Check milestone detection in live system
            var rootStatus = b.Achievements.GetStatus("first_root");
            Assert.That(rootStatus.CurrentProgress, Is.EqualTo(1));
            Assert.That(rootStatus.CanClaim, Is.True);

            // Open view and claim
            view.Open();
            yield return null;
            view.OnClaimClicked("first_root");
            yield return null;

            // Verify coin awarded and claimed
            Assert.That(b.State.permanent.coin, Is.EqualTo(10));
            Assert.That(b.Achievements.GetStatus("first_root").IsClaimed, Is.True);
            Assert.That(b.Achievements.GetStatus("first_root").CanClaim, Is.False);

            // Duplicate claim does nothing
            view.OnClaimClicked("first_root");
            Assert.That(b.State.permanent.coin, Is.EqualTo(10));

            view.Close();

            // Force save and reload scene
            b.SaveCtrl.ForceSave("T08A_TEST_RELOAD");
            yield return Reload();

            // Verify persistence
            Assert.That(b.State.permanent.coin, Is.EqualTo(10));
            var reloadedRoot = b.Achievements.GetStatus("first_root");
            Assert.That(reloadedRoot.IsClaimed, Is.True);
            Assert.That(reloadedRoot.CurrentProgress, Is.EqualTo(1));
            Assert.That(reloadedRoot.CanClaim, Is.False);

            // HUD displays coin
            var hud = Object.FindAnyObjectByType<FoundationHudView>();
            Assert.That(hud.ResourceContent, Does.Contain("Coin: 10"));
        }

        [UnityTest]
        public IEnumerator RebirthInScene_PreservesAchievementsAndCoin()
        {
            // Set up state with coin and claim
            b.Achievements.TryUpdateProgress("first_root", 1);
            var claimRes = b.Achievements.TryClaim("first_root");
            Assert.That(claimRes.Success, Is.True);
            Assert.That(b.State.permanent.coin, Is.EqualTo(10));

            // Unlock Rebirth by setting stage to young_tree
            b.State.run.stageId = "young_tree";
            string runId = b.State.run.runId;
            var rebirthRes = b.Rebirth.TryRebirth(runId);
            Assert.That(rebirthRes.Success, Is.True);

            // Verify run reset to seedling, but coin and achievements preserved
            Assert.That(b.State.run.stageId, Is.EqualTo("seedling"));
            Assert.That(b.State.permanent.coin, Is.EqualTo(10));
            Assert.That(b.Achievements.GetStatus("first_root").IsClaimed, Is.True);

            // Rebirth achievement is also completed now!
            Assert.That(b.Achievements.GetStatus("first_rebirth").CurrentProgress, Is.EqualTo(1));
            Assert.That(b.Achievements.GetStatus("first_rebirth").CanClaim, Is.True);

            // Claim rebirth achievement
            var rebirthClaim = b.Achievements.TryClaim("first_rebirth");
            Assert.That(rebirthClaim.Success, Is.True);
            Assert.That(b.State.permanent.coin, Is.EqualTo(20));
            yield return null;
        }
    }
}
