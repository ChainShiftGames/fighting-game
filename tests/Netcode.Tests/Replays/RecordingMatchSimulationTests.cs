using Netcode.Drivers;
using Netcode.Replays;
using Netcode.Tests.Matches;
using Sim.Composition;
using Sim.State;
using Xunit;

namespace Netcode.Tests.Replays;

/// <summary>
/// A match recorded while it runs, saved, loaded and played back through the replay driver ends in the same state:
/// the replay reproduces the match.
/// </summary>
public sealed class RecordingMatchSimulationTests
{
    private const int Frames = 3600;

    // The rollback window (developer setting, ADR-0003).
    private const int RollbackFrames = 8;
    private const uint Player1Seed = 20261016;
    private const uint Player2Seed = 61016202;

    [Fact]
    public void ALocalMatchPlaysBackToTheSameFinalChecksum()
    {
        var recorder = new ReplayRecorder(RecordedMatches.Header());
        var simulation = new RecordingMatchSimulation(new ComposedMatchSimulation(TestmanMirror.Create()), recorder);
        using var match = Played(new LocalMatchDriver<MatchState>(simulation));

        Assert.Equal(Frames, recorder.FrameCount);
        Assert.Equal(StateChecksum.Compute(in match.State), PlayedBack(recorder.ToReplay()));
    }

    [Fact]
    public void ARolledBackMatchPlaysBackToTheSameFinalChecksum()
    {
        // SyncTest rolls back and resimulates every frame, as an online match does when inputs arrive late.
        var recorder = new ReplayRecorder(RecordedMatches.Header());
        var simulation = new RecordingMatchSimulation(new ComposedMatchSimulation(TestmanMirror.Create()), recorder);
        using var match = Played(
            new RollbackMatchDriver<MatchState>(simulation, SyncTestSession.Build<MatchState>(RollbackFrames)));

        Assert.Equal(Frames, recorder.FrameCount);
        Assert.Equal(StateChecksum.Compute(in match.State), PlayedBack(recorder.ToReplay()));
    }

    private static TestMatchDriver<MatchState> Played(IMatchDriver<MatchState> driver)
    {
        var match = new TestMatchDriver<MatchState>(
            driver, [new FightingInputs(Player1Seed), new FightingInputs(Player2Seed)]);
        for (int frame = 0; frame < Frames; frame++)
        {
            Assert.True(match.TryAdvanceFrame([]), $"Frame {frame} did not run.");
        }

        return match;
    }

    // As the game will: save the file, load it, rebuild the match from its header and play it to the end.
    private static ulong PlayedBack(Replay recorded)
    {
        Replay loaded = RecordedMatches.Load(RecordedMatches.Save(recorded));
        MatchSimulation match = RecordedMatches.MatchOf(loaded.Header);
        using var replay = new ReplayMatchDriver<MatchState>(new ComposedMatchSimulation(match), loaded.Inputs);
        while (replay.TryAdvanceFrame([]))
        {
        }

        Assert.Equal(Frames, replay.CurrentFrame);
        return StateChecksum.Compute(in replay.State);
    }
}
