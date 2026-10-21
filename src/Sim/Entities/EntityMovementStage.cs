using Sim.Events;
using Sim.Pipeline;
using Sim.State;

namespace Sim.Entities;

/// <summary>The entity part of the movement step: entities age a frame, then expire or fly on.</summary>
/// <remarks>
/// Runs after the fighters move and before new projectiles spawn (<see cref="MoveSpawnStage"/>), so a projectile
/// that runs out this frame makes room for one spawned this frame, and a new one doesn't move on its spawn frame.
/// An entity whose frame passes its lifetime disappears before moving (<see cref="FrameEventKind.ProjectileExpire"/>
/// at its last position). Time: entities stop in a global freeze unless their owner keeps running (an exempt owner
/// that advances this frame); a rehit cooldown counts down only on frames the entity advances. They don't share their owner's hitstop or time scale. Entities don't stop at walls;
/// their lifetime ends them. Each entity reads only itself and its data, so slot order can't matter.
/// </remarks>
public sealed class EntityMovementStage : ITickStage
{
    private readonly OwnerCharacters _characters;

    public EntityMovementStage(OwnerCharacters characters)
    {
        _characters = characters;
    }

    public void Run(ref MatchState state, in FrameActivity activity)
    {
        for (int slot = 0; slot < EntityPool.Capacity; slot++)
        {
            EntityState entity = state.Entities.Slots[slot];
            if (entity.IsActive != 0 && (!activity.IsFrozen || activity.Advances(entity.Owner)))
            {
                state.Entities.Slots[slot] = Advance(ref state, entity);
            }
        }
    }

    private EntityState Advance(ref MatchState state, EntityState entity)
    {
        entity.Frame++;
        if (entity.RehitCooldown > 0)
        {
            entity.RehitCooldown--;
        }

        if (entity.Frame <= _characters.ProjectileOf(in entity).Lifetime)
        {
            entity.Position += entity.Velocity;
            return entity;
        }

        state.Events.Emit(
            state.Frame, FrameEventKind.ProjectileExpire, entity.Owner, entity.Position, entity.Projectile);
        return default;
    }
}
