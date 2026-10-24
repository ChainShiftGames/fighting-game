using System;
using Godot;

namespace FightingGame.Input;

/// <summary>
/// The assignment each new match starts with: the one chosen on side select this session (UI-03), else the command
/// line's test assignment when given (developer option), otherwise the default one (<see cref="DefaultPlayerDevices"/>).
/// The last two are built when the match starts, from the pads connected then.
/// </summary>
/// <remarks>
/// A chosen assignment is kept and handed out as it is, so rematches and character select use it too, and a free pad
/// that takes a disconnected one's place during a match stays in it (ADR-0038 §6).
/// </remarks>
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

    /// <summary>The two players side select last chose for versus; null before the first choice.</summary>
    public PlayerDevices? ChosenVersus { get; private set; }

    /// <summary>The one player side select last chose for training; null before the first choice.</summary>
    public PlayerDevices? ChosenTraining { get; private set; }

    public PlayerDevices Versus() => ChosenVersus ?? FromArgs(2) ?? DefaultPlayerDevices.Versus(_roster);

    public PlayerDevices Training() => ChosenTraining ?? FromArgs(1) ?? DefaultPlayerDevices.Training(_roster);

    /// <summary>Side select's choice: the next versus matches use it.</summary>
    public void ChooseVersus(PlayerDevices assignment) => ChosenVersus = Checked(assignment, 2);

    /// <summary>Side select's choice: the next training matches use it.</summary>
    public void ChooseTraining(PlayerDevices assignment) => ChosenTraining = Checked(assignment, 1);

    public PlayerDevices Online() => FromArgs(1) ?? DefaultPlayerDevices.Online(_roster);

    private static PlayerDevices Checked(PlayerDevices assignment, int playerCount) =>
        assignment.PlayerCount == playerCount
            ? assignment
            : throw new ArgumentException($"Expected {playerCount} players, got {assignment.PlayerCount}.", nameof(assignment));

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
