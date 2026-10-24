using System;
using System.Collections.Generic;
using FightingGame.Input.Profiles;
using FightingGame.UI.Framework;

namespace FightingGame.UI.Settings.Controls;

/// <summary>A pad's analog options: left stick on or off, its deadzone and the trigger threshold, in percent.</summary>
public sealed class StickOptionsScreen : IMenuScreen
{
    // Player setting limits, in percent: a 100 % deadzone or threshold would never count.
    private const int MaxPercent = 95;
    private const int MinThresholdPercent = 5;
    private const int PercentStep = 5;

    private readonly ControlsMenu _menu;

    public StickOptionsScreen(ControlsMenu menu)
    {
        _menu = menu;
    }

    public MenuText Title { get; } = MenuText.Key("MENU_CONTROLS_STICK_TITLE");

    public IReadOnlyList<MenuText> Body => [MenuText.Key("MENU_CONTROLS_DEVICE_TITLE", _menu.Device.Name), .. _menu.TestLines()];

    public IReadOnlyList<MenuItem> Items
    {
        get
        {
            StickOptions stick = _menu.Profile.Stick;
            return
            [
                new ToggleItem(MenuText.Key("MENU_CONTROLS_LEFT_STICK"), stick.LeftStick, on => Change(stick with { LeftStick = on }))
                {
                    Description = MenuText.Key("MENU_CONTROLS_LEFT_STICK_INFO"),
                },
                new SliderItem(
                    MenuText.Key("MENU_CONTROLS_DEADZONE"),
                    Percent(stick.StickDeadzone),
                    0,
                    MaxPercent,
                    PercentStep,
                    percent => Change(stick with { StickDeadzone = percent / 100f }),
                    stick.LeftStick)
                {
                    Description = MenuText.Key("MENU_CONTROLS_DEADZONE_INFO"),
                },
                new SliderItem(
                    MenuText.Key("MENU_CONTROLS_TRIGGER_THRESHOLD"),
                    Percent(stick.TriggerThreshold),
                    MinThresholdPercent,
                    MaxPercent,
                    PercentStep,
                    percent => Change(stick with { TriggerThreshold = percent / 100f }))
                {
                    Description = MenuText.Key("MENU_CONTROLS_TRIGGER_THRESHOLD_INFO"),
                },
            ];
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

    private static int Percent(float fraction) => (int)Math.Round(fraction * 100f);

    private void Change(StickOptions stick) => _menu.Change(_menu.Profile.WithStick(stick));
}
