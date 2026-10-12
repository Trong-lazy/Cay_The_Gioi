using System;
namespace CayTheGioi.Domain
{
    public readonly struct RebirthQuote
    {
        public readonly string RunId, StageId, StageName, Reason;
        public readonly long Points;
        public readonly bool Forced;
        public bool CanConfirm => Reason=="";
        public RebirthQuote(string run,string stage,string name,long points,bool forced,string reason) {RunId=run;StageId=stage;StageName=name;Points=points;Forced=forced;Reason=reason;}
    }
    public sealed class RebirthSystem
    {
        private readonly GameState state;
        private readonly PrototypeRules rules;
        private readonly Func<GameState,GameState> clone;
        private readonly Func<GameState,string,CommandResult> persist;
        private readonly Func<bool> busy;
        public bool IsExecuting {get;private set;}
        public event Action Completed;
        public string LastObserverError {get;private set;}
        public RebirthSystem(GameState state,PrototypeRules rules,Func<GameState,GameState> clone,Func<GameState,string,CommandResult> persist,Func<bool> busy=null)
        {this.state=state;this.rules=rules;this.clone=clone;this.persist=persist;this.busy=busy;}
        public RebirthQuote GetQuote()
        {
            var run=state.run;var stage=rules.FindStage(run.stageId);bool forced=run.life=="DEAD";string reason="";long points=0;
            try {points=PermanentEffects.Points(state.permanent,run.stageId);}
            catch(ArgumentException){reason="INVALID_POINTS";}
            if(IsExecuting || (busy!=null&&busy()))reason="BUSY";
            else if(stage==null || (run.life!="ALIVE"&&!forced) || !ProductionSystem.Finite(run.hp) || (forced?run.hp!=0:run.hp<=0) || run.hp>rules.maxHp ||
                !ProductionSystem.Finite(run.water)||run.water<0 || !ProductionSystem.Finite(run.growth)||run.growth<0 || !ProductionSystem.Finite(run.photosynthesis)||run.photosynthesis<0 || !ProductionSystem.Finite(run.spirit)||run.spirit<0 || run.levels==null ||
                run.levels.root<0||run.levels.root>stage.rootCap||run.levels.leaves<0||run.levels.leaves>stage.leavesCap||run.levels.trunk<0||run.levels.trunk>stage.trunkCap || run.levels.spiritAbsorption<0||run.levels.spiritAbsorption>stage.spiritAbsorptionCap ||
                (run.spiritAbsorptionUnlocked && (run.levels.spiritAbsorption<1||stage.spiritAbsorptionCap==0)) ||
                (!run.spiritAbsorptionUnlocked && (run.levels.spiritAbsorption!=0||run.spirit!=0)))reason="INVALID_STATE";
            else if(!forced&&!stage.rebirthUnlocked)reason="LOCKED";
            else if(points==long.MaxValue || state.permanent.rebirthCount==long.MaxValue)reason="OVERFLOW";
            return new RebirthQuote(run.runId,run.stageId,stage?.displayName,points,forced,reason);
        }
        public CommandResult TryRebirth(string expectedRunId)
        {
            if(IsExecuting)return new CommandResult(false,"BUSY");
            if(string.IsNullOrWhiteSpace(expectedRunId)||expectedRunId!=state.run.runId)return new CommandResult(false,"STALE_RUN");
            var quote=GetQuote();if(!quote.CanConfirm)return new CommandResult(false,quote.Reason);
            IsExecuting=true;
            try
            {
                var candidate=clone(state);
                if(ReferenceEquals(candidate,state)||ReferenceEquals(candidate.permanent,state.permanent))return new CommandResult(false,"CLONE_ALIAS");
                candidate.run=GameStateFactory.CreateNew(rules).run;
                var entry=candidate.permanent.stagePoints.Find(p=>p.stageId==quote.StageId);
                if(entry==null){entry=new StagePointEntry{stageId=quote.StageId};candidate.permanent.stagePoints.Add(entry);}
                entry.points=checked(quote.Points+1);candidate.permanent.rebirthCount=checked(candidate.permanent.rebirthCount+1);
                var result=persist(candidate,expectedRunId);if(!result.Success)return result;
                state.run=candidate.run;state.permanent=candidate.permanent;
                // Vitality/settings remain the original live objects and values.
                LastObserverError=null;
                if(Completed!=null)foreach(Action subscriber in Completed.GetInvocationList())
                    try{subscriber();}catch(Exception error){LastObserverError=error.Message;}
                return new CommandResult(true);
            }
            catch(OverflowException){return new CommandResult(false,"OVERFLOW");}
            catch(ArgumentException){return new CommandResult(false,"INVALID_STATE");}
            finally{IsExecuting=false;}
        }
    }
}
