using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Backdash.Network.Client;

namespace Netcode.Transport;

/// <summary>
/// Backdash's socket over an <see cref="ITransport"/>, for a session with one opponent. Every packet that arrives is
/// checked before Backdash reads it; oversized and malformed ones are dropped and counted. The desync check's
/// packets travel alongside (ADR-0019) and go to their link instead.
/// </summary>
/// <remarks>The only place transports meet Backdash, so transports stay free of Backdash types (ADR-0005 §2).</remarks>
internal sealed class TransportSocket : IPeerSocket
{
    private readonly ITransport _transport;
    private readonly EndPoint _opponent;
    private readonly SocketAddress _opponentAddress;
    private readonly DroppedPackets _dropped;
    private readonly TransportChecksumLink? _checksums;

    /// <param name="opponent">The other PC, as the session's remote player knows it.</param>
    /// <param name="port">The port Backdash was told this socket has.</param>
    /// <param name="checksums">Takes the desync check's packets; without it they count as malformed.</param>
    public TransportSocket(
        ITransport transport, EndPoint opponent, int port, DroppedPackets dropped, TransportChecksumLink? checksums = null)
    {
        _transport = transport;
        _opponent = opponent;
        _opponentAddress = opponent.Serialize();
        _dropped = dropped;
        _checksums = checksums;
        Port = port;
    }

    public AddressFamily AddressFamily => _opponent.AddressFamily;

    public int Port { get; }

    public async ValueTask<int> ReceiveFromAsync(Memory<byte> buffer, SocketAddress address, CancellationToken cancellationToken)
    {
        int length = await ReceiveCheckedAsync(buffer, cancellationToken).ConfigureAwait(false);

        // Backdash tells peers apart by these bytes, and every packet the transport hands over is the opponent's.
        _opponentAddress.Buffer.CopyTo(address.Buffer);
        address.Size = _opponentAddress.Size;
        return length;
    }

    public async ValueTask<SocketReceiveFromResult> ReceiveAsync(Memory<byte> buffer, CancellationToken cancellationToken)
    {
        int length = await ReceiveCheckedAsync(buffer, cancellationToken).ConfigureAwait(false);
        return new SocketReceiveFromResult { ReceivedBytes = length, RemoteEndPoint = _opponent };
    }

    public ValueTask<int> SendToAsync(ReadOnlyMemory<byte> buffer, SocketAddress socketAddress, CancellationToken cancellationToken) =>
        ValueTask.FromResult(SendToOpponent(buffer.Span, socketAddress.Equals(_opponentAddress), socketAddress));

    public ValueTask<int> SendToAsync(ReadOnlyMemory<byte> buffer, EndPoint remoteEndPoint, CancellationToken cancellationToken) =>
        ValueTask.FromResult(SendToOpponent(buffer.Span, remoteEndPoint.Equals(_opponent), remoteEndPoint));

    public void Update() => _transport.Poll();

    public void Close() => _transport.Dispose();

    public void Dispose() => _transport.Dispose();

    // Backdash's buffer is far bigger than a packet may be; offering only MaxPacketSize makes the transport report
    // anything longer as oversized.
    private async ValueTask<int> ReceiveCheckedAsync(Memory<byte> buffer, CancellationToken cancellationToken)
    {
        Memory<byte> packetSpace = buffer[..Math.Min(buffer.Length, ITransport.MaxPacketSize)];
        while (true)
        {
            int length = await _transport.ReceiveAsync(packetSpace, cancellationToken).ConfigureAwait(false);
            if (length > packetSpace.Length)
            {
                _dropped.CountOversized();
            }
            else if (ChecksumMessage.IsChecksumMessage(packetSpace.Span[..length]))
            {
                if (_checksums is null || !_checksums.Deliver(packetSpace.Span[..length]))
                {
                    _dropped.CountMalformed();
                }
            }
            else if (!BackdashPacketFormat.IsWellFormed(packetSpace.Span[..length]))
            {
                _dropped.CountMalformed();
            }
            else
            {
                return length;
            }
        }
    }

    // A transport reaches only the opponent; Backdash sending elsewhere (spectators) would be a setup mistake.
    private int SendToOpponent(ReadOnlySpan<byte> packet, bool isOpponent, object recipient)
    {
        if (!isOpponent)
        {
            throw new ArgumentException($"This session's transport only reaches {_opponent}, not {recipient}.");
        }

        _transport.Send(packet);
        return packet.Length;
    }
}
