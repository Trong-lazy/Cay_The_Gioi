using System;
using CayTheGioi.Domain;
using CayTheGioi.Infrastructure;
using CayTheGioi.Presentation;
using UnityEngine;

namespace CayTheGioi.Bootstrap
{
    [DefaultExecutionOrder(1000)]
    [DisallowMultipleComponent]
    public sealed class SaveController : MonoBehaviour
    {
        private GameState state;
        private SaveService service;
        private OfflineCoordinator offline;
        private ResourceSystem resources;
        private TreeInputAdapter input;
        private OnlineTickDriver tick;
        private string saveId;
        private long revision, backgroundCheckpoint;
        private float autosaveInterval, autosaveTimer;
        private bool persistenceBusy;
        public bool PersistenceBusy => persistenceBusy;
        private bool initialized, paused, focused = true, hasDirtySave, applyingOffline;
        public bool Suspended => paused || !focused;
        public bool AwaitingOffline { get; private set; }
        public string LastError { get; private set; }
        public string SaveId => saveId;
        public long Revision => revision;
        public bool HasDirtySave => hasDirtySave;
        public event Action StatusChanged;
        public event Action<string> SaveFailed;
        public event Action<OfflineReward> OfflineCommitted;

        public void Bind(GameState gameState, SaveService saveService, string existingSaveId, long existingRevision,
            float intervalSeconds = 30f, OfflineCoordinator coordinator = null, ResourceSystem resourceSystem = null,
            TreeInputAdapter adapter = null, OnlineTickDriver driver = null)
        {
            state = gameState ?? throw new ArgumentNullException(nameof(gameState));
            service = saveService ?? throw new ArgumentNullException(nameof(saveService));
            saveId = string.IsNullOrWhiteSpace(existingSaveId) ? Guid.NewGuid().ToString("N") : existingSaveId;
            revision = existingRevision; offline = coordinator; resources = resourceSystem; input = adapter; tick = driver;
            autosaveInterval = Mathf.Max(1, intervalSeconds); autosaveTimer = autosaveInterval; initialized = true;
        }
        private void Update() { Pulse(Time.unscaledDeltaTime); }
        public void Pulse(float seconds)
        {
            if (!initialized || !enabled || persistenceBusy || Suspended || AwaitingOffline || seconds < 0 || float.IsNaN(seconds) || float.IsInfinity(seconds)) return;
            autosaveTimer -= seconds;
            if (autosaveTimer <= 0) { autosaveTimer = autosaveInterval; DoSave("AUTOSAVE"); }
        }
        private void OnApplicationPause(bool value) { SetLifecycle(value, focused); }
        private void OnApplicationFocus(bool value) { SetLifecycle(paused, value); }
        private void OnApplicationQuit()
        {
            if (!initialized || !enabled) return;
            // A pending candidate must never be replaced by an unrewarded quit snapshot.
            if (AwaitingOffline) { if (Suspended && hasDirtySave) DoSave("QUIT_BACKGROUND_RETRY", backgroundCheckpoint); return; }
            if (!Suspended || hasDirtySave) DoSave("QUIT");
        }
        private void SetLifecycle(bool pause, bool focus)
        {
            bool wasSuspended = Suspended; paused = pause; focused = focus;
            if (!initialized || !enabled) return;
            if (!wasSuspended && Suspended)
            {
                // Keep the original source/checkpoint when a failed resume goes into background again.
                if (AwaitingOffline) { SetBlocked(true); return; }
                backgroundCheckpoint = Math.Max(service.CheckpointUtcSeconds, Math.Max(0, service.UtcNowSeconds()));
                AwaitingOffline = offline != null;
                SetBlocked(true); DoSave("BACKGROUND", backgroundCheckpoint);
            }
            else if (wasSuspended && !Suspended)
            {
                if (AwaitingOffline) RetryOffline(); else SetBlocked(false);
            }
        }
        private void SetBlocked(bool blocked)
        {
            if (resources != null) resources.SimulationBlocked = blocked;
            if (input != null) { input.CatchUpBlocked = blocked; input.Cancel(); }
        }
        public SaveResult RetryOffline()
        {
            if (!initialized || offline == null) return SaveResult.Fail("NOT_INITIALIZED");
            if (Suspended) return SaveResult.Fail("SUSPENDED");
            if (!AwaitingOffline) return SaveResult.Ok();
            if (applyingOffline) return SaveResult.Fail("BUSY");
            applyingOffline = true;
            try
            {
                var result = offline.TryApply(state, saveId, ref revision, backgroundCheckpoint, out var reward);
                SetResult(result, "OFFLINE_RESUME");
                if (result.Success)
                {
                    AwaitingOffline = false; autosaveTimer = autosaveInterval;
                    if (tick != null) tick.ResumeAfterCatchUp();
                    SetBlocked(false);
                    if (resources != null) resources.NotifyExternalCommit();
                    OfflineCommitted?.Invoke(reward);
                }
                StatusChanged?.Invoke(); return result;
            }
            finally { applyingOffline = false; }
        }
        public SaveResult ForceSave(string reason = "EXPLICIT")
        {
            if (!initialized) return SaveResult.Fail("NOT_INITIALIZED");
            if (persistenceBusy) return SaveResult.Fail("BUSY");
            if (AwaitingOffline) return Suspended ? SaveResult.Fail("SUSPENDED") : RetryOffline();
            return DoSave(reason);
        }
        public CommandResult CommitCandidate(GameState candidate,string expectedRunId)
        {
            if(!initialized || !enabled)return new CommandResult(false,"NOT_READY");
            if(persistenceBusy || Suspended || AwaitingOffline)return new CommandResult(false,"BUSY");
            if(state.run.runId!=expectedRunId)return new CommandResult(false,"STALE_RUN");
            persistenceBusy=true;SetBlocked(true);
            try
            {
                var result=service.Save(candidate,saveId,ref revision);
                // Rebirth failure is handled by its confirmation view, not an autosave retry of old state.
                Debug.Log("[SaveController] Rebirth "+(result.Success?"COMMITTED":"FAILED")+" rev="+revision);
                if(result.Success){hasDirtySave=false;LastError=null;autosaveTimer=autosaveInterval;}
                return new CommandResult(result.Success,result.Success?"":"SAVE_FAILED: "+result.Reason);
            }
            finally{persistenceBusy=false;SetBlocked(Suspended||AwaitingOffline);}
        }
        private SaveResult DoSave(string reason, long? checkpoint = null)
        {
            if(persistenceBusy)return SaveResult.Fail("BUSY");
            var result = service.Save(state, saveId, ref revision, checkpoint);
            SetResult(result, reason); StatusChanged?.Invoke(); return result;
        }
        private void SetResult(SaveResult result, string reason)
        {
            hasDirtySave = !result.Success; LastError = result.Success ? null : result.Reason;
            Debug.Log("[SaveController] Save " + (result.Success ? "OK" : "FAILED") + " (" + reason + ") rev=" + revision);
            if (!result.Success) SaveFailed?.Invoke(result.Reason);
        }
    }
}
