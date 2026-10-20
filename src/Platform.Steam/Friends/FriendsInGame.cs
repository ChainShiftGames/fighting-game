using System.Collections.Generic;
using Steamworks;

namespace Platform.Steam.Friends;

/// <summary>
/// Finds Steam friends who are running this game right now, so they can get a lobby invite, and the lobby each is in, so
/// it can be joined.
/// </summary>
internal static class FriendsInGame
{
    public static IReadOnlyList<SteamFriend> List()
    {
        var friends = new List<SteamFriend>();
        AppId_t thisGame = SteamUtils.GetAppID();
        int count = SteamFriends.GetFriendCount(EFriendFlags.k_EFriendFlagImmediate);
        for (int i = 0; i < count; i++)
        {
            CSteamID friend = SteamFriends.GetFriendByIndex(i, EFriendFlags.k_EFriendFlagImmediate);
            if (SteamFriends.GetFriendGamePlayed(friend, out FriendGameInfo_t game) && game.m_gameID.AppID() == thisGame)
            {
                ulong lobby = game.m_steamIDLobby.IsValid() ? game.m_steamIDLobby.m_SteamID : SteamFriend.NoLobby;
                friends.Add(new SteamFriend(friend.m_SteamID, SteamFriends.GetFriendPersonaName(friend), lobby));
            }
        }

        return friends;
    }
}
