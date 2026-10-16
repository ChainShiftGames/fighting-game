using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Sim.Modules;
using Sim.Rules;
using Sim.State;
using Xunit;

namespace Sim.Tests.Modules;

public sealed class ModuleLineupTests
{
    private static FighterContact P2HitsP1 => new(attacker: 1, defender: 0);

    [Fact]
    public void ModulesRunInSlotOrderAtEveryHookPoint()
    {
        var log = new List<string>();
        MatchRules rules = default;
        rules.Modules[1].Module = new ModuleId(20);
        rules.Modules[4].Module = new ModuleId(10);
        ModuleLineup lineup = CreateLineup(rules, new OrderRecordingModule(10, log), new OrderRecordingModule(20, log));
        MatchState state = default;

        lineup.RunAfterInput(ref state);
        lineup.RunEndOfFrame(ref state);

        Assert.Equal(["AfterInput 20", "AfterInput 10", "EndOfFrame 20", "EndOfFrame 10"], log);
    }

    [Fact]
    public void EachHookPointCallsOnlyItsOwnHook()
    {
        MatchRules rules = default;
        rules.Modules[0].Module = new ModuleId(1);
        ModuleLineup lineup = CreateLineup(rules, new CountingModule(1));
        MatchState state = default;

        lineup.RunAfterInput(ref state);
        lineup.RunBeforeStateMachines(ref state);
        lineup.RunBeforeStateMachines(ref state);
        lineup.RunAfterStateMachines(ref state);
        lineup.RunOnHit(ref state, P2HitsP1);
        lineup.RunOnBlock(ref state, P2HitsP1);
        lineup.RunOnBlock(ref state, P2HitsP1);
        lineup.RunOnThrow(ref state, P2HitsP1);
        lineup.RunEndOfFrame(ref state);

        CountingState counts = SliceAs<CountingState>(ref state, 0);
        Assert.Equal(
            (1, 2, 1, 1, 2, 1, 1),
            (counts.AfterInput, counts.BeforeStateMachines, counts.AfterStateMachines, counts.OnHit, counts.OnBlock,
                counts.OnThrow, counts.EndOfFrame));
    }

    [Fact]
    public void ContactHooksSeeTheContact()
    {
        MatchRules rules = default;
        rules.Modules[0].Module = new ModuleId(1);
        ModuleLineup lineup = CreateLineup(rules, new CountingModule(1));
        MatchState state = default;

        lineup.RunOnThrow(ref state, P2HitsP1);

        CountingState counts = SliceAs<CountingState>(ref state, 0);
        Assert.Equal((1, 0), (counts.LastAttacker, counts.LastDefender));
    }

    [Fact]
    public void AModuleWritesOnlyItsOwnSlice()
    {
        MatchRules rules = default;
        rules.Modules[5].Module = new ModuleId(1);
        ModuleLineup lineup = CreateLineup(rules, new CountingModule(1));
        MatchState state = default;
        state.Fighters[0].Health = 100;
        MatchState before = state;

        lineup.RunAfterInput(ref state);

        Assert.Equal(1, SliceAs<CountingState>(ref state, 5).AfterInput);
        state.Modules[5] = before.Modules[5];
        Assert.Equal(StateChecksum.Compute(in before), StateChecksum.Compute(in state));
    }

    [Fact]
    public void AModuleReadsTheConfigOfItsOwnSlot()
    {
        MatchRules rules = default;
        rules.Modules[0].Module = new ModuleId(1);
        rules.Modules[0].Config[0] = 11;
        rules.Modules[2].Module = new ModuleId(2);
        rules.Modules[2].Config[0] = 22;
        ModuleLineup lineup = CreateLineup(rules, new CountingModule(1), new CountingModule(2));
        MatchState state = default;

        lineup.RunAfterInput(ref state);

        Assert.Equal(11, SliceAs<CountingState>(ref state, 0).ConfigSeen);
        Assert.Equal(22, SliceAs<CountingState>(ref state, 2).ConfigSeen);
    }

    [Fact]
    public void RequestsAreKeyedByTheModuleSlotAndFighter()
    {
        MatchRules rules = default;
        rules.Modules[3].Module = new ModuleId(1);
        rules.Modules[3].Config[0] = 9;
        var requests = new CoreRequests();
        var lineup = new ModuleLineup(new ModuleCatalog(new RequestingModule(1)), in rules.Modules, requests);
        MatchState state = default;

        lineup.RunAfterInput(ref state);

        int key = CoreRequests.FighterKey(3, RequestingModule.Fighter);
        Assert.True(requests.Cancels.TryGet(key, out CancelRequest cancel));
        Assert.True(requests.TimeScales.TryGet(key, out TimeScaleRequest timeScale));
        Assert.True(requests.HitstopOverrides.TryGet(key, out HitstopOverrideRequest hitstop));
        Assert.True(requests.Freezes.TryGet(3, out FreezeRequest freeze));
        Assert.Equal((9, 9, 9, 9), (cancel.Priority, timeScale.Denominator, hitstop.Frames, freeze.Frames));
        Assert.False(requests.Cancels.TryGet(CoreRequests.FighterKey(3, 0), out _));
    }

    [Fact]
    public void AnUnregisteredModuleInTheLineupFailsLoudly()
    {
        MatchRules rules = default;
        rules.Modules[0].Module = new ModuleId(99);

        Assert.Throws<ArgumentException>(() => CreateLineup(rules, new CountingModule(1)));
    }

    [Fact]
    public void AModuleTwiceInTheLineupFailsLoudly()
    {
        MatchRules rules = default;
        rules.Modules[0].Module = new ModuleId(1);
        rules.Modules[1].Module = new ModuleId(1);

        Assert.Throws<ArgumentException>(() => CreateLineup(rules, new CountingModule(1)));
    }

    [Fact]
    public void RegisteredButNotEnabledModulesDoNotRun()
    {
        MatchRules rules = default;
        ModuleLineup lineup = CreateLineup(rules, new CountingModule(1));
        MatchState state = default;

        lineup.RunAfterInput(ref state);

        MatchState untouched = default;
        Assert.Equal(StateChecksum.Compute(in untouched), StateChecksum.Compute(in state));
    }

    [Fact]
    public void RunningHooksAllocatesNothing()
    {
        MatchRules rules = default;
        for (int slot = 0; slot < ModuleStates.MaxModules; slot++)
        {
            rules.Modules[slot].Module = new ModuleId(slot + 1);
        }

        var modules = new IMechanicModule[ModuleStates.MaxModules];
        for (int i = 0; i < modules.Length; i++)
        {
            modules[i] = new CountingModule(i + 1);
        }

        ModuleLineup lineup = CreateLineup(rules, modules);
        MatchState state = default;
        RunEveryHook(lineup, ref state);

        long before = GC.GetAllocatedBytesForCurrentThread();
        for (int frame = 0; frame < 100; frame++)
        {
            RunEveryHook(lineup, ref state);
        }

        Assert.Equal(0, GC.GetAllocatedBytesForCurrentThread() - before);
    }

    private static void RunEveryHook(ModuleLineup lineup, ref MatchState state)
    {
        lineup.RunAfterInput(ref state);
        lineup.RunBeforeStateMachines(ref state);
        lineup.RunAfterStateMachines(ref state);
        lineup.RunOnHit(ref state, P2HitsP1);
        lineup.RunOnBlock(ref state, P2HitsP1);
        lineup.RunOnThrow(ref state, P2HitsP1);
        lineup.RunEndOfFrame(ref state);
    }

    private static ModuleLineup CreateLineup(MatchRules rules, params IMechanicModule[] modules) =>
        new(new ModuleCatalog(modules), in rules.Modules, new CoreRequests());

    private static TSlice SliceAs<TSlice>(ref MatchState state, int slot)
        where TSlice : unmanaged
    {
        Span<int> slice = state.Modules[slot];
        return MemoryMarshal.Read<TSlice>(MemoryMarshal.AsBytes(slice));
    }
}
