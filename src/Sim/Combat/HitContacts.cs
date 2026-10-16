using Sim.Math;
using Sim.State;

namespace Sim.Combat;

/// <summary>What touched each fighter this frame: the other fighter's attack, and whether projectiles did.</summary>
/// <remarks>
/// Per-frame scratch handed from hit detection to hit resolution, not match state: <see cref="HitDetectionStage"/>
/// clears and refills it every tick before resolution reads it (projectile detection runs after it in the same step
/// and adds its touches), so nothing in it outlives a tick and snapshots don't need it. Created once at match start;
/// allocates nothing afterwards.
/// </remarks>
public sealed class HitContacts
{
    private readonly bool[] _touches = new bool[MatchState.PlayerCount];
    private readonly IntVector2[] _points = new IntVector2[MatchState.PlayerCount];
    private readonly bool[] _touchedByProjectile = new bool[MatchState.PlayerCount];

    /// <summary>True when the attack of <paramref name="attacker"/> touched its opponent this frame.</summary>
    public bool Touches(int attacker) => _touches[attacker];

    /// <summary>Where it touched: the centre of the first overlapping hitbox and hurtbox.</summary>
    public IntVector2 PointOf(int attacker) => _points[attacker];

    /// <summary>
    /// Both fighters were touched by something of the opponent's this frame (its attack or a projectile): the
    /// same-frame exchange <see cref="Rules.MatchRules.TradeRule"/> decides.
    /// </summary>
    public bool IsSameFrameExchange => IsTouched(0) && IsTouched(1);

    public void Clear()
    {
        for (int fighter = 0; fighter < MatchState.PlayerCount; fighter++)
        {
            _touches[fighter] = false;
            _points[fighter] = IntVector2.Zero;
            _touchedByProjectile[fighter] = false;
        }
    }

    public void Record(int attacker, IntVector2 point)
    {
        _touches[attacker] = true;
        _points[attacker] = point;
    }

    /// <summary>Records that a projectile of the opponent's touched <paramref name="defender"/>.</summary>
    public void RecordProjectileTouch(int defender) => _touchedByProjectile[defender] = true;

    private bool IsTouched(int defender) =>
        _touches[MatchState.PlayerCount - 1 - defender] || _touchedByProjectile[defender];
}
