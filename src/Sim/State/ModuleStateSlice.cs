using System.Runtime.CompilerServices;

namespace Sim.State;

/// <summary>One module's fixed-size part of the match state, as raw integers the module interprets.</summary>
[InlineArray(Length)]
public struct ModuleStateSlice
{
    /// <summary>Integers per module.</summary>
    public const int Length = 32;

    private int _element;
}
