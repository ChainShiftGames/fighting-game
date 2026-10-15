using Steamworks;

namespace Platform.Steam.Transport;

/// <summary>Steam Networking addresses a user by an identity struct rather than the bare 64-bit Steam ID.</summary>
internal static class SteamIdentity
{
    public static SteamNetworkingIdentity For(ulong steamId)
    {
        var identity = default(SteamNetworkingIdentity);
        identity.SetSteamID64(steamId);
        return identity;
    }
}
