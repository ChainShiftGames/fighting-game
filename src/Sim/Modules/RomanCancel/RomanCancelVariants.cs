namespace Sim.Modules.RomanCancel;

/// <summary>The situations a Roman Cancel can come from, as <see cref="RomanCancelConfig.Variants"/> bits.</summary>
/// <remarks>The variant is also the code of the module event it emits: its bit index (red 0, purple 1, blue 2, yellow 3).</remarks>
public static class RomanCancelVariants
{
    /// <summary>During an attack that hit or was blocked: keeps the pressure going or extends a combo.</summary>
    public const int Red = 1 << 0;

    /// <summary>During an attack that hasn't made contact: covers a whiff or a move's startup.</summary>
    public const int Purple = 1 << 1;

    /// <summary>From neutral (not attacking, not stunned): a burst of speed with the opponent slowed.</summary>
    public const int Blue = 1 << 2;

    /// <summary>In blockstun: escapes pressure into neutral, without slowing the opponent.</summary>
    public const int Yellow = 1 << 3;

    public const int All = Red | Purple | Blue | Yellow;
}
