using FightingGame.Presentation.World;
using Xunit;

namespace FightingGame.Tests.Presentation.World;

public sealed class FacingTurnTests
{
    [Fact]
    public void SwingsTowardTheCameraThenSwapsSidesAndSwingsBack()
    {
        var turn = new FacingTurn();
        turn.Observe(1, 100);
        turn.Observe(-1, 101);

        // 4 frames: a fifth of the way per frame, so no frame shows the model edge-on.
        Assert.Equal((1, -36f), Rounded(turn.PoseAt(101, frames: 4)));
        Assert.Equal((1, -72f), Rounded(turn.PoseAt(102, frames: 4)));
        Assert.Equal((-1, -72f), Rounded(turn.PoseAt(103, frames: 4)));
        Assert.Equal((-1, -36f), Rounded(turn.PoseAt(104, frames: 4)));
        Assert.Equal((-1, 0f), Rounded(turn.PoseAt(105, frames: 4)));
    }

    [Fact]
    public void MirrorsAtOnceWithoutTurnFramesOrAtTheStart()
    {
        var turn = new FacingTurn();
        turn.Observe(-1, 1);
        Assert.Equal((-1, 0f), turn.PoseAt(1, frames: 4));

        turn.Observe(1, 2);
        Assert.Equal((1, 0f), turn.PoseAt(2, frames: 0));
    }

    private static (int, float) Rounded((int Facing, float Yaw) pose) => (pose.Facing, (float)System.Math.Round(pose.Yaw));
}
