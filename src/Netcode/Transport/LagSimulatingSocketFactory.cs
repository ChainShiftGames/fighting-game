using Backdash.Network.Client;
using Backdash.Options;

namespace Netcode.Transport;

/// <summary>Adds bad-network simulation to the sockets of any transport (UDP or Steam relay).</summary>
public sealed class LagSimulatingSocketFactory : IPeerSocketFactory
{
    private readonly IPeerSocketFactory _sockets;
    private readonly NetworkSimulation _simulation;

    public LagSimulatingSocketFactory(IPeerSocketFactory sockets, NetworkSimulation simulation)
    {
        _sockets = sockets;
        _simulation = simulation;
    }

    public IPeerSocket Create(int port, NetcodeOptions options) =>
        new LagSimulatingSocket(_sockets.Create(port, options), _simulation);
}
