namespace FightingGame.Input;

/// <summary>
/// Keeps an assignment playable as joypads come and go: knows which player waits for a disconnected controller,
/// lets a free controller take its place, and hands new joypads out under the default assignment (ADR-0038).
/// </summary>
public sealed class ControllerReconnect
{
    public const int NobodyWaiting = -1;

    private readonly PlayerDevices _assignment;
    private readonly DeviceRoster _roster;

    public ControllerReconnect(PlayerDevices assignment, DeviceRoster roster)
    {
        _assignment = assignment;
        _roster = roster;
    }

    /// <summary>The first player with an assigned joypad that isn't connected, or <see cref="NobodyWaiting"/>.</summary>
    public int WaitingPlayer
    {
        get
        {
            for (int player = 0; player < _assignment.PlayerCount; player++)
            {
                if (TryGetMissing(player, out _))
                {
                    return player;
                }
            }

            return NobodyWaiting;
        }
    }

    /// <summary>
    /// For <see cref="DeviceRoster.Connected"/>. A pad that comes back is already its player's. Under the default
    /// assignment a new one goes to the waiting player, else to the first player without a joypad.
    /// </summary>
    public void OnConnected(InputDeviceInfo device)
    {
        if (!_assignment.ClaimsNewJoypads || !device.Id.IsJoypad || _assignment.PlayerOf(device.Id) >= 0)
        {
            return;
        }

        if (!TakeOver(device.Id))
        {
            ClaimForPlayerWithoutJoypad(device.Id);
        }
    }

    /// <summary>
    /// Start pressed on Godot's joypad <paramref name="joypad"/>: when it is free, it replaces the waiting player's
    /// missing controller. False when nobody waits or the pad is someone else's.
    /// </summary>
    public bool TakeOver(int joypad) => _roster.TryFindJoypad(joypad, out InputDeviceInfo device) && TakeOver(device.Id);

    private bool TakeOver(InputDeviceId device)
    {
        int player = WaitingPlayer;
        if (player == NobodyWaiting || _assignment.PlayerOf(device) >= 0 ||
            !TryGetMissing(player, out InputDeviceId missing))
        {
            return false;
        }

        _assignment.Replace(player, missing, device);
        return true;
    }

    private void ClaimForPlayerWithoutJoypad(InputDeviceId device)
    {
        for (int player = 0; player < _assignment.PlayerCount; player++)
        {
            if (!_assignment.HasJoypad(player))
            {
                _assignment.Assign(player, device);
                return;
            }
        }
    }

    private bool TryGetMissing(int player, out InputDeviceId missing)
    {
        foreach (InputDeviceId device in _assignment.Of(player))
        {
            if (device.IsJoypad && !_roster.IsConnected(device))
            {
                missing = device;
                return true;
            }
        }

        missing = default;
        return false;
    }
}
