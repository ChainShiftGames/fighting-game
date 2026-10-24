namespace Platform.Steam.Lobbies;

/// <summary>What went wrong or changed on its own in the lobby, for the online screens to say (<see cref="LobbyNotice"/>).</summary>
public enum LobbyNoticeKind
{
    None,

    /// <summary>Steam couldn't create the lobby; the detail is Steam's result.</summary>
    CreateFailed,

    /// <summary>Steam couldn't join the lobby; the detail is Steam's result.</summary>
    JoinFailed,

    /// <summary>The opponent left the lobby; the detail is their name.</summary>
    OpponentLeft,

    /// <summary>The opponent's PC stopped answering during or after a match; the detail is their name.</summary>
    ConnectionLost,
}
