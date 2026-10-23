using System.Collections.Generic;
using FightingGame.Tests.Input;
using FightingGame.UI.Framework;
using Godot;
using Xunit;

namespace FightingGame.Tests.UI.Framework;

public sealed class MenuActionReaderTests
{
    private static readonly MenuTiming _timing = new(
        GraceMilliseconds: 600,
        RepeatDelayMilliseconds: 300,
        RepeatIntervalMilliseconds: 100,
        TransitionMilliseconds: 0,
        StickDeadzone: 0.5f);

    private readonly MenuActionReader _reader = new(MenuBindings.Default, _timing);

    [Fact]
    public void KeysAndButtonsBecomeActionsWithTheirDevice()
    {
        Assert.Equal(new MenuPress(MenuAction.Confirm, MenuDevice.Keyboard), _reader.KeyChanged(Key.Enter, true, 0));
        Assert.Equal(new MenuPress(MenuAction.Back, MenuDevice.Gamepad(3)), _reader.ButtonChanged(3, JoyButton.B, true, 0));
        Assert.Equal(MenuDevice.Gamepad(1), _reader.ButtonChanged(1, JoyButton.A, true, 0)!.Value.Device);
    }

    [Fact]
    public void EscapeIsBothBackAndStart()
    {
        MenuPress press = _reader.KeyChanged(Key.Escape, true, 0)!.Value;

        Assert.True(press.Has(MenuAction.Back));
        Assert.True(press.Has(MenuAction.Start));
    }

    [Fact]
    public void UnboundControlsAndReleasesMakeNoPress()
    {
        Assert.Null(_reader.KeyChanged(Key.F1, true, 0));
        Assert.Null(_reader.ButtonChanged(0, JoyButton.X, true, 0));
        _reader.KeyChanged(Key.Down, true, 0);
        Assert.Null(_reader.KeyChanged(Key.Down, false, 10));
    }

    [Fact]
    public void AControlAlreadyDownMakesNoSecondPress()
    {
        _reader.KeyChanged(Key.Down, true, 0);

        Assert.Null(_reader.KeyChanged(Key.Down, true, 50));
    }

    [Fact]
    public void HeldDirectionsRepeatAfterTheDelayThenAtTheInterval()
    {
        _reader.KeyChanged(Key.Down, true, 0);

        Assert.Empty(Repeats(299));
        Assert.Single(Repeats(300));
        Assert.Empty(Repeats(399));
        MenuPress repeat = Assert.Single(Repeats(400));
        Assert.Equal(new MenuPress(MenuAction.Down, MenuDevice.Keyboard, IsRepeat: true), repeat);
    }

    [Fact]
    public void ASlowFrameRepeatsOnlyOnce()
    {
        _reader.KeyChanged(Key.Down, true, 0);

        Assert.Single(Repeats(2000));
        Assert.Single(Repeats(2100));
    }

    [Fact]
    public void OnlyDirectionsRepeat()
    {
        _reader.KeyChanged(Key.Enter, true, 0);
        _reader.ButtonChanged(0, JoyButton.B, true, 0);

        Assert.Empty(Repeats(5000));
    }

    [Fact]
    public void LettingGoStopsTheRepeat()
    {
        _reader.ButtonChanged(0, JoyButton.DpadUp, true, 0);
        _reader.ButtonChanged(0, JoyButton.DpadUp, false, 100);

        Assert.Empty(Repeats(1000));
    }

    [Fact]
    public void TheStickCountsPastTheDeadzone()
    {
        Assert.Null(_reader.StickMoved(0, JoyAxis.LeftY, 0.4f, 0));
        Assert.Equal(new MenuPress(MenuAction.Down, MenuDevice.Gamepad(0)), _reader.StickMoved(0, JoyAxis.LeftY, 0.8f, 0));
        Assert.Null(_reader.StickMoved(0, JoyAxis.LeftY, 0.9f, 10));
        Assert.Equal(MenuAction.Up, _reader.StickMoved(0, JoyAxis.LeftY, -0.9f, 20)!.Value.Actions);
        Assert.Null(_reader.StickMoved(0, JoyAxis.RightX, 1f, 30));
    }

    [Fact]
    public void ControlsReleasedOutOfSightAreForgotten()
    {
        var devices = new FakeDeviceState().Hold(Key.Down);
        _reader.KeyChanged(Key.Down, true, 0);
        _reader.KeyChanged(Key.Enter, true, 0);

        _reader.ForgetLetGo(devices);

        Assert.False(_reader.IsHeld(MenuAction.Confirm));
        Assert.True(_reader.IsHeld(MenuAction.Down));
    }

    [Fact]
    public void IsHeldSeesAnyDevice()
    {
        _reader.ButtonChanged(2, JoyButton.A, true, 0);

        Assert.True(_reader.IsHeld(MenuAction.Confirm | MenuAction.Back));
        Assert.False(_reader.IsHeld(MenuAction.Start));
    }

    private List<MenuPress> Repeats(long now)
    {
        var presses = new List<MenuPress>();
        _reader.AddRepeats(now, presses);
        return presses;
    }
}
