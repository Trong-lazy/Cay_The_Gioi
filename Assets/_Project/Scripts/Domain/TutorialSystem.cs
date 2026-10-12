using System;
using System.Collections.Generic;

namespace CayTheGioi.Domain
{
    public sealed class TutorialStepDefinition
    {
        public string StepId;
        public string Title;
        public string Description;
        public string TargetElement; // "tree", "upgrade_root", "upgrade_leaves", "evolution", "dialogue"
        public bool RequiresConfirmation; // Cần người chơi bấm "Đã hiểu" (cho dialogue)
    }

    public sealed class TutorialSystem
    {
        public const string StepVitality = "step_vitality";
        public const string StepUpgradeRoot = "step_upgrade_root";
        public const string StepUpgradeLeaves = "step_upgrade_leaves";
        public const string StepUpgradeTrunk="step_upgrade_trunk";
        public const string StepEvolution = "step_evolution";
        public const string StepTrunkWeather = "step_trunk_weather";
        public const string StepRebirthIntro = "step_rebirth_intro";

        private readonly GameState state;
        private readonly PrototypeRules rules;
        private readonly Func<CommandResult> saveCallback;

        public event Action<string> StepCompleted;
        public event Action StateChanged;

        private readonly List<TutorialStepDefinition> stepDefinitions = new List<TutorialStepDefinition>
        {
            new TutorialStepDefinition
            {
                StepId = StepVitality,
                Title = "Tích lũy Sức sống",
                Description = "Nhấp chuột hoặc chạm vào Cây để tích lũy Sức sống. Cứ 50 lần tap nhận 20s Sức sống (tăng +5% sản xuất khi có quỹ, tối đa 3600s).",
                TargetElement = "tree",
                RequiresConfirmation = false
            },
            new TutorialStepDefinition
            {
                StepId = StepUpgradeRoot,
                Title = "Nâng cấp Rễ (Root)",
                Description = "Mở Nâng Cấp và mua Rễ cấp 1 bằng 5 Water khởi đầu để tự động hút Nước.",
                TargetElement = "upgrade_root",
                RequiresConfirmation = false
            },
            new TutorialStepDefinition
            {
                StepId = StepUpgradeLeaves,
                Title = "Nâng cấp Lá (Leaves)",
                Description = "Dùng Quang hợp khởi đầu mua Lá cấp 1. Lá tiêu Nước để tạo Quang hợp; dùng Growth khởi đầu mua Thân để bắt đầu tạo Growth.",
                TargetElement = "upgrade_leaves",
                RequiresConfirmation = false
            },
            new TutorialStepDefinition
            {
                StepId=StepUpgradeTrunk,
                Title="Nâng cấp Thân (Trunk)",
                Description="Dùng 10 Growth khởi đầu mua Thân cấp 1. Thân tiêu Quang hợp để tạo Growth và tăng Protection.",
                TargetElement="upgrade_trunk",
                RequiresConfirmation=false
            },
            new TutorialStepDefinition
            {
                StepId = StepEvolution,
                Title = "Tiến hóa Cây Non",
                Description = "Khi tích lũy đủ Nước, Quang hợp và Growth, bấm Tiến Hóa để phát triển lên Cây Non!",
                TargetElement = "evolution",
                RequiresConfirmation = false
            },
            new TutorialStepDefinition
            {
                StepId = StepTrunkWeather,
                Title = "Thân cây & Thời tiết",
                Description = "Ở Cây Non, bạn đã mở Thời Tiết. Thân tạo Growth từ Quang hợp và tăng Protection giúp giảm rủi ro sét. Hãy theo dõi dự báo để phòng ngừa thời tiết xấu!",
                TargetElement = "dialogue",
                RequiresConfirmation = true
            },
            new TutorialStepDefinition
            {
                StepId = StepRebirthIntro,
                Title = "Hệ thống Tái Sinh (Rebirth)",
                Description = "Khi cây gặp rủi ro hoặc hoàn thành chu kỳ, bạn có thể Tái Sinh (Rebirth) để nhận Điểm vĩnh viễn giúp cây chu kỳ sau phát triển mạnh mẽ hơn.",
                TargetElement = "dialogue",
                RequiresConfirmation = true
            }
        };

        public IReadOnlyList<TutorialStepDefinition> StepDefinitions => stepDefinitions;

        public TutorialSystem(GameState gameState, PrototypeRules prototypeRules, Func<CommandResult> onSaveStep = null)
        {
            state = gameState ?? throw new ArgumentNullException(nameof(gameState));
            rules = prototypeRules ?? throw new ArgumentNullException(nameof(prototypeRules));
            saveCallback = onSaveStep;

            if (state.permanent.tutorialCompletedStepIds == null)
            {
                state.permanent.tutorialCompletedStepIds = new List<string>();
            }
        }

        public bool IsStepCompleted(string stepId)
        {
            if (state.permanent.tutorialCompletedStepIds == null) return false;
            return state.permanent.tutorialCompletedStepIds.Contains(stepId);
        }

        public TutorialStepDefinition GetStepDefinition(string stepId)
        {
            for (int i = 0; i < stepDefinitions.Count; i++)
            {
                if (stepDefinitions[i].StepId == stepId)
                    return stepDefinitions[i];
            }
            return null;
        }

        public string GetCurrentStepId()
        {
            if (state.run.life == "DEAD") return null;

            if (!IsStepCompleted(StepVitality)) return StepVitality;
            if (!IsStepCompleted(StepUpgradeRoot)) return StepUpgradeRoot;
            if (!IsStepCompleted(StepUpgradeLeaves)) return StepUpgradeLeaves;
            if(!IsStepCompleted(StepUpgradeTrunk)&&!IsStepCompleted(StepEvolution))return StepUpgradeTrunk;
            if (!IsStepCompleted(StepEvolution)) return StepEvolution;

            bool isYoungTreeOrHigher = state.run.stageId == "young_tree" || state.run.stageId == "large_tree" || state.run.stageId=="mature_tree";
            if (isYoungTreeOrHigher)
            {
                if (!IsStepCompleted(StepTrunkWeather)) return StepTrunkWeather;
                if (!IsStepCompleted(StepRebirthIntro)) return StepRebirthIntro;
            }

            return null;
        }

        public CommandResult TryCompleteStep(string stepId, Func<bool> validation = null)
        {
            if (string.IsNullOrEmpty(stepId)) return new CommandResult(false, "INVALID_STEP_ID");
            if (IsStepCompleted(stepId)) return new CommandResult(false, "ALREADY_COMPLETED");

            if (validation != null && !validation())
            {
                return new CommandResult(false, "VALIDATION_FAILED");
            }

            state.permanent.tutorialCompletedStepIds.Add(stepId);

            if (saveCallback != null)
            {
                var saveRes = saveCallback.Invoke();
                if (!saveRes.Success)
                {
                    state.permanent.tutorialCompletedStepIds.Remove(stepId);
                    return new CommandResult(false, saveRes.Reason ?? "SAVE_FAILED");
                }
            }

            StepCompleted?.Invoke(stepId);
            StateChanged?.Invoke();
            return new CommandResult(true);
        }

        // Domain event hooks
        public void OnVitalityRewardEarned(double bankSeconds)
        {
            if (!IsStepCompleted(StepVitality) && bankSeconds > 0)
            {
                TryCompleteStep(StepVitality);
            }
        }

        public void OnUpgradeCompleted(string branchId)
        {
            if (branchId == "root" && !IsStepCompleted(StepUpgradeRoot))
            {
                TryCompleteStep(StepUpgradeRoot);
            }
            else if(branchId=="trunk"&&!IsStepCompleted(StepUpgradeTrunk))
            { TryCompleteStep(StepUpgradeTrunk); }
            else if (branchId == "leaves" && !IsStepCompleted(StepUpgradeLeaves))
            {
                TryCompleteStep(StepUpgradeLeaves);
            }
        }

        public void OnStageChanged(string newStageId)
        {
            if (newStageId != "seedling" && !IsStepCompleted(StepEvolution))
            {
                TryCompleteStep(StepEvolution);
            }
            StateChanged?.Invoke();
        }

        public void ConfirmDialogueStep(string stepId)
        {
            var def = GetStepDefinition(stepId);
            if (def != null && def.RequiresConfirmation)
            {
                TryCompleteStep(stepId);
            }
        }
    }
}
