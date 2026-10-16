using Sim.Characters;
using Sim.Fighters.States;
using Sim.State;

namespace Sim.Combat;

/// <summary>What a hit or a block does to the defender: damage or chip, then hitstun, knockdown or blockstun.</summary>
/// <remarks>
/// Health stops at 0; what a KO does is round logic (P1-13). Chip damage can therefore finish a round. Hitstop is
/// not applied here: it freezes both fighters, so <see cref="HitResolutionStage"/> applies it.
/// </remarks>
public static class HitReactions
{
    /// <summary>
    /// Air hits go to air hitstun (which ends in a knockdown on landing), knockdown hits on the ground knock down
    /// straight away, and other hits go to standing or crouching hitstun after the defender's posture.
    /// </summary>
    public static void TakeHit(ref FighterState defender, in HitData hit, CharacterData character)
    {
        defender.Health = LoseHealth(defender.Health, hit.Damage);
        if (defender.IsAirborne != 0)
        {
            MoveEntry.EnterStun(ref defender, FighterAction.AirHitstun, hit.Hitstun, character);
        }
        else if (hit.Reaction == HitReaction.Knockdown)
        {
            MoveEntry.EnterBasic(ref defender, FighterAction.Knockdown, character);
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
        FighterAction blockstun =
            stance == BlockStance.Crouching ? FighterAction.CrouchBlockstun : FighterAction.Blockstun;
        MoveEntry.EnterStun(ref defender, blockstun, hit.Blockstun, character);
    }

    private static int LoseHealth(int health, int damage) => health > damage ? health - damage : 0;

    private static bool IsCrouching(FighterAction action) => action is FighterAction.Crouch
        or FighterAction.CrouchHitstun or FighterAction.CrouchBlockstun;
}
