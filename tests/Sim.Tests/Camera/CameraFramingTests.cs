using Sim.Camera;
using Sim.Collision;
using Sim.Rules;
using Xunit;

namespace Sim.Tests.Camera;

public sealed class CameraFramingTests
{
    [Fact]
    public void TheScreenIsAsWideAsTheMaxPlayerDistanceOnTheRulesStage()
    {
        MatchRules rules = default;
        rules.WallDistance = 600_000;
        rules.MaxPlayerDistance = 640_000;

        CameraFraming framing = CameraFraming.From(in rules);

        Assert.Equal(640_000, framing.Width);
        Assert.Equal(new StageWalls(-600_000, 600_000), framing.Stage);
    }

    [Fact]
    public void AnOddMaxDistanceLosesItsLastSubpixel()
    {
        MatchRules rules = default;
        rules.WallDistance = 600_000;
        rules.MaxPlayerDistance = 640_001;

        Assert.Equal(640_000, CameraFraming.From(in rules).Width);
    }

    [Fact]
    public void TheScreenReachesHalfItsWidthEitherSideOfTheCamera()
    {
        CameraFraming framing = new(100_000, new StageWalls(-300_000, 300_000));

        Assert.Equal(new StageWalls(-50_000, 150_000), framing.ScreenAt(50_000));
    }

    [Theory]
    [InlineData(-20_000, 40_000, 10_000)]
    [InlineData(-300_000, -250_000, -200_000)]
    [InlineData(250_000, 300_000, 200_000)]
    [InlineData(-3, 0, -1)]
    [InlineData(0, 3, 1)]
    public void TheCentreIsKeptFromShowingPastAWall(int left, int right, int expected)
    {
        CameraFraming framing = new(100_000, new StageWalls(-300_000, 300_000));

        Assert.Equal(expected, framing.Centre(left, right));
    }

    [Fact]
    public void AScreenAsWideAsTheStageNeverMoves()
    {
        CameraFraming framing = new(300_000, new StageWalls(-300_000, 300_000));

        Assert.Equal(0, framing.Centre(100_000, 250_000));
    }
}
