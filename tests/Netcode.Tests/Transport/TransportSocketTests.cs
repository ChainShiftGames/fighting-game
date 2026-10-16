using System;
using System.Buffers.Binary;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Netcode.Transport;
using Sim.State;
using Xunit;

namespace Netcode.Tests.Transport;

/// <summary>P2-03: malformed and oversized packets are dropped and counted; the match carries on.</summary>
public sealed class TransportSocketTests
{
    private const int FightingFrames = 600;
    private const int SettleFrames = 60;
    private const int StepsBetweenJunk = 10;

    // Backdash receives into a buffer far bigger than any packet may be.
    private const int BackdashBufferSize = 65_527;

    private static readonly IPEndPoint _opponent = new(IPAddress.Loopback, 7001);

    // A keep-alive: a header with type 6 and nothing after it.
    private static readonly byte[] _keepAlive = [0, 6, 0, 0, 0, 0];

    [Fact]
    public async Task OversizedAndMalformedPacketsAreDroppedAndCounted()
    {
        using var transport = new InMemoryTransport();
        var dropped = new DroppedPackets();
        using var socket = new TransportSocket(transport, _opponent, 1, dropped);
        transport.Deliver(new byte[ITransport.MaxPacketSize + 1]);
        transport.Deliver([1, 2, 3]);
        transport.Deliver(_keepAlive);

        int length = await ReceiveAsync(socket, new SocketAddress(AddressFamily.InterNetwork));

        Assert.Equal(_keepAlive.Length, length);
        Assert.Equal(1, dropped.Oversized);
        Assert.Equal(1, dropped.Malformed);
    }

    [Fact]
    public async Task APacketOfTheMostAllowedSizeIsCheckedNotDroppedAsOversized()
    {
        using var transport = new InMemoryTransport();
        var dropped = new DroppedPackets();
        using var socket = new TransportSocket(transport, _opponent, 1, dropped);
        transport.Deliver(new byte[ITransport.MaxPacketSize]);
        transport.Deliver(_keepAlive);

        await ReceiveAsync(socket, new SocketAddress(AddressFamily.InterNetwork));

        Assert.Equal(0, dropped.Oversized);
        Assert.Equal(1, dropped.Malformed);
    }

    [Fact]
    public async Task EveryPacketComesFromTheOpponentsAddress()
    {
        using var transport = new InMemoryTransport();
        using var socket = new TransportSocket(transport, _opponent, 1, new DroppedPackets());
        transport.Deliver(_keepAlive);
        var address = new SocketAddress(AddressFamily.InterNetwork);

        await ReceiveAsync(socket, address);

        Assert.Equal(_opponent.Serialize(), address);
    }

    [Fact]
    public async Task PacketsToTheOpponentGoOutAndPacketsToAnyoneElseAreRefused()
    {
        using var transport = new InMemoryTransport();
        using var socket = new TransportSocket(transport, _opponent, 1, new DroppedPackets());

        await socket.SendToAsync(_keepAlive, _opponent.Serialize(), TestContext.Current.CancellationToken);

        Assert.Single(transport.Sent);
        await Assert.ThrowsAsync<ArgumentException>(async () =>
            await socket.SendToAsync(_keepAlive, new IPEndPoint(IPAddress.Loopback, 7002), TestContext.Current.CancellationToken));
    }

    // Without the check, the cut-off and over-full input messages throw inside Backdash's receive loop, which stops
    // receiving for good, and the match stalls.
    [Fact]
    public void AMatchKeepsRunningWhileJunkArrives()
    {
        byte[][] junk =
        [
            [],
            [0xDE, 0xAD, 0xBE, 0xEF, 0x01, 0x02, 0x03, 0x04, 0x05],
            CutOffInputMessage(),
            InputMessageWithPlayers(byte.MaxValue),
            new byte[ITransport.MaxPacketSize * 2],
        ];
        using var match = new InMemoryMatch(FightingFrames);
        int step = 0;

        match.RunUntilFrame(FightingFrames + SettleFrames, () =>
        {
            step++;
            if (step % StepsBetweenJunk == 0)
            {
                byte[] packet = junk[step / StepsBetweenJunk % junk.Length];
                match.Player1Transport.Deliver(packet);
                match.Player2Transport.Deliver(packet);
            }
        });

        Assert.Equal(StateChecksum.Compute(in match.Player1.State), StateChecksum.Compute(in match.Player2.State));
        Assert.True(match.Player1Peer.Dropped.Malformed > 0 && match.Player2Peer.Dropped.Malformed > 0);
        Assert.True(match.Player1Peer.Dropped.Oversized > 0 && match.Player2Peer.Dropped.Oversized > 0);
    }

    private static async Task<int> ReceiveAsync(TransportSocket socket, SocketAddress address)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(5));
        return await socket.ReceiveFromAsync(new byte[BackdashBufferSize], address, timeout.Token);
    }

    // An input message's header and start frame, then nothing: Backdash would read past the end.
    private static byte[] CutOffInputMessage()
    {
        byte[] packet = new byte[10];
        BinaryPrimitives.WriteUInt16BigEndian(packet, RealSessionTraffic.Input);
        return packet;
    }

    // Up to the player count, which Backdash trusts as the number of statuses to write.
    private static byte[] InputMessageWithPlayers(byte players)
    {
        byte[] packet = new byte[16];
        BinaryPrimitives.WriteUInt16BigEndian(packet, RealSessionTraffic.Input);
        packet[15] = players;
        return packet;
    }
}
