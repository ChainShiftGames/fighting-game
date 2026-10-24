using System.Collections.Generic;
using FightingGame.Input;
using FightingGame.Input.Profiles;
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

    [Fact]
    public void ConfirmAndBackComeFromEachDevicesLayout()
    {
        var profiles = new RemappedMenus();
        var reader = new MenuActionReader(MenuBindings.Default.WithProfiles(profiles), _timing);

        Assert.Equal(MenuAction.Confirm, reader.KeyChanged(Key.J, true, 0)!.Value.Actions);
        Assert.Null(reader.KeyChanged(Key.Enter, true, 0));
        Assert.Equal(MenuAction.Start, reader.KeyChanged(Key.Escape, true, 0)!.Value.Actions);
        Assert.Equal(MenuAction.Back, reader.ButtonChanged(0, JoyButton.Y, true, 0)!.Value.Actions);
        Assert.Equal(MenuAction.Up, reader.KeyChanged(Key.Up, true, 0)!.Value.Actions);
    }

    // Nobody locks themselves out: the default back control held long enough is back, however back was rebound.
    [Fact]
    public void HoldingTheDefaultBackControlGoesBackWhenBackWasRemapped()
    {
        var reader = new MenuActionReader(MenuBindings.Default.WithProfiles(new RemappedMenus()), _timing);
        reader.KeyChanged(Key.Escape, true, 0);
        Assert.Null(reader.ButtonChanged(1, JoyButton.B, true, 0));

        var early = new List<MenuPress>();
        reader.AddRepeats(MenuBindings.FallbackHoldMilliseconds - 1, early);
        var due = new List<MenuPress>();
        reader.AddRepeats(MenuBindings.FallbackHoldMilliseconds, due);
        var later = new List<MenuPress>();
        reader.AddRepeats(MenuBindings.FallbackHoldMilliseconds * 3, later);

        Assert.Empty(early);
        Assert.Equal([new MenuPress(MenuAction.Back, MenuDevice.Keyboard), new MenuPress(MenuAction.Back, MenuDevice.Gamepad(1))], due);
        Assert.Empty(later);
    }

    [Fact]
    public void LettingGoBeforeTheTimeIsUpDoesNothing()
    {
        var reader = new MenuActionReader(MenuBindings.Default.WithProfiles(new RemappedMenus()), _timing);
        reader.ButtonChanged(1, JoyButton.B, true, 0);
        reader.ButtonChanged(1, JoyButton.B, false, 500);

        var presses = new List<MenuPress>();
        reader.AddRepeats(MenuBindings.FallbackHoldMilliseconds, presses);

        Assert.Empty(presses);
    }

    [Fact]
    public void WhileBIsStillBackHoldingItGoesBackOnlyOnce()
    {
        _reader.ButtonChanged(1, JoyButton.B, true, 0);

        Assert.Empty(Repeats(MenuBindings.FallbackHoldMilliseconds * 2));
    }

    private List<MenuPress> Repeats(long now)
    {
        var presses = new List<MenuPress>();
        _reader.AddRepeats(now, presses);
        return presses;
    }

    // Keyboard: confirm on J, back on K. Pads: confirm on X, back on Y.
    private sealed class RemappedMenus : IMenuProfiles
    {
        public BindingProfile Keyboard { get; } = Menu(InputDeviceKind.Keyboard, BoundControl.OfKey(Key.J), BoundControl.OfKey(Key.K));

        public BindingProfile? Joypad(int joypad) =>
            Menu(InputDeviceKind.Joypad, BoundControl.OfButton(JoyButton.X), BoundControl.OfButton(JoyButton.Y));

        private static BindingProfile Menu(InputDeviceKind kind, BoundControl confirm, BoundControl back) =>
            new(kind, [], [new(MenuInputs.Confirm, confirm), new(MenuInputs.Back, back)], StickOptions.None);
    }
}
