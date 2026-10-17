using System;
using System.Linq;
using System.Runtime.InteropServices;
using Netcode.Drivers;
using Netcode.Replays;
using Netcode.Tests.Drivers;
using Sim.Rules;
using Xunit;

namespace Netcode.Tests.Replays;

/// <summary>Replay files written by <see cref="ReplayWriter"/>: round trips and every way a file gets rejected.</summary>
public sealed class ReplayReaderTests
{
    private const int Seed = 1016;

    // Magic and format stay readable; everything after them is compressed.
    private const int FormatBytes = 6;

    [Fact]
    public void ReadsBackWhatWasWritten()
    {
        Replay written = RandomReplay(frames: 600);

        Replay read = RecordedMatches.Load(RecordedMatches.Save(written));

        Assert.Equal(RecordedMatches.GameVersion, read.Header.GameVersion);
        Assert.Equal(RecordedMatches.DataHash, read.Header.DataHash.ToArray());
        Assert.Equal(written.Header.RulesHash, read.Header.RulesHash);
        Assert.Equal(written.Header.Player1, read.Header.Player1);
        Assert.Equal(written.Header.Player2, read.Header.Player2);
        Assert.Equal(written.Inputs, read.Inputs);
    }

    [Fact]
    public void KeepsTheRulesTheMatchWasPlayedBy()
    {
        // A developer tweaked a rule before recording; playback must use the tweaked rules, not today's data.
        MatchRules rules = RecordedMatches.Header().Rules;
        rules.RoundTimerFrames += 60;
        var header = new ReplayHeader(
            RecordedMatches.GameVersion, RecordedMatches.DataHash, in rules, new("testman", 2), new("testman", 3));

        Replay read = RecordedMatches.Load(RecordedMatches.Save(new Replay(header, [])));

        Assert.Equal(rules.RoundTimerFrames, read.Header.Rules.RoundTimerFrames);
        Assert.Equal(rules.ComputeHash(), read.Header.RulesHash);
    }

    [Fact]
    public void ReadsAnEmptyReplay()
    {
        Replay read = RecordedMatches.Load(RecordedMatches.Save(new Replay(RecordedMatches.Header(), [])));

        Assert.Empty(read.Inputs);
    }

    [Fact]
    public void CompressesHeldInputs()
    {
        // A minute of both players holding still: far smaller than the raw 4 bytes per frame.
        var inputs = new FrameInputs[3600];

        byte[] file = RecordedMatches.Save(new Replay(RecordedMatches.Header(), inputs));

        Assert.True(file.Length < 300, $"A minute of neutral took {file.Length} bytes.");
    }

    [Fact]
    public void RejectsAReplayFromAnotherGameVersion()
    {
        byte[] file = RecordedMatches.Save(RandomReplay(frames: 60));

        var rejection = Assert.Throws<ReplayRejectedException>(() => RecordedMatches.Load(file, gameVersion: "1.0.1+other"));

        Assert.Equal(ReplayRejection.OtherGameVersion, rejection.Reason);
        Assert.Contains(RecordedMatches.GameVersion, rejection.Message, StringComparison.Ordinal);
        Assert.Contains("1.0.1+other", rejection.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void RejectsAReplayOfOtherGameData()
    {
        byte[] file = RecordedMatches.Save(RandomReplay(frames: 60));
        byte[] otherData = (byte[])RecordedMatches.DataHash.Clone();
        otherData[0] ^= 1;

        var rejection = Assert.Throws<ReplayRejectedException>(() => RecordedMatches.Load(file, dataHash: otherData));

        Assert.Equal(ReplayRejection.OtherGameData, rejection.Reason);
    }

    [Fact]
    public void RejectsAReplayOfAnotherFormat()
    {
        byte[] file = RecordedMatches.Save(RandomReplay(frames: 60));
        file[4] = 99;

        var rejection = Assert.Throws<ReplayRejectedException>(() => RecordedMatches.Load(file));

        Assert.Equal(ReplayRejection.OtherFormat, rejection.Reason);
    }

    [Theory]
    [InlineData(new byte[0])]
    [InlineData(new byte[] { (byte)'F', (byte)'G' })]
    [InlineData(new byte[] { 0x50, 0x4b, 0x03, 0x04, 0x14, 0x00, 0x00, 0x00 })]
    public void RejectsAFileThatIsNoReplay(byte[] file)
    {
        var rejection = Assert.Throws<ReplayRejectedException>(() => RecordedMatches.Load(file));

        Assert.Equal(ReplayRejection.NotAReplay, rejection.Reason);
    }

    [Fact]
    public void RejectsEveryCutOffFileThatLostAnInput()
    {
        Replay written = RandomReplay(frames: 120);
        byte[] file = RecordedMatches.Save(written);

        for (int length = FormatBytes; length < file.Length; length++)
        {
            Replay? read = LoadOrRejectAsDamaged(file[..length]);

            // .NET's zlib reader ignores a missing stream end (last block marker, checksum): a file cut there still
            // holds every input and plays the same match. Any other cut must be rejected.
            Assert.True(read is null || read.Inputs.SequenceEqual(written.Inputs), $"Cut to {length} bytes it read wrong.");
        }

        Assert.Null(LoadOrRejectAsDamaged(file[..(file.Length / 2)]));
    }

    [Fact]
    public void RejectsEveryCorruptedByteWithoutCrashing()
    {
        Replay written = RandomReplay(frames: 120);
        byte[] file = RecordedMatches.Save(written);
        byte[] writtenRules = RulesBytes(written.Header.Rules);

        for (int position = FormatBytes; position < file.Length; position++)
        {
            byte[] corrupted = (byte[])file.Clone();
            corrupted[position] ^= 0x5a;
            Replay? read = LoadOrReject(corrupted);

            // Some deflate bits don't change the output (e.g. unused code lengths); a flip there still decodes to the
            // same replay and passes the checksum. Any flip that changes what is read must be rejected.
            Assert.True(
                read is null ||
                (read.Inputs.SequenceEqual(written.Inputs) && RulesBytes(read.Header.Rules).SequenceEqual(writtenRules)),
                $"Byte {position} flipped, it read wrong.");
        }
    }

    private static byte[] RulesBytes(in MatchRules rules) =>
        MemoryMarshal.AsBytes(new ReadOnlySpan<MatchRules>(in rules)).ToArray();

    private static Replay? LoadOrReject(byte[] file)
    {
        try
        {
            return RecordedMatches.Load(file);
        }
        catch (ReplayRejectedException)
        {
            return null;
        }
    }

    private static Replay? LoadOrRejectAsDamaged(byte[] file)
    {
        try
        {
            return RecordedMatches.Load(file);
        }
        catch (ReplayRejectedException rejection) when (rejection.Reason == ReplayRejection.Damaged)
        {
            return null;
        }
    }

    private static Replay RandomReplay(int frames)
    {
        var inputs = new SeededInputs(Seed);
        var recording = new FrameInputs[frames];
        for (int frame = 0; frame < frames; frame++)
        {
            recording[frame] = new FrameInputs(inputs.Next(), inputs.Next());
        }

        return new Replay(RecordedMatches.Header(), recording);
    }
}
