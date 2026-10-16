using Sim.Rules;

namespace Sim.Modules;

/// <summary>A self-contained mechanic that plugs into the tick pipeline through hooks (ADR-0011).</summary>
/// <remarks>
/// A module implements this plus only the hook interfaces it needs (<see cref="IAfterInputHook"/> and the others in
/// this folder). It keeps no match state itself: its state lives in its slice of <c>MatchState</c>, read through
/// <see cref="ModuleContext.GetState{TState}"/>, and its settings in its <see cref="MatchRules"/> slot. It changes
/// core state only through the context's core requests. Created once at match start; hooks must not allocate.
/// Hooks run once per frame for the whole match, so the module handles both fighters the same way (no port bias).
/// </remarks>
public interface IMechanicModule
{
    /// <summary>Stable, non-zero and unique among registered modules.</summary>
    ModuleId Id { get; }

    /// <summary>The snake_case name the module order in data uses for this module.</summary>
    string Name { get; }
}
