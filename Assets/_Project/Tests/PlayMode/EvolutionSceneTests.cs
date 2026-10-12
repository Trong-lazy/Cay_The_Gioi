using System.Collections;
using System.Text.RegularExpressions;
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
using TouchPhase=UnityEngine.InputSystem.TouchPhase;

namespace CayTheGioi.Tests
{
    public sealed class EvolutionSceneTests
    {
        private GameBootstrapper b;
        private EvolutionPanelView view;
        private UpgradePanelView upgrade;
        private OrbitCameraRig rig;
        private TreeView tree;
        private Mouse mouse,priorMouse;
        private Touchscreen touch,priorTouch;
        private Vector2 mousePoint;
        [UnitySetUp] public IEnumerator Setup()
        {
            SaveTestScope.Begin();
            yield return SceneManager.LoadSceneAsync("MainScene");yield return null;
            b=Object.FindAnyObjectByType<GameBootstrapper>();b.TickDriver.ManualTick=true;b.Input.SendMessage("OnApplicationFocus",true);
            view=Object.FindAnyObjectByType<EvolutionPanelView>();upgrade=Object.FindAnyObjectByType<UpgradePanelView>();rig=Object.FindAnyObjectByType<OrbitCameraRig>();tree=Object.FindAnyObjectByType<TreeView>();
            Assert.That(view,Is.Not.Null);priorMouse=Mouse.current;priorTouch=Touchscreen.current;mouse=InputSystem.AddDevice<Mouse>();touch=InputSystem.AddDevice<Touchscreen>();yield return null;
        }
        [UnityTearDown] public IEnumerator TearDown()
        {
            if(mouse!=null&&mouse.added)InputSystem.RemoveDevice(mouse);if(touch!=null&&touch.added)InputSystem.RemoveDevice(touch);
            if(priorMouse!=null&&priorMouse.added)priorMouse.MakeCurrent();if(priorTouch!=null&&priorTouch.added)priorTouch.MakeCurrent();
            SaveTestScope.End();
            yield return null;
        }
        private Vector2 Center(Component c) {Canvas.ForceUpdateCanvases();var r=(RectTransform)c.transform;return RectTransformUtility.WorldToScreenPoint(null,r.TransformPoint(r.rect.center));}
        private IEnumerator MouseFrame(Vector2 p,bool left=false,bool right=false)
        {var s=new MouseState{position=p,delta=p-mousePoint};mousePoint=p;if(left)s=s.WithButton(MouseButton.Left);if(right)s=s.WithButton(MouseButton.Right);InputSystem.QueueStateEvent(mouse,s);yield return null;yield return null;}
        private IEnumerator Click(Component c) {var p=Center(c);yield return MouseFrame(p);yield return MouseFrame(p,true);yield return MouseFrame(p);}
        private IEnumerator TouchFrame(int id,TouchPhase phase,Vector2 p)
        {InputSystem.QueueStateEvent(touch,new TouchState{touchId=id,phase=phase,position=p});yield return null;yield return null;}
        private void Fund() {b.State.run.water=1000;b.State.run.growth=1000;b.State.run.photosynthesis=1000;view.Refresh();upgrade.Refresh();}
        private IEnumerator Finish() {yield return new WaitForSecondsRealtime(b.Config.EvolutionTransitionSeconds+0.1f);Assert.That(b.Evolution.IsTransitioning,Is.False);Assert.That(b.Input.GameplayAllowed,Is.True);Assert.That(rig.TransitionLocked,Is.False);}
        [UnityTest] public IEnumerator MouseButtonEvolvesBothStagesOnceUpdatesViewCapsHudAndCamera()
        {
            Fund();b.State.run.levels.root=5;b.State.run.levels.leaves=3;view.Refresh();upgrade.Refresh();
            Assert.That(tree.CurrentStageId,Is.EqualTo("seedling"));yield return Click(upgrade.OpenButton);
            Assert.That(view.Details,Does.Contain("50 Water"));Vector3 before=rig.SceneCamera.transform.position;
            yield return Click(view.EvolveButton);Assert.That(b.State.run.stageId,Is.EqualTo("young_tree"));Assert.That(b.State.run.water,Is.EqualTo(950));Assert.That(b.State.run.growth,Is.EqualTo(975));
            Assert.That(b.Evolution.TryEvolve().Reason,Is.EqualTo("BUSY"));Assert.That(b.Upgrades.TryUpgrade("root").Reason,Is.EqualTo("BUSY"));
            yield return Click(view.EvolveButton);Assert.That(b.State.run.water,Is.EqualTo(950));Assert.That(b.State.run.stageId,Is.EqualTo("young_tree"));
            yield return Finish();Assert.That(tree.CurrentStageId,Is.EqualTo("young_tree"));Assert.That(tree.ModelSlot.childCount,Is.EqualTo(1));Assert.That(rig.Distance,Is.EqualTo(b.Config.FindVisual("young_tree").cameraDistance));Assert.That(rig.SceneCamera.transform.position,Is.Not.EqualTo(before));
            Assert.That(upgrade.FindRow("root").details.text,Does.Contain("L5/10"));Assert.That(upgrade.FindRow("trunk").button.interactable,Is.True);Assert.That(b.Upgrades.TryUpgrade("root").Success,Is.True);
            view.Refresh();double water=b.State.run.water;yield return Click(view.EvolveButton);yield return Finish();
            Assert.That(b.State.run.stageId,Is.EqualTo("large_tree"));Assert.That(b.State.run.water,Is.EqualTo(water-200));Assert.That(b.State.run.growth,Is.EqualTo(875));Assert.That(b.State.run.levels.root,Is.EqualTo(6));Assert.That(b.State.run.levels.leaves,Is.EqualTo(3));
            Assert.That(tree.CurrentStageId,Is.EqualTo("large_tree"));Assert.That(tree.ModelSlot.childCount,Is.EqualTo(1));Assert.That(rig.Distance,Is.EqualTo(b.Config.FindVisual("large_tree").cameraDistance));Assert.That(view.EvolveButton.interactable,Is.True);Assert.That(view.Details,Does.Contain("Cây Trưởng Thành"));
            Assert.That(tree.LeavesAnchor.position.y,Is.GreaterThan(tree.TrunkAnchor.position.y));Assert.That(tree.GetComponent<SphereCollider>().radius,Is.GreaterThan(1));Assert.That(b.State.vitality.tapProgress,Is.Zero);
            foreach(var entry in b.State.permanent.stagePoints)Assert.That(entry.points,Is.Zero);
            upgrade.Close();
            var treePoint=(Vector2)rig.SceneCamera.WorldToScreenPoint(tree.transform.TransformPoint(tree.GetComponent<SphereCollider>().center));
            yield return MouseFrame(treePoint);yield return MouseFrame(treePoint,true);yield return MouseFrame(treePoint);
            Assert.That(b.State.vitality.tapProgress,Is.EqualTo(1),"The new stage collider must remain tappable after transition.");
        }
        [UnityTest] public IEnumerator TouchEvolutionAndOrbitLockUiDoNotTapTreeAndDragRestoresAfterTransition()
        {
            Fund();var p=Center(upgrade.OpenButton);yield return TouchFrame(1,TouchPhase.Began,p);yield return TouchFrame(1,TouchPhase.Ended,p);
            p=Center(view.OrbitButton);yield return TouchFrame(2,TouchPhase.Began,p);yield return TouchFrame(2,TouchPhase.Ended,p);Assert.That(b.State.settings.orbitLocked,Is.True);
            p=Center(view.EvolveButton);yield return TouchFrame(3,TouchPhase.Began,p);yield return TouchFrame(3,TouchPhase.Ended,p);
            float yaw=rig.SceneCamera.transform.eulerAngles.y;
            var blockedWorld=new Vector2(Screen.width*0.1f,Screen.height*0.45f);
            yield return TouchFrame(30,TouchPhase.Began,blockedWorld);yield return TouchFrame(30,TouchPhase.Moved,blockedWorld+Vector2.right*50);yield return TouchFrame(30,TouchPhase.Ended,blockedWorld+Vector2.right*50);
            Assert.That(Mathf.DeltaAngle(yaw,rig.SceneCamera.transform.eulerAngles.y),Is.EqualTo(0).Within(0.001f));Assert.That(b.State.vitality.tapProgress,Is.Zero);
            yield return Finish();Assert.That(rig.OrbitLocked,Is.True);Assert.That(b.State.run.stageId,Is.EqualTo("young_tree"));
            // The reference backdrop now closes Upgrade on outside release, including a canceled drag.
            if(!upgrade.IsOpen){p=Center(upgrade.OpenButton);yield return TouchFrame(31,TouchPhase.Began,p);yield return TouchFrame(31,TouchPhase.Ended,p);}
            p=Center(view.OrbitButton);yield return TouchFrame(4,TouchPhase.Began,p);yield return TouchFrame(4,TouchPhase.Ended,p);Assert.That(rig.OrbitLocked,Is.False);
            upgrade.Close();var world=new Vector2(Screen.width*0.1f,Screen.height*0.45f);Vector3 before=rig.SceneCamera.transform.position;
            yield return TouchFrame(5,TouchPhase.Began,world);yield return TouchFrame(5,TouchPhase.Moved,world+Vector2.right*50);yield return TouchFrame(5,TouchPhase.Ended,world+Vector2.right*50);
            Assert.That(rig.SceneCamera.transform.position,Is.Not.EqualTo(before));Assert.That(b.State.vitality.tapProgress,Is.Zero);
            upgrade.Toggle();before=rig.SceneCamera.transform.position;p=Center(view.OrbitButton);
            yield return TouchFrame(6,TouchPhase.Began,p);yield return TouchFrame(6,TouchPhase.Moved,p+Vector2.right*60);yield return TouchFrame(6,TouchPhase.Ended,p+Vector2.right*60);
            Assert.That(rig.SceneCamera.transform.position,Is.EqualTo(before));Assert.That(b.State.vitality.tapProgress,Is.Zero);
        }
        [UnityTest] public IEnumerator MouseOrbitSettingAndTransitionLocksRecoverAndUiStartNeverOrbits()
        {
            Fund();yield return Click(upgrade.OpenButton);yield return Click(view.OrbitButton);Assert.That(rig.OrbitLocked,Is.True);
            var world=new Vector2(Screen.width*0.1f,Screen.height*0.45f);Vector3 before=rig.SceneCamera.transform.position;
            yield return MouseFrame(world,false,true);yield return MouseFrame(world+Vector2.right*60,false,true);yield return MouseFrame(world+Vector2.right*60);Assert.That(rig.SceneCamera.transform.position,Is.EqualTo(before));
            yield return Click(view.EvolveButton);yield return Finish();Assert.That(rig.OrbitLocked,Is.True);yield return Click(view.OrbitButton);Assert.That(rig.OrbitLocked,Is.False);
            upgrade.Close();
            before=rig.SceneCamera.transform.position;yield return MouseFrame(world,false,true);yield return MouseFrame(world+Vector2.right*60,false,true);yield return MouseFrame(world+Vector2.right*60);Assert.That(rig.SceneCamera.transform.position,Is.Not.EqualTo(before));
            upgrade.Toggle();
            before=rig.SceneCamera.transform.position;var ui=Center(view.OrbitButton);yield return MouseFrame(ui,false,true);yield return MouseFrame(world,false,true);yield return MouseFrame(world);Assert.That(rig.SceneCamera.transform.position,Is.EqualTo(before));
        }
        [UnityTest] public IEnumerator DisableAndViewFailureReleaseLocksWithoutDuplicatePayment()
        {
            Fund();b.Evolution.TryEvolve();b.Transition.enabled=false;yield return null;
            Assert.That(b.Evolution.IsTransitioning,Is.False);Assert.That(b.Input.GameplayAllowed,Is.True);Assert.That(rig.TransitionLocked,Is.False);Assert.That(b.State.run.water,Is.EqualTo(950));
            Assert.That(rig.Distance,Is.EqualTo(b.Config.FindVisual("young_tree").cameraDistance));
            b.Transition.enabled=true;var visual=b.Config.FindVisual("large_tree");var prefab=visual.prefab;
            try {visual.prefab=null;LogAssert.Expect(LogType.Error,new Regex("Evolution view failed"));b.Evolution.TryEvolve();yield return null;
                Assert.That(b.Evolution.IsTransitioning,Is.False);Assert.That(b.Input.GameplayAllowed,Is.True);Assert.That(b.State.run.water,Is.EqualTo(750));}
            finally {visual.prefab=prefab;}
        }
        [UnityTest] public IEnumerator InsufficientDeadAndTransitionBlockUiAndWorldCommands()
        {
            Assert.That(view.EvolveButton.interactable,Is.False);Assert.That(b.Evolution.TryEvolve().Reason,Is.EqualTo("INSUFFICIENT_RESOURCE"));
            Fund();b.State.run.life="DEAD";view.Refresh();Assert.That(view.EvolveButton.interactable,Is.False);Assert.That(b.Evolution.TryEvolve().Reason,Is.EqualTo("DEAD"));
            b.State.run.life="ALIVE";b.Evolution.TryEvolve();Assert.That(b.Input.GameplayAllowed,Is.False);Assert.That(view.EvolveButton.interactable,Is.False);Assert.That(view.OrbitButton.interactable,Is.False);
            Assert.That(b.Vitality.TryTap(b.Input.GameplayAllowed).Success,Is.False);yield return Finish();Assert.That(view.OrbitButton.interactable,Is.True);
        }
    }
}
