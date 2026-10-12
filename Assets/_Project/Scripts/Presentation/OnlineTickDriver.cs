using CayTheGioi.Domain;
using UnityEngine;

namespace CayTheGioi.Presentation
{
    public sealed class OnlineTickDriver : MonoBehaviour
    {
        private ResourceSystem resources;
        private WeatherSystem weather;
        private HealthStatusSystem health;
        private bool skipNextTick;
        public void ResumeAfterCatchUp() { skipNextTick = true; }
        private bool paused, focused = true;
        public bool Suspended => paused || !focused;
        public bool ManualTick { get; set; }

        public void Bind(ResourceSystem resourceSystem, WeatherSystem weatherSystem = null, HealthStatusSystem healthSystem = null)
        {
            resources = resourceSystem;
            weather = weatherSystem;
            health = healthSystem;
        }

        private void Update()
        {
            if (resources == null || resources.SimulationBlocked || Suspended || ManualTick) return;
            if (skipNextTick) { skipNextTick = false; return; }
            double dt = Time.unscaledDeltaTime;
            TickManual(dt);
        }

        public void TickManual(double dt)
        {
            if (!ProductionSystem.Finite(dt) || dt <= 0 || (resources != null && resources.SimulationBlocked)) return;
            if (weather != null)
            {
                weather.Tick(dt, AdvanceSegment, () => health != null ? health.SecondsToBoundary : double.PositiveInfinity);
            }
            else AdvanceSegment(health != null ? System.Math.Min(dt, health.SecondsToBoundary) : dt);
        }

        private void AdvanceSegment(double seconds)
        {
            // Commit production while the run is still ALIVE, up to the exact death/status boundary.
            if (resources != null)
            {
                var result = resources.Tick(seconds);
                if (!result.Success && result.Reason != "DEAD")
                { Debug.LogError("[CayTheGioi] Tick rejected: " + result.Reason, this); enabled = false; }
            }
            if (health != null) health.Tick(seconds, out _);
        }

        private void OnApplicationPause(bool value) { paused = value; }
        private void OnApplicationFocus(bool value) { focused = value; }
    }
}
