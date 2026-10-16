using System;
using Sim.Events;
using Sim.Math;
using Sim.Modules;
using Sim.Pipeline;
using Sim.Rules;
using Sim.State;
using Xunit;
using static Sim.Tests.Entities.ProjectileCharacter;

namespace Sim.Tests.Entities;

/// <summary>Modules spawn entities with the spawn core request (ADR-0011), applied at the end of the frame.</summary>
public sealed class SpawnRequestStageTests
{
    private const int Frame = 12;

    private readonly EntityStages _stages = new(entitiesPerPlayer: 1);

    [Fact]
    public void ASpawnRequestSpawnsTheOwnersProjectileLikeAMoveWould()
    {
        MatchState state = Fighters();
        _stages.Requests.Spawns.Set(CoreRequests.FighterKey(0, 1), new SpawnRequest { Projectile = SturdyProjectile });

        _stages.Tick(ref state);

        EntityState spawned = state.Entities.Slots[EntityPool.FirstSlotOf(1)];
        Assert.Equal(
            (1, SturdyProjectile, new IntVector2(60_000, 50_000), -1, 1),
            (spawned.Owner, spawned.Projectile, spawned.Position, spawned.Facing, spawned.Frame));
        Assert.Equal(FrameEventKind.ProjectileSpawn, state.Events.OfFrame(Frame)[0].Kind);
    }

    [Fact]
    public void RequestsAreClearedOnceApplied()
    {
        MatchState state = Fighters();
        _stages.Requests.Spawns.Set(
            CoreRequests.FighterKey(3, 0), new SpawnRequest { Projectile = FireballProjectile });

        _stages.Tick(ref state);

        Assert.False(_stages.Requests.Spawns.TryGet(CoreRequests.FighterKey(3, 0), out _));
    }

    // Spawn requests outlive the request applier's clear (after the before-state-machine hooks) so the entity step
    // can apply them at the end of the same frame.
    [Fact]
    public void TheRequestApplierLeavesSpawnRequestsForTheEntityStep()
    {
        CoreRequests requests = new();
        requests.Spawns.Set(CoreRequests.FighterKey(0, 0), new SpawnRequest { Projectile = FireballProjectile });
        MatchState state = default;

        new CoreRequestApplier(requests).Apply(ref state);

        Assert.True(requests.Spawns.TryGet(CoreRequests.FighterKey(0, 0), out _));
    }

    // Documented order: module slot by slot; on a full pool the later slot's request is the one refused.
    [Fact]
    public void OnAFullPoolTheLaterModuleSlotsRequestIsRefused()
    {
        MatchState state = Fighters();
        _stages.Requests.Spawns.Set(CoreRequests.FighterKey(2, 0), new SpawnRequest { Projectile = SturdyProjectile });
        _stages.Requests.Spawns.Set(
            CoreRequests.FighterKey(5, 0), new SpawnRequest { Projectile = FireballProjectile });

        _stages.Tick(ref state);

        Assert.Equal(SturdyProjectile, state.Entities.Slots[0].Projectile);
        Assert.Equal(1, EntityStages.Count(in state));
    }

    [Fact]
    public void AProjectileTheCharacterDoesNotHaveIsAModuleBug()
    {
        MatchState state = Fighters();
        _stages.Requests.Spawns.Set(CoreRequests.FighterKey(4, 1), new SpawnRequest { Projectile = 9 });

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() => _stages.Tick(ref state));

        Assert.Contains("slot 4", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void AModuleRequestsThroughItsContext()
    {
        MatchState state = Fighters();
        MatchRules rules = default;
        var context = new ModuleContext(
            in state, ref state.Modules[1], in rules.Modules[1].Config, _stages.Requests, slot: 1);

        context.RequestSpawn(0, new SpawnRequest { Projectile = SturdyProjectile });

        Assert.True(_stages.Requests.Spawns.TryGet(CoreRequests.FighterKey(1, 0), out SpawnRequest request));
        Assert.Equal(SturdyProjectile, request.Projectile);
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
