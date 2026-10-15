using System.Net;
using Netcode.Transport;

namespace Netcode.Online;

/// <summary>How to set up a two-PC rollback match over direct UDP (developer settings; Steam relay comes in P0-21).</summary>
public sealed record PeerMatchSettings
{
    /// <summary>UDP port this PC listens on.</summary>
    public required int LocalPort { get; init; }

    /// <summary>Where the other player's game listens.</summary>
    public required IPEndPoint RemoteEndPoint { get; init; }

    /// <summary>Whether this PC plays P1 or P2. The two PCs must pick different ones.</summary>
    public required bool LocalIsPlayer1 { get; init; }

    /// <summary>Most frames predicted ahead of the last confirmed input, and so rolled back (ADR-0003).</summary>
    public int RollbackFrames { get; init; } = 8;

    /// <summary>Fixed input delay until the automatic 1–2 frame delay of ADR-0005 exists.</summary>
    public int InputDelayFrames { get; init; } = 2;

    public NetworkSimulation NetworkSimulation { get; init; } = NetworkSimulation.None;
}
