using Netcode.Transport;

namespace Netcode.Online;

/// <summary>How online matches reach one opponent: direct UDP (development) or a platform transport such as Steam.</summary>
/// <remarks>
/// Transports implement <see cref="ITransport"/>; Netcode alone adapts them to Backdash, so no caller outside Netcode
/// handles Backdash types (ADR-0005 §2).
/// </remarks>
public sealed class PeerTransport
{
    /// <param name="transports">Opens the transport for each session.</param>
    /// <param name="link">The transport's own link readings, when it has any.</param>
    public PeerTransport(ITransportFactory transports, ITransportLink? link = null)
    {
        Transports = transports;
        Link = link;
    }

    /// <summary>The transport's own view of the link (e.g. Steam's relay ping); null when it has none.</summary>
    public ITransportLink? Link { get; }

    /// <summary>Packets dropped on arrival over every session with this opponent.</summary>
    public DroppedPackets Dropped { get; } = new();

    internal ITransportFactory Transports { get; }

    /// <summary>Plain UDP to an IP address and port. Development only (ADR-0005 §1).</summary>
    public static PeerTransport DirectUdp() => new(new UdpTransportFactory());
}
