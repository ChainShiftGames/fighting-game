using Backdash.Network.Client;
using Netcode.Transport;

namespace Netcode.Online;

/// <summary>How a peer session reaches the other PC: direct UDP (development) or a platform relay such as Steam.</summary>
/// <remarks>Wraps Backdash's socket factory so callers outside Netcode never handle Backdash types (ADR-0005 §2).</remarks>
public sealed class PeerTransport
{
    /// <summary>For platform transports (e.g. Platform.Steam) that implement Backdash's socket.</summary>
    public PeerTransport(IPeerSocketFactory sockets)
    {
        Sockets = sockets;
    }

    /// <summary>Plain UDP to an IP address and port. Development only (ADR-0005 §1).</summary>
    public static PeerTransport DirectUdp { get; } = new(new UdpSocketFactory());

    internal IPeerSocketFactory Sockets { get; }
}
