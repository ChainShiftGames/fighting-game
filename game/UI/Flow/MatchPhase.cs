namespace FightingGame.UI.Flow;

/// <summary>Whether a match is on screen, and whether it is finished (<see cref="GameFlow.Follow"/>).</summary>
public enum MatchPhase
{
    /// <summary>No match.</summary>
    None,

    Playing,

    /// <summary>Finished or stopped for good, still on screen under a menu.</summary>
    Over,
}
