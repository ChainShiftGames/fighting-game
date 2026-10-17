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
///   against any attack, even when the projectile comes from behind, and in the air only through a module,
///   <see cref="Guarding"/>; <see cref="HitReactions"/> for damage, chip,
///   stun and knockdown), against the defender as fighter attacks left it this frame. A hit that catches the
///   defender attacking is a counter hit or punish counter, as a fighter's is (<see cref="CounterHits"/>; the
///   counter hit event follows the hit event). Only the defender gets the
///   projectile's hitstop (<see cref="Hitstop"/>); the owner isn't in contact, so it keeps acting, and its move
///   doesn't record the outcome (no cancel on a projectile's hit).
/// - The modules adjust a hit before it lands (<see cref="IHitAdjustmentHook"/>); one they make whiff leaves the
///   projectile flying and the defender untouched.
/// - One hit per projectile: it disappears on hit or block. Several touching the same defender land in slot order.
/// - Each landed touch emits its event and runs the modules' hit or block hooks with the owner as the attacker.
/// A projectile only ever changes its owner's opponent, so P1's and P2's projectiles can't affect each other here.
/// </remarks>
public sealed class EntityHitResolutionStage : ITickStage
{
    private readonly OwnerCharacters _characters;
    private readonly int _flagsClearedWhenHit;
    private readonly EntityContacts _contacts;
    private readonly HitContacts _fighterContacts;
    private readonly ModuleLineup _modules;
    private readonly TradeRule _tradeRule;
    private readonly CounterHits _counterHits;
    private readonly Guarding _guarding;
    private readonly Pushback _pushback;
    private readonly ShortcutDamage _shortcutDamage;

    /// <param name="flagsClearedWhenHit">Fighter flags a landed hit takes away (<c>MatchRules.FlagsClearedWhenHit</c>).</param>
    public EntityHitResolutionStage(
        OwnerCharacters characters,
        EntityContacts contacts,
        HitContacts fighterContacts,
        ModuleLineup modules,
        TradeRule tradeRule,
        CounterHits counterHits,
        Guarding guarding,
        Pushback pushback,
        ShortcutDamage shortcutDamage,
        int flagsClearedWhenHit = 0)
    {
        _flagsClearedWhenHit = flagsClearedWhenHit;
        _characters = characters;
        _contacts = contacts;
        _fighterContacts = fighterContacts;
        _modules = modules;
        _tradeRule = tradeRule;
        _counterHits = counterHits;
        _guarding = guarding;
        _pushback = pushback;
        _shortcutDamage = shortcutDamage;
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
        HitData hit = _shortcutDamage.Apply(_characters.ProjectileOf(in entity).Hit, entity.IsShortcut != 0);
        CounterHitKind counter = Defend(
            ref state, defender, in hit, entity.Owner, entity.Facing, out bool isBlocked, out bool whiffs);
        if (whiffs)
        {
            return;
        }

        IntVector2 point = _contacts.TouchPointOf(slot);
        Remove(ref state, slot, isBlocked ? FrameEventKind.ProjectileBlock : FrameEventKind.ProjectileHit, point);
        CounterHitEvent.Emit(ref state, counter, entity.Owner, point);

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

    // The defender takes the hit or block and the projectile's hitstop; returns whether a hit was a counter.
    // A hit the modules make whiff changes nothing, and the projectile flies on.
    // Projectile pushback never moves the owner, however far away it is: the wall just stops the slide.
    private CounterHitKind Defend(
        ref MatchState state,
        int defender,
        in HitData hit,
        int owner,
        int facing,
        out bool isBlocked,
        out bool whiffs)
    {
        whiffs = false;
        CharacterData character = _characters.Of(defender);
        ref FighterState defenderState = ref state.Fighters[defender];
        AirGuardCheck check = new(owner, defender, MoveCategory.None, MoveStance.Ground, isProjectile: true);
        BlockStance stance = _guarding.StanceOf(
            ref state, in defenderState, in state.Fighters[owner], state.Inputs[defender].FramesAgo(0), in check);
        isBlocked = Blocking.Stops(stance, hit.Guard);
        if (isBlocked)
        {
            HitData blocked = hit;
            _modules.AdjustBlock(ref state, new FighterContact(owner, defender), ref blocked);
            defenderState = ref state.Fighters[defender];
            HitReactions.Block(ref defenderState, in blocked, stance, character);
            Hitstop.Extend(ref defenderState.Timing, blocked.Hitstop);
            _pushback.Start(ref defenderState, blocked.Pushback, facing, pushesAttacker: false);
            return CounterHitKind.None;
        }

        CounterHitKind counter = CounterHits.KindAgainst(in defenderState, character);
        HitData landed = _counterHits.Apply(in hit, counter);
        if (!_modules.AdjustHit(ref state, new FighterContact(owner, defender), ref landed))
        {
            whiffs = true;
            return CounterHitKind.None;
        }

        HitReactions.TakeHit(ref defenderState, in landed, character, facing, _flagsClearedWhenHit);
        Hitstop.Extend(ref defenderState.Timing, landed.Hitstop);
        _pushback.Start(ref defenderState, landed.Pushback, facing, pushesAttacker: false);
        return counter;
    }

    private static void Remove(ref MatchState state, int slot, FrameEventKind kind, IntVector2 point)
    {
        EntityState entity = state.Entities.Slots[slot];
        state.Entities.Slots[slot] = default;
        state.Events.Emit(state.Frame, kind, entity.Owner, point, entity.Projectile);
    }
}
