namespace Sim.State;

/// <summary>The match's seeded random number generator; its whole state lives in the match state.</summary>
/// <remarks>
/// SplitMix64: one 64-bit counter, every seed valid, same sequence on every platform. Rolled back with the rest of
/// the state, so a resimulated frame draws the same numbers again.
/// </remarks>
public struct RandomState
{
    private const ulong Increment = 0x9E3779B97F4A7C15;

    // The 64-bit counter as two halves: a ulong field would make the match state 8-byte aligned and add padding.
    private uint _counterLow;
    private uint _counterHigh;

    public static RandomState FromSeed(ulong seed) =>
        new() { _counterLow = (uint)seed, _counterHigh = (uint)(seed >> 32) };

    /// <summary>The next 32 random bits.</summary>
    public uint NextUInt()
    {
        ulong counter = (((ulong)_counterHigh << 32) | _counterLow) + Increment;
        _counterLow = (uint)counter;
        _counterHigh = (uint)(counter >> 32);
        ulong mixed = counter;
        mixed = (mixed ^ (mixed >> 30)) * 0xBF58476D1CE4E5B9;
        mixed = (mixed ^ (mixed >> 27)) * 0x94D049BB133111EB;
        mixed ^= mixed >> 31;
        return (uint)(mixed >> 32);
    }

    /// <summary>A number in [0, <paramref name="exclusiveMax"/>); 0 when <paramref name="exclusiveMax"/> ≤ 0.</summary>
    /// <remarks>
    /// Multiply-shift: the bias is below 1 in 2^32 / exclusiveMax, far below anything a player could notice, and it
    /// never loops, so every call takes the same time.
    /// </remarks>
    public int NextBelow(int exclusiveMax) =>
        exclusiveMax <= 0 ? 0 : (int)(((ulong)NextUInt() * (uint)exclusiveMax) >> 32);
}
