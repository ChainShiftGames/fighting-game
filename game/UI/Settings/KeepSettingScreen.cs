using System;
using System.Collections.Generic;
using System.Globalization;
using FightingGame.UI.Framework;

namespace FightingGame.UI.Settings;

/// <summary>"Keep this?" after a video change, counting down; back or no answer in time reverts it.</summary>
public sealed class KeepSettingScreen : IMenuScreen
{
    private readonly Func<int> _secondsLeft;
    private readonly Action _keep;
    private readonly Action _revert;

    public KeepSettingScreen(Func<int> secondsLeft, Action keep, Action revert, bool isOverMatch)
    {
        _secondsLeft = secondsLeft;
        _keep = keep;
        _revert = revert;
        IsOverMatch = isOverMatch;
    }

    public MenuText Title { get; } = MenuText.Key("MENU_SETTINGS_KEEP_TITLE");

    public IReadOnlyList<MenuText> Body =>
        [MenuText.Key("MENU_SETTINGS_KEEP_COUNTDOWN", _secondsLeft().ToString(CultureInfo.InvariantCulture))];

    public IReadOnlyList<MenuItem> Items =>
    [
        new ButtonItem(MenuText.Key("MENU_SETTINGS_KEEP"), _keep),
        new ButtonItem(MenuText.Key("MENU_SETTINGS_REVERT"), _revert),
    ];

    public bool IsOverMatch { get; }

    public bool Back()
    {
        _revert();
        return false;
    }

    public void Press(MenuPress press)
    {
    }
}
