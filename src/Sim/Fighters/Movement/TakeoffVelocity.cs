using Sim.Characters;
using Sim.Math;

namespace Sim.Fighters.Movement;

/// <summary>The velocity a jump starts with: height from its kind, arc from its direction; values from data.</summary>
public static class TakeoffVelocity
{
    /// <param name="direction">+1 forward, -1 back, 0 straight up, relative to <paramref name="facing"/>.</param>
    /// <param name="facing">+1 facing right, -1 facing left.</param>
    public static IntVector2 For(Takeoff takeoff, int direction, int facing, in CharacterStats stats)
    {
        int upward = takeoff == Takeoff.HighJump ? stats.HighJumpVelocity : stats.JumpVelocity;
        int forward = direction switch
        {
            > 0 => stats.JumpForwardSpeed,
            < 0 => -stats.JumpBackSpeed,
            _ => 0,
        };
        return new IntVector2(forward * facing, upward);
    }
}
