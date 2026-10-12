using System;
using CayTheGioi.Domain;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace CayTheGioi.Infrastructure
{
    // Uses the Newtonsoft assembly already present in this project; no new package.
    public static class SaveCodec
    {
        public static string Encode(SaveData data) => JsonConvert.SerializeObject(data);

        public static SaveResult Decode(string json, PrototypeRules rules)
        {
            try
            {
                var root = JObject.Parse(json, new JsonLoadSettings { DuplicatePropertyNameHandling = DuplicatePropertyNameHandling.Error });
                int schema = root.Value<int?>("schemaVersion") ?? 0;
                if (schema > SaveSerializer.CurrentSchemaVersion) return SaveResult.Fail("VERSION_TOO_NEW");
                if (schema != SaveSerializer.CurrentSchemaVersion) return SaveResult.Fail("UNSUPPORTED_SCHEMA");
                foreach (string field in new[] { "configVersion", "saveId", "revision", "savedAtUtcSeconds", "offlineCheckpointUtcSeconds", "run" })
                    if (root[field] == null || root[field].Type == JTokenType.Null) return SaveResult.Fail("MISSING_FIELD: " + field);
                string version = root.Value<string>("configVersion");
                bool compatible = version == rules.configVersion ||
                    (rules.configVersion=="prototype-6" && (version=="prototype-1"||version=="prototype-2"||version=="prototype-3"||version=="prototype-4"||version=="prototype-5")) ||
                    (rules.configVersion == "prototype-5" && (version == "prototype-1" || version == "prototype-2" || version == "prototype-3" || version == "prototype-4")) ||
                    (rules.configVersion == "prototype-4" && (version == "prototype-1" || version == "prototype-2" || version == "prototype-3")) ||
                    (rules.configVersion == "prototype-3" && (version == "prototype-1" || version == "prototype-2")) ||
                    (rules.configVersion == "prototype-2" && version == "prototype-1");
                if (!compatible) return SaveResult.Fail("CONFIG_INCOMPATIBLE: " + version);
                var run = root["run"] as JObject;
                if (run == null) return SaveResult.Fail("INVALID_RUN");
                foreach (string field in new[] { "runId", "life", "stageId", "water", "growth", "hp", "levels" })
                    if (run[field] == null || run[field].Type == JTokenType.Null) return SaveResult.Fail("MISSING_RUN_FIELD: " + field);
                var dto = root.ToObject<SaveData>();
                // Missing optional v1 fields use constructor defaults; explicit null is rejected.
                var defaults = GameStateFactory.CreateNew(rules);
                if (root["permanent"] == null) dto.permanent = defaults.permanent;
                if (root["vitality"] == null) dto.vitality = defaults.vitality;
                if (root["settings"] == null) dto.settings = defaults.settings;
                if (dto.permanent != null && root["permanent"] is JObject && root["permanent"]["stagePoints"] == null)
                    dto.permanent.stagePoints = defaults.permanent.stagePoints;
                if(rules.configVersion=="prototype-6" && version!="prototype-6")
                {
                    // Legacy Leaves produced Growth. Keep that balance; grant only the new
                    // currency and an affordable initial Trunk if its producer is absent.
                    if(!ProductionSystem.Finite(dto.run.growth)||dto.run.growth<0)return SaveResult.Fail("INVALID_LEGACY_GROWTH");
                    dto.run.photosynthesis=rules.initialPhotosynthesis;
                    if(dto.run.levels!=null && dto.run.levels.trunk==0)
                        dto.run.growth=Math.Max(dto.run.growth,rules.trunkBaseCost);
                    dto.run.spirit=0; dto.run.spiritAbsorptionUnlocked=false;
                    if(dto.run.levels!=null)dto.run.levels.spiritAbsorption=0;
                }
                else if(rules.configVersion=="prototype-6")
                {
                    foreach(string field in new[]{"photosynthesis","spirit","spiritAbsorptionUnlocked"})
                        if(run[field]==null||run[field].Type==JTokenType.Null)return SaveResult.Fail("MISSING_RUN_FIELD: "+field);
                    if(run["levels"]?["spiritAbsorption"]==null)return SaveResult.Fail("MISSING_RUN_FIELD: spiritAbsorption");
                }
                var errors = SaveValidator.Validate(dto, rules);
                return errors.Count == 0 ? SaveResult.Ok(dto) : SaveResult.Fail(string.Join("; ", errors));
            }
            catch (Exception ex) { return SaveResult.Fail("INVALID_JSON: " + ex.Message); }
        }
    }
}
