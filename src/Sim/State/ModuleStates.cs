using System.Runtime.CompilerServices;

namespace Sim.State;

/// <summary>The reserved state slices for mechanics modules, one per module slot (ADR-0011).</summary>
/// <remarks>How a module claims and reads its slice is defined by the module contract (P1-20).</remarks>
[InlineArray(MaxModules)]
public struct ModuleStates
{
    public const int MaxModules = 16;

    private ModuleStateSlice _element;
}
