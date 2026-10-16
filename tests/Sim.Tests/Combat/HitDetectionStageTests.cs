using Sim.Characters;
using Sim.Math;
using Sim.State;
using Xunit;
using static Sim.Tests.Combat.CombatCharacter;

namespace Sim.Tests.Combat;

public sealed class HitDetectionStageTests
{
    private readonly CombatStages _stages = new();

    [Fact]
    public void AnActiveHitboxOnTheOpponentsHurtboxTouchesAtTheCentreOfTheOverlap()
    {
        MatchState state = Exchange(Attacking(-30_000, 1, Mid), At(30_000, -1));

        _stages.Detect(ref state);

        // Hitbox -10000..30000 x 70000..90000 meets hurtbox 5000..55000 x 0..120000.
        Assert.True(_stages.Contacts.Touches(0));
        Assert.Equal(new IntVector2(17_500, 80_000), _stages.Contacts.PointOf(0));
        Assert.False(_stages.Contacts.Touches(1));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(4)]
    public void OutsideTheActiveFramesNothingTouches(int moveFrame)
    {
        MatchState state = Exchange(At(-30_000, 1, Mid, moveFrame), At(30_000, -1));

        _stages.Detect(ref state);

        Assert.False(_stages.Contacts.Touches(0));
    }

    [Fact]
    public void OutOfRangeNothingTouches()
    {
        MatchState state = Exchange(Attacking(-60_000, 1, Mid), At(60_000, -1));

        _stages.Detect(ref state);

        Assert.False(_stages.Contacts.Touches(0));
    }

    [Theory]
    [InlineData(CancelOn.Hit)]
    [InlineData(CancelOn.Block)]
    [InlineData(CancelOn.Clash)]
    public void AMoveThatAlreadyConnectedDoesNotTouchAgain(CancelOn outcome)
    {
        MatchState state = Exchange(Attacking(-30_000, 1, Mid) with { MoveOutcome = outcome }, At(30_000, -1));

        _stages.Detect(ref state);

        Assert.False(_stages.Contacts.Touches(0));
    }

    [Fact]
    public void AWhiffedMoveCanStillTouch()
    {
        MatchState state = Exchange(Attacking(-30_000, 1, Mid) with { MoveOutcome = CancelOn.Whiff }, At(30_000, -1));

        _stages.Detect(ref state);

        Assert.True(_stages.Contacts.Touches(0));
    }

    [Fact]
    public void AnInvulnerableDefenderIsNotTouched()
    {
        MatchState state = Exchange(Attacking(-30_000, 1, Mid), At(30_000, -1, Invulnerable, 5));

        _stages.Detect(ref state);

        Assert.False(_stages.Contacts.Touches(0));
    }

    [Fact]
    public void AMidHitboxPassesOverACrouchingHurtboxThatALowOneTouches()
    {
        MatchState mid = Exchange(Attacking(-30_000, 1, Mid), At(30_000, -1, Crouch));
        MatchState low = Exchange(Attacking(-30_000, 1, Low), At(30_000, -1, Crouch));

        _stages.Detect(ref mid);
        Assert.False(_stages.Contacts.Touches(0));
        _stages.Detect(ref low);
        Assert.True(_stages.Contacts.Touches(0));
    }

    [Fact]
    public void BothAttacksAreCheckedAndMirroredExchangesTouchAtMirroredPoints()
    {
        MatchState state = Exchange(Attacking(-30_000, 1, Mid), Attacking(30_000, -1, Mid));

        _stages.Detect(ref state);

        Assert.True(_stages.Contacts.Touches(0) && _stages.Contacts.Touches(1));
        IntVector2 player1Point = _stages.Contacts.PointOf(0);
        Assert.Equal(new IntVector2(-player1Point.X, player1Point.Y), _stages.Contacts.PointOf(1));
    }

    [Fact]
    public void ContactsFromAnEarlierFrameAreCleared()
    {
        MatchState touching = Exchange(Attacking(-30_000, 1, Mid), At(30_000, -1));
        MatchState apart = Exchange(Attacking(-90_000, 1, Mid), At(90_000, -1));

        _stages.Detect(ref touching);
        _stages.Detect(ref apart);

        Assert.False(_stages.Contacts.Touches(0));
        Assert.Equal(IntVector2.Zero, _stages.Contacts.PointOf(0));
    }

    private static MatchState Exchange(FighterState player1, FighterState player2)
    {
        MatchState state = default;
        state.Fighters[0] = player1;
        state.Fighters[1] = player2;
        return state;
    }
}
