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
/// </remarks>
public sealed class RollbackMatchDriver<TState> : IMatchDriver<TState>, IConnectionReadout, INetcodeSessionHandler
    where TState : unmanaged
{
    private readonly IMatchSimulation<TState> _simulation;
    private readonly INetcodeSession<InputWord> _session;
    private readonly NetcodePlayer[] _localPlayers;
    private TState _state;
    private int _currentFrame;
    private int _framesToWait;
    private string? _desync;
    private int _rollbacks;
    private long _rolledBackFrames;
    private int _longestRollbackFrames;
    private int _framesInThisRollback;

    public RollbackMatchDriver(IMatchSimulation<TState> simulation, INetcodeSession<InputWord> session)
    {
        _simulation = simulation;
        _session = session;
        _state = simulation.CreateInitialState();
        _localPlayers = LocalPlayersInPortOrder(session);
        _session.SetHandler(this);
        _session.Start();
    }

    public int LocalPlayerCount => _localPlayers.Length;

    public int CurrentFrame => _currentFrame;

    public ref readonly TState State => ref _state;

    public bool TryAdvanceFrame(ReadOnlySpan<InputWord> localInputs)
    {
        MatchPlayers.EnsureInputCount(localInputs, _localPlayers.Length);
        try
        {
            _session.BeginFrame();
            if (_desync is not null)
            {
                throw new DesyncDetectedException(_desync);
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
        catch (NetcodeException exception) when (_session.IsSyncTest())
        {
            throw new DesyncDetectedException($"SyncTest found a desync near frame {_currentFrame}.", exception);
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
        if (evt.Type == PeerEvent.ChecksumMismatch)
        {
            ChecksumMismatchEventInfo mismatch = evt.ChecksumMismatch;
            _desync = $"Checksums differ at frame {mismatch.MismatchFrame.Number}: " +
                $"this PC {mismatch.LocalChecksum}, other PC {mismatch.RemoteChecksum}.";
        }
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
