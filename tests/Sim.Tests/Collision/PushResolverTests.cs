using Sim.Characters;
using Sim.Collision;
using Sim.State;
using Xunit;
using static Sim.Tests.Collision.BoxedCharacter;

namespace Sim.Tests.Collision;

public sealed class PushResolverTests
{
    private const int Wall = 300_000;

    private static readonly CharacterData _character = Create();
    private static readonly StageWalls _walls = new(-Wall, Wall);

    [Fact]
    public void OverlappingFightersArePushedApartByHalfTheOverlapEach()
    {
        (FighterState left, FighterState right) = Resolve(At(-15_000, facing: 1), At(15_000, facing: -1));

        Assert.Equal(-PushHalfWidth, left.Position.X);
        Assert.Equal(PushHalfWidth, right.Position.X);
    }

    [Fact]
    public void AnOddOverlapMovesBothFightersTheSameDistance()
    {
        (FighterState left, FighterState right) = Resolve(At(-15_000, facing: 1), At(15_001, facing: -1));

        Assert.Equal(-15_000 - 5000, left.Position.X);
        Assert.Equal(15_001 + 5000, right.Position.X);
    }

    [Fact]
    public void SwappingPortsGivesTheSameResult()
    {
        (FighterState left, FighterState right) = Resolve(At(-5000, facing: 1), At(7000, facing: -1));
        (FighterState swappedRight, FighterState swappedLeft) = Resolve(At(7000, facing: -1), At(-5000, facing: 1));

        Assert.Equal(left.Position, swappedLeft.Position);
        Assert.Equal(right.Position, swappedRight.Position);
    }

    [Fact]
    public void MirroredFightersArePushedMirrored()
    {
        (FighterState left, FighterState right) = Resolve(At(Wall - 50_000, facing: 1), At(Wall - 25_000, facing: -1));
        (FighterState mirroredRight, FighterState mirroredLeft) =
            Resolve(At(-Wall + 50_000, facing: -1), At(-Wall + 25_000, facing: 1));

        Assert.Equal(left.Position.MirrorX(), mirroredRight.Position);
        Assert.Equal(right.Position.MirrorX(), mirroredLeft.Position);
    }

    [Fact]
    public void FightersAreKeptInsideTheWalls()
    {
        (FighterState left, FighterState right) = Resolve(At(-Wall, facing: 1), At(Wall + 5000, facing: -1));

        Assert.Equal(-Wall + PushHalfWidth, left.Position.X);
        Assert.Equal(Wall - PushHalfWidth, right.Position.X);
    }

    [Fact]
    public void ACorneredFighterStaysAndTheOtherTakesThePush()
    {
        int cornered = Wall - PushHalfWidth;

        (FighterState left, FighterState right) = Resolve(At(cornered - 30_000, facing: 1), At(cornered, facing: -1));

        Assert.Equal(cornered, right.Position.X);
        Assert.Equal(cornered - (2 * PushHalfWidth), left.Position.X);
    }

    [Fact]
    public void FightersAtTheSameXAreSeparatedByTheSideTheyFace()
    {
        int cornered = Wall - PushHalfWidth;

        (FighterState jumper, FighterState defender) =
            Resolve(At(cornered, facing: 1), At(cornered, facing: -1));

        Assert.Equal(cornered, defender.Position.X);
        Assert.Equal(cornered - (2 * PushHalfWidth), jumper.Position.X);
    }

    [Fact]
    public void FightersAtTheSameXFacingTheSameWayAreNotPushed()
    {
        (FighterState first, FighterState second) = Resolve(At(0, facing: 1), At(0, facing: 1));

        Assert.Equal(0, first.Position.X);
        Assert.Equal(0, second.Position.X);
    }

    [Fact]
    public void AFighterAboveThePushboxIsNotPushed()
    {
        (FighterState jumper, FighterState standing) =
            Resolve(At(-5000, facing: 1, y: PushHeight), At(5000, facing: -1));

        Assert.Equal(-5000, jumper.Position.X);
        Assert.Equal(5000, standing.Position.X);
    }

    [Fact]
    public void AFighterWithoutPushboxPassesThrough()
    {
        (FighterState passing, FighterState standing) = Resolve(At(-5000, facing: 1, PassThrough), At(5000, facing: -1));

        Assert.Equal(-5000, passing.Position.X);
        Assert.Equal(5000, standing.Position.X);
    }

    private static (FighterState First, FighterState Second) Resolve(FighterState first, FighterState second)
    {
        PushResolver.Resolve(
            ref first, Pushbox.Of(_character, in first), ref second, Pushbox.Of(_character, in second), in _walls);
        return (first, second);
    }
}
