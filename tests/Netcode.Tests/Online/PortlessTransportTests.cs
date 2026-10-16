using System.Net;
using Netcode.Drivers;
using Netcode.Online;
using Netcode.Tests.Handshake;
using Netcode.Tests.Matches;
using Netcode.Tests.Transport;
using Sim.State;
using Xunit;

namespace Netcode.Tests.Online;

/// <summary>Relay transports (Steam) have no local port; a session over one must still start.</summary>
public sealed class PortlessTransportTests
{
    [Fact]
    public void ASessionStartsWithoutALocalPort()
    {
        using var transport = new InMemoryTransport();
        var transports = new OpenedTransport(transport);
        var settings = new PeerMatchSettings
        {
            RemoteEndPoint = new IPEndPoint(IPAddress.Loopback, 1),
            LocalIsPlayer1 = true,
            RollbackFrames = 8,
        };

        using IMatchDriver<MatchState> driver = PeerSession.CreateDriver(
            new ComposedMatchSimulation(TestmanMirror.Create()), settings, new PeerTransport(transports), SameBuild.Handshake());

        Assert.Equal(0, transports.LocalPort);
    }
}
