using System;
using Netcode.Desync;
using Netcode.Drivers;
using Sim.Input;
using Sim.Toy;
using Xunit;

namespace Netcode.Tests.Drivers;

/// <summary>SyncTest (ADR-0006): every frame is rolled back and resimulated; the runs must match bit for bit.</summary>
public sealed class SyncTestTests
{
    private const int Seed = 20261015;
    private const int Frames = 10_000;
    private const int RollbackFrames = 8;

    [Fact]
    public void ToySimulationStaysInSyncForTenThousandFrames()
    {
        var simulation = new CountingSimulation();
        using var driver = CreateDriver(simulation);

        RunFrames(driver, Frames);

        Assert.Equal(Frames, driver.CurrentFrame);
        Assert.Equal(Frames, driver.State.Frame);

        // Each frame runs once forward and once more when SyncTest rolls back over it.
        Assert.True(simulation.TickCalls >= 2 * (Frames - RollbackFrames), $"Only {simulation.TickCalls} ticks ran.");
    }

    [Fact]
    public void DeliberateNonDeterminismIsCaught()
    {
        using var driver = CreateDriver(new NonDeterministicSimulation());

        Assert.Throws<DesyncDetectedException>(() => RunFrames(driver, Frames));
    }

    private static RollbackMatchDriver<ToyState> CreateDriver(IMatchSimulation<ToyState> simulation) =>
        new(simulation, SyncTestSession.Build<ToyState>(RollbackFrames));

    private static void RunFrames(RollbackMatchDriver<ToyState> driver, int frames)
    {
        var inputs = new SeededInputs(Seed);
        Span<InputWord> localInputs = stackalloc InputWord[driver.LocalPlayerCount];
        for (int frame = 0; frame < frames; frame++)
        {
            for (int player = 0; player < localInputs.Length; player++)
            {
                localInputs[player] = inputs.Next();
            }

            Assert.True(driver.TryAdvanceFrame(localInputs), $"Frame {frame} did not run.");
        }
    }
}
