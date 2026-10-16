using Sim.Characters;
using Sim.Fighters.States;
using Sim.State;

namespace Sim.Throws;

/// <summary>Starting and ending a hold: the thrown fighter's side of a throw.</summary>
public static class ThrowHolds
{
    /// <summary>Puts the defender in the thrower's hold for the tech window.</summary>
    public static void Start(ref FighterState defender, int throwMove, int techWindowFrames, CharacterData character)
    {
        MoveEntry.EnterBasic(ref defender, FighterAction.Thrown, character);
        defender.ThrowTechFramesLeft = techWindowFrames;
        defender.ThrownByMove = throwMove;
    }

    /// <summary>Forgets the hold; the caller decides what the fighter does next.</summary>
    public static void Clear(ref FighterState fighter)
    {
        fighter.ThrowTechFramesLeft = 0;
        fighter.ThrownByMove = 0;
    }

    /// <summary>The fighter is in a hold this frame.</summary>
    public static bool IsHeld(in FighterState fighter) =>
        fighter.Action == FighterAction.Thrown && fighter.ThrowTechFramesLeft > 0;
}
