using System;
using System.Diagnostics;
using System.Net;
using System.Threading;
using Backdash;
using Backdash.Core;
using Netcode.Desync;
using Netcode.Drivers;
using Netcode.Online;
using Netcode.Tests.Handshake;
using Netcode.Tests.Matches;
using Netcode.Tests.Transport;
using Netcode.Transport;
using Sim.Input;
using Sim.State;
using Xunit;

namespace Netcode.Tests.Online;

/// <summary>
/// Input delay is each PC's own business (ADR-0005 §4): two real Testman peers over in-memory transports end in the
/// same state with different delays, and when each lowers its delay mid-match.
/// </summary>
public sealed class LocalInputDelayTests
{
    private const int FightingFrames = 400;
    private const int LastFrame = 600;
    private const int DelayChangeFrame = 200;
    private const int StartingDelay = 2;

    // The rollback window (developer setting, data/netcode/online.json).
    private const int RollbackFrames = 8;
    private const uint Player1Seed = 20261017;
    private const uint Player2Seed = 71016202;
    private static readonly TimeSpan _timeout = TimeSpan.FromSeconds(60);

    // Only names for the session: the in-memory transports ignore addresses.
    private static readonly IPEndPoint _player1Address = new(IPAddress.Loopback, 1);
    private static readonly IPEndPoint _player2Address = new(IPAddress.Loopback, 2);

    [Fact]
    public void PeersWithDifferentPlayerDelaysEndInTheSameState()
    {
        (InMemoryTransport player1Transport, InMemoryTransport player2Transport) = InMemoryTransport.CreatePair();
        using InputDelayMatchDriver player1Driver = CreateDriver(player1Transport, _player2Address, true, playerOverride: 0);
        using InputDelayMatchDriver player2Driver = CreateDriver(player2Transport, _player1Address, false, playerOverride: 4);
        var player1 = new TestMatchDriver<MatchState>(player1Driver, [Inputs(true)]);
        var player2 = new TestMatchDriver<MatchState>(player2Driver, [Inputs(false)]);

        RunUntilFrame(player1, player2, LastFrame, betweenFrames: null);

        Assert.Equal((0, 4), (player1Driver.InputDelayFrames, player2Driver.InputDelayFrames));
        Assert.Equal(StateChecksum.Compute(in player1.State), StateChecksum.Compute(in player2.State));
    }

    [Fact]
    public void LoweringEachPeersDelayMidMatchOverABadNetworkKeepsBothInTheSameState()
    {
        (InMemoryTransport player1Transport, InMemoryTransport player2Transport) = InMemoryTransport.CreatePair();
        INetcodeSession<InputWord> player1Session = BuildSession(player1Transport, _player2Address, true);
        INetcodeSession<InputWord> player2Session = BuildSession(player2Transport, _player1Address, false);
        using TestMatchDriver<MatchState> player1 = Peer(player1Session, true);
        using TestMatchDriver<MatchState> player2 = Peer(player2Session, false);
        bool changed = false;

        // Mid-fight on purpose, the harshest case: players' delays only change between rounds.
        RunUntilFrame(player1, player2, LastFrame, () =>
        {
            if (!changed && player1.CurrentFrame >= DelayChangeFrame && player2.CurrentFrame >= DelayChangeFrame)
            {
                new SessionInputDelay(player1Session).LowerLocalInputDelay(0);
                new SessionInputDelay(player2Session).LowerLocalInputDelay(1);
                changed = true;
            }
        });

        Assert.True(changed);
        Assert.Equal(StateChecksum.Compute(in player1.State), StateChecksum.Compute(in player2.State));
    }

    /// <summary>
    /// Why a running match never raises its delay (IInputDelayControl). Backdash 0.7.8 fills the frame a raise skips
    /// with a copy of the input before it on this PC, but sends nothing for it, so the other PC reads each following
    /// input one frame early until its acknowledgements catch up. Backdash only notices when an acknowledgement lands
    /// on the skipped frame, which depends on timing; otherwise the raise goes through and the two PCs play
    /// different inputs. Rerun on a Backdash upgrade: when this fails, raising works and the automatic delay could
    /// follow a rising ping within a match.
    /// </summary>
    [Fact]
    public void RaisingTheDelayOfARunningMatchBreaksIt()
    {
        (InMemoryTransport player1Transport, InMemoryTransport player2Transport) = InMemoryTransport.CreatePair();
        INetcodeSession<InputWord> player1Session = BuildSession(player1Transport, _player2Address, true);
        INetcodeSession<InputWord> player2Session = BuildSession(player2Transport, _player1Address, false);
        var player1Game = new InputRecordingSimulation(LastFrame);
        var player2Game = new InputRecordingSimulation(LastFrame);

        // Player 1's input changes every frame, so an input read one frame early always differs.
        using TestMatchDriver<MatchState> player1 = new(
            new RollbackMatchDriver<MatchState>(player1Game, player1Session), [new ChangingInputs()]);
        using TestMatchDriver<MatchState> player2 = new(
            new RollbackMatchDriver<MatchState>(player2Game, player2Session), [Inputs(false)]);
        RunUntilFrame(player1, player2, DelayChangeFrame, betweenFrames: null);

        player1Session.SetFrameDelay(StartingDelay + 1);
        Exception? failure = Record.Exception(() => RunUntilFrame(player1, player2, LastFrame, betweenFrames: null));

        switch (failure)
        {
            case NetcodeAssertionException:
                // An acknowledgement landed on the skipped frame: Backdash rejects the raise.
                return;
            case DesyncDetectedException:
                // Backdash's consistency check caught the two PCs disagreeing: the raise desynced the match.
                return;
            default:
                // The raise went through unnoticed. The skipped frame is where the PCs start to disagree about
                // player 1's inputs: this PC repeats the input before it, the other PC already reads the next one.
                Assert.Null(failure);
                Assert.Equal(DelayChangeFrame + StartingDelay, FirstFrameWithDifferentPlayer1Input(player1Game, player2Game));
                return;
        }
    }

    private static InputDelayMatchDriver CreateDriver(
        InMemoryTransport transport, EndPoint opponent, bool localIsPlayer1, int playerOverride)
    {
        var delay = new InputDelaySettings
        {
            Automatic = new InputDelayTuning
            {
                MinimumFrames = 1,
                MaximumFrames = 2,
                PingStepMilliseconds = 50,
                PingSampleIntervalFrames = 60,
                PingSamples = 15,
            },
            PlayerOverrideFrames = playerOverride,
        };
        return PeerSession.CreateDriver(
            new ComposedMatchSimulation(TestmanMirror.Create()),
            Settings(opponent, localIsPlayer1),
            delay,
            new PeerTransport(new OpenedTransport(transport)),
            SameBuild.Handshake());
    }

    private static INetcodeSession<InputWord> BuildSession(InMemoryTransport transport, EndPoint opponent, bool localIsPlayer1) =>
        PeerSession.Build<MatchState>(
            Settings(opponent, localIsPlayer1) with { InputDelayFrames = StartingDelay },
            new PeerTransport(new OpenedTransport(transport)),
            SameBuild.Handshake());

    private static TestMatchDriver<MatchState> Peer(INetcodeSession<InputWord> session, bool player1) => new(
        new RollbackMatchDriver<MatchState>(new ComposedMatchSimulation(TestmanMirror.Create()), session),
        [Inputs(player1)]);

    // ADR-0005's bad-network test conditions, so delayed inputs are still in flight when a delay differs or changes.
    private static PeerMatchSettings Settings(EndPoint opponent, bool localIsPlayer1) => new()
    {
        RemoteEndPoint = opponent,
        LocalIsPlayer1 = localIsPlayer1,
        RollbackFrames = RollbackFrames,
        NetworkSimulation = NetworkSimulation.TestConditions,
    };

    private static NeutralAfter Inputs(bool player1) =>
        new(new FightingInputs(player1 ? Player1Seed : Player2Seed), FightingFrames);

    // Only frames well before the end, whose inputs both PCs have confirmed.
    private static int FirstFrameWithDifferentPlayer1Input(InputRecordingSimulation player1, InputRecordingSimulation player2)
    {
        for (int frame = 0; frame < LastFrame - RollbackFrames; frame++)
        {
            if (player1.Player1At(frame) != player2.Player1At(frame))
            {
                return frame;
            }
        }

        return -1;
    }

    // Each peer stops at exactly lastFrame, so the two states can be compared.
    private static void RunUntilFrame(
        TestMatchDriver<MatchState> player1, TestMatchDriver<MatchState> player2, int lastFrame, Action? betweenFrames)
    {
        var clock = Stopwatch.StartNew();
        while (player1.CurrentFrame < lastFrame || player2.CurrentFrame < lastFrame)
        {
            Assert.True(clock.Elapsed < _timeout, $"Stuck at frames {player1.CurrentFrame} / {player2.CurrentFrame}.");
            if (player1.CurrentFrame < lastFrame)
            {
                player1.TryAdvanceFrame([]);
            }

            if (player2.CurrentFrame < lastFrame)
            {
                player2.TryAdvanceFrame([]);
            }

            betweenFrames?.Invoke();

            // Roughly the game's pace, so the sessions see time pass between frames.
            Thread.Sleep(1);
        }
    }
}
