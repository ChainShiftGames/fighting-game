using System.Collections.Generic;
using FightingGame.Input.Profiles;
using Sim.Input;

namespace FightingGame.Input;

/// <summary>
/// Reads any roster device with that device's own layout (ADR-0047), whichever player it is on: the layout follows
/// the device to either side. <see cref="Resolve"/> compiles the layouts when a match starts, so a frame only looks up
/// what is already built.
/// </summary>
/// <remarks>
/// Layouts changed in the options menu during a match count from the next match. A pad first seen during a match is
/// compiled once, on its first frame.
/// </remarks>
public sealed class InputDeviceReader
{
    private readonly ControlProfiles _profiles;
    private readonly ProfileCompiler _compiler;
    private readonly IDeviceState _devices;
    private readonly DeviceRoster _roster;
    private readonly Dictionary<InputDeviceId, GamepadDevice> _joypads = [];
    private KeyboardDevice _keyboard;

    public InputDeviceReader(ControlProfiles profiles, ProfileCompiler compiler, IDeviceState devices, DeviceRoster roster)
    {
        _profiles = profiles;
        _compiler = compiler;
        _devices = devices;
        _roster = roster;
        _keyboard = Keyboard();
    }

    /// <summary>Compiles every connected device's layout as it is now; call when a match starts.</summary>
    public void Resolve()
    {
        _keyboard = Keyboard();
        _joypads.Clear();
        foreach (InputDeviceInfo device in _roster.Devices)
        {
            if (device.Id.IsJoypad)
            {
                _joypads[device.Id] = Joypad(device.Id);
            }
        }
    }

    /// <summary>The raw word <paramref name="device"/> gives; neutral while it is disconnected.</summary>
    public InputWord Read(InputDeviceId device)
    {
        if (!device.IsJoypad)
        {
            return _keyboard.Read();
        }

        if (!_roster.TryGetJoypad(device, out int joypad))
        {
            return InputWord.Neutral;
        }

        if (!_joypads.TryGetValue(device, out GamepadDevice? gamepad))
        {
            gamepad = _joypads[device] = Joypad(device);
        }

        return gamepad.Read(joypad);
    }

    private KeyboardDevice Keyboard() =>
        new(_compiler.Keyboard(_profiles.For(InputDeviceId.Keyboard)), _devices);

    private GamepadDevice Joypad(InputDeviceId device) => new(_compiler.Gamepad(_profiles.For(device)), _devices);
}
