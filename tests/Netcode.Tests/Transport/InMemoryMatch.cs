using System;
using System.Diagnostics;
using System.Net;
using System.Threading;
using Netcode.Drivers;
using Netcode.Online;
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

    public InMemoryMatch(int fightingFrames, IMatchSimulation<MatchState>? player2Simulation = null)
    {
        (Player1Transport, Player2Transport) = InMemoryTransport.CreatePair();
        Player1Peer = new PeerTransport(new OpenedTransport(Player1Transport));
        Player2Peer = new PeerTransport(new OpenedTransport(Player2Transport));
        Player1 = CreatePeer(Player1Peer, _player2Address, true, new ComposedMatchSimulation(TestmanMirror.Create()), fightingFrames);
        Player2 = CreatePeer(
            Player2Peer,
            _player1Address,
            false,
            player2Simulation ?? new ComposedMatchSimulation(TestmanMirror.Create()),
            fightingFrames);
    }

    public InMemoryTransport Player1Transport { get; }

    public InMemoryTransport Player2Transport { get; }

    public PeerTransport Player1Peer { get; }

    public PeerTransport Player2Peer { get; }

    public TestMatchDriver<MatchState> Player1 { get; }

    public TestMatchDriver<MatchState> Player2 { get; }

    /// <summary>Runs both peers until <paramref name="done"/> holds, calling <paramref name="betweenFrames"/> each step.</summary>
    public void RunUntil(Func<bool> done, Action? betweenFrames = null)
    {
        var clock = Stopwatch.StartNew();
        while (!done())
        {
            Assert.True(clock.Elapsed < _timeout, $"Stuck at frames {Player1.CurrentFrame} / {Player2.CurrentFrame}.");
            Player1.TryAdvanceFrame([]);
            Player2.TryAdvanceFrame([]);
            betweenFrames?.Invoke();

            // Roughly the game's pace, so the sessions see time pass between frames.
            Thread.Sleep(1);
        }
    }

    /// <summary>Runs both peers until each has reached <paramref name="frames"/>.</summary>
    public void RunUntilFrame(int frames, Action? betweenFrames = null) =>
        RunUntil(() => Player1.CurrentFrame >= frames && Player2.CurrentFrame >= frames, betweenFrames);

    public void Dispose()
    {
        Player1.Dispose();
        Player2.Dispose();
    }

    private static TestMatchDriver<MatchState> CreatePeer(
        PeerTransport transport,
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
        RollbackMatchDriver<MatchState> driver = PeerSession.CreateDriver(simulation, settings, transport);
        var inputs = new FightingInputs(localIsPlayer1 ? Player1Seed : Player2Seed);
        return new TestMatchDriver<MatchState>(driver, [new NeutralAfter(inputs, fightingFrames)]);
    }
}
