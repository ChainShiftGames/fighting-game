using FightingGame.Input;
using Xunit;
using static FightingGame.Tests.Input.TestJoypads;

namespace FightingGame.Tests.Input;

public sealed class PlayerDevicesTests
{
    private readonly DeviceRoster _roster = new();

    [Fact]
    public void ADeviceIsNeverOnTwoPlayers()
    {
        var assignment = new PlayerDevices(2);
        assignment.Assign(0, InputDeviceId.KeyboardLeft);

        assignment.Assign(1, InputDeviceId.KeyboardLeft);

        Assert.Empty(assignment.Of(0));
        Assert.Equal(1, assignment.PlayerOf(InputDeviceId.KeyboardLeft));
    }

    [Fact]
    public void ReplacingTakesTheDeviceFromItsOldPlayer()
    {
        var assignment = new PlayerDevices(2);
        assignment.Assign(0, InputDeviceId.KeyboardLeft);
        assignment.Assign(1, InputDeviceId.KeyboardRight);

        assignment.Replace(0, InputDeviceId.KeyboardLeft, InputDeviceId.KeyboardRight);

        Assert.Equal([InputDeviceId.KeyboardRight], assignment.Of(0));
        Assert.Empty(assignment.Of(1));
    }

    // Before players could choose, player n read their keyboard half and the joypad in slot n.
    [Fact]
    public void TheVersusDefaultIsTheOldSlotOrder()
    {
        _roster.Update([Pad(3), Pad(1, Hitbox), Pad(5)]);

        PlayerDevices assignment = DefaultPlayerDevices.Versus(_roster);

        Assert.Equal([InputDeviceId.KeyboardLeft, Id(_roster, 1)], assignment.Of(0));
        Assert.Equal([InputDeviceId.KeyboardRight, Id(_roster, 3)], assignment.Of(1));
        Assert.Equal(-1, assignment.PlayerOf(Id(_roster, 5)));
    }

    [Fact]
    public void WithoutPadsTheDefaultsAreTheKeyboardHalves()
    {
        Assert.Equal([InputDeviceId.KeyboardLeft], DefaultPlayerDevices.Versus(_roster).Of(0));
        Assert.Equal([InputDeviceId.KeyboardLeft], DefaultPlayerDevices.Training(_roster).Of(0));
        Assert.Equal(
            [InputDeviceId.KeyboardLeft, InputDeviceId.KeyboardRight], DefaultPlayerDevices.Online(_roster).Of(0));
    }

    [Fact]
    public void TrainingAndOnlineTakeTheFirstPad()
    {
        _roster.Update([Pad(0), Pad(1)]);

        Assert.Equal([InputDeviceId.KeyboardLeft, Id(_roster, 0)], DefaultPlayerDevices.Training(_roster).Of(0));
        Assert.Equal(
            [InputDeviceId.KeyboardLeft, InputDeviceId.KeyboardRight, Id(_roster, 0)],
            DefaultPlayerDevices.Online(_roster).Of(0));
    }
}
