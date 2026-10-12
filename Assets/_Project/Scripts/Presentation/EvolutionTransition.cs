using System;
using System.Collections;
using CayTheGioi.Configuration;
using CayTheGioi.Domain;
using UnityEngine;

namespace CayTheGioi.Presentation
{
    public sealed class EvolutionTransition : MonoBehaviour
    {
        private EvolutionSystem evolution;
        private PrototypeConfig config;
        private GameState state;
        private TreeView tree;
        private OrbitCameraRig rig;
        private TreeInputAdapter input;
        private FoundationHudView hud;
        private bool active, previousAllowed, previousTransitionLock;
        public void Bind(EvolutionSystem service, PrototypeConfig profile, GameState game, TreeView target, OrbitCameraRig camera, TreeInputAdapter adapter, FoundationHudView view)
        {
            if (evolution != null) evolution.StageChanged -= Begin;
            evolution = service; config = profile; state = game; tree = target; rig = camera; input = adapter; hud = view;
            evolution.StageChanged += Begin;
        }
        private void Begin(string stageId)
        {
            previousAllowed = input.GameplayAllowed; previousTransitionLock = rig.TransitionLocked; active = true;
            input.GameplayAllowed = false; input.Cancel(); rig.TransitionLocked = true;
            if (!isActiveAndEnabled) { Finish(); return; }
            StartCoroutine(Blend(stageId));
        }
        private IEnumerator Blend(string stageId)
        {
            float fromDistance = rig.Distance, fromPitch = rig.Pitch, fromHeight = rig.TargetHeight;
            try
            {
                var visual = config.FindVisual(stageId);
                // Swap only after domain commit; failure cannot leave the command/input lock stuck.
                try { tree.ShowStage(visual); hud.Bind(state, config.Rules.FindStage(stageId).displayName, config.Rules.tapsPerReward); }
                catch (Exception error) { Debug.LogError("[CayTheGioi] Evolution view failed: " + error.Message); yield break; }
                float elapsed = 0;
                while (elapsed < config.EvolutionTransitionSeconds)
                {
                    elapsed += Time.unscaledDeltaTime;
                    float t = Mathf.SmoothStep(0, 1, Mathf.Clamp01(elapsed / config.EvolutionTransitionSeconds));
                    rig.SetStagePose(Mathf.Lerp(fromDistance, visual.cameraDistance, t), Mathf.Lerp(fromPitch, visual.cameraPitch, t), Mathf.Lerp(fromHeight, visual.cameraTargetHeight, t));
                    yield return null;
                }
                rig.SetStagePose(visual.cameraDistance, visual.cameraPitch, visual.cameraTargetHeight);
            }
            finally { Finish(); }
        }
        private void Finish()
        {
            if (!active) return;
            active = false;
            try
            {
                var visual = config.FindVisual(state.run.stageId);
                if (visual != null) rig.SetStagePose(visual.cameraDistance, visual.cameraPitch, visual.cameraTargetHeight);
            }
            finally
            {
                input.Cancel(); input.GameplayAllowed = previousAllowed; rig.TransitionLocked = previousTransitionLock;
                evolution.CompleteTransition();
            }
        }
        private void OnDisable() { StopAllCoroutines(); Finish(); }
        private void OnDestroy() { if (evolution != null) evolution.StageChanged -= Begin; }
    }
}
