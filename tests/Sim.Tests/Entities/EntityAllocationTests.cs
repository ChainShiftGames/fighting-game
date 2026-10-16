using System;
using Sim.State;
using Xunit;
using static Sim.Tests.Entities.ProjectileCharacter;

namespace Sim.Tests.Entities;

/// <summary>ARCHITECTURE.md §4: entity stages allocate nothing per frame, even spawning, hitting and clashing.</summary>
public sealed class EntityAllocationTests
{
    [Fact]
    public void EntityFramesAllocateNothing()
    {
        EntityStages stages = new();
        MatchState state = default;
        state.Fighters[0] = At(-120_000, 1);
        state.Fighters[1] = At(90_000, -1);
        Run(stages, ref state, 0, 60);

        long before = GC.GetAllocatedBytesForCurrentThread();
        Run(stages, ref state, 60, 1000);

        Assert.Equal(0, GC.GetAllocatedBytesForCurrentThread() - before);
        Assert.True(state.Fighters[1].Health < Health, "No projectile ever hit, so the run proves little.");
    }

    private static void Run(EntityStages stages, ref MatchState state, int from, int to)
    {
        for (int frame = from; frame < to; frame++)
        {
            EntityScript.Play(ref state, stages.Requests, frame, role: 0, fighter: 0);
            EntityScript.Play(ref state, stages.Requests, frame, role: 1, fighter: 1);
            stages.Tick(ref state);
        }
    }
}
