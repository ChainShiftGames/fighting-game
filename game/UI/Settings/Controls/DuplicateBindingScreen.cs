using System.Collections.Generic;
using FightingGame.Input.Profiles;
using FightingGame.UI.Framework;

namespace FightingGame.UI.Settings.Controls;

/// <summary>
/// The pressed control is already on other inputs of the same layer: the player picks whether it moves to the new
/// input only, stays on both (a key for two buttons at once is sometimes wanted), or nothing changes.
/// </summary>
public sealed class DuplicateBindingScreen : IMenuScreen
{
    private readonly ControlsMenu _menu;
    private readonly ControlCapture _capture;
    private readonly BoundControl _control;
    private readonly IReadOnlyList<string> _others;

    /// <param name="others">The inputs that use <paramref name="control"/> already.</param>
    public DuplicateBindingScreen(
        ControlsMenu menu, ControlCapture capture, BoundControl control, IReadOnlyList<string> others)
    {
        _menu = menu;
        _capture = capture;
        _control = control;
        _others = others;
    }

    public MenuText Title { get; } = MenuText.Key("MENU_CONTROLS_DUPLICATE_TITLE");

    public IReadOnlyList<MenuText> Body =>
    [
        MenuText.Key(
            "MENU_CONTROLS_DUPLICATE", InputNames.Of(_control), InputNames.Join(_others), InputNames.Of(_capture.Input)),
    ];

    public IReadOnlyList<MenuItem> Items =>
    [
        new ButtonItem(MenuText.Key("MENU_CONTROLS_DUPLICATE_MOVE", InputNames.Of(_capture.Input)), () => Choose(onlyHere: true)),
        new ButtonItem(MenuText.Key("MENU_CONTROLS_DUPLICATE_BOTH"), () => Choose(onlyHere: false)),
        new ButtonItem(MenuText.Key("MENU_CONTROLS_CANCEL"), () => _menu.Close(this)),
    ];

    public bool IsOverMatch => _menu.IsOverMatch;

    public bool Back()
    {
        _menu.Close(this);
        return false;
    }

    public void Press(MenuPress press)
    {
    }

    private void Choose(bool onlyHere)
    {
        _menu.Bind(_capture, _control, onlyHere);
        _menu.Close(this);
    }
}
