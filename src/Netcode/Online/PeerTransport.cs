using Backdash.Network.Client;
using Netcode.Transport;

namespace Netcode.Online;

/// <summary>How a peer session reaches the other PC: direct UDP (development) or a platform relay such as Steam.</summary>
/// <remarks>Wraps Backdash's socket factory so callers outside Netcode never handle Backdash types (ADR-0005 §2).</remarks>
public sealed class PeerTransport
{
    /// <summary>For platform transports (e.g. Platform.Steam) that implement Backdash's socket.</summary>
    /// <param name="link">The transport's own link readings, when it has any.</param>
    public PeerTransport(IPeerSocketFactory sockets, ITransportLink? link = null)
    {
        Sockets = sockets;
        Link = link;
    }

    /// <summary>Plain UDP to an IP address and port. Development only (ADR-0005 §1).</summary>
    public static PeerTransport DirectUdp { get; } = new(new UdpSocketFactory());

    /// <summary>The transport's own view of the link (e.g. Steam's relay ping); null when it has none.</summary>
    public ITransportLink? Link { get; }

    internal IPeerSocketFactory Sockets { get; }
}
