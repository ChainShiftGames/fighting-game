namespace Platform.Steam.Lobbies;

/// <summary>Whether this PC is in a lobby on the platform, on its way into one, or in none.</summary>
public enum LobbyPresence
{
    Outside,
    Entering,
    Inside,
}
