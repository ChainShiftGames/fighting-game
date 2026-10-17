using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Netcode.Desync;
using Netcode.Transport;
using Xunit;

namespace Netcode.Tests.Transport;

/// <summary>
/// ADR-0019: checksums sent over an unreliable transport arrive in frame order, each once, despite loss, duplicates
/// and reordering, without a resend timer while the match runs.
/// </summary>
public sealed class TransportChecksumLinkTests
{
    private const int Capacity = 16;

    // Unconnected transports: the test picks which sent packets arrive, how often and in which order.
    private readonly InMemoryTransport _player1Transport = new();
    private readonly InMemoryTransport _player2Transport = new();
    private readonly TransportChecksumLink _player1 = new(Capacity);
    private readonly TransportChecksumLink _player2 = new(Capacity);

    public TransportChecksumLinkTests()
    {
        _player1.Attach(_player1Transport);
        _player2.Attach(_player2Transport);
    }

    [Fact]
    public void EachChecksumArrivesOnceInFrameOrderDespiteLossDuplicatesAndReordering()
    {
        for (int frame = 1; frame <= 6; frame++)
        {
            SendAndWait(_player1, _player1Transport, Checksum(frame));
        }

        byte[][] sent = _player1Transport.Sent.ToArray();

        // Packet 3 is lost, 5 arrives twice, 2 arrives after 4.
        foreach (int packet in new[] { 0, 3, 1, 4, 4, 5 })
        {
            Assert.True(_player2.Deliver(sent[packet]));
        }

        Assert.Equal([Checksum(1), Checksum(2), Checksum(3), Checksum(4), Checksum(5), Checksum(6)], Received(_player2));
    }

    [Fact]
    public void ChecksumsAreRepeatedUntilTheOtherPcAcknowledgesThem()
    {
        SendAndWait(_player1, _player1Transport, Checksum(1));
        SendAndWait(_player1, _player1Transport, Checksum(2));
        Assert.Equal(2, ChecksumsIn(Last(_player1Transport)));

        _player2.Deliver(Last(_player1Transport));
        SendAndWait(_player2, _player2Transport, Checksum(1));
        _player1.Deliver(Last(_player2Transport));
        SendAndWait(_player1, _player1Transport, Checksum(3));

        Assert.True(ChecksumMessage.TryRead(Last(_player1Transport), out int acknowledged, out int firstFrame, out int count));
        Assert.Equal((1, 3, 1), (acknowledged, firstFrame, count));
    }

    [Fact]
    public void ChecksumsAfterAGapWaitForTheRepeat()
    {
        Assert.True(_player2.Deliver(Packet(acknowledged: 0, firstFrame: 3, 33)));
        Assert.Empty(Received(_player2));

        Assert.True(_player2.Deliver(Packet(acknowledged: 0, firstFrame: 1, 11, 22, 33)));

        Assert.Equal([new FrameChecksum(1, 11), new FrameChecksum(2, 22), new FrameChecksum(3, 33)], Received(_player2));
    }

    [Fact]
    public void ChecksumsMustGoOutInFrameOrder()
    {
        _player1.Send(Checksum(1));

        Assert.Throws<ArgumentException>(() => _player1.Send(Checksum(3)));
        Assert.Throws<ArgumentException>(() => _player1.Send(Checksum(1)));
    }

    [Fact]
    public void AnOtherPcThatNeverAcknowledgesFailsLoudly()
    {
        for (int frame = 1; frame <= Capacity; frame++)
        {
            _player1.Send(Checksum(frame));
        }

        Assert.Throws<InvalidOperationException>(() => _player1.Send(Checksum(Capacity + 1)));
    }

    [Fact]
    public void AReaderThatFallsBehindGetsTheRestFromTheRepeats()
    {
        for (int frame = 1; frame <= Capacity + 4; frame++)
        {
            SendAndWait(_player1, _player1Transport, Checksum(frame));
            _player2.Deliver(Last(_player1Transport));
            if (frame == Capacity)
            {
                // P2 acknowledges only what fitted, so P1 keeps sending the rest.
                SendAndWait(_player2, _player2Transport, Checksum(1));
                _player1.Deliver(Last(_player2Transport));
            }
        }

        List<FrameChecksum> first = Received(_player2);
        _player2.Deliver(Last(_player1Transport));
        List<FrameChecksum> rest = Received(_player2);

        Assert.Equal(Capacity, first.Count);
        Assert.Equal(Capacity + 4, first.Count + rest.Count);
        Assert.Equal(Checksum(Capacity + 4), rest[^1]);
    }

    [Fact]
    public void AnAcknowledgementOfFramesNeverSentIsMalformed()
    {
        _player1.Send(Checksum(1));

        Assert.False(_player1.Deliver(Packet(acknowledged: 2, firstFrame: 1)));
        Assert.False(_player1.Deliver([0x46, 0x43, 9]));
    }

    [Fact]
    public async Task AfterADesyncTheLastChecksumsAreSentAgainUntilAcknowledged()
    {
        SendAndWait(_player1, _player1Transport, Checksum(1));
        int sentBefore = _player1Transport.Sent.Count;

        _player1.FinishAfterDesync();

        var clock = Stopwatch.StartNew();
        while (_player1Transport.Sent.Count < sentBefore + 2 && clock.Elapsed < TimeSpan.FromSeconds(5))
        {
            await Task.Delay(20, TestContext.Current.CancellationToken);
        }

        Assert.True(_player1Transport.Sent.Count >= sentBefore + 2);
        _player1.Deliver(Packet(acknowledged: 1, firstFrame: 1));
        await Task.Delay(300, TestContext.Current.CancellationToken);
        int afterAcknowledged = _player1Transport.Sent.Count;
        await Task.Delay(300, TestContext.Current.CancellationToken);
        Assert.Equal(afterAcknowledged, _player1Transport.Sent.Count);
    }

    [Fact]
    public void AClosedTransportLosesThePacketWithoutFailing()
    {
        var transport = new ClosedTransport();
        var link = new TransportChecksumLink(Capacity);
        link.Attach(transport);

        link.Send(Checksum(1));
        SpinWait.SpinUntil(() => transport.Sends > 0, TimeSpan.FromSeconds(5));
        link.Send(Checksum(2));

        Assert.Equal(1, transport.Sends);
    }

    private static FrameChecksum Checksum(int frame) => new(frame, (ulong)frame * 0x9E3779B97F4A7C15);

    // Packets go out from the thread pool: waits for this send's own packet, so the test can pick it.
    private static void SendAndWait(TransportChecksumLink link, InMemoryTransport transport, FrameChecksum checksum)
    {
        int sentBefore = transport.Sent.Count;
        link.Send(checksum);
        var clock = Stopwatch.StartNew();
        while (transport.Sent.Count == sentBefore)
        {
            Assert.True(clock.Elapsed < TimeSpan.FromSeconds(5), "The packet never went out.");
            Thread.Sleep(1);
        }
    }

    private static byte[] Last(InMemoryTransport transport) => transport.Sent.ToArray()[^1];

    private static int ChecksumsIn(byte[] packet)
    {
        Assert.True(ChecksumMessage.TryRead(packet, out _, out _, out int count));
        return count;
    }

    private static byte[] Packet(int acknowledged, int firstFrame, params ulong[] checksums)
    {
        byte[] packet = new byte[ChecksumMessage.MaxSize];
        int length = ChecksumMessage.Write(acknowledged, firstFrame, checksums, packet);
        return packet[..length];
    }

    private static List<FrameChecksum> Received(TransportChecksumLink link)
    {
        var received = new List<FrameChecksum>();
        while (link.TryReceive(out FrameChecksum checksum))
        {
            received.Add(checksum);
        }

        return received;
    }

    private sealed class ClosedTransport : ITransport
    {
        private int _sends;

        public int Sends => Volatile.Read(ref _sends);

        public void Send(ReadOnlySpan<byte> packet)
        {
            Interlocked.Increment(ref _sends);
            throw new ObjectDisposedException(nameof(ClosedTransport));
        }

        public ValueTask<int> ReceiveAsync(Memory<byte> buffer, CancellationToken cancellationToken) =>
            throw new ObjectDisposedException(nameof(ClosedTransport));

        public void Poll()
        {
        }

        public void Dispose()
        {
        }
    }
}
