namespace FightingGame.UI;

/// <summary>Which menu is on screen (<see cref="MenuScreenChoice"/>).</summary>
public enum MenuScreen
{
    /// <summary>No menu: a match is being played.</summary>
    None,

    /// <summary>Versus, training, online, quit.</summary>
    MainMenu,

    /// <summary>The online lobby: host, join or invite, ready.</summary>
    Lobby,

    /// <summary>Over a finished match: rematch or back to the main menu.</summary>
    MatchOver,

    /// <summary>Opened during a match: resume or leave it.</summary>
    Pause,
}
