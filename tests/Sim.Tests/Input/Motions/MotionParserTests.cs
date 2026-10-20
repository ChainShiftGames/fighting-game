using System;
using Sim.Input;
using Sim.Input.Motions;
using Sim.Rules;
using Sim.State;
using Xunit;

namespace Sim.Tests.Input.Motions;

/// <summary>ADR-0002: each motion, the motion window and the button buffer, with the default 12 and 2 frames.</summary>
public sealed class MotionParserTests
{
    private const ushort C = InputSequence.ButtonC;
    private const int NoButton = 0;

    private readonly MotionParser _parser = TestMotions.Parser();

    // Each motion as a player inputs it, one frame per direction, the button pressed on the last direction.
    public static TheoryData<int, string, int> Motions => new()
    {
        { 632146, "632146", 1 }, { 632146, "632146", -1 },
        { 236236, "236236", 1 }, { 236236, "236236", -1 },
        { 63214, "63214", 1 }, { 63214, "63214", -1 },
        { 41236, "41236", 1 }, { 41236, "41236", -1 },
        { 623, "623", 1 }, { 623, "623", -1 },
        { 214, "214", 1 }, { 214, "214", -1 },
        { 236, "236", 1 }, { 236, "236", -1 },
        { 22, "252", 1 }, { 22, "252", -1 },
    };

    [Theory]
    [MemberData(nameof(Motions))]
    public void EachMotionIsInputForEitherFacing(int notation, string directions, int facing)
    {
        InputHistory history = WithPressOnLast(directions, facing);

        Assert.True(IsInput(notation, history, facing, C));
    }

    [Theory]
    [MemberData(nameof(Motions))]
    public void MotionWithoutItsFirstDirectionIsNotInput(int notation, string directions, int facing)
    {
        InputHistory history = WithPressOnLast(directions[1..], facing);

        Assert.False(IsInput(notation, history, facing, C));
    }

    [Fact]
    public void FacingTheOtherWayMirrorsTheMotion()
    {
        // Facing left, 236 on the stick is 214 for the fighter.
        InputHistory history = WithPressOnLast("236", facing: 1);

        Assert.False(IsInput(236, history, -1, C));
        Assert.True(IsInput(214, history, -1, C));
    }

    [Fact]
    public void HeldButtonIsNotAPress()
    {
        InputHistory history = new InputSequence().Hold(5, 1, C).Hold(2, 1, C).Hold(3, 1, C).Hold(6, 1, C).History;

        Assert.False(IsInput(236, history, 1, C));
    }

    [Fact]
    public void MotionMustBeCompleteByThePress()
    {
        InputHistory history = new InputSequence().Directions("2").Press(3, C).Hold(6, 1, C).History;

        Assert.False(IsInput(236, history, 1, C));
    }

    [Theory]
    [InlineData(TestMotions.Window, true)]
    [InlineData(TestMotions.Window + 1, false)]
    public void FirstStepMustLieWithinTheMotionWindow(int framesFromFirstStepToPress, bool expected)
    {
        // 2 and 3 for one frame each, then 6 held until the press: the window counts the 2 and the press frame.
        InputHistory history = new InputSequence()
            .Directions("23").Hold(6, framesFromFirstStepToPress - 3).Press(6, C).History;

        Assert.Equal(expected, IsInput(236, history, 1, C));
    }

    [Theory]
    [InlineData(0, true)]
    [InlineData(1, true)]
    [InlineData(TestMotions.Buffer, true)]
    [InlineData(TestMotions.Buffer + 1, false)]
    public void PressCountsForTheButtonBuffer(int framesSincePress, bool expected)
    {
        InputHistory history = new InputSequence().Directions("23").Press(6, C).Hold(5, framesSincePress, C).History;

        Assert.Equal(expected, IsInput(236, history, 1, C));
    }

    [Theory]
    [InlineData(true, true)]
    [InlineData(false, false)]
    public void MotionSpreadAcrossSkippedFramesStaysWithinTheWindow(bool isSkipped, bool expected)
    {
        // 21 match frames from the 2 to the press, but only 11 of the fighter's own when the 3 is in its hitstop.
        var sequence = new InputSequence().Directions("2").Hold(5, 4);
        sequence = isSkipped ? sequence.Skipped(3, 10) : sequence.Hold(3, 10);
        InputHistory history = sequence.Hold(6, 5).Press(6, C).History;

        Assert.Equal(expected, IsInput(236, history, 1, C));
    }

    [Theory]
    [InlineData(TestMotions.Window, true)]
    [InlineData(TestMotions.Window + 1, false)]
    public void WindowCountsTheOwnFramesAroundSkippedOnes(int ownFramesFromFirstStepToPress, bool expected)
    {
        InputHistory history = new InputSequence()
            .Directions("23").Skipped(6, 5).Hold(6, ownFramesFromFirstStepToPress - 3).Press(6, C).History;

        Assert.Equal(expected, IsInput(236, history, 1, C));
    }

    [Theory]
    [InlineData(TestMotions.Buffer, true)]
    [InlineData(TestMotions.Buffer + 1, false)]
    public void PressAgesOnlyOnTheFightersOwnFrames(int ownFramesSincePress, bool expected)
    {
        InputHistory history = new InputSequence()
            .Directions("23").Press(6, C).Skipped(5, 10, C).Hold(5, ownFramesSincePress, C).History;

        Assert.Equal(expected, IsInput(236, history, 1, C));
        Assert.Equal(expected, _parser.FindFirst(MotionMask.None.With(Index(236)), in history, 1, C) == Index(236));
    }

    [Fact]
    public void DashSurvivesSkippedFrames()
    {
        InputHistory history = new InputSequence().Directions("656").Skipped(6, 8).Hold(6, TestMotions.Buffer).History;

        Assert.True(IsInput(66, history, 1, NoButton));
    }

    [Fact]
    public void BufferIsReadFromTheMatchRules()
    {
        MotionParser parser = new(TestMotions.Default(), TestMotions.Rules(buffer: 3));
        InputHistory history = new InputSequence().Directions("23").Press(6, C).Hold(5, 3, C).History;

        Assert.True(parser.IsInput(TestMotions.IndexOf(parser.Motions, 236), in history, 1, C));
    }

    [Fact]
    public void LenientDragonPunchAcceptsEndingForward()
    {
        // 6236 contains 6, 2, 3 in order, so it completes 623 (and 236; priority picks between them).
        InputHistory history = WithPressOnLast("6236", facing: 1);

        Assert.True(IsInput(623, history, 1, C));
        Assert.True(IsInput(236, history, 1, C));
    }

    [Fact]
    public void DirectionsBetweenStepsAreAllowed()
    {
        InputHistory history = WithPressOnLast("25356", facing: 1);

        Assert.True(IsInput(236, history, 1, C));
    }

    [Theory]
    [InlineData(66, "656", 1)]
    [InlineData(66, "656", -1)]
    [InlineData(44, "454", 1)]
    [InlineData(66, "6456", 1)]
    public void DashMotionsNeedNoButton(int notation, string directions, int facing)
    {
        InputHistory history = new InputSequence(facing).Directions(directions).History;

        Assert.True(IsInput(notation, history, facing, NoButton));
    }

    [Fact]
    public void HoldingForwardIsNotADash()
    {
        InputHistory history = new InputSequence().Hold(6, 6).History;

        Assert.False(IsInput(66, history, 1, NoButton));
    }

    [Theory]
    [InlineData(TestMotions.Buffer, true)]
    [InlineData(TestMotions.Buffer + 1, false)]
    public void DashCountsForTheBufferAfterItsLastDirection(int framesSinceLastDirection, bool expected)
    {
        InputHistory history = new InputSequence().Directions("656").Hold(6, framesSinceLastDirection).History;

        Assert.Equal(expected, IsInput(66, history, 1, NoButton));
    }

    [Fact]
    public void SingleDirectionIsReadOnThePressFrame()
    {
        InputHistory history = new InputSequence().Directions("6").Press(5, C).History;

        Assert.False(IsInput(6, history, 1, C));
        Assert.True(IsInput(5, history, 1, C));
    }

    [Fact]
    public void BufferedPressKeepsTheDirectionOfItsFrame()
    {
        InputHistory history = new InputSequence().Press(2, C).Hold(6, TestMotions.Buffer, C).History;

        Assert.True(IsInput(2, history, 1, C));
        Assert.False(IsInput(6, history, 1, C));
    }

    [Theory]
    [InlineData(0, 2)]
    [InlineData(12, -1)]
    [InlineData(63, 2)]
    public void RejectsTimingTheHistoryCannotHold(int window, int buffer)
    {
        Assert.Throws<ArgumentException>(() => new MotionParser(TestMotions.Default(), TestMotions.Rules(window, buffer)));
    }

    [Fact]
    public void RejectsAMotionWindowTooLongForTheBuffer()
    {
        MotionSet motions = new([TestMotions.Motion(236, "2 3 6", window: InputHistory.Capacity)]);

        Assert.Throws<ArgumentException>(() => new MotionParser(motions, TestMotions.Rules(buffer: 1)));
    }

    [Fact]
    public void MotionOwnWindowOverridesTheMatchWindow()
    {
        MotionSet motions = new([TestMotions.Motion(236, "2 3 6", window: 20)]);
        MotionParser parser = new(motions, TestMotions.Rules());
        InputHistory history = new InputSequence().Directions("23").Hold(6, 17).Press(6, C).History;

        Assert.True(parser.IsInput(0, in history, 1, C));
    }

    [Fact]
    public void AUsedPressTriggersNoMotion()
    {
        InputHistory history = WithPressOnLast("236", facing: 1);
        history.UsePresses();
        history.Record(new InputWord(C | InputWord.Right));

        Assert.False(IsInput(236, history, 1, C));
    }

    [Fact]
    public void AFreshPressCanReuseTheMotionsDirections()
    {
        // 236 with a used press, then the button released and pressed again: the motion's directions still count.
        InputHistory history = WithPressOnLast("236", facing: 1);
        history.UsePresses();
        history.Record(new InputWord(InputWord.Right));
        history.Record(new InputWord(C | InputWord.Right));

        Assert.True(IsInput(236, history, 1, C));
    }

    [Fact]
    public void AUsedDirectionTriggersNoDashMotion()
    {
        InputHistory history = new InputSequence().Directions("656").History;
        history.UsePresses();

        Assert.False(IsInput(66, history, 1, NoButton));
    }

    private static InputHistory WithPressOnLast(string directions, int facing) =>
        new InputSequence(facing).Directions(directions[..^1]).Press(directions[^1] - '0', C).History;

    private bool IsInput(int notation, InputHistory history, int facing, int buttonMask) =>
        _parser.IsInput(Index(notation), in history, facing, buttonMask);

    private int Index(int notation) => TestMotions.IndexOf(_parser.Motions, notation);
}
