using Sim.Input;

namespace FightingGame.Input;

/// <summary>
/// Reads any roster device for a local player, with that player's bindings: their keyboard keys for the keyboard,
/// their joypad bindings for a pad (ADR-0038).
/// </summary>
public sealed class InputDeviceReader
{
    private readonly KeyboardDevice[] _keyboardOfPlayer;
    private readonly GamepadDevice[] _gamepadOfPlayer;
    private readonly DeviceRoster _roster;

    /// <param name="bindings">One per player on this PC (player settings).</param>
    public InputDeviceReader(PlayerBindings[] bindings, IDeviceState devices, DeviceRoster roster)
    {
        _keyboardOfPlayer = new KeyboardDevice[bindings.Length];
        _gamepadOfPlayer = new GamepadDevice[bindings.Length];
        for (int player = 0; player < bindings.Length; player++)
        {
            _keyboardOfPlayer[player] = new KeyboardDevice(bindings[player].Keyboard, devices);
            _gamepadOfPlayer[player] = new GamepadDevice(bindings[player].Gamepad, devices);
        }

        _roster = roster;
    }

    /// <summary>The raw word <paramref name="device"/> gives <paramref name="player"/>; neutral while it is disconnected.</summary>
    public InputWord Read(InputDeviceId device, int player) => device.Kind switch
    {
        InputDeviceKind.Keyboard => _keyboardOfPlayer[player].Read(),
        _ => _roster.TryGetJoypad(device, out int joypad) ? _gamepadOfPlayer[player].Read(joypad) : InputWord.Neutral,
    };
}
