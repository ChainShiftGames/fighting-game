using System.Collections.Generic;
using FightingGame.UI.Framework;
using Platform.Steam.Lobbies;

namespace FightingGame.UI;

/// <summary>What the online lobby menu says above its buttons at each <see cref="LobbyStage"/>: who is there, who is ready.</summary>
public static class LobbyMenuText
{
    /// <param name="lastAction">The menu's own note on the last button chosen (e.g. an invite sent); may be empty.</param>
    /// <param name="matchNote">What the finished match underneath says (e.g. a desync); may be empty.</param>
    public static List<MenuText> Describe(LobbyFlow lobby, MenuText lastAction, string matchNote = "")
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
                lines.Add(MenuText.Key("LOBBY_WAITING"));
                lines.Add(MenuText.Key("LOBBY_WAITING_INVITE"));
                break;
            case LobbyStage.InLobby:
                lines.Add(MenuText.Key("LOBBY_PLAYERS", Players(lobby)));
                lines.Add(MenuText.Empty);
                AddReadiness(lines, lobby, "LOBBY_STATE_READY");
                break;
            case LobbyStage.Starting:
                lines.Add(Players(lobby));
                lines.Add(MenuText.Empty);
                lines.Add(MenuText.Key("LOBBY_STARTING"));
                break;
            case LobbyStage.InMatch:
                return lines;
            case LobbyStage.Rematch:
                AddReadiness(lines, lobby, "LOBBY_STATE_REMATCH");
                break;
        }

        // Each note gets a blank line before it.
        foreach (MenuText note in (MenuText[])[MenuText.Plain(matchNote), MenuText.Plain(lobby.Notice), lastAction])
        {
            if (!note.IsEmpty)
            {
                lines.Add(MenuText.Empty);
                lines.Add(note);
            }
        }

        return lines;
    }

    private static void AddReadiness(List<MenuText> lines, LobbyFlow lobby, string readyKey)
    {
        lines.Add(MenuText.Key("LOBBY_YOU_ARE", State(lobby.IsLocalReady, readyKey)));
        MenuText opponent = MenuText.Plain(lobby.OpponentName);
        lines.Add(MenuText.Key("LOBBY_OPPONENT_IS", opponent, State(lobby.IsOpponentReady, readyKey)));
    }

    private static MenuText State(bool isReady, string readyKey) => MenuText.Key(isReady ? readyKey : "LOBBY_STATE_NOT_YET");

    private static MenuText Players(LobbyFlow lobby) =>
        MenuText.Key(lobby.IsHost ? "LOBBY_HOST_VS" : "LOBBY_GUEST_VS", lobby.OpponentName);
}
