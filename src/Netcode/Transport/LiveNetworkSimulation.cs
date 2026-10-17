using System;
using System.Threading;

namespace Netcode.Transport;

/// <summary>
/// This PC's network simulation as it is right now, changed while a match runs (developer settings, debug panel).
/// Every <see cref="LagSimulatingTransport"/> that follows it uses the new conditions from its next packet on.
/// </summary>
/// <remarks>
/// Only the packets change, never the game logic: the netcode copes with the new conditions as it would with a real
/// network getting worse. Set on the game thread and read on whichever thread sends, so the set is read whole.
/// </remarks>
public sealed class LiveNetworkSimulation
{
    private const int MaxPacketLossPercent = 100;

    private readonly Lock _lock = new();
    private NetworkSimulation _current;

    public LiveNetworkSimulation()
        : this(NetworkSimulation.None)
    {
    }

    public LiveNetworkSimulation(NetworkSimulation initial) => Current = initial;

    /// <summary>The conditions every following transport applies to its next packet.</summary>
    /// <exception cref="ArgumentOutOfRangeException">A negative time, or a loss outside 0–100 %.</exception>
    public NetworkSimulation Current
    {
        get
        {
            lock (_lock)
            {
                return _current;
            }
        }

        set
        {
            ArgumentOutOfRangeException.ThrowIfNegative(value.PingMilliseconds, nameof(value.PingMilliseconds));
            ArgumentOutOfRangeException.ThrowIfNegative(value.JitterMilliseconds, nameof(value.JitterMilliseconds));
            ArgumentOutOfRangeException.ThrowIfNegative(value.PacketLossPercent, nameof(value.PacketLossPercent));
            ArgumentOutOfRangeException.ThrowIfGreaterThan(
                value.PacketLossPercent, MaxPacketLossPercent, nameof(value.PacketLossPercent));

            // Only the values: a set that itself follows another live simulation must not chain to it.
            var values = new NetworkSimulation
            {
                PingMilliseconds = value.PingMilliseconds,
                JitterMilliseconds = value.JitterMilliseconds,
                PacketLossPercent = value.PacketLossPercent,
            };
            lock (_lock)
            {
                _current = values;
            }
        }
    }
}
