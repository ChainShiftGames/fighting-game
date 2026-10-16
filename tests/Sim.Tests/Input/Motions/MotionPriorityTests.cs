using Sim.Input.Motions;
using Sim.State;
using Xunit;

namespace Sim.Tests.Input.Motions;

/// <summary>When several motions are input on the same frame, the first one in the motion set wins.</summary>
public sealed class MotionPriorityTests
{
    private const ushort C = InputSequence.ButtonC;

    private readonly MotionParser _parser = TestMotions.Parser();

    [Theory]
    [InlineData("6236", new[] { 236, 623 }, 623)]
    [InlineData("6236", new[] { 236 }, 236)]
    [InlineData("41236", new[] { 236, 41236 }, 41236)]
    [InlineData("236236", new[] { 236, 236236 }, 236236)]
    [InlineData("632146", new[] { 214, 63214, 632146 }, 632146)]
    [InlineData("63214", new[] { 214, 63214 }, 63214)]
    [InlineData("252", new[] { 2, 22 }, 22)]
    [InlineData("236", new[] { 5, 6, 236 }, 236)]
    [InlineData("6", new[] { 5, 6, 236 }, 6)]
    [InlineData("3", new[] { 2, 3 }, 3)]
    [InlineData("1", new[] { 2 }, 2)]
    [InlineData("9", new[] { 5 }, 5)]
    public void FirstMotionInTheSetWins(string directions, int[] candidates, int expected)
    {
        InputHistory history = new InputSequence().Directions(directions[..^1]).Press(directions[^1] - '0', C).History;

        int found = _parser.FindFirst(Candidates(candidates), in history, 1, C);

        Assert.Equal(TestMotions.IndexOf(_parser.Motions, expected), found);
    }

    [Fact]
    public void NothingWithoutAPress()
    {
        InputHistory history = new InputSequence().Directions("236").History;

        Assert.Equal(MotionSet.NoMotion, _parser.FindFirst(Candidates(236, 5), in history, 1, C));
    }

    [Fact]
    public void NothingWhenNoCandidateWasInput()
    {
        InputHistory history = new InputSequence().Directions("23").Press(6, C).History;

        Assert.Equal(MotionSet.NoMotion, _parser.FindFirst(Candidates(214, 623), in history, 1, C));
    }

    private MotionMask Candidates(params int[] notations)
    {
        MotionMask mask = MotionMask.None;
        foreach (int notation in notations)
        {
            mask = mask.With(TestMotions.IndexOf(_parser.Motions, notation));
        }

        return mask;
    }
}
