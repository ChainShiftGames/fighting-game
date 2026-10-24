using FightingGame.Input;
using Xunit;
using static FightingGame.Tests.Input.TestJoypads;

namespace FightingGame.Tests.Input;

public sealed class ControllerReconnectTests
{
    private readonly DeviceRoster _roster = new();

    [Fact]
    public void APlayerWaitsWhileTheirPadIsUnplugged()
    {
        _roster.Update([Pad(0), Pad(1)]);
        ControllerReconnect reconnect = Watch(DefaultPlayerDevices.Versus(_roster));

        _roster.Update([Pad(0)]);

        Assert.Equal(1, reconnect.WaitingPlayer);
    }

    [Fact]
    public void ThePadGoesBackToItsPlayerWhenReplugged()
    {
        _roster.Update([Pad(0), Pad(1)]);
        PlayerDevices assignment = DefaultPlayerDevices.Versus(_roster);
        ControllerReconnect reconnect = Watch(assignment);
        InputDeviceId player1Pad = Id(_roster, 0);

        _roster.Update([Pad(1)]);
        _roster.Update([Pad(0), Pad(1)]);

        Assert.Equal(ControllerReconnect.NobodyWaiting, reconnect.WaitingPlayer);
        Assert.Equal(0, assignment.PlayerOf(player1Pad));
    }

    [Fact]
    public void StartOnAFreePadTakesTheMissingOnesPlace()
    {
        _roster.Update([Pad(0)]);
        PlayerDevices assignment = Explicit((0, Id(_roster, 0)));
        ControllerReconnect reconnect = Watch(assignment);
        _roster.Update([Pad(1, Hitbox)]);

        Assert.True(reconnect.TakeOver(1));

        Assert.Equal(ControllerReconnect.NobodyWaiting, reconnect.WaitingPlayer);
        Assert.Equal([InputDeviceId.Keyboard, Id(_roster, 1)], assignment.Of(0));
    }

    [Fact]
    public void StartOnTheOtherPlayersPadTakesNothing()
    {
        _roster.Update([Pad(0), Pad(1)]);
        PlayerDevices assignment = DefaultPlayerDevices.Versus(_roster);
        ControllerReconnect reconnect = Watch(assignment);
        _roster.Update([Pad(1)]);

        Assert.False(reconnect.TakeOver(1));
        Assert.Equal(0, reconnect.WaitingPlayer);
    }

    [Fact]
    public void StartDoesNothingWhileNobodyWaits()
    {
        _roster.Update([Pad(0), Pad(1)]);
        PlayerDevices assignment = Explicit((0, Id(_roster, 0)));

        Assert.False(Watch(assignment).TakeOver(1));
        Assert.Equal(-1, assignment.PlayerOf(Id(_roster, 1)));
    }

    // As before players chose: plugging a pad in mid-match gives it to someone without one.
    [Fact]
    public void UnderTheDefaultANewPadGoesToTheFirstPlayerWithout()
    {
        _roster.Update([Pad(0)]);
        PlayerDevices assignment = DefaultPlayerDevices.Versus(_roster);
        Watch(assignment);

        _roster.Update([Pad(0), Pad(1, Hitbox)]);

        Assert.Equal(1, assignment.PlayerOf(Id(_roster, 1)));
    }

    [Fact]
    public void UnderTheDefaultANewPadReplacesAWaitingPlayersPad()
    {
        _roster.Update([Pad(0)]);
        PlayerDevices assignment = DefaultPlayerDevices.Versus(_roster);
        ControllerReconnect reconnect = Watch(assignment);

        _roster.Update([Pad(1, Hitbox)]);

        Assert.Equal(0, assignment.PlayerOf(Id(_roster, 1)));
        Assert.Equal(ControllerReconnect.NobodyWaiting, reconnect.WaitingPlayer);
    }

    [Fact]
    public void AChosenAssignmentLeavesNewPadsFree()
    {
        PlayerDevices assignment = Explicit();
        Watch(assignment);

        _roster.Update([Pad(0)]);

        Assert.Equal(-1, assignment.PlayerOf(Id(_roster, 0)));
    }

    private ControllerReconnect Watch(PlayerDevices assignment)
    {
        var reconnect = new ControllerReconnect(assignment, _roster);
        _roster.Connected += reconnect.OnConnected;
        return reconnect;
    }

    // Player 1 on the keyboard, plus the given pads.
    private static PlayerDevices Explicit(params (int Player, InputDeviceId Device)[] pads)
    {
        var assignment = new PlayerDevices(2);
        assignment.Assign(0, InputDeviceId.Keyboard);
        foreach ((int player, InputDeviceId device) in pads)
        {
            assignment.Assign(player, device);
        }

        return assignment;
    }
}
