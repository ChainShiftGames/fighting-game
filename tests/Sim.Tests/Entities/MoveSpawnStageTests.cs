using Sim.Pipeline;
using Sim.State;
using Xunit;
using static Sim.Tests.Entities.ProjectileCharacter;

namespace Sim.Tests.Entities;

public sealed class MoveSpawnStageTests
{
    private const int Frame = 30;
    private const int Player1Slot = 0;
    private static readonly int _player2Slot = EntityPool.FirstSlotOf(1);

    private readonly EntityStages _stages = new();

    [Fact]
    public void AMoveSpawnsItsProjectileOnItsSpawnFrameOnly()
    {
        for (int moveFrame = 1; moveFrame <= 3; moveFrame++)
        {
            MatchState state = Fighters();
            state.Fighters[0] = At(-100_000, 1, Fireball, moveFrame);

            _stages.Move(ref state, FrameActivity.Everyone);

            Assert.Equal(moveFrame == SpawnFrame ? 1 : 0, EntityStages.Count(in state));
        }
    }

    [Fact]
    public void AnOwnerThatDoesNotAdvanceDoesNotSpawnAgain()
    {
        MatchState state = Fighters();
        state.Fighters[1] = At(100_000, -1, Fireball, SpawnFrame);

        _stages.Move(ref state, new FrameActivity(advancingFighters: 1, isFrozen: false));

        Assert.Equal(0, EntityStages.Count(in state));
    }

    [Fact]
    public void ANewProjectileDoesNotMoveOnItsSpawnFrame()
    {
        MatchState state = Fighters();
        state.Fighters[1] = At(100_000, -1, Fireball, SpawnFrame);

        _stages.Move(ref state, FrameActivity.Everyone);

        EntityState spawned = state.Entities.Slots[_player2Slot];
        Assert.Equal((60_000, 1), (spawned.Position.X, spawned.Frame));
    }

    [Fact]
    public void AProjectileRunningOutMakesRoomForOneSpawnedTheSameFrame()
    {
        MatchState state = Fighters();
        EntityStages stages = new(entitiesPerPlayer: 1);
        state.Entities.Slots[Player1Slot] = Entity(0, -50_000, frame: FireballLifetime);
        state.Fighters[0] = At(-100_000, 1, Sturdy, SpawnFrame);

        stages.Move(ref state, FrameActivity.Everyone);

        EntityState spawned = state.Entities.Slots[Player1Slot];
        Assert.Equal((1, SturdyProjectile), (spawned.Frame, spawned.Projectile));
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
