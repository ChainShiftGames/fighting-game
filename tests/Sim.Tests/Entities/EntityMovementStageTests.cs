using Sim.Events;
using Sim.Math;
using Sim.Pipeline;
using Sim.State;
using Xunit;
using static Sim.Tests.Entities.ProjectileCharacter;

namespace Sim.Tests.Entities;

public sealed class EntityMovementStageTests
{
    private const int Frame = 30;
    private const int Player1Slot = 0;
    private static readonly int _player2Slot = EntityPool.FirstSlotOf(1);

    private readonly EntityStages _stages = new();

    [Fact]
    public void EntitiesFlyByTheirVelocityAndAgeAFrame()
    {
        MatchState state = Fighters();
        state.Entities.Slots[Player1Slot] = Entity(0, -50_000);
        state.Entities.Slots[_player2Slot] = Entity(1, 50_000, SturdyProjectile);

        _stages.Move(ref state, FrameActivity.Everyone);

        EntityState player1 = state.Entities.Slots[Player1Slot];
        Assert.Equal((-42_000, 2), (player1.Position.X, player1.Frame));
        Assert.Equal(48_000, state.Entities.Slots[_player2Slot].Position.X);
    }

    [Fact]
    public void AnEntityExpiresOnTheFrameAfterItsLifetimeAtItsLastPosition()
    {
        MatchState state = Fighters();
        state.Entities.Slots[Player1Slot] = Entity(0, -50_000, frame: FireballLifetime - 1);

        _stages.Move(ref state, FrameActivity.Everyone);
        Assert.Equal(1, state.Entities.Slots[Player1Slot].IsActive);

        state.Frame++;
        _stages.Move(ref state, FrameActivity.Everyone);

        Assert.Equal(default, state.Entities.Slots[Player1Slot]);
        FrameEvent expire = Assert.Single(state.Events.OfFrame(state.Frame).ToArray());
        Assert.Equal(
            (FrameEventKind.ProjectileExpire, 0, new IntVector2(-42_000, Offset.Y), FireballProjectile),
            (expire.Kind, expire.Fighter, expire.Position, expire.Detail));
    }

    [Fact]
    public void AGlobalFreezeStopsEntitiesUnlessTheirOwnerKeepsRunning()
    {
        MatchState state = Fighters();
        state.Entities.Slots[Player1Slot] = Entity(0, -50_000);
        state.Entities.Slots[_player2Slot] = Entity(1, 50_000);

        _stages.Move(ref state, new FrameActivity(advancingFighters: 1, isFrozen: true));

        Assert.Equal(-42_000, state.Entities.Slots[Player1Slot].Position.X);
        EntityState player2 = state.Entities.Slots[_player2Slot];
        Assert.Equal((50_000, 1), (player2.Position.X, player2.Frame));
    }

    [Fact]
    public void EntitiesKeepFlyingThroughTheirOwnersHitstop()
    {
        MatchState state = Fighters();
        state.Entities.Slots[Player1Slot] = Entity(0, -50_000);

        _stages.Move(ref state, new FrameActivity(advancingFighters: 0, isFrozen: false));

        Assert.Equal(-42_000, state.Entities.Slots[Player1Slot].Position.X);
    }

    private static MatchState Fighters()
    {
        MatchState state = default;
        state.Frame = Frame;
        state.Fighters[0] = At(-100_000, 1);
        state.Fighters[1] = At(100_000, -1);
        return state;
    }
}
