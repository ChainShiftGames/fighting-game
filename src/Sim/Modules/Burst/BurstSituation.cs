using Sim.Fighters.States;

namespace Sim.Modules.Burst;

/// <summary>Which of a fighter's actions the config lets it burst from.</summary>
/// <remarks>
/// Neutral means the free actions the state machine starts moves from. Never from an attack, knockdown, wake-up,
/// throw or a short transition (prejump, landing, dashes): a burst breaks combos and pressure, it isn't a cancel.
/// </remarks>
public static class BurstSituation
{
    public static bool Allows(FighterAction action, in BurstConfig config) => action switch
    {
        FighterAction.Hitstun or FighterAction.CrouchHitstun or FighterAction.AirHitstun =>
            config.UsableInHitstun != 0,
        FighterAction.Blockstun or FighterAction.CrouchBlockstun or FighterAction.AirBlockstun =>
            config.UsableInBlockstun != 0,
        FighterAction.Idle or FighterAction.Crouch or FighterAction.WalkForward or FighterAction.WalkBack
            or FighterAction.Jump => config.UsableInNeutral != 0,
        _ => false,
    };
}
