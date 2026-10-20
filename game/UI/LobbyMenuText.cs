using System.Text;
using Platform.Steam.Lobbies;

namespace FightingGame.UI;

/// <summary>What the online lobby menu says above its buttons at each <see cref="LobbyStage"/>: who is there, who is ready.</summary>
public static class LobbyMenuText
{
    /// <param name="lastAction">The menu's own note on the last button chosen (e.g. an invite sent); may be empty.</param>
    /// <param name="matchNote">What the finished match underneath says (e.g. a desync); may be empty.</param>
    public static string Describe(LobbyFlow lobby, string lastAction, string matchNote = "")
    {
        var text = new StringBuilder();
        switch (lobby.Stage)
        {
            case LobbyStage.Idle:
                text.Append("Host a lobby and invite a friend, or join a friend's lobby.\n");
                text.Append("A friend's invite accepted in Steam also joins.\n");
                break;
            case LobbyStage.Entering:
                text.Append("Entering the lobby…\n");
                break;
            case LobbyStage.InLobby when !lobby.HasOpponent:
                text.Append("Lobby: waiting for an opponent.\nInvite a friend, or they join from their Online menu.\n");
                break;
            case LobbyStage.InLobby:
                text.Append($"Lobby: {Players(lobby)}\n\n");
                AppendReadiness(text, lobby, "ready");
                break;
            case LobbyStage.Starting:
                text.Append($"{Players(lobby)}\n\nBoth ready: starting the match…\n");
                break;
            case LobbyStage.InMatch:
                return string.Empty;
            case LobbyStage.Rematch:
                AppendReadiness(text, lobby, "wants a rematch");
                break;
        }

        return AppendNotes(text, matchNote, lobby.Notice, lastAction).ToString().TrimEnd('\n');
    }

    private static void AppendReadiness(StringBuilder text, LobbyFlow lobby, string readyWord)
    {
        text.Append($"You: {(lobby.IsLocalReady ? readyWord : "not yet")}\n");
        text.Append($"{lobby.OpponentName}: {(lobby.IsOpponentReady ? readyWord : "not yet")}\n");
    }

    private static string Players(LobbyFlow lobby) =>
        lobby.IsHost ? $"you (P1) vs {lobby.OpponentName} (P2)" : $"{lobby.OpponentName} (P1) vs you (P2)";

    private static StringBuilder AppendNotes(StringBuilder text, params string[] notes)
    {
        foreach (string note in notes)
        {
            if (note.Length > 0)
            {
                text.Append('\n').Append(note).Append('\n');
            }
        }

        return text;
    }
}
