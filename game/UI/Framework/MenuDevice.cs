namespace FightingGame.UI.Framework;

/// <summary>Which device a menu press came from, so screens such as side select can tell the players apart.</summary>
/// <param name="Index">For a gamepad, Godot's joypad index; 0 for the keyboard and mouse.</param>
public readonly record struct MenuDevice(MenuDeviceKind Kind, int Index)
{
    public static MenuDevice Keyboard { get; } = new(MenuDeviceKind.Keyboard, 0);

    public static MenuDevice Mouse { get; } = new(MenuDeviceKind.Mouse, 0);

    public static MenuDevice Gamepad(int index) => new(MenuDeviceKind.Gamepad, index);
}
