using Sim.Characters;
using Sim.Combat;
using Sim.Events;
using Sim.Math;
using Sim.Modules;
using Sim.Pipeline;
using Sim.Rules;
using Sim.State;

namespace Sim.Entities;

/// <summary>The entity part of the hit resolution step: clashes cost durability, then projectiles land.</summary>
/// <remarks>
/// Runs after <see cref="HitResolutionStage"/>, on what <see cref="EntityHitDetectionStage"/> found. Rules (P1-21):
/// - Clashes first: each entity loses the durability its clashes cost (one per opposing projectile it overlapped)
///   and disappears at 0, with a <see cref="FrameEventKind.ProjectileClash"/> event. A destroyed one hits nothing.
/// - A same-frame exchange (<see cref="HitContacts.IsSameFrameExchange"/>: both fighters touched by the other's
///   attacks or projectiles) under <see cref="TradeRule.Clash"/>: no projectile lands; each touching one disappears
///   with a clash event, as the fighters' moves are spent. Otherwise every touch lands (trades included).
/// - A touch is blocked or hits with the fighters' rules (<see cref="Blocking"/> holding away from the owner, as
///   against any attack, even when the projectile comes from behind; <see cref="HitReactions"/> for damage, chip,
///   stun and knockdown), against the defender as fighter attacks left it this frame. Only the defender gets the
///   projectile's hitstop (<see cref="Hitstop"/>); the owner isn't in contact, so it keeps acting, and its move
///   doesn't record the outcome (no cancel on a projectile's hit).
/// - One hit per projectile: it disappears on hit or block. Several touching the same defender land in slot order.
/// - Each landed touch emits its event and runs the modules' hit or block hooks with the owner as the attacker.
/// A projectile only ever changes its owner's opponent, so P1's and P2's projectiles can't affect each other here.
/// </remarks>
public sealed class EntityHitResolutionStage : ITickStage
{
    private readonly OwnerCharacters _characters;
    private readonly EntityContacts _contacts;
    private readonly HitContacts _fighterContacts;
    private readonly ModuleLineup _modules;
    private readonly TradeRule _tradeRule;

    public EntityHitResolutionStage(
        OwnerCharacters characters,
        EntityContacts contacts,
        HitContacts fighterContacts,
        ModuleLineup modules,
        TradeRule tradeRule)
    {
        _characters = characters;
        _contacts = contacts;
        _fighterContacts = fighterContacts;
        _modules = modules;
        _tradeRule = tradeRule;
    }

    public void Run(ref MatchState state, in FrameActivity activity)
    {
        ResolveClashes(ref state);
        bool cancelsOut = _fighterContacts.IsSameFrameExchange && _tradeRule == TradeRule.Clash;
        for (int slot = 0; slot < EntityPool.Capacity; slot++)
        {
            if (state.Entities.Slots[slot].IsActive == 0 || !_contacts.Touches(slot))
            {
                continue;
            }

            if (cancelsOut)
            {
                Remove(ref state, slot, FrameEventKind.ProjectileClash, _contacts.TouchPointOf(slot));
            }
            else
            {
                Land(ref state, slot);
            }
        }
    }

    private void ResolveClashes(ref MatchState state)
    {
        for (int slot = 0; slot < EntityPool.Capacity; slot++)
        {
            int clashes = _contacts.ClashesOf(slot);
            if (clashes == 0)
            {
                continue;
            }

            ref EntityState entity = ref state.Entities.Slots[slot];
            entity.Durability = entity.Durability > clashes ? entity.Durability - clashes : 0;
            if (entity.Durability == 0)
            {
                Remove(ref state, slot, FrameEventKind.ProjectileClash, _contacts.ClashPointOf(slot));
            }
            else
            {
                state.Events.Emit(
                    state.Frame, FrameEventKind.ProjectileClash, entity.Owner, _contacts.ClashPointOf(slot),
                    entity.Projectile);
            }
        }
    }

    private void Land(ref MatchState state, int slot)
    {
        EntityState entity = state.Entities.Slots[slot];
        int defender = MatchState.PlayerCount - 1 - entity.Owner;
        HitData hit = _characters.ProjectileOf(in entity).Hit;
        bool isBlocked = Defend(ref state, defender, in entity, in hit);
        Remove(
            ref state,
            slot,
            isBlocked ? FrameEventKind.ProjectileBlock : FrameEventKind.ProjectileHit,
            _contacts.TouchPointOf(slot));

        FighterContact contact = new(entity.Owner, defender);
        if (isBlocked)
        {
            _modules.RunOnBlock(ref state, in contact);
        }
        else
        {
            _modules.RunOnHit(ref state, in contact);
        }
    }

    // True when blocked. The defender takes the hit or block and the projectile's hitstop.
    private bool Defend(ref MatchState state, int defender, in EntityState entity, in HitData hit)
    {
        CharacterData character = _characters.Of(defender);
        ref FighterState defenderState = ref state.Fighters[defender];
        BlockStance stance = Blocking.StanceOf(
            in defenderState, in state.Fighters[entity.Owner], state.Inputs[defender].FramesAgo(0));
        bool isBlocked = Blocking.Stops(stance, hit.Guard);
        if (isBlocked)
        {
            HitReactions.Block(ref defenderState, in hit, stance, character);
        }
        else
        {
            HitReactions.TakeHit(ref defenderState, in hit, character);
        }

        Hitstop.Extend(ref defenderState.Timing, hit.Hitstop);
        return isBlocked;
    }

    private static void Remove(ref MatchState state, int slot, FrameEventKind kind, IntVector2 point)
    {
        EntityState entity = state.Entities.Slots[slot];
        state.Entities.Slots[slot] = default;
        state.Events.Emit(state.Frame, kind, entity.Owner, point, entity.Projectile);
    }
}
