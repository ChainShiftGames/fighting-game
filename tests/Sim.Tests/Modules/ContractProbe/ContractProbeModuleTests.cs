using System;
using System.Runtime.InteropServices;
using Sim.Modules;
using Sim.Modules.ContractProbe;
using Sim.Rules;
using Sim.State;
using Xunit;

namespace Sim.Tests.Modules.ContractProbe;

/// <summary>The contract end to end through the real registration: lineup from rules, own slice, config, requests.</summary>
/// <remarks>Deleted together with src/Sim/Modules/ContractProbe/ in the removal test.</remarks>
public sealed class ContractProbeModuleTests
{
    private const int Slot = 2;

    [Fact]
    public void CountsFramesAndHitsInItsSliceAndRequestsHitstopOnHit()
    {
        var probe = new ContractProbeModule();
        MatchRules rules = default;
        rules.Modules[Slot].Module = probe.Id;
        ConfigOf(ref rules.Modules[Slot].Config).HitstopFrames = 12;
        var requests = new CoreRequests();
        var lineup = new ModuleLineup(RegisteredModules.Create(), in rules.Modules, requests);
        MatchState state = default;

        lineup.RunAfterInput(ref state);
        lineup.RunAfterInput(ref state);
        lineup.RunOnHit(ref state, new FighterContact(attacker: 0, defender: 1));

        ContractProbeState probeState = MemoryMarshal.Read<ContractProbeState>(
            MemoryMarshal.AsBytes((Span<int>)state.Modules[Slot]));
        Assert.Equal((2, 1, 1), (probeState.FramesSeen, probeState.HitsSeen, probeState.LastDefender));
        Assert.True(requests.HitstopOverrides.TryGet(CoreRequests.FighterKey(Slot, 1), out HitstopOverrideRequest hitstop));
        Assert.Equal(12, hitstop.Frames);
    }

    [Fact]
    public void IsFoundByItsDataName()
    {
        Assert.True(RegisteredModules.Create().TryFindId("contract_probe", out ModuleId id));
        Assert.Equal(new ContractProbeModule().Id, id);
    }

    private static ref ContractProbeConfig ConfigOf(ref ModuleConfig config) =>
        ref MemoryMarshal.AsRef<ContractProbeConfig>(MemoryMarshal.AsBytes((Span<int>)config));
}
