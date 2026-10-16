using Sim.Camera;
using Sim.Characters;
using Sim.Collision;
using Sim.Pipeline;
using Sim.State;
using Xunit;
using static Sim.Tests.Collision.BoxedCharacter;

namespace Sim.Tests.Camera;

public sealed class CameraStageTests
{
    private const int Wall = 300_000;
    private const int HalfScreen = 100_000;

    private static readonly CharacterData _character = Create();

    [Fact]
    public void TheCameraCentresOnBothPushboxes()
    {
        MatchState state = Facing(-50_000, 70_000);

        Stage().Run(ref state, FrameActivity.Everyone);

        // Pushboxes span [-70000, 90000].
        Assert.Equal(10_000, state.Camera.X);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(1)]
    public void TheCameraStopsWhereTheScreenWouldShowPastAWall(int side)
    {
        MatchState state = Facing(side * 280_000, side * 200_000);

        Stage().Run(ref state, FrameActivity.Everyone);

        Assert.Equal(side * (Wall - HalfScreen), state.Camera.X);
    }

    [Fact]
    public void FightersThatFitOnScreenAreNotMoved()
    {
        MatchState state = Facing(-80_000, 80_000);
        state.Camera.X = 50_000;

        Stage().Run(ref state, FrameActivity.Everyone);

        Assert.Equal((-80_000, 80_000), (state.Fighters[0].Position.X, state.Fighters[1].Position.X));
        Assert.Equal(0, state.Camera.X);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(1)]
    public void AFighterMovingOffScreenIsHeldAtTheMaxDistanceWithoutPullingTheOther(int side)
    {
        // The screen was at [-100000, 100000]; one fighter stands at its edge, the other moved 4000 past the other edge.
        MatchState state = Facing(side * -84_000, side * 80_000);

        Stage().Run(ref state, FrameActivity.Everyone);

        Assert.Equal(side * -80_000, state.Fighters[0].Position.X);
        Assert.Equal(side * 80_000, state.Fighters[1].Position.X);
        Assert.Equal(0, state.Camera.X);
    }

    [Fact]
    public void BothFightersMovingApartAreBothHeld()
    {
        MatchState state = Facing(-90_000, 90_000);

        Stage().Run(ref state, FrameActivity.Everyone);

        Assert.Equal((-80_000, 80_000), (state.Fighters[0].Position.X, state.Fighters[1].Position.X));
    }

    [Fact]
    public void FrozenFightersAreHeldToo()
    {
        MatchState state = Facing(-84_000, 80_000);

        Stage().Run(ref state, new FrameActivity(0, isFrozen: true));

        Assert.Equal(-80_000, state.Fighters[0].Position.X);
    }

    [Fact]
    public void MirroredFightersGiveAMirroredCamera()
    {
        // An odd span sum, where rounding could favour one side.
        MatchState state = Facing(-50_001, 70_000);
        MatchState mirrored = default;
        mirrored.Fighters[0] = At(-70_000, facing: 1);
        mirrored.Fighters[1] = At(50_001, facing: -1);

        Stage().Run(ref state, FrameActivity.Everyone);
        Stage().Run(ref mirrored, FrameActivity.Everyone);

        Assert.Equal(-state.Camera.X, mirrored.Camera.X);
    }

    private static MatchState Facing(int player1X, int player2X)
    {
        MatchState state = default;
        state.Fighters[0] = At(player1X, facing: player1X < player2X ? 1 : -1);
        state.Fighters[1] = At(player2X, facing: player1X < player2X ? -1 : 1);
        return state;
    }

    private static CameraStage Stage() =>
        new(_character, _character, new CameraFraming(HalfScreen, new StageWalls(-Wall, Wall)));
}
