using System.Runtime.CompilerServices;

namespace Sim.State;

/// <summary>The fixed-size storage behind <see cref="InputHistory"/>'s skipped frames: one bit per frame.</summary>
[InlineArray(Length)]
public struct InputHistorySkips
{
    public const int BitsPerElement = 32;

    public const int Length = (InputHistoryFrames.Length + BitsPerElement - 1) / BitsPerElement;

    private uint _element;
}
