using CayTheGioi.Bootstrap;
using CayTheGioi.Domain;
using UnityEditor;
using UnityEngine;

namespace CayTheGioi.Editor
{
    public static class WeatherDebug
    {
        private const string Menu = "Cây Thế Giới/T07 Weather/Debug/";

        private static GameBootstrapper Bootstrap()
        {
            var b = Object.FindAnyObjectByType<GameBootstrapper>();
            if (!EditorApplication.isPlaying || b == null || !b.IsInitialized)
                throw new System.InvalidOperationException("Enter MainScene Play first.");
            return b;
        }

        [MenuItem(Menu + "Force Calm")]
        public static void ForceCalm()
        {
            var b = Bootstrap();
            b.Weather.ForceWeather("calm");
            Debug.Log("[T07 Debug] Weather forced to calm.");
        }
        [MenuItem(Menu + "Force Calm", validate = true)]
        public static bool ForceCalmValidate() => EditorApplication.isPlaying;

        [MenuItem(Menu + "Force Sunny (+20% Growth)")]
        public static void ForceSunny()
        {
            var b = Bootstrap();
            b.Weather.ForceWeather("sunny", 300);
            Debug.Log("[T07 Debug] Weather forced to sunny (300s).");
        }
        [MenuItem(Menu + "Force Sunny (+20% Growth)", validate = true)]
        public static bool ForceSunnyValidate() => EditorApplication.isPlaying;

        [MenuItem(Menu + "Force Rainy (+20% Water)")]
        public static void ForceRainy()
        {
            var b = Bootstrap();
            b.Weather.ForceWeather("rainy", 300);
            Debug.Log("[T07 Debug] Weather forced to rainy (300s).");
        }
        [MenuItem(Menu + "Force Rainy (+20% Water)", validate = true)]
        public static bool ForceRainyValidate() => EditorApplication.isPlaying;

        [MenuItem(Menu + "Force Thunderstorm (Risk)")]
        public static void ForceThunderstorm()
        {
            var b = Bootstrap();
            b.Weather.ForceWeather("thunderstorm", 300);
            Debug.Log("[T07 Debug] Weather forced to thunderstorm (300s).");
        }
        [MenuItem(Menu + "Force Thunderstorm (Risk)", validate = true)]
        public static bool ForceThunderstormValidate() => EditorApplication.isPlaying;

        [MenuItem(Menu + "Strike Leaves (Burning -30% Growth)")]
        public static void StrikeLeaves()
        {
            var b = Bootstrap();
            b.Weather.TriggerStrike("leaves");
            Debug.Log("[T07 Debug] Lightning strike on Leaves. Burning started!");
        }
        [MenuItem(Menu + "Strike Leaves (Burning -30% Growth)", validate = true)]
        public static bool StrikeLeavesValidate() => EditorApplication.isPlaying;

        [MenuItem(Menu + "Strike Root (Burning)")]
        public static void StrikeRoot()
        {
            var b = Bootstrap();
            b.Weather.TriggerStrike("root");
            Debug.Log("[T07 Debug] Lightning strike on Root. Burning started!");
        }
        [MenuItem(Menu + "Strike Root (Burning)", validate = true)]
        public static bool StrikeRootValidate() => EditorApplication.isPlaying;

        [MenuItem(Menu + "Strike Trunk (Burning)")]
        public static void StrikeTrunk()
        {
            var b = Bootstrap();
            b.Weather.TriggerStrike("trunk");
            Debug.Log("[T07 Debug] Lightning strike on Trunk. Burning started!");
        }
        [MenuItem(Menu + "Strike Trunk (Burning)", validate = true)]
        public static bool StrikeTrunkValidate() => EditorApplication.isPlaying;

        [MenuItem(Menu + "Set HP to 10")]
        public static void SetHpTen()
        {
            var b = Bootstrap();
            b.Health.SetHp(10);
            Debug.Log("[T07 Debug] HP set to 10.");
        }
        [MenuItem(Menu + "Set HP to 10", validate = true)]
        public static bool SetHpTenValidate() => EditorApplication.isPlaying;

        [MenuItem(Menu + "Kill Tree (HP = 0 -> Forced Rebirth)")]
        public static void KillTree()
        {
            var b = Bootstrap();
            b.Health.Kill();
            Debug.Log("[T07 Debug] Tree killed. HP = 0, DEAD modal should appear.");
        }
        [MenuItem(Menu + "Kill Tree (HP = 0 -> Forced Rebirth)", validate = true)]
        public static bool KillTreeValidate() => EditorApplication.isPlaying;

        [MenuItem(Menu + "Clear Burning & Status")]
        public static void ClearBurning()
        {
            var b = Bootstrap();
            b.Weather.ClearStatus();
            Debug.Log("[T07 Debug] Burning and status cleared.");
        }
        [MenuItem(Menu + "Clear Burning & Status", validate = true)]
        public static bool ClearBurningValidate() => EditorApplication.isPlaying;

        [MenuItem(Menu + "Log Weather & Health Info")]
        public static void LogInfo()
        {
            var b = Bootstrap();
            var w = b.State.run.weather;
            var st = b.State.run.status;
            Debug.Log(string.Format("[T07 Debug] Weather={0} rem={1:0.#}s badStreak={2} cooldown={3:0.#}s | HP={4:0.#}/{5} life={6} status={7} impact={8} remStatus={9:0.#}s",
                w.weatherId, w.remainingOnlineSeconds, w.badStreak, w.incidentCooldownOnlineSeconds,
                b.State.run.hp, b.Config.Rules.maxHp, b.State.run.life, st.statusId, st.impactRegionId, st.remainingOnlineSeconds));
        }
        [MenuItem(Menu + "Log Weather & Health Info", validate = true)]
        public static bool LogInfoValidate() => EditorApplication.isPlaying;
    }
}
