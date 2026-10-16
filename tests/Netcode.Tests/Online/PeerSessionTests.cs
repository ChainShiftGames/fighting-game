using System;
using System.Diagnostics;
using System.Net;
using System.Threading;
using Netcode.Desync;
using Netcode.Drivers;
using Netcode.Online;
using Netcode.Tests.Drivers;
using Netcode.Tests.Handshake;
using Netcode.Tests.Matches;
using Netcode.Transport;
using Sim.State;
using Xunit;

namespace Netcode.Tests.Online;

/// <summary>
/// The real match online (P2-02): two Testman mirror peers in one process over loopback UDP with the ADR-0005
/// bad-network test conditions, each played by seeded fighting inputs through the test driver.
/// </summary>
public sealed class PeerSessionTests
{
    private const int FightingFrames = 900;
    private const int SettleFrames = 60;

    // The rollback window (developer setting, data/netcode/online.json).
    private const int RollbackFrames = 8;
    private const uint Player1Seed = 20261017;
    private const uint Player2Seed = 71016202;
    private static readonly TimeSpan _timeout = TimeSpan.FromSeconds(90);

    [Fact]
    public void BothPeersEndInTheSameStateOverABadNetwork()
    {
        using var player1 = CreatePeer(47011, 47012, true, new ComposedMatchSimulation(TestmanMirror.Create()), out _);
        using var player2 = CreatePeer(47012, 47011, false, new ComposedMatchSimulation(TestmanMirror.Create()), out _);

        RunUntil(player1, player2, FightingFrames + SettleFrames);

        // Inputs go neutral after FightingFrames, so by the end every frame both states depend on is confirmed.
        Assert.Equal(StateChecksum.Compute(in player1.State), StateChecksum.Compute(in player2.State));
    }

    [Fact]
    public void ConnectionStatsShowThePingAndTheRollbacksOfABadNetwork()
    {
        using var player1 = CreatePeer(
            47031, 47032, true, new ComposedMatchSimulation(TestmanMirror.Create()), out IConnectionReadout readout);
        using var player2 = CreatePeer(47032, 47031, false, new ComposedMatchSimulation(TestmanMirror.Create()), out _);

        RunUntil(player1, player2, FightingFrames);

        Assert.True(readout.TryReadConnection(out ConnectionStats stats));

        // Backdash's ping is a sample, not the true round trip: it can read far lower (8 ms seen in CI here), so the
        // test only checks that one is reported (ADR-0014, problem 4).
        Assert.True(stats.PingMilliseconds > 0);
        Assert.True(stats.Rollbacks > 0, "Fighting inputs at 100 ms should mispredict.");
        Assert.InRange(stats.LongestRollbackFrames, 1, RollbackFrames);
        Assert.True(stats.RolledBackFrames >= stats.Rollbacks);
    }

    [Fact]
    public void ADesyncBetweenPeersIsCaught()
    {
        using var player1 = CreatePeer(47021, 47022, true, new ComposedMatchSimulation(TestmanMirror.Create()), out _);
        using var player2 = CreatePeer(47022, 47021, false, new NonDeterministicSimulation(), out _);

        Assert.Throws<DesyncDetectedException>(() => RunUntil(player1, player2, int.MaxValue));
    }

    private static TestMatchDriver<MatchState> CreatePeer(
        int localPort,
        int remotePort,
        bool localIsPlayer1,
        IMatchSimulation<MatchState> simulation,
        out IConnectionReadout readout)
    {
        var settings = new PeerMatchSettings
        {
            LocalPort = localPort,
            RemoteEndPoint = new IPEndPoint(IPAddress.Loopback, remotePort),
            LocalIsPlayer1 = localIsPlayer1,
            RollbackFrames = RollbackFrames,
            NetworkSimulation = NetworkSimulation.TestConditions,
        };
        RollbackMatchDriver<MatchState> driver = PeerSession.CreateDriver(simulation, settings, PeerTransport.DirectUdp(), SameBuild.Handshake());
        readout = driver;
        var inputs = new FightingInputs(localIsPlayer1 ? Player1Seed : Player2Seed);
        return new TestMatchDriver<MatchState>(driver, [new NeutralAfter(inputs, FightingFrames)]);
    }

    private static void RunUntil(TestMatchDriver<MatchState> player1, TestMatchDriver<MatchState> player2, int frames)
    {
        var clock = Stopwatch.StartNew();
        while (player1.CurrentFrame < frames || player2.CurrentFrame < frames)
        {
            Assert.True(clock.Elapsed < _timeout, $"Stuck at frames {player1.CurrentFrame} / {player2.CurrentFrame}.");
            if (player1.CurrentFrame < frames)
            {
                player1.TryAdvanceFrame([]);
            }

            if (player2.CurrentFrame < frames)
            {
                player2.TryAdvanceFrame([]);
            }

            // Roughly the game's pace, so the session sees time pass between frames.
            Thread.Sleep(1);
        }
    }
}
