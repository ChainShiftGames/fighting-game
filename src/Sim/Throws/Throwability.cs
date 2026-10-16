using Sim.Characters;
using Sim.Fighters.States;
using Sim.State;

namespace Sim.Throws;

/// <summary>Who can be thrown (P1-23): standard throw invulnerability.</summary>
/// <remarks>
/// A fighter can't be thrown while airborne (throws are ground throws), while stunned (hitstun, blockstun,
/// knockdown), while already in a throw (held or teching), on its move's invulnerable frames, or while its wake-up
/// or landing protection runs (<see cref="FighterState.ThrowInvulnerableFramesLeft"/>). Anything else can be
/// thrown, including attack startup and prejump.
/// </remarks>
public static class Throwability
{
    public static bool CanBeThrown(in FighterState fighter, CharacterData character) =>
        fighter.IsAirborne == 0 && fighter.ThrowInvulnerableFramesLeft == 0 && !IsStunnedOrInThrow(fighter.Action) &&
        !character.Moves[fighter.Move].IsInvulnerableOn(fighter.MoveFrame);

    /// <summary>Stunned, knocked down, held or teching: such a fighter can't be thrown and can't keep a hold.</summary>
    public static bool IsStunnedOrInThrow(FighterAction action) => action is FighterAction.Hitstun
        or FighterAction.CrouchHitstun or FighterAction.AirHitstun or FighterAction.Blockstun
        or FighterAction.CrouchBlockstun or FighterAction.AirBlockstun or FighterAction.Knockdown or FighterAction.Thrown
        or FighterAction.ThrowTech;
}
