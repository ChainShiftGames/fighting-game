using FightingGame.Input;
using Godot;
using Sim.Input;
using Xunit;

namespace FightingGame.Tests.Input;

public sealed class GamepadDeviceTests
{
    private const int Pad = 3;

    private readonly GamepadBindings _bindings = TestButtons.Defaults()[0].Gamepad;
    private readonly FakeDeviceState _devices = new();

    // A leverless controller in XInput mode reports its direction buttons as the d-pad.
    [Fact]
    public void DpadLeftAndRightTogetherSendBothBits()
    {
        _devices.Hold(Pad, JoyButton.DpadLeft, JoyButton.DpadRight);

        Assert.Equal(InputWord.Left | InputWord.Right, Read().Bits);
    }

    [Fact]
    public void TheLogicResolvesDpadUpAndDownToUp()
    {
        _devices.Hold(Pad, JoyButton.DpadUp, JoyButton.DpadDown);

        Assert.Equal(InputWord.Up, new InputStep(TestButtons.Repository).Clean(Read()).Bits);
    }

    [Fact]
    public void BoundButtonsSetTheirButtonSetBits()
    {
        _devices.Hold(Pad, JoyButton.X, JoyButton.RightShoulder);

        ushort expected = (ushort)(TestButtons.Repository.Mask("a") | TestButtons.Repository.Mask("special"));
        Assert.Equal(expected, Read().Bits);
    }

    [Theory]
    [InlineData(0.4f, false)]
    [InlineData(0.6f, true)]
    public void ATriggerCountsAsHeldPastTheThreshold(float travel, bool held)
    {
        _devices.Move(Pad, JoyAxis.TriggerRight, travel);

        Assert.Equal(held ? TestButtons.Repository.Mask("dash") : 0, Read().Bits);
    }

    [Theory]
    [InlineData(-0.4f, 0)]
    [InlineData(-0.6f, InputWord.Left)]
    [InlineData(0.6f, InputWord.Right)]
    public void TheStickCountsPastTheDeadzone(float stickX, ushort expected)
    {
        _devices.Move(Pad, JoyAxis.LeftX, stickX);

        Assert.Equal(expected, Read().Bits);
    }

    [Fact]
    public void TheStickIsIgnoredWhenNotUsed()
    {
        _devices.Move(Pad, JoyAxis.LeftX, -1).Move(Pad, JoyAxis.LeftY, 1);

        InputWord raw = new GamepadDevice(_bindings with { LeftStick = false }, _devices).Read(Pad);

        Assert.Equal(InputWord.Neutral, raw);
    }

    [Fact]
    public void AnotherJoypadsButtonsAreNotRead()
    {
        _devices.Hold(Pad + 1, JoyButton.X, JoyButton.DpadLeft);

        Assert.Equal(InputWord.Neutral, Read());
    }

    private InputWord Read() => new GamepadDevice(_bindings, _devices).Read(Pad);
}
