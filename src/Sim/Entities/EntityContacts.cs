using System;
using Sim.Math;
using Sim.State;

namespace Sim.Entities;

/// <summary>What each entity touched this frame: opposing projectiles (clashes) and the owner's opponent.</summary>
/// <remarks>
/// Per-frame scratch from entity hit detection to entity hit resolution, like <c>Combat.HitContacts</c>: detection
/// clears and refills it every tick, so nothing outlives a tick. Indexed by pool slot. Created once at match start.
/// </remarks>
public sealed class EntityContacts
{
    private readonly int[] _clashes = new int[EntityPool.Capacity];
    private readonly IntVector2[] _clashPoints = new IntVector2[EntityPool.Capacity];
    private readonly bool[] _touches = new bool[EntityPool.Capacity];
    private readonly IntVector2[] _touchPoints = new IntVector2[EntityPool.Capacity];

    /// <summary>Opposing projectiles the entity overlapped: the durability it loses this frame.</summary>
    public int ClashesOf(int slot) => _clashes[slot];

    /// <summary>Where it first overlapped one (pair order).</summary>
    public IntVector2 ClashPointOf(int slot) => _clashPoints[slot];

    /// <summary>True when the entity touched its owner's opponent and survived its clashes.</summary>
    public bool Touches(int slot) => _touches[slot];

    /// <summary>Where it touched: the centre of the first overlapping hitbox and hurtbox.</summary>
    public IntVector2 TouchPointOf(int slot) => _touchPoints[slot];

    public void Clear()
    {
        Array.Clear(_clashes);
        Array.Clear(_clashPoints);
        Array.Clear(_touches);
        Array.Clear(_touchPoints);
    }

    public void RecordClash(int slot, IntVector2 point)
    {
        if (_clashes[slot] == 0)
        {
            _clashPoints[slot] = point;
        }

        _clashes[slot]++;
    }

    public void RecordTouch(int slot, IntVector2 point)
    {
        _touches[slot] = true;
        _touchPoints[slot] = point;
    }
}
