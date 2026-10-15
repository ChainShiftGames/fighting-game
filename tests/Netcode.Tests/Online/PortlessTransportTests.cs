using System.Net;
using Backdash.Network.Client;
using Backdash.Options;
using Netcode.Drivers;
using Netcode.Online;
using Sim.Toy;
using Xunit;

namespace Netcode.Tests.Online;

/// <summary>Relay transports (Steam) have no local port; a session over one must still start.</summary>
public sealed class PortlessTransportTests
{
    private const int TestSocketPort = 47031;

    [Fact]
    public void ASessionStartsWithoutALocalPort()
    {
        var sockets = new StandInRelaySockets();
        var settings = new PeerMatchSettings
        {
            RemoteEndPoint = new IPEndPoint(IPAddress.Loopback, TestSocketPort + 1),
            LocalIsPlayer1 = true,
        };

        using IMatchDriver<ToyState> driver = PeerSession.CreateDriver(
            new ToyMatchSimulation(new ToySimulation(ToyRules.Default)), settings, new PeerTransport(sockets));

        Assert.True(sockets.Created);
    }

    // Stands in for the Steam relay: ignores the port it's given and opens its own socket.
    private sealed class StandInRelaySockets : IPeerSocketFactory
    {
        public bool Created { get; private set; }

        public IPeerSocket Create(int port, NetcodeOptions options)
        {
            Created = true;
            return new UdpSocket(TestSocketPort, options.UseIPv6);
        }
    }
}
