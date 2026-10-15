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

    public static NetworkSimulation None => new() { PingMilliseconds = 0, JitterMilliseconds = 0, PacketLossPercent = 0 };

    /// <summary>The test conditions of ADR-0005: 100 ms ping, ±20 ms jitter, 2 % loss.</summary>
    public static NetworkSimulation TestConditions =>
        new() { PingMilliseconds = 100, JitterMilliseconds = 20, PacketLossPercent = 2 };

    public bool IsNone => PingMilliseconds == 0 && JitterMilliseconds == 0 && PacketLossPercent == 0;
}
