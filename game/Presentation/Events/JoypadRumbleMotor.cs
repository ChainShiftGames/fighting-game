using FightingGame.Input;

namespace FightingGame.Presentation.Events;

/// <summary>Rumbles the joypad a player reads on this PC, through Godot's joypad vibration.</summary>
public sealed class JoypadRumbleMotor : IRumbleMotor
{
    /// <summary>Value in the slot list for a player who has no joypad slot on this PC (e.g. the online opponent).</summary>
    public const int NoSlot = -1;

    private readonly JoypadRoster _joypads;
    private readonly int[] _slotOfPlayer;

    /// <param name="slotOfPlayer">
    /// Per player, the <see cref="JoypadRoster"/> slot their input reads, or <see cref="NoSlot"/>.
    /// </param>
    public JoypadRumbleMotor(JoypadRoster joypads, int[] slotOfPlayer)
    {
        _joypads = joypads;
        _slotOfPlayer = slotOfPlayer;
    }

    public void Start(int player, float weak, float strong, float seconds)
    {
        if (TryGetDevice(player, out int device))
        {
            Godot.Input.StartJoyVibration(device, weak, strong, seconds);
        }
    }

    public void Stop(int player)
    {
        if (TryGetDevice(player, out int device))
        {
            Godot.Input.StopJoyVibration(device);
        }
    }

    private bool TryGetDevice(int player, out int device)
    {
        device = -1;
        int slot = _slotOfPlayer[player];
        return slot != NoSlot && _joypads.TryGetDevice(slot, out device);
    }
}
