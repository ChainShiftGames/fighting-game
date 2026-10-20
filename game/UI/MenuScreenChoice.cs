using FightingGame.Online;

namespace FightingGame.UI;

/// <summary>Picks the menu to show from what the game is doing.</summary>
public static class MenuScreenChoice
{
    /// <param name="hasMatch">A match exists (playing, or finished under the menu).</param>
    /// <param name="isMatchOver">That match is finished or stopped for good.</param>
    /// <param name="isPaused">The player opened the pause menu during it.</param>
    public static MenuScreen For(bool hasMatch, bool isMatchOver, LaunchMode mode, bool isPaused)
    {
        if (!hasMatch)
        {
            return mode == LaunchMode.Online ? MenuScreen.Lobby : MenuScreen.MainMenu;
        }

        if (isMatchOver)
        {
            return MenuScreen.MatchOver;
        }

        return isPaused ? MenuScreen.Pause : MenuScreen.None;
    }
}
