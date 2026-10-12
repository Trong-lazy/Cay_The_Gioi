using System;

namespace CayTheGioi.Domain
{
    public sealed class HealthStatusSystem
    {
        private readonly GameState state;
        private readonly PrototypeRules rules;

        public event Action HealthChanged;
        public event Action RunDied;
        public event Action StatusChanged;

        public double Hp => state.run.hp;
        public double MaxHp => rules.maxHp;
        public string StatusId => state.run.status.statusId;
        public double StatusRemainingSeconds => state.run.status.remainingOnlineSeconds;
        public string ImpactRegionId => state.run.status.impactRegionId;
        public bool IsBurning => state.run.status.statusId == "burning" && state.run.status.remainingOnlineSeconds > 0;

        public HealthStatusSystem(GameState state, PrototypeRules rules)
        {
            this.state = state ?? throw new ArgumentNullException(nameof(state));
            this.rules = rules ?? throw new ArgumentNullException(nameof(rules));
        }

        public double SecondsToBoundary
        {
            get
            {
                if (!IsBurning) return double.PositiveInfinity;
                double protection = state.run.levels.trunk * rules.trunkProtectionPerLevel +
                    PermanentEffects.Evaluate(state.permanent, rules).Protection;
                double dps = rules.CalculateBurningDamagePerSecond(protection);
                return Math.Min(state.run.status.remainingOnlineSeconds,
                    dps > 0 ? state.run.hp / dps : double.PositiveInfinity);
            }
        }

        public void Tick(double seconds, out double aliveSeconds)
        {
            if (!ProductionSystem.Finite(seconds) || seconds <= 0)
            {
                aliveSeconds = 0;
                return;
            }

            if (state.run.life != "ALIVE")
            {
                aliveSeconds = 0;
                return;
            }

            var st = state.run.status;
            if (st.statusId == "burning" && st.remainingOnlineSeconds > 0)
            {
                double effectiveSeconds = seconds;

                double burnTime = Math.Min(effectiveSeconds, st.remainingOnlineSeconds);
                double totalProtection = state.run.levels.trunk * rules.trunkProtectionPerLevel +
                    PermanentEffects.Evaluate(state.permanent, rules).Protection;
                double dps = rules.CalculateBurningDamagePerSecond(totalProtection);
                double damage = burnTime * dps;

                if (damage >= state.run.hp)
                {
                    // Tree died during this tick
                    aliveSeconds = dps > 0 ? (state.run.hp / dps) : seconds;
                    state.run.hp = 0;
                    state.run.life = "DEAD";
                    st.remainingOnlineSeconds = Math.Max(0, st.remainingOnlineSeconds - aliveSeconds);

                    HealthChanged?.Invoke();
                    RunDied?.Invoke();
                    return;
                }

                // Still alive
                state.run.hp -= damage;
                st.remainingOnlineSeconds -= effectiveSeconds;
                if (st.remainingOnlineSeconds <= 0)
                {
                    st.statusId = "none";
                    st.remainingOnlineSeconds = 0;
                    st.impactRegionId = "none";
                    StatusChanged?.Invoke();
                }

                aliveSeconds = seconds;
                HealthChanged?.Invoke();
                return;
            }

            aliveSeconds = seconds;
        }

        public void SetHp(double value)
        {
            if (!ProductionSystem.Finite(value)) return;
            double clamped = Math.Max(0, Math.Min(rules.maxHp, value));
            bool wasAlive = state.run.life == "ALIVE";
            state.run.hp = clamped;

            if (clamped <= 0 && wasAlive)
            {
                state.run.life = "DEAD";
                HealthChanged?.Invoke();
                RunDied?.Invoke();
            }
            else
            {
                HealthChanged?.Invoke();
            }
        }

        public void Kill() => SetHp(0);

        public void ResetForRebirth()
        {
            state.run.hp = rules.maxHp;
            state.run.status.statusId = "none";
            state.run.status.remainingOnlineSeconds = 0;
            state.run.status.impactRegionId = "none";
            HealthChanged?.Invoke();
            StatusChanged?.Invoke();
        }
    }
}
