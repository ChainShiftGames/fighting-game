using System;
using Netcode.Drivers;
using Netcode.Replays;
using Sim.Input;
using Xunit;

namespace Netcode.Tests.Replays;

public sealed class ReplayRecorderTests
{
    private static readonly InputWord _left = new(InputWord.Left);
    private static readonly InputWord _right = new(InputWord.Right);

    [Fact]
    public void RecordsFramesInOrder()
    {
        var recorder = new ReplayRecorder(RecordedMatches.Header());

        recorder.Record(0, _left, _right);
        recorder.Record(1, _right, _left);

        Assert.Equal([new FrameInputs(_left, _right), new FrameInputs(_right, _left)], recorder.ToReplay().Inputs);
    }

    [Fact]
    public void ARolledBackFrameReplacesItsInputsAndDropsTheFramesAfterIt()
    {
        var recorder = new ReplayRecorder(RecordedMatches.Header());
        for (int frame = 0; frame < 5; frame++)
        {
            recorder.Record(frame, _left, _left);
        }

        // Rollback to frame 2: the other player's real input differs from the prediction.
        recorder.Record(2, _left, _right);

        Assert.Equal(3, recorder.FrameCount);
        Assert.Equal(new FrameInputs(_left, _right), recorder.ToReplay().Inputs[2]);
    }

    [Fact]
    public void RefusesToSkipAFrame()
    {
        var recorder = new ReplayRecorder(RecordedMatches.Header());
        recorder.Record(0, _left, _left);

        Assert.Throws<ArgumentOutOfRangeException>(() => recorder.Record(2, _left, _left));
    }

    [Fact]
    public void AReplayTakenMidMatchIsNotChangedByLaterFrames()
    {
        var recorder = new ReplayRecorder(RecordedMatches.Header());
        recorder.Record(0, _left, _left);
        Replay replay = recorder.ToReplay();

        recorder.Record(0, _right, _right);

        Assert.Equal(new FrameInputs(_left, _left), replay.Inputs[0]);
    }
}
