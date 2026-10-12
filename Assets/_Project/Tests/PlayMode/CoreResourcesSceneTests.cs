using System.Collections;
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
using UnityEngine.UI;
using TouchPhase = UnityEngine.InputSystem.TouchPhase;

namespace CayTheGioi.Tests
{
    public sealed class CoreResourcesSceneTests
    {
        private GameBootstrapper b;
        private Mouse mouse, priorMouse;
        private Touchscreen touch, priorTouch;
        private Vector2 treePoint;
        [UnitySetUp]
        public IEnumerator Setup()
        {
            SaveTestScope.Begin();
            yield return SceneManager.LoadSceneAsync("MainScene"); yield return null;
            b = Object.FindAnyObjectByType<GameBootstrapper>(); b.TickDriver.ManualTick = true;
            b.Input.SendMessage("OnApplicationFocus", true); b.TickDriver.SendMessage("OnApplicationFocus", true);
            b.State.vitality.tapProgress = 0;
            priorMouse = Mouse.current; priorTouch = Touchscreen.current;
            mouse = InputSystem.AddDevice<Mouse>(); touch = InputSystem.AddDevice<Touchscreen>();
            var treeView = Object.FindAnyObjectByType<TreeView>();
            treePoint = Camera.main.WorldToScreenPoint(treeView.transform.TransformPoint(treeView.GetComponent<SphereCollider>().center));
            Assert.That(b.Input.IsOverUi(treePoint), Is.False, "Tree must remain outside the HUD hit region.");
            yield return null;
        }
        [UnityTearDown]
        public IEnumerator TearDown()
        {
            InputSystem.RemoveDevice(mouse); InputSystem.RemoveDevice(touch);
            if (priorMouse != null && priorMouse.added) priorMouse.MakeCurrent();
            if (priorTouch != null && priorTouch.added) priorTouch.MakeCurrent();
            SaveTestScope.End();
            yield return null;
        }
        private IEnumerator MouseFrame(Vector2 position, int button = -1)
        {
            var s = new MouseState { position = position };
            if (button >= 0) s = s.WithButton((MouseButton)button);
            InputSystem.QueueStateEvent(mouse, s); yield return null; yield return null;
        }
        private IEnumerator TouchFrame(int id, TouchPhase phase, Vector2 point)
        {
            InputSystem.QueueStateEvent(touch, new TouchState { touchId = id, phase = phase, position = point });
            yield return null; yield return null;
        }
        [UnityTest]
        public IEnumerator MouseTapHoldDragUiAndRightOrbit()
        {
            yield return MouseFrame(treePoint, 0); Assert.That(b.State.vitality.tapProgress, Is.Zero);
            yield return MouseFrame(treePoint); Assert.That(b.State.vitality.tapProgress, Is.EqualTo(1));
            yield return MouseFrame(treePoint, 0); yield return new WaitForSecondsRealtime(0.5f); yield return MouseFrame(treePoint);
            Assert.That(b.State.vitality.tapProgress, Is.EqualTo(1), "Holding is not a tap.");
            yield return MouseFrame(treePoint, 0); yield return MouseFrame(treePoint + Vector2.right * 100, 0); yield return MouseFrame(treePoint);
            Assert.That(b.State.vitality.tapProgress, Is.EqualTo(1), "Returning after dragging must not reward.");
            var panel = Object.FindAnyObjectByType<FoundationHudView>().transform.Find("SafeArea/WaterCard") as RectTransform;
            var uiPoint = RectTransformUtility.WorldToScreenPoint(null, panel.TransformPoint(panel.rect.center));
            Assert.That(b.Input.IsOverUi(uiPoint), Is.True);
            yield return MouseFrame(uiPoint, 0); yield return MouseFrame(treePoint, 0); yield return MouseFrame(treePoint);
            Assert.That(b.State.vitality.tapProgress, Is.EqualTo(1), "UI-started gesture cannot tap through.");
            Quaternion before = Camera.main.transform.rotation;
            InputSystem.QueueStateEvent(mouse, new MouseState { position = treePoint }.WithButton(MouseButton.Right)); yield return null; yield return null;
            InputSystem.QueueStateEvent(mouse, new MouseState { position = treePoint + Vector2.right * 30, delta = new Vector2(30, 0) }.WithButton(MouseButton.Right)); yield return null; yield return null;
            Assert.That(Quaternion.Angle(before, Camera.main.transform.rotation), Is.GreaterThan(0.1));
            var orbit = Object.FindAnyObjectByType<OrbitCameraRig>(); orbit.OrbitLocked = true; before = Camera.main.transform.rotation;
            InputSystem.QueueStateEvent(mouse, new MouseState { position = treePoint + Vector2.right * 60, delta = new Vector2(30, 0) }.WithButton(MouseButton.Right)); yield return null; yield return null;
            Assert.That(Quaternion.Angle(before, Camera.main.transform.rotation), Is.LessThan(0.01));
            yield return MouseFrame(treePoint); Assert.That(b.State.vitality.tapProgress, Is.EqualTo(1));
        }
        [UnityTest]
        public IEnumerator SimulatedTouchTapAndMouseDuplicationSuppression()
        {
            InputSystem.QueueStateEvent(mouse, new MouseState { position = treePoint }.WithButton(MouseButton.Left));
            yield return TouchFrame(1, TouchPhase.Began, treePoint); Assert.That(b.State.vitality.tapProgress, Is.Zero);
            InputSystem.QueueStateEvent(mouse, new MouseState { position = treePoint });
            yield return TouchFrame(1, TouchPhase.Ended, treePoint);
            Assert.That(b.State.vitality.tapProgress, Is.EqualTo(1), "Concurrent synthetic mouse and touch award once.");
            yield return MouseFrame(treePoint, 0); yield return MouseFrame(treePoint);
            Assert.That(b.State.vitality.tapProgress, Is.EqualTo(2), "Fresh mouse input works again after touch release.");
        }
        [UnityTest]
        public IEnumerator SimulatedTouchDragHoldMultitouchCancelUiAndFocusLoss()
        {
            var before = Camera.main.transform.rotation;
            yield return TouchFrame(1, TouchPhase.Began, treePoint);
            yield return TouchFrame(1, TouchPhase.Moved, treePoint + Vector2.right * 80);
            Assert.That(Quaternion.Angle(before, Camera.main.transform.rotation), Is.GreaterThan(0.1));
            yield return TouchFrame(1, TouchPhase.Ended, treePoint); Assert.That(b.State.vitality.tapProgress, Is.Zero);
            yield return TouchFrame(2, TouchPhase.Began, treePoint); yield return new WaitForSecondsRealtime(0.5f); yield return TouchFrame(2, TouchPhase.Ended, treePoint);
            Assert.That(b.State.vitality.tapProgress, Is.Zero);
            yield return TouchFrame(3, TouchPhase.Began, treePoint); yield return TouchFrame(4, TouchPhase.Began, treePoint);
            yield return TouchFrame(3, TouchPhase.Ended, treePoint); yield return TouchFrame(4, TouchPhase.Ended, treePoint);
            Assert.That(b.State.vitality.tapProgress, Is.Zero);
            yield return TouchFrame(5, TouchPhase.Began, treePoint); yield return TouchFrame(5, TouchPhase.Canceled, treePoint);
            Assert.That(b.State.vitality.tapProgress, Is.Zero);
            var panel = Object.FindAnyObjectByType<FoundationHudView>().transform.Find("SafeArea/WaterCard") as RectTransform;
            var ui = RectTransformUtility.WorldToScreenPoint(null, panel.TransformPoint(panel.rect.center));
            yield return TouchFrame(6, TouchPhase.Began, ui); yield return TouchFrame(6, TouchPhase.Ended, treePoint);
            Assert.That(b.State.vitality.tapProgress, Is.Zero);
            yield return TouchFrame(7, TouchPhase.Began, treePoint); b.Input.SendMessage("OnApplicationFocus", false);
            b.Input.SendMessage("OnApplicationFocus", true); yield return TouchFrame(7, TouchPhase.Ended, treePoint);
            Assert.That(b.State.vitality.tapProgress, Is.Zero);
            yield return TouchFrame(8, TouchPhase.Began, treePoint); yield return TouchFrame(8, TouchPhase.Ended, treePoint);
            Assert.That(b.State.vitality.tapProgress, Is.EqualTo(1), "New gesture works after cancellation.");
        }
        [UnityTest]
        public IEnumerator OnlineTickHudPauseAndDead()
        {
            b.State.run.water = 0; b.State.run.levels.root = 1; b.TickDriver.ManualTick = false;
            yield return new WaitForSecondsRealtime(0.2f);
            Assert.That(b.State.run.water, Is.GreaterThan(0));
            b.TickDriver.SendMessage("OnApplicationPause", true); double water = b.State.run.water; b.State.vitality.bankSeconds = 5;
            yield return new WaitForSecondsRealtime(0.2f);
            Assert.That(b.State.run.water, Is.EqualTo(water)); Assert.That(b.State.vitality.bankSeconds, Is.EqualTo(5));
            b.TickDriver.SendMessage("OnApplicationPause", false); b.TickDriver.ManualTick = true;
            b.State.run.water = 0; b.Resources.Tick(10);
            var hud = Object.FindAnyObjectByType<FoundationHudView>();
            Assert.That(hud.ResourceContent, Does.Contain("10.25")); Assert.That(hud.ResourceContent, Does.Contain("Tap 0/50"));
            b.State.run.life = "DEAD"; b.State.vitality.bankSeconds = 5; water = b.State.run.water;
            yield return MouseFrame(treePoint, 0); yield return MouseFrame(treePoint); b.Resources.Tick(10);
            Assert.That(b.State.vitality.tapProgress, Is.Zero); Assert.That(b.State.run.water, Is.EqualTo(water)); Assert.That(b.State.vitality.bankSeconds, Is.EqualTo(5));
        }
        [UnityTest]
        public IEnumerator SafeAreaPortraitAndLandscapeKeepHudInsideInsets()
        {
            var layout = Object.FindAnyObjectByType<SafeAreaHudLayout>();
            layout.enabled = false;
            layout.Apply(new Rect(40, 60, 1000, 1800), new Vector2(1080, 1920));
            var safe = layout.transform.Find("SafeArea") as RectTransform;
            Assert.That(safe.anchorMin.x, Is.EqualTo(40f / 1080).Within(1e-6)); Assert.That(safe.anchorMax.y, Is.EqualTo(1860f / 1920).Within(1e-6));
            Assert.That(layout.GetComponent<CanvasScaler>().referenceResolution, Is.EqualTo(new Vector2(1080, 1920)));
            layout.Apply(new Rect(60, 40, 1800, 1000), new Vector2(1920, 1080));
            Assert.That(layout.GetComponent<CanvasScaler>().referenceResolution, Is.EqualTo(new Vector2(1920, 1080)));
            layout.enabled = true; yield return null;
        }
    }
}
