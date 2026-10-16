using System.Collections.Generic;
using Sim.Data.Characters.Compiled;

namespace Sim.Data.Characters.Reading;

/// <summary>The validated contents of a character file, before its move files are read.</summary>
/// <param name="MoveIds">The move list, in input priority order.</param>
/// <param name="BasicStateMoves">Move index per <see cref="BasicState"/>, or <see cref="CharacterData.NoMove"/>.</param>
public sealed record CharacterDefinition(
    string Id,
    string DisplayName,
    CharacterStats Stats,
    IReadOnlyList<string> MoveIds,
    int[] BasicStateMoves);
