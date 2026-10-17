namespace Platform.Steam.Lobbies;

/// <summary>A match the lobby starts: against whom, which player this PC is, and which match of the pairing it is.</summary>
/// <param name="Number">1 for the first match against this opponent, 2 for the first rematch, and so on.</param>
public sealed record LobbyMatch(ulong OpponentSteamId, string OpponentName, bool LocalIsPlayer1, int Number);
