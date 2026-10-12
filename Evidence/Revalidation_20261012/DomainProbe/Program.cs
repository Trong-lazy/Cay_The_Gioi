using System;
using System.Collections.Generic;
using System.Text.Json;
using CayTheGioi.Domain;

class Program
{
    static object Timeline(double dt, double until)
    {
        var rules = new PrototypeRules();
        var state = GameStateFactory.CreateNew(rules);
        state.run.stageId = "large_tree";
        state.run.levels.trunk = 0;
        var w = new WeatherSystem(state, rules, () => 0, max => 0);
        var h = new HealthStatusSystem(state, rules);
        w.ForceWeather("thunderstorm", 60);
        int strikes = 0;
        w.LightningStruck += _ => strikes++;
        for(double elapsed=0;elapsed<until-1e-8;)
        {
            double step=Math.Min(dt,until-elapsed);
            w.Tick(step, segment => h.Tick(segment,out _), () => h.SecondsToBoundary);
            elapsed+=step;
        }
        return new {dt,until,hp=state.run.hp,strikes,burningRemaining=state.run.status.remainingOnlineSeconds,weather=w.CurrentWeatherId};
    }
    static object EndStorm(bool nearDeath)
    {
        var rules = new PrototypeRules();
        var state = GameStateFactory.CreateNew(rules);
        state.run.stageId = "large_tree";
        state.run.levels.trunk=0;
        state.run.hp=nearDeath?5:100;
        var w = new WeatherSystem(state,rules,()=>1,max=>0);
        var h = new HealthStatusSystem(state,rules);
        w.ForceWeather("thunderstorm",1);
        state.run.status.statusId="burning";
        state.run.status.remainingOnlineSeconds=10;
        int died=0,changed=0;
        h.RunDied+=()=>died++;
        h.HealthChanged+=()=>changed++;
        double alive=0;
        w.Tick(1, segment => {h.Tick(segment,out double part); alive+=part;}, () => h.SecondsToBoundary);
        return new {nearDeath,hp=state.run.hp,life=state.run.life,diedEvents=died,healthEvents=changed,aliveSeconds=alive,weather=w.CurrentWeatherId};
    }
    static object DueStrike()
    {
        var rules = new PrototypeRules();
        var state = GameStateFactory.CreateNew(rules);
        state.run.stageId = "large_tree";
        var w = new WeatherSystem(state,rules,()=>0,max=>0);
        w.ForceWeather("thunderstorm",60);
        state.run.weather.strikeDelaySeconds=0;
        int strikes=0;
        w.LightningStruck+=_=>strikes++;
        w.Tick(5);
        return new {dt=5,weatherRemaining=w.RemainingSeconds,strikes,expectedWeatherRemaining=55};
    }
    static object DeathBoundary()
    {
        var rules=new PrototypeRules(); var s=GameStateFactory.CreateNew(rules);
        s.run.stageId="large_tree"; s.run.hp=1; s.run.levels.root=1;
        var w=new WeatherSystem(s,rules,()=>0,max=>0);
        var h=new HealthStatusSystem(s,rules);
        var r=new ResourceSystem(s,rules,new VitalitySystem(s,rules));
        w.ForceWeather("thunderstorm",60);
        double alive=0; bool success=true;
        w.Tick(20, segment => {success &= r.Tick(segment).Success; h.Tick(segment,out double part);alive+=part;}, () => h.SecondsToBoundary);
        return new {alive,expectedAlive=18.5,water=s.run.water,expectedWater=28.5,resourceSuccess=success};
    }
    static void Main()
    {
        Console.WriteLine(JsonSerializer.Serialize(new {source="Actual project Domain sources compiled under .NET; not Unity Play Mode",timeline=new[]{Timeline(0.05,20),Timeline(20,20),Timeline(60,60)},stormEnd=new[]{EndStorm(false),EndStorm(true)},dueStrike=DueStrike(),death=DeathBoundary()},new JsonSerializerOptions{WriteIndented=true}));
    }
}
