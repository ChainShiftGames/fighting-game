using System.Collections.Generic;
using Platform.Steam.Friends;
using Platform.Steam.Lobbies;

namespace FightingGame.UI;

/// <summary>The buttons of the online lobby menu at each <see cref="LobbyStage"/>.</summary>
public static class LobbyMenuItems
{
    /// <summary>Most friends listed with their own button.</summary>
    public const int MaxListedFriends = 8;

    public static List<LobbyMenuItem> For(LobbyFlow lobby)
    {
        var items = new List<LobbyMenuItem>();
        switch (lobby.Stage)
        {
            case LobbyStage.Idle:
                items.Add(new LobbyMenuItem(LobbyChoice.Host, "Host a lobby"));
                AddFriends(items, lobby, LobbyChoice.JoinFriend);
                items.Add(new LobbyMenuItem(LobbyChoice.MainMenu, "Back"));
                return items;
            case LobbyStage.Entering:
                items.Add(new LobbyMenuItem(LobbyChoice.Leave, "Cancel"));
                return items;
            case LobbyStage.InLobby when !lobby.HasOpponent:
                items.Add(new LobbyMenuItem(LobbyChoice.InviteThroughOverlay, "Invite through the Steam overlay"));
                AddFriends(items, lobby, LobbyChoice.InviteFriend);
                break;
            case LobbyStage.InLobby:
                items.Add(new LobbyMenuItem(
                    LobbyChoice.Ready, lobby.IsLocalReady ? "Ready — waiting" : "Ready", Enabled: !lobby.IsLocalReady));
                break;
            case LobbyStage.Rematch:
                items.Add(new LobbyMenuItem(
                    LobbyChoice.Ready, lobby.IsLocalReady ? "Rematch — waiting" : "Rematch", Enabled: !lobby.IsLocalReady));
                items.Add(new LobbyMenuItem(LobbyChoice.MainMenu, "Main menu"));
                return items;
            case LobbyStage.Starting or LobbyStage.InMatch:
                return items;
        }

        items.Add(new LobbyMenuItem(LobbyChoice.Leave, "Leave the lobby"));
        return items;
    }

    // Friends running the game: everyone can be invited, only those in a lobby can be joined.
    private static void AddFriends(List<LobbyMenuItem> items, LobbyFlow lobby, LobbyChoice choice)
    {
        for (int i = 0; i < lobby.InvitableFriends.Count && i < MaxListedFriends; i++)
        {
            SteamFriend friend = lobby.InvitableFriends[i];
            if (choice == LobbyChoice.JoinFriend && !friend.HasLobby)
            {
                continue;
            }

            string label = choice == LobbyChoice.JoinFriend ? $"Join {friend.Name}'s lobby" : $"Invite {friend.Name}";
            items.Add(new LobbyMenuItem(choice, label, FriendIndex: i));
        }
    }
}
