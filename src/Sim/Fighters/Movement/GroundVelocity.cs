using Sim.Characters;
using Sim.Fighters.States;

namespace Sim.Fighters.Movement;

/// <summary>Horizontal speed on the ground from the fighter's action: walking moves it, anything else stops it.</summary>
/// <remarks>Dashes and other travelling moves give their own velocity in move data, which takes precedence.</remarks>
public static class GroundVelocity
{
    /// <param name="facing">+1 facing right, -1 facing left.</param>
    public static int For(FighterAction action, int facing, in CharacterStats stats) => action switch
    {
        FighterAction.WalkForward => stats.WalkForwardSpeed * facing,
        FighterAction.WalkBack => -stats.WalkBackSpeed * facing,
        _ => 0,
    };
}
