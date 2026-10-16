using System.Net;
using Netcode.Transport;

namespace Netcode.Tests.Transport;

/// <summary>Hands a session a transport made by the test, and remembers how the session asked for it.</summary>
internal sealed class OpenedTransport : ITransportFactory
{
    private readonly ITransport _transport;

    public OpenedTransport(ITransport transport)
    {
        _transport = transport;
    }

    public int? LocalPort { get; private set; }

    public ITransport Open(int localPort, EndPoint opponent)
    {
        LocalPort = localPort;
        return _transport;
    }
}
