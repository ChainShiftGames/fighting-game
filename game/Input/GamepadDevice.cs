using Godot;
using Sim.Input;

namespace FightingGame.Input;

/// <summary>Reads a joypad into a raw input word through a joypad layout.</summary>
public sealed class GamepadDevice
{
    private readonly GamepadBindings _bindings;
    private readonly IDeviceState _devices;

    public GamepadDevice(GamepadBindings bindings, IDeviceState devices)
    {
        _bindings = bindings;
        _devices = devices;
    }

    // Raw directions only: a d-pad or leverless holding left+right sends both bits; the logic resolves SOCD.
    /// <param name="device">Godot's joypad index.</param>
    public InputWord Read(int device)
    {
        ushort bits = 0;
        foreach (PadButtonBinding binding in _bindings.Buttons)
        {
            if (_devices.IsJoyButtonHeld(device, binding.Button))
            {
                bits |= binding.Mask;
            }
        }

        foreach (PadTriggerBinding binding in _bindings.Triggers)
        {
            if (_devices.JoyAxisValue(device, binding.Trigger) > _bindings.TriggerThreshold)
            {
                bits |= binding.Mask;
            }
        }

        return new InputWord((ushort)(bits | StickDirections(device)));
    }

    private ushort StickDirections(int device)
    {
        if (!_bindings.LeftStick)
        {
            return 0;
        }

        float stickX = _devices.JoyAxisValue(device, JoyAxis.LeftX);

        // Godot's stick Y axis points down.
        float stickY = _devices.JoyAxisValue(device, JoyAxis.LeftY);
        float deadzone = _bindings.StickDeadzone;
        ushort bits = 0;
        bits |= stickY < -deadzone ? InputWord.Up : (ushort)0;
        bits |= stickY > deadzone ? InputWord.Down : (ushort)0;
        bits |= stickX < -deadzone ? InputWord.Left : (ushort)0;
        bits |= stickX > deadzone ? InputWord.Right : (ushort)0;
        return bits;
    }
}
