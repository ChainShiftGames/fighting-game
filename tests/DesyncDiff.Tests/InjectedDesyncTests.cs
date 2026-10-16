using System;
using System.IO;
using Netcode.Desync;
using Netcode.Drivers;
using Sim.Input;
using Sim.State;
using Xunit;

namespace DesyncDiff.Tests;

/// <summary>
/// P2-09's check: a deliberately injected desync between two in-process peers of the real match produces a dump on
/// each side, and DesyncDiff names the exact field that differs.
/// </summary>
public sealed class InjectedDesyncTests : IDisposable
{
    private const int DesyncFrame = 150;
    private const int MaxFrames = 300;

    // Developer setting (DesyncDumpSettings.HistoryFrames); lockstep peers only need a few frames.
    private const int HistoryFrames = 32;

    private readonly string _folder =
        Path.Combine(Path.GetTempPath(), "fg-desync-tests", Guid.NewGuid().ToString("N"), DesyncDumpSettings.DefaultFolderName);

    [Fact]
    public void BothPeersDumpAndTheToolNamesTheInjectedField()
    {
        (InMemoryChecksumLink link1, InMemoryChecksumLink link2) = InMemoryChecksumLink.CreatePair();
        using var player1 = new LockstepPeer(new ComposedMatchSimulation(TestmanMatch.Create()), Monitor(link1, true));
        using var player2 = new LockstepPeer(
            new InjectedDesyncSimulation(new ComposedMatchSimulation(TestmanMatch.Create()), DesyncFrame),
            Monitor(link2, false));

        for (int frame = 1; frame <= MaxFrames && (player1.Desync is null || player2.Desync is null); frame++)
        {
            (InputWord first, InputWord second) = Inputs(frame);
            player1.Step(first, second);
            player2.Step(first, second);
        }

        string player1Dump = AssertDumped(player1);
        string player2Dump = AssertDumped(player2);
        var output = new StringWriter();
        var error = new StringWriter();

        int exitCode = DesyncDiffCommand.Run([player1Dump, player2Dump], output, error);

        Assert.Equal(DesyncDiffCommand.Compared, exitCode);
        string report = output.ToString();
        Assert.Contains($"First mismatched frame: {DesyncFrame} (last agreed: {DesyncFrame - 1})", report);
        Assert.Contains($"Frame {DesyncFrame - 1}: states identical.", report);
        Assert.Contains($"Frame {DesyncFrame}: 1 field(s) differ:", report);
        Assert.Contains($"  {InjectedDesyncSimulation.ChangedField}: A=", report);
        Assert.DoesNotContain("Warning", report);
    }

    public void Dispose()
    {
        string root = Path.GetDirectoryName(_folder)!;
        if (Directory.Exists(root))
        {
            Directory.Delete(root, recursive: true);
        }
    }

    private static string AssertDumped(LockstepPeer peer)
    {
        Assert.NotNull(peer.Desync);
        Assert.NotNull(peer.Desync.DumpPath);
        Assert.True(File.Exists(peer.Desync.DumpPath));
        return peer.Desync.DumpPath;
    }

    // Both walk in and press buttons, so the frames before the desync are real gameplay, not idle.
    private static (InputWord Player1, InputWord Player2) Inputs(int frame)
    {
        ushort button = (ushort)(1 << (InputWord.FirstButtonBit + (frame / 20 % 4)));
        ushort player1 = frame % 20 == 0 ? (ushort)(InputWord.Right | button) : InputWord.Right;
        ushort player2 = frame % 25 == 0 ? (ushort)(InputWord.Left | button) : InputWord.Left;
        return (new InputWord(player1), new InputWord(player2));
    }

    private DesyncMonitor<MatchState> Monitor(InMemoryChecksumLink link, bool localIsPlayer1) => new(
        new DesyncDumpSettings { Folder = _folder, HistoryFrames = HistoryFrames },
        new DesyncDumpHeader("test", "test-data", localIsPlayer1),
        link,
        TimeProvider.System);
}
