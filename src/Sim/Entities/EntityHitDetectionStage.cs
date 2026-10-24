using Sim.Characters;
using Sim.Collision;
using Sim.Combat;
using Sim.Math;
using Sim.Pipeline;
using Sim.State;

namespace Sim.Entities;

/// <summary>The entity part of the hit detection step: projectile clashes, and projectiles touching fighters.</summary>
/// <remarks>
/// Runs after <see cref="HitDetectionStage"/> (which clears <see cref="HitContacts"/>), on the frame's final
/// positions, and only records contacts; <see cref="EntityHitResolutionStage"/> acts on them.
/// - Clash: every pair of opposing entities whose hitboxes overlap costs both one durability this frame. Pairs are
///   counted once each from the state as the step started, so the result doesn't depend on slot or port order.
/// - Touch: an entity that survives its clashes touches its owner's opponent when its hitboxes overlap the
///   opponent's hurtboxes and the opponent isn't invulnerable on its move frame (the same rule as fighter attacks).
///   The touch also goes into <see cref="HitContacts"/>, so a same-frame exchange counts it.
/// An entity waiting out its rehit cooldown (<see cref="EntityState.RehitCooldown"/>) touches nothing but still
/// clashes. Entities never touch their own owner, and an entity in a global freeze still has its boxes.
/// </remarks>
public sealed class EntityHitDetectionStage : ITickStage
{
    private readonly OwnerCharacters _characters;
    private readonly EntityContacts _contacts;
    private readonly HitContacts _fighterContacts;

    public EntityHitDetectionStage(OwnerCharacters characters, EntityContacts contacts, HitContacts fighterContacts)
    {
        _characters = characters;
        _contacts = contacts;
        _fighterContacts = fighterContacts;
    }

    public void Run(ref MatchState state, in FrameActivity activity)
    {
        _contacts.Clear();
        DetectClashes(in state.Entities);
        for (int slot = 0; slot < EntityPool.Capacity; slot++)
        {
            ref readonly EntityState entity = ref state.Entities.Slots[slot];
            if (entity.IsActive != 0 && entity.RehitCooldown == 0 && entity.Durability > _contacts.ClashesOf(slot))
            {
                DetectTouch(in state, slot, in entity);
            }
        }
    }

    private void DetectClashes(in EntityPool pool)
    {
        for (int first = 0; first < EntityPool.Capacity; first++)
        {
            ref readonly EntityState entity = ref pool.Slots[first];
            if (entity.IsActive == 0)
            {
                continue;
            }

            for (int second = first + 1; second < EntityPool.Capacity; second++)
            {
                ref readonly EntityState other = ref pool.Slots[second];
                if (other.IsActive != 0 && other.Owner != entity.Owner &&
                    TryFindOverlap(in entity, in other, out IntVector2 point))
                {
                    _contacts.RecordClash(first, point);
                    _contacts.RecordClash(second, point);
                }
            }
        }
    }

    private void DetectTouch(in MatchState state, int slot, in EntityState entity)
    {
        int defender = MatchState.PlayerCount - 1 - entity.Owner;
        CharacterData defenderCharacter = _characters.Of(defender);
        ref readonly FighterState defenderState = ref state.Fighters[defender];
        if (defenderCharacter.Moves[defenderState.Move].IsInvulnerableOn(defenderState.MoveFrame))
        {
            return;
        }

        foreach (AxisAlignedBox hitbox in new EntityHitboxes(_characters, in entity))
        {
            foreach (AxisAlignedBox hurtbox in new ActiveBoxes(defenderCharacter, in defenderState, BoxKind.Hurt))
            {
                if (hitbox.Overlaps(hurtbox))
                {
                    _contacts.RecordTouch(slot, HitboxOverlap.CentreOfOverlap(hitbox, hurtbox));
                    _fighterContacts.RecordProjectileTouch(defender);
                    return;
                }
            }
        }
    }

    private bool TryFindOverlap(in EntityState entity, in EntityState other, out IntVector2 point)
    {
        foreach (AxisAlignedBox hitbox in new EntityHitboxes(_characters, in entity))
        {
            foreach (AxisAlignedBox otherHitbox in new EntityHitboxes(_characters, in other))
            {
                if (hitbox.Overlaps(otherHitbox))
                {
                    point = HitboxOverlap.CentreOfOverlap(hitbox, otherHitbox);
                    return true;
                }
            }
        }

        point = IntVector2.Zero;
        return false;
    }
}
