using Sim.Math;
using Sim.State;

namespace Sim.Throws;

/// <summary>Which fighters' throw boxes touched a throwable opponent this frame, and where.</summary>
/// <remarks>
/// Per-frame scratch from <see cref="ThrowDetectionStage"/> to <see cref="ThrowResolutionStage"/>, like
/// <c>Combat.HitContacts</c>: cleared and refilled every tick, so snapshots don't need it. Created once at match
/// start; allocates nothing afterwards.
/// </remarks>
public sealed class ThrowContacts
{
    private readonly bool[] _touches = new bool[MatchState.PlayerCount];
    private readonly IntVector2[] _points = new IntVector2[MatchState.PlayerCount];

    public bool Touches(int thrower) => _touches[thrower];

    /// <summary>The centre of the first overlapping throw box and hurtbox.</summary>
    public IntVector2 PointOf(int thrower) => _points[thrower];

    public void Clear()
    {
        for (int fighter = 0; fighter < MatchState.PlayerCount; fighter++)
        {
            _touches[fighter] = false;
            _points[fighter] = IntVector2.Zero;
        }
    }

    public void Record(int thrower, IntVector2 point)
    {
        _touches[thrower] = true;
        _points[thrower] = point;
    }
}
