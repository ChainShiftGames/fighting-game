using System;
using System.Net;
using Backdash.Network.Client;
using Backdash.Options;

namespace Netcode.Transport;

/// <summary>Gives Backdash a <see cref="TransportSocket"/> over a transport opened for the session.</summary>
internal sealed class TransportSocketFactory : IPeerSocketFactory
{
    private readonly Func<ITransport> _open;
    private readonly EndPoint _opponent;
    private readonly DroppedPackets _dropped;

    public TransportSocketFactory(Func<ITransport> open, EndPoint opponent, DroppedPackets dropped)
    {
        _open = open;
        _opponent = opponent;
        _dropped = dropped;
    }

    public IPeerSocket Create(int port, NetcodeOptions options) => new TransportSocket(_open(), _opponent, port, _dropped);
}
