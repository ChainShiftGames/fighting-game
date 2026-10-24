using System;
using System.Collections.Generic;
using FightingGame.UI.Framework;

namespace FightingGame.UI.Settings;

/// <summary>Asks before a tab's settings go back to their defaults; the cursor starts on "cancel".</summary>
public sealed class ResetTabScreen : IMenuScreen
{
    private readonly MenuText _tabName;
    private readonly Action _reset;
    private readonly Action _cancel;

    public ResetTabScreen(MenuText tabName, Action reset, Action cancel, bool isOverMatch)
    {
        _tabName = tabName;
        _reset = reset;
        _cancel = cancel;
        IsOverMatch = isOverMatch;
    }

    public MenuText Title { get; } = MenuText.Key("MENU_SETTINGS_RESET_TITLE");

    public IReadOnlyList<MenuText> Body => [MenuText.Key("MENU_SETTINGS_RESET_QUESTION", _tabName)];

    public IReadOnlyList<MenuItem> Items =>
    [
        new ButtonItem(MenuText.Key("MENU_SETTINGS_RESET_CONFIRM"), _reset),
        new ButtonItem(MenuText.Key("MENU_SETTINGS_RESET_CANCEL"), _cancel),
    ];

    public int FirstFocus => 1;

    public bool IsOverMatch { get; }

    public bool Back()
    {
        _cancel();
        return false;
    }

    public void Press(MenuPress press)
    {
    }
}
