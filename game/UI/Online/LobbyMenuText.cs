using System.Collections.Generic;
using System.Globalization;
using FightingGame.UI.Framework;
using Platform.Steam.Lobbies;

namespace FightingGame.UI.Online;

/// <summary>
/// What the online screens say above their buttons at each <see cref="LobbyStage"/>: both players with their sides and
/// readiness, the connection before the match starts, and what went wrong.
/// </summary>
public static class LobbyMenuText
{
    /// <param name="localName">This player's name on the platform.</param>
    /// <param name="lastAction">The menu's own note on the last button chosen (e.g. an invite sent); may be empty.</param>
    /// <param name="matchNote">What a match that failed to start says; may be empty.</param>
    public static List<MenuText> Describe(LobbyFlow lobby, string localName, MenuText lastAction, string matchNote = "")
    {
        var lines = new List<MenuText>();
        switch (lobby.Stage)
        {
            case LobbyStage.Idle:
                lines.Add(MenuText.Key("LOBBY_IDLE"));
                lines.Add(MenuText.Key("LOBBY_IDLE_INVITES"));
                break;
            case LobbyStage.Entering:
                lines.Add(MenuText.Key("LOBBY_ENTERING"));
                break;
            case LobbyStage.InLobby when !lobby.HasOpponent:
                AddMembers(lines, lobby, localName, "LOBBY_STATE_READY");
                lines.Add(MenuText.Empty);
                lines.Add(MenuText.Key("LOBBY_WAITING"));
                lines.Add(MenuText.Key("LOBBY_WAITING_INVITE"));
                break;
            case LobbyStage.InLobby or LobbyStage.Rematch:
                string readyKey = lobby.Stage == LobbyStage.Rematch ? "LOBBY_STATE_REMATCH" : "LOBBY_STATE_READY";
                AddMembers(lines, lobby, localName, readyKey);
                lines.Add(MenuText.Empty);
                lines.Add(ConnectionText.For(lobby.OpponentPingMilliseconds));
                break;
            case LobbyStage.Picking:
                lines.Add(MenuText.Key("LOBBY_PICKING"));
                break;
            case LobbyStage.Starting:
                lines.Add(MenuText.Key("LOBBY_STARTING"));
                break;
            case LobbyStage.InMatch:
                return lines;
        }

        // Each note gets a blank line before it.
        foreach (MenuText note in (MenuText[])[MenuText.Plain(matchNote), LobbyNoticeText.For(lobby.Notice), lastAction])
        {
            if (!note.IsEmpty)
            {
                lines.Add(MenuText.Empty);
                lines.Add(note);
            }
        }

        return lines;
    }

    // One line per player, P1 first: "P1  Name (you): ready". Alone in the lobby, nobody is ready for anything yet.
    private static void AddMembers(List<MenuText> lines, LobbyFlow lobby, string localName, string readyKey)
    {
        foreach (LobbyMember member in LobbyMembers.Of(lobby, localName))
        {
            MenuText state = !lobby.HasOpponent ? MenuText.Key("LOBBY_STATE_ALONE")
                : MenuText.Key(member.IsReady ? readyKey : "LOBBY_STATE_NOT_YET");
            lines.Add(MenuText.Key(
                member.IsYou ? "LOBBY_MEMBER_YOU" : "LOBBY_MEMBER",
                MenuText.Key("MENU_SELECT_PLAYER", (member.Side + 1).ToString(CultureInfo.InvariantCulture)),
                MenuText.Plain(member.Name),
                state));
        }
    }
}
