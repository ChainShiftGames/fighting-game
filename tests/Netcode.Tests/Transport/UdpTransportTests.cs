using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Netcode.Transport;
using Xunit;

namespace Netcode.Tests.Transport;

/// <summary>P2-03: direct UDP survives what the network throws at it, so the netcode's receive loop never stops.</summary>
public sealed class UdpTransportTests
{
    // Apart from the ports of the other tests and the benchmark.
    private const int FirstPort = 47051;

    [Fact]
    public async Task AnOversizedDatagramArrivesAsItsFullLengthNotAsAnError()
    {
        using var sender = Open(FirstPort, FirstPort + 1);
        using var receiver = Open(FirstPort + 1, FirstPort);
        byte[] buffer = new byte[ITransport.MaxPacketSize];

        sender.Send(new byte[ITransport.MaxPacketSize * 3]);
        sender.Send([7]);

        Assert.Equal(ITransport.MaxPacketSize * 3, await ReceiveAsync(receiver, buffer));
        Assert.Equal(1, await ReceiveAsync(receiver, buffer));
        Assert.Equal(7, buffer[0]);
    }

    [Fact]
    public async Task PacketsFromAnyoneButTheOpponentAreIgnored()
    {
        using var opponent = Open(FirstPort + 2, FirstPort + 3);
        using var receiver = Open(FirstPort + 3, FirstPort + 2);
        using var stranger = new UdpClient(new IPEndPoint(IPAddress.Loopback, FirstPort + 4));
        byte[] buffer = new byte[ITransport.MaxPacketSize];

        await stranger.SendAsync(new byte[] { 9, 9 }, new IPEndPoint(IPAddress.Loopback, FirstPort + 3), TestContext.Current.CancellationToken);
        opponent.Send([1]);

        Assert.Equal(1, await ReceiveAsync(receiver, buffer));
        Assert.Equal(1, buffer[0]);
    }

    // Windows reports a packet sent to a closed port as an error on the next receive, unless the transport opts out.
    [Fact]
    public async Task SendingToAClosedPortIsALostPacket()
    {
        using var early = Open(FirstPort + 5, FirstPort + 6);
        byte[] buffer = new byte[ITransport.MaxPacketSize];
        early.Send([1]);
        await Task.Delay(100, TestContext.Current.CancellationToken);

        using var late = Open(FirstPort + 6, FirstPort + 5);
        late.Send([2]);

        Assert.Equal(1, await ReceiveAsync(early, buffer));
        Assert.Equal(2, buffer[0]);
    }

    private static UdpTransport Open(int localPort, int opponentPort) =>
        new(localPort, new IPEndPoint(IPAddress.Loopback, opponentPort));

    private static async Task<int> ReceiveAsync(UdpTransport transport, byte[] buffer)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(5));
        return await transport.ReceiveAsync(buffer, timeout.Token);
    }
}
