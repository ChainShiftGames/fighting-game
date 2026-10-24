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
        assignment.Assign(0, InputDeviceId.Keyboard);

        assignment.Assign(1, InputDeviceId.Keyboard);

        Assert.Empty(assignment.Of(0));
        Assert.Equal(1, assignment.PlayerOf(InputDeviceId.Keyboard));
    }

    [Fact]
    public void ReplacingTakesTheDeviceFromItsOldPlayer()
    {
        _roster.Update([Pad(0)]);
        var assignment = new PlayerDevices(2);
        assignment.Assign(0, Id(_roster, 0));
        assignment.Assign(1, InputDeviceId.Keyboard);

        assignment.Replace(0, Id(_roster, 0), InputDeviceId.Keyboard);

        Assert.Equal([InputDeviceId.Keyboard], assignment.Of(0));
        Assert.Empty(assignment.Of(1));
    }

    // Player n gets the n-th pad, as when player n read joypad slot n; the keyboard goes to player 1.
    [Fact]
    public void TheVersusDefaultGivesPadsInRosterOrder()
    {
        _roster.Update([Pad(3), Pad(1, Hitbox), Pad(5)]);

        PlayerDevices assignment = DefaultPlayerDevices.Versus(_roster);

        Assert.Equal([InputDeviceId.Keyboard, Id(_roster, 1)], assignment.Of(0));
        Assert.Equal([Id(_roster, 3)], assignment.Of(1));
        Assert.Equal(-1, assignment.PlayerOf(Id(_roster, 5)));
    }

    [Fact]
    public void WithoutPadsPlayer1HasTheKeyboard()
    {
        PlayerDevices versus = DefaultPlayerDevices.Versus(_roster);

        Assert.Equal([InputDeviceId.Keyboard], versus.Of(0));
        Assert.Empty(versus.Of(1));
        Assert.Equal([InputDeviceId.Keyboard], DefaultPlayerDevices.Training(_roster).Of(0));
        Assert.Equal([InputDeviceId.Keyboard], DefaultPlayerDevices.Online(_roster).Of(0));
    }

    [Fact]
    public void TrainingAndOnlineTakeTheFirstPad()
    {
        _roster.Update([Pad(0), Pad(1)]);

        Assert.Equal([InputDeviceId.Keyboard, Id(_roster, 0)], DefaultPlayerDevices.Training(_roster).Of(0));
        Assert.Equal([InputDeviceId.Keyboard, Id(_roster, 0)], DefaultPlayerDevices.Online(_roster).Of(0));
    }
}
