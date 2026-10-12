using System;
using System.IO;
using CayTheGioi.Domain;
using CayTheGioi.Infrastructure;
using NUnit.Framework;
using UnityEngine;
namespace CayTheGioi.Tests
{
    public sealed class RebirthTests
    {
        private PrototypeRules rules;private GameState state;private RebirthSystem rebirth;private SaveService service;
        private string directory;private long revision,now;private ResourceSystem resources;
        [SetUp]public void Setup()
        {
            rules=new PrototypeRules();state=GameStateFactory.CreateNew(rules);state.run.stageId="young_tree";
            state.run.water=777;state.run.growth=888;state.run.levels.root=3;state.run.levels.leaves=2;state.run.levels.trunk=1;
            state.run.hp=45;state.run.status.statusId="burning";state.run.status.remainingOnlineSeconds=8;state.run.status.impactRegionId="leaves";
            state.run.weather.remainingOnlineSeconds=73;state.run.weather.badStreak=1;state.run.weather.recentWeatherIds.Add("calm");state.run.weather.incidentCooldownOnlineSeconds=11;
            state.run.runCounters.Add(new CounterEntry{counterId="run.tap",value=8});state.vitality.bankSeconds=250;state.vitality.tapProgress=17;
            state.permanent.coin=31;state.permanent.highestStageEverId="large_tree";state.permanent.rebirthCount=7;
            state.permanent.ownedCosmeticIds.Add("glow_placeholder");state.permanent.equippedCosmeticId="glow_placeholder";
            state.permanent.achievements.Add(new AchievementEntry{achievementId="first.root",progress=1,claimed=true});
            state.permanent.tutorialCompletedStepIds.Add("tap");state.permanent.permanentCounters.Add(new CounterEntry{counterId="meta.tap",value=99});
            state.settings.musicEnabled=false;state.settings.orbitLocked=true;
            directory=Path.Combine(Application.temporaryCachePath,"T06_Edit_"+Guid.NewGuid().ToString("N"));service=new SaveService(rules,directory);now=2000000000;service.UtcNowSeconds=()=>now;revision=0;
            Assert.That(service.Save(state,"t06",ref revision).Success,Is.True);
            rebirth=new RebirthSystem(state,rules,SaveSerializer.Clone,(candidate,id)=>{var r=service.Save(candidate,"t06",ref revision);return new CommandResult(r.Success,r.Reason);});
            resources=new ResourceSystem(state,rules,new VitalitySystem(state,rules));
        }
        [TearDown]public void Cleanup(){if(Directory.Exists(directory))Directory.Delete(directory,true);}
        [Test]public void PreviewDoesNotMutateAndYoungTreeAwardsOnlyItsWalletResetRunKeepsEverythingElse()
        {
            string original=SaveCodec.Encode(SaveSerializer.Snapshot(state,rules.configVersion,"t06",revision));string runId=state.run.runId;
            var bank=state.vitality;var settings=state.settings;var quote=rebirth.GetQuote();Assert.That(quote.CanConfirm,Is.True);Assert.That(quote.StageId,Is.EqualTo("young_tree"));
            Assert.That(SaveCodec.Encode(SaveSerializer.Snapshot(state,rules.configVersion,"t06",revision)),Is.EqualTo(original));
            rebirth.Completed+=()=>{var dto=service.Load().result.Data;Assert.That(dto.run.runId,Is.EqualTo(state.run.runId));Assert.That(PermanentEffects.Points(dto.permanent,"young_tree"),Is.EqualTo(1));Assert.That(rebirth.TryRebirth(state.run.runId).Reason,Is.EqualTo("BUSY"));};
            Assert.That(rebirth.TryRebirth(runId).Success,Is.True);Assert.That(state.run.runId,Is.Not.EqualTo(runId));Assert.That(rebirth.TryRebirth(runId).Reason,Is.EqualTo("STALE_RUN"));
            Assert.That(PermanentEffects.Points(state.permanent,"young_tree"),Is.EqualTo(1));Assert.That(PermanentEffects.Points(state.permanent,"seedling"),Is.Zero);Assert.That(PermanentEffects.Points(state.permanent,"large_tree"),Is.Zero);
            Assert.That(state.run.stageId,Is.EqualTo("seedling"));Assert.That(state.run.water,Is.EqualTo(10));Assert.That(state.run.growth,Is.EqualTo(rules.initialGrowth));Assert.That(state.run.levels.root+state.run.levels.leaves+state.run.levels.trunk,Is.Zero);
            Assert.That(state.run.hp,Is.EqualTo(100));Assert.That(state.run.life,Is.EqualTo("ALIVE"));Assert.That(state.run.status.statusId,Is.EqualTo("none"));Assert.That(state.run.status.impactRegionId,Is.EqualTo("none"));Assert.That(state.run.status.remainingOnlineSeconds,Is.Zero);
            Assert.That(state.run.weather.weatherId,Is.EqualTo("calm"));Assert.That(state.run.weather.remainingOnlineSeconds,Is.EqualTo(120));Assert.That(state.run.weather.badStreak,Is.Zero);Assert.That(state.run.weather.incidentCooldownOnlineSeconds,Is.Zero);Assert.That(state.run.weather.recentWeatherIds,Is.Empty);Assert.That(state.run.runCounters,Is.Empty);
            Assert.That(state.vitality,Is.SameAs(bank));Assert.That(state.vitality.bankSeconds,Is.EqualTo(250));Assert.That(state.vitality.tapProgress,Is.EqualTo(17));Assert.That(state.settings,Is.SameAs(settings));Assert.That(settings.musicEnabled,Is.False);Assert.That(settings.orbitLocked,Is.True);
            Assert.That(state.permanent.coin,Is.EqualTo(31));Assert.That(state.permanent.rebirthCount,Is.EqualTo(8));Assert.That(state.permanent.highestStageEverId,Is.EqualTo("large_tree"));Assert.That(state.permanent.equippedCosmeticId,Is.EqualTo("glow_placeholder"));Assert.That(state.permanent.ownedCosmeticIds,Does.Contain("glow_placeholder"));Assert.That(state.permanent.achievements[0].claimed,Is.True);Assert.That(state.permanent.tutorialCompletedStepIds,Does.Contain("tap"));Assert.That(state.permanent.permanentCounters[0].value,Is.EqualTo(99));
        }
        [TestCase("seedling")] [TestCase("young_tree")] [TestCase("large_tree")]
        public void ForcedDeathAnyStageAwardsItsPointAndFreezesUntilConfirmation(string stage)
        {
            state.run.stageId=stage;if(stage=="seedling")state.run.levels.trunk=0;state.run.life="DEAD";state.run.hp=0;string id=state.run.runId;
            Assert.That(resources.Tick(3600).Reason,Is.EqualTo("DEAD"));Assert.That(new VitalitySystem(state,rules).TryTap().Reason,Is.EqualTo("DEAD"));Assert.That(new UpgradeSystem(state,rules,resources).TryUpgrade("root").Reason,Is.EqualTo("DEAD"));Assert.That(new EvolutionSystem(state,rules,resources).TryEvolve().Reason,Is.EqualTo("DEAD"));
            Assert.That(OfflineProduction.Evaluate(state,rules,now,now+86400).WaterDelta,Is.Zero);Assert.That(state.vitality.bankSeconds,Is.EqualTo(250));Assert.That(rebirth.GetQuote().Forced,Is.True);
            Assert.That(rebirth.TryRebirth(id).Success,Is.True);Assert.That(PermanentEffects.Points(state.permanent,stage),Is.EqualTo(1));
            state.run.levels.root=1;Assert.That(resources.Tick(1).Success,Is.True);Assert.That(state.vitality.bankSeconds,Is.EqualTo(249));
        }
        [TestCase(SaveWritePhase.AfterFlush)] [TestCase(SaveWritePhase.BeforeReplace)]
        public void FailedCommitKeepsSourceFileAndPointThenRetryAwardsOnce(SaveWritePhase phase)
        {
            string json=File.ReadAllText(service.MainPath);string id=state.run.runId;var run=state.run;
            service.FaultInjection=at=>{if(at==phase)throw new IOException("T06 simulated write failure");};
            Assert.That(rebirth.TryRebirth(id).Success,Is.False);Assert.That(state.run,Is.SameAs(run));Assert.That(state.run.water,Is.EqualTo(777));Assert.That(PermanentEffects.Points(state.permanent,"young_tree"),Is.Zero);Assert.That(File.ReadAllText(service.MainPath),Is.EqualTo(json));Assert.That(revision,Is.EqualTo(1));
            service.FaultInjection=null;Assert.That(rebirth.TryRebirth(id).Success,Is.True);Assert.That(rebirth.TryRebirth(id).Success,Is.False);Assert.That(PermanentEffects.Points(state.permanent,"young_tree"),Is.EqualTo(1));
        }
        [Test]public void CrashAfterReplacementReloadsNewRunAndOldCommandCannotReplay()
        {
            string old=state.run.runId;service.FaultInjection=at=>{if(at==SaveWritePhase.AfterReplace)throw new IOException("Crash diagnostic");};Assert.That(rebirth.TryRebirth(old).Success,Is.True);
            var dto=new SaveService(rules,directory).Load().result.Data;SaveSerializer.Restore(dto,state);Assert.That(rebirth.TryRebirth(old).Reason,Is.EqualTo("STALE_RUN"));Assert.That(PermanentEffects.Points(state.permanent,"young_tree"),Is.EqualTo(1));
        }
        [TestCase("locked")] [TestCase("overflow")] [TestCase("count")] [TestCase("invalid")]
        public void InvalidOrLockedCannotCommit(string kind)
        {
            string original=File.ReadAllText(service.MainPath);
            if(kind=="locked"){state.run.stageId="seedling";state.run.levels.trunk=0;}
            if(kind=="overflow")state.permanent.stagePoints.Find(p=>p.stageId=="young_tree").points=long.MaxValue;
            if(kind=="count")state.permanent.rebirthCount=long.MaxValue;
            if(kind=="invalid")state.run.hp=0;
            Assert.That(rebirth.TryRebirth(state.run.runId).Success,Is.False);Assert.That(File.ReadAllText(service.MainPath),Is.EqualTo(original));
        }
        [Test]public void ObserverFailureCannotUndoAcknowledgedCommitOrLeaveGuardLocked()
        {rebirth.Completed+=()=>throw new ArgumentException("View failure");Assert.That(rebirth.TryRebirth(state.run.runId).Success,Is.True);Assert.That(rebirth.LastObserverError,Is.EqualTo("View failure"));Assert.That(rebirth.IsExecuting,Is.False);Assert.That(service.Load().result.Data.run.runId,Is.EqualTo(state.run.runId));}
        [TestCase("seedling",1.1,1,0)] [TestCase("young_tree",1,1.1,0)] [TestCase("large_tree",1,1,.04)]
        public void SeparateEffectsAffectOnlyTheirTarget(string stage,double water,double growth,double protection)
        {
            state.permanent.stagePoints.Find(p=>p.stageId==stage).points=2;var e=PermanentEffects.Evaluate(state.permanent,rules);
            Assert.That(e.Water,Is.EqualTo(water));Assert.That(e.Growth,Is.EqualTo(growth));Assert.That(e.Protection,Is.EqualTo(protection));
            var up=new UpgradeSystem(state,rules,resources);Assert.That(up.Protection,Is.EqualTo(.05+protection).Within(1e-9));Assert.That(up.GetQuote("root").Cap,Is.EqualTo(10));
            Assert.That(up.GetQuote("trunk").CurrentOutput,Is.EqualTo(1.05*growth).Within(1e-9));
        }
        [Test]public void OnlineOfflineRatesSharePermanentEffectsWithoutVitalityOrConsumptionBonus()
        {
            state.run.status.statusId = "none";
            state.permanent.stagePoints.Find(p=>p.stageId=="seedling").points=2;state.permanent.stagePoints.Find(p=>p.stageId=="young_tree").points=4;
            state.run.levels.root=1;state.run.levels.leaves=1;state.run.levels.trunk=1;state.run.water=100;state.run.photosynthesis=100;state.run.growth=0;
            var rates=resources.Rates();Assert.That(rates.WaterProduced,Is.EqualTo(1.155).Within(1e-9));Assert.That(rates.WaterConsumed,Is.EqualTo(.25));Assert.That(rates.GrowthProduced,Is.EqualTo(1.26).Within(1e-9));
            var report=OfflineProduction.Evaluate(state,rules,now,now+3600);Assert.That(report.Water,Is.EqualTo(1630).Within(1e-6));Assert.That(report.Growth,Is.EqualTo(2160).Within(1e-6));Assert.That(state.vitality.bankSeconds,Is.EqualTo(250));
            state.run.water=0;state.run.photosynthesis=0;state.run.levels.root=0;report=OfflineProduction.Evaluate(state,rules,now,now+3600);Assert.That(report.GrowthDelta,Is.Zero);
        }
        [Test]public void CloneAliasCannotExposeUncommittedReset()
        {
            var unsafeSystem=new RebirthSystem(state,rules,s=>s,(s,id)=>new CommandResult(true));var old=state.run;
            Assert.That(unsafeSystem.TryRebirth(state.run.runId).Reason,Is.EqualTo("CLONE_ALIAS"));Assert.That(state.run,Is.SameAs(old));Assert.That(PermanentEffects.Points(state.permanent,"young_tree"),Is.Zero);
        }
        [TestCase("duplicate")] [TestCase("missing")] [TestCase("target")] [TestCase("nan")]
        public void InvalidEffectConfigurationIsRejected(string kind)
        {
            if(kind=="duplicate")rules.pointEffects[1].stageId="seedling";
            if(kind=="missing")rules.pointEffects=new PointEffectDefinition[0];
            if(kind=="target")rules.pointEffects[0].targetId="damage";
            if(kind=="nan")rules.pointEffects[0].perPoint=double.NaN;
            Assert.That(rules.Validate(),Is.Not.Empty);Assert.Throws<ArgumentException>(()=>GameStateFactory.CreateNew(rules));
        }
        [Test]public void EffectMappingUsesIdsRegardlessOfWalletAndConfigOrder()
        {
            state.permanent.stagePoints.Find(p=>p.stageId=="seedling").points=2;state.permanent.stagePoints.Find(p=>p.stageId=="young_tree").points=3;state.permanent.stagePoints.Find(p=>p.stageId=="large_tree").points=4;
            state.permanent.stagePoints.Reverse();Array.Reverse(rules.pointEffects);var e=PermanentEffects.Evaluate(state.permanent,rules);
            Assert.That(e.Water,Is.EqualTo(1.1));Assert.That(e.Growth,Is.EqualTo(1.15));Assert.That(e.Protection,Is.EqualTo(.08));
        }
        [TestCase("prototype-1")] [TestCase("prototype-2")]
        public void OlderConfigMigratesByValidationWithoutResettingWallets(string version)
        {state.permanent.stagePoints[0].points=4;var dto=SaveSerializer.Snapshot(state,version,"t06",1);var r=SaveCodec.Decode(SaveCodec.Encode(dto),rules);Assert.That(r.Success,Is.True,r.Reason);Assert.That(r.Data.permanent.stagePoints[0].points,Is.EqualTo(4));}

        [Test]
        public void MatureRebirthLocksSpiritAndFundsAllDefaultFirstLevels()
        {
            state.run.stageId="mature_tree";state.run.spiritAbsorptionUnlocked=true;state.run.spirit=100;state.run.levels.spiritAbsorption=2;
            Assert.That(rebirth.TryRebirth(state.run.runId).Success,Is.True);
            Assert.That(state.run.spirit,Is.Zero);Assert.That(state.run.spiritAbsorptionUnlocked,Is.False);Assert.That(state.run.levels.spiritAbsorption,Is.Zero);
            Assert.That(PermanentEffects.Points(state.permanent,"mature_tree"),Is.EqualTo(1));
            var up=new UpgradeSystem(state,rules,resources);
            Assert.That(up.TryUpgrade("root").Success,Is.True);Assert.That(up.TryUpgrade("leaves").Success,Is.True);Assert.That(up.TryUpgrade("trunk").Success,Is.True);
        }
    }
}
