using System;
using Sim.Entities;
using Sim.Events;
using Sim.Math;
using Sim.Rules;
using Sim.State;
using Xunit;
using static Sim.Tests.Entities.ProjectileCharacter;

namespace Sim.Tests.Entities;

public sealed class ProjectileSpawnerTests
{
    private const int Frame = 20;

    [Fact]
    public void AProjectileAppearsAtTheOwnersPositionPlusItsOffsetAndFliesItsWay()
    {
        MatchState state = Fighters();

        Assert.True(Spawner().TrySpawn(ref state, 0, FireballProjectile));
        Assert.True(Spawner().TrySpawn(ref state, 1, FireballProjectile));

        EntityState player1 = state.Entities.Slots[EntityPool.FirstSlotOf(0)];
        EntityState player2 = state.Entities.Slots[EntityPool.FirstSlotOf(1)];
        Assert.Equal(
            (1, 0, 1, new IntVector2(-60_000, 50_000), new IntVector2(FireballSpeed, 0), 1, 1),
            (player1.IsActive, player1.Owner, player1.Frame, player1.Position, player1.Velocity, player1.Facing,
                player1.Durability));
        Assert.Equal(
            (1, new IntVector2(60_000, 50_000), new IntVector2(-FireballSpeed, 0), -1),
            (player2.Owner, player2.Position, player2.Velocity, player2.Facing));
    }

    [Fact]
    public void ASpawnEmitsAnEventWithTheProjectile()
    {
        MatchState state = Fighters();

        Spawner().TrySpawn(ref state, 1, SturdyProjectile);

        FrameEvent spawn = Assert.Single(state.Events.OfFrame(Frame).ToArray());
        Assert.Equal(
            (FrameEventKind.ProjectileSpawn, 1, new IntVector2(60_000, 50_000), SturdyProjectile),
            (spawn.Kind, spawn.Fighter, spawn.Position, spawn.Detail));
        Assert.Equal(SturdyDurability, state.Entities.Slots[EntityPool.FirstSlotOf(1)].Durability);
    }

    [Fact]
    public void ASpawnTakesTheOwnersLowestFreeSlot()
    {
        MatchState state = Fighters();
        ProjectileSpawner spawner = Spawner();
        spawner.TrySpawn(ref state, 0, FireballProjectile);
        spawner.TrySpawn(ref state, 0, FireballProjectile);
        state.Entities.Slots[0] = default;

        spawner.TrySpawn(ref state, 0, SturdyProjectile);

        Assert.Equal(SturdyProjectile, state.Entities.Slots[0].Projectile);
        Assert.Equal(1, state.Entities.Slots[1].IsActive);
    }

    // Documented pool rule: when the owner's slots are full the new spawn is refused; nothing already out changes.
    [Fact]
    public void WhenTheOwnersSlotsAreFullTheNewSpawnIsRefusedAndNothingChanges()
    {
        MatchState state = Fighters();
        ProjectileSpawner spawner = Spawner(entitiesPerPlayer: 2);
        spawner.TrySpawn(ref state, 0, FireballProjectile);
        spawner.TrySpawn(ref state, 0, FireballProjectile);
        state.Frame++;
        MatchState full = state;

        Assert.False(spawner.TrySpawn(ref state, 0, SturdyProjectile));

        Assert.Equal(StateChecksum.Compute(in full), StateChecksum.Compute(in state));
        Assert.Equal(2, EntityStages.Count(in state));
        Assert.True(state.Events.OfFrame(state.Frame).IsEmpty);
    }

    [Fact]
    public void OnePlayersFullSlotsNeverBlockTheOther()
    {
        MatchState state = Fighters();
        ProjectileSpawner spawner = Spawner(entitiesPerPlayer: 1);
        spawner.TrySpawn(ref state, 0, FireballProjectile);

        Assert.False(spawner.TrySpawn(ref state, 0, FireballProjectile));
        Assert.True(spawner.TrySpawn(ref state, 1, FireballProjectile));
    }

    [Fact]
    public void SlotsBeyondTheRulesAreNeverUsed()
    {
        MatchState state = Fighters();
        ProjectileSpawner spawner = Spawner(entitiesPerPlayer: 3);
        for (int i = 0; i < EntityPool.MaxPerPlayer; i++)
        {
            spawner.TrySpawn(ref state, 1, FireballProjectile);
        }

        Assert.Equal(3, EntityStages.Count(in state));
        Assert.Equal(0, state.Entities.Slots[EntityPool.FirstSlotOf(1) + 3].IsActive);
    }

    [Fact]
    public void RulesWithoutEntitySlotsRefuseEverySpawn()
    {
        MatchState state = Fighters();

        Assert.False(Spawner(entitiesPerPlayer: 0).TrySpawn(ref state, 0, FireballProjectile));
    }

    [Fact]
    public void RulesAllowingMoreEntitiesThanTheStateHoldsAreRejected()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Spawner(entitiesPerPlayer: EntityPool.MaxPerPlayer + 1));
    }

    private static ProjectileSpawner Spawner(int entitiesPerPlayer = 4)
    {
        MatchRules rules = default;
        rules.EntitiesPerPlayer = entitiesPerPlayer;
        var character = Create();
        return new ProjectileSpawner(new OwnerCharacters(character, character), in rules);
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
