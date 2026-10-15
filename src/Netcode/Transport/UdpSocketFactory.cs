using Backdash.Network.Client;
using Backdash.Options;

namespace Netcode.Transport;

/// <summary>Direct UDP sockets for Backdash, with optional bad-network simulation. Development only (ADR-0005).</summary>
public sealed class UdpSocketFactory : IPeerSocketFactory
{
    private readonly NetworkSimulation _simulation;

    public UdpSocketFactory(NetworkSimulation simulation)
    {
        _simulation = simulation;
    }

    public IPeerSocket Create(int port, NetcodeOptions options)
    {
        var socket = new UdpSocket(port, options.UseIPv6);
        return _simulation.IsNone ? socket : new LagSimulatingSocket(socket, _simulation);
    }
}
