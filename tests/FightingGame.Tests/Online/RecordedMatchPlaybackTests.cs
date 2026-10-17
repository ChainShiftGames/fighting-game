using System;
using System.IO;
using FightingGame.Online;
using Netcode.Drivers;
using Netcode.Replays;
using Sim.Input;
using Sim.State;
using Xunit;

namespace FightingGame.Tests.Online;

/// <summary>
/// A local match recorded, saved and played back the way the match startups compose it (without the engine parts:
/// view, effects, loop) ends on the same state.
/// </summary>
public sealed class RecordedMatchPlaybackTests : IDisposable
{
    private const int Frames = 900;

    private readonly string _path = Path.Combine(Path.GetTempPath(), "fg-replays-" + Guid.NewGuid().ToString("N"));

    [Fact]
    public void ASavedLocalMatchPlaysBackToTheSameChecksum()
    {
        MatchLineup lineup = MatchLineup.From(TestGameData.Data);
        var recorder = new ReplayRecorder(lineup.CreateReplayHeader(TestGameData.Data));
        using var match = new LocalMatchDriver<MatchState>(lineup.RecordedSimulation(recorder));
        var recording = new MatchRecording(recorder, match, confirmed: null, "local");
        for (int frame = 0; frame < Frames; frame++)
        {
            Assert.True(match.TryAdvanceFrame([Player1Input(frame), Player2Input(frame)]));
        }

        string file = new ReplayFolder(_path).Save(recording.TakeOnEnd()!, DateTime.Now, recording.Kind);
        Replay replay = ReplayFolder.Load(file, TestGameData.Data);
        using var playback = new ReplayMatchDriver<MatchState>(
            new ComposedMatchSimulation(MatchLineup.FromReplay(TestGameData.Data, replay.Header).Simulation),
            replay.Inputs);
        while (playback.TryAdvanceFrame([]))
        {
        }

        Assert.Equal(Frames, playback.CurrentFrame);
        Assert.Equal(StateChecksum.Compute(in match.State), StateChecksum.Compute(in playback.State));
    }

    [Fact]
    public void TheHeaderHoldsTheRulesTheMatchWasBuiltWith()
    {
        MatchLineup lineup = MatchLineup.From(TestGameData.Data);

        ReplayHeader header = lineup.CreateReplayHeader(TestGameData.Data);

        Assert.Equal(lineup.Simulation.Rules.ComputeHash(), header.RulesHash);
        Assert.Equal(GameVersion.Current, header.GameVersion);
        Assert.Equal(lineup.Characters[1].Id, header.Player2.CharacterId);
    }

    [Fact]
    public void PlaybackUsesTheRecordedRulesNotTheCurrentData()
    {
        ReplayHeader recorded = MatchLineup.From(TestGameData.Data).CreateReplayHeader(TestGameData.Data);
        Sim.Rules.MatchRules tweaked = recorded.Rules;
        tweaked.RoundsToWin++;
        var header = new ReplayHeader(
            recorded.GameVersion, recorded.DataHash, in tweaked, recorded.Player1, recorded.Player2);

        MatchLineup lineup = MatchLineup.FromReplay(TestGameData.Data, header);

        Assert.Equal(tweaked.RoundsToWin, lineup.Simulation.Rules.RoundsToWin);
    }

    public void Dispose()
    {
        if (Directory.Exists(_path))
        {
            Directory.Delete(_path, recursive: true);
        }
    }

    // Walks in, attacks with a few buttons and jumps now and then, so the match has movement, hits and blocks.
    private static InputWord Player1Input(int frame)
    {
        ushort direction = frame % 120 < 60 ? InputWord.Right : (frame % 90 == 0 ? InputWord.Up : (ushort)0);
        ushort button = frame % 17 == 0 ? (ushort)(1 << (InputWord.FirstButtonBit + (frame / 17 % 3))) : (ushort)0;
        return new InputWord((ushort)(direction | button));
    }

    private static InputWord Player2Input(int frame)
    {
        ushort direction = frame % 100 < 40 ? InputWord.Left : (frame % 100 < 70 ? InputWord.Right : (ushort)0);
        ushort button = frame % 23 == 0 ? (ushort)(1 << (InputWord.FirstButtonBit + 1)) : (ushort)0;
        return new InputWord((ushort)(direction | button));
    }
}
