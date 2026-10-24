namespace FightingGame.Input;

/// <summary>
/// The assignments matches start with until players choose (side select, UI-03): player 1 reads the keyboard, player
/// n reads the n-th joypad, and a joypad plugged in later goes to a player without one (ADR-0038).
/// </summary>
public static class DefaultPlayerDevices
{
    /// <summary>Local versus: player 1 on the keyboard and the first pad, player 2 on the second pad.</summary>
    public static PlayerDevices Versus(DeviceRoster roster) => Create(roster, playerCount: 2);

    /// <summary>Training: the one player on the keyboard and the first pad.</summary>
    public static PlayerDevices Training(DeviceRoster roster) => Create(roster, playerCount: 1);

    /// <summary>Online: the one player on this PC on the keyboard and the first pad.</summary>
    public static PlayerDevices Online(DeviceRoster roster) => Create(roster, playerCount: 1);

    private static PlayerDevices Create(DeviceRoster roster, int playerCount)
    {
        var assignment = new PlayerDevices(playerCount, claimsNewJoypads: true);
        assignment.Assign(0, InputDeviceId.Keyboard);
        int nextPlayer = 0;
        foreach (InputDeviceInfo device in roster.Devices)
        {
            if (device.Id.IsJoypad && nextPlayer < assignment.PlayerCount)
            {
                assignment.Assign(nextPlayer++, device.Id);
            }
        }

        return assignment;
    }
}
