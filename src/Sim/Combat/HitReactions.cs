using Sim.Characters;
using Sim.Fighters.States;
using Sim.Math;
using Sim.State;

namespace Sim.Combat;

/// <summary>What a hit or a block does to the defender: damage or chip, then hitstun, a launch, knockdown or blockstun.</summary>
/// <remarks>
/// Health stops at 0; ending the round on a KO is round logic (P1-13). Chip damage can therefore finish a round. A
/// hit with launch data (ADR-0022) sends the defender up into air hitstun (a juggle); otherwise a hit or block that
/// KOs a grounded defender knocks it down at once, whatever the hit's reaction, and an airborne one keeps falling in
/// air hitstun and is knocked down on landing. Either way a KO'd fighter stays down until the next round (the state
/// machine doesn't wake it up). Every hit also sets the knockdown length and the bounces of the juggle it starts or
/// continues, so the last hit taken decides them. Hitstop is not applied here: it freezes both fighters, so
/// <see cref="HitResolutionStage"/> applies it. Neither is the turn to face the attacker, which needs to know where
/// the attacker stands (the resolution stages do it first).
/// </remarks>
public static class HitReactions
{
    /// <summary>
    /// A launch goes to air hitstun with the launch velocity; without one, air hits keep the defender's momentum in
    /// air hitstun (a knockdown on landing), knockdown, KO and OTG hits on the ground knock down straight away, and
    /// other hits go to standing or crouching hitstun after the defender's posture.
    /// </summary>
    /// <param name="direction">
    /// The way the hit sends the defender, +1 right (<see cref="HitDirection"/>; a throw passes its thrower's facing):
    /// a launch goes that way.
    /// </param>
    /// <param name="flagsClearedWhenHit">Fighter flags a hit takes away (<c>MatchRules.FlagsClearedWhenHit</c>).</param>
    public static void TakeHit(
        ref FighterState defender, in HitData hit, CharacterData character, int direction, int flagsClearedWhenHit)
    {
        defender.Health = LoseHealth(defender.Health, hit.Damage);
        defender.Flags &= ~flagsClearedWhenHit;
        defender.KnockdownFrames = hit.KnockdownFrames;
        defender.GroundBounce = hit.GroundBounce;
        defender.WallBounce = hit.WallBounce;
        bool isAirborne = defender.IsAirborne != 0;
        if (isAirborne ? hit.HasAirLaunch != 0 || hit.HasLaunch != 0 : hit.HasLaunch != 0)
        {
            IntVector2 launch = isAirborne && hit.HasAirLaunch != 0 ? hit.AirLaunch : hit.Launch;
            defender.Velocity = new IntVector2(launch.X * direction, launch.Y);
            defender.IsAirborne = 1;
            MoveEntry.EnterStun(ref defender, FighterAction.AirHitstun, hit.Hitstun, character);
        }
        else if (isAirborne)
        {
            MoveEntry.EnterStun(ref defender, FighterAction.AirHitstun, hit.Hitstun, character);
        }
        else if (hit.Reaction == HitReaction.Knockdown || KnockOut.IsKnockedOut(in defender) ||
            defender.Action == FighterAction.Knockdown)
        {
            MoveEntry.EnterKnockdown(ref defender, character);
        }
        else
        {
            FighterAction hitstun = IsCrouching(defender.Action) ? FighterAction.CrouchHitstun : FighterAction.Hitstun;
            MoveEntry.EnterStun(ref defender, hitstun, hit.Hitstun, character);
        }
    }

    public static void Block(ref FighterState defender, in HitData hit, BlockStance stance, CharacterData character)
    {
        defender.Health = LoseHealth(defender.Health, hit.ChipDamage);
        defender.KnockdownFrames = 0;
        if (KnockOut.IsKnockedOut(in defender))
        {
            MoveEntry.EnterKnockdown(ref defender, character);
            return;
        }

        FighterAction blockstun = stance switch
        {
            BlockStance.Crouching => FighterAction.CrouchBlockstun,
            BlockStance.Air => FighterAction.AirBlockstun,
            _ => FighterAction.Blockstun,
        };
        MoveEntry.EnterStun(ref defender, blockstun, hit.Blockstun, character);
    }

    private static int LoseHealth(int health, int damage) => health > damage ? health - damage : 0;

    private static bool IsCrouching(FighterAction action) => action is FighterAction.Crouch
        or FighterAction.CrouchHitstun or FighterAction.CrouchBlockstun;
}
