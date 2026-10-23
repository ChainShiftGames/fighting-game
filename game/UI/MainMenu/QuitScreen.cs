using System;
using System.Collections.Generic;
using FightingGame.UI.Flow;
using FightingGame.UI.Framework;

namespace FightingGame.UI.MainMenu;

/// <summary>Over the main menu: quit the game, or not. The cursor starts on cancel, so a mashed confirm stays.</summary>
public sealed class QuitScreen : IMenuScreen
{
    private readonly GameFlow _flow;
    private readonly Action _quit;

    public QuitScreen(GameFlow flow, Action quit)
    {
        _flow = flow;
        _quit = quit;
    }

    public MenuText Title { get; } = MenuText.Key("MENU_TITLE_QUIT");

    public IReadOnlyList<MenuText> Body { get; } = [MenuText.Key("MENU_QUIT_BODY")];

    public IReadOnlyList<MenuItem> Items =>
    [
        new ButtonItem(MenuText.Key("MENU_QUIT"), _quit),
        new ButtonItem(MenuText.Key("MENU_CANCEL"), _flow.Back),
    ];

    public int FirstFocus => 1;

    public bool IsOverMatch => false;

    public bool Back()
    {
        _flow.Back();
        return false;
    }

    public void Press(MenuPress press)
    {
    }
}
