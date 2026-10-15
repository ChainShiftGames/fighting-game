using Backdash.Network.Client;
using Backdash.Options;

namespace Netcode.Transport;

/// <summary>Direct UDP sockets for Backdash. Development only (ADR-0005).</summary>
public sealed class UdpSocketFactory : IPeerSocketFactory
{
    public IPeerSocket Create(int port, NetcodeOptions options) => new UdpSocket(port, options.UseIPv6);
}
