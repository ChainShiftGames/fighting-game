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
    private readonly TransportChecksumLink? _checksums;

    /// <param name="checksums">The desync check's link, sent over the same transport; none when null.</param>
    public TransportSocketFactory(
        Func<ITransport> open, EndPoint opponent, DroppedPackets dropped, TransportChecksumLink? checksums = null)
    {
        _open = open;
        _opponent = opponent;
        _dropped = dropped;
        _checksums = checksums;
    }

    public IPeerSocket Create(int port, NetcodeOptions options)
    {
        ITransport transport = _open();
        _checksums?.Attach(transport);
        return new TransportSocket(transport, _opponent, port, _dropped, _checksums);
    }
}
