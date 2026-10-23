using System;
using Godot;

namespace FightingGame.Input;

/// <summary>
/// The assignment each new match starts with: the command line's test assignment when given (developer option),
/// otherwise the default one (<see cref="DefaultPlayerDevices"/>). Built when the match starts, from the pads
/// connected then.
/// </summary>
public sealed class PlayerDevicesChoice
{
    private readonly DeviceRoster _roster;
    private readonly string[] _args;

    public PlayerDevicesChoice(DeviceRoster roster, string[] args)
    {
        _roster = roster;
        _args = args;
    }

    public DeviceRoster Roster => _roster;

    public PlayerDevices Versus() => FromArgs(2) ?? DefaultPlayerDevices.Versus(_roster);

    public PlayerDevices Training() => FromArgs(1) ?? DefaultPlayerDevices.Training(_roster);

    public PlayerDevices Online() => FromArgs(1) ?? DefaultPlayerDevices.Online(_roster);

    private PlayerDevices? FromArgs(int playerCount)
    {
        try
        {
            return DeviceAssignmentArgs.Parse(_args, playerCount, _roster);
        }
        catch (FormatException exception)
        {
            GD.PushError($"Ignoring the device options, using the default devices: {exception.Message}");
            return null;
        }
    }
}
