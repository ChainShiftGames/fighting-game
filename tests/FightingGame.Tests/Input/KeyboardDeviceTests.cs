using FightingGame.Input;
using Godot;
using Sim.Input;
using Xunit;

namespace FightingGame.Tests.Input;

public sealed class KeyboardDeviceTests
{
    private readonly KeyboardBindings _keyboard = TestButtons.KeyboardDefault();
    private readonly FakeDeviceState _devices = new();

    [Fact]
    public void LeftAndRightTogetherSendBothBits()
    {
        _devices.Hold(Key.A, Key.D);

        InputWord raw = Read();

        Assert.Equal(InputWord.Left | InputWord.Right, raw.Bits);
    }

    [Fact]
    public void UpAndDownTogetherSendBothBits()
    {
        _devices.Hold(Key.W, Key.S);

        Assert.Equal(InputWord.Up | InputWord.Down, Read().Bits);
    }

    // The device layer keeps both bits; the logic's input step is the one place SOCD is resolved (ADR-0002).
    [Fact]
    public void TheLogicResolvesLeftAndRightToNeutral()
    {
        _devices.Hold(Key.A, Key.D, Key.U);

        InputWord clean = new InputStep(TestButtons.Repository).Clean(Read());

        Assert.Equal(TestButtons.Repository.Mask("a"), clean.Bits);
    }

    [Fact]
    public void BoundButtonsSetTheirButtonSetBits()
    {
        _devices.Hold(Key.U, Key.L);

        ushort expected = (ushort)(TestButtons.Repository.Mask("a") | TestButtons.Repository.Mask("dash"));
        Assert.Equal(expected, Read().Bits);
    }

    [Fact]
    public void UnboundKeysSendNothing()
    {
        _devices.Hold(Key.Up, Key.Kp4, Key.F1);

        Assert.Equal(InputWord.Neutral, Read());
    }

    private InputWord Read() => new KeyboardDevice(_keyboard, _devices).Read();
}
