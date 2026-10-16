using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Netcode.Transport;

namespace Netcode.Tests.Transport;

/// <summary>One end of a transport pair inside this process. Records what it sends and takes extra packets.</summary>
internal sealed class InMemoryTransport : ITransport
{
    private readonly Channel<byte[]> _inbox = Channel.CreateUnbounded<byte[]>();
    private InMemoryTransport? _other;
    private volatile bool _cut;

    /// <summary>Every packet sent from this end, in order.</summary>
    public ConcurrentQueue<byte[]> Sent { get; } = new();

    public static (InMemoryTransport Player1, InMemoryTransport Player2) CreatePair()
    {
        var player1 = new InMemoryTransport();
        var player2 = new InMemoryTransport { _other = player1 };
        player1._other = player2;
        return (player1, player2);
    }

    /// <summary>Hands this end a packet as if the other PC had sent it.</summary>
    public void Deliver(byte[] packet) => _inbox.Writer.TryWrite(packet);

    /// <summary>Cuts the link from this end: what it sends from now on never arrives, as when a cable is pulled.</summary>
    public void Cut() => _cut = true;

    public void Send(ReadOnlySpan<byte> packet)
    {
        byte[] copy = packet.ToArray();
        Sent.Enqueue(copy);
        if (!_cut)
        {
            _other?.Deliver(copy);
        }
    }

    public async ValueTask<int> ReceiveAsync(Memory<byte> buffer, CancellationToken cancellationToken)
    {
        byte[] packet = await _inbox.Reader.ReadAsync(cancellationToken).ConfigureAwait(false);
        packet.AsSpan(0, Math.Min(packet.Length, buffer.Length)).CopyTo(buffer.Span);
        return packet.Length;
    }

    public void Poll()
    {
    }

    public void Dispose() => _inbox.Writer.TryComplete();
}
