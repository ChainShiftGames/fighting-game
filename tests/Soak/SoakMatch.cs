using System;
using System.Diagnostics;
using System.Net;
using System.Threading;
using Netcode.Desync;
using Netcode.Drivers;
using Netcode.Online;
using Netcode.Tests.Matches;
using Netcode.Tests.Transport;
using Sim.State;

namespace Soak;

/// <summary>
/// One online match between two peers in this process, through the real online driver (Backdash) over an
/// in-memory transport pair with simulated lag and loss, each peer played by seeded fighter inputs.
/// </summary>
/// <remarks>
/// Two desync checks, both fatal: Backdash's own consistency check (the driver throws), and a frame-by-frame
/// comparison of both peers' checksums for every frame that is confirmed on both.
/// </remarks>
internal sealed class SoakMatch : IDisposable
{
    // Frames that may still be rolled back trail the newest by at most the rollback window plus the input delay;
    // a second more on top means a compared frame is certainly confirmed while the peers are connected.
    private const int ConfirmedFramesMargin = 60;

    // Roughly the game's pace, so the sessions see time pass between frames.
    private const int MillisecondsBetweenSteps = 1;

    private static readonly TimeSpan _stallTimeout = TimeSpan.FromSeconds(30);

    // Only names for the sessions: the in-memory transports ignore addresses.
    private static readonly IPEndPoint _player1Address = new(IPAddress.Loopback, 1);
    private static readonly IPEndPoint _player2Address = new(IPAddress.Loopback, 2);

    private readonly int _framesBehindToCompare;
    private readonly string _dumpDirectory;
    private readonly string _reproduction;
    private readonly RecordingSimulation _player1Simulation;
    private readonly RecordingSimulation _player2Simulation;
    private readonly TestMatchDriver<MatchState> _player1;
    private readonly TestMatchDriver<MatchState> _player2;
    private readonly Stopwatch _sinceProgress = Stopwatch.StartNew();
    private int _lastProgressFrame;

    /// <param name="reproduction">How to replay this match, for the dump.</param>
    public SoakMatch(SoakGame game, uint player1Seed, uint player2Seed, string dumpDirectory, string reproduction)
    {
        _framesBehindToCompare = game.RollbackFrames + ConfirmedFramesMargin;
        _dumpDirectory = dumpDirectory;
        _reproduction = reproduction;
        (InMemoryTransport player1Transport, InMemoryTransport player2Transport) = InMemoryTransport.CreatePair();
        _player1Simulation = new RecordingSimulation(game.CreateSimulation());
        _player2Simulation = new RecordingSimulation(game.CreateSimulation());
        _player1 = CreatePeer(
            game, player1Transport, _player2Address, true, _player1Simulation, player1Seed, out IConnectionReadout first);
        _player2 = CreatePeer(
            game, player2Transport, _player1Address, false, _player2Simulation, player2Seed, out IConnectionReadout second);
        Player1Connection = first;
        Player2Connection = second;
    }

    /// <summary>Backdash's view of the connection from P1's side.</summary>
    public IConnectionReadout Player1Connection { get; }

    /// <summary>Backdash's view of the connection from P2's side.</summary>
    public IConnectionReadout Player2Connection { get; }

    /// <summary>Every frame up to here matched on both peers after it was confirmed.</summary>
    public int ComparedThroughFrame { get; private set; }

    public int Player1Frame => _player1.CurrentFrame;

    public int Player2Frame => _player2.CurrentFrame;

    /// <summary>The match is over in a confirmed frame both peers agree on.</summary>
    public bool IsOver => ComparedThroughFrame > 0 && _player1Simulation.IsMatchOverAt(ComparedThroughFrame);

    /// <summary>Tries to advance each peer one frame, then compares every newly confirmed frame.</summary>
    /// <exception cref="DesyncDetectedException">Either check found the peers disagree.</exception>
    /// <exception cref="TimeoutException">Neither peer advanced for too long.</exception>
    public void Step()
    {
        try
        {
            _player1.TryAdvanceFrame([]);
            _player2.TryAdvanceFrame([]);
        }
        catch (DesyncDetectedException backdashFound)
        {
            // Name the frame and dump the states when the confirmed frames already show it.
            CompareConfirmedFrames(backdashFound);
            throw;
        }

        CompareConfirmedFrames(null);
        CheckProgress();
        Thread.Sleep(MillisecondsBetweenSteps);
    }

    public void Dispose()
    {
        _player1.Dispose();
        _player2.Dispose();
    }

    private void CompareConfirmedFrames(Exception? backdashFound)
    {
        int confirmed = Math.Min(Player1Frame, Player2Frame) - _framesBehindToCompare;
        for (int frame = ComparedThroughFrame + 1; frame <= confirmed; frame++)
        {
            ulong player1 = _player1Simulation.ChecksumAt(frame);
            ulong player2 = _player2Simulation.ChecksumAt(frame);
            if (player1 != player2)
            {
                string dump = DesyncDump.Write(
                    _dumpDirectory, _reproduction, frame, _player1Simulation, _player2Simulation);
                string message = $"Confirmed frame {frame} differs between the peers: P1 {player1:x16}, " +
                    $"P2 {player2:x16} (peers at frames {Player1Frame} / {Player2Frame}). If Backdash logged a " +
                    $"disconnect just before, each peer ran on alone from there. Dump: {dump}";
                throw backdashFound is null
                    ? new DesyncDetectedException(message)
                    : new DesyncDetectedException(message, backdashFound);
            }

            ComparedThroughFrame = frame;
        }
    }

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

    private static TestMatchDriver<MatchState> CreatePeer(
        SoakGame game,
        InMemoryTransport transport,
        IPEndPoint opponent,
        bool localIsPlayer1,
        RecordingSimulation simulation,
        uint seed,
        out IConnectionReadout connection)
    {
        var settings = new PeerMatchSettings
        {
            RemoteEndPoint = opponent,
            LocalIsPlayer1 = localIsPlayer1,
            RollbackFrames = game.RollbackFrames,
            NetworkSimulation = game.Network,
        };
        RollbackMatchDriver<MatchState> driver =
            PeerSession.CreateDriver(
                simulation, settings, new PeerTransport(new OpenedTransport(transport)), game.CreateHandshake());
        connection = driver;
        return new TestMatchDriver<MatchState>(driver, [new FightingInputs(seed)]);
    }
}
