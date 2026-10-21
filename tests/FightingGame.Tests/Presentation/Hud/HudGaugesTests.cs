using FightingGame.Presentation.Hud;
using Sim.Modules;
using Sim.Rules;
using Sim.State;
using Xunit;

namespace FightingGame.Tests.Presentation.Hud;

public sealed class HudGaugesTests
{
    private static readonly ModuleCatalog _catalog = RegisteredModules.Create();

    [Fact]
    public void HidesEveryGaugeWhenTheirModulesAreOff()
    {
        var gauges = new HudGauges(_catalog, default(MatchRules));
        var state = default(MatchState);

        Assert.False(gauges.HasMeter);
        Assert.False(gauges.HasBurst);
        Assert.Equal(0, gauges.Meter(in state, 0));
        Assert.Equal(0f, gauges.BurstFraction(in state, 1));
    }

    [Fact]
    public void ReadsEachPlayersMeterFromTheMeterSlice()
    {
        MatchRules rules = Lineup(("meter", null, 0));
        var state = default(MatchState);
        state.Modules[0][0] = 2500;
        state.Modules[0][1] = 12_000;

        var gauges = new HudGauges(_catalog, in rules);

        Assert.True(gauges.HasMeter);
        Assert.Equal(2500, gauges.Meter(in state, 0));
        Assert.Equal(HudGauges.FullMeter, gauges.Meter(in state, 1));
    }

    [Fact]
    public void ReadsBurstAsShareOfTheConfiguredMaxGauge()
    {
        MatchRules rules = Lineup(("combo_limits", null, 0), ("burst", "maxGauge", 8000));
        var state = default(MatchState);
        state.Modules[1][0] = 2000;
        state.Modules[1][2] = 8000;

        var gauges = new HudGauges(_catalog, in rules);

        Assert.True(gauges.HasBurst);
        Assert.Equal(0.25f, gauges.BurstFraction(in state, 0));
        Assert.Equal(1f, gauges.BurstFraction(in state, 1));
    }

    // Puts the named modules in slots 0, 1, … with one config value set by key.
    private static MatchRules Lineup(params (string Name, string? Key, int Value)[] modules)
    {
        var rules = default(MatchRules);
        for (int slot = 0; slot < modules.Length; slot++)
        {
            Assert.True(_catalog.TryFindId(modules[slot].Name, out ModuleId id));
            rules.Modules[slot].Module = id;
            _catalog.TryFind(id, out IMechanicModule? module);
            for (int i = 0; i < module!.ConfigLayout.Count; i++)
            {
                if (module.ConfigLayout[i].Key == modules[slot].Key)
                {
                    rules.Modules[slot].Config[i] = modules[slot].Value;
                }
            }
        }

        return rules;
    }
}
