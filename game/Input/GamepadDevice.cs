using Godot;
using Sim.Input;

namespace FightingGame.Input;

/// <summary>
/// Reads the joypad in one <see cref="JoypadRoster"/> slot into a raw input word. Reads neutral while no joypad
/// fills that slot.
/// </summary>
public sealed class GamepadDevice : IInputDevice
{
    private readonly GamepadBindings _bindings;
    private readonly IDeviceState _devices;
    private readonly JoypadRoster _roster;
    private readonly int _slot;

    public GamepadDevice(GamepadBindings bindings, IDeviceState devices, JoypadRoster roster, int slot)
    {
        _bindings = bindings;
        _devices = devices;
        _roster = roster;
        _slot = slot;
    }

    // Raw directions only: a d-pad or leverless holding left+right sends both bits; the logic resolves SOCD.
    public InputWord Read()
    {
        if (!_roster.TryGetDevice(_slot, out int device))
        {
            return InputWord.Neutral;
        }

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
