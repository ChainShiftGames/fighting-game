using FightingGame.Presentation.World;
using Xunit;

namespace FightingGame.Tests.Presentation.World;

public sealed class MoveEntryTrackerTests
{
    [Fact]
    public void RemembersTheMoveBeforeAndCountsFromTheFrameTheMoveBegan()
    {
        var tracker = new MoveEntryTracker();
        tracker.Observe("idle", 100);
        tracker.Observe("crouch", 140);
        tracker.Observe("crouch", 143);

        Assert.Equal("idle", tracker.CameFrom);
        Assert.Equal(4, tracker.FramesInMove(143));
    }

    [Fact]
    public void KeepsCountingWhileTheMovesOwnFrameRepeats()
    {
        // Crouch is one frame long and repeats: the match clock still moves on.
        var tracker = new MoveEntryTracker();
        tracker.Observe("crouch", 10);
        tracker.Observe("crouch", 30);

        Assert.Equal(21, tracker.FramesInMove(30));
    }

    [Fact]
    public void ForgetsWhereTheMoveCameFromWhenTheMatchGoesBack()
    {
        var tracker = new MoveEntryTracker();
        tracker.Observe("idle", 100);
        tracker.Observe("crouch", 140);
        tracker.Observe("crouch", 120);

        Assert.Null(tracker.CameFrom);
        Assert.Equal(1, tracker.FramesInMove(120));
    }
}
