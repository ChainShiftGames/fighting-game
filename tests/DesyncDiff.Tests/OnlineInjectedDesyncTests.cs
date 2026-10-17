using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Threading;
using Netcode.Desync;
using Netcode.Drivers;
using Netcode.Handshake;
using Netcode.Online;
using Netcode.Tests.Matches;
using Netcode.Tests.Transport;
using Netcode.Transport;
using Sim.State;
using Xunit;

namespace DesyncDiff.Tests;

/// <summary>
/// P2-09 in online play: a deliberately injected desync between two online peers, through the real online driver and
/// its checksum link over the match transport (ADR-0019), makes both peers write a dump, and DesyncDiff names the
/// injected field.
/// </summary>
public sealed class OnlineInjectedDesyncTests : IDisposable
{
    private const int DesyncFrame = 150;
    private const int RollbackFrames = 8;

    // Developer setting (data/netcode/online.json).
    private const int HistoryFrames = 600;
    private const uint Player1Seed = 20261017;
    private const uint Player2Seed = 71016202;

    // This test's own loopback ports, apart from every other test's.
    private const int Player1Port = 47361;
    private const int Player2Port = 47362;

    private static readonly TimeSpan _timeout = TimeSpan.FromSeconds(60);

    private readonly string _folder =
        Path.Combine(Path.GetTempPath(), "fg-desync-tests", Guid.NewGuid().ToString("N"), DesyncDumpSettings.DefaultFolderName);

    [Fact]
    public void OverALossyInMemoryLinkBothPeersDumpAndTheToolNamesTheField()
    {
        (InMemoryTransport player1, InMemoryTransport player2) = InMemoryTransport.CreatePair();

        // Only names for the sessions: the in-memory transports ignore addresses.
        (string player1Dump, string player2Dump) = PlayUntilBothFindTheDesync(
            Settings(new IPEndPoint(IPAddress.Loopback, 2), true, NetworkSimulation.TestConditions),
            new PeerTransport(new OpenedTransport(player1)),
            Settings(new IPEndPoint(IPAddress.Loopback, 1), false, NetworkSimulation.TestConditions),
            new PeerTransport(new OpenedTransport(player2)));

        AssertTheToolNamesTheInjectedField(player1Dump, player2Dump);
    }

    [Fact]
    public void OverLoopbackUdpBothPeersDumpAndTheToolNamesTheField()
    {
        (string player1Dump, string player2Dump) = PlayUntilBothFindTheDesync(
            Settings(new IPEndPoint(IPAddress.Loopback, Player2Port), true, NetworkSimulation.None) with
            {
                LocalPort = Player1Port,
            },
            PeerTransport.DirectUdp(),
            Settings(new IPEndPoint(IPAddress.Loopback, Player1Port), false, NetworkSimulation.None) with
            {
                LocalPort = Player2Port,
            },
            PeerTransport.DirectUdp());

        AssertTheToolNamesTheInjectedField(player1Dump, player2Dump);
    }

    public void Dispose()
    {
        string root = Path.GetDirectoryName(_folder)!;
        if (Directory.Exists(root))
        {
            Directory.Delete(root, recursive: true);
        }
    }

    private static void AssertTheToolNamesTheInjectedField(string player1Dump, string player2Dump)
    {
        var output = new StringWriter();

        int exitCode = DesyncDiffCommand.Run([player1Dump, player2Dump], output, new StringWriter());

        Assert.Equal(DesyncDiffCommand.Compared, exitCode);
        string report = output.ToString();
        Assert.Contains($"First mismatched frame: {DesyncFrame} (last agreed: {DesyncFrame - 1})", report);
        Assert.Contains($"Frame {DesyncFrame - 1}: states identical.", report);
        Assert.Contains($"Frame {DesyncFrame}: 1 field(s) differ:", report);
        Assert.Contains($"  {InjectedDesyncSimulation.ChangedField}: A=", report);
        Assert.DoesNotContain("Warning", report);
    }

    private static VersionHandshake Handshake() =>
        new(new MatchIdentity("test", TestmanMatch.Data.Hash.ToBytes(), TestmanMatch.Data.CreateMatchRules()));

    // Each peer goes on after the other stopped, as the second PC does: it must still get the first one's checksums.
    private static (string Player1Dump, string Player2Dump) PlayUntilBothFindTheDesync(
        PeerMatchSettings player1Settings,
        PeerTransport player1Transport,
        PeerMatchSettings player2Settings,
        PeerTransport player2Transport)
    {
        using var player1 = new TestMatchDriver<MatchState>(
            PeerSession.CreateDriver(
                new ComposedMatchSimulation(TestmanMatch.Create()), player1Settings, player1Transport, Handshake()),
            [new FightingInputs(Player1Seed)]);
        using var player2 = new TestMatchDriver<MatchState>(
            PeerSession.CreateDriver(
                new InjectedDesyncSimulation(new ComposedMatchSimulation(TestmanMatch.Create()), DesyncFrame),
                player2Settings,
                player2Transport,
                Handshake()),
            [new FightingInputs(Player2Seed)]);
        DesyncDetectedException? player1Desync = null;
        DesyncDetectedException? player2Desync = null;
        var clock = Stopwatch.StartNew();
        while (player1Desync is null || player2Desync is null)
        {
            Assert.True(
                clock.Elapsed < _timeout,
                $"P1 {player1Desync?.Message ?? "found nothing"} at frame {player1.CurrentFrame}; " +
                $"P2 {player2Desync?.Message ?? "found nothing"} at frame {player2.CurrentFrame}.");
            player1Desync ??= TryStep(player1);
            player2Desync ??= TryStep(player2);
            Thread.Sleep(1);
        }

        return (AssertDumped(player1Desync), AssertDumped(player2Desync));
    }

    private static DesyncDetectedException? TryStep(TestMatchDriver<MatchState> peer)
    {
        try
        {
            peer.TryAdvanceFrame([]);
            return null;
        }
        catch (DesyncDetectedException desync)
        {
            return desync;
        }
    }

    private static string AssertDumped(DesyncDetectedException desync)
    {
        Assert.NotNull(desync.DumpPath);
        Assert.True(File.Exists(desync.DumpPath));
        Assert.Contains(desync.DumpPath, desync.Message, StringComparison.Ordinal);
        return desync.DumpPath;
    }

    private PeerMatchSettings Settings(EndPoint opponent, bool localIsPlayer1, NetworkSimulation network) => new()
    {
        RemoteEndPoint = opponent,
        LocalIsPlayer1 = localIsPlayer1,
        RollbackFrames = RollbackFrames,
        NetworkSimulation = network,
        DesyncDumps = new DesyncDumpSettings { Folder = _folder, HistoryFrames = HistoryFrames },
    };
}
