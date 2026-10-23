namespace FightingGame.Input;

/// <summary>
/// Which input device: the keyboard, or one joypad by its model (Godot's GUID) and which of the connected pads of
/// that model it is. A joypad keeps its id while connected and gets it back on a quick unplug and replug (ADR-0038).
/// </summary>
public readonly record struct InputDeviceId(InputDeviceKind Kind, string Guid, int Ordinal)
{
    public static InputDeviceId Keyboard { get; } = new(InputDeviceKind.Keyboard, string.Empty, 0);

    public bool IsJoypad => Kind == InputDeviceKind.Joypad;

    public static InputDeviceId Joypad(string guid, int ordinal) => new(InputDeviceKind.Joypad, guid, ordinal);
}
