namespace Sim.Input.Motions;

/// <summary>A set of motions, by their index in the <see cref="MotionSet"/>.</summary>
public readonly record struct MotionMask(ulong Bits)
{
    /// <summary>Motions one mask can hold.</summary>
    public const int Capacity = 64;

    public static MotionMask None => default;

    public MotionMask With(int motion) => new(Bits | (1UL << motion));

    public bool Contains(int motion) => (Bits & (1UL << motion)) != 0;
}
