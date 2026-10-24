namespace FightingGame.UI.Results;

/// <summary>One choice on the results screen (<see cref="ResultsEntries"/> says which show for a match).</summary>
public enum ResultsEntry
{
    /// <summary>The same setup again; in versus both players must want it, online the other PC too.</summary>
    Rematch,

    /// <summary>The replay just watched, from the start.</summary>
    WatchAgain,

    /// <summary>New picks with the same devices; the session score stays.</summary>
    CharacterSelect,

    /// <summary>Out of the lobby, back to the online screens.</summary>
    LeaveLobby,

    /// <summary>Ends the session: the score starts again from 0 – 0.</summary>
    MainMenu,
}
