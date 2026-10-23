using System;
using System.Collections.Generic;
using FightingGame.Online;
using FightingGame.UI.Framework;

namespace FightingGame.UI.Screens;

/// <summary>The main menu: versus, training, online (when the platform has it) and quit.</summary>
public sealed class MainMenuScreen : IMenuScreen
{
    private readonly MatchLauncher _launcher;
    private readonly Action _quit;
    private readonly Func<string> _note;

    /// <param name="note">What a match that failed to start has to say; may be empty.</param>
    public MainMenuScreen(MatchLauncher launcher, Action quit, Func<string> note)
    {
        _launcher = launcher;
        _quit = quit;
        _note = note;
    }

    public MenuText Title { get; } = MenuText.Key("MENU_TITLE_MAIN");

    public IReadOnlyList<MenuText> Body => [MenuText.Plain(_note())];

    public IReadOnlyList<MenuItem> Items
    {
        get
        {
            bool hasOnline = _launcher.Matchmaking is not null;
            MenuText online = hasOnline
                ? MenuText.Key("MENU_ONLINE")
                : MenuText.Key("MENU_ONLINE_NEEDS_PLATFORM", _launcher.PlatformName);
            return
            [
                new ButtonItem(MenuText.Key("MENU_VERSUS"), _launcher.StartVersus),
                new ButtonItem(MenuText.Key("MENU_TRAINING"), _launcher.StartTraining),
                new ButtonItem(online, () => _launcher.OpenOnline(), hasOnline),
                new ButtonItem(MenuText.Key("MENU_QUIT"), _quit),
            ];
        }
    }

    public bool IsOverMatch => false;

    // The bottom screen: there is nothing to go back to.
    public bool Back() => false;

    public void Press(MenuPress press)
    {
    }
}
