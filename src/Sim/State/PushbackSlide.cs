namespace Sim.State;

/// <summary>
/// A defender sliding away from a hit or block: the distance still to go, spread over the frames left. In the corner the
/// part the wall stops pushes the attacker back instead, unless the hit came from a projectile.
/// </summary>
public struct PushbackSlide
{
    /// <summary>Subpixels still to slide.</summary>
    public int DistanceLeft;

    /// <summary>The defender's own frames the rest is spread over; 0 = no slide.</summary>
    public int FramesLeft;

    /// <summary>+1 slides right, -1 left: the way the attacker faced.</summary>
    public int Direction;

    /// <summary>1 when the wall passes what it stops on to the opponent (a fighter's own attack), else 0.</summary>
    public int PushesAttacker;
}
