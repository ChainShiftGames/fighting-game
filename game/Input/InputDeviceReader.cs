using Sim.Input;

namespace FightingGame.Input;

/// <summary>
/// Reads any roster device for a local player. A keyboard half always reads its own layout, whoever plays on it
/// (left: the first player's keyboard bindings, right: the second's); a joypad reads with the reading player's joypad
/// bindings (ADR-0038).
/// </summary>
public sealed class InputDeviceReader
{
    private readonly KeyboardDevice _keyboardLeft;
    private readonly KeyboardDevice _keyboardRight;
    private readonly GamepadDevice[] _gamepadOfPlayer;
    private readonly DeviceRoster _roster;

    /// <param name="bindings">One per player on this PC (player settings); the first two give the keyboard halves.</param>
    public InputDeviceReader(PlayerBindings[] bindings, IDeviceState devices, DeviceRoster roster)
    {
        _keyboardLeft = new KeyboardDevice(bindings[0].Keyboard, devices);
        _keyboardRight = new KeyboardDevice(bindings[1].Keyboard, devices);
        _gamepadOfPlayer = new GamepadDevice[bindings.Length];
        for (int player = 0; player < bindings.Length; player++)
        {
            _gamepadOfPlayer[player] = new GamepadDevice(bindings[player].Gamepad, devices);
        }

        _roster = roster;
    }

    /// <summary>The raw word <paramref name="device"/> gives <paramref name="player"/>; neutral while it is disconnected.</summary>
    public InputWord Read(InputDeviceId device, int player) => device.Kind switch
    {
        InputDeviceKind.KeyboardLeft => _keyboardLeft.Read(),
        InputDeviceKind.KeyboardRight => _keyboardRight.Read(),
        _ => _roster.TryGetJoypad(device, out int joypad) ? _gamepadOfPlayer[player].Read(joypad) : InputWord.Neutral,
    };
}
