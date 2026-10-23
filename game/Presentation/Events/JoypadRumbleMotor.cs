using System.Collections.Generic;
using FightingGame.Input;

namespace FightingGame.Presentation.Events;

/// <summary>Rumbles the joypad a player reads on this PC, through Godot's joypad vibration.</summary>
public sealed class JoypadRumbleMotor : IRumbleMotor
{
    /// <summary>Value in the local player list for a player who doesn't play on this PC (e.g. the online opponent).</summary>
    public const int NoLocalPlayer = -1;

    private readonly DeviceRoster _roster;
    private readonly PlayerDevices _assignment;
    private readonly int[] _localPlayerOf;

    /// <param name="assignment">The devices each local player reads; the first connected joypad among them rumbles.</param>
    /// <param name="localPlayerOf">
    /// Per player of the match, their place in <paramref name="assignment"/>, or <see cref="NoLocalPlayer"/>.
    /// </param>
    public JoypadRumbleMotor(DeviceRoster roster, PlayerDevices assignment, int[] localPlayerOf)
    {
        _roster = roster;
        _assignment = assignment;
        _localPlayerOf = localPlayerOf;
    }

    /// <summary>A motor for a match nobody on this PC plays (replays): nothing rumbles.</summary>
    public static JoypadRumbleMotor None(DeviceRoster roster) =>
        new(roster, new PlayerDevices(0), [NoLocalPlayer, NoLocalPlayer]);

    public void Start(int player, float weak, float strong, float seconds)
    {
        if (TryGetJoypad(player, out int joypad))
        {
            Godot.Input.StartJoyVibration(joypad, weak, strong, seconds);
        }
    }

    public void Stop(int player)
    {
        if (TryGetJoypad(player, out int joypad))
        {
            Godot.Input.StopJoyVibration(joypad);
        }
    }

    private bool TryGetJoypad(int player, out int joypad)
    {
        joypad = InputDeviceInfo.NoJoypad;
        int local = _localPlayerOf[player];
        if (local == NoLocalPlayer)
        {
            return false;
        }

        IReadOnlyList<InputDeviceId> devices = _assignment.Of(local);
        for (int index = 0; index < devices.Count; index++)
        {
            if (_roster.TryGetJoypad(devices[index], out joypad))
            {
                return true;
            }
        }

        return false;
    }
}
