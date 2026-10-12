using System;
namespace CayTheGioi.Domain
{
    public readonly struct ProductionResult
    {
        public readonly double Water, Photosynthesis, Growth, Spirit;
        public readonly double WaterProduced, WaterConsumed, PhotosynthesisProduced, PhotosynthesisConsumed, GrowthProduced, SpiritProduced;
        public ProductionResult(double water,double growth,double produced,double consumed,double generated,
            double photosynthesis=0,double photoProduced=0,double photoConsumed=0,double spirit=0,double spiritProduced=0)
        { Water=water;Growth=growth;WaterProduced=produced;WaterConsumed=consumed;GrowthProduced=generated;
          Photosynthesis=photosynthesis;PhotosynthesisProduced=photoProduced;PhotosynthesisConsumed=photoConsumed;Spirit=spirit;SpiritProduced=spiritProduced; }
    }
    // Water depletion splits the interval. Photo supply/demand is then integrated exactly
    // in each constant-rate segment, so offline and small online ticks are equivalent.
    public static class ProductionSystem
    {
        public static bool Finite(double value)=>!double.IsNaN(value)&&!double.IsInfinity(value);
        public static ProductionResult Evaluate(RunState run,PrototypeRules rules,double seconds,double outputMultiplier=1)
            =>Evaluate(run,rules,seconds,ProductionModifiers.Neutral,outputMultiplier);
        public static ProductionResult Evaluate(RunState run,PrototypeRules rules,double seconds,ProductionModifiers permanent,double outputMultiplier=1)
            =>Evaluate(run,rules,seconds,permanent,outputMultiplier,1,1,1);
        public static ProductionResult Evaluate(RunState run,PrototypeRules rules,double seconds,ProductionModifiers permanent,
            double outputMultiplier,double weatherWaterMultiplier,double weatherGrowthMultiplier,double burningLeavesMultiplier)
        {
            if(run==null||run.levels==null||!Valid(run.water)||!Valid(run.photosynthesis)||!Valid(run.growth)||!Valid(run.spirit)||
                !Valid(seconds)||!Valid(outputMultiplier)||!Valid(permanent.Water)||permanent.Water<1||!Valid(permanent.Growth)||permanent.Growth<1||
                !Valid(permanent.Photosynthesis)||permanent.Photosynthesis<1||!Valid(permanent.Spirit)||permanent.Spirit<1||
                !Valid(weatherWaterMultiplier)||weatherWaterMultiplier<=0||!Valid(weatherGrowthMultiplier)||weatherGrowthMultiplier<=0||
                !Valid(burningLeavesMultiplier)||burningLeavesMultiplier<=0||run.levels.root<0||run.levels.leaves<0||run.levels.trunk<0||run.levels.spiritAbsorption<0)
                throw new ArgumentException("Invalid production input.");
            if(run.life!="ALIVE"||seconds==0)return new ProductionResult(run.water,run.growth,0,0,0,run.photosynthesis,0,0,run.spirit);
            double supply=run.levels.root*rules.rootWaterPerLevelSecond*permanent.Water*weatherWaterMultiplier*outputMultiplier;
            double waterDemand=run.levels.leaves*rules.leavesWaterPerLevelSecond;
            double photoRate=run.levels.leaves*rules.leavesPhotosynthesisPerLevelSecond*permanent.Photosynthesis*weatherGrowthMultiplier*burningLeavesMultiplier*outputMultiplier;
            double photoDemand=run.levels.trunk*rules.trunkPhotosynthesisPerLevelSecond;
            double growthRate=run.levels.trunk*rules.trunkGrowthPerLevelSecond*permanent.Growth*outputMultiplier;
            double spiritRate=run.spiritAbsorptionUnlocked?run.levels.spiritAbsorption*rules.spiritPerLevelSecond*permanent.Spirit*outputMultiplier:0;
            if(!Valid(supply)||!Valid(waterDemand)||!Valid(photoRate)||!Valid(photoDemand)||!Valid(growthRate)||!Valid(spiritRate))
                throw new ArgumentException("Invalid production rates.");
            double fullSeconds=waterDemand>supply?Math.Min(seconds,run.water/(waterDemand-supply)):seconds;
            double fraction=waterDemand>0?Math.Min(1,supply/waterDemand):1;
            double photo=run.photosynthesis,photoProduced=0,photoConsumed=0,growthProduced=0;
            IntegratePhoto(fullSeconds,photoRate,photoDemand,growthRate,ref photo,ref photoProduced,ref photoConsumed,ref growthProduced);
            IntegratePhoto(seconds-fullSeconds,photoRate*fraction,photoDemand,growthRate,ref photo,ref photoProduced,ref photoConsumed,ref growthProduced);
            double waterProduced=supply*seconds,waterConsumed=waterDemand*fullSeconds+Math.Min(supply,waterDemand)*(seconds-fullSeconds);
            double water=Math.Max(0,run.water+waterProduced-waterConsumed),growth=run.growth+growthProduced;
            double spiritProduced=spiritRate*seconds,spirit=run.spirit+spiritProduced;
            if(!Valid(waterProduced)||!Valid(waterConsumed)||!Valid(photoProduced)||!Valid(photoConsumed)||!Valid(growthProduced)||!Valid(water)||!Valid(photo)||!Valid(growth)||!Valid(spirit))
                throw new ArgumentException("Production overflow.");
            return new ProductionResult(water,growth,waterProduced,waterConsumed,growthProduced,photo,photoProduced,photoConsumed,spirit,spiritProduced);
        }
        private static bool Valid(double value)=>Finite(value)&&value>=0;
        private static void IntegratePhoto(double seconds,double supply,double demand,double output,ref double balance,ref double produced,ref double consumed,ref double growth)
        {
            double generated=supply*seconds,spent=Math.Min(balance+generated,demand*seconds),active=demand>0?spent/demand:seconds;
            balance=Math.Max(0,balance+generated-spent);produced+=generated;consumed+=spent;growth+=output*active;
        }
    }
}
