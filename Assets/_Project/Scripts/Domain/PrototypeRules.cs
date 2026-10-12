using System;
using System.Collections.Generic;

namespace CayTheGioi.Domain
{
    [Serializable]
    public sealed class StageDefinition
    {
        public string stageId;
        public string displayName;
        public string nextStageId;
        public int rootCap;
        public int leavesCap;
        public int trunkCap;
        public int spiritAbsorptionCap;
        public double evolutionWaterCost;
        public double evolutionPhotosynthesisCost;
        public double evolutionGrowthCost;
        public double exposure;
        public bool rebirthUnlocked;
        public bool weatherUnlocked;
        public bool harmfulWeatherUnlocked;
    }

    [Serializable]
    public sealed class PointEffectDefinition
    {
        public string stageId;
        public string targetId;
        public double perPoint;
    }

    [Serializable]
    public sealed class AchievementDefinition
    {
        public string achievementId;
        public string displayName;
        public string description;
        public long targetProgress = 1;
        public long coinReward = 10;
    }

    [Serializable]
    public sealed class CosmeticDefinition
    {
        public string cosmeticId;
        public string displayName;
        public string description;
        public long price = 20;
    }

    [Serializable]
    public sealed class PrototypeRules
    {
        public string configVersion = "prototype-6";
        public string initialStageId = "seedling";
        public double initialWater = 10;
        public double initialPhotosynthesis = 5;
        public double initialGrowth = 10;
        public double maxHp = 100;
        public double initialCalmSeconds = 120;
        public double minWeatherDurationSeconds = 180;
        public double maxWeatherDurationSeconds = 600;
        public double sunnyGrowthMultiplier = 1.2;
        public double rainyWaterMultiplier = 1.2;
        public double baseStrikeRate = 0.001;
        public double strikeCooldownOnlineSeconds = 120;
        public double thunderstormStrikeProbability = 0.10;
        public double strikeDelayFraction = 0.30;
        public double burningDurationSeconds = 10;
        public double burningDamagePerSecond = 2;
        public double burningLeavesGrowthMultiplier = 0.7;
        public PointEffectDefinition[] pointEffects = {
            new PointEffectDefinition { stageId="seedling", targetId="water", perPoint=0.05 },
            new PointEffectDefinition { stageId="young_tree", targetId="growth", perPoint=0.05 },
            new PointEffectDefinition { stageId="large_tree", targetId="protection", perPoint=0.02 },
            new PointEffectDefinition { stageId="mature_tree", targetId="spirit", perPoint=0.05 }
        };
        public double rootWaterPerLevelSecond = 1;
        public double leavesPhotosynthesisPerLevelSecond = 1;
        public double trunkGrowthPerLevelSecond = 1;
        public double trunkPhotosynthesisPerLevelSecond = 0.25;
        public double leavesWaterPerLevelSecond = 0.25;
        public double trunkProtectionPerLevel = 0.05;
        public double spiritPerLevelSecond = 1;
        public double spiritAbsorptionBaseCost = 10;
        public double spiritUnlockWaterCost = 1000;
        public double spiritUnlockPhotosynthesisCost = 1000;
        public double spiritUnlockGrowthCost = 1000;
        public double rootBaseCost = 5;
        public double leavesBaseCost = 5;
        public double trunkBaseCost = 10;
        public string rootCostResourceId = "water";
        public string leavesCostResourceId = "photosynthesis";
        public string trunkCostResourceId = "growth";
        public double upgradeCostFactor = 1.6;
        public double autosaveIntervalSeconds = 30;
        public double offlineMaxSeconds = 86400;
        public double offlineRateFactor = 0.5;
        public int tapsPerReward = 50;
        public double secondsPerTapReward = 20;
        public double vitalityMaxBankSeconds = 3600;
        public double vitalityOutputMultiplier = 1.05;
        public StageDefinition[] stages = DefaultStages();
        public AchievementDefinition[] achievements = DefaultAchievements();
        public CosmeticDefinition[] cosmetics = DefaultCosmetics();

        public StageDefinition FindStage(string id)
        {
            if (stages == null) return null;
            foreach (var stage in stages)
                if (stage != null && stage.stageId == id) return stage;
            return null;
        }

        public AchievementDefinition FindAchievement(string id)
        {
            if (achievements == null) return null;
            foreach (var a in achievements)
                if (a != null && a.achievementId == id) return a;
            return null;
        }

        public CosmeticDefinition FindCosmetic(string id)
        {
            if (cosmetics == null) return null;
            foreach (var c in cosmetics)
                if (c != null && c.cosmeticId == id) return c;
            return null;
        }

        public static bool IsUpgradeResourceId(string id) => id == "water" || id == "photosynthesis" || id == "growth" || id == "spirit";

        public string UpgradeResourceId(string branchId, int currentLevel)
        {
            switch (branchId)
            {
                case "root": return rootCostResourceId;
                case "leaves": return leavesCostResourceId;
                case "trunk": return trunkCostResourceId;
                case "spirit_absorption": return "spirit";
                default: return null;
            }
        }

        public double CalculateBurningDamagePerSecond(double totalProtection)
        {
            if (!Finite(burningDamagePerSecond) || burningDamagePerSecond <= 0) return 0;
            double prot = Math.Max(0, totalProtection);
            return burningDamagePerSecond / (1.0 + prot);
        }

        public List<string> Validate()
        {
            var errors = new List<string>();
            if (string.IsNullOrWhiteSpace(configVersion)) errors.Add("configVersion is required.");
            NonNegative(errors, "initialWater", initialWater);
            NonNegative(errors, "initialPhotosynthesis", initialPhotosynthesis);
            NonNegative(errors, "initialGrowth", initialGrowth);
            Positive(errors, "maxHp", maxHp);
            Positive(errors, "initialCalmSeconds", initialCalmSeconds);
            Positive(errors, "minWeatherDurationSeconds", minWeatherDurationSeconds);
            Positive(errors, "maxWeatherDurationSeconds", maxWeatherDurationSeconds);
            if (minWeatherDurationSeconds > maxWeatherDurationSeconds) errors.Add("minWeatherDurationSeconds cannot exceed maxWeatherDurationSeconds.");
            Positive(errors, "sunnyGrowthMultiplier", sunnyGrowthMultiplier);
            Positive(errors, "rainyWaterMultiplier", rainyWaterMultiplier);
            NonNegative(errors, "baseStrikeRate", baseStrikeRate);
            NonNegative(errors, "strikeCooldownOnlineSeconds", strikeCooldownOnlineSeconds);
            NonNegative(errors, "thunderstormStrikeProbability", thunderstormStrikeProbability);
            if (thunderstormStrikeProbability > 1.0) errors.Add("thunderstormStrikeProbability cannot exceed 1.0.");
            NonNegative(errors, "strikeDelayFraction", strikeDelayFraction);
            if (strikeDelayFraction > 1.0) errors.Add("strikeDelayFraction cannot exceed 1.0.");
            Positive(errors, "burningDurationSeconds", burningDurationSeconds);
            Positive(errors, "burningDamagePerSecond", burningDamagePerSecond);
            if (!Finite(burningLeavesGrowthMultiplier) || burningLeavesGrowthMultiplier <= 0 || burningLeavesGrowthMultiplier > 1)
                errors.Add("burningLeavesGrowthMultiplier must be between 0 and 1.");
            Positive(errors, "autosaveIntervalSeconds", autosaveIntervalSeconds);
            Positive(errors, "rootWaterPerLevelSecond", rootWaterPerLevelSecond);
            Positive(errors, "leavesPhotosynthesisPerLevelSecond", leavesPhotosynthesisPerLevelSecond);
            Positive(errors, "trunkGrowthPerLevelSecond", trunkGrowthPerLevelSecond);
            Positive(errors, "trunkPhotosynthesisPerLevelSecond", trunkPhotosynthesisPerLevelSecond);
            Positive(errors, "leavesWaterPerLevelSecond", leavesWaterPerLevelSecond);
            NonNegative(errors, "trunkProtectionPerLevel", trunkProtectionPerLevel);
            Positive(errors, "rootBaseCost", rootBaseCost);
            Positive(errors, "leavesBaseCost", leavesBaseCost);
            Positive(errors, "trunkBaseCost", trunkBaseCost);
            Positive(errors, "spiritPerLevelSecond", spiritPerLevelSecond);
            Positive(errors, "spiritAbsorptionBaseCost", spiritAbsorptionBaseCost);
            Positive(errors, "spiritUnlockWaterCost", spiritUnlockWaterCost);
            Positive(errors, "spiritUnlockPhotosynthesisCost", spiritUnlockPhotosynthesisCost);
            Positive(errors, "spiritUnlockGrowthCost", spiritUnlockGrowthCost);
            if (!Finite(upgradeCostFactor) || upgradeCostFactor <= 1) errors.Add("upgradeCostFactor must exceed 1.");
            if (offlineMaxSeconds != 86400 || offlineRateFactor != 0.5) errors.Add("Confirmed offline rule is 24h at 50%.");
            if (vitalityMaxBankSeconds != 3600 || Math.Abs(vitalityOutputMultiplier - 1.05) > 1e-9 || !Finite(vitalityOutputMultiplier))
                errors.Add("Confirmed vitality rule is a 3600s bank and a 5% output bonus.");
            if (tapsPerReward <= 0) errors.Add("tapsPerReward must be positive.");
            Positive(errors, "secondsPerTapReward", secondsPerTapReward);
            if(rootCostResourceId!="water" || leavesCostResourceId!="photosynthesis" || trunkCostResourceId!="growth")
                errors.Add("Root/Leaves/Trunk must use water/photosynthesis/growth.");
            if(initialWater<rootBaseCost || initialPhotosynthesis<leavesBaseCost || initialGrowth<trunkBaseCost)
                errors.Add("Starting grants must fund the first level of each default branch.");
            if (stages == null || stages.Length == 0)
            {
                errors.Add("At least one stage is required.");
                return errors;
            }
            var ids = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < stages.Length; i++)
            {
                var s = stages[i];
                if (s == null) { errors.Add("Stage entry is null."); continue; }
                if (string.IsNullOrWhiteSpace(s.stageId) || !ids.Add(s.stageId)) errors.Add("Stage IDs must be nonempty and unique.");
                if (string.IsNullOrWhiteSpace(s.displayName)) errors.Add("Stage displayName is required.");
                if (s.rootCap < 0 || s.leavesCap < 0 || s.trunkCap < 0) errors.Add("Stage caps cannot be negative.");
                NonNegative(errors, "evolutionWaterCost", s.evolutionWaterCost);
                NonNegative(errors, "evolutionPhotosynthesisCost", s.evolutionPhotosynthesisCost);
                NonNegative(errors, "evolutionGrowthCost", s.evolutionGrowthCost);
                if (s.spiritAbsorptionCap < 0) errors.Add("spiritAbsorptionCap cannot be negative.");
                NonNegative(errors, "exposure", s.exposure);
                string expectedNext = i + 1 < stages.Length && stages[i + 1] != null ? stages[i + 1].stageId : "";
                if ((s.nextStageId ?? "") != expectedNext) errors.Add("nextStageId must follow the configured ordered stage chain.");
                if (i > 0 && stages[i - 1] != null)
                {
                    var previous = stages[i - 1];
                    if (s.rootCap < previous.rootCap || s.leavesCap < previous.leavesCap || s.trunkCap < previous.trunkCap || s.spiritAbsorptionCap<previous.spiritAbsorptionCap)
                        errors.Add("Caps must not shrink while Evolution retains levels.");
                }
            }
            var effectIds = new HashSet<string>(StringComparer.Ordinal);
            if (pointEffects == null) errors.Add("pointEffects required.");
            else foreach (var effect in pointEffects)
            {
                if (effect == null || FindStage(effect.stageId) == null || !effectIds.Add(effect.stageId) ||
                    (effect.targetId != "water" && effect.targetId != "growth" && effect.targetId != "photosynthesis" && effect.targetId != "spirit" && effect.targetId != "protection")) errors.Add("Invalid/duplicate point effect ID.");
                else NonNegative(errors, "perPoint", effect.perPoint);
            }
            if(effectIds.Count != ids.Count)errors.Add("Each stage requires one point effect.");
            if(stages[0]!=null && (stages[0].rootCap<1||stages[0].leavesCap<1||stages[0].trunkCap<1))errors.Add("Default producers must be available at the initial stage.");
            if (stages[0] == null || initialStageId != stages[0].stageId) errors.Add("initialStageId must identify the first configured stage.");

            if (achievements == null || achievements.Length == 0)
            {
                errors.Add("achievements list is required.");
            }
            else
            {
                var achIds = new HashSet<string>(StringComparer.Ordinal);
                foreach (var ach in achievements)
                {
                    if (ach == null) { errors.Add("Achievement entry is null."); continue; }
                    if (string.IsNullOrWhiteSpace(ach.achievementId) || !achIds.Add(ach.achievementId))
                        errors.Add("Achievement IDs must be nonempty and unique.");
                    if (string.IsNullOrWhiteSpace(ach.displayName))
                        errors.Add("Achievement displayName is required.");
                    if (ach.targetProgress <= 0)
                        errors.Add("Achievement targetProgress must be positive.");
                    if (ach.coinReward <= 0)
                        errors.Add("Achievement coinReward must be positive.");
                }
            }

            if (cosmetics == null || cosmetics.Length == 0)
            {
                errors.Add("cosmetics list is required.");
            }
            else
            {
                var cosIds = new HashSet<string>(StringComparer.Ordinal);
                foreach (var cos in cosmetics)
                {
                    if (cos == null) { errors.Add("Cosmetic entry is null."); continue; }
                    if (string.IsNullOrWhiteSpace(cos.cosmeticId) || !cosIds.Add(cos.cosmeticId))
                        errors.Add("Cosmetic IDs must be nonempty and unique.");
                    if (string.IsNullOrWhiteSpace(cos.displayName))
                        errors.Add("Cosmetic displayName is required.");
                    if (cos.price <= 0)
                        errors.Add("Cosmetic price must be positive.");
                }
            }

            return errors;
        }

        private static bool Finite(double value) { return !double.IsNaN(value) && !double.IsInfinity(value); }
        private static void NonNegative(List<string> errors, string name, double value)
        { if (!Finite(value) || value < 0) errors.Add(name + " must be finite and nonnegative."); }
        private static void Positive(List<string> errors, string name, double value)
        { if (!Finite(value) || value <= 0) errors.Add(name + " must be finite and positive."); }

        private static StageDefinition[] DefaultStages()
        {
            return new[]
            {
                new StageDefinition { stageId = "seedling", displayName = "Mầm", nextStageId = "young_tree", rootCap = 5, leavesCap = 5, trunkCap = 5 },
                new StageDefinition { stageId = "young_tree", displayName = "Cây Non", nextStageId = "large_tree", rootCap = 10, leavesCap = 10, trunkCap = 5, evolutionWaterCost = 50, evolutionPhotosynthesisCost = 25, evolutionGrowthCost = 25, rebirthUnlocked = true, weatherUnlocked = true },
                new StageDefinition { stageId = "large_tree", displayName = "Cây Lớn", nextStageId = "mature_tree", rootCap = 20, leavesCap = 20, trunkCap = 10, evolutionWaterCost = 200, evolutionPhotosynthesisCost = 100, evolutionGrowthCost = 100, exposure = 1, rebirthUnlocked = true, weatherUnlocked = true, harmfulWeatherUnlocked = true },
                new StageDefinition { stageId="mature_tree", displayName="Cây Trưởng Thành", nextStageId="", rootCap=30, leavesCap=30, trunkCap=20, spiritAbsorptionCap=10, evolutionWaterCost=500, evolutionPhotosynthesisCost=250, evolutionGrowthCost=250, exposure=1.2, rebirthUnlocked=true, weatherUnlocked=true, harmfulWeatherUnlocked=true }
            };
        }

        private static AchievementDefinition[] DefaultAchievements()
        {
            return new[]
            {
                new AchievementDefinition
                {
                    achievementId = "first_root",
                    displayName = "Nâng cấp Rễ lần đầu",
                    description = "Nâng cấp nhánh Rễ lên cấp 1",
                    targetProgress = 1,
                    coinReward = 10
                },
                new AchievementDefinition
                {
                    achievementId = "first_leaves",
                    displayName = "Nâng cấp Lá lần đầu",
                    description = "Nâng cấp nhánh Lá lên cấp 1",
                    targetProgress = 1,
                    coinReward = 10
                },
                new AchievementDefinition
                {
                    achievementId = "evolve_young_tree",
                    displayName = "Tiến hóa Cây Non",
                    description = "Tiến hóa cây lên giai đoạn Cây Non",
                    targetProgress = 1,
                    coinReward = 10
                },
                new AchievementDefinition
                {
                    achievementId = "evolve_large_tree",
                    displayName = "Tiến hóa Cây Lớn",
                    description = "Tiến hóa cây lên giai đoạn Cây Lớn",
                    targetProgress = 1,
                    coinReward = 10
                },
                new AchievementDefinition
                {
                    achievementId = "first_rebirth",
                    displayName = "Rebirth lần đầu",
                    description = "Thực hiện Rebirth lần đầu tiên để nhận điểm vĩnh viễn",
                    targetProgress = 1,
                    coinReward = 10
                },
                new AchievementDefinition
                {
                    achievementId = "first_vitality",
                    displayName = "Nhận thưởng Sức sống",
                    description = "Hoàn thành chuỗi 50 tap để nhận thưởng thời gian Sức sống lần đầu",
                    targetProgress = 1,
                    coinReward = 10
                }
            };
        }

        private static CosmeticDefinition[] DefaultCosmetics()
        {
            return new[]
            {
                new CosmeticDefinition
                {
                    cosmeticId = "glow_placeholder",
                    displayName = "Hào Quang Sinh Mệnh",
                    description = "Hiệu ứng hào quang ngọc bích phát sáng quanh cây",
                    price = 20
                }
            };
        }
    }
}
