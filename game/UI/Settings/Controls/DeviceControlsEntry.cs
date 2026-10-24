using System;
using FightingGame.Input;
using FightingGame.Settings;
using FightingGame.UI.Framework;

namespace FightingGame.UI.Settings.Controls;

/// <summary>
/// One connected device in the Controls tab: opens its controls screens. Its layout is not in
/// <see cref="PlayerSettings"/> but in the controls file, so the tab's "Reset to defaults" resets it directly.
/// </summary>
public sealed class DeviceControlsEntry : ISettingEntry
{
    private readonly ControlsMenu _menu;
    private readonly InputDeviceInfo _device;
    private readonly bool _isOverMatch;

    public DeviceControlsEntry(ControlsMenu menu, InputDeviceInfo device, bool isOverMatch)
    {
        _menu = menu;
        _device = device;
        _isOverMatch = isOverMatch;
    }

    public bool NeedsKeeping => false;

    public MenuItem Item(PlayerSettings settings, Action<PlayerSettings> change) =>
        new ButtonItem(MenuText.Key("MENU_CONTROLS_DEVICE_TITLE", _device.Name), () => _menu.Open(_device, _isOverMatch))
        {
            Description = MenuText.Key(_device.Id.IsJoypad ? "MENU_CONTROLS_PAD_INFO" : "MENU_CONTROLS_KEYBOARD_INFO"),
        };

    /// <summary>Puts the device's layout back on its kind's default (saved at once); the player settings don't change.</summary>
    public PlayerSettings Reset(PlayerSettings settings, PlayerSettings defaults)
    {
        _menu.ResetToDefault(_device);
        return settings;
    }

    public bool IsSame(PlayerSettings first, PlayerSettings second) => true;
}
