using System.Net;
using Netcode.Online;
using Netcode.Transport;

namespace Platform.Steam.Transport;

/// <summary>Steam Networking Messages as the peer transport for one opponent.</summary>
public static class SteamTransport
{
    public static PeerTransport For(ulong opponentSteamId) =>
        new(new Factory(opponentSteamId), new SteamSessionLink(opponentSteamId));

    // Steam has no ports, and the opponent is fixed by its Steam ID.
    private sealed class Factory(ulong opponentSteamId) : ITransportFactory
    {
        public ITransport Open(int localPort, EndPoint opponent) => new SteamMessagesTransport(opponentSteamId);
    }
}
