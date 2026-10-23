namespace FightingGame.Input;

/// <summary>One usable input device in the <see cref="DeviceRoster"/>.</summary>
/// <param name="Name">What players see: "Keyboard (left)", or the joypad's name.</param>
/// <param name="Joypad">Godot's joypad index while connected; <see cref="NoJoypad"/> for a keyboard half.</param>
public readonly record struct InputDeviceInfo(InputDeviceId Id, string Name, int Joypad)
{
    public const int NoJoypad = -1;
}
