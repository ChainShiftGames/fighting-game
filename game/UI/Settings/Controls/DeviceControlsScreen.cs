using System;
using System.Collections.Generic;
using FightingGame.Input;
using FightingGame.Input.Profiles;
using FightingGame.UI.Framework;

namespace FightingGame.UI.Settings.Controls;

/// <summary>
/// One device's controls: its directions, buttons, menu buttons, stick options (pads), presets and reset, under a
/// test area that shows what it presses now. Back leaves, as in every menu.
/// </summary>
public sealed class DeviceControlsScreen : IMenuScreen
{
    private readonly ControlsMenu _menu;

    public DeviceControlsScreen(ControlsMenu menu)
    {
        _menu = menu;
    }

    public MenuText Title => MenuText.Key("MENU_CONTROLS_DEVICE_TITLE", _menu.Device.Name);

    public IReadOnlyList<MenuText> Body => [MenuText.Key("MENU_CONTROLS_FALLBACK_HINT"), MenuText.Empty, .. _menu.TestLines()];

    public IReadOnlyList<MenuItem> Items
    {
        get
        {
            var items = new List<MenuItem>
            {
                Open("MENU_CONTROLS_DIRECTIONS", () => List(BindingLayer.Game, _menu.Directions, "MENU_CONTROLS_DIRECTIONS_TITLE")),
                Open("MENU_CONTROLS_BUTTONS", () => List(BindingLayer.Game, _menu.Buttons, "MENU_CONTROLS_BUTTONS_TITLE")),
                Open("MENU_CONTROLS_MENU_BUTTONS", () => List(BindingLayer.Menu, MenuInputs.All, "MENU_CONTROLS_MENU_BUTTONS_TITLE")),
            };
            if (_menu.Device.Id.Kind == InputDeviceKind.Joypad)
            {
                items.Add(Open("MENU_CONTROLS_STICK", () => new StickOptionsScreen(_menu)));
            }

            items.Add(Open("MENU_CONTROLS_PRESETS", () => new PresetScreen(_menu)));
            items.Add(new ButtonItem(MenuText.Key("MENU_CONTROLS_RESET"), AskToReset));
            return items;
        }
    }

    public bool IsOverMatch => _menu.IsOverMatch;

    public bool Back()
    {
        _menu.Close(this);
        return false;
    }

    public void Press(MenuPress press)
    {
    }

    // No descriptions: menus don't scroll, and a pad's six entries must fit a 720p screen under the test area.
    private ButtonItem Open(string labelKey, Func<IMenuScreen> screen) =>
        new(MenuText.Key(labelKey), () => _menu.Push(screen()));

    private BindingListScreen List(BindingLayer layer, IReadOnlyList<string> inputs, string titleKey) =>
        new(_menu, layer, inputs, titleKey);

    // The settings menu's reset question, about this one device; the cursor starts on cancel.
    private void AskToReset()
    {
        ResetTabScreen? question = null;
        question = new ResetTabScreen(
            MenuText.Plain(_menu.Device.Name),
            () =>
            {
                _menu.ResetToDefault(_menu.Device);
                _menu.Close(question!);
            },
            () => _menu.Close(question!),
            IsOverMatch);
        _menu.Push(question);
    }
}
