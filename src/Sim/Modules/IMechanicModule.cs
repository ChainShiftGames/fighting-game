using System.Collections.Generic;
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

    /// <summary>The snake_case name data uses for this module (<c>data/rules/modules/&lt;name&gt;.json</c>).</summary>
    string Name { get; }

    /// <summary>The keys its data file sets in its config struct (developer settings); none by default.</summary>
    ModuleConfigLayout ConfigLayout => ModuleConfigLayout.None;

    /// <summary>Move roles every character must name for this module (ADR-0023); none by default.</summary>
    /// <remarks>
    /// Resolved once at match start; the module finds role <c>i</c>'s move with <see cref="ModuleContext.RoleMove"/>.
    /// A character without one of them fails loudly when data is loaded, and when a match starts.
    /// </remarks>
    IReadOnlyList<string> MoveRoles => [];

    /// <summary>True for a character's own mechanic (ADR-0023): never in the match-wide lineup.</summary>
    /// <remarks>
    /// A character names it in <c>character.json</c> (<c>mechanic</c>, with its config); it then runs in that fighter's
    /// mechanic slot after the lineup and serves only that fighter (<see cref="ModuleContext.Owner"/>). In a mirror
    /// match the same instance runs in both fighters' slots, each with its own state slice and config.
    /// </remarks>
    bool IsCharacterMechanic => false;
}
