using System;
using System.Collections.Generic;
using FightingGame.UI.Flow;
using FightingGame.UI.Framework;

namespace FightingGame.UI.Title;

/// <summary>
/// The first screen: the game's logo (a placeholder) and "Press any button". The press goes on to the main menu and
/// tells the menus which device the player is on.
/// </summary>
public sealed class TitleScreen : IMenuScreen
{
    private readonly GameFlow _flow;
    private readonly Func<string> _note;

    /// <param name="note">What a match that failed to start has to say; may be empty.</param>
    public TitleScreen(GameFlow flow, Func<string> note)
    {
        _flow = flow;
        _note = note;
    }

    public MenuText Title { get; } = MenuText.Key("MENU_LOGO");

    public IReadOnlyList<MenuText> Body => [MenuText.Key("MENU_PRESS_ANY_BUTTON"), MenuText.Plain(_note())];

    public IReadOnlyList<MenuItem> Items { get; } = [];

    public bool IsOverMatch => false;

    /// <summary>Any press but a held direction repeating, back included (see <see cref="Back"/>).</summary>
    public void Press(MenuPress press)
    {
        if (!press.IsRepeat)
        {
            _flow.PressOnTitle(press.Device);
        }
    }

    // The menus hand every press to Press, with its device; back never gets here.
    public bool Back() => false;
}
