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
    public sealed class CosmeticSceneTests
    {
        private GameBootstrapper b;
        private ShopView view;
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

            view = Object.FindAnyObjectByType<ShopView>();
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
            view = Object.FindAnyObjectByType<ShopView>();
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
        public IEnumerator ShopView_OpenAndClose_LocksAndUnlocksInput()
        {
            Assert.That(view, Is.Not.Null);
            Assert.That(view.IsVisible, Is.False);
            Assert.That(b.Input.GameplayAllowed, Is.True);

            // Open
            yield return Click(view.OpenButton);
            Assert.That(view.IsVisible, Is.True);
            Assert.That(b.Input.GameplayAllowed, Is.False);

            // Close
            view.Close();
            yield return null;
            Assert.That(view.IsVisible, Is.False);
            Assert.That(b.Input.GameplayAllowed, Is.True);
        }

        [UnityTest]
        public IEnumerator Preview_DoesNotDeductCoin_AndDoesNotMutateSavedEquip_AndRestoresOnClose()
        {
            b.State.permanent.coin = 25;
            string originalEquip = b.Cosmetics.EquippedCosmeticId;
            Assert.That(originalEquip, Is.EqualTo("none"));

            view.Open();
            yield return null;

            // Toggle preview on
            view.OnPreviewToggle("glow_placeholder");
            yield return null;

            Assert.That(b.CosmeticVisual.IsPreviewing, Is.True);
            Assert.That(b.CosmeticVisual.ActiveVisualId, Is.EqualTo("glow_placeholder"));
            Assert.That(b.State.permanent.coin, Is.EqualTo(25)); // Coin completely untouched
            Assert.That(b.Cosmetics.EquippedCosmeticId, Is.EqualTo("none")); // Saved equip unchanged

            // Close shop -> must cancel preview and restore saved visual
            view.Close();
            yield return null;

            Assert.That(b.CosmeticVisual.IsPreviewing, Is.False);
            Assert.That(b.CosmeticVisual.ActiveVisualId, Is.EqualTo("none"));
            Assert.That(b.State.permanent.coin, Is.EqualTo(25));
            Assert.That(b.Cosmetics.EquippedCosmeticId, Is.EqualTo("none"));
        }

        [UnityTest]
        public IEnumerator BuyAndEquipInScene_DeductsCoin_PersistsAcrossReload()
        {
            b.State.permanent.coin = 25;
            view.Open();
            yield return null;

            // Buy
            view.OnActionClicked("glow_placeholder");
            yield return null;

            Assert.That(b.State.permanent.coin, Is.EqualTo(5));
            Assert.That(b.Cosmetics.IsOwned("glow_placeholder"), Is.True);
            Assert.That(b.Cosmetics.EquippedCosmeticId, Is.EqualTo("none")); // Not auto equipped

            // Equip
            view.OnActionClicked("glow_placeholder");
            yield return null;

            Assert.That(b.Cosmetics.EquippedCosmeticId, Is.EqualTo("glow_placeholder"));
            Assert.That(b.CosmeticVisual.ActiveVisualId, Is.EqualTo("glow_placeholder"));

            view.Close();

            // Force save and reload
            b.SaveCtrl.ForceSave("T08B_TEST_RELOAD");
            yield return Reload();

            // Verify persistence after reload
            Assert.That(b.State.permanent.coin, Is.EqualTo(5));
            Assert.That(b.Cosmetics.IsOwned("glow_placeholder"), Is.True);
            Assert.That(b.Cosmetics.EquippedCosmeticId, Is.EqualTo("glow_placeholder"));
            Assert.That(b.CosmeticVisual.ActiveVisualId, Is.EqualTo("glow_placeholder"));
        }

        [UnityTest]
        public IEnumerator DeadOrRebirth_CancelsPreview()
        {
            view.Open();
            yield return null;

            view.OnPreviewToggle("glow_placeholder");
            yield return null;
            Assert.That(b.CosmeticVisual.IsPreviewing, Is.True);

            // Rebirth cancels preview
            b.State.run.stageId = "young_tree";
            string runId = b.State.run.runId;
            b.Rebirth.TryRebirth(runId);
            yield return null;

            Assert.That(b.CosmeticVisual.IsPreviewing, Is.False);
        }
    }
}
