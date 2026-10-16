using Sim.Input;

namespace Sim.Fighters.States;

/// <summary>The basic action a free fighter takes from the direction held: walk, crouch, jump or stand.</summary>
public static class NeutralActions
{
    public static FighterAction For(int direction, bool isAirborne)
    {
        if (isAirborne)
        {
            return FighterAction.Jump;
        }

        if (NumpadDirection.IsUp(direction))
        {
            return FighterAction.Prejump;
        }

        if (NumpadDirection.IsDown(direction))
        {
            return FighterAction.Crouch;
        }

        if (NumpadDirection.IsForward(direction))
        {
            return FighterAction.WalkForward;
        }

        return NumpadDirection.IsBack(direction) ? FighterAction.WalkBack : FighterAction.Idle;
    }

    /// <summary>Free to act: walking, crouching, standing or in a jump. Moves can start from these.</summary>
    public static bool IsFree(FighterAction action) => action is FighterAction.Idle or FighterAction.Crouch
        or FighterAction.WalkForward or FighterAction.WalkBack or FighterAction.Jump;
}
