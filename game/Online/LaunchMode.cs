namespace FightingGame.Online;

/// <summary>What the game is doing: at the main menu, or which kind of match it plays (<see cref="MatchLauncher"/>).</summary>
public enum LaunchMode
{
    /// <summary>No match: the menus are up.</summary>
    MainMenu,

    /// <summary>Two players on this PC.</summary>
    Versus,

    /// <summary>Player 1 against the training dummy.</summary>
    Training,

    /// <summary>In the online lobby, or playing a match it started.</summary>
    Online,

    /// <summary>An online match over direct UDP, from the command line (development only, ADR-0014).</summary>
    DirectOnline,

    /// <summary>Playing a replay file back, from the command line.</summary>
    Replay,
}
