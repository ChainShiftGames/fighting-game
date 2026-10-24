using System;
using FightingGame.Input;

namespace FightingGame.UI.SideSelect;

/// <summary>Picks a device's generic icon from what the roster knows: the keyboard, or a joypad by its name.</summary>
/// <remarks>Godot reports sticks and leverless controllers as joypads; only their names tell them apart.</remarks>
public static class DeviceIcons
{
    private static readonly string[] _stickWords =
        ["stick", "hitbox", "hit box", "arcade", "leverless", "snack box", "mixbox", "fight box"];

    public static DeviceIcon Of(InputDeviceInfo device)
    {
        if (!device.Id.IsJoypad)
        {
            return DeviceIcon.Keyboard;
        }

        foreach (string word in _stickWords)
        {
            if (device.Name.Contains(word, StringComparison.OrdinalIgnoreCase))
            {
                return DeviceIcon.ArcadeStick;
            }
        }

        return DeviceIcon.Gamepad;
    }
}
