namespace FightingGame.Input;

/// <summary>
/// Which input device: one keyboard half, or one joypad by its model (Godot's GUID) and which of the connected pads of
/// that model it is. A joypad keeps its id while connected and gets it back on a quick unplug and replug (ADR-0038).
/// </summary>
public readonly record struct InputDeviceId(InputDeviceKind Kind, string Guid, int Ordinal)
{
    public static InputDeviceId KeyboardLeft { get; } = new(InputDeviceKind.KeyboardLeft, string.Empty, 0);

    public static InputDeviceId KeyboardRight { get; } = new(InputDeviceKind.KeyboardRight, string.Empty, 0);

    public bool IsJoypad => Kind == InputDeviceKind.Joypad;

    public static InputDeviceId Joypad(string guid, int ordinal) => new(InputDeviceKind.Joypad, guid, ordinal);
}
