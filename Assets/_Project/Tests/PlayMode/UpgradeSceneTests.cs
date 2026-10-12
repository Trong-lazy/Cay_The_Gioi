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
    public sealed class UpgradeSceneTests
    {
        private GameBootstrapper b;
        private UpgradePanelView view;
        private Mouse mouse, priorMouse;
        private Touchscreen touch, priorTouch;
        [UnitySetUp]
        public IEnumerator Setup()
        {
            SaveTestScope.Begin();
            yield return SceneManager.LoadSceneAsync("MainScene"); yield return null;
            b = Object.FindAnyObjectByType<GameBootstrapper>(); b.TickDriver.ManualTick = true;
            b.Input.SendMessage("OnApplicationFocus", true);
            view = Object.FindAnyObjectByType<UpgradePanelView>(); Assert.That(view, Is.Not.Null);
            priorMouse = Mouse.current; priorTouch = Touchscreen.current;
            mouse = InputSystem.AddDevice<Mouse>(); touch = InputSystem.AddDevice<Touchscreen>(); yield return null;
        }
        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (mouse != null && mouse.added) InputSystem.RemoveDevice(mouse);
            if (touch != null && touch.added) InputSystem.RemoveDevice(touch);
            if (priorMouse != null && priorMouse.added) priorMouse.MakeCurrent();
            if (priorTouch != null && priorTouch.added) priorTouch.MakeCurrent();
            SaveTestScope.End();
            yield return null;
        }
        private Vector2 Center(Component component)
        {
            Canvas.ForceUpdateCanvases(); var rect = (RectTransform)component.transform;
            return RectTransformUtility.WorldToScreenPoint(null, rect.TransformPoint(rect.rect.center));
        }
        private IEnumerator MouseFrame(Vector2 point, bool down)
        {
            var s = new MouseState { position = point }; if (down) s = s.WithButton(MouseButton.Left);
            InputSystem.QueueStateEvent(mouse, s); yield return null; yield return null;
        }
        private IEnumerator Click(Component target)
        { var p = Center(target); yield return MouseFrame(p, false); yield return MouseFrame(p, true); yield return MouseFrame(p, false); }
        private IEnumerator TouchFrame(int id, TouchPhase phase, Vector2 point)
        {
            InputSystem.QueueStateEvent(touch, new TouchState { touchId = id, phase = phase, position = point }); yield return null; yield return null;
        }
        [UnityTest]
        public IEnumerator MouseOpenBuyOnceHoldAndCloseUpdateHudWithoutTap()
        {
            Assert.That(view.IsOpen, Is.False); yield return Click(view.OpenButton); Assert.That(view.IsOpen, Is.True);
            var root = view.FindRow("root"); Assert.That(root.details.text, Does.Contain("L0/5")); Assert.That(root.details.text, Does.Contain("5 Water"));
            Assert.That(view.FindRow("spirit_absorption").reason.text, Does.Contain("Cây Trưởng Thành")); Assert.That(view.FindRow("spirit_absorption").button.interactable, Is.False);
            int events = 0; b.Upgrades.Completed += _ => events++;
            var p = Center(root.button); yield return MouseFrame(p, true); yield return new WaitForSecondsRealtime(0.5f);
            Assert.That(b.State.run.levels.root, Is.Zero, "UI hold never auto-repeats.");
            yield return MouseFrame(p, false);
            Assert.That(b.State.run.levels.root, Is.EqualTo(1)); Assert.That(b.State.run.water, Is.EqualTo(5)); Assert.That(events, Is.EqualTo(1));
            Assert.That(root.details.text, Does.Contain("8 Water")); Assert.That(root.reason.text, Does.Contain("Thiếu"));
            Assert.That(Object.FindAnyObjectByType<FoundationHudView>().ResourceContent, Does.Contain("+1/s"));
            yield return Click(root.button); Assert.That(b.State.run.levels.root, Is.EqualTo(1)); Assert.That(events, Is.EqualTo(1));
            yield return Click(view.Panel.Find("Close").GetComponent<Button>()); Assert.That(view.IsOpen, Is.False);
            Assert.That(b.State.vitality.tapProgress, Is.Zero);
        }
        [UnityTest]
        public IEnumerator SimulatedTouchUiBuysOneAndBlocksTreeBelowButton()
        {
            var openPoint = Center(view.OpenButton);
            yield return TouchFrame(1, TouchPhase.Began, openPoint); yield return TouchFrame(1, TouchPhase.Ended, openPoint);
            Assert.That(view.IsOpen, Is.True);
            var root = view.FindRow("root"); var rect = (RectTransform)root.button.transform;
            var treeView = Object.FindAnyObjectByType<TreeView>();
            Vector2 treePoint = Camera.main.WorldToScreenPoint(treeView.transform.TransformPoint(treeView.GetComponent<SphereCollider>().center));
            rect.position += (Vector3)(treePoint - Center(root.button));
            Canvas.ForceUpdateCanvases(); var point = Center(root.button); Assert.That(b.Input.IsOverUi(point), Is.True);
            int events = 0; b.Upgrades.Completed += _ => events++;
            yield return TouchFrame(2, TouchPhase.Began, point); yield return TouchFrame(2, TouchPhase.Ended, point);
            Assert.That(b.State.run.levels.root, Is.EqualTo(1)); Assert.That(events, Is.EqualTo(1)); Assert.That(b.State.run.water, Is.EqualTo(5));
            Assert.That(b.State.vitality.tapProgress, Is.Zero, "Touch UI cannot tap the tree beneath.");
        }
        [UnityTest]
        public IEnumerator FreshRunLoopsRootThenLeavesAndBuffUpdatesQuotes()
        {
            b.Upgrades.TryUpgrade("root"); b.Upgrades.TryUpgrade("leaves");
            Assert.That(b.State.run.water, Is.EqualTo(5)); b.Resources.Tick(10);
            Assert.That(b.State.run.water, Is.EqualTo(12.5)); Assert.That(b.State.run.growth, Is.EqualTo(10));
            for (int i = 0; i < 50; i++) b.Vitality.TryTap();
            Assert.That(view.FindRow("root").details.text, Does.Contain("1.05"));
            Assert.That(view.FindRow("leaves").details.text, Does.Contain("1.05"));
            b.State.run.water = 100; Assert.That(b.Upgrades.TryUpgrade("root").Success, Is.True);
            Assert.That(b.Resources.Rates().WaterProduced, Is.EqualTo(2.1)); Assert.That(b.Resources.Rates().WaterConsumed, Is.EqualTo(0.25));
            Assert.That(Object.FindAnyObjectByType<FoundationHudView>().ResourceContent, Does.Contain("+1.85/s"));
            b.State.run.stageId = "young_tree"; view.Refresh(); yield return Click(view.OpenButton);
            yield return Click(view.FindRow("trunk").button);
            Assert.That(b.State.run.levels.trunk, Is.EqualTo(1)); Assert.That(b.Upgrades.Protection, Is.EqualTo(0.05));
            Assert.That(view.FindRow("trunk").details.text, Does.Contain("Growth/s"));
        }
        [UnityTest]
        public IEnumerator CapDeadAndRebindDoNotDuplicateListeners()
        {
            view.Bind(b.Upgrades, b.Resources, b.Vitality); view.Bind(b.Upgrades, b.Resources, b.Vitality);
            b.State.run.water = 100; yield return Click(view.OpenButton); yield return Click(view.FindRow("root").button);
            Assert.That(b.State.run.levels.root, Is.EqualTo(1)); Assert.That(b.State.run.water, Is.EqualTo(95));
            b.State.run.levels.root = 5; view.Refresh(); Assert.That(view.FindRow("root").reason.text, Does.Contain("cap")); Assert.That(view.FindRow("root").button.interactable, Is.False);
            b.State.run.life = "DEAD"; view.Refresh(); Assert.That(view.FindRow("leaves").reason.text, Does.Contain("chết"));
            yield return Click(view.FindRow("leaves").button); Assert.That(b.State.run.levels.leaves, Is.Zero); Assert.That(b.State.run.water, Is.EqualTo(95));
        }

        [UnityTest]
        public IEnumerator MouseAndTouchShowAndSpendGrowthWithoutWaterFallback()
        {
            Assert.That(b.Upgrades.TryUpgrade("root").Success, Is.True);
            Assert.That(b.Upgrades.TryUpgrade("leaves").Success, Is.True);
            b.State.run.water = 100; b.State.run.photosynthesis=0; b.State.run.growth = 0; view.Refresh();
            yield return Click(view.OpenButton);
            var leaves = view.FindRow("leaves");
            Assert.That(leaves.details.text, Does.Contain("8 Quang hợp"));
            Assert.That(leaves.reason.text, Is.EqualTo("Thiếu Quang hợp")); Assert.That(leaves.button.interactable, Is.False);
            yield return Click(leaves.button); Assert.That(b.State.run.levels.leaves, Is.EqualTo(1));
            b.State.run.photosynthesis = 8; view.Refresh(); yield return Click(leaves.button);
            Assert.That(b.State.run.levels.leaves, Is.EqualTo(2)); Assert.That(b.State.run.photosynthesis, Is.Zero); Assert.That(b.State.run.water, Is.EqualTo(100));
            b.State.run.stageId = "young_tree"; b.State.run.growth = 10; view.Refresh();
            var trunk = view.FindRow("trunk"); Assert.That(trunk.details.text, Does.Contain("10 Growth"));
            var point = Center(trunk.button);
            yield return TouchFrame(51, TouchPhase.Began, point); yield return TouchFrame(51, TouchPhase.Ended, point);
            Assert.That(b.State.run.levels.trunk, Is.EqualTo(1)); Assert.That(b.State.run.growth, Is.Zero); Assert.That(b.State.run.water, Is.EqualTo(100));
            Assert.That(b.State.vitality.tapProgress, Is.Zero);
        }
        [UnityTest]
        public IEnumerator PanelAndButtonsStayWithinPortraitAndLandscapeSafeArea()
        {
            var layout = Object.FindAnyObjectByType<SafeAreaHudLayout>(); layout.enabled = false;
            foreach (var dimensions in new[] { new Vector2(1080, 1920), new Vector2(1920, 1080) })
            {
                layout.Apply(new Rect(40, 40, dimensions.x - 80, dimensions.y - 80), dimensions); view.Layout(dimensions.x < dimensions.y);
                var composition=Object.FindAnyObjectByType<ReferenceUiLayout>();Canvas.ForceUpdateCanvases();composition.Layout();Canvas.ForceUpdateCanvases();
                var safe=(RectTransform)view.Panel.parent;var corners=new Vector3[4];view.Panel.GetWorldCorners(corners);
                foreach(var corner in corners){var local=safe.InverseTransformPoint(corner);Assert.That(local.x,Is.InRange(safe.rect.xMin-.1f,safe.rect.xMax+.1f));Assert.That(local.y,Is.InRange(safe.rect.yMin-.1f,safe.rect.yMax+.1f));}
                Assert.That(((RectTransform)view.OpenButton.transform).sizeDelta.y, Is.GreaterThanOrEqualTo(88));
            }
            // Let CanvasScaler update against the real Game View dimensions before checking rendered text.
            layout.Apply(Screen.safeArea, new Vector2(Screen.width, Screen.height));
            layout.enabled = true; yield return null;
            view.Layout(Screen.width < Screen.height); Canvas.ForceUpdateCanvases();
            foreach (var id in new[] { "root", "leaves", "trunk" })
            {
                var details = view.FindRow(id).details;
                Assert.That(details.preferredHeight, Is.LessThanOrEqualTo(((RectTransform)details.transform).rect.height),
                    id + " output/consumption text must fit without vertical clipping.");
            }
        }

        [UnityTest]
        public IEnumerator MatureSpiritUnlockPaysThreeWalletsAndPersistsProduction()
        {
            b.State.run.stageId="mature_tree";b.State.run.water=1000;b.State.run.photosynthesis=1000;b.State.run.growth=1000;
            view.Refresh();yield return Click(view.OpenButton);
            var spirit=view.FindRow("spirit_absorption");
            Assert.That(spirit.details.text,Does.Contain("1K Water"));
            yield return Click(spirit.button);
            Assert.That(b.State.run.spiritAbsorptionUnlocked,Is.True);
            Assert.That(b.State.run.levels.spiritAbsorption,Is.EqualTo(1));
            Assert.That(b.State.run.water+b.State.run.photosynthesis+b.State.run.growth,Is.Zero);
            b.Resources.Tick(16);Assert.That(b.State.run.spirit,Is.EqualTo(16));
            Assert.That(Object.FindAnyObjectByType<FoundationHudView>().ResourceContent,Does.Contain("Linh khí"));
            Assert.That(b.SaveCtrl.ForceSave("SPIRIT_ROUNDTRIP").Success,Is.True);
            yield return SceneManager.LoadSceneAsync("MainScene");yield return null;
            b=Object.FindAnyObjectByType<GameBootstrapper>();b.TickDriver.ManualTick=true;
            Assert.That(b.State.run.spiritAbsorptionUnlocked,Is.True);
            Assert.That(b.State.run.levels.spiritAbsorption,Is.EqualTo(1));
            Assert.That(b.State.run.spirit,Is.GreaterThanOrEqualTo(16));
            Assert.That(b.State.run.stageId,Is.EqualTo("mature_tree"));
        }
    }
}
