using Netcode.Online;
using Steamworks;

namespace Platform.Steam.Transport;

/// <summary>Steam's own readings of the Networking Messages session with the opponent: relay ping and delivery.</summary>
public sealed class SteamRelayLink : ITransportLink
{
    private const int UnknownDelivery = -1;

    private readonly ulong _opponentSteamId;

    public SteamRelayLink(ulong opponentSteamId)
    {
        _opponentSteamId = opponentSteamId;
    }

    public bool TryRead(out TransportLinkStats stats)
    {
        SteamNetworkingIdentity opponent = SteamIdentity.For(_opponentSteamId);
        ESteamNetworkingConnectionState state = SteamNetworkingMessages.GetSessionConnectionInfo(
            ref opponent, out SteamNetConnectionInfo_t info, out SteamNetConnectionRealTimeStatus_t status);
        if (state != ESteamNetworkingConnectionState.k_ESteamNetworkingConnectionState_Connected)
        {
            stats = default;
            return false;
        }

        bool relayed = (info.m_nFlags & Constants.k_nSteamNetworkConnectionInfoFlags_Relayed) != 0;
        stats = new TransportLinkStats(status.m_nPing, relayed, DeliveredPercent(status.m_flConnectionQualityLocal));
        return true;
    }

    // Steam reports the share of the opponent's packets that arrive as 0..1, or a negative value while unknown.
    private static int DeliveredPercent(float quality) => quality < 0 ? UnknownDelivery : (int)(quality * 100);
}
