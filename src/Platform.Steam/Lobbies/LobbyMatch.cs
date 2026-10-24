namespace Platform.Steam.Lobbies;

/// <summary>A match the lobby starts: against whom, which player this PC is, and which match of the pairing it is.</summary>
/// <param name="Number">1 for the first match against this opponent, 2 for the first rematch, and so on.</param>
public sealed record LobbyMatch(ulong OpponentSteamId, string OpponentName, bool LocalIsPlayer1, int Number)
{
    /// <summary>This player's pick, as the game wrote it (ADR-0042).</summary>
    public string LocalPick { get; init; } = string.Empty;

    /// <summary>The opponent's pick, as their game wrote it.</summary>
    public string OpponentPick { get; init; } = string.Empty;
}
