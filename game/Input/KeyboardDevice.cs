using Sim.Input;

namespace FightingGame.Input;

/// <summary>Reads the keyboard into a raw input word through a keyboard layout.</summary>
public sealed class KeyboardDevice : IInputDevice
{
    private readonly KeyBinding[] _keys;
    private readonly IDeviceState _devices;

    public KeyboardDevice(KeyboardBindings bindings, IDeviceState devices)
    {
        _keys = bindings.Keys;
        _devices = devices;
    }

    // Raw directions only: opposite keys held together stay in the word, SOCD is resolved in the logic (ADR-0002).
    public InputWord Read()
    {
        ushort bits = 0;
        foreach (KeyBinding binding in _keys)
        {
            if (_devices.IsKeyHeld(binding.Key))
            {
                bits |= binding.Mask;
            }
        }

        return new InputWord(bits);
    }
}
