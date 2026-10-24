using System.Collections.Generic;
using System.Globalization;
using FightingGame.UI.Framework;

namespace FightingGame.UI.Settings.Controls;

/// <summary>
/// "Press a control": waits for the edited device's next press (<see cref="ControlsMenu.Offer"/>), counting down.
/// Another device's back or the cancel item stops it, and so does running out of time.
/// </summary>
public sealed class CaptureScreen : IMenuScreen
{
    private readonly ControlsMenu _menu;

    public CaptureScreen(ControlsMenu menu)
    {
        _menu = menu;
    }

    public MenuText Title { get; } = MenuText.Key("MENU_CONTROLS_CAPTURE_TITLE");

    public IReadOnlyList<MenuText> Body => _menu.Capture is { } capture
        ?
        [
            MenuText.Key("MENU_CONTROLS_CAPTURE", InputNames.Of(capture.Input), MenuText.Plain(_menu.Device.Name)),
            MenuText.Key("MENU_CONTROLS_CAPTURE_TIME", _menu.CaptureSecondsLeft().ToString(CultureInfo.InvariantCulture)),
        ]
        : [];

    public IReadOnlyList<MenuItem> Items => [new ButtonItem(MenuText.Key("MENU_CONTROLS_CANCEL"), _menu.CancelCapture)];

    public bool IsOverMatch => _menu.IsOverMatch;

    public bool Back()
    {
        _menu.CancelCapture();
        return false;
    }

    public void Press(MenuPress press)
    {
    }
}
