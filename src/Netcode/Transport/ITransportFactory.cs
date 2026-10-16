using System.Net;

namespace Netcode.Transport;

/// <summary>Opens a fresh <see cref="ITransport"/> for each online match session against one opponent.</summary>
public interface ITransportFactory
{
    /// <param name="localPort">Port to listen on; 0 for transports without ports (Steam), which ignore it.</param>
    /// <param name="opponent">The other PC: an <see cref="IPEndPoint"/> for UDP, or the transport's own endpoint.</param>
    ITransport Open(int localPort, EndPoint opponent);
}
