using System;
using CayTheGioi.Configuration;
using CayTheGioi.Domain;
using CayTheGioi.Infrastructure;
using CayTheGioi.Presentation;
using UnityEngine;

namespace CayTheGioi.Bootstrap
{
    [DisallowMultipleComponent]
    public sealed class GameBootstrapper : MonoBehaviour
    {
        [SerializeField] private PrototypeConfig config;
        [SerializeField] private TreeView treeView;
        [SerializeField] private OrbitCameraRig cameraRig;
        [SerializeField] private FoundationHudView hud;

        public GameState State { get; private set; }
        public bool IsInitialized { get; private set; }
        public PrototypeConfig Config => config;
        public ResourceSystem Resources { get; private set; }
        public VitalitySystem Vitality { get; private set; }
        public TreeInputAdapter Input { get; private set; }
        public OnlineTickDriver TickDriver { get; private set; }
        public UpgradeSystem Upgrades { get; private set; }
        public RebirthSystem Rebirth { get; private set; }
        public WeatherSystem Weather { get; private set; }
        public HealthStatusSystem Health { get; private set; }
        public EvolutionSystem Evolution { get; private set; }
        public EvolutionTransition Transition { get; private set; }
        public SaveService SaveSvc { get; private set; }
        public OfflineReward LastOfflineReward => session?.LastOfflineReward;
        public SaveController SaveCtrl { get; private set; }
        public AchievementSystem Achievements { get; private set; }
        public CosmeticSystem Cosmetics { get; private set; }
        public CosmeticView CosmeticVisual { get; private set; }
        public ShopView Shop { get; private set; }
        public AudioController Audio { get; private set; }
        public ToastView Toast { get; private set; }
        public SettingsView Settings { get; private set; }
        public TutorialSystem Tutorial { get; private set; }
        public TutorialView TutorialVisual { get; private set; }
        public UiPriorityManager UiPriority { get; private set; }

        // Populated after load; used by tests and debug tools.
        public SaveService.LoadOutcome LastLoadOutcome { get; private set; } = SaveService.LoadOutcome.NewGame;

        private SaveSession session;
        private bool confirmRecovery;
        public string LoadError { get; private set; }
        public bool RetryLoad(bool recover = false)
        {
            confirmRecovery = recover;
            bool ok = TryInitialize(out string error);
            LoadError = error;
            var view = hud.GetComponent<SaveStatusView>();
            if (view != null) view.Refresh();
            return ok;
        }
        public SaveResult ArchiveAndReset()
        {
            if (IsInitialized) return SaveResult.Fail("RESET_REQUIRES_BLOCKED_LOAD");
            if (SaveSvc == null) return SaveResult.Fail("NOT_READY");
            var result = SaveSvc.ArchiveForReset();
            if (result.Success) { session = null; RetryLoad(); }
            return result;
        }

        private void Awake()
        {
            if (!TryInitialize(out string error))
            {
                if (hud != null) hud.ShowUnavailable();
                LoadError = error;
                if (SaveSvc == null) Debug.LogError("[CayTheGioi] Foundation initialization failed: " + error, this);
                else Debug.Log("[CayTheGioi] Load blocked: " + error, this);
            }
        }

        public bool TryInitialize(out string error)
        {
            error = null;
            if (IsInitialized) return true;
            if (config == null || treeView == null || cameraRig == null || hud == null)
            { error = "Missing bootstrap configuration or presentation references."; return false; }

            var errors = config.Validate();
            if (errors.Count != 0) { error = string.Join("\n", errors); return false; }

            // Runtime snapshot isolates read-only tuning from Inspector changes while playing.
            var rules = JsonUtility.FromJson<PrototypeRules>(JsonUtility.ToJson(config.Rules));

            // â”€â”€ 1. Load or create state â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
            var saveView = hud.GetComponent<SaveStatusView>();
            if (saveView != null) saveView.Bind(this);
            session = session ?? new SaveSession(rules);
            SaveSvc = session.Service;
            var load = session.Load(confirmRecovery);
            confirmRecovery = false;
            LastLoadOutcome = session.Outcome;
            if (!load.Success) { error = load.Reason; LoadError = error; if (saveView != null) saveView.Refresh(); return false; }
            State = session.State;
            LoadError = null;
            string loadedSaveId = session.SaveId;
            long loadedRevision = session.Revision;

            // â”€â”€ 2. Wire domain systems â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
            Vitality = new VitalitySystem(State, rules, () => (Resources != null && Resources.SimulationBlocked) || (Rebirth != null && Rebirth.IsExecuting));
            Resources = new ResourceSystem(State, rules, Vitality, () => Rebirth != null && Rebirth.IsExecuting);
            Weather = new WeatherSystem(State, rules);
            Health = new HealthStatusSystem(State, rules);
            Evolution = new EvolutionSystem(State, rules, Resources, () => Rebirth != null && Rebirth.IsExecuting);
            Upgrades = new UpgradeSystem(State, rules, Resources, () => Evolution.IsTransitioning || Resources.SimulationBlocked || (Rebirth != null && Rebirth.IsExecuting));

            // â”€â”€ 3. Wire presentation â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
            Audio = GetComponent<AudioController>() ?? gameObject.AddComponent<AudioController>();

            var visual = config.FindVisual(State.run.stageId);
            treeView.ShowStage(visual);

            var treeAnimation = treeView.GetComponent<TreeAnimationController>() ?? treeView.gameObject.AddComponent<TreeAnimationController>();
            treeAnimation.Bind(treeView, Weather, Health, config != null ? config.TreeAnimation : null);

            cameraRig.OrbitLocked = State.settings.orbitLocked;
            cameraRig.SetStagePose(visual.cameraDistance, visual.cameraPitch, visual.cameraTargetHeight);
            hud.Bind(State, config.Rules.FindStage(State.run.stageId).displayName, config.Rules.tapsPerReward);
            hud.BindSystems(Resources, Vitality, Weather, Health);

            var weatherView = GetComponent<WeatherView>() ?? gameObject.AddComponent<WeatherView>();
            weatherView.Bind(Weather, Health, treeView, cameraRig, Audio, Rebirth, config != null ? config.WeatherVfx : null);

            var upgradeView = hud.GetComponent<UpgradePanelView>();
            if (upgradeView != null) upgradeView.Bind(Upgrades, Resources, Vitality);

            TickDriver = GetComponent<OnlineTickDriver>() ?? gameObject.AddComponent<OnlineTickDriver>();
            TickDriver.Bind(Resources, Weather, Health);

            Input = GetComponent<TreeInputAdapter>() ?? gameObject.AddComponent<TreeInputAdapter>();
            Input.Bind(State, Vitality, JsonUtility.FromJson<GameplayInputConfig>(JsonUtility.ToJson(config.Input)), treeView, cameraRig);

            Transition = GetComponent<EvolutionTransition>() ?? gameObject.AddComponent<EvolutionTransition>();
            Transition.Bind(Evolution, config, State, treeView, cameraRig, Input, hud);
            Evolution.TransitionCompleted += () => { if (upgradeView != null) upgradeView.Refresh(); };

            var evolutionView = hud.GetComponent<EvolutionPanelView>();
            if (evolutionView != null) evolutionView.Bind(Evolution, Resources, State, cameraRig);

            // â”€â”€ 4. SaveController & Meta â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
            SaveCtrl = GetComponent<SaveController>() ?? gameObject.AddComponent<SaveController>();
            SaveCtrl.Bind(State, SaveSvc, loadedSaveId, loadedRevision, (float)rules.autosaveIntervalSeconds, session.Offline, Resources, Input, TickDriver);
            Rebirth = new RebirthSystem(State, rules, SaveSerializer.Clone, SaveCtrl.CommitCandidate,
                () => Evolution.IsTransitioning || Resources.SimulationBlocked || SaveCtrl.Suspended || SaveCtrl.AwaitingOffline);
            var rebirthView = hud.GetComponent<RebirthView>() ?? hud.gameObject.AddComponent<RebirthView>();
            rebirthView.Bind(this, config, treeView, cameraRig, Input, hud, rules);
            Rebirth.Completed += () =>
            {
                Weather.ResetForRebirth();
                Health.ResetForRebirth();
                if (weatherView != null) weatherView.ResetForRebirth();
                if (saveView != null) saveView.Refresh();
            };

            Achievements = new AchievementSystem(State, rules, () =>
            {
                if (SaveCtrl == null) return new CommandResult(true);
                var res = SaveCtrl.ForceSave("ACHIEVEMENT_CLAIM");
                return new CommandResult(res.Success, res.Reason);
            });
            Upgrades.Completed += Achievements.OnUpgradeCompleted;
            Evolution.StageChanged += Achievements.OnStageChanged;
            Rebirth.Completed += Achievements.OnRebirthCompleted;
            Vitality.RewardEarned += Achievements.OnVitalityRewardEarned;
            Achievements.SyncFromCurrentState();

            hud.BindSystems(Resources, Vitality, Weather, Health, Achievements);

            var achievementView = hud.GetComponent<AchievementView>() ?? hud.gameObject.AddComponent<AchievementView>();
            achievementView.Bind(Achievements, State, Input, cameraRig);

            Cosmetics = new CosmeticSystem(State, rules, () =>
            {
                if (SaveCtrl == null) return new CommandResult(true);
                var res = SaveCtrl.ForceSave("COSMETIC_COMMIT");
                return new CommandResult(res.Success, res.Reason);
            });

            CosmeticVisual = GetComponent<CosmeticView>() ?? gameObject.AddComponent<CosmeticView>();
            CosmeticVisual.Bind(Cosmetics, treeView, Health, Evolution, Rebirth);

            Shop = hud.GetComponent<ShopView>() ?? hud.gameObject.AddComponent<ShopView>();
            Shop.Bind(Cosmetics, CosmeticVisual, State, Input, cameraRig);

            var offlineView = hud.GetComponent<OfflineRewardView>() ?? hud.gameObject.AddComponent<OfflineRewardView>();
            offlineView.Bind();
            SaveCtrl.OfflineCommitted += offlineView.Show;
            SaveCtrl.StatusChanged += () => { if (saveView != null) saveView.Refresh(); };

            // Force-save after Evolution so persistent data is committed.
            Evolution.StageChanged += stage => SaveCtrl.ForceSave("EVOLUTION_COMMIT");

            // â”€â”€ 5. Audio, Toast, Settings, Tutorial & UI Priority (T08C) â”€â”€
            Audio = GetComponent<AudioController>() ?? gameObject.AddComponent<AudioController>();
            Audio.Bind(State, Weather, Health, Rebirth, config != null ? config.AudioConfig : null);

            Toast = hud.GetComponent<ToastView>() ?? hud.gameObject.AddComponent<ToastView>();
            Toast.Bind();

            Settings = hud.GetComponent<SettingsView>() ?? hud.gameObject.AddComponent<SettingsView>();
            Settings.Bind(State, Audio, cameraRig, Input, id => SaveCtrl.ForceSave(id), Toast);

            Tutorial = new TutorialSystem(State, rules, () =>
            {
                if (SaveCtrl == null) return new CommandResult(true);
                var res = SaveCtrl.ForceSave("TUTORIAL_STEP");
                return new CommandResult(res.Success, res.Reason);
            });

            TutorialVisual = hud.GetComponent<TutorialView>() ?? hud.gameObject.AddComponent<TutorialView>();
            TutorialVisual.Bind(Tutorial, State, Input, Toast);

            UiPriority = GetComponent<UiPriorityManager>() ?? gameObject.AddComponent<UiPriorityManager>();
            UiPriority.Bind(State, rebirthView, Settings, Shop, achievementView, upgradeView);

            // Audio & Tutorial event wiring
            Vitality.Tapped += () => Audio.PlayTap();
            Vitality.RewardEarned += () =>
            {
                Tutorial.OnVitalityRewardEarned(State.vitality.bankSeconds);
                if (State.vitality.bankSeconds >= rules.vitalityMaxBankSeconds)
                {
                    Toast.Show("Sá»©c sá»‘ng Ä‘Ã£ Ä‘áº¡t tá»‘i Ä‘a (3600s)!");
                }
            };
            Upgrades.Completed += branchId =>
            {
                Audio.PlayUpgrade();
                Tutorial.OnUpgradeCompleted(branchId);
            };
            Evolution.StageChanged += stage =>
            {
                Audio.PlayEvolution();
                Tutorial.OnStageChanged(stage);
            };
            Cosmetics.Purchased += id => Audio.PlayShop();
            Achievements.Claimed += (id, coin) => Audio.PlayAchievement();

            Rebirth.Completed += () =>
            {
                TutorialVisual?.Refresh();
            };

            GameUiTheme.Apply(hud);
            IsInitialized = true;
            offlineView.Show(session.LastOfflineReward);
            if (saveView != null) saveView.Refresh();
            if (QaLaunchOptions.Active && !string.IsNullOrEmpty(QaLaunchOptions.Scenario))
            {
                gameObject.AddComponent<QaBuildRunner>();
            }
            Debug.Log($"[CayTheGioi] T07 initialized: {rules.configVersion}, stage={State.run.stageId}, loadOutcome={LastLoadOutcome}", this);
            return true;
        }

#if UNITY_EDITOR
        public void Configure(PrototypeConfig profile, TreeView tree, OrbitCameraRig camera, FoundationHudView view)
        { config = profile; treeView = tree; cameraRig = camera; hud = view; }
#endif
    }
}
