using System;
using System.Diagnostics;
using System.Net;
using System.Threading;
using Netcode.Drivers;
using Netcode.Handshake;
using Netcode.Online;
using Netcode.Tests.Handshake;
using Netcode.Tests.Matches;
using Sim.State;
using Xunit;

namespace Netcode.Tests.Transport;

/// <summary>
/// Two Testman mirror peers in this process over an <see cref="InMemoryTransport"/> pair, each played by seeded
/// fighting inputs that go neutral after a number of frames. No ports, so tests using it never collide.
/// </summary>
internal sealed class InMemoryMatch : IDisposable
{
    // The rollback window (developer setting, data/netcode/online.json).
    private const int RollbackFrames = 8;
    private const uint Player1Seed = 20261017;
    private const uint Player2Seed = 71016202;
    private static readonly TimeSpan _timeout = TimeSpan.FromSeconds(60);

    // Only names for the session: the in-memory transports ignore addresses.
    private static readonly IPEndPoint _player1Address = new(IPAddress.Loopback, 1);
    private static readonly IPEndPoint _player2Address = new(IPAddress.Loopback, 2);

    /// <param name="player2Simulation">P2's game logic; the same as P1's when null.</param>
    /// <param name="player2Identity">What P2 tells the handshake; the same build as P1 when null.</param>
    public InMemoryMatch(
        int fightingFrames,
        IMatchSimulation<MatchState>? player2Simulation = null,
        MatchIdentity? player2Identity = null)
    {
        (Player1Transport, Player2Transport) = InMemoryTransport.CreatePair();
        Player1Peer = new PeerTransport(new OpenedTransport(Player1Transport));
        Player2Peer = new PeerTransport(new OpenedTransport(Player2Transport));
        Player1Handshake = SameBuild.Handshake();
        Player2Handshake = player2Identity is null ? SameBuild.Handshake() : new VersionHandshake(player2Identity);
        Player1 = CreatePeer(
            Player1Peer, Player1Handshake, _player2Address, true, new ComposedMatchSimulation(TestmanMirror.Create()), fightingFrames);
        Player2 = CreatePeer(
            Player2Peer,
            Player2Handshake,
            _player1Address,
            false,
            player2Simulation ?? new ComposedMatchSimulation(TestmanMirror.Create()),
            fightingFrames);
    }

    public InMemoryTransport Player1Transport { get; }

    public InMemoryTransport Player2Transport { get; }

    public PeerTransport Player1Peer { get; }

    public PeerTransport Player2Peer { get; }

    public VersionHandshake Player1Handshake { get; }

    public VersionHandshake Player2Handshake { get; }

    public TestMatchDriver<MatchState> Player1 { get; }

    public TestMatchDriver<MatchState> Player2 { get; }

    /// <summary>Runs both peers until <paramref name="done"/> holds, calling <paramref name="betweenFrames"/> each step.</summary>
    public void RunUntil(Func<bool> done, Action? betweenFrames = null) => Run(done, betweenFrames, int.MaxValue);

    /// <summary>Runs both peers until each is at exactly <paramref name="frames"/>, so their states can be compared.</summary>
    public void RunUntilFrame(int frames, Action? betweenFrames = null) =>
        Run(() => Player1.CurrentFrame >= frames && Player2.CurrentFrame >= frames, betweenFrames, frames);

    // A peer that reached lastFrame waits there while the other catches up.
    private void Run(Func<bool> done, Action? betweenFrames, int lastFrame)
    {
        var clock = Stopwatch.StartNew();
        while (!done())
        {
            Assert.True(clock.Elapsed < _timeout, $"Stuck at frames {Player1.CurrentFrame} / {Player2.CurrentFrame}.");
            if (Player1.CurrentFrame < lastFrame)
            {
                Player1.TryAdvanceFrame([]);
            }

            if (Player2.CurrentFrame < lastFrame)
            {
                Player2.TryAdvanceFrame([]);
            }

            betweenFrames?.Invoke();

            // Roughly the game's pace, so the sessions see time pass between frames.
            Thread.Sleep(1);
        }
    }

    public void Dispose()
    {
        Player1.Dispose();
        Player2.Dispose();
    }

    private static TestMatchDriver<MatchState> CreatePeer(
        PeerTransport transport,
        VersionHandshake handshake,
        EndPoint opponent,
        bool localIsPlayer1,
        IMatchSimulation<MatchState> simulation,
        int fightingFrames)
    {
        var settings = new PeerMatchSettings
        {
            RemoteEndPoint = opponent,
            LocalIsPlayer1 = localIsPlayer1,
            RollbackFrames = RollbackFrames,
        };
        RollbackMatchDriver<MatchState> driver = PeerSession.CreateDriver(simulation, settings, transport, handshake);
        var inputs = new FightingInputs(localIsPlayer1 ? Player1Seed : Player2Seed);
        return new TestMatchDriver<MatchState>(driver, [new NeutralAfter(inputs, fightingFrames)]);
    }
}
