using Sim.Fighters.States;
using Sim.State;

namespace Netcode.Drivers.Training;

/// <summary>What training needs to know about a fighter: whether it is being hit, or free of the opponent's pressure.</summary>
public static class FighterCondition
{
    /// <summary>In a hit reaction: hitstun, knocked down or being thrown.</summary>
    public static bool IsHit(in FighterState fighter) => fighter.Action is FighterAction.Hitstun
        or FighterAction.CrouchHitstun or FighterAction.AirHitstun or FighterAction.Knockdown or FighterAction.Thrown;

    /// <summary>Neither hit nor blocking nor getting up: the opponent's combo or blockstring is over for it.</summary>
    public static bool IsFree(in FighterState fighter) => !IsHit(in fighter) && fighter.Action is not (
        FighterAction.Blockstun or FighterAction.CrouchBlockstun or FighterAction.AirBlockstun
        or FighterAction.WakeUp or FighterAction.ThrowTech);
}
