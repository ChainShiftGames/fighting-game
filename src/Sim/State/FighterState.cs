using Sim.Math;

namespace Sim.State;

/// <summary>One fighter's part of the match state. Positions and velocities in subpixels.</summary>
/// <remarks>Gameplay systems add their own fields here as they are built (P1-08 onwards).</remarks>
public struct FighterState
{
    public IntVector2 Position;
    public IntVector2 Velocity;

    /// <summary>+1 facing right, -1 facing left.</summary>
    public int Facing;

    public int Health;
}
