using Backdash.Network.Client;
using Backdash.Options;
using Netcode.Online;

namespace Platform.Steam.Transport;

/// <summary>The Steam relay as a peer transport for one opponent.</summary>
public static class SteamTransport
{
    public static PeerTransport For(ulong opponentSteamId) => new(new SocketFactory(opponentSteamId), new SteamRelayLink(opponentSteamId));

    private sealed class SocketFactory(ulong opponentSteamId) : IPeerSocketFactory
    {
        public IPeerSocket Create(int port, NetcodeOptions options) => new SteamMessagesSocket(opponentSteamId);
    }
}
