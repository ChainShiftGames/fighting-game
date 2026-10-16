using System;
using System.Collections.Generic;
using System.IO;
using Netcode.Drivers;
using Netcode.Tests.Matches;
using Sim.Composition;
using Sim.Input;
using Sim.State;
using Xunit;

namespace Netcode.Tests.GoldenReplays;

/// <summary>
/// Golden replays (ADR-0006, P1-19): each <c>Replays/*.replay</c> is played through the replay match driver and its
/// state checksums must match the recorded ones. They only change intentionally (ARCHITECTURE.md §10).
/// </summary>
/// <remarks>
/// To re-record after an intentional gameplay or data change, run
/// <c>GOLDEN_REPLAYS_RECORD=1 dotnet test --project tests/Netcode.Tests</c> and say why in the PR. Recording keeps
/// each file's inputs (or generates them from its seed when it has none) and rewrites the data hash and checksums.
/// </remarks>
public sealed class GoldenReplayTests
{
    private const string RecordVariable = "GOLDEN_REPLAYS_RECORD";
    private const int CheckpointFrames = 600;

    public static TheoryData<string> Files => [.. Directory.GetFiles(ReplayFolder(), "*.replay")];

    [Theory]
    [MemberData(nameof(Files))]
    public void ReplayMatchesItsRecordedChecksums(string path)
    {
        GoldenReplay replay = GoldenReplay.Read(path);
        if (Environment.GetEnvironmentVariable(RecordVariable) == "1")
        {
            Record(replay, path);
            return;
        }

        string name = Path.GetFileName(path);
        string dataHash = TestmanMirror.Data.Hash.ToString();
        Assert.True(
            replay.DataHash == dataHash,
            $"{name}: game data changed since it was recorded ({replay.DataHash} -> {dataHash}). If intentional, " +
            $"re-record with {RecordVariable}=1 and say why in the PR.");
        Assert.NotEmpty(replay.Checksums);
        Assert.Equal(replay.Frames, replay.Inputs.Count);

        SortedDictionary<int, ulong> actual = Play(replay);
        foreach ((int frame, ulong expected) in replay.Checksums)
        {
            Assert.True(
                actual.TryGetValue(frame, out ulong checksum) && checksum == expected,
                $"{name}: frame {frame} checksum is {checksum:x16}, recorded {expected:x16}. Gameplay changed: if " +
                $"intentional, re-record with {RecordVariable}=1 and say why in the PR.");
        }
    }

    private static void Record(GoldenReplay replay, string path)
    {
        if (replay.Inputs.Count == 0)
        {
            var player1 = new FightingInputs(replay.Seed);
            var player2 = new FightingInputs(~replay.Seed);
            for (int frame = 0; frame < replay.Frames; frame++)
            {
                replay.Inputs.Add(new FrameInputs(player1.Next(), player2.Next()));
            }
        }

        replay.Frames = replay.Inputs.Count;
        replay.DataHash = TestmanMirror.Data.Hash.ToString();
        replay.Checksums.Clear();
        foreach ((int frame, ulong checksum) in Play(replay))
        {
            replay.Checksums[frame] = checksum;
        }

        replay.Write(path);
    }

    // Checksums after every checkpoint frame and the last one.
    private static SortedDictionary<int, ulong> Play(GoldenReplay replay)
    {
        MatchSimulation match = TestmanMirror.Create(replay.Player1, replay.Player2);
        using var driver = new ReplayMatchDriver<MatchState>(new ComposedMatchSimulation(match), replay.Inputs);
        var checksums = new SortedDictionary<int, ulong>();
        for (int frame = 1; frame <= replay.Inputs.Count; frame++)
        {
            Assert.True(driver.TryAdvanceFrame([]), $"Frame {frame} did not run.");
            if (frame % CheckpointFrames == 0 || frame == replay.Inputs.Count)
            {
                checksums[frame] = StateChecksum.Compute(in driver.State);
            }
        }

        return checksums;
    }

    private static string ReplayFolder() =>
        Path.Combine(TestmanMirror.RepositoryRoot(), "tests", "Netcode.Tests", "GoldenReplays", "Replays");
}
