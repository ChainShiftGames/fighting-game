using System;
using System.Collections.Generic;
using FightingGame.Online;
using FightingGame.UI.Flow;
using FightingGame.UI.Framework;

namespace FightingGame.UI.Screens;

/// <summary>
/// Over a finished match on this PC (or a replay): play it again, back to character select (once it exists), or to the
/// main menu.
/// </summary>
public sealed class MatchOverScreen : IMenuScreen
{
    private readonly GameFlow _flow;
    private readonly MatchLauncher _launcher;
    private readonly Func<string> _note;

    /// <param name="note">What the finished match says, e.g. a desync; may be empty.</param>
    public MatchOverScreen(GameFlow flow, MatchLauncher launcher, Func<string> note)
    {
        _flow = flow;
        _launcher = launcher;
        _note = note;
    }

    public MenuText Title { get; } = MenuText.Key("MENU_TITLE_MATCH_OVER");

    public IReadOnlyList<MenuText> Body => [MenuText.Plain(_note())];

    public IReadOnlyList<MenuItem> Items
    {
        get
        {
            var items = new List<MenuItem>();
            if (_launcher.CanRematch)
            {
                string again = _launcher.Mode == LaunchMode.Replay ? "MENU_WATCH_AGAIN" : "MENU_REMATCH";
                items.Add(new ButtonItem(MenuText.Key(again), _flow.Rematch));
            }

            if (_flow.CanReselect)
            {
                items.Add(new ButtonItem(MenuText.Key("MENU_CHARACTER_SELECT"), _flow.Reselect));
            }

            items.Add(new ButtonItem(MenuText.Key("MENU_MAIN_MENU"), _flow.ToMainMenu));
            return items;
        }
    }

    public bool IsOverMatch => true;

    // Its buttons say where to go; back doesn't guess.
    public bool Back() => false;

    public void Press(MenuPress press)
    {
    }
}
