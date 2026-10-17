using System;
using Sim.Characters;
using Sim.Fighters.States;
using Sim.State;

namespace Netcode.Bots;

/// <summary>
/// Turns the match state into what one player sees of it. The only bot code that reads the game state, so the
/// boundary between "on screen" and "internal" is in one place.
/// </summary>
public static class MatchObserver
{
    /// <param name="state">The state the player's screen shows (online: possibly predicted, as for a human).</param>
    /// <param name="player">The bot's port: 0 = P1, 1 = P2.</param>
    /// <param name="self">The bot's character, for reading its own animation.</param>
    /// <param name="opponent">The opponent's character, for reading its animation.</param>
    public static BotObservation Observe(in MatchState state, int player, CharacterData self, CharacterData opponent)
    {
        int other = 1 - player;
        ref readonly FighterState own = ref state.Fighters[player];
        return new BotObservation(
            state.Round.Phase == RoundPhase.Fighting,
            ViewOf(in own, self),
            ViewOf(in state.Fighters[other], opponent),
            IncomingProjectileDistance(in state, other, own.Position.X));
    }

    private static FighterView ViewOf(in FighterState fighter, CharacterData character)
    {
        FighterPose pose = PoseOf(fighter.Action);
        MovePhase phase = MovePhase.None;
        Guard guard = Guard.Mid;
        if (pose == FighterPose.Attacking)
        {
            ref readonly MoveData move = ref character.Moves[fighter.Move];
            phase = MovePhases.Of(in move, fighter.MoveFrame);
            ReadOnlySpan<HitData> hits = character.HitsOf(in move);
            guard = hits.IsEmpty ? Guard.Mid : hits[0].Guard;
        }

        return new FighterView(
            fighter.Position.X,
            fighter.Position.Y,
            fighter.Facing,
            fighter.Health,
            fighter.IsAirborne != 0,
            pose,
            phase,
            guard);
    }

    private static FighterPose PoseOf(FighterAction action) => action switch
    {
        FighterAction.Idle => FighterPose.Standing,
        FighterAction.Crouch => FighterPose.Crouching,
        FighterAction.WalkForward or FighterAction.WalkBack => FighterPose.Walking,
        FighterAction.Prejump or FighterAction.Jump or FighterAction.AirDash or FighterAction.AirBackdash =>
            FighterPose.Jumping,
        FighterAction.Landing => FighterPose.Landing,
        FighterAction.Dash or FighterAction.Backdash => FighterPose.Dashing,
        FighterAction.Hitstun or FighterAction.CrouchHitstun or FighterAction.AirHitstun => FighterPose.Hitstun,
        FighterAction.Blockstun or FighterAction.CrouchBlockstun or FighterAction.AirBlockstun =>
            FighterPose.Blockstun,
        FighterAction.Knockdown or FighterAction.WakeUp => FighterPose.KnockedDown,
        FighterAction.Thrown or FighterAction.ThrowTech => FighterPose.Thrown,
        FighterAction.Attack => FighterPose.Attacking,

        // A new action needs a pose here: guessing one would let a bot misread the screen without anyone noticing.
        _ => throw new ArgumentOutOfRangeException(nameof(action), action, "No pose for this fighter action."),
    };

    // The nearest of the opponent's projectiles still flying towards the bot; ones flying away are no threat.
    private static int IncomingProjectileDistance(in MatchState state, int owner, int targetX)
    {
        int nearest = BotObservation.NoProjectile;
        int first = EntityPool.FirstSlotOf(owner);
        for (int slot = first; slot < first + EntityPool.MaxPerPlayer; slot++)
        {
            ref readonly EntityState entity = ref state.Entities.Slots[slot];
            int towardTarget = targetX - entity.Position.X;
            if (entity.IsActive == 0 || Math.Sign(entity.Velocity.X) != Math.Sign(towardTarget))
            {
                continue;
            }

            nearest = Math.Min(nearest, Math.Abs(towardTarget));
        }

        return nearest;
    }
}
