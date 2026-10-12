using System;
using System.Collections;
using System.IO;
using CayTheGioi.Bootstrap;
using CayTheGioi.Infrastructure;
using CayTheGioi.Presentation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using TouchPhase = UnityEngine.InputSystem.TouchPhase;

namespace CayTheGioi.Tests
{
    public sealed class OfflineSceneTests
    {
        private long now;
        private GameBootstrapper B => UnityEngine.Object.FindAnyObjectByType<GameBootstrapper>();
        private OfflineRewardView Popup => UnityEngine.Object.FindAnyObjectByType<OfflineRewardView>();
        [UnitySetUp] public IEnumerator Setup()
        {
            SaveTestScope.Begin(); now = 2000000000;
            SaveService.ValidationClock = () => now;
            SceneManager.sceneLoaded += Freeze;
            yield return Reload();
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            SceneManager.sceneLoaded -= Freeze;
            SaveTestScope.End(); yield return null;
        }
        private void Freeze(Scene scene, LoadSceneMode mode)
        { var d=UnityEngine.Object.FindAnyObjectByType<OnlineTickDriver>(); if(d!=null) d.ManualTick=true; }
        private IEnumerator Reload()
        { yield return SceneManager.LoadSceneAsync("MainScene"); yield return null; }
        private void Seed()
        {
            B.State.run.levels.root=1; B.State.run.levels.leaves=0;
            B.State.run.water=0; B.State.run.growth=0;
            B.State.vitality.bankSeconds=600; B.State.vitality.tapProgress=37;
            B.State.run.weather.remainingOnlineSeconds=73;
            B.State.run.status.statusId="burning";B.State.run.status.remainingOnlineSeconds=8;B.State.run.status.impactRegionId="leaves";
            Assert.That(B.SaveCtrl.ForceSave("T05_SEED").Success,Is.True);
        }
        private void AssertFrozen(double expected)
        {
            Assert.That(B.State.run.water,Is.EqualTo(expected).Within(1e-6));
            Assert.That(B.State.vitality.bankSeconds,Is.EqualTo(600));
            Assert.That(B.State.vitality.tapProgress,Is.EqualTo(37));
            Assert.That(B.State.run.weather.remainingOnlineSeconds,Is.EqualTo(73));
            Assert.That(B.State.run.status.remainingOnlineSeconds,Is.EqualTo(8));
            Assert.That(B.State.run.stageId,Is.EqualTo("seedling"));
            Assert.That(B.State.run.hp,Is.EqualTo(100));
            Assert.That(B.State.permanent.rebirthCount,Is.Zero);
            Assert.That(B.Evolution.IsTransitioning,Is.False);
        }
        [UnityTest] public IEnumerator StartupClockCapNeutralFreezePopupAndImmediateReopen()
        {
            long[] times={3600,86400,108000};double[] water={1800,43200,43200};
            for(int i=0;i<times.Length;i++)
            {
                Seed();now+=times[i];yield return Reload();AssertFrozen(water[i]);
                Assert.That(Popup.IsVisible,Is.True);Assert.That(Popup.Content,Does.Contain(water[i].ToString()));
                Assert.That(B.LastOfflineReward.ConsideredSeconds,Is.EqualTo(Math.Min(times[i],86400)));
                Assert.That(B.SaveSvc.Load().result.Data.offlineCheckpointUtcSeconds,Is.EqualTo(now));
                yield return Reload();AssertFrozen(water[i]);Assert.That(Popup.IsVisible,Is.False);
                Assert.That(B.LastOfflineReward.WaterDelta,Is.Zero);
            }
            Assert.That(B.Resources.Tick(1).Success,Is.True);
            Assert.That(B.State.run.water,Is.EqualTo(43201.05).Within(1e-6));
            Assert.That(B.State.vitality.bankSeconds,Is.EqualTo(599));
        }
        [UnityTest] public IEnumerator PauseAndFocusAggregateCommitBeforeNotificationAndResumeOnline()
        {
            Seed();long revision=B.SaveCtrl.Revision;int notifications=0;
            B.SaveCtrl.OfflineCommitted += r => {Assert.That(B.SaveSvc.Load().result.Data.run.water,Is.EqualTo(1800));notifications++;};
            B.SaveCtrl.SendMessage("OnApplicationPause",true);B.SaveCtrl.SendMessage("OnApplicationFocus",false);
            now+=3600;B.SaveCtrl.SendMessage("OnApplicationPause",false);
            Assert.That(B.SaveCtrl.Suspended,Is.True);AssertFrozen(0);
            Assert.That(B.Resources.Tick(30).Reason,Is.EqualTo("SUSPENDED"));
            B.SaveCtrl.SendMessage("OnApplicationFocus",true);AssertFrozen(1800);
            Assert.That(B.SaveCtrl.Revision,Is.EqualTo(revision+2));Assert.That(notifications,Is.EqualTo(1));
            B.SaveCtrl.SendMessage("OnApplicationFocus",true);B.SaveCtrl.SendMessage("OnApplicationPause",false);
            Assert.That(B.SaveCtrl.Revision,Is.EqualTo(revision+2));AssertFrozen(1800);
            Assert.That(B.Resources.Tick(1).Success,Is.True);Assert.That(B.State.run.water,Is.EqualTo(1801.05).Within(1e-6));
            yield return null;
        }
        [UnityTest] public IEnumerator FailedResumeBlocksTransactionsAutosaveQuitThenRetryFromOriginalBaseline()
        {
            Seed();B.SaveCtrl.SendMessage("OnApplicationPause",true);now+=3600;
            string saved=File.ReadAllText(B.SaveSvc.MainPath);long revision=B.SaveCtrl.Revision;
            B.SaveSvc.FaultInjection = phase => {if(phase==SaveWritePhase.BeforeReplace)throw new IOException("T05 simulated I/O failure");};
            B.SaveCtrl.SendMessage("OnApplicationPause",false);
            Assert.That(B.SaveCtrl.AwaitingOffline,Is.True);Assert.That(B.Input.CatchUpBlocked,Is.True);AssertFrozen(0);
            Assert.That(B.Resources.Tick(1).Reason,Is.EqualTo("SUSPENDED"));
            Assert.That(B.Upgrades.TryUpgrade("root").Reason,Is.EqualTo("BUSY"));
            Assert.That(B.Evolution.GetQuote().Reason,Is.EqualTo("BUSY"));
            Assert.That(B.Vitality.TryTap().Reason,Is.EqualTo("INPUT_BLOCKED"));
            B.SaveCtrl.Pulse(120);B.SaveCtrl.SendMessage("OnApplicationQuit");Assert.That(B.SaveCtrl.ForceSave().Success,Is.False);
            Assert.That(File.ReadAllText(B.SaveSvc.MainPath),Is.EqualTo(saved));Assert.That(B.SaveCtrl.Revision,Is.EqualTo(revision));
            Assert.That(UnityEngine.Object.FindAnyObjectByType<SaveStatusView>().LoadPanelVisible,Is.True);
            now+=60;B.SaveSvc.FaultInjection=null;
            Assert.That(B.SaveCtrl.RetryOffline().Success,Is.True);AssertFrozen(1830);
            Assert.That(B.SaveCtrl.AwaitingOffline,Is.False);Assert.That(B.Input.CatchUpBlocked,Is.False);
            Assert.That(B.SaveCtrl.RetryOffline().Success,Is.True);AssertFrozen(1830);
            yield return Reload();AssertFrozen(1830);Assert.That(Popup.IsVisible,Is.False);
        }
        [UnityTest] public IEnumerator StartupCommitIoFailureKeepsGameplayUninitializedUntilRetry()
        {
            Seed();string path=B.SaveSvc.MainPath;string saved=File.ReadAllText(path);now+=3600;
            // Hold only the isolated temp file: load can read main, but reward cannot commit.
            using(var handle=new FileStream(path+".tmp",FileMode.OpenOrCreate,FileAccess.ReadWrite,FileShare.None))
            {
                yield return Reload();Assert.That(B.IsInitialized,Is.False);Assert.That(B.State,Is.Null);
                Assert.That(B.Resources,Is.Null);Assert.That(B.LoadError,Does.StartWith("OFFLINE_SAVE_FAILED"));
                Assert.That(File.ReadAllText(path),Is.EqualTo(saved));
                var view=UnityEngine.Object.FindAnyObjectByType<SaveStatusView>();
                Assert.That(view.LoadPanelVisible,Is.True);Assert.That(view.ResetButton.gameObject.activeSelf,Is.False);
                Assert.That(B.RetryLoad(),Is.False);Assert.That(File.ReadAllText(path),Is.EqualTo(saved));
            }
            Assert.That(B.RetryLoad(),Is.True);B.TickDriver.ManualTick=true;AssertFrozen(1800);
            Assert.That(Popup.IsVisible,Is.True);yield return Reload();AssertFrozen(1800);
            Assert.That(Popup.IsVisible,Is.False);
        }
        [UnityTest] public IEnumerator BackupRecoveryDoesNotReplayOldOfflineReward()
        {
            Seed();Assert.That(B.SaveCtrl.ForceSave().Success,Is.True);
            File.WriteAllText(B.SaveSvc.MainPath,"invalid isolated test save");now+=3600;
            yield return Reload();Assert.That(B.IsInitialized,Is.False);
            Assert.That(B.RetryLoad(true),Is.True);B.TickDriver.ManualTick=true;
            AssertFrozen(0);Assert.That(Popup.IsVisible,Is.False);
            Assert.That(B.SaveSvc.Load().result.Data.offlineCheckpointUtcSeconds,Is.EqualTo(now));
            yield return Reload();AssertFrozen(0);Assert.That(B.LastOfflineReward.WaterDelta,Is.Zero);
        }
        [UnityTest] public IEnumerator PopupMouseAndSimulatedTouchCloseDoNotCountTreeTap()
        {
            var priorMouse=Mouse.current;var priorTouch=Touchscreen.current;
            Mouse mouse=null;Touchscreen touch=null;
            try
            {
                Seed();now+=3600;yield return Reload();B.Input.SendMessage("OnApplicationFocus",true);
                mouse=InputSystem.AddDevice<Mouse>();touch=InputSystem.AddDevice<Touchscreen>();yield return null;
                Canvas.ForceUpdateCanvases();var r=(RectTransform)Popup.CloseButton.transform;
                Vector2 p=RectTransformUtility.WorldToScreenPoint(null,r.TransformPoint(r.rect.center));
                InputSystem.QueueStateEvent(mouse,new MouseState{position=p});yield return null;yield return null;
                InputSystem.QueueStateEvent(mouse,new MouseState{position=p}.WithButton(MouseButton.Left));yield return null;yield return null;
                InputSystem.QueueStateEvent(mouse,new MouseState{position=p});yield return null;yield return null;
                Assert.That(Popup.IsVisible,Is.False);Assert.That(B.State.vitality.tapProgress,Is.EqualTo(37));
                B.SaveCtrl.SendMessage("OnApplicationPause",true);now+=3600;
                B.SaveSvc.FaultInjection=phase=>{if(phase==SaveWritePhase.BeforeReplace)throw new IOException("T05 touch retry");};
                B.SaveCtrl.SendMessage("OnApplicationPause",false);Assert.That(B.SaveCtrl.AwaitingOffline,Is.True);
                B.SaveSvc.FaultInjection=null;Canvas.ForceUpdateCanvases();
                r=(RectTransform)UnityEngine.Object.FindAnyObjectByType<SaveStatusView>().RetryButton.transform;
                p=RectTransformUtility.WorldToScreenPoint(null,r.TransformPoint(r.rect.center));
                InputSystem.QueueStateEvent(touch,new TouchState{touchId=500,phase=TouchPhase.Began,position=p});yield return null;yield return null;
                InputSystem.QueueStateEvent(touch,new TouchState{touchId=500,phase=TouchPhase.Ended,position=p});yield return null;yield return null;
                Assert.That(B.SaveCtrl.AwaitingOffline,Is.False);Assert.That(B.State.run.water,Is.EqualTo(3600));
                Assert.That(Popup.IsVisible,Is.True);Canvas.ForceUpdateCanvases();r=(RectTransform)Popup.CloseButton.transform;
                p=RectTransformUtility.WorldToScreenPoint(null,r.TransformPoint(r.rect.center));
                InputSystem.QueueStateEvent(touch,new TouchState{touchId=501,phase=TouchPhase.Began,position=p});yield return null;yield return null;
                InputSystem.QueueStateEvent(touch,new TouchState{touchId=501,phase=TouchPhase.Ended,position=p});yield return null;yield return null;
                Assert.That(Popup.IsVisible,Is.False);Assert.That(B.State.vitality.tapProgress,Is.EqualTo(37));
            }
            finally
            {
                if(mouse!=null&&mouse.added)InputSystem.RemoveDevice(mouse);if(touch!=null&&touch.added)InputSystem.RemoveDevice(touch);
                if(priorMouse!=null&&priorMouse.added)priorMouse.MakeCurrent();if(priorTouch!=null&&priorTouch.added)priorTouch.MakeCurrent();
            }
        }
    }
}
