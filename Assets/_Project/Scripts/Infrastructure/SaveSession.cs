using System;
using CayTheGioi.Domain;

namespace CayTheGioi.Infrastructure
{
    public sealed class SaveSession
    {
        private readonly PrototypeRules rules;
        public SaveService Service { get; }
        public OfflineReward LastOfflineReward { get; private set; }
        public OfflineCoordinator Offline { get; }
        public GameState State { get; private set; }
        public string SaveId { get; private set; }
        public long Revision { get; private set; }
        public SaveService.LoadOutcome Outcome { get; private set; }
        public SaveSession(PrototypeRules rules)
        { this.rules = rules; Service = new SaveService(rules); Offline = new OfflineCoordinator(rules, Service); }

        public SaveResult Load(bool confirmRecovery = false)
        {
            LastOfflineReward = null;
            var loaded = Service.Load(); Outcome = loaded.outcome;
            if (!loaded.result.Success) return loaded.result;
            if (Outcome == SaveService.LoadOutcome.LoadedFromBackup && !confirmRecovery)
                return SaveResult.Fail("RECOVERY_CONFIRMATION_REQUIRED");
            var candidate = GameStateFactory.CreateNew(rules);
            var dto = loaded.result.Data;
            string id = dto == null ? Guid.NewGuid().ToString("N") : dto.saveId;
            long revision = dto == null ? 0 : dto.revision;
            if (dto != null) SaveSerializer.Restore(dto, candidate);
            if (Outcome == SaveService.LoadOutcome.LoadedFromBackup)
            {
                Service.AcceptRecovery();
                var committed = Service.Save(candidate, id, ref revision);
                if (!committed.Success) return committed;
            }
            else if (dto != null)
            {
                var committed = Offline.TryApply(candidate, id, ref revision, dto.offlineCheckpointUtcSeconds, out var reward);
                if (!committed.Success) return committed;
                LastOfflineReward = reward;
            }
            State = candidate; SaveId = id; Revision = revision;
            return SaveResult.Ok(dto);
        }
    }
}
