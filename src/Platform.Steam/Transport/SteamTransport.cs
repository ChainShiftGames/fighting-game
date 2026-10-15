using Backdash.Network.Client;
using Backdash.Options;
using Netcode.Online;

namespace Platform.Steam.Transport;

/// <summary>Steam Networking Messages as a peer transport for one opponent.</summary>
public static class SteamTransport
{
    public static PeerTransport For(ulong opponentSteamId) => new(new SocketFactory(opponentSteamId), new SteamSessionLink(opponentSteamId));

    private sealed class SocketFactory(ulong opponentSteamId) : IPeerSocketFactory
    {
        public IPeerSocket Create(int port, NetcodeOptions options) => new SteamMessagesSocket(opponentSteamId);
    }
}
