using System;
using System.Runtime.InteropServices;
using Steamworks;

namespace Platform.Steam.Transport;

/// <summary>
/// Turns off Steam's direct peer-to-peer route (ICE), so every packet goes through Valve's relay and neither player
/// learns the other's IP address (ADR-0005 §1). Left on, Steam connected the P0-21 test directly.
/// </summary>
internal static class SteamRelayOnly
{
    public static void Apply()
    {
        IntPtr value = Marshal.AllocHGlobal(sizeof(int));
        try
        {
            Marshal.WriteInt32(value, Constants.k_nSteamNetworkingConfig_P2P_Transport_ICE_Enable_Disable);
            bool applied = SteamNetworkingUtils.SetConfigValue(
                ESteamNetworkingConfigValue.k_ESteamNetworkingConfig_P2P_Transport_ICE_Enable,
                ESteamNetworkingConfigScope.k_ESteamNetworkingConfig_Global,
                IntPtr.Zero,
                ESteamNetworkingConfigDataType.k_ESteamNetworkingConfig_Int32,
                value);
            if (!applied)
            {
                throw new InvalidOperationException("Steam refused to turn off direct connections; refusing to expose IP addresses.");
            }
        }
        finally
        {
            Marshal.FreeHGlobal(value);
        }
    }
}
