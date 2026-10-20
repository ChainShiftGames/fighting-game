using Sim.Input.Motions;
using Sim.State;
using Xunit;

namespace Sim.Tests.Input.Motions;

/// <summary>The button buffer counts the fighter's own frames: frames it skips (hitstop, slowdown) don't age a press.</summary>
public sealed class ButtonPressTests
{
    private const ushort A = InputSequence.ButtonA;
    private const int Buffer = TestMotions.Buffer;

    [Theory]
    [InlineData(Buffer, true)]
    [InlineData(Buffer + 1, false)]
    public void PressExpiresAfterTheBufferOnAdvancedFrames(int framesSincePress, bool expected)
    {
        InputHistory history = new InputSequence().Press(5, A).Hold(5, framesSincePress).History;

        Assert.Equal(expected, ButtonPress.IsPressedWithin(in history, A, Buffer));
    }

    [Fact]
    public void SkippedFramesDoNotAgeAPress()
    {
        // Pressed, then a long hitstop, then the fighter's first own frame.
        InputHistory history = new InputSequence().Press(5, A).Skipped(5, 12).Hold(5, 1).History;

        Assert.True(ButtonPress.IsPressedWithin(in history, A, Buffer));
    }

    [Theory]
    [InlineData(Buffer, true)]
    [InlineData(Buffer + 1, false)]
    public void PressMadeOnASkippedFrameAgesFromTheFightersNextOwnFrame(int ownFramesSinceTheFirst, bool expected)
    {
        // Pressed on the second of 10 hitstop frames (released at once). On the fighter's first own frame after the
        // hitstop the press is as fresh as one made on that frame, then it ages one per own frame.
        InputHistory history = new InputSequence()
            .Hold(5, 3).Skipped(5, 1).Skipped(5, 1, A).Skipped(5, 8).Hold(5, ownFramesSinceTheFirst + 1).History;

        Assert.Equal(expected, ButtonPress.IsPressedWithin(in history, A, Buffer));
    }

    [Fact]
    public void ZeroBufferCoversEveryFrameSinceTheFighterLastAdvanced()
    {
        InputHistory history = new InputSequence().Hold(5, 2).Skipped(5, 1, A).Skipped(5, 3).Hold(5, 1).History;

        Assert.True(ButtonPress.IsPressedWithin(in history, A, buffer: 0));
        Assert.False(ButtonPress.IsPressedAt(in history, A, framesAgo: 0));
    }

    [Fact]
    public void ZeroBufferWithoutSkippedFramesIsThisFrameOnly()
    {
        InputHistory history = new InputSequence().Press(5, A).Hold(5, 1).History;

        Assert.False(ButtonPress.IsPressedWithin(in history, A, buffer: 0));
    }
}
