using Sim.Fighters.States;

namespace FightingGame.Presentation.World;

/// <summary>
/// The hit and block reactions (hitstun and blockstun, standing, crouching and in the air): a fighter model snaps into
/// them and counts them from each hit (<see cref="HitReactionClock"/>), where other basic moves fade in and run on.
/// </summary>
public static class HitReactionActions
{
    public static bool Includes(FighterAction action) => action is FighterAction.Hitstun
        or FighterAction.CrouchHitstun or FighterAction.AirHitstun or FighterAction.Blockstun
        or FighterAction.CrouchBlockstun or FighterAction.AirBlockstun;
}
