using System.Runtime.CompilerServices;
using Sim.State;

namespace Sim.Rules;

/// <summary>The match-wide module lineup: slot order is hook order, and slot i's module owns state slice i (ADR-0011).</summary>
/// <remarks>
/// Filled from data (the module order list) before the match. Empty slots hold <see cref="ModuleId.None"/> and are
/// skipped, so the lineup can have gaps.
/// </remarks>
[InlineArray(ModuleStates.LineupSlots)]
public struct ModuleSlots
{
    private ModuleSlot _element;
}
