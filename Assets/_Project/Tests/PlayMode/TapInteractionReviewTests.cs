using System.Collections;
using CayTheGioi.Bootstrap;
using CayTheGioi.Presentation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace CayTheGioi.Tests
{
    public sealed class TapInteractionReviewTests
    {
        private GameBootstrapper b;
        private TreeView tree;
        private OrbitCameraRig rig;
        private Mouse mouse, priorMouse;
        private Touchscreen touch, priorTouch;
        private int touchId = 900;

        [UnitySetUp]
        public IEnumerator Setup()
        {
            SaveTestScope.Begin();
            yield return SceneManager.LoadSceneAsync("MainScene");
            yield return null;
            b = Object.FindAnyObjectByType<GameBootstrapper>();
            b.TickDriver.ManualTick = true;
            b.Input.SendMessage("OnApplicationFocus", true);
            tree = Object.FindAnyObjectByType<TreeView>();
            rig = Object.FindAnyObjectByType<OrbitCameraRig>();
            priorMouse = Mouse.current; priorTouch = Touchscreen.current;
            mouse = InputSystem.AddDevice<Mouse>();
            touch = InputSystem.AddDevice<Touchscreen>();
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator Teardown()
        {
            if (mouse != null && mouse.added) InputSystem.RemoveDevice(mouse);
            if (touch != null && touch.added) InputSystem.RemoveDevice(touch);
            if (priorMouse != null && priorMouse.added) priorMouse.MakeCurrent();
            if (priorTouch != null && priorTouch.added) priorTouch.MakeCurrent();
            SaveTestScope.End();
            yield return null;
        }

        private IEnumerator Tap(Vector2 point, bool finger)
        {
            // Let mouse suppression clear after a previous touch before starting another gesture.
            InputSystem.QueueStateEvent(mouse, new MouseState { position = point });
            yield return null; yield return null;
            if (finger)
                InputSystem.QueueStateEvent(touch, new TouchState { touchId = ++touchId, phase = UnityEngine.InputSystem.TouchPhase.Began, position = point });
            else
                InputSystem.QueueStateEvent(mouse, new MouseState { position = point }.WithButton(MouseButton.Left));
            yield return null; yield return null;
            if (finger)
                InputSystem.QueueStateEvent(touch, new TouchState { touchId = touchId, phase = UnityEngine.InputSystem.TouchPhase.Ended, position = point });
            else
                InputSystem.QueueStateEvent(mouse, new MouseState { position = point });
            yield return null; yield return null;
        }

        private Vector2 InteriorPoint()
        {
            var sphere = tree.GetComponent<SphereCollider>();
            Vector2 center = rig.SceneCamera.WorldToScreenPoint(tree.transform.TransformPoint(sphere.center));
            if (!b.Input.IsOverUi(center)) return center;
            foreach (var offset in new[] { new Vector2(0,-30), new Vector2(-30,0), new Vector2(30,0), new Vector2(0,30) })
                if (!b.Input.IsOverUi(center + offset)) return center + offset;
            Assert.Fail("Tap area center covered by UI");
            return center;
        }

        [UnityTest]
        public IEnumerator SphericalGapsAcceptTapsFromEveryYawAndBlockOutsideAndFrontOccluder()
        {
            foreach (var step in b.Tutorial.StepDefinitions)
                b.State.permanent.tutorialCompletedStepIds.Add(step.StepId);
            Object.FindAnyObjectByType<TutorialView>().Refresh();
            foreach (string stage in new[] { "seedling", "young_tree", "large_tree" })
            {
                b.State.run.stageId = stage;
                var visual = b.Config.FindVisual(stage);
                tree.ShowStage(visual);
                rig.SetStagePose(visual.cameraDistance, visual.cameraPitch, visual.cameraTargetHeight);
                var sphere = tree.GetComponent<SphereCollider>();
                Assert.That(sphere.enabled, Is.True);
                Assert.That(sphere.isTrigger, Is.True);
                foreach (string weather in new[] { "calm", "thunderstorm" })
                {
                    b.Weather.ForceWeather(weather, 600);
                    foreach (float yaw in new[] { 0f,90f,180f,270f })
                    {
                        rig.SetYaw(yaw);
                        yield return null;
                        var centerWorld = tree.transform.TransformPoint(sphere.center);
                        // Off-center volume point: does not require touching a thin leaf polygon.
                        Vector2 point = rig.SceneCamera.WorldToScreenPoint(centerWorld + rig.SceneCamera.transform.right * sphere.radius * .35f);
                        Assert.That(b.Input.IsOverUi(point), Is.False);
                        int before = b.State.vitality.tapProgress;
                        yield return Tap(point, yaw == 90 || yaw == 270);
                        Assert.That(b.State.vitality.tapProgress, Is.EqualTo((before+1)%50), stage+" "+weather+" yaw="+yaw);
                    }
                }
                var target = InteriorPoint();
                var ray = rig.SceneCamera.ScreenPointToRay(target);
                Assert.That(sphere.Raycast(ray, out var entry, 100), Is.True);
                var blocker = GameObject.CreatePrimitive(PrimitiveType.Cube);
                try
                {
                    blocker.transform.position = ray.GetPoint(entry.distance * .5f);
                    blocker.transform.localScale = Vector3.one * .25f;
                    Physics.SyncTransforms();
                    int before = b.State.vitality.tapProgress;
                    yield return Tap(target, false);
                    Assert.That(b.State.vitality.tapProgress, Is.EqualTo(before), "Solid occluder in front of tap sphere");
                }
                finally { Object.DestroyImmediate(blocker); }
                Physics.SyncTransforms();
                Vector2 outside = new Vector2(Screen.width*.95f, Screen.height*.5f);
                Assert.That(sphere.Raycast(rig.SceneCamera.ScreenPointToRay(outside), out _, 100), Is.False);
                int previous = b.State.vitality.tapProgress;
                yield return Tap(outside, true);
                Assert.That(b.State.vitality.tapProgress, Is.EqualTo(previous), "Far background must not tap");
            }
        }

        [UnityTest]
        public IEnumerator FiftyRealGesturesRewardExactlyOnceAcrossStagesAndYawWithAnimation()
        {
            int rewards = 0, accepted = 0;
            b.Vitality.RewardEarned += () => rewards++;
            b.Vitality.Tapped += () => accepted++;
            var report = new System.Text.StringBuilder();
            foreach (string stage in new[] { "seedling", "young_tree", "large_tree" })
            {
                b.State.run.stageId = stage;
                b.State.vitality.tapProgress = 0; b.State.vitality.bankSeconds = 0;
                b.State.run.levels.root = 1;
                var visual = b.Config.FindVisual(stage);
                tree.ShowStage(visual);
                rig.SetStagePose(visual.cameraDistance, visual.cameraPitch, visual.cameraTargetHeight);
                b.Weather.ForceWeather("calm", 600);
                Assert.That(tree.GetComponent<TreeAnimationController>().enabled, Is.True);
                int previousRewards = rewards, previousAccepted = accepted;
                double baseline = b.Resources.Rates().WaterProduced;
                for (int i = 0; i < 50; i++)
                {
                    if (i % 10 == 0) rig.SetYaw(i / 10 * 72);
                    yield return null;
                    var point = InteriorPoint();
                    yield return Tap(point, i % 2 != 0);
                    Assert.That(accepted - previousAccepted, Is.EqualTo(i + 1), stage + " gesture " + (i + 1));
                    Assert.That(b.State.vitality.tapProgress, Is.EqualTo((i + 1) % 50));
                    Assert.That(rewards - previousRewards, Is.EqualTo(i == 49 ? 1 : 0));
                }
                Assert.That(b.State.vitality.bankSeconds, Is.EqualTo(20).Within(.001));
                Assert.That(b.Resources.Rates().WaterProduced, Is.EqualTo(baseline * 1.05).Within(.00001));
                report.AppendLine(stage + ": 25 mouse + 25 touch, five yaw angles, reward=1, bank=20s, Water rate=1.05x");
            }
            System.IO.Directory.CreateDirectory("Evidence/TapFix_Recheck_20261012");
            System.IO.File.WriteAllText("Evidence/TapFix_Recheck_20261012/fifty-tap-results.txt", report.ToString());
        }
    }
}
