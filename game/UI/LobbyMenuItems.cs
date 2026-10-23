using System.Collections.Generic;
using FightingGame.UI.Framework;
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
                items.Add(new LobbyMenuItem(LobbyChoice.Host, MenuText.Key("LOBBY_HOST")));
                AddFriends(items, lobby, LobbyChoice.JoinFriend);
                items.Add(new LobbyMenuItem(LobbyChoice.MainMenu, MenuText.Key("MENU_BACK")));
                return items;
            case LobbyStage.Entering:
                items.Add(new LobbyMenuItem(LobbyChoice.Leave, MenuText.Key("LOBBY_CANCEL")));
                return items;
            case LobbyStage.InLobby when !lobby.HasOpponent:
                items.Add(new LobbyMenuItem(LobbyChoice.InviteThroughOverlay, MenuText.Key("LOBBY_INVITE_OVERLAY")));
                AddFriends(items, lobby, LobbyChoice.InviteFriend);
                break;
            case LobbyStage.InLobby:
                items.Add(ReadyItem(lobby, "LOBBY_READY", "LOBBY_READY_WAITING"));
                break;
            case LobbyStage.Rematch:
                items.Add(ReadyItem(lobby, "MENU_REMATCH", "LOBBY_REMATCH_WAITING"));
                items.Add(new LobbyMenuItem(LobbyChoice.MainMenu, MenuText.Key("MENU_MAIN_MENU")));
                return items;
            case LobbyStage.Starting or LobbyStage.InMatch:
                return items;
        }

        items.Add(new LobbyMenuItem(LobbyChoice.Leave, MenuText.Key("LOBBY_LEAVE")));
        return items;
    }

    // Ready (or rematch) can be chosen once; after that it says the other player is awaited.
    private static LobbyMenuItem ReadyItem(LobbyFlow lobby, string readyKey, string waitingKey) => new(
        LobbyChoice.Ready, MenuText.Key(lobby.IsLocalReady ? waitingKey : readyKey), Enabled: !lobby.IsLocalReady);

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

            string key = choice == LobbyChoice.JoinFriend ? "LOBBY_JOIN_FRIEND" : "LOBBY_INVITE_FRIEND";
            MenuText label = MenuText.Key(key, friend.Name);
            items.Add(new LobbyMenuItem(choice, label, FriendIndex: i));
        }
    }
}
