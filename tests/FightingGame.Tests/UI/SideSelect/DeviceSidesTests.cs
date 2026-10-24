using FightingGame.Input;
using FightingGame.UI.Framework;
using FightingGame.UI.SideSelect;
using Xunit;
using static FightingGame.Tests.Input.TestJoypads;

namespace FightingGame.Tests.UI.SideSelect;

public sealed class DeviceSidesTests
{
    private const long ReadyTime = 400;

    private readonly DeviceRoster _roster = new();
    private readonly DeviceSides _sides = new(new SideSelectSettings(ReadyTime, SlideMilliseconds: 0, ShakeMilliseconds: 0));

    [Fact]
    public void EveryDeviceStartsInTheMiddle()
    {
        Begin(SideRules.Versus, Pad(0), Pad(1, Hitbox));

        Assert.Equal(3, _sides.Devices.Count);
        Assert.All(_sides.Devices, device => Assert.Equal(SideColumn.NotPlaying, device.Column));
        Assert.Equal(DeviceIcon.Keyboard, _sides.Devices[0].Icon);
    }

    [Fact]
    public void LeftAndRightMoveOnlyThatDeviceOneColumn()
    {
        Begin(SideRules.Versus, Pad(0), Pad(1, Hitbox));

        Press(Device(0), MenuAction.Left);
        Press(Device(1), MenuAction.Right);

        Assert.Equal(SideColumn.Player1, Column(Device(0)));
        Assert.Equal(SideColumn.Player2, Column(Device(1)));
        Assert.Equal(SideColumn.NotPlaying, Column(Keyboard));

        Press(Device(1), MenuAction.Left);
        Assert.Equal(SideColumn.NotPlaying, Column(Device(1)));
    }

    [Fact]
    public void ATakenSideRefusesTheMove()
    {
        Begin(SideRules.Versus, Pad(0));
        Press(Device(0), MenuAction.Left);

        Press(Keyboard, MenuAction.Left);

        Assert.Equal(SideColumn.NotPlaying, Column(Keyboard));
        Assert.Equal(1, Find(Keyboard).Refusals);
    }

    [Fact]
    public void MovingPastTheEdgeIsRefused()
    {
        Begin(SideRules.Versus, Pad(0));
        Press(Device(0), MenuAction.Left);

        Press(Device(0), MenuAction.Left);

        Assert.Equal(SideColumn.Player1, Column(Device(0)));
        Assert.Equal(1, Find(Device(0)).Refusals);
    }

    [Fact]
    public void ConfirmReadiesAndAReadyDeviceStaysPut()
    {
        Begin(SideRules.Versus, Pad(0));
        Press(Device(0), MenuAction.Right);

        Press(Device(0), MenuAction.Confirm);
        Press(Device(0), MenuAction.Left);

        Assert.True(Find(Device(0)).IsReady);
        Assert.Equal(SideColumn.Player2, Column(Device(0)));
    }

    [Fact]
    public void ConfirmInTheMiddleTakesTheFirstFreeSideReady()
    {
        Begin(SideRules.Versus, Pad(0), Pad(1, Hitbox));

        Press(Device(1), MenuAction.Confirm);
        Press(Device(0), MenuAction.Confirm);
        Press(Keyboard, MenuAction.Confirm);

        Assert.Equal(SideColumn.Player1, Column(Device(1)));
        Assert.Equal(SideColumn.Player2, Column(Device(0)));
        Assert.True(_sides.IsAllReady);
        Assert.Equal(SideColumn.NotPlaying, Column(Keyboard));
        Assert.Equal(1, Find(Keyboard).Refusals);
    }

    [Fact]
    public void BackUnreadiesThenReturnsToTheMiddleThenLeaves()
    {
        Begin(SideRules.Versus, Pad(0));
        Press(Device(0), MenuAction.Left);
        Press(Device(0), MenuAction.Confirm);

        Assert.False(Press(Device(0), MenuAction.Back));
        Assert.False(Find(Device(0)).IsReady);
        Assert.Equal(SideColumn.Player1, Column(Device(0)));
        Assert.False(Press(Device(0), MenuAction.Back));
        Assert.Equal(SideColumn.NotPlaying, Column(Device(0)));
        Assert.True(Press(Device(0), MenuAction.Back));
    }

    [Fact]
    public void TheKeyboardsEscapeIsItsBack()
    {
        Begin(SideRules.Versus, Pad(0));

        Assert.True(Press(Keyboard, MenuAction.Back | MenuAction.Start));
    }

    [Fact]
    public void VersusMovesOnOnlyOnceBothSidesHaveBeenReadyForAMoment()
    {
        Begin(SideRules.Versus, Pad(0), Pad(1, Hitbox));
        Press(Device(0), MenuAction.Left);
        Press(Device(0), MenuAction.Confirm);
        Assert.False(_sides.Update(0));
        Assert.False(_sides.Update(ReadyTime));

        Press(Device(1), MenuAction.Right);
        Press(Device(1), MenuAction.Confirm);

        Assert.False(_sides.Update(1000));
        Assert.True(_sides.Update(1000 + ReadyTime));
    }

    [Fact]
    public void ABackDuringTheReadyMomentStopsIt()
    {
        Begin(SideRules.Versus, Pad(0), Pad(1, Hitbox));
        Press(Device(0), MenuAction.Confirm);
        Press(Device(1), MenuAction.Confirm);
        Assert.False(_sides.Update(0));

        Press(Device(1), MenuAction.Back);

        Assert.False(_sides.Update(ReadyTime));
    }

    [Fact]
    public void TrainingNeedsOnlyPlayerOneAndPlayerTwoIsTheDummy()
    {
        Begin(SideRules.Training, Pad(0), Pad(1, Hitbox));
        Press(Device(1), MenuAction.Right);

        Assert.Equal(SideColumn.NotPlaying, Column(Device(1)));
        Assert.Equal(1, Find(Device(1)).Refusals);

        Press(Device(0), MenuAction.Confirm);
        Assert.False(_sides.Update(0));
        Assert.True(_sides.Update(ReadyTime));
        PlayerDevices assignment = _sides.ToAssignment();
        Assert.Equal(1, assignment.PlayerCount);
        Assert.Equal([Device(0)], assignment.Of(0));
    }

    [Fact]
    public void ALoneDeviceStartsOnPlayerOneAndOneConfirmContinues()
    {
        Begin(SideRules.Training);

        Assert.Equal(SideColumn.Player1, Column(Keyboard));
        Press(Keyboard, MenuAction.Confirm);
        Assert.True(_sides.IsAllReady);
    }

    [Fact]
    public void ALoneDeviceInVersusStillNeedsASecondOne()
    {
        Begin(SideRules.Versus);
        Press(Keyboard, MenuAction.Confirm);

        Assert.False(_sides.IsAllReady);
        Assert.False(_sides.Update(ReadyTime));
    }

    [Fact]
    public void TheAssignmentHasExactlyTheDevicesOnTheSidesAndNeverClaimsPads()
    {
        Begin(SideRules.Versus, Pad(0), Pad(1, Hitbox));
        Press(Device(1), MenuAction.Left);
        Press(Keyboard, MenuAction.Right);

        PlayerDevices assignment = _sides.ToAssignment();

        Assert.Equal([Device(1)], assignment.Of(0));
        Assert.Equal([Keyboard], assignment.Of(1));
        Assert.Equal(-1, assignment.PlayerOf(Device(0)));
        Assert.False(assignment.ClaimsNewJoypads);
    }

    [Fact]
    public void ThePlayersStartWhereTheyWereLastChosen()
    {
        _roster.Update([Pad(0), Pad(1, Hitbox)]);
        var remembered = new PlayerDevices(2);
        remembered.Assign(0, Device(1));
        remembered.Assign(1, Device(0));

        _sides.Begin(_roster.Devices, SideRules.Versus, remembered);

        Assert.Equal(SideColumn.Player1, Column(Device(1)));
        Assert.Equal(SideColumn.Player2, Column(Device(0)));
        Assert.All(_sides.Devices, device => Assert.False(device.IsReady));
    }

    [Fact]
    public void TrainingLeavesARememberedPlayerTwoInTheMiddle()
    {
        _roster.Update([Pad(0), Pad(1, Hitbox)]);
        var remembered = new PlayerDevices(2);
        remembered.Assign(0, Device(0));
        remembered.Assign(1, Device(1));

        _sides.Begin(_roster.Devices, SideRules.Training, remembered);

        Assert.Equal(SideColumn.Player1, Column(Device(0)));
        Assert.Equal(SideColumn.NotPlaying, Column(Device(1)));
    }

    [Fact]
    public void ARememberedDeviceThatIsGoneLeavesItsSideFree()
    {
        _roster.Update([Pad(0), Pad(1, Hitbox)]);
        var remembered = new PlayerDevices(2);
        remembered.Assign(0, Device(1));
        _roster.Update([Pad(0)]);

        _sides.Begin(_roster.Devices, SideRules.Versus, remembered);

        Assert.Null(_sides.On(0));
    }

    [Fact]
    public void APluggedInPadAppearsInTheMiddle()
    {
        Begin(SideRules.Versus, Pad(0));

        _roster.Update([Pad(0), Pad(1, Hitbox)]);
        _sides.Follow(_roster.Devices);

        Assert.Equal(3, _sides.Devices.Count);
        Assert.Equal(SideColumn.NotPlaying, Column(Device(1)));
    }

    [Fact]
    public void APressFromADeviceNotOnTheScreenOnlyMakesItAppear()
    {
        Begin(SideRules.Versus);
        _roster.Update([Pad(0)]);

        Press(Device(0), MenuAction.Confirm);

        Assert.Equal(SideColumn.NotPlaying, Column(Device(0)));
        Assert.False(Find(Device(0)).IsReady);
    }

    [Fact]
    public void UnpluggingFreesTheSideAndPluggingBackReturnsToIt()
    {
        Begin(SideRules.Versus, Pad(0), Pad(1, Hitbox));
        InputDeviceId hitbox = Device(1);
        Press(hitbox, MenuAction.Right);
        Press(hitbox, MenuAction.Confirm);

        _roster.Update([Pad(0)]);
        _sides.Follow(_roster.Devices);

        Assert.Null(_sides.On(1));
        Assert.Equal(2, _sides.Devices.Count);

        _roster.Update([Pad(0), Pad(1, Hitbox)]);
        _sides.Follow(_roster.Devices);

        Assert.Equal(SideColumn.Player2, Column(hitbox));
        Assert.False(Find(hitbox).IsReady);
    }

    [Fact]
    public void APadPluggedBackAfterItsSideWasTakenWaitsInTheMiddle()
    {
        Begin(SideRules.Versus, Pad(0), Pad(1, Hitbox));
        InputDeviceId hitbox = Device(1);
        Press(hitbox, MenuAction.Right);
        _roster.Update([Pad(0)]);
        _sides.Follow(_roster.Devices);
        Press(Device(0), MenuAction.Right);

        _roster.Update([Pad(0), Pad(1, Hitbox)]);
        _sides.Follow(_roster.Devices);

        Assert.Equal(SideColumn.NotPlaying, Column(hitbox));
        Assert.Equal(SideColumn.Player2, Column(Device(0)));
    }

    [Fact]
    public void UnpluggingAReadyPlayerStopsTheReadyMoment()
    {
        Begin(SideRules.Versus, Pad(0), Pad(1, Hitbox));
        Press(Device(0), MenuAction.Confirm);
        Press(Device(1), MenuAction.Confirm);
        Assert.False(_sides.Update(0));

        _roster.Update([Pad(0)]);
        _sides.Follow(_roster.Devices);

        Assert.False(_sides.Update(ReadyTime));
    }

    private static InputDeviceId Keyboard => InputDeviceId.Keyboard;

    private void Begin(SideRules rules, params JoypadInfo[] pads)
    {
        _roster.Update(pads);
        _sides.Begin(_roster.Devices, rules, remembered: null);
    }

    private InputDeviceId Device(int joypad) => Id(_roster, joypad);

    private bool Press(InputDeviceId id, MenuAction action) => _sides.Press(Info(id), action);

    private InputDeviceInfo Info(InputDeviceId id)
    {
        foreach (InputDeviceInfo device in _roster.Devices)
        {
            if (device.Id == id)
            {
                return device;
            }
        }

        throw new Xunit.Sdk.XunitException($"{id} isn't connected");
    }

    private SideSelectDevice Find(InputDeviceId id) => Assert.Single(_sides.Devices, device => device.Id == id);

    private SideColumn Column(InputDeviceId id) => Find(id).Column;
}
