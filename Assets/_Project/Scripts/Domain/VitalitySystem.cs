using System;

namespace CayTheGioi.Domain
{
    public readonly struct CommandResult
    {
        public readonly bool Success;
        public readonly string Reason;
        public CommandResult(bool success, string reason = "") { Success = success; Reason = reason; }
    }

    public sealed class VitalitySystem
    {
        private readonly GameState state;
        private readonly PrototypeRules rules;
        private readonly Func<bool> blocked;
        public event Action Changed;
        public event Action RewardEarned;
        public event Action Tapped;
        public VitalitySystem(GameState state, PrototypeRules rules, Func<bool> blocked = null) { this.state = state; this.rules = rules; this.blocked = blocked; }
        public CommandResult TryTap(bool inputAllowed = true)
        {
            if (state.run.life != "ALIVE") return new CommandResult(false, "DEAD");
            if (!inputAllowed || (blocked != null && blocked())) return new CommandResult(false, "INPUT_BLOCKED");
            if (state.vitality.tapProgress < 0 || state.vitality.tapProgress >= rules.tapsPerReward ||
                !ProductionSystem.Finite(state.vitality.bankSeconds) || state.vitality.bankSeconds < 0 || state.vitality.bankSeconds > rules.vitalityMaxBankSeconds)
                return new CommandResult(false, "INVALID_STATE");
            int counter = state.vitality.tapProgress + 1;
            double bank = state.vitality.bankSeconds;
            bool earned = false;
            if (counter == rules.tapsPerReward)
            {
                counter = 0;
                bank += Math.Min(rules.secondsPerTapReward, rules.vitalityMaxBankSeconds - bank);
                earned = true;
            }
            state.vitality.tapProgress = counter;
            state.vitality.bankSeconds = bank;
            Tapped?.Invoke();
            Changed?.Invoke();
            if (earned) RewardEarned?.Invoke();
            return new CommandResult(true, bank >= rules.vitalityMaxBankSeconds ? "BANK_FULL" : "");
        }
        internal void NotifyChanged() => Changed?.Invoke();
    }
}
