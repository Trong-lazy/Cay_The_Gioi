using System;

namespace CayTheGioi.Domain
{
    // Versioned DTO: T04 snapshot/file I/O, T05 offline handling.
    // Nested types are plain serializable values: a future snapshot must deep-copy them.
    [Serializable]
    public sealed class SaveData
    {
        public int schemaVersion = 1;
        public string configVersion;
        public string saveId;
        public long revision;
        public long savedAtUtcSeconds;
        public long offlineCheckpointUtcSeconds;
        public RunState run;
        public PermanentState permanent;
        public VitalityState vitality;
        public SettingsState settings;
    }
}
