using System;
using Netcode.Drivers;
using Sim.Composition;
using Sim.Input;
using Sim.State;
using Xunit;

namespace Netcode.Tests.Matches;

/// <summary>
/// SyncTest on the real match (ADR-0006, P1-19): a Testman mirror with seeded fighting inputs, every frame rolled
/// back and resimulated; any checksum mismatch throws a desync.
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
        using var driver = new RollbackMatchDriver<MatchState>(
            new ComposedMatchSimulation(match), SyncTestSession.Build<MatchState>(RollbackFrames));
        var player1 = new FightingInputs(Player1Seed);
        var player2 = new FightingInputs(Player2Seed);
        Span<InputWord> inputs = stackalloc InputWord[driver.LocalPlayerCount];
        Assert.Equal(2, inputs.Length);

        for (int frame = 0; frame < Frames; frame++)
        {
            inputs[0] = player1.Next();
            inputs[1] = player2.Next();
            Assert.True(driver.TryAdvanceFrame(inputs), $"Frame {frame} did not run.");
        }

        Assert.Equal(Frames, driver.State.Frame);

        // The inputs must actually fight, or the test proves little: rounds were won.
        Assert.True(
            driver.State.Round.Round > 1 || driver.State.Round.Phase != RoundPhase.Fighting,
            "No round ended in 10,000 frames; the seeded inputs don't fight.");
    }
}
