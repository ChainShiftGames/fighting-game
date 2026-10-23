namespace FightingGame.Input;

/// <summary>
/// The assignments matches start with until players choose (side select, UI-03). As before there was a choice,
/// player n reads their keyboard half and the n-th joypad, and a joypad plugged in later goes to a player without one.
/// </summary>
public static class DefaultPlayerDevices
{
    /// <summary>Local versus: player 1 on the left keyboard half and the first pad, player 2 on the right and the second.</summary>
    public static PlayerDevices Versus(DeviceRoster roster) =>
        Create(roster, [InputDeviceId.KeyboardLeft], [InputDeviceId.KeyboardRight]);

    /// <summary>Training: the one player on the left keyboard half and the first pad.</summary>
    public static PlayerDevices Training(DeviceRoster roster) => Create(roster, [InputDeviceId.KeyboardLeft]);

    /// <summary>Online: the one player on this PC on either keyboard half and the first pad.</summary>
    public static PlayerDevices Online(DeviceRoster roster) =>
        Create(roster, [InputDeviceId.KeyboardLeft, InputDeviceId.KeyboardRight]);

    private static PlayerDevices Create(DeviceRoster roster, params InputDeviceId[][] keyboardsOfPlayer)
    {
        var assignment = new PlayerDevices(keyboardsOfPlayer.Length, claimsNewJoypads: true);
        for (int player = 0; player < keyboardsOfPlayer.Length; player++)
        {
            foreach (InputDeviceId keyboard in keyboardsOfPlayer[player])
            {
                assignment.Assign(player, keyboard);
            }
        }

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
