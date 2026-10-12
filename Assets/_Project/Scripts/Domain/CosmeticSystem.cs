using System;
using System.Collections.Generic;

namespace CayTheGioi.Domain
{
    public readonly struct CosmeticStatus
    {
        public readonly string CosmeticId;
        public readonly string DisplayName;
        public readonly string Description;
        public readonly long Price;
        public readonly bool IsOwned;
        public readonly bool IsEquipped;
        public readonly bool CanBuy;

        public CosmeticStatus(string id, string name, string desc, long price, bool owned, bool equipped, bool canBuy)
        {
            CosmeticId = id;
            DisplayName = name;
            Description = desc;
            Price = price;
            IsOwned = owned;
            IsEquipped = equipped;
            CanBuy = canBuy;
        }
    }

    public sealed class CosmeticSystem
    {
        private readonly GameState state;
        private readonly PrototypeRules rules;
        private readonly Func<CommandResult> saveFunc;
        private bool isTransacting;

        public event Action<string> Purchased;
        public event Action<string> Equipped;
        public event Action<string> Unequipped;
        public event Action StateChanged;

        public CosmeticSystem(GameState state, PrototypeRules rules, Func<CommandResult> saveFunc = null)
        {
            this.state = state ?? throw new ArgumentNullException(nameof(state));
            this.rules = rules ?? throw new ArgumentNullException(nameof(rules));
            this.saveFunc = saveFunc;

            if (this.state.permanent != null)
            {
                if (this.state.permanent.ownedCosmeticIds == null)
                    this.state.permanent.ownedCosmeticIds = new List<string>();
                if (string.IsNullOrWhiteSpace(this.state.permanent.equippedCosmeticId))
                    this.state.permanent.equippedCosmeticId = "none";
            }
        }

        public string EquippedCosmeticId => state?.permanent?.equippedCosmeticId ?? "none";

        public bool IsOwned(string cosmeticId)
        {
            if (string.IsNullOrWhiteSpace(cosmeticId)) return false;
            return state?.permanent?.ownedCosmeticIds != null && state.permanent.ownedCosmeticIds.Contains(cosmeticId);
        }

        public bool IsEquipped(string cosmeticId)
        {
            if (string.IsNullOrWhiteSpace(cosmeticId)) return false;
            return string.Equals(EquippedCosmeticId, cosmeticId, StringComparison.Ordinal);
        }

        public CommandResult TryBuy(string cosmeticId)
        {
            if (isTransacting) return new CommandResult(false, "BUSY");
            if (string.IsNullOrWhiteSpace(cosmeticId)) return new CommandResult(false, "INVALID_ID");

            var def = rules.FindCosmetic(cosmeticId);
            if (def == null) return new CommandResult(false, "UNKNOWN_COSMETIC");

            if (state.permanent.ownedCosmeticIds == null)
                state.permanent.ownedCosmeticIds = new List<string>();

            if (state.permanent.ownedCosmeticIds.Contains(cosmeticId))
                return new CommandResult(false, "ALREADY_OWNED");

            if (state.permanent.coin < def.price)
                return new CommandResult(false, "INSUFFICIENT_COIN");

            isTransacting = true;
            try
            {
                state.permanent.coin -= def.price;
                state.permanent.ownedCosmeticIds.Add(cosmeticId);

                if (saveFunc != null)
                {
                    var res = saveFunc();
                    if (!res.Success)
                    {
                        // Rollback on failure to prevent coin loss and duplicate bug on retry
                        state.permanent.coin += def.price;
                        state.permanent.ownedCosmeticIds.Remove(cosmeticId);
                        return new CommandResult(false, "SAVE_FAILED: " + res.Reason);
                    }
                }

                Purchased?.Invoke(cosmeticId);
                StateChanged?.Invoke();
                return new CommandResult(true);
            }
            finally
            {
                isTransacting = false;
            }
        }

        public CommandResult TryEquip(string cosmeticId)
        {
            if (isTransacting) return new CommandResult(false, "BUSY");
            if (string.IsNullOrWhiteSpace(cosmeticId) || cosmeticId == "none")
                return TryUnequip();

            var def = rules.FindCosmetic(cosmeticId);
            if (def == null) return new CommandResult(false, "UNKNOWN_COSMETIC");

            if (!IsOwned(cosmeticId))
                return new CommandResult(false, "NOT_OWNED");

            if (IsEquipped(cosmeticId))
                return new CommandResult(true); // Idempotent

            isTransacting = true;
            string prev = state.permanent.equippedCosmeticId;
            try
            {
                state.permanent.equippedCosmeticId = cosmeticId;

                if (saveFunc != null)
                {
                    var res = saveFunc();
                    if (!res.Success)
                    {
                        state.permanent.equippedCosmeticId = prev; // Rollback
                        return new CommandResult(false, "SAVE_FAILED: " + res.Reason);
                    }
                }

                Equipped?.Invoke(cosmeticId);
                StateChanged?.Invoke();
                return new CommandResult(true);
            }
            finally
            {
                isTransacting = false;
            }
        }

        public CommandResult TryUnequip()
        {
            if (isTransacting) return new CommandResult(false, "BUSY");
            if (state.permanent.equippedCosmeticId == "none")
                return new CommandResult(true);

            isTransacting = true;
            string prev = state.permanent.equippedCosmeticId;
            try
            {
                state.permanent.equippedCosmeticId = "none";

                if (saveFunc != null)
                {
                    var res = saveFunc();
                    if (!res.Success)
                    {
                        state.permanent.equippedCosmeticId = prev;
                        return new CommandResult(false, "SAVE_FAILED: " + res.Reason);
                    }
                }

                Unequipped?.Invoke(prev);
                StateChanged?.Invoke();
                return new CommandResult(true);
            }
            finally
            {
                isTransacting = false;
            }
        }

        public CosmeticStatus GetStatus(string cosmeticId)
        {
            var def = rules.FindCosmetic(cosmeticId);
            if (def == null) return default;

            bool owned = IsOwned(cosmeticId);
            bool equipped = IsEquipped(cosmeticId);
            bool canBuy = !owned && (state.permanent.coin >= def.price);

            return new CosmeticStatus(def.cosmeticId, def.displayName, def.description, def.price, owned, equipped, canBuy);
        }

        public List<CosmeticStatus> GetAllStatuses()
        {
            var list = new List<CosmeticStatus>();
            if (rules.cosmetics == null) return list;

            foreach (var def in rules.cosmetics)
            {
                if (def == null) continue;
                bool owned = IsOwned(def.cosmeticId);
                bool equipped = IsEquipped(def.cosmeticId);
                bool canBuy = !owned && (state.permanent.coin >= def.price);
                list.Add(new CosmeticStatus(def.cosmeticId, def.displayName, def.description, def.price, owned, equipped, canBuy));
            }
            return list;
        }
    }
}
