using System;
using System.Collections.Generic;

namespace CayTheGioi.Domain
{
    // Serializable value data only. Systems and Unity view references never belong here.
    [Serializable]
    public sealed class GameState
    {
        public RunState run = new RunState();
        public PermanentState permanent = new PermanentState();
        public VitalityState vitality = new VitalityState();
        public SettingsState settings = new SettingsState();
    }

    [Serializable]
    public sealed class RunState
    {
        public string runId;
        public string life = "ALIVE";
        public string stageId = "seedling";
        public double water;
        public double photosynthesis;
        public double growth;
        public double spirit;
        public bool spiritAbsorptionUnlocked;
        public LevelState levels = new LevelState();
        public double hp;
        public WeatherState weather = new WeatherState();
        public StatusState status = new StatusState();
        public List<CounterEntry> runCounters = new List<CounterEntry>();
        public double Balance(string id)
        {
            switch(id) { case "water":return water; case "photosynthesis":return photosynthesis; case "growth":return growth; case "spirit":return spirit; default:return double.NaN; }
        }
        internal void SetBalance(string id,double value)
        {
            switch(id) { case "water":water=value;break; case "photosynthesis":photosynthesis=value;break; case "growth":growth=value;break; case "spirit":spirit=value;break; default:throw new ArgumentException("Unknown resource ID."); }
        }

    }

    [Serializable]
    public sealed class LevelState
    {
        public int root;
        public int leaves;
        public int trunk;
        public int spiritAbsorption;
    }

    [Serializable]
    public sealed class WeatherState
    {
        public string weatherId = "calm";
        public double remainingOnlineSeconds = 120;
        public int badStreak;
        public List<string> recentWeatherIds = new List<string>();
        public double incidentCooldownOnlineSeconds;
        public bool strikeEvaluated;
        public bool strikeWillHit;
        public double strikeDelaySeconds;
        public bool strikeExecuted;
    }

    [Serializable]
    public sealed class StatusState
    {
        public string statusId = "none";
        public double remainingOnlineSeconds;
        public string impactRegionId = "none";
    }

    [Serializable]
    public sealed class PermanentState
    {
        public List<StagePointEntry> stagePoints = new List<StagePointEntry>();
        public long coin;
        public List<AchievementEntry> achievements = new List<AchievementEntry>();
        public List<string> ownedCosmeticIds = new List<string>();
        public string equippedCosmeticId = "none";
        public List<string> tutorialCompletedStepIds = new List<string>();
        public long rebirthCount;
        public string highestStageEverId = "seedling";
        public List<CounterEntry> permanentCounters = new List<CounterEntry>();
    }

    [Serializable] public sealed class StagePointEntry { public string stageId; public long points; }
    [Serializable] public sealed class AchievementEntry { public string achievementId; public long progress; public bool claimed; }
    [Serializable] public sealed class CounterEntry { public string counterId; public long value; }
    [Serializable] public sealed class VitalityState { public int tapProgress; public double bankSeconds; }
    [Serializable] public sealed class SettingsState { public bool musicEnabled = true; public bool orbitLocked; }
}
