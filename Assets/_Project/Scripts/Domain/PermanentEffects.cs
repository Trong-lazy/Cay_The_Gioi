using System;
using System.Collections.Generic;
namespace CayTheGioi.Domain
{
    public readonly struct ProductionModifiers
    {
        public readonly double Water, Growth, Protection, Photosynthesis, Spirit;
        public ProductionModifiers(double water, double growth, double protection, double photosynthesis=1, double spirit=1) { Water=water; Growth=growth; Protection=protection; Photosynthesis=photosynthesis; Spirit=spirit; }
        public static ProductionModifiers Neutral => new ProductionModifiers(1,1,0);
    }
    public static class PermanentEffects
    {
        public static long Points(PermanentState state,string stageId)
        {
            long value=0; bool found=false;
            if(state==null || state.stagePoints==null)throw new ArgumentException("Missing point wallets.");
            foreach(var entry in state.stagePoints)
            {
                if(entry==null || entry.points<0)throw new ArgumentException("Invalid point wallet.");
                if(entry.stageId!=stageId)continue;
                if(found)throw new ArgumentException("Duplicate point wallet.");found=true;value=entry.points;
            }
            return value;
        }
        public static ProductionModifiers Evaluate(PermanentState state,PrototypeRules rules)
        {
            double water=1,growth=1,protection=0,photosynthesis=1,spirit=1;
            if(rules.pointEffects==null)throw new ArgumentException("Missing point effects.");
            var ids=new HashSet<string>(StringComparer.Ordinal);
            foreach(var effect in rules.pointEffects)
            {
                if(effect==null || rules.FindStage(effect.stageId)==null || !ids.Add(effect.stageId) || !ProductionSystem.Finite(effect.perPoint) || effect.perPoint<0)throw new ArgumentException("Invalid point effect.");
                double delta=Points(state,effect.stageId)*effect.perPoint;
                switch(effect.targetId) {case "water":water+=delta;break;case "growth":growth+=delta;break;case "photosynthesis":photosynthesis+=delta;break;case "spirit":spirit+=delta;break;case "protection":protection+=delta;break;default:throw new ArgumentException("Unknown effect target.");}
            }
            if(!ProductionSystem.Finite(water)||!ProductionSystem.Finite(growth)||!ProductionSystem.Finite(protection)||!ProductionSystem.Finite(photosynthesis)||!ProductionSystem.Finite(spirit))throw new ArgumentException("Point effect overflow.");
            return new ProductionModifiers(water,growth,protection,photosynthesis,spirit);
        }
    }
}
