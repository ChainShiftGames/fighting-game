using Godot;
using Sim.Input;

namespace FightingGame.Input;

/// <summary>Reads one gamepad's d-pad and left stick. Reads neutral while that gamepad is unplugged.</summary>
public sealed class GamepadDevice : IInputDevice
{
    private readonly int _deviceIndex;
    private readonly float _stickDeadzone;

    /// <param name="deviceIndex">Godot's joypad index.</param>
    /// <param name="stickDeadzone">Stick travel (0–1) before it counts as a direction (player setting).</param>
    public GamepadDevice(int deviceIndex, float stickDeadzone)
    {
        _deviceIndex = deviceIndex;
        _stickDeadzone = stickDeadzone;
    }

    public InputWord Read()
    {
        float stickX = Godot.Input.GetJoyAxis(_deviceIndex, JoyAxis.LeftX);

        // Godot's stick Y axis points down.
        float stickY = Godot.Input.GetJoyAxis(_deviceIndex, JoyAxis.LeftY);

        ushort bits = 0;
        bits |= Direction(JoyButton.DpadUp, stickY < -_stickDeadzone, InputWord.Up);
        bits |= Direction(JoyButton.DpadDown, stickY > _stickDeadzone, InputWord.Down);
        bits |= Direction(JoyButton.DpadLeft, stickX < -_stickDeadzone, InputWord.Left);
        bits |= Direction(JoyButton.DpadRight, stickX > _stickDeadzone, InputWord.Right);
        return new InputWord(bits);
    }

    private ushort Direction(JoyButton dpadButton, bool stickHeld, ushort bit) =>
        stickHeld || Godot.Input.IsJoyButtonPressed(_deviceIndex, dpadButton) ? bit : (ushort)0;
}
