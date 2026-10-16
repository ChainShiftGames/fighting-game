using System;
using Netcode.Drivers;
using Sim.Composition;
using Sim.Input;
using Sim.State;
using Xunit;

namespace Netcode.Tests.Matches;

/// <summary>
/// SyncTest on the real match (ADR-0006, P1-19): a Testman mirror played by seeded fighting inputs through the test
/// driver, every frame rolled back and resimulated; any checksum mismatch throws a desync.
/// </summary>
public sealed class MatchSyncTestTests
{
    private const int Frames = 10_000;

    // The rollback window (developer setting, ADR-0003).
    private const int RollbackFrames = 8;
    private const uint Player1Seed = 20261016;
    private const uint Player2Seed = 61016202;

    [Fact]
    public void TestmanMirrorStaysInSyncForTenThousandFrames()
    {
        MatchSimulation match = TestmanMirror.Create();
        var syncTest = new RollbackMatchDriver<MatchState>(
            new ComposedMatchSimulation(match), SyncTestSession.Build<MatchState>(RollbackFrames));
        using var driver = new TestMatchDriver<MatchState>(
            syncTest, [new FightingInputs(Player1Seed), new FightingInputs(Player2Seed)]);

        for (int frame = 0; frame < Frames; frame++)
        {
            Assert.True(driver.TryAdvanceFrame([]), $"Frame {frame} did not run.");
        }

        Assert.Equal(Frames, driver.State.Frame);

        // The inputs must actually fight, or the test proves little: rounds were won.
        Assert.True(
            driver.State.Round.Round > 1 || driver.State.Round.Phase != RoundPhase.Fighting,
            "No round ended in 10,000 frames; the seeded inputs don't fight.");
    }
}
