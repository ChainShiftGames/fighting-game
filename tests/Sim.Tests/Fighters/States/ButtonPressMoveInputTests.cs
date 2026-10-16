using Sim.Fighters.States;
using Sim.Input;
using Sim.State;
using Xunit;

namespace Sim.Tests.Fighters.States;

public sealed class ButtonPressMoveInputTests
{
    private const ushort DashButton = 1 << 9;

    private readonly ButtonPressMoveInput _input = new(DashButton);

    [Theory]
    [InlineData(DashButton, true, false)]
    [InlineData(DashButton | InputWord.Right, true, false)]
    [InlineData(DashButton | InputWord.Left, false, true)]
    [InlineData(InputWord.Left, false, false)]
    public void TheDashButtonDashesOrWithBackBackdashes(ushort input, bool isDash, bool isBackdash)
    {
        InputHistory history = History(0, input);

        Assert.Equal(isDash, _input.IsDashTriggered(in history, facing: 1));
        Assert.Equal(isBackdash, _input.IsBackdashTriggered(in history, facing: 1));
    }

    [Fact]
    public void HoldingTheDashButtonDashesOnlyOnce()
    {
        InputHistory history = History(DashButton, DashButton);

        Assert.False(_input.IsDashTriggered(in history, facing: 1));
    }

    [Fact]
    public void WithoutADashButtonNothingDashes()
    {
        InputHistory history = History(0, DashButton);

        Assert.False(new ButtonPressMoveInput().IsDashTriggered(in history, facing: 1));
    }

    [Theory]
    [InlineData(InputWord.Down, InputWord.Up, true)]
    [InlineData(InputWord.Down | InputWord.Left, InputWord.Up | InputWord.Right, true)]
    [InlineData(0, InputWord.Up, false)]
    [InlineData(InputWord.Up, InputWord.Up, false)]
    public void UpStraightAfterDownIsAHighJump(ushort previous, ushort now, bool isHighJump)
    {
        InputHistory history = History(previous, now);

        Assert.Equal(isHighJump, _input.IsHighJumpTriggered(in history, facing: 1));
    }

    [Theory]
    [InlineData(0, InputWord.Up, true)]
    [InlineData(InputWord.Up, InputWord.Up, false)]
    [InlineData(InputWord.Right, InputWord.Up | InputWord.Right, true)]
    public void AFreshUpPressIsAnAirJump(ushort previous, ushort now, bool isAirJump)
    {
        InputHistory history = History(previous, now);

        Assert.Equal(isAirJump, _input.IsAirJumpTriggered(in history, facing: 1));
    }

    private static InputHistory History(ushort previous, ushort now)
    {
        InputHistory history = default;
        history.Record(new InputWord(previous));
        history.Record(new InputWord(now));
        return history;
    }
}
