namespace FightingGame.Settings;

/// <summary>How the game's window fills the screen (a player setting).</summary>
public enum ScreenMode
{
    Windowed,

    /// <summary>A borderless window the size of the screen: switching away is instant.</summary>
    Borderless,

    /// <summary>Exclusive fullscreen: the lowest latency on Windows, slower to switch away from.</summary>
    Fullscreen,
}
