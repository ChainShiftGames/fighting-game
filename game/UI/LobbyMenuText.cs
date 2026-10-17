using System.Text;
using Platform.Steam.Lobbies;

namespace FightingGame.UI;

/// <summary>What the placeholder lobby menu says at each <see cref="LobbyStage"/>: who is there, who is ready, the keys.</summary>
public static class LobbyMenuText
{
    /// <summary>Most friends listed with a number key (1–9).</summary>
    public const int MaxListedFriends = 9;

    /// <param name="lastAction">The menu's own note on the last key pressed (e.g. an invite sent); may be empty.</param>
    /// <param name="matchNote">What the finished match underneath says (e.g. a desync); may be empty.</param>
    public static string Describe(LobbyFlow lobby, string lastAction, string matchNote = "")
    {
        var text = new StringBuilder();
        switch (lobby.Stage)
        {
            case LobbyStage.Idle:
                text.Append("Not in a lobby.\n\nH   host a lobby\nOr accept a friend's invite in Steam.\n");
                break;
            case LobbyStage.Entering:
                text.Append("Entering the lobby…\n");
                break;
            case LobbyStage.InLobby when !lobby.HasOpponent:
                AppendInvites(text, lobby);
                break;
            case LobbyStage.InLobby:
                text.Append($"Lobby: {Players(lobby)}\n\n");
                AppendReadiness(text, lobby, "ready", "Enter   ready");
                break;
            case LobbyStage.Starting:
                text.Append($"{Players(lobby)}\n\nBoth ready: starting the match…\n");
                break;
            case LobbyStage.InMatch:
                return string.Empty;
            case LobbyStage.Rematch:
                text.Append("Match over\n\n");
                AppendReadiness(text, lobby, "wants a rematch", "Enter   rematch");
                break;
        }

        if (lobby.Stage is not (LobbyStage.Idle or LobbyStage.Starting))
        {
            text.Append("Esc   leave the lobby\n");
        }

        return AppendNotes(text, matchNote, lobby.Notice, lastAction).ToString();
    }

    private static void AppendInvites(StringBuilder text, LobbyFlow lobby)
    {
        text.Append("Lobby: waiting for an opponent\n\nI   invite a friend through the Steam overlay\n");
        for (int i = 0; i < lobby.InvitableFriends.Count && i < MaxListedFriends; i++)
        {
            text.Append($"{i + 1}   invite {lobby.InvitableFriends[i].Name} (running the game)\n");
        }

        text.Append("Or accept a friend's invite in Steam.\n\n");
    }

    private static void AppendReadiness(StringBuilder text, LobbyFlow lobby, string readyWord, string readyKey)
    {
        text.Append($"You: {(lobby.IsLocalReady ? readyWord : $"not yet — {readyKey}")}\n");
        text.Append($"{lobby.OpponentName}: {(lobby.IsOpponentReady ? readyWord : "not yet")}\n\n");
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
