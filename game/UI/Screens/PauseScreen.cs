using System;
using System.Collections.Generic;
using FightingGame.Online;
using FightingGame.UI.Framework;

namespace FightingGame.UI.Screens;

/// <summary>
/// Opened during a match: resume or leave it. On this PC the match is held meanwhile; online it can't be (the other
/// player's game keeps running), so there it is only a menu to leave from.
/// </summary>
public sealed class PauseScreen : IMenuScreen
{
    private readonly MatchLauncher _launcher;
    private readonly Action _resume;
    private readonly Func<string> _note;

    /// <param name="resume">Closes the pause menu, back to the match.</param>
    /// <param name="note">What the match has to say, e.g. a connection warning; may be empty.</param>
    public PauseScreen(MatchLauncher launcher, Action resume, Func<string> note)
    {
        _launcher = launcher;
        _resume = resume;
        _note = note;
    }

    public MenuText Title => MenuText.Key(_launcher.IsLocal ? "MENU_TITLE_PAUSED" : "MENU_TITLE_MENU");

    public IReadOnlyList<MenuText> Body => [MenuText.Plain(_note())];

    public IReadOnlyList<MenuItem> Items =>
    [
        new ButtonItem(MenuText.Key("MENU_RESUME"), _resume),
        new ButtonItem(
            MenuText.Key(_launcher.IsLocal ? "MENU_MAIN_MENU" : "MENU_LEAVE_MATCH"), _launcher.ReturnToMainMenu),
    ];

    public bool IsOverMatch => true;

    public bool Back()
    {
        _resume();
        return false;
    }

    // Start closing it is the opener's job (GameMenus), like opening it.
    public void Press(MenuPress press)
    {
    }
}
