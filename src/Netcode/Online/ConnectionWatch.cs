using System;

namespace Netcode.Online;

/// <summary>
/// Follows the netcode's connection events (interrupted, resumed, disconnected) and counts down the seconds left
/// while interrupted.
/// </summary>
/// <remarks>A disconnect is final: nothing after it brings the match back (ARCHITECTURE.md §8: never auto-correct).</remarks>
public sealed class ConnectionWatch : IConnectionStatusReadout
{
    private readonly TimeProvider _clock;
    private ConnectionState _state = ConnectionState.Connected;
    private long _disconnectAt;

    public ConnectionWatch(TimeProvider clock)
    {
        _clock = clock;
    }

    public bool IsDisconnected => _state == ConnectionState.Disconnected;

    /// <param name="untilDisconnect">Time left before the netcode gives up, as it reports it.</param>
    public void Interrupted(TimeSpan untilDisconnect)
    {
        if (_state == ConnectionState.Disconnected)
        {
            return;
        }

        _state = ConnectionState.Interrupted;
        _disconnectAt = _clock.GetTimestamp() + (long)(untilDisconnect.TotalSeconds * _clock.TimestampFrequency);
    }

    public void Resumed()
    {
        if (_state == ConnectionState.Interrupted)
        {
            _state = ConnectionState.Connected;
        }
    }

    public void Disconnected() => _state = ConnectionState.Disconnected;

    public ConnectionStatus ReadConnectionStatus() => _state switch
    {
        ConnectionState.Interrupted => new ConnectionStatus(ConnectionState.Interrupted, SecondsLeft()),
        ConnectionState.Disconnected => ConnectionStatus.Disconnected,
        _ => ConnectionStatus.Connected,
    };

    // Rounded up, so the countdown shows 1 until the very end, never 0 while still waiting.
    private int SecondsLeft()
    {
        long ticksLeft = Math.Max(_disconnectAt - _clock.GetTimestamp(), 0);
        long frequency = _clock.TimestampFrequency;
        return (int)((ticksLeft + frequency - 1) / frequency);
    }
}
