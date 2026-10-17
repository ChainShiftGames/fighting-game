using System;
using Sim.Characters;
using Sim.Events;
using Sim.Math;
using Sim.Rules;
using Sim.State;

namespace Sim.Entities;

/// <summary>Puts a projectile in its owner's part of the entity pool, or refuses it when that part is full.</summary>
/// <remarks>
/// Pool rule (P1-21): each player has its own <see cref="MatchRules.EntitiesPerPlayer"/> slots, so one player's
/// projectiles never block the other's. A spawn takes the owner's lowest free slot; when there is none, that spawn
/// is refused and nothing changes: entities already out keep flying, so the newest spawn is the one that loses.
/// Spawns are tried in a fixed order (moves in the movement step, in data order; module requests at the end of the
/// frame, in module slot order), so on a full pool both PCs refuse the same ones. A refused spawn emits no event.
/// The projectile appears at the owner's position plus its data offset and flies with its data velocity, both
/// mirrored when the owner faces left, and keeps that facing.
/// </remarks>
public sealed class ProjectileSpawner
{
    private readonly OwnerCharacters _characters;
    private readonly int _slotsPerPlayer;

    /// <exception cref="ArgumentOutOfRangeException">The rules allow more entities than the state holds.</exception>
    public ProjectileSpawner(OwnerCharacters characters, in MatchRules rules)
    {
        if (rules.EntitiesPerPlayer < 0 || rules.EntitiesPerPlayer > EntityPool.MaxPerPlayer)
        {
            throw new ArgumentOutOfRangeException(
                nameof(rules),
                rules.EntitiesPerPlayer,
                $"Entities per player must be from 0 to {EntityPool.MaxPerPlayer}, the slots the state has.");
        }

        _characters = characters;
        _slotsPerPlayer = rules.EntitiesPerPlayer;
    }

    /// <summary>Spawns projectile <paramref name="projectile"/> of the owner's character; false when refused.</summary>
    /// <param name="isShortcut">A shortcut move spawns it, so its hit deals the shortcut damage (ADR-0020).</param>
    public bool TrySpawn(ref MatchState state, int owner, int projectile, bool isShortcut = false)
    {
        int slot = FreeSlotOf(in state.Entities, owner);
        if (slot < 0)
        {
            return false;
        }

        ref readonly FighterState fighter = ref state.Fighters[owner];
        ref readonly ProjectileData data = ref _characters.Of(owner).Projectiles[projectile];
        IntVector2 position = fighter.Position + Facing(data.Offset, fighter.Facing);
        state.Entities.Slots[slot] = new EntityState
        {
            IsActive = 1,
            Owner = owner,
            Projectile = projectile,
            Frame = 1,
            Position = position,
            Velocity = Facing(data.Velocity, fighter.Facing),
            Facing = fighter.Facing,
            Durability = data.Durability,
            IsShortcut = isShortcut ? 1 : 0,
        };
        state.Events.Emit(state.Frame, FrameEventKind.ProjectileSpawn, owner, position, projectile);
        return true;
    }

    private int FreeSlotOf(in EntityPool pool, int owner)
    {
        int first = EntityPool.FirstSlotOf(owner);
        for (int slot = first; slot < first + _slotsPerPlayer; slot++)
        {
            if (pool.Slots[slot].IsActive == 0)
            {
                return slot;
            }
        }

        return -1;
    }

    private static IntVector2 Facing(IntVector2 forward, int facing) => facing < 0 ? forward.MirrorX() : forward;
}
