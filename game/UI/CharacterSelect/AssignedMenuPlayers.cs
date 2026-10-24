using FightingGame.Input;
using FightingGame.UI.Framework;

namespace FightingGame.UI.CharacterSelect;

/// <summary>
/// The menu devices of each player in a <see cref="PlayerDevices"/> assignment (ADR-0038). Like the match, an
/// assignment that claims new joypads gives a pad nobody has to the first player without one, so a pad plugged in on
/// the screen can join.
/// </summary>
public sealed class AssignedMenuPlayers : IMenuPlayers
{
    private readonly PlayerDevices _assignment;
    private readonly DeviceRoster _roster;

    public AssignedMenuPlayers(PlayerDevices assignment, DeviceRoster roster)
    {
        _assignment = assignment;
        _roster = roster;
    }

    public int PlayerOf(MenuDevice device) => device.Kind switch
    {
        MenuDeviceKind.Keyboard => _assignment.PlayerOf(InputDeviceId.Keyboard),
        MenuDeviceKind.Gamepad when _roster.TryFindJoypad(device.Index, out InputDeviceInfo pad) => PlayerOf(pad.Id),
        _ => -1,
    };

    public bool CanPress(int player)
    {
        if (player >= _assignment.PlayerCount)
        {
            return false;
        }

        foreach (InputDeviceId device in _assignment.Of(player))
        {
            if (_roster.IsConnected(device))
            {
                return true;
            }
        }

        return false;
    }

    private int PlayerOf(InputDeviceId pad)
    {
        int player = _assignment.PlayerOf(pad);
        if (player >= 0 || !_assignment.ClaimsNewJoypads)
        {
            return player;
        }

        for (int candidate = 0; candidate < _assignment.PlayerCount; candidate++)
        {
            if (!HasConnectedJoypad(candidate))
            {
                _assignment.Assign(candidate, pad);
                return candidate;
            }
        }

        return -1;
    }

    private bool HasConnectedJoypad(int player)
    {
        foreach (InputDeviceId device in _assignment.Of(player))
        {
            if (device.IsJoypad && _roster.IsConnected(device))
            {
                return true;
            }
        }

        return false;
    }
}
