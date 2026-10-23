namespace FightingGame.UI.Framework;

/// <summary>The kind of device a menu press came from.</summary>
public enum MenuDeviceKind
{
    Keyboard,

    /// <summary>A joypad; leverless controllers that report as one count as one.</summary>
    Gamepad,

    /// <summary>Clicks and pointing; a bonus, menus never need it.</summary>
    Mouse,
}
