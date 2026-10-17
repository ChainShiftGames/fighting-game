using System.Collections.Generic;
using Netcode.Bots;
using Sim.Input;
using Xunit;

namespace Netcode.Tests.Bots;

public sealed class InputPlanTests
{
    private const ushort C = 1 << 6;
    private const ushort Down = InputWord.Down;
    private const ushort Left = InputWord.Left;
    private const ushort Right = InputWord.Right;

    [Fact]
    public void AMotionPlaysEachStepThenTheButtonThenLetsGo()
    {
        var plan = new InputPlan();

        plan.Motion(236, C, facing: 1);

        Assert.Equal([Down, Down | Right, Right | C, 0], Drain(plan));
    }

    [Fact]
    public void FacingLeftMirrorsTheMotion()
    {
        var plan = new InputPlan();

        plan.Motion(236, C, facing: -1);

        Assert.Equal([Down, Down | Left, Left | C, 0], Drain(plan));
    }

    [Fact]
    public void ARepeatedDirectionGetsANeutralFrameBetweenAndAMotionAloneKeepsItsDirection()
    {
        var plan = new InputPlan();

        plan.Motion(66, 0, facing: 1);

        Assert.Equal([Right, 0, Right], Drain(plan));
    }

    [Fact]
    public void ANormalIsJustTheButton()
    {
        var plan = new InputPlan();

        plan.Motion(5, C, facing: 1);

        Assert.Equal([C, 0], Drain(plan));
    }

    [Theory]
    [InlineData(1, 1, Down | Left)]
    [InlineData(3, 1, Down | Right)]
    [InlineData(4, -1, Right)]
    [InlineData(5, 1, 0)]
    [InlineData(9, -1, InputWord.Up | Left)]
    public void NumpadDirectionsBecomeInputBits(int direction, int facing, int bits)
    {
        Assert.Equal(bits, InputPlan.NumpadBits(direction, facing));
    }

    [Fact]
    public void AnEmptyPlanGivesNothing()
    {
        var plan = new InputPlan();

        Assert.True(plan.IsEmpty);
        Assert.False(plan.TryTake(out _));
    }

    private static List<ushort> Drain(InputPlan plan)
    {
        var words = new List<ushort>();
        while (plan.TryTake(out InputWord word))
        {
            words.Add(word.Bits);
        }

        return words;
    }
}
