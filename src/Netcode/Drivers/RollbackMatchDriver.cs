using System;
using Backdash;
using Backdash.Core;
using Backdash.Serialization;
using Netcode.Desync;
using Netcode.Online;
using Sim.Input;

namespace Netcode.Drivers;

/// <summary>Runs a match through a Backdash session, which predicts, rolls back and resimulates (ADR-0005).</summary>
/// <remarks>
/// Works for every Backdash session type (SyncTest, peer-to-peer), so the session is built outside and passed in.
/// Backdash calls back into this class to save, load and resimulate frames; the host never sees a rollback.
/// Online, an optional desync monitor sees every frame run and compares each final one with the other PC
/// (ADR-0005 §5); Backdash's own periodic consistency check stays on as a second line.
/// </remarks>
public sealed class RollbackMatchDriver<TState> :
    IMatchDriver<TState>, IConfirmedFrameReadout, IConnectionReadout, IConnectionStatusReadout, INetcodeSessionHandler
    where TState : unmanaged
{
    private const int NoMismatch = -1;

    private readonly IMatchSimulation<TState> _simulation;
    private readonly INetcodeSession<InputWord> _session;
    private readonly NetcodePlayer[] _localPlayers;
    private readonly ConnectionWatch _connection;
    private readonly ConfirmedInputFrames? _confirmedInputs;
    private readonly DesyncMonitor<TState>? _desyncMonitor;
    private TState _state;
    private int _currentFrame;
    private int _framesToWait;
    private int _backdashMismatchFrame = NoMismatch;
    private string _backdashMismatch = string.Empty;
    private DesyncDetectedException? _desync;
    private int _rollbacks;
    private long _rolledBackFrames;
    private int _longestRollbackFrames;
    private int _framesInThisRollback;

    /// <param name="clock">Times the countdown while the connection is interrupted; the system clock when null.</param>
    /// <param name="confirmedInputs">
    /// The input listener the session was built with (online); null when every frame is final as soon as it runs
    /// (SyncTest).
    /// </param>
    /// <param name="desyncMonitor">
    /// Compares each final frame with the other PC (online); needs <paramref name="confirmedInputs"/>.
    /// </param>
    public RollbackMatchDriver(
        IMatchSimulation<TState> simulation,
        INetcodeSession<InputWord> session,
        TimeProvider? clock = null,
        ConfirmedInputFrames? confirmedInputs = null,
        DesyncMonitor<TState>? desyncMonitor = null)
    {
        if (desyncMonitor is not null && confirmedInputs is null)
        {
            // Comparing a frame that may still be rolled back would report desyncs that never happened.
            throw new ArgumentException(
                "A desync monitor may only see final frames: pass the session's confirmed inputs.", nameof(confirmedInputs));
        }

        _simulation = simulation;
        _session = session;
        _confirmedInputs = confirmedInputs;
        _desyncMonitor = desyncMonitor;
        _connection = new ConnectionWatch(clock ?? TimeProvider.System);
        _state = simulation.CreateInitialState();
        _localPlayers = LocalPlayersInPortOrder(session);
        _session.SetHandler(this);
        _session.Start();
    }

    public int LocalPlayerCount => _localPlayers.Length;

    public int CurrentFrame => _currentFrame;

    public ref readonly TState State => ref _state;

    // Backdash reports a frame confirmed only after rerunning every frame it had predicted wrongly, and a frame
    // still to run will run with those inputs: the frames up to the smaller number are final.
    public int ConfirmedFrame =>
        _confirmedInputs is null ? _currentFrame : Math.Min(_confirmedInputs.Count, _currentFrame);

    /// <summary>The newest frame both PCs' desync monitors computed the same checksum for; 0 without a monitor.</summary>
    public int AgreedFrame => _desyncMonitor?.LastAgreedFrame ?? 0;

    /// <exception cref="DesyncDetectedException">
    /// The PCs disagree. Every later call throws it again: the match is over, and its dump is written only once.
    /// </exception>
    public bool TryAdvanceFrame(ReadOnlySpan<InputWord> localInputs)
    {
        MatchPlayers.EnsureInputCount(localInputs, _localPlayers.Length);
        if (_desync is not null)
        {
            throw _desync;
        }

        try
        {
            bool advanced = AdvanceSession(localInputs);
            _desyncMonitor?.ConfirmThrough(ConfirmedFrame);
            return advanced;
        }
        catch (DesyncDetectedException desync)
        {
            _desync = desync;
            throw;
        }
        catch (NetcodeException exception) when (_session.IsSyncTest())
        {
            _desync = new DesyncDetectedException($"SyncTest found a desync near frame {_currentFrame}.", exception);
            throw _desync;
        }
    }

    /// <summary>Backdash's network stats for the other player plus the rollbacks so far.</summary>
    public bool TryReadConnection(out ConnectionStats stats)
    {
        if (!_session.TryGetRemotePlayer(out NetcodePlayer? remote) || !_session.UpdateNetworkStats(remote))
        {
            stats = default;
            return false;
        }

        PeerNetworkStats network = remote.NetworkStats;
        stats = new ConnectionStats(
            _currentFrame,
            (int)network.Ping.TotalMilliseconds,
            network.LocalFramesBehind.Frames,
            network.RemoteFramesBehind.Frames,
            network.PendingInputCount,
            _rollbacks,
            _rolledBackFrames,
            _longestRollbackFrames);
        return true;
    }

    /// <summary>Whether the other PC is still heard from: connected, interrupted (with the seconds left) or gone.</summary>
    public ConnectionStatus ReadConnectionStatus() => _connection.ReadConnectionStatus();

    public void Dispose() => _session.Dispose();

    void INetcodeSessionHandler.SaveState(Frame frame, ref readonly BinaryBufferWriter writer) =>
        writer.WriteStruct(in _state);

    void INetcodeSessionHandler.LoadState(Frame frame, ref readonly BinaryBufferReader reader)
    {
        reader.ReadStruct(ref _state);
        _currentFrame = frame.Number;
    }

    // Backdash resimulates a frame during a rollback.
    void INetcodeSessionHandler.AdvanceFrame()
    {
        _framesInThisRollback++;
        RunSynchronizedFrame();
    }

    // A rollback's length is counted in resimulated frames: by the time Backdash calls BeginRollback the current
    // frame already equals the frame it passes, so the numbers there don't show how far back it went.
    void INetcodeSessionHandler.BeginRollback(Frame frame) => _framesInThisRollback = 0;

    void INetcodeSessionHandler.EndRollback(Frame frame)
    {
        _rollbacks++;
        _rolledBackFrames += _framesInThisRollback;
        _longestRollbackFrames = Math.Max(_longestRollbackFrames, _framesInThisRollback);
    }

    void INetcodeSessionHandler.OnSessionStart()
    {
    }

    void INetcodeSessionHandler.OnSessionClose()
    {
    }

    void INetcodeSessionHandler.TimeSync(FrameSpan framesAhead) => _framesToWait = framesAhead.Frames;

    // Raised from Backdash's callback, thrown from the host's next frame so the host sees it (ARCHITECTURE.md §8).
    void INetcodeSessionHandler.OnPeerEvent(NetcodePlayer player, in PeerEventInfo evt)
    {
        if (evt.Type == PeerEvent.ChecksumMismatch && _backdashMismatchFrame == NoMismatch)
        {
            ChecksumMismatchEventInfo mismatch = evt.ChecksumMismatch;
            _backdashMismatchFrame = mismatch.MismatchFrame.Number;
            _backdashMismatch = $"Backdash's consistency check: checksums differ at frame {_backdashMismatchFrame}: " +
                $"this PC {mismatch.LocalChecksum}, other PC {mismatch.RemoteChecksum}.";
        }
        else if (evt.Type == PeerEvent.ConnectionInterrupted)
        {
            _connection.Interrupted(evt.ConnectionInterrupted.DisconnectTimeout);
        }
        else if (evt.Type == PeerEvent.ConnectionResumed)
        {
            _connection.Resumed();
        }
        else if (evt.Type == PeerEvent.Disconnected)
        {
            _connection.Disconnected();
        }
    }

    private bool AdvanceSession(ReadOnlySpan<InputWord> localInputs)
    {
        if (!_connection.IsDisconnected)
        {
            _session.BeginFrame();
        }

        ThrowIfBackdashFoundADesync();

        // After a disconnect Backdash plays on alone, with default inputs for the other player, so the two PCs
        // would drift apart unnoticed. The match holds instead and is over (ReadConnectionStatus), without a result.
        if (_connection.IsDisconnected)
        {
            return false;
        }

        // Time sync: this PC is ahead of the other one, so it waits a frame for it to catch up (ADR-0004 §4).
        if (_framesToWait > 0)
        {
            _framesToWait--;
            return false;
        }

        for (int i = 0; i < _localPlayers.Length; i++)
        {
            InputWord input = localInputs[i];
            if (_session.AddLocalInput(_localPlayers[i], in input) != ResultCode.Ok)
            {
                return false;
            }
        }

        return RunSynchronizedFrame();
    }

    // One desync path for the game whichever check found it: with a monitor, Backdash's finding gets a dump too.
    // Backdash numbers the state it saved before running frame n as frame n: the monitor's frame n, after n frames.
    private void ThrowIfBackdashFoundADesync()
    {
        if (_backdashMismatchFrame == NoMismatch)
        {
            return;
        }

        _desyncMonitor?.ReportMismatch(_backdashMismatchFrame, _backdashMismatch);
        throw new DesyncDetectedException(_backdashMismatch);
    }

    private bool RunSynchronizedFrame()
    {
        if (_session.SynchronizeInputs() != ResultCode.Ok)
        {
            return false;
        }

        ReadOnlySpan<SynchronizedInput<InputWord>> inputs = _session.CurrentSynchronizedInputs;
        _simulation.Tick(ref _state, inputs[0].Input, inputs[1].Input);
        _currentFrame++;
        _desyncMonitor?.RecordFrame(_currentFrame, in _state, inputs[0].Input, inputs[1].Input);
        _session.AdvanceFrame();
        return true;
    }

    private static NetcodePlayer[] LocalPlayersInPortOrder(INetcodeSession<InputWord> session)
    {
        var players = new NetcodePlayer[MatchPlayers.Count];
        int count = 0;
        foreach (NetcodePlayer player in session.GetPlayers())
        {
            if (player.IsLocal())
            {
                players[count++] = player;
            }
        }

        Array.Resize(ref players, count);
        Array.Sort(players, static (left, right) => left.Index.CompareTo(right.Index));
        return players;
    }
}
