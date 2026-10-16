using System;
using Netcode.Desync;
using Sim.Input;
using Xunit;

namespace Netcode.Tests.Desync;

public sealed class DesyncMonitorTests : IDisposable
{
    private const int HistoryFrames = 16;
    private const int Frames = 100;
    private const int DesyncFrame = 50;

    private readonly TemporaryFolder _folder = new();
    private readonly DesyncMonitor<ToyState> _player1;
    private readonly DesyncMonitor<ToyState> _player2;

    public DesyncMonitorTests()
    {
        (InMemoryChecksumLink link1, InMemoryChecksumLink link2) = InMemoryChecksumLink.CreatePair();
        var settings = new DesyncDumpSettings { Folder = _folder.Path, HistoryFrames = HistoryFrames };
        _player1 = new DesyncMonitor<ToyState>(settings, new DesyncDumpHeader("1.0", "ABCD", true), link1, TimeProvider.System);
        _player2 = new DesyncMonitor<ToyState>(settings, new DesyncDumpHeader("1.0", "ABCD", false), link2, TimeProvider.System);
    }

    [Fact]
    public void PeersThatAgreeRunOn()
    {
        for (int frame = 1; frame <= Frames; frame++)
        {
            Step(_player1, frame, desyncAt: 0);
            Step(_player2, frame, desyncAt: 0);
        }

        Assert.True(_player1.LastAgreedFrame >= Frames - 1);
        Assert.Equal(Frames, _player2.LastAgreedFrame);
    }

    [Fact]
    public void ADesyncMakesBothPeersWriteADumpAndThrow()
    {
        DesyncDetectedException? player1Desync = null;
        DesyncDetectedException? player2Desync = null;
        for (int frame = 1; frame <= Frames && (player1Desync is null || player2Desync is null); frame++)
        {
            player1Desync ??= Record.Exception(() => Step(_player1, frame, desyncAt: 0)) as DesyncDetectedException;
            player2Desync ??= Record.Exception(() => Step(_player2, frame, DesyncFrame)) as DesyncDetectedException;
        }

        Assert.NotNull(player1Desync?.DumpPath);
        Assert.NotNull(player2Desync?.DumpPath);
        DesyncDump player1Dump = DesyncDumpFile.Read(player1Desync.DumpPath);
        DesyncDump player2Dump = DesyncDumpFile.Read(player2Desync.DumpPath);

        Assert.Equal((1, 2), (player1Dump.LocalPlayer, player2Dump.LocalPlayer));
        Assert.All(new[] { player1Dump, player2Dump }, dump =>
        {
            Assert.Equal(DesyncFrame, dump.FirstMismatchFrame);
            Assert.Equal(DesyncFrame - 1, dump.LastAgreedFrame);
            Assert.Equal(DesyncFrame - 1, dump.LastAgreedState?.Frame);
            Assert.Equal(DesyncFrame, dump.FirstMismatchState?.Frame);
            Assert.Equal(DesyncFrame, dump.Inputs[^1].Frame);
            Assert.Equal((int)InputWord.Up, dump.Inputs[^1].Player1);
        });
        Assert.Equal(player1Dump.LocalChecksum, player2Dump.RemoteChecksum);
        Assert.Equal(DesyncFrame, player1Dump.FirstMismatchState!.Fields["Health"]);
        Assert.Equal(DesyncFrame + 1, player2Dump.FirstMismatchState!.Fields["Health"]);
    }

    public void Dispose() => _folder.Dispose();

    // Lockstep: every frame is final as soon as it runs. The state at frame f is (f, f), plus one on the desync frame.
    private static void Step(DesyncMonitor<ToyState> monitor, int frame, int desyncAt)
    {
        var state = new ToyState { Frame = frame, Health = frame == desyncAt ? frame + 1 : frame };
        monitor.RecordFrame(frame, in state, new InputWord(InputWord.Up), InputWord.Neutral);
        monitor.ConfirmThrough(frame);
    }

    private struct ToyState
    {
        public int Frame;
        public int Health;
    }
}
