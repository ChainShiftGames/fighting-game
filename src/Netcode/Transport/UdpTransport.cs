using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Netcode.Transport;

/// <summary>Plain UDP to the other PC's IP address and port. Development only (ADR-0005 §1, ADR-0014).</summary>
public sealed class UdpTransport : ITransport
{
    // Largest possible UDP payload: every datagram fits, so an oversized one is reported by its length, never as a
    // socket error (Windows fails a receive into a smaller buffer).
    private const int MaxDatagramSize = 65_535;

    // Windows reports an earlier send to a closed port as a failed receive; a lost packet must not end the match.
    private const int StopReportingUnreachablePorts = unchecked((int)0x9800000C);

    private readonly Socket _socket;
    private readonly SocketAddress _opponent;
    private readonly SocketAddress _sender;
    private readonly byte[] _receiveBuffer = new byte[MaxDatagramSize];

    public UdpTransport(int localPort, IPEndPoint opponent)
    {
        _socket = new Socket(opponent.AddressFamily, SocketType.Dgram, ProtocolType.Udp);
        if (OperatingSystem.IsWindows())
        {
            _socket.IOControl(StopReportingUnreachablePorts, [0, 0, 0, 0], null);
        }

        IPAddress anyAddress = opponent.AddressFamily == AddressFamily.InterNetworkV6 ? IPAddress.IPv6Any : IPAddress.Any;
        _socket.Bind(new IPEndPoint(anyAddress, localPort));
        _opponent = opponent.Serialize();
        _sender = new SocketAddress(opponent.AddressFamily, SocketAddress.GetMaximumAddressSize(opponent.AddressFamily));
    }

    public void Send(ReadOnlySpan<byte> packet)
    {
        try
        {
            _socket.SendTo(packet, SocketFlags.None, _opponent);
        }
        catch (SocketException)
        {
            // The network refused it (no route, buffer full): for an unreliable transport that's a lost packet.
        }
    }

    // Only one receive runs at a time (the netcode's receive loop), so the shared buffer and sender are safe.
    public async ValueTask<int> ReceiveAsync(Memory<byte> buffer, CancellationToken cancellationToken)
    {
        while (true)
        {
            int length = await _socket.ReceiveFromAsync(_receiveBuffer, SocketFlags.None, _sender, cancellationToken)
                .ConfigureAwait(false);
            if (!_sender.Equals(_opponent))
            {
                continue;
            }

            _receiveBuffer.AsSpan(0, Math.Min(length, buffer.Length)).CopyTo(buffer.Span);
            return length;
        }
    }

    public void Poll()
    {
        // Packets arrive through ReceiveAsync as soon as the OS has them.
    }

    public void Dispose() => _socket.Dispose();
}
