using System;
using System.Net;

namespace Netcode.Transport;

/// <summary>Opens <see cref="UdpTransport"/>s. Development only (ADR-0005 §1).</summary>
public sealed class UdpTransportFactory : ITransportFactory
{
    public ITransport Open(int localPort, EndPoint opponent) =>
        opponent is IPEndPoint address
            ? new UdpTransport(localPort, address)
            : throw new ArgumentException($"Direct UDP needs an IP address and port, got {opponent}.", nameof(opponent));
}
