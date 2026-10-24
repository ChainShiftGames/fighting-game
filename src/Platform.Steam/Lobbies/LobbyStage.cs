namespace Platform.Steam.Lobbies;

/// <summary>Where the lobby is, from this player's side (see <see cref="LobbyFlow"/>).</summary>
public enum LobbyStage
{
    /// <summary>Not in a lobby.</summary>
    Idle,

    /// <summary>Creating a lobby, or joining a friend's.</summary>
    Entering,

    /// <summary>In a lobby, as host or guest: waiting for an opponent, or for both players to be ready.</summary>
    InLobby,

    /// <summary>Both players are ready in the lobby: each picks on character select (<see cref="LobbyFlow.Pick"/>).</summary>
    Picking,

    /// <summary>Both players are ready and picked: the game starts the match now (<see cref="LobbyFlow.TakeMatchToStart"/>).</summary>
    Starting,

    /// <summary>The match is being played.</summary>
    InMatch,

    /// <summary>The match is over: both players ready again starts a rematch with the same picks.</summary>
    Rematch,
}
