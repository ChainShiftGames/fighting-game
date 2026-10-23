using FightingGame.Input;
using FightingGame.UI.CharacterSelect;
using FightingGame.UI.Framework;
using Xunit;
using static FightingGame.Tests.Input.TestJoypads;

namespace FightingGame.Tests.UI.CharacterSelect;

public sealed class AssignedMenuPlayersTests
{
    private readonly DeviceRoster _roster = new();

    [Fact]
    public void EachDeviceMovesItsOwnPlayer()
    {
        _roster.Update([Pad(0), Pad(1, Hitbox)]);
        var players = new AssignedMenuPlayers(DefaultPlayerDevices.Versus(_roster), _roster);

        Assert.Equal(0, players.PlayerOf(MenuDevice.Keyboard));
        Assert.Equal(0, players.PlayerOf(MenuDevice.Gamepad(0)));
        Assert.Equal(1, players.PlayerOf(MenuDevice.Gamepad(1)));
        Assert.Equal(-1, players.PlayerOf(MenuDevice.Mouse));
        Assert.True(players.CanPress(1));
    }

    [Fact]
    public void WithOnlyTheKeyboardPlayerTwoCantPressUntilAPadJoins()
    {
        var players = new AssignedMenuPlayers(DefaultPlayerDevices.Versus(_roster), _roster);
        Assert.False(players.CanPress(1));

        _roster.Update([Pad(0)]);

        Assert.Equal(0, players.PlayerOf(MenuDevice.Gamepad(0)));
        _roster.Update([Pad(0), Pad(1, Hitbox)]);
        Assert.Equal(1, players.PlayerOf(MenuDevice.Gamepad(1)));
        Assert.True(players.CanPress(1));
    }

    [Fact]
    public void AChosenAssignmentNeverClaimsPads()
    {
        _roster.Update([Pad(0)]);
        var assignment = new PlayerDevices(2);
        assignment.Assign(0, InputDeviceId.Keyboard);
        var players = new AssignedMenuPlayers(assignment, _roster);

        Assert.Equal(-1, players.PlayerOf(MenuDevice.Gamepad(0)));
    }

    [Fact]
    public void TrainingHasNoSecondPlayer()
    {
        _roster.Update([Pad(0), Pad(1, Hitbox)]);
        var players = new AssignedMenuPlayers(DefaultPlayerDevices.Training(_roster), _roster);

        Assert.Equal(-1, players.PlayerOf(MenuDevice.Gamepad(1)));
        Assert.False(players.CanPress(1));
    }
}
