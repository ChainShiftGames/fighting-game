using System;
using FightingGame.Input;
using Xunit;
using static FightingGame.Tests.Input.TestJoypads;

namespace FightingGame.Tests.Input;

public sealed class DeviceAssignmentArgsTests
{
    private readonly DeviceRoster _roster = new();

    public DeviceAssignmentArgsTests()
    {
        _roster.Update([Pad(0), Pad(1, Hitbox)]);
    }

    [Fact]
    public void NoOptionsGiveNoAssignment() => Assert.Null(DeviceAssignmentArgs.Parse(["--training"], 2, _roster));

    [Fact]
    public void PadsCountFromOneInRosterOrder()
    {
        PlayerDevices assignment = DeviceAssignmentArgs.Parse(
            ["--p1-devices", "pad2", "--p2-devices", "keyboard-left,pad1"], 2, _roster)!;

        Assert.Equal([Id(_roster, 1)], assignment.Of(0));
        Assert.Equal([InputDeviceId.KeyboardLeft, Id(_roster, 0)], assignment.Of(1));
        Assert.False(assignment.ClaimsNewJoypads);
    }

    [Fact]
    public void OnePlayerMatchesIgnoreThePlayer2Option()
    {
        PlayerDevices assignment = DeviceAssignmentArgs.Parse(
            ["--p1-devices", "keyboard-right", "--p2-devices", "pad1"], 1, _roster)!;

        Assert.Equal(1, assignment.PlayerCount);
        Assert.Equal([InputDeviceId.KeyboardRight], assignment.Of(0));
    }

    [Theory]
    [InlineData("pad3")]
    [InlineData("pad0")]
    [InlineData("mouse")]
    [InlineData("pad1,pad1")]
    public void BadDevicesAreRefused(string devices) =>
        Assert.Throws<FormatException>(() => DeviceAssignmentArgs.Parse(["--p1-devices", devices], 2, _roster));

    [Fact]
    public void OneDeviceOnTwoPlayersIsRefused() =>
        Assert.Throws<FormatException>(
            () => DeviceAssignmentArgs.Parse(["--p1-devices", "pad1", "--p2-devices", "pad1"], 2, _roster));
}
