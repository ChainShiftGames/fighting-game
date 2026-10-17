using System.Runtime.CompilerServices;

namespace Sim.State;

/// <summary>The reserved state slices for mechanics modules, one per module slot (ADR-0011, ADR-0023).</summary>
/// <remarks>
/// Slots 0 to <see cref="LineupSlots"/> − 1 belong to the match-wide lineup (<c>MatchRules.Modules</c>); after them
/// each fighter has one slot for its character's own mechanic (<see cref="MechanicSlot"/>), P1 first.
/// </remarks>
[InlineArray(MaxModules)]
public struct ModuleStates
{
    /// <summary>Slots of the match-wide module lineup.</summary>
    public const int LineupSlots = 24;

    /// <summary>Every module slot: the lineup plus one character mechanic per fighter.</summary>
    public const int MaxModules = LineupSlots + MatchState.PlayerCount;

    private ModuleStateSlice _element;

    /// <summary>The slot of <paramref name="fighter"/>'s character mechanic.</summary>
    public static int MechanicSlot(int fighter) => LineupSlots + fighter;
}
