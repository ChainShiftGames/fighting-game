using System;
using System.Diagnostics;
using System.Net;
using System.Threading;
using Netcode.Drivers;
using Netcode.Online;
using Netcode.Transport;
using Sim.Composition;
using Sim.Input;
using Sim.State;
using Xunit;

namespace Netcode.Tests.Matches;

/// <summary>
/// The real match online (P2-02): two Testman mirror peers in one process over loopback UDP with the ADR-0005
/// bad-network test conditions, each played by seeded fighting inputs through the test driver.
/// </summary>
public sealed class MatchPeerSessionTests
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
        using var player1 = CreatePeer(47041, 47042, true, new FightingInputs(Player1Seed), out IConnectionReadout readout);
        using var player2 = CreatePeer(47042, 47041, false, new FightingInputs(Player2Seed), out _);

        RunUntil(player1, player2, FightingFrames + SettleFrames);

        // Inputs go neutral after FightingFrames, so by the end every frame both states depend on is confirmed.
        Assert.Equal(StateChecksum.Compute(in player1.State), StateChecksum.Compute(in player2.State));

        // Fighting inputs at 100 ms mispredict, so the real match was rolled back and resimulated on the way.
        Assert.True(readout.TryReadConnection(out ConnectionStats stats));
        Assert.True(stats.Rollbacks > 0, "No rollback happened, so the test proves little.");
        Assert.InRange(stats.LongestRollbackFrames, 1, RollbackFrames);
    }

    private static TestMatchDriver<MatchState> CreatePeer(
        int localPort, int remotePort, bool localIsPlayer1, FightingInputs inputs, out IConnectionReadout readout)
    {
        MatchSimulation match = TestmanMirror.Create();
        var settings = new PeerMatchSettings
        {
            LocalPort = localPort,
            RemoteEndPoint = new IPEndPoint(IPAddress.Loopback, remotePort),
            LocalIsPlayer1 = localIsPlayer1,
            RollbackFrames = RollbackFrames,
            NetworkSimulation = NetworkSimulation.TestConditions,
        };
        RollbackMatchDriver<MatchState> driver =
            PeerSession.CreateDriver(new ComposedMatchSimulation(match), settings, PeerTransport.DirectUdp);
        readout = driver;
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

    // Plays the source for a number of frames, then lets go of everything.
    private sealed class NeutralAfter : IInputSource
    {
        private readonly IInputSource _source;
        private int _framesLeft;

        public NeutralAfter(IInputSource source, int frames)
        {
            _source = source;
            _framesLeft = frames;
        }

        public InputWord Next()
        {
            if (_framesLeft == 0)
            {
                return InputWord.Neutral;
            }

            _framesLeft--;
            return _source.Next();
        }
    }
}
