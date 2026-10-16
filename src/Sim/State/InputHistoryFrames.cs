using System.Runtime.CompilerServices;
using Sim.Input;

namespace Sim.State;

/// <summary>The fixed-size storage behind <see cref="InputHistory"/>.</summary>
// Even, so the 2-byte input words end on a 4-byte boundary and the history needs no padding.
[InlineArray(Length)]
public struct InputHistoryFrames
{
    public const int Length = 64;

    private InputWord _element;
}
