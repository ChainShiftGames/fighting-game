using System.Net;
using Netcode.Transport;

namespace Netcode.Online;

/// <summary>How to set up a two-PC rollback match (developer settings until there is an options menu).</summary>
public sealed record PeerMatchSettings
{
    /// <summary>UDP port this PC listens on. Relay transports (Steam) have no port and leave it at 0.</summary>
    public int LocalPort { get; init; }

    /// <summary>
    /// The other player's address: an <see cref="IPEndPoint"/> for direct UDP, or the platform transport's own
    /// endpoint type (e.g. a Steam ID).
    /// </summary>
    public required EndPoint RemoteEndPoint { get; init; }

    /// <summary>Whether this PC plays P1 or P2. The two PCs must pick different ones.</summary>
    public required bool LocalIsPlayer1 { get; init; }

    /// <summary>
    /// Rollback window (ADR-0003): most frames predicted ahead of the last confirmed input, and so rolled back.
    /// Developer setting, from <c>data/netcode/online.json</c>.
    /// </summary>
    public required int RollbackFrames { get; init; }

    /// <summary>
    /// Input delay the session starts with and keeps. Developer setting for tests and tools; players' matches use the
    /// <see cref="InputDelaySettings"/> overload of <c>PeerSession.CreateDriver</c>, which replaces it.
    /// </summary>
    public int InputDelayFrames { get; init; } = 2;

    public NetworkSimulation NetworkSimulation { get; init; } = NetworkSimulation.None;

    /// <summary>
    /// When a silent connection is warned about and ends the match. Developer settings, from
    /// <c>data/netcode/online.json</c>; null keeps Backdash's own timeouts (tests and tools).
    /// </summary>
    public DisconnectTimeouts? Disconnect { get; init; }
}
