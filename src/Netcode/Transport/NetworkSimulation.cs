namespace Netcode.Transport;

/// <summary>
/// Bad-network conditions added to every packet this PC sends (developer settings). Both players simulate their
/// half, so the round trip ends up at <see cref="PingMilliseconds"/>.
/// </summary>
public readonly record struct NetworkSimulation
{
    /// <summary>Round-trip time to add. Each side delays its own packets by half of it.</summary>
    public required int PingMilliseconds { get; init; }

    /// <summary>How far the ping varies either way, per packet.</summary>
    public required int JitterMilliseconds { get; init; }

    /// <summary>Share of packets dropped, 0–100, per direction.</summary>
    public required int PacketLossPercent { get; init; }

    /// <summary>
    /// When set, the transport applies whatever this live simulation holds at each packet instead of the fixed values
    /// above, which are then only its starting values (see <see cref="Following"/>).
    /// </summary>
    public LiveNetworkSimulation? Live { get; private init; }

    public static NetworkSimulation None => new() { PingMilliseconds = 0, JitterMilliseconds = 0, PacketLossPercent = 0 };

    /// <summary>The test conditions of ADR-0005: 100 ms ping, ±20 ms jitter, 2 % loss.</summary>
    public static NetworkSimulation TestConditions =>
        new() { PingMilliseconds = 100, JitterMilliseconds = 20, PacketLossPercent = 2 };

    /// <summary>No simulation at all: neither fixed conditions nor a live simulation that could add some later.</summary>
    public bool IsNone => Live is null && PingMilliseconds == 0 && JitterMilliseconds == 0 && PacketLossPercent == 0;

    /// <summary>
    /// Conditions that follow <paramref name="live"/> while the match runs, so the debug panel can change them; the
    /// values carried here are the live ones at this moment, for describing the match.
    /// </summary>
    /// <remarks>
    /// Carried inside the match settings' fixed conditions so the online session wraps its transport the same way
    /// for both kinds; it isn't <see cref="IsNone"/> even at zero, because the panel may raise it mid-match.
    /// </remarks>
    public static NetworkSimulation Following(LiveNetworkSimulation live) => live.Current with { Live = live };
}
