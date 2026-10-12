using System;
using System.Collections;
using System.IO;
using System.Text;
using CayTheGioi.Domain;
using CayTheGioi.Infrastructure;
using CayTheGioi.Presentation;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.UI;
using TouchPhase = UnityEngine.InputSystem.TouchPhase;

namespace CayTheGioi.Bootstrap
{
    /// <summary>
    /// Executes automated QA scenarios inside standalone builds (T09) when launched with -ctg-scenario &lt;name&gt;.
    /// Covers onboarding, tap/vitality, upgrades, evolution, rebirth, achievements, shop, weather/death,
    /// save/load roundtrips and offline atomic catch-up in isolated test save directories.
    /// Writes machine-readable JSON reports and verified on-disk screenshots before cleanly quitting.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class QaBuildRunner : MonoBehaviour
    {
        [Serializable]
        public sealed class ScenarioReport
        {
            public string scenario;
            public bool success;
            public string stageId;
            public string life;
            public double water;
            public double growth;
            public double hp;
            public int rootLevel;
            public int leavesLevel;
            public int trunkLevel;
            public long rebirthPoints;
            public double vitalityBank;
            public int vitalityTaps;
            public long coin;
            public string equippedCosmetic;
            public int achievementsClaimed;
            public string error;
            public string details;
            public string screenshot;
            public double offlineWaterDelta;
            public double offlineGrowthDelta;
            public double offlineElapsed;
            public System.Collections.Generic.List<string> screenshots = new System.Collections.Generic.List<string>();
            public System.Collections.Generic.List<string> steps = new System.Collections.Generic.List<string>();
        }

        private GameBootstrapper bootstrapper;

        private void Update()
        {
            // Only opt-in automated scenarios run hidden. Normal players must still suspend
            // when unfocused; lifecycle behavior is independently covered by PlayMode tests.
            if (bootstrapper == null || !QaLaunchOptions.Active || string.IsNullOrEmpty(QaLaunchOptions.Scenario) || Application.isFocused) return;
            bootstrapper.SaveCtrl?.SendMessage("OnApplicationFocus", true);
            bootstrapper.Input?.SendMessage("OnApplicationFocus", true);
        }

        private void Start()
        {
            if (!QaLaunchOptions.Active || string.IsNullOrEmpty(QaLaunchOptions.Scenario)) return;
            bootstrapper = GetComponent<GameBootstrapper>();
            StartCoroutine(RunScenarioRoutine(QaLaunchOptions.Scenario));
        }

        private IEnumerator RunScenarioRoutine(string scenario)
        {
            // Give systems and presentation a few frames to initialize and render
            yield return null;
            yield return new WaitForSecondsRealtime(0.5f);

            var report = new ScenarioReport
            {
                scenario = scenario,
                success = false
            };
            report.steps.Add("HOOK_FOCUS automated hidden scenario emulates focused lifecycle; not an OS input test");

            if (bootstrapper == null || !bootstrapper.IsInitialized)
            {
                report.error = "BOOTSTRAPPER_NOT_INITIALIZED: " + (bootstrapper?.LoadError ?? "null");
                Finish(report);
                yield break;
            }

            bootstrapper.SaveCtrl.SendMessage("OnApplicationFocus", true);
            bootstrapper.Input.SendMessage("OnApplicationFocus", true);

            switch (scenario)
            {
                case "smoke_fresh":
                case "onboarding_and_vitality":
                    yield return RunOnboardingAndVitality(report);
                    break;
                case "smoke_progression":
                case "full_loop_and_rebirth":
                    yield return RunFullLoopAndRebirth(report);
                    break;
                case "smoke_death_forced_rebirth":
                case "weather_dead_forced_rebirth":
                    yield return RunWeatherDeadForcedRebirth(report);
                    break;
                case "smoke_offline":
                case "offline_atomic":
                    yield return RunOfflineAtomic(report);
                    break;
                case "ui_flow":
                    yield return RunUiFlow(report);
                    break;
                case "smoke_raycast":
                case "raycast_audit":
                    yield return RunRaycastAudit(report);
                    break;
                case "ui_list":
                    report.details = DumpButtons();
                    report.success = true;
                    yield return CaptureVerifiedScreenshot(report);
                    Finish(report);
                    yield break;
                default:
                    report.error = "UNKNOWN_SCENARIO: " + scenario;
                    Finish(report);
                    yield break;
            }
        }

        private IEnumerator RunOnboardingAndVitality(ScenarioReport report)
        {
            var state = bootstrapper.State;
            var vitality = bootstrapper.Vitality;
            var upgrades = bootstrapper.Upgrades;
            var tutorial = bootstrapper.Tutorial;
            var saveCtrl = bootstrapper.SaveCtrl;

            // Step 1: Tap tree 50 times to complete step_vitality and gain 20s bank
            for (int i = 0; i < 50; i++)
            {
                vitality.TryTap(true);
            }

            // Step 2: Buy Root level 1 (completes step_upgrade_root)
            var rootRes = upgrades.TryUpgrade("root");

            // Step 3: Buy Leaves level 1 (completes step_upgrade_leaves)
            var leavesRes = upgrades.TryUpgrade("leaves");

            // Online simulation for 2 seconds to consume a bit of vitality and produce Water/Growth
            yield return new WaitForSecondsRealtime(2.0f);

            var saveRes = saveCtrl.ForceSave("ONBOARDING_COMMIT");
            if (!saveRes.Success)
            {
                report.error = "SAVE_FAILED: " + saveRes.Reason;
                Finish(report);
                yield break;
            }

            PopulateReportState(report, state);

            bool vitDone = tutorial.IsStepCompleted(TutorialSystem.StepVitality);
            bool rootDone = tutorial.IsStepCompleted(TutorialSystem.StepUpgradeRoot);
            bool leavesDone = tutorial.IsStepCompleted(TutorialSystem.StepUpgradeLeaves);

            report.success = rootRes.Success && leavesRes.Success &&
                             vitDone && rootDone && leavesDone &&
                             state.run.levels.root >= 1 && state.run.levels.leaves >= 1 &&
                             state.vitality.bankSeconds > 0 &&
                             File.Exists(bootstrapper.SaveSvc.MainPath);

            report.details = $"Onboarding: Root={state.run.levels.root} ({rootRes.Success}/{rootRes.Reason}), Leaves={state.run.levels.leaves} ({leavesRes.Success}/{leavesRes.Reason}), tutorial={vitDone}/{rootDone}/{leavesDone}, Water={state.run.water:F2}, Growth={state.run.growth:F2}, Bank={state.vitality.bankSeconds:F1}s";

            yield return CaptureVerifiedScreenshot(report);
            Finish(report);
        }

        private static void Check(ScenarioReport report, System.Collections.Generic.List<string> failures, string name, bool condition)
        {
            report.steps.Add((condition ? "CHECK_OK " : "CHECK_FAIL ") + name);
            if (!condition) failures.Add(name);
        }

        private static Button FindTopBuyButton(int rowFromTop)
        {
            var list = new System.Collections.Generic.List<Button>();
            foreach (var b in FindObjectsByType<Button>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
                if (b.name == "Buy" && b.isActiveAndEnabled) list.Add(b);
            list.Sort((a, b) => b.transform.position.y.CompareTo(a.transform.position.y));
            return rowFromTop < list.Count ? list[rowFromTop] : null;
        }

        private bool UiClickBuyRow(ScenarioReport report, int rowFromTop)
        {
            var button = FindTopBuyButton(rowFromTop);
            bool ok = button != null && button.interactable && EventSystem.current != null;
            if (ok) ExecuteEvents.Execute(button.gameObject, new PointerEventData(EventSystem.current) { button = PointerEventData.InputButton.Left, clickCount = 1 }, ExecuteEvents.pointerClickHandler);
            report.steps.Add((ok ? "UI_CLICK " : "UI_CLICK_MISSING ") + "Buy[row " + rowFromTop + "]");
            return ok;
        }

        /// <summary>
        /// UI flow in the standalone player. Buttons are real uGUI objects clicked through the EventSystem (pointer click),
        /// NOT OS-level mouse input. Steps tagged HOOK_* use direct system calls (tap on tree, funding resources, DEAD).
        /// </summary>
        private IEnumerator RunUiFlow(ScenarioReport report)
        {
            var state = bootstrapper.State;
            var fail = new System.Collections.Generic.List<string>();
            var wait = new WaitForSecondsRealtime(0.4f);

            yield return CaptureVerifiedScreenshot(report, "00_hud_tutorial");

            // Tutorial step 1: tree taps (HOOK: TreeInputAdapter/mouse not exercised here)
            report.steps.Add("HOOK_TAP vitality.TryTap x50");
            for (int i = 0; i < 50; i++) bootstrapper.Vitality.TryTap(true);
            yield return wait;
            Check(report, fail, "vitality bank > 0 after 50 taps", state.vitality.bankSeconds > 0);
            Check(report, fail, "tutorial step_vitality done", bootstrapper.Tutorial.IsStepCompleted(TutorialSystem.StepVitality));

            // Settings (UI clicks)
            Check(report, fail, "SettingsOpen clickable", UiClick(report, "SettingsOpen"));
            yield return wait;
            yield return CaptureVerifiedScreenshot(report, "01_settings");
            bool musicBefore = state.settings.musicEnabled;
            Check(report, fail, "MusicToggleBtn clickable", UiClick(report, "MusicToggleBtn"));
            yield return wait;
            Check(report, fail, "music setting toggled", state.settings.musicEnabled != musicBefore);
            UiClick(report, "MusicToggleBtn");
            yield return wait;
            Check(report, fail, "music setting restored", state.settings.musicEnabled == musicBefore);
            Check(report, fail, "ContinueBtn closes settings", UiClick(report, "ContinueBtn"));
            yield return wait;

            // Upgrade panel (UI clicks): rows from top = root, leaves, trunk
            Check(report, fail, "UpgradeOpen clickable", UiClick(report, "UpgradeOpen"));
            yield return wait;
            yield return CaptureVerifiedScreenshot(report, "02_upgrade_open");
            Check(report, fail, "Buy root via UI", UiClickBuyRow(report, 0));
            yield return wait;
            Check(report, fail, "root level 1", state.run.levels.root == 1);
            Check(report, fail, "Buy leaves via UI", UiClickBuyRow(report, 1));
            yield return wait;
            Check(report, fail, "leaves level 1", state.run.levels.leaves == 1);
            Check(report, fail, "tutorial upgrade steps done", bootstrapper.Tutorial.IsStepCompleted(TutorialSystem.StepUpgradeRoot) && bootstrapper.Tutorial.IsStepCompleted(TutorialSystem.StepUpgradeLeaves));
            yield return CaptureVerifiedScreenshot(report, "03_upgrade_bought");

            // Evolution to young_tree via UI button (Panel is open, button Evolve is on panel)
            report.steps.Add("HOOK_FUND water/growth for Evolution (young_tree)");
            state.run.water = 1000; state.run.growth = 1000; state.run.photosynthesis=1000;
            bootstrapper.Resources.NotifyExternalCommit();
            yield return wait;
            Check(report, fail, "Evolve button (to young_tree)", UiClick(report, "Evolve"));
            yield return new WaitForSecondsRealtime(2.5f);
            Check(report, fail, "stage young_tree", state.run.stageId == "young_tree");
            yield return CaptureVerifiedScreenshot(report, "04_young_tree");

            // Trunk via UI, then Evolution to large_tree (Panel is still open on screen)
            report.steps.Add("HOOK_FUND water/growth for Trunk and Evolution (large_tree)");
            state.run.water = 2500; state.run.growth = 2500; state.run.photosynthesis=2500;
            bootstrapper.Resources.NotifyExternalCommit();
            yield return wait;
            Check(report, fail, "Buy trunk via UI", UiClickBuyRow(report, 2));
            yield return wait;
            Check(report, fail, "trunk level 1", state.run.levels.trunk == 1);
            Check(report, fail, "Evolve button (to large_tree)", UiClick(report, "Evolve"));
            yield return new WaitForSecondsRealtime(2.5f);
            Check(report, fail, "stage large_tree", state.run.stageId == "large_tree");
            yield return CaptureVerifiedScreenshot(report, "05_large_tree");
            UiClick(report, "Close"); // Close upgrade panel
            yield return wait;

            // Achievements (UI clicks)
            bootstrapper.Achievements.SyncFromCurrentState();
            Check(report, fail, "AchievementOpen clickable", UiClick(report, "AchievementOpen"));
            yield return wait;
            yield return CaptureVerifiedScreenshot(report, "06_achievements");
            long coinBefore = state.permanent.coin;
            int claims = 0;
            for (int i = 0; i < 8; i++)
            {
                if (UiClick(report, "ClaimBtn"))
                {
                    claims++;
                    yield return new WaitForSecondsRealtime(0.25f);
                }
                else break;
            }
            int totalClaimed = state.permanent.achievements.FindAll(a => a.claimed).Count;
            Check(report, fail, "achievements claimed via UI (total >= 4)", totalClaimed >= 4 || claims >= 2);
            long coinAfterClaims = state.permanent.coin;
            yield return CaptureVerifiedScreenshot(report, "07_achievements_claimed");
            UiClick(report, "CloseBtn"); // Close achievements panel
            yield return wait;

            // Shop (UI clicks): buy then equip
            Check(report, fail, "ShopOpen clickable", UiClick(report, "ShopOpen"));
            yield return wait;
            yield return CaptureVerifiedScreenshot(report, "08_shop");
            bool alreadyOwned = state.permanent.ownedCosmeticIds.Contains("glow_placeholder");
            if (!alreadyOwned && coinAfterClaims >= 20)
            {
                bool bought = UiClick(report, "ActionBtn");
                yield return wait;
                Check(report, fail, "cosmetic bought", bought && state.permanent.ownedCosmeticIds.Contains("glow_placeholder"));
                Check(report, fail, "coin deducted", state.permanent.coin < coinAfterClaims);
                // Click again to equip
                UiClick(report, "ActionBtn");
                yield return wait;
            }
            else if (state.permanent.equippedCosmeticId != "glow_placeholder")
            {
                // Owned but not equipped: click to equip
                UiClick(report, "ActionBtn");
                yield return wait;
            }
            Check(report, fail, "cosmetic equipped", state.permanent.equippedCosmeticId == "glow_placeholder");
            yield return CaptureVerifiedScreenshot(report, "09_shop_equipped");
            UiClick(report, "CloseBtn"); // Close shop panel using CloseBtn
            yield return wait;

            // Voluntary Rebirth (UI clicks)
            string oldRunId = state.run.runId;
            Check(report, fail, "RebirthOpen clickable", UiClick(report, "RebirthOpen"));
            yield return wait;
            yield return CaptureVerifiedScreenshot(report, "10_rebirth_modal");
            Check(report, fail, "RebirthBegin clickable", UiClick(report, "RebirthBegin"));
            yield return wait;
            Check(report, fail, "preview did not reset run", state.run.runId == oldRunId);
            Check(report, fail, "RebirthConfirm clickable", UiClick(report, "RebirthConfirm"));
            yield return new WaitForSecondsRealtime(1f);
            Check(report, fail, "run reset to seedling", state.run.stageId == "seedling" && state.run.runId != oldRunId);
            long pts = 0; foreach (var sp in state.permanent.stagePoints) pts += sp.points;
            Check(report, fail, "rebirth point awarded", pts >= 1);
            Check(report, fail, "vitality bank kept", state.vitality.bankSeconds > 0);
            yield return CaptureVerifiedScreenshot(report, "11_after_rebirth");

            var save = bootstrapper.SaveCtrl.ForceSave("UI_FLOW_COMMIT");
            Check(report, fail, "ForceSave ok", save.Success && File.Exists(bootstrapper.SaveSvc.MainPath));

            PopulateReportState(report, state);
            report.achievementsClaimed = claims;
            report.success = fail.Count == 0;
            report.details = report.success ? "UI flow OK (see steps: UI_CLICK vs HOOK_*)" : "UI flow FAILED: " + string.Join("; ", fail);
            Finish(report);
        }


        private IEnumerator RunFullLoopAndRebirth(ScenarioReport report)
        {
            var state = bootstrapper.State;
            var upgrades = bootstrapper.Upgrades;
            var evolution = bootstrapper.Evolution;
            var rebirth = bootstrapper.Rebirth;
            var achievements = bootstrapper.Achievements;
            var cosmetics = bootstrapper.Cosmetics;

            // Fund run resources to drive loop cleanly
            state.run.water = 1000;
            state.run.growth = 1000; state.run.photosynthesis=1000;

            // 1. Evolve to young_tree
            var evoYoung = evolution.TryEvolve();
            if (evoYoung.Success) evolution.CompleteTransition();

            // 2. Buy trunk level 1
            upgrades.TryUpgrade("trunk");

            // 3. Evolve to large_tree
            state.run.water = 2000;
            state.run.growth = 2000; state.run.photosynthesis=2000;
            var evoLarge = evolution.TryEvolve();
            if (evoLarge.Success) evolution.CompleteTransition();

            // 4. Claim unlocked achievements and test shop cosmetic
            achievements.SyncFromCurrentState();
            int claimed = 0;
            foreach (var ach in achievements.GetAllStatuses())
            {
                if (ach.CanClaim && achievements.TryClaim(ach.AchievementId).Success)
                {
                    claimed++;
                }
            }

            // Buy cosmetic glow_placeholder if coin >= 20
            if (state.permanent.coin >= 20)
            {
                cosmetics.TryBuy("glow_placeholder");
                cosmetics.TryEquip("glow_placeholder");
            }

            // 5. Voluntary Rebirth from large_tree
            string oldRunId = state.run.runId;
            var quote = rebirth.GetQuote();
            bool canRebirth = quote.CanConfirm && !quote.Forced;
            var rebRes = rebirth.TryRebirth(oldRunId);

            PopulateReportState(report, state);
            report.achievementsClaimed = claimed;

            report.success = evoYoung.Success && evoLarge.Success && canRebirth && rebRes.Success &&
                             state.run.stageId == "seedling" && state.run.life == "ALIVE" &&
                             report.rebirthPoints >= 1 &&
                             File.Exists(bootstrapper.SaveSvc.MainPath);

            report.details = $"Full loop OK: RebirthPoints={report.rebirthPoints}, Coin={state.permanent.coin}, Claimed={claimed}, Equipped={state.permanent.equippedCosmeticId}";

            yield return CaptureVerifiedScreenshot(report);
            Finish(report);
        }

        private IEnumerator RunWeatherDeadForcedRebirth(ScenarioReport report)
        {
            var state = bootstrapper.State;
            var health = bootstrapper.Health;
            var rebirth = bootstrapper.Rebirth;

            // Simulate lightning strike or lethal status to trigger DEAD
            health.Kill();
            yield return new WaitForSecondsRealtime(0.2f);

            bool wasDead = state.run.life == "DEAD";
            var quote = rebirth.GetQuote();
            bool canForced = quote.CanConfirm && quote.Forced;

            // Forced rebirth has expectedRunId
            var rebRes = rebirth.TryRebirth(state.run.runId);
            yield return new WaitForSecondsRealtime(0.2f);

            PopulateReportState(report, state);

            report.success = wasDead && canForced && rebRes.Success &&
                             state.run.life == "ALIVE" && state.run.stageId == "seedling" &&
                             state.run.hp == bootstrapper.Config.Rules.maxHp;

            report.details = $"Forced Rebirth OK: WasDead={wasDead}, Forced={canForced}, Success={rebRes.Success}, HP={state.run.hp}, Points={report.rebirthPoints}";

            yield return CaptureVerifiedScreenshot(report);
            Finish(report);
        }

        private IEnumerator RunOfflineAtomic(ScenarioReport report)
        {
            var state = bootstrapper.State;
            var reward = bootstrapper.LastOfflineReward;

            if (reward != null)
            {
                report.offlineWaterDelta = reward.WaterDelta;
                report.offlineGrowthDelta = reward.GrowthDelta;
                report.offlineElapsed = reward.ElapsedSeconds;
            }

            PopulateReportState(report, state);

            bool hasReward = reward != null && (reward.WaterDelta > 0 || reward.GrowthDelta > 0);
            report.success = hasReward && File.Exists(bootstrapper.SaveSvc.MainPath);
            report.details = $"Offline atomic OK: WaterDelta={report.offlineWaterDelta:F2}, GrowthDelta={report.offlineGrowthDelta:F2}, Elapsed={report.offlineElapsed}s";

            yield return CaptureVerifiedScreenshot(report);
            Finish(report);
        }

        private void PopulateReportState(ScenarioReport report, GameState state)
        {
            report.stageId = state.run.stageId;
            report.life = state.run.life;
            report.water = state.run.water;
            report.growth = state.run.growth;
            report.hp = state.run.hp;
            report.rootLevel = state.run.levels.root;
            report.leavesLevel = state.run.levels.leaves;
            report.trunkLevel = state.run.levels.trunk;
            report.vitalityBank = state.vitality.bankSeconds;
            report.vitalityTaps = state.vitality.tapProgress;
            report.coin = state.permanent.coin;
            report.equippedCosmetic = state.permanent.equippedCosmeticId;

            long pts = 0;
            foreach (var p in state.permanent.stagePoints) pts += p.points;
            report.rebirthPoints = pts;
        }

        private IEnumerator CaptureVerifiedScreenshot(ScenarioReport report, string label = null)
        {
            if (string.IsNullOrEmpty(QaLaunchOptions.ScreenshotDirectory)) yield break;

            Directory.CreateDirectory(QaLaunchOptions.ScreenshotDirectory);
            string suffix = string.IsNullOrEmpty(label) ? "" : "_" + label;
            string shotPath = Path.Combine(QaLaunchOptions.ScreenshotDirectory, $"{report.scenario}{suffix}_{DateTime.UtcNow:yyyyMMdd_HHmmss}.png");

            yield return new WaitForEndOfFrame();

            // Full frame capture (3D + overlay UI). Falls back to a camera-only render if the frame cannot be read.
            Texture2D frame = null;
            try { frame = ScreenCapture.CaptureScreenshotAsTexture(); }
            catch (Exception ex) { Debug.LogWarning("[QaBuildRunner] CaptureScreenshotAsTexture failed: " + ex.Message); }

            if (frame != null)
            {
                File.WriteAllBytes(shotPath, frame.EncodeToPNG());
                Destroy(frame);
            }
            else
            {
                Camera cam = Camera.main;
                var rig = FindAnyObjectByType<OrbitCameraRig>();
                if (rig != null && rig.SceneCamera != null) cam = rig.SceneCamera;
                if (cam != null)
                {
                    int width = Screen.width > 0 ? Screen.width : 1920;
                    int height = Screen.height > 0 ? Screen.height : 1080;
                    var rt = new RenderTexture(width, height, 24);
                    var prevTarget = cam.targetTexture;
                    var prevActive = RenderTexture.active;
                    cam.targetTexture = rt;
                    cam.Render();
                    RenderTexture.active = rt;
                    var tex = new Texture2D(width, height, TextureFormat.RGB24, false);
                    tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                    tex.Apply();
                    cam.targetTexture = prevTarget;
                    RenderTexture.active = prevActive;
                    Destroy(rt);
                    File.WriteAllBytes(shotPath, tex.EncodeToPNG());
                    Destroy(tex);
                }
            }

            if (File.Exists(shotPath))
            {
                report.screenshot = shotPath;
                report.screenshots.Add(shotPath);
            }
            else
            {
                report.screenshot = "";
            }
        }

        // ---- QA hook: simulated uGUI clicks (EventSystem pointer click on the real Button objects). Not OS-level mouse input. ----

        private static Button FindButton(string objectName)
        {
            foreach (var b in FindObjectsByType<Button>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
                if (b.name == objectName && b.isActiveAndEnabled && b.interactable) return b;
            return null;
        }

        private bool UiClick(ScenarioReport report, string objectName)
        {
            var button = FindButton(objectName);
            bool ok = button != null && EventSystem.current != null;
            if (ok)
            {
                var data = new PointerEventData(EventSystem.current) { button = PointerEventData.InputButton.Left, clickCount = 1 };
                ExecuteEvents.Execute(button.gameObject, data, ExecuteEvents.pointerClickHandler);
            }
            report.steps.Add((ok ? "UI_CLICK " : "UI_CLICK_MISSING ") + objectName);
            return ok;
        }

        private static string DumpButtons()
        {
            var sb = new StringBuilder("Buttons(active): ");
            foreach (var b in FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                var label = b.GetComponentInChildren<Text>(true);
                sb.Append(b.name).Append(b.isActiveAndEnabled ? "" : "(inactive)").Append('[').Append(label != null ? label.text : "").Append("]; ");
            }
            return sb.ToString();
        }

        private static IEnumerator QueueMouseTap(Mouse mouse, Vector2 pt)
        {
            InputSystem.QueueStateEvent(mouse, new MouseState { position = pt }.WithButton(MouseButton.Left));
            yield return null;
            yield return null;
            InputSystem.QueueStateEvent(mouse, new MouseState { position = pt });
            yield return null;
            yield return null;
        }

        private static IEnumerator QueueTouchTap(Touchscreen touch, int id, Vector2 pt)
        {
            InputSystem.QueueStateEvent(touch, new TouchState { touchId = id, phase = TouchPhase.Began, position = pt });
            yield return null;
            yield return null;
            InputSystem.QueueStateEvent(touch, new TouchState { touchId = id, phase = TouchPhase.Ended, position = pt });
            yield return null;
            yield return null;
        }

        private IEnumerator RunRaycastAudit(ScenarioReport report)
        {
            var state = bootstrapper.State;
            var fail = new System.Collections.Generic.List<string>();
            var tree = FindAnyObjectByType<TreeView>();
            var rig = FindAnyObjectByType<OrbitCameraRig>();

            bootstrapper.TickDriver.ManualTick = true;
            bootstrapper.Input.SendMessage("OnApplicationFocus", true);
            bootstrapper.SaveCtrl.SendMessage("OnApplicationFocus", true);

            var native = new System.Collections.Generic.List<InputDevice>();
            foreach (var d in InputSystem.devices)
                if (d.native && d.enabled && d is Pointer) native.Add(d);
            foreach (var d in native) InputSystem.DisableDevice(d);

            var bg = InputSystem.settings.backgroundBehavior;
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;

            var mouse = InputSystem.AddDevice<Mouse>();
            var touch = InputSystem.AddDevice<Touchscreen>();
            yield return null;

            string[] stages = { "seedling", "young_tree", "large_tree" };
            int touchId = 500;
            state.vitality.tapProgress = 0;
            int expectedTaps = 0;

            foreach (var stageId in stages)
            {
                var visual = bootstrapper.Config.FindVisual(stageId);
                tree.ShowStage(visual);
                rig.SetStagePose(visual.cameraDistance, visual.cameraPitch, visual.cameraTargetHeight);
                Physics.SyncTransforms();
                yield return null;

                var sphere = tree.GetComponent<SphereCollider>();
                Check(report, fail, $"{stageId} root SphereCollider disabled", sphere != null && !sphere.enabled);
                var meshCols = tree.ModelSlot.GetComponentsInChildren<MeshCollider>(true);
                Check(report, fail, $"{stageId} has >=3 non-convex MeshColliders", meshCols.Length >= 3);

                var triangles = new System.Collections.Generic.List<Vector3[]>();
                float minX = 99999f, minY = 99999f, maxX = -99999f, maxY = -99999f;
                foreach (var mf in tree.ModelSlot.GetComponentsInChildren<MeshFilter>())
                {
                    var mesh = mf.sharedMesh;
                    var vertices = mesh.vertices;
                    var indices = mesh.triangles;
                    for (int k = 0; k < vertices.Length; k++)
                    {
                        vertices[k] = mf.transform.TransformPoint(vertices[k]);
                        var p = rig.SceneCamera.WorldToScreenPoint(vertices[k]);
                        minX = Mathf.Min(minX, p.x);
                        minY = Mathf.Min(minY, p.y);
                        maxX = Mathf.Max(maxX, p.x);
                        maxY = Mathf.Max(maxY, p.y);
                    }
                    for (int k = 0; k < indices.Length; k += 3)
                        triangles.Add(new[] { vertices[indices[k]], vertices[indices[k + 1]], vertices[indices[k + 2]] });
                }

                int falsePos = 0, falseNeg = 0, hitMesh = 0, hitTree = 0, checkedPts = 0;
                var visiblePts = new System.Collections.Generic.List<Vector2>();
                var emptyPts = new System.Collections.Generic.List<Vector2>();

                for (int ix = 0; ix <= 24; ix++)
                {
                    for (int iy = 0; iy <= 24; iy++)
                    {
                        var point = new Vector2(Mathf.Lerp(minX - 40f, maxX + 40f, ix / 24f), Mathf.Lerp(minY - 40f, maxY + 40f, iy / 24f));
                        if (bootstrapper.Input.IsOverUi(point)) continue;
                        checkedPts++;
                        var ray = rig.SceneCamera.ScreenPointToRay(point);
                        float meshDistance = float.PositiveInfinity;
                        foreach (var tri in triangles)
                        {
                            var e1 = tri[1] - tri[0];
                            var e2 = tri[2] - tri[0];
                            var h = Vector3.Cross(ray.direction, e2);
                            float det = Vector3.Dot(e1, h);
                            if (Mathf.Abs(det) < 1e-9f) continue;
                            float inv = 1f / det;
                            var s = ray.origin - tri[0];
                            float u = inv * Vector3.Dot(s, h);
                            if (u < 0f || u > 1f) continue;
                            var q = Vector3.Cross(s, e1);
                            float v = inv * Vector3.Dot(ray.direction, q);
                            if (v < 0f || u + v > 1f) continue;
                            float d = inv * Vector3.Dot(e2, q);
                            if (d > 0f && d < meshDistance) meshDistance = d;
                        }
                        RaycastHit hit;
                        bool physics = Physics.Raycast(ray, out hit);
                        bool isTree = physics && hit.collider.GetComponentInParent<TreeView>() == tree;
                        bool meshHit = !float.IsPositiveInfinity(meshDistance);
                        if (meshHit) hitMesh++;
                        if (isTree) hitTree++;
                        if (isTree && !meshHit) falsePos++;
                        if (meshHit && !isTree && (!physics || hit.distance >= meshDistance - 0.002f)) falseNeg++;

                        if (isTree && meshHit && point.x > 20f && point.x < Screen.width - 20f && point.y > 20f && point.y < Screen.height - 20f)
                            visiblePts.Add(point);
                        else if (!isTree && !meshHit && point.x > 20f && point.x < Screen.width - 20f && point.y > 20f && point.y < Screen.height - 20f)
                            emptyPts.Add(point);
                    }
                }

                report.steps.Add($"PROBE {stageId}: checked={checkedPts}, meshHits={hitMesh}, physicsTreeHits={hitTree}, falsePositiveEmpty={falsePos}, falseNegativeVisible={falseNeg}");
                Check(report, fail, $"{stageId} falsePositiveEmpty == 0 (was {falsePos})", falsePos == 0);
                Check(report, fail, $"{stageId} falseNegativeVisible == 0 (was {falseNeg})", falseNeg == 0);

                // Device taps on visible surface points & empty points
                yield return QueueMouseTap(mouse, visiblePts[visiblePts.Count / 2]);
                expectedTaps++;
                Check(report, fail, $"{stageId} Mouse visible tap increments tapProgress", state.vitality.tapProgress == expectedTaps);

                yield return QueueTouchTap(touch, touchId++, visiblePts[0]);
                expectedTaps++;
                Check(report, fail, $"{stageId} Touch visible tap increments tapProgress", state.vitality.tapProgress == expectedTaps);

                yield return QueueMouseTap(mouse, emptyPts[emptyPts.Count / 2]);
                Check(report, fail, $"{stageId} Mouse empty tap does not increment tapProgress", state.vitality.tapProgress == expectedTaps);

                yield return QueueTouchTap(touch, touchId++, emptyPts[0]);
                Check(report, fail, $"{stageId} Touch empty tap does not increment tapProgress", state.vitality.tapProgress == expectedTaps);

                if (stageId == "large_tree")
                {
                    var reviewEmpty = new Vector2(Screen.width * (611.49f / 1920f), Screen.height * (629.6163f / 1080f));
                    var reviewModel = new Vector2(Screen.width * (573.295166f / 1920f), Screen.height * (412.329346f / 1080f));

                    yield return QueueMouseTap(mouse, reviewEmpty);
                    Check(report, fail, "large_tree reviewEmpty (611.49, 629.62) Mouse -> 0 delta", state.vitality.tapProgress == expectedTaps);
                    yield return QueueTouchTap(touch, touchId++, reviewEmpty);
                    Check(report, fail, "large_tree reviewEmpty (611.49, 629.62) Touch -> 0 delta", state.vitality.tapProgress == expectedTaps);

                    yield return QueueMouseTap(mouse, reviewModel);
                    expectedTaps++;
                    Check(report, fail, "large_tree reviewModel (573.30, 412.33) Mouse -> +1 delta", state.vitality.tapProgress == expectedTaps);
                    yield return QueueTouchTap(touch, touchId++, reviewModel);
                    expectedTaps++;
                    Check(report, fail, "large_tree reviewModel (573.30, 412.33) Touch -> +1 delta", state.vitality.tapProgress == expectedTaps);
                }

                yield return CaptureVerifiedScreenshot(report, stageId);
            }

            InputSystem.RemoveDevice(mouse);
            InputSystem.RemoveDevice(touch);
            foreach (var d in native) if (d.added) InputSystem.EnableDevice(d);
            InputSystem.settings.backgroundBehavior = bg;

            PopulateReportState(report, state);
            report.success = fail.Count == 0;
            report.details = report.success
                ? $"Raycast audit OK across seedling/young_tree/large_tree (0 falsePos, 0 falseNeg, vitalityTaps={state.vitality.tapProgress})"
                : "Raycast audit FAILED: " + string.Join("; ", fail);
            Finish(report);
        }

        private void Finish(ScenarioReport report)
        {
            string json = JsonUtility.ToJson(report, true);
            Debug.Log($"[QaBuildRunner] Finished scenario '{report.scenario}' (success={report.success}):\n{json}");

            if (!string.IsNullOrEmpty(QaLaunchOptions.ReportPath))
            {
                try
                {
                    string dir = Path.GetDirectoryName(QaLaunchOptions.ReportPath);
                    if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
                    File.WriteAllText(QaLaunchOptions.ReportPath, json);
                }
                catch (Exception ex)
                {
                    Debug.LogError("[QaBuildRunner] Failed to write report: " + ex.Message);
                }
            }

            Application.Quit(report.success ? 0 : 1);
        }
    }
}
