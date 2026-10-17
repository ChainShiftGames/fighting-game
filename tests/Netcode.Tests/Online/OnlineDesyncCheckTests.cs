using System;
using System.Collections.Generic;
using System.IO;
using Backdash;
using Netcode.Desync;
using Netcode.Drivers;
using Netcode.Online;
using Netcode.Tests.Desync;
using Netcode.Tests.Transport;
using Netcode.Transport;
using Sim.State;
using Xunit;

namespace Netcode.Tests.Online;

/// <summary>
/// P2-09 wired into online matches: the driver's confirmed frame is exactly the final ones, and the per-frame desync
/// check over the match transport stays quiet through a normal match on a bad network (ADR-0005 §5, ADR-0019).
/// </summary>
public sealed class OnlineDesyncCheckTests : IDisposable
{
    private const int FightingFrames = 400;
    private const int LastFrame = 600;

    // Developer setting (data/netcode/online.json).
    private const int HistoryFrames = 600;

    private readonly TemporaryFolder _folder = new();

    [Fact]
    public void ConfirmedFramesNeverChangeAgainAndMatchTheOtherPc()
    {
        var player1Game = new ChecksumRecordingSimulation();
        var player2Game = new ChecksumRecordingSimulation();
        using var match = new InMemoryMatch(
            FightingFrames,
            player2Simulation: player2Game,
            network: NetworkSimulation.TestConditions,
            player1Simulation: player1Game);
        var player1Final = new FinalFrames(match.Player1Driver, player1Game);
        var player2Final = new FinalFrames(match.Player2Driver, player2Game);

        match.RunUntilFrame(LastFrame, () =>
        {
            player1Final.Update();
            player2Final.Update();
        });
        player1Final.Update();
        player2Final.Update();

        // Rollbacks happened, so predicted frames were rerun while the confirmed ones stayed put.
        Assert.True(match.Player1Driver.TryReadConnection(out ConnectionStats stats) && stats.Rollbacks > 0);
        player1Final.AssertUnchanged();
        player2Final.AssertUnchanged();
        int bothFinal = Math.Min(player1Final.Count, player2Final.Count);
        Assert.True(bothFinal > LastFrame - 60, $"Only {bothFinal} frames final on both PCs.");
        for (int frame = 1; frame <= bothFinal; frame++)
        {
            Assert.Equal(player1Final.ChecksumAt(frame), player2Final.ChecksumAt(frame));
        }
    }

    [Fact]
    public void ANormalMatchOnABadNetworkFindsNoDesyncAndWritesNoDump()
    {
        using var match = new InMemoryMatch(
            FightingFrames, network: NetworkSimulation.TestConditions, desyncDumps: Dumps());

        match.RunUntilFrame(LastFrame);

        // Compared over the transport right up to the newest final frames, not just started.
        Assert.True(match.Player1Driver.AgreedFrame > LastFrame - 60, $"P1 agreed through {match.Player1Driver.AgreedFrame}.");
        Assert.True(match.Player2Driver.AgreedFrame > LastFrame - 60, $"P2 agreed through {match.Player2Driver.AgreedFrame}.");
        Assert.False(Directory.Exists(_folder.Path) && Directory.GetFiles(_folder.Path).Length > 0);
        Assert.Equal(0, match.Player1Peer.Dropped.Malformed);
        Assert.Equal(0, match.Player2Peer.Dropped.Malformed);
    }

    [Fact]
    public void ADesyncMonitorNeedsTheSessionsConfirmedFrames()
    {
        var monitor = new DesyncMonitor<MatchState>(
            Dumps(), new DesyncDumpHeader("test", "data", true), InMemoryChecksumLink.CreatePair().Player1, TimeProvider.System);

        Assert.Throws<ArgumentException>(() => new RollbackMatchDriver<MatchState>(
            new ChecksumRecordingSimulation(), SyncTestSession.Build<MatchState>(8), desyncMonitor: monitor));
    }

    [Fact]
    public void ADesyncBackdashFindsGetsADumpAndIsThrownOnEveryLaterFrame()
    {
        using var match = new InMemoryMatch(FightingFrames, desyncDumps: Dumps());
        match.RunUntilFrame(100);
        var mismatch = new PeerEventInfo(PeerEvent.ChecksumMismatch)
        {
            ChecksumMismatch = new ChecksumMismatchEventInfo(new Frame(90), new Checksum(1), new Checksum(2)),
        };
        ((INetcodeSessionHandler)match.Player1Driver).OnPeerEvent(NetcodePlayer.CreateLocal(), in mismatch);

        DesyncDetectedException desync = Assert.Throws<DesyncDetectedException>(() => match.Player1.TryAdvanceFrame([]));
        DesyncDetectedException again = Assert.Throws<DesyncDetectedException>(() => match.Player1.TryAdvanceFrame([]));

        Assert.Contains("Backdash's consistency check", desync.Message, StringComparison.Ordinal);
        Assert.NotNull(desync.DumpPath);
        DesyncDump dump = DesyncDumpFile.Read(desync.DumpPath);
        Assert.Equal(90, dump.FirstMismatchFrame);
        Assert.NotNull(dump.FirstMismatchState);
        Assert.Same(desync, again);
        Assert.Single(Directory.GetFiles(_folder.Path));
    }

    public void Dispose() => _folder.Dispose();

    private DesyncDumpSettings Dumps() => new() { Folder = _folder.Path, HistoryFrames = HistoryFrames };

    /// <summary>One PC's final frames as its driver reports them, with each one's checksum when it became final.</summary>
    private sealed class FinalFrames
    {
        private readonly RollbackMatchDriver<MatchState> _driver;
        private readonly ChecksumRecordingSimulation _game;
        private readonly List<ulong> _whenFinal = [];

        public FinalFrames(RollbackMatchDriver<MatchState> driver, ChecksumRecordingSimulation game)
        {
            _driver = driver;
            _game = game;
        }

        public int Count => _whenFinal.Count;

        public ulong ChecksumAt(int frame) => _whenFinal[frame - 1];

        public void Update()
        {
            int confirmed = _driver.ConfirmedFrame;
            Assert.True(confirmed >= _whenFinal.Count, $"The confirmed frame went back from {_whenFinal.Count} to {confirmed}.");
            Assert.True(confirmed <= _driver.CurrentFrame);
            for (int frame = _whenFinal.Count + 1; frame <= confirmed; frame++)
            {
                _whenFinal.Add(_game.ChecksumAt(frame));
            }
        }

        public void AssertUnchanged()
        {
            for (int frame = 1; frame <= _whenFinal.Count; frame++)
            {
                Assert.True(_whenFinal[frame - 1] == _game.ChecksumAt(frame), $"Frame {frame} changed after it was final.");
            }
        }
    }
}
