using Godot;

namespace FightingGame.Input;

/// <summary>Reads devices through Godot's input singleton, as of the last event pump (see MatchLoop).</summary>
public sealed class GodotDeviceState : IDeviceState
{
    public bool IsKeyHeld(Key key) => Godot.Input.IsPhysicalKeyPressed(key);

    public bool IsJoyButtonHeld(int device, JoyButton button) => Godot.Input.IsJoyButtonPressed(device, button);

    public float JoyAxisValue(int device, JoyAxis axis) => Godot.Input.GetJoyAxis(device, axis);
}
