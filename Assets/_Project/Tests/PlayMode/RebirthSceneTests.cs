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
using TouchPhase=UnityEngine.InputSystem.TouchPhase;
namespace CayTheGioi.Tests
{
    public sealed class RebirthSceneTests
    {
        private GameBootstrapper b;private RebirthView view;private Mouse mouse,priorMouse;private Touchscreen touch,priorTouch;private long now;
        [UnitySetUp]public IEnumerator Setup()
        {
            SaveTestScope.Begin();now=2000000000;SaveService.ValidationClock=()=>now;SceneManager.sceneLoaded+=Freeze;
            yield return Reload();b=Object.FindAnyObjectByType<GameBootstrapper>();
            Debug.Log("[T06 Test] viewport="+Screen.width+"x"+Screen.height);
            b.Input.SendMessage("OnApplicationFocus",true);b.SaveCtrl.SendMessage("OnApplicationFocus",true);
            b.State.run.water=1000;b.State.run.growth=1000;b.State.run.photosynthesis=1000;b.State.run.levels.root=1;b.State.run.levels.leaves=1;
            Assert.That(b.Evolution.TryEvolve().Success,Is.True);yield return new WaitForSecondsRealtime(b.Config.EvolutionTransitionSeconds+.1f);
            b.State.vitality.bankSeconds=250;b.State.vitality.tapProgress=17;b.State.permanent.coin=31;b.State.settings.orbitLocked=true;
            Object.FindAnyObjectByType<OrbitCameraRig>().OrbitLocked=true;b.SaveCtrl.ForceSave("T06_SEED");
            view=Object.FindAnyObjectByType<RebirthView>();priorMouse=Mouse.current;priorTouch=Touchscreen.current;mouse=InputSystem.AddDevice<Mouse>();touch=InputSystem.AddDevice<Touchscreen>();yield return null;
        }
        [UnityTearDown]public IEnumerator Cleanup()
        {
            SceneManager.sceneLoaded-=Freeze;
            if(mouse!=null&&mouse.added)InputSystem.RemoveDevice(mouse);if(touch!=null&&touch.added)InputSystem.RemoveDevice(touch);
            if(priorMouse!=null&&priorMouse.added)priorMouse.MakeCurrent();if(priorTouch!=null&&priorTouch.added)priorTouch.MakeCurrent();SaveTestScope.End();yield return null;
        }
        private void Freeze(Scene s,LoadSceneMode m){var t=Object.FindAnyObjectByType<OnlineTickDriver>();if(t!=null)t.ManualTick=true;}
        private IEnumerator Reload(){yield return SceneManager.LoadSceneAsync("MainScene");yield return null;b=Object.FindAnyObjectByType<GameBootstrapper>();view=Object.FindAnyObjectByType<RebirthView>();}
        private Vector2 Center(Component c){Canvas.ForceUpdateCanvases();var r=(RectTransform)c.transform;return RectTransformUtility.WorldToScreenPoint(null,r.TransformPoint(r.rect.center));}
        private IEnumerator MouseFrame(Vector2 p,bool down=false){var s=new MouseState{position=p};if(down)s=s.WithButton(MouseButton.Left);InputSystem.QueueStateEvent(mouse,s);yield return null;yield return null;}
        private IEnumerator Click(Component c){var p=Center(c);yield return MouseFrame(p);yield return MouseFrame(p,true);yield return MouseFrame(p);}
        private IEnumerator TouchClick(Component c,int id){var p=Center(c);InputSystem.QueueStateEvent(touch,new TouchState{touchId=id,phase=TouchPhase.Began,position=p});yield return null;yield return null;InputSystem.QueueStateEvent(touch,new TouchState{touchId=id,phase=TouchPhase.Ended,position=p});yield return null;yield return null;}
        private void AssertNewRun(string old)
        {
            Assert.That(b.State.run.runId,Is.Not.EqualTo(old));Assert.That(b.State.run.stageId,Is.EqualTo("seedling"));Assert.That(b.State.run.life,Is.EqualTo("ALIVE"));Assert.That(b.State.run.hp,Is.EqualTo(100));Assert.That(b.State.run.water,Is.EqualTo(10));Assert.That(b.State.run.growth,Is.EqualTo(b.Config.Rules.initialGrowth));Assert.That(b.State.run.levels.root+b.State.run.levels.leaves+b.State.run.levels.trunk,Is.Zero);
            Assert.That(PermanentEffects.Points(b.State.permanent,"young_tree"),Is.EqualTo(1));Assert.That(PermanentEffects.Points(b.State.permanent,"seedling"),Is.Zero);Assert.That(PermanentEffects.Points(b.State.permanent,"large_tree"),Is.Zero);Assert.That(b.State.vitality.bankSeconds,Is.EqualTo(250));Assert.That(b.State.vitality.tapProgress,Is.EqualTo(17));Assert.That(b.State.permanent.coin,Is.EqualTo(31));Assert.That(b.State.settings.orbitLocked,Is.True);
            Assert.That(Object.FindAnyObjectByType<TreeView>().CurrentStageId,Is.EqualTo("seedling"));Assert.That(Object.FindAnyObjectByType<OrbitCameraRig>().Distance,Is.EqualTo(b.Config.FindVisual("seedling").cameraDistance));Assert.That(b.Input.GameplayAllowed,Is.True);Assert.That(Object.FindAnyObjectByType<OrbitCameraRig>().TransitionLocked,Is.False);
        }
        [UnityTest]public IEnumerator MousePreviewCancelConfirmPersistsBeforeEventRestoresModelHudCapsAndReopen()
        {
            string id=b.State.run.runId;string saved=File.ReadAllText(b.SaveSvc.MainPath);double water=b.State.run.water;
            yield return Click(view.OpenButton);Assert.That(view.IsVisible,Is.True);Assert.That(view.Content,Does.Contain("point Cây Non"));Assert.That(view.Content,Does.Contain("+5% Trunk Growth"));Assert.That(b.Input.GameplayAllowed,Is.False);
            yield return Click(view.CancelButton);Assert.That(view.IsVisible,Is.False);Assert.That(b.State.run.runId,Is.EqualTo(id));Assert.That(b.State.run.water,Is.EqualTo(water));Assert.That(File.ReadAllText(b.SaveSvc.MainPath),Is.EqualTo(saved));
            yield return Click(view.OpenButton);int count=0;b.Rebirth.Completed+=()=>{count++;Assert.That(b.SaveSvc.Load().result.Data.run.runId,Is.EqualTo(b.State.run.runId));Assert.That(b.Rebirth.TryRebirth(b.State.run.runId).Reason,Is.EqualTo("BUSY"));Assert.That(b.Upgrades.TryUpgrade("root").Reason,Is.EqualTo("BUSY"));Assert.That(b.Evolution.TryEvolve().Reason,Is.EqualTo("BUSY"));Assert.That(b.Resources.Tick(1).Reason,Is.EqualTo("SUSPENDED"));Assert.That(b.Vitality.TryTap().Reason,Is.EqualTo("INPUT_BLOCKED"));};
            Assert.That(view.IsConfirming,Is.False);yield return Click(view.BeginButton);Assert.That(view.IsConfirming,Is.True);Assert.That(b.State.run.runId,Is.EqualTo(id));
            yield return Click(view.ConfirmButton);AssertNewRun(id);Assert.That(count,Is.EqualTo(1));view.Confirm();Assert.That(count,Is.EqualTo(1));Assert.That(b.Rebirth.TryRebirth(id).Reason,Is.EqualTo("STALE_RUN"));
            Assert.That(b.Upgrades.GetQuote("spirit_absorption").Reason,Is.EqualTo("LOCKED"));Assert.That(Object.FindAnyObjectByType<FoundationHudView>().ResourceContent,Does.Contain("Tap 17/50"));yield return Reload();AssertNewRun(id);Assert.That(view.IsVisible,Is.False);Assert.That(b.LastOfflineReward.WaterDelta,Is.Zero);
        }
        [UnityTest]public IEnumerator SimulatedTouchConfirmAwardsOnceNoTreeTapAndKeepsBank()
        {
            string id=b.State.run.runId;yield return TouchClick(view.OpenButton,601);Assert.That(view.IsVisible,Is.True);yield return TouchClick(view.BeginButton,600);Assert.That(view.IsConfirming,Is.True);yield return TouchClick(view.ConfirmButton,602);AssertNewRun(id);
            Assert.That(view.OpenButton.interactable,Is.False);Assert.That(b.Resources.Tick(1).Success,Is.True);Assert.That(b.State.vitality.bankSeconds,Is.EqualTo(249));
        }
        [UnityTest]public IEnumerator IoFailureMouseRetryAndCrashReloadDoNotDoubleAward()
        {
            string id=b.State.run.runId;string saved=File.ReadAllText(b.SaveSvc.MainPath);long revision=b.SaveCtrl.Revision;
            yield return Click(view.OpenButton);yield return Click(view.BeginButton);b.SaveSvc.FaultInjection=phase=>{if(phase==SaveWritePhase.BeforeReplace)throw new IOException("T06 I/O");};
            yield return Click(view.ConfirmButton);Assert.That(view.IsVisible,Is.True);Assert.That(view.Content,Does.Contain("chưa nhận point"));Assert.That(b.State.run.runId,Is.EqualTo(id));Assert.That(PermanentEffects.Points(b.State.permanent,"young_tree"),Is.Zero);Assert.That(File.ReadAllText(b.SaveSvc.MainPath),Is.EqualTo(saved));Assert.That(b.SaveCtrl.Revision,Is.EqualTo(revision));
            Assert.That(b.SaveCtrl.ForceSave().Success,Is.False);Assert.That(Object.FindAnyObjectByType<SaveStatusView>().StatusText,Does.Contain("Chưa lưu"));
            b.SaveSvc.FaultInjection=phase=>{if(phase==SaveWritePhase.AfterReplace)throw new IOException("Post-commit diagnostic");};yield return Click(view.ConfirmButton);AssertNewRun(id);Assert.That(Object.FindAnyObjectByType<SaveStatusView>().StatusText,Does.Not.Contain("Chưa lưu"));yield return Reload();AssertNewRun(id);Assert.That(b.Rebirth.TryRebirth(id).Reason,Is.EqualTo("STALE_RUN"));
        }
        [UnityTest]public IEnumerator DeadModalOverridesOtherPanelsCannotCancelFreezesAcrossSaveLoadThenForcedTouchRebirth()
        {
            string id=b.State.run.runId;Object.FindAnyObjectByType<UpgradePanelView>().Toggle();
            b.State.run.life="DEAD";b.State.run.hp=0;b.Resources.NotifyExternalCommit();b.SaveCtrl.ForceSave("DEAD");yield return null;
            Assert.That(view.IsVisible,Is.True);Assert.That(view.CancelButton.gameObject.activeSelf,Is.False);Assert.That(Object.FindAnyObjectByType<UpgradePanelView>().IsOpen,Is.False);view.Cancel();Assert.That(view.IsVisible,Is.True);
            Assert.That(b.Resources.Tick(3600).Reason,Is.EqualTo("DEAD"));Assert.That(b.Vitality.TryTap().Reason,Is.EqualTo("DEAD"));Assert.That(b.Upgrades.TryUpgrade("root").Reason,Is.EqualTo("DEAD"));Assert.That(b.Evolution.TryEvolve().Reason,Is.EqualTo("DEAD"));now+=3600;yield return Reload();Assert.That(view.IsVisible,Is.True);Assert.That(view.Content,Does.Contain("Forced Rebirth"));Assert.That(b.LastOfflineReward.WaterDelta,Is.Zero);Assert.That(b.State.vitality.bankSeconds,Is.EqualTo(250));
            yield return TouchClick(view.ConfirmButton,603);AssertNewRun(id);Assert.That(view.IsVisible,Is.False);b.State.run.levels.root=1;Assert.That(b.Resources.Tick(1).Success,Is.True);Assert.That(b.State.run.water,Is.EqualTo(11.05).Within(1e-6));Assert.That(b.State.vitality.bankSeconds,Is.EqualTo(249));
        }
        [UnityTest]public IEnumerator PauseAndPendingOfflineBlockRebirthUntilCatchupThenClearOldPopup()
        {
            string id=b.State.run.runId;view.Open();b.SaveCtrl.SendMessage("OnApplicationPause",true);now+=3600;Assert.That(b.Rebirth.TryRebirth(id).Reason,Is.EqualTo("BUSY"));
            b.SaveSvc.FaultInjection=phase=>{if(phase==SaveWritePhase.BeforeReplace)throw new IOException("Offline before Rebirth");};b.SaveCtrl.SendMessage("OnApplicationPause",false);Assert.That(b.Rebirth.TryRebirth(id).Reason,Is.EqualTo("BUSY"));Assert.That(PermanentEffects.Points(b.State.permanent,"young_tree"),Is.Zero);
            b.SaveSvc.FaultInjection=null;Assert.That(b.SaveCtrl.RetryOffline().Success,Is.True);view.Confirm();yield return null;AssertNewRun(id);Assert.That(Object.FindAnyObjectByType<OfflineRewardView>().IsVisible,Is.False);
        }
        [UnityTest]public IEnumerator PreviewAndResetUseRuntimeConfigSnapshot()
        {
            double water=b.Config.Rules.initialWater;double perPoint=b.Config.Rules.pointEffects[1].perPoint;
            try
            {
                b.Config.Rules.initialWater=123;b.Config.Rules.pointEffects[1].perPoint=.5;
                view.Open();Assert.That(view.Content,Does.Contain("Water 10"));Assert.That(view.Content,Does.Contain("+5% Trunk Growth"));
                view.Confirm();Assert.That(b.State.run.water,Is.EqualTo(10));
            }
            finally{b.Config.Rules.initialWater=water;b.Config.Rules.pointEffects[1].perPoint=perPoint;}
            yield return null;
        }
        [UnityTest]public IEnumerator PointEffectsStayInOfflineAndUpgradeQuotesWithoutVitalityOrCapBuff()
        {
            b.State.permanent.stagePoints.Find(p=>p.stageId=="seedling").points=2;b.State.permanent.stagePoints.Find(p=>p.stageId=="young_tree").points=4;b.State.permanent.stagePoints.Find(p=>p.stageId=="large_tree").points=2;
            b.State.run.water=100;b.State.run.growth=0;b.State.run.levels.root=1;b.State.run.levels.leaves=1;b.State.run.levels.trunk=1;b.SaveCtrl.ForceSave("POINTS");now+=3600;yield return Reload();
            Assert.That(b.State.run.water,Is.EqualTo(1630).Within(1e-6));Assert.That(b.State.run.growth,Is.EqualTo(2160).Within(1e-6));Assert.That(b.State.vitality.bankSeconds,Is.EqualTo(250));Assert.That(b.Upgrades.Protection,Is.EqualTo(.09).Within(1e-9));Assert.That(b.Upgrades.GetQuote("root").CurrentOutput,Is.EqualTo(1.155).Within(1e-9));Assert.That(b.Upgrades.GetQuote("leaves").CurrentOutput,Is.EqualTo(1.05).Within(1e-9));Assert.That(b.Upgrades.GetQuote("trunk").CurrentOutput,Is.EqualTo(1.26).Within(1e-9));Assert.That(b.Upgrades.GetQuote("root").Cap,Is.EqualTo(10));
        }
    }
}
