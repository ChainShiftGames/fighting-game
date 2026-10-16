using System;
using System.Threading;
using System.Threading.Tasks;
using Netcode.Transport;

namespace Netcode.Handshake;

/// <summary>
/// Runs the version handshake on a match's transport, ahead of the netcode: hellos go out from here, and the
/// netcode's packets are held back both ways until the handshake agrees.
/// </summary>
/// <remarks>
/// Held-back netcode packets are dropped, not queued: the netcode's connection setup resends until it gets an
/// answer, so the match starts a moment after agreement. Junk passes through, for the transport socket to count.
/// </remarks>
internal sealed class HandshakeTransport : ITransport
{
    // A hello every 6 frames (100 ms) until agreement; after a refusal they keep going, so the other PC learns too.
    private const int PollsBetweenHellos = 6;

    private readonly ITransport _transport;
    private readonly VersionHandshake _handshake;
    private readonly byte[] _hello = new byte[ITransport.MaxPacketSize];
    private int _pollsUntilHello;

    public HandshakeTransport(ITransport transport, VersionHandshake handshake)
    {
        _transport = transport;
        _handshake = handshake;
    }

    public void Send(ReadOnlySpan<byte> packet)
    {
        if (_handshake.Outcome == HandshakeOutcome.Agreed)
        {
            _transport.Send(packet);
        }
    }

    public async ValueTask<int> ReceiveAsync(Memory<byte> buffer, CancellationToken cancellationToken)
    {
        while (true)
        {
            int length = await _transport.ReceiveAsync(buffer, cancellationToken).ConfigureAwait(false);
            if (length > buffer.Length)
            {
                return length;
            }

            ReadOnlySpan<byte> packet = buffer.Span[..length];
            if (HelloMessage.IsHello(packet))
            {
                Read(packet);
            }
            else if (!BackdashPacketFormat.IsWellFormed(packet) || _handshake.OnNetcodePacket())
            {
                return length;
            }
        }
    }

    // On the game thread, once per frame: the only place hellos are written, so one buffer serves them all.
    public void Poll()
    {
        _transport.Poll();
        if (--_pollsUntilHello > 0 || _handshake.Outcome == HandshakeOutcome.Agreed)
        {
            return;
        }

        _pollsUntilHello = PollsBetweenHellos;
        int length = HelloMessage.Write(_handshake.Local, _handshake.HasMatchedTheirs, _hello);
        _transport.Send(_hello.AsSpan(0, length));
    }

    public void Dispose() => _transport.Dispose();

    private void Read(ReadOnlySpan<byte> hello)
    {
        if (HelloMessage.TryRead(hello, out MatchIdentity? theirs, out bool theySawOurs))
        {
            _handshake.OnHello(theirs, theySawOurs);
        }
        else if (HelloMessage.FormatOf(hello) is int format and >= 0 && format != HelloMessage.Format)
        {
            _handshake.OnHelloOfAnotherFormat(format);
        }

        // A cut-off hello of our own format is a lost packet; the next one comes shortly.
    }
}
