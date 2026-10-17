using System;
using System.Diagnostics;
using System.Net;
using System.Threading;
using Netcode.Desync;
using Netcode.Drivers;
using Netcode.Online;
using Netcode.Tests.Transport;
using Sim.State;

namespace Soak;

/// <summary>
/// One online match between two peers in this process, through the real online driver (Backdash) over an
/// in-memory transport pair with simulated lag and loss, each peer played by a seeded CPU bot (P3-50).
/// </summary>
/// <remarks>
/// Three desync checks, all fatal: Backdash's own consistency check and each peer's desync monitor, both inside the
/// driver exactly as in the game (the monitors trade checksums over the transport and each writes a dump), and,
/// independently of both, a frame-by-frame comparison of the two peers' checksums for every frame final on both.
/// </remarks>
internal sealed class SoakMatch : IDisposable
{
    // Roughly the game's pace, so the sessions see time pass between frames.
    private const int MillisecondsBetweenSteps = 1;

    private static readonly TimeSpan _stallTimeout = TimeSpan.FromSeconds(30);

    // After one peer finds a desync, how long the other gets to find it too and write its own dump.
    private static readonly TimeSpan _otherPeerTimeout = TimeSpan.FromSeconds(5);

    // Only names for the sessions: the in-memory transports ignore addresses.
    private static readonly IPEndPoint _player1Address = new(IPAddress.Loopback, 1);
    private static readonly IPEndPoint _player2Address = new(IPAddress.Loopback, 2);

    private readonly SoakGame _game;
    private readonly string _dumpDirectory;
    private readonly string _reproduction;
    private readonly RecordingSimulation _player1Simulation;
    private readonly RecordingSimulation _player2Simulation;
    private readonly RollbackMatchDriver<MatchState> _player1Driver;
    private readonly RollbackMatchDriver<MatchState> _player2Driver;
    private readonly TestMatchDriver<MatchState> _player1;
    private readonly TestMatchDriver<MatchState> _player2;
    private readonly Stopwatch _sinceProgress = Stopwatch.StartNew();
    private int _lastProgressFrame;

    /// <param name="player1Seed">Seeds P1's bot.</param>
    /// <param name="player2Seed">Seeds P2's bot.</param>
    /// <param name="reproduction">How to replay this match, for the failure message.</param>
    public SoakMatch(SoakGame game, uint player1Seed, uint player2Seed, string dumpDirectory, string reproduction)
    {
        _game = game;
        _dumpDirectory = dumpDirectory;
        _reproduction = reproduction;
        (InMemoryTransport player1Transport, InMemoryTransport player2Transport) = InMemoryTransport.CreatePair();
        _player1Simulation = new RecordingSimulation(game.CreateSimulation(), game.DesyncHistoryFrames);
        _player2Simulation = new RecordingSimulation(game.CreateSimulation(), game.DesyncHistoryFrames);
        _player1Driver = CreateDriver(game, player1Transport, _player2Address, true, _player1Simulation, dumpDirectory);
        _player2Driver = CreateDriver(game, player2Transport, _player1Address, false, _player2Simulation, dumpDirectory);
        _player1 = new TestMatchDriver<MatchState>(_player1Driver, [game.CreateBot(_player1Driver, 0, player1Seed)]);
        _player2 = new TestMatchDriver<MatchState>(_player2Driver, [game.CreateBot(_player2Driver, 1, player2Seed)]);
    }

    /// <summary>Backdash's view of the connection from P1's side.</summary>
    public IConnectionReadout Player1Connection => _player1Driver;

    /// <summary>Backdash's view of the connection from P2's side.</summary>
    public IConnectionReadout Player2Connection => _player2Driver;

    /// <summary>Every frame up to here was final on both peers and matched.</summary>
    public int ComparedThroughFrame { get; private set; }

    /// <summary>The newest frame P1's desync monitor found both peers agree on.</summary>
    public int Player1AgreedFrame => _player1Driver.AgreedFrame;

    /// <summary>The newest frame P2's desync monitor found both peers agree on.</summary>
    public int Player2AgreedFrame => _player2Driver.AgreedFrame;

    public int Player1Frame => _player1.CurrentFrame;

    public int Player2Frame => _player2.CurrentFrame;

    /// <summary>The match is over in a confirmed frame both peers agree on.</summary>
    public bool IsOver => ComparedThroughFrame > 0 && _player1Simulation.IsMatchOverAt(ComparedThroughFrame);

    /// <summary>Tries to advance each peer one frame, then compares every frame newly final on both.</summary>
    /// <exception cref="DesyncDetectedException">A check found the peers disagree.</exception>
    /// <exception cref="TimeoutException">Neither peer advanced for too long.</exception>
    public void Step()
    {
        try
        {
            _player1.TryAdvanceFrame([]);
            _player2.TryAdvanceFrame([]);
        }
        catch (DesyncDetectedException found)
        {
            ReportDesync(found);
        }

        CompareFinalFrames();
        CheckProgress();
        Thread.Sleep(MillisecondsBetweenSteps);
    }

    public void Dispose()
    {
        _player1.Dispose();
        _player2.Dispose();
    }

    // One peer's check found it: let the other one find it too, as on two PCs, so both dumps exist; then name the
    // frame from the soak's own comparison when the final frames already show it.
    private void ReportDesync(DesyncDetectedException found)
    {
        DesyncDetectedException? player1 = TryStep(_player1);
        DesyncDetectedException? player2 = TryStep(_player2);
        var clock = Stopwatch.StartNew();
        while ((player1 is null || player2 is null) && clock.Elapsed < _otherPeerTimeout)
        {
            Thread.Sleep(MillisecondsBetweenSteps);
            player1 ??= TryStep(_player1);
            player2 ??= TryStep(_player2);
        }

        string message = $"P1: {player1?.Message ?? "found nothing"} P2: {player2?.Message ?? "found nothing"} " +
            $"(peers at frames {Player1Frame} / {Player2Frame}). {_reproduction}";
        try
        {
            CompareFinalFrames();
        }
        catch (DesyncDetectedException soakFound)
        {
            throw new DesyncDetectedException($"{soakFound.Message} {message}", found);
        }

        throw new DesyncDetectedException(message, found);
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

    // Independent of the monitors: if they ever miss a frame, this still finds it.
    private void CompareFinalFrames()
    {
        int final = Math.Min(_player1Driver.ConfirmedFrame, _player2Driver.ConfirmedFrame);
        for (int frame = ComparedThroughFrame + 1; frame <= final; frame++)
        {
            ulong player1 = _player1Simulation.ChecksumAt(frame);
            ulong player2 = _player2Simulation.ChecksumAt(frame);
            if (player1 != player2)
            {
                string player1Dump = WriteDump(_player1Simulation, true, new(frame, player1, player2, ComparedThroughFrame));
                string player2Dump = WriteDump(_player2Simulation, false, new(frame, player2, player1, ComparedThroughFrame));
                throw new DesyncDetectedException(
                    $"The soak's own check: final frame {frame} differs between the peers: P1 {player1:x16}, " +
                    $"P2 {player2:x16}. Dumps: {player1Dump} {player2Dump}",
                    player1Dump);
            }

            ComparedThroughFrame = frame;
        }
    }

    private string WriteDump(RecordingSimulation simulation, bool localIsPlayer1, ChecksumMismatch mismatch) =>
        DesyncDumpFile.Write(
            _dumpDirectory,
            DesyncDumpBuilder.Build(_game.DumpHeader(localIsPlayer1), mismatch, simulation.History, DateTimeOffset.UtcNow));

    private void CheckProgress()
    {
        int progress = Player1Frame + Player2Frame;
        if (progress != _lastProgressFrame)
        {
            _lastProgressFrame = progress;
            _sinceProgress.Restart();
        }
        else if (_sinceProgress.Elapsed > _stallTimeout)
        {
            throw new TimeoutException(
                $"Neither peer advanced for {_stallTimeout.TotalSeconds} s, stuck at frames " +
                $"{Player1Frame} / {Player2Frame}.");
        }
    }

    private static RollbackMatchDriver<MatchState> CreateDriver(
        SoakGame game,
        InMemoryTransport transport,
        IPEndPoint opponent,
        bool localIsPlayer1,
        RecordingSimulation simulation,
        string dumpDirectory)
    {
        var settings = new PeerMatchSettings
        {
            RemoteEndPoint = opponent,
            LocalIsPlayer1 = localIsPlayer1,
            RollbackFrames = game.RollbackFrames,
            NetworkSimulation = game.Network,
            DesyncDumps = new DesyncDumpSettings { Folder = dumpDirectory, HistoryFrames = game.DesyncHistoryFrames },
        };
        return PeerSession.CreateDriver(
            simulation, settings, new PeerTransport(new OpenedTransport(transport)), game.CreateHandshake());
    }
}
