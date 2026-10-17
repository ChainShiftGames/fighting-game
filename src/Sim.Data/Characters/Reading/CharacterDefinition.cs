using System.Collections.Generic;
using Sim.Characters;

namespace Sim.Data.Characters.Reading;

/// <summary>The validated contents of a character file, before its move files are read.</summary>
/// <param name="MoveIds">The move list, in input priority order.</param>
/// <param name="BasicStateMoves">Move index per <see cref="BasicState"/>, or <see cref="CharacterData.NoMove"/>.</param>
/// <param name="MoveRoles">The moves named for module jobs (ADR-0023), in file order.</param>
public sealed record CharacterDefinition(
    string Id,
    string DisplayName,
    CharacterStats Stats,
    IReadOnlyList<string> MoveIds,
    int[] BasicStateMoves,
    IReadOnlyList<MoveRole> MoveRoles);
