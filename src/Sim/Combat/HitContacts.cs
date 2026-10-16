using Sim.Math;
using Sim.State;

namespace Sim.Combat;

/// <summary>Which attackers' hitboxes touched the opponent's hurtboxes this frame, and where.</summary>
/// <remarks>
/// Per-frame scratch handed from hit detection to hit resolution, not match state: detection clears and refills it
/// every tick before resolution reads it, so nothing in it outlives a tick and snapshots don't need it. Created once
/// at match start; allocates nothing afterwards.
/// </remarks>
public sealed class HitContacts
{
    private readonly bool[] _touches = new bool[MatchState.PlayerCount];
    private readonly IntVector2[] _points = new IntVector2[MatchState.PlayerCount];

    /// <summary>True when the attack of <paramref name="attacker"/> touched its opponent this frame.</summary>
    public bool Touches(int attacker) => _touches[attacker];

    /// <summary>Where it touched: the centre of the first overlapping hitbox and hurtbox.</summary>
    public IntVector2 PointOf(int attacker) => _points[attacker];

    public void Clear()
    {
        for (int fighter = 0; fighter < MatchState.PlayerCount; fighter++)
        {
            _touches[fighter] = false;
            _points[fighter] = IntVector2.Zero;
        }
    }

    public void Record(int attacker, IntVector2 point)
    {
        _touches[attacker] = true;
        _points[attacker] = point;
    }
}
