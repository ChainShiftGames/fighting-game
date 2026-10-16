using System;
using Sim.Input.Motions;
using Sim.State;
using Xunit;

namespace Sim.Tests.Input.Motions;

public sealed class MotionSetTests
{
    [Fact]
    public void KeepsDataOrderAsPriority()
    {
        MotionSet motions = TestMotions.Default();

        Assert.Equal(632146, motions.NotationOf(0));
        Assert.True(motions.TryFind(623, out int dragonPunch));
        Assert.True(motions.TryFind(236, out int fireball));
        Assert.True(dragonPunch < fireball);
    }

    [Fact]
    public void UnknownNotationIsNotFound()
    {
        Assert.False(TestMotions.Default().TryFind(412, out int motion));
        Assert.Equal(MotionSet.NoMotion, motion);
    }

    [Fact]
    public void StoresStepsAndWindow()
    {
        MotionSet motions = new([TestMotions.Motion(22, "2 5 2"), TestMotions.Motion(2, "123", window: 1)]);

        Assert.Equal([MotionStep.Of(2), MotionStep.Of(5), MotionStep.Of(2)], motions.StepsOf(0).ToArray());
        Assert.Equal(MotionDefinition.UsesMatchWindow, motions.WindowOf(0));
        Assert.Equal(MotionStep.Of(1, 2, 3), motions.StepsOf(1)[0]);
        Assert.Equal(1, motions.WindowOf(1));
    }

    [Fact]
    public void RejectsAnEmptySet()
    {
        Assert.Throws<ArgumentException>(() => new MotionSet([]));
    }

    [Fact]
    public void RejectsMoreMotionsThanAMaskHolds()
    {
        MotionDefinition[] motions = new MotionDefinition[MotionSet.MaxMotions + 1];
        for (int motion = 0; motion < motions.Length; motion++)
        {
            motions[motion] = TestMotions.Motion(1000 + motion, "2 3 6");
        }

        Assert.Throws<ArgumentException>(() => new MotionSet(motions));
    }

    [Fact]
    public void RejectsARepeatedNotation()
    {
        Assert.Throws<ArgumentException>(() =>
            new MotionSet([TestMotions.Motion(236, "2 3 6"), TestMotions.Motion(236, "1 2 3 6")]));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-236)]
    public void RejectsANotationThatIsNotPositive(int notation)
    {
        Assert.Throws<ArgumentException>(() => new MotionSet([TestMotions.Motion(notation, "2 3 6")]));
    }

    [Fact]
    public void RejectsAMotionWithoutSteps()
    {
        Assert.Throws<ArgumentException>(() => new MotionSet([new MotionDefinition(236, [])]));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(1 << 10)]
    public void RejectsAStepOutsideTheNumpad(int directions)
    {
        Assert.Throws<ArgumentException>(() => new MotionSet([new MotionDefinition(5, [new MotionStep(directions)])]));
    }

    [Theory]
    [InlineData("2 2")]
    [InlineData("123 3 6")]
    public void RejectsConsecutiveStepsSharingADirection(string steps)
    {
        Assert.Throws<ArgumentException>(() => new MotionSet([TestMotions.Motion(22, steps)]));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(InputHistory.Capacity + 1)]
    public void RejectsAWindowOutsideTheHistory(int window)
    {
        Assert.Throws<ArgumentException>(() => new MotionSet([TestMotions.Motion(236, "2 3 6", window)]));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(10)]
    public void StepRejectsANonNumpadDirection(int direction)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => MotionStep.Of(direction));
    }
}
