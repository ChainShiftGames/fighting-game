using FightingGame.Input;
using FightingGame.Input.Profiles;
using Godot;
using Xunit;

namespace FightingGame.Tests.Input.Profiles;

public sealed class BindingProfileTests
{
    private static readonly BoundControl _j = BoundControl.OfKey(Key.J);
    private static readonly BoundControl _p = BoundControl.OfKey(Key.P);
    private static readonly BoundControl _q = BoundControl.OfKey(Key.Q);

    private readonly BindingProfile _keyboard = TestButtons.Presets.DefaultOf(InputDeviceKind.Keyboard);

    [Fact]
    public void ReplacingTakesTheInputsOtherControlsOff()
    {
        BindingProfile profile = _keyboard.Bind(BindingLayer.Game, "c", _q, replace: true);

        Assert.Equal([_q], profile.ControlsOf(BindingLayer.Game, "c"));
    }

    [Fact]
    public void AddingKeepsTheOthersSoAnInputCanHaveSeveralControls()
    {
        BindingProfile profile = _keyboard.Bind(BindingLayer.Game, "c", _q, replace: false);

        Assert.Equal([_j, _p, _q], profile.ControlsOf(BindingLayer.Game, "c"));
    }

    [Fact]
    public void AddingAControlTheInputHasDoesntListItTwice()
    {
        BindingProfile profile = _keyboard.Bind(BindingLayer.Game, "c", _j, replace: false);

        Assert.Equal([_p, _j], profile.ControlsOf(BindingLayer.Game, "c"));
    }

    // The shipped keyboard puts P on special and C at once, for the 236C shortcut.
    [Fact]
    public void AControlOnSeveralInputsIsFound()
    {
        Assert.Equal(["special"], _keyboard.InputsBoundTo(BindingLayer.Game, _p, except: "c"));
        Assert.Empty(_keyboard.InputsBoundTo(BindingLayer.Game, _j, except: "c"));
    }

    [Fact]
    public void MenuAndGameBindingsAreNotDuplicatesOfEachOther()
    {
        BindingProfile pad = TestButtons.Presets.DefaultOf(InputDeviceKind.Joypad);

        Assert.Equal(["d"], pad.InputsBoundTo(BindingLayer.Game, BoundControl.OfButton(JoyButton.A)));
        Assert.Equal([MenuInputs.Confirm], pad.InputsBoundTo(BindingLayer.Menu, BoundControl.OfButton(JoyButton.A)));
    }

    [Fact]
    public void ReleasingKeepsTheControlOnOneInputOnly()
    {
        BindingProfile profile = _keyboard.Release(BindingLayer.Game, _p, keepOn: "special");

        Assert.Equal([_j], profile.ControlsOf(BindingLayer.Game, "c"));
        Assert.Contains(_p, profile.ControlsOf(BindingLayer.Game, "special"));
    }

    [Fact]
    public void UnbindingTakesOneControlOff()
    {
        BindingProfile profile = _keyboard.Unbind(BindingLayer.Game, "c", _p);

        Assert.Equal([_j], profile.ControlsOf(BindingLayer.Game, "c"));
        Assert.Contains(_p, profile.ControlsOf(BindingLayer.Game, "special"));
    }

    [Fact]
    public void EditsLeaveTheOriginalAsItWas()
    {
        _keyboard.Bind(BindingLayer.Game, "c", _q, replace: true);

        Assert.Equal([_j, _p], _keyboard.ControlsOf(BindingLayer.Game, "c"));
    }

    [Fact]
    public void TheSameBindingsInAnotherOrderAreTheSameLayout()
    {
        BindingProfile reordered = _keyboard.Unbind(BindingLayer.Game, "c", _j).Bind(BindingLayer.Game, "c", _j, replace: false);

        Assert.True(reordered.SameAs(_keyboard));
        Assert.False(reordered.Bind(BindingLayer.Game, "a", _q, replace: false).SameAs(_keyboard));
    }

    [Fact]
    public void KeyboardsHaveNoStickOptions()
    {
        var keyboard = new BindingProfile(InputDeviceKind.Keyboard, [], [], new StickOptions(true, 0.3f, 0.3f));

        Assert.Equal(StickOptions.None, keyboard.Stick);
    }
}
